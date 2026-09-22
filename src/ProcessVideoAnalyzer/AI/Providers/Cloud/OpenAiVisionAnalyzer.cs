using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ProcessVideoAnalyzer.AI.Interfaces;
using ProcessVideoAnalyzer.AI.Models;

namespace ProcessVideoAnalyzer.AI.Providers.Cloud;

public sealed class OpenAiVisionAnalyzer : ICloudVisionAnalyzer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;

    public OpenAiVisionAnalyzer(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public string Provider => "openai";

    public async Task<CloudVisionResult> AnalyzeAsync(
        CloudVisionRequest request,
        string apiKey,
        CancellationToken cancellationToken = default)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var content = new List<object>
        {
            new { type = "input_text", text = request.Prompt }
        };

        foreach (var framePath in request.FramePaths.Where(File.Exists))
        {
            var base64 = Convert.ToBase64String(await File.ReadAllBytesAsync(framePath, cancellationToken));
            content.Add(new
            {
                type = "input_image",
                image_url = $"data:{GetMimeType(framePath)};base64,{base64}"
            });
        }

        message.Content = JsonContent.Create(new
        {
            model = request.Model,
            input = new[]
            {
                new { role = "user", content }
            },
            text = new
            {
                format = new { type = "json_object" }
            }
        }, options: JsonOptions);

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw CreateApiException("OpenAI", response.StatusCode, body);
        }

        using var document = JsonDocument.Parse(body);
        var text = ExtractOutputText(document.RootElement);
        var result = CloudVisionJsonParser.Parse(text);
        result.Sequence = request.Segment.Sequence;
        return result;
    }

    public async Task<IReadOnlyList<CloudVisionResult>> AnalyzeBatchAsync(
        IReadOnlyList<CloudVisionRequest> requests,
        string apiKey,
        CancellationToken cancellationToken = default)
    {
        var results = new List<CloudVisionResult>();
        foreach (var request in requests)
        {
            results.Add(await AnalyzeAsync(request, apiKey, cancellationToken));
        }

        return results;
    }

    public async Task<AiTestResult> TestAsync(
        string apiKey,
        string model,
        CancellationToken cancellationToken = default)
    {
        using var message = new HttpRequestMessage(HttpMethod.Get, "https://api.openai.com/v1/models");
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        using var response = await _httpClient.SendAsync(message, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return BuildTestResult("OpenAI", response.StatusCode, body);
    }

    private static string ExtractOutputText(JsonElement root)
    {
        if (root.TryGetProperty("output_text", out var outputText) &&
            outputText.ValueKind == JsonValueKind.String)
        {
            return outputText.GetString() ?? "";
        }

        if (!root.TryGetProperty("output", out var output) ||
            output.ValueKind != JsonValueKind.Array)
        {
            return root.GetRawText();
        }

        foreach (var item in output.EnumerateArray())
        {
            if (!item.TryGetProperty("content", out var content) ||
                content.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var contentItem in content.EnumerateArray())
            {
                if (contentItem.TryGetProperty("text", out var text) &&
                    text.ValueKind == JsonValueKind.String)
                {
                    return text.GetString() ?? "";
                }
            }
        }

        return root.GetRawText();
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

    private static Exception CreateApiException(string provider, HttpStatusCode statusCode, string body)
    {
        if (statusCode == (HttpStatusCode)429)
        {
            return new ApiQuotaExceededException(
                provider,
                statusCode,
                $"{provider} API quota or rate limit exceeded. {TrimBody(body)}");
        }

        return new InvalidOperationException($"{provider} API failed: {(int)statusCode} {Classify(statusCode)}. {TrimBody(body)}");
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
