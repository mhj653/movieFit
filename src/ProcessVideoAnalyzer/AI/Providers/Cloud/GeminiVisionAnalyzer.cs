using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using ProcessVideoAnalyzer.AI.Interfaces;
using ProcessVideoAnalyzer.AI.Models;

namespace ProcessVideoAnalyzer.AI.Providers.Cloud;

public sealed class GeminiVisionAnalyzer : ICloudVisionAnalyzer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan[] TransientRetryDelays =
    {
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(4),
        TimeSpan.FromSeconds(8)
    };

    private readonly HttpClient _httpClient;

    public GeminiVisionAnalyzer(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public string Provider => "gemini";

    public async Task<CloudVisionResult> AnalyzeAsync(
        CloudVisionRequest request,
        string apiKey,
        CancellationToken cancellationToken = default)
    {
        var parts = new List<object> { new { text = request.Prompt } };
        foreach (var framePath in request.FramePaths.Where(File.Exists))
        {
            parts.Add(new
            {
                inline_data = new
                {
                    mime_type = GetMimeType(framePath),
                    data = Convert.ToBase64String(await File.ReadAllBytesAsync(framePath, cancellationToken))
                }
            });
        }

        var payload = new
        {
            contents = new[]
            {
                new { role = "user", parts }
            },
            generationConfig = new
            {
                response_mime_type = "application/json",
                temperature = 0.1
            }
        };

        var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(request.Model)}:generateContent?key={Uri.EscapeDataString(apiKey)}";
        var body = await PostWithTransientRetryAsync(endpoint, payload, request.Model, cancellationToken);

        using var document = JsonDocument.Parse(body);
        var text = document.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? "";

        var result = CloudVisionJsonParser.Parse(text);
        result.Sequence = request.Segment.Sequence;
        return result;
    }

    public async Task<IReadOnlyList<CloudVisionResult>> AnalyzeBatchAsync(
        IReadOnlyList<CloudVisionRequest> requests,
        string apiKey,
        CancellationToken cancellationToken = default)
    {
        if (requests.Count == 0)
        {
            return Array.Empty<CloudVisionResult>();
        }

        var first = requests[0];
        var parts = new List<object>
        {
            new { text = BuildBatchPrompt(requests) }
        };

        foreach (var request in requests)
        {
            parts.Add(new { text = $"Segment {request.Segment.Sequence} frames:" });
            foreach (var framePath in request.FramePaths.Where(File.Exists))
            {
                parts.Add(new
                {
                    inline_data = new
                    {
                        mime_type = GetMimeType(framePath),
                        data = Convert.ToBase64String(await File.ReadAllBytesAsync(framePath, cancellationToken))
                    }
                });
            }
        }

        var payload = new
        {
            contents = new[]
            {
                new { role = "user", parts }
            },
            generationConfig = new
            {
                response_mime_type = "application/json",
                temperature = 0.1
            }
        };

        var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(first.Model)}:generateContent?key={Uri.EscapeDataString(apiKey)}";
        var body = await PostWithTransientRetryAsync(endpoint, payload, first.Model, cancellationToken);

        using var document = JsonDocument.Parse(body);
        var text = document.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? "";

        return CloudVisionJsonParser.ParseBatch(text);
    }

    public async Task<AiTestResult> TestAsync(
        string apiKey,
        string model,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            contents = new[]
            {
                new { role = "user", parts = new[] { new { text = "Return only OK." } } }
            }
        };

        var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(model)}:generateContent?key={Uri.EscapeDataString(apiKey)}";
        using var response = await _httpClient.PostAsJsonAsync(endpoint, payload, JsonOptions, cancellationToken);
        return BuildTestResult("Gemini", response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
    }

    private async Task<string> PostWithTransientRetryAsync(
        string endpoint,
        object payload,
        string model,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            using var response = await _httpClient.PostAsJsonAsync(endpoint, payload, JsonOptions, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return body;
            }

            if (response.StatusCode == HttpStatusCode.ServiceUnavailable &&
                attempt < TransientRetryDelays.Length)
            {
                await Task.Delay(TransientRetryDelays[attempt], cancellationToken);
                continue;
            }

            throw CreateApiException("Gemini", model, response.StatusCode, body);
        }
    }

    private static string GetMimeType(string path)
    {
        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "image/jpeg"
        };
    }

    private static Exception CreateApiException(string provider, string model, HttpStatusCode statusCode, string body)
    {
        if (statusCode == (HttpStatusCode)429)
        {
            return new ApiQuotaExceededException(
                provider,
                statusCode,
                $"{provider} API quota or rate limit exceeded. {TrimBody(body)}");
        }

        if (statusCode == HttpStatusCode.ServiceUnavailable)
        {
            return new ApiModelUnavailableException(
                provider,
                model,
                statusCode,
                $"{provider} model '{model}' is temporarily unavailable after retrying 2s, 4s, and 8s. {TrimBody(body)}");
        }

        return new InvalidOperationException($"{provider} API failed: {(int)statusCode} {Classify(statusCode)}. {TrimBody(body)}");
    }

    private static string BuildBatchPrompt(IReadOnlyList<CloudVisionRequest> requests)
    {
        var first = requests[0];
        var languageInstruction = first.ResultLanguage.Equals("en", StringComparison.OrdinalIgnoreCase)
            ? "Write all user-facing text values in English."
            : "모든 사용자 표시용 텍스트 값은 한국어로 작성하세요.";

        var lines = new List<string>
        {
            "제조 설비 자동화 관점에서 공정 영상을 분석합니다.",
            "아래 여러 Segment를 한 번의 요청으로 분석하되, 각 Segment의 start/end/duration은 이미 OpenCV가 계산한 값이므로 변경하지 마세요.",
            "OpenCV label은 보조 힌트입니다. 실제 작업자 동작 또는 설비 동작이 보이면 IDLE로 판단하지 마세요.",
            languageInstruction,
            "응답은 markdown 없이 JSON object 하나만 반환하세요.",
            $"각 Segment마다 Top {Math.Clamp(first.CandidateCount, 1, 5)} 후보를 candidates에 포함하세요.",
            "Schema: {\"segments\":[{\"sequence\":1,\"schemaVersion\":\"1.2\",\"primary_action\":\"체결\",\"description\":\"작업자가 제품 위치를 맞춘 뒤 체결 동작을 수행하는 것으로 추정됩니다.\",\"reason\":\"손 이동 후 대상 위치가 고정되고 반복적인 미세 움직임이 관찰됩니다.\",\"semantic\":{\"actorType\":\"human|machine|mixed|unknown\",\"motionType\":\"move|hold|contact|release|idle|unknown\",\"processRole\":\"prepare|load|unload|transfer|position|process|fasten|inspect|wait|return|other|unknown\",\"stateType\":\"active|idle|waiting|transition|unknown\",\"actionName\":\"체결\",\"targetObject\":\"제품\",\"toolOrActor\":\"전동 드라이버\",\"description\":\"관찰된 동작 설명\",\"pathConsistency\":\"consistent|variable|unknown\",\"positionConsistency\":\"consistent|variable|unknown\",\"evidence\":[\"보이는 근거 1\",\"보이는 근거 2\"]},\"observation\":{\"motionDirection\":\"좌에서 우\",\"motionLevel\":\"low|medium|high|unknown\"},\"interaction\":{\"interactionType\":\"human_to_machine|machine_to_product|human_to_product|none|unknown\",\"dependency\":\"previous_step|next_step|independent|waiting|unknown\",\"waitReason\":\"\"},\"repeatability\":{\"type\":\"cyclic|one_time|irregular|unknown\"},\"quality\":{\"actorConfidence\":0.8,\"actionConfidence\":0.75,\"uncertainties\":[\"불확실한 점\"]},\"candidates\":[{\"action\":\"체결\",\"match_score\":78},{\"action\":\"삽입\",\"match_score\":17},{\"action\":\"위치 고정\",\"match_score\":5}]}]}",
            "",
            "Segments:"
        };

        foreach (var request in requests)
        {
            lines.Add($"- sequence={request.Segment.Sequence}, start={request.Segment.StartTime:0.000}s, end={request.Segment.EndTime:0.000}s, duration={request.Segment.Duration:0.000}s, opencv_motion={request.Segment.MotionType}, average_motion_score={request.Segment.AverageMotionScore:0.00}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static AiTestResult BuildTestResult(string provider, HttpStatusCode statusCode, string body)
    {
        return new AiTestResult
        {
            Success = (int)statusCode is >= 200 and < 300,
            Status = Classify(statusCode),
            Message = $"{provider}: {(int)statusCode} {TrimBody(body)}"
        };
    }

    private static string Classify(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => "Invalid key or permission",
            (HttpStatusCode)429 => "Rate limited",
            _ when (int)statusCode >= 500 => "Provider error",
            _ => statusCode.ToString()
        };
    }

    private static string TrimBody(string body)
    {
        return string.IsNullOrWhiteSpace(body) ? "" : body[..Math.Min(body.Length, 240)];
    }
}
