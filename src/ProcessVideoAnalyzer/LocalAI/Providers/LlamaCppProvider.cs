using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using ProcessVideoAnalyzer.LocalAI.Core;

namespace ProcessVideoAnalyzer.LocalAI.Providers;

public sealed class LlamaCppProvider : ILocalVlmProvider
{
    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromMinutes(5)
    };

    private VlmModelProfile? _model;
    private Process? _serverProcess;
    private Uri? _endpoint;
    private bool _ownsServerProcess;
    private VlmHealthStatus _status = new()
    {
        ProviderId = "llama.cpp",
        State = "not_ready",
        Message = "Model path is not configured."
    };

    public string ProviderId => "llama.cpp";

    public async Task InitializeAsync(VlmModelProfile model, CancellationToken cancellationToken)
    {
        await UnloadAsync();

        _model = model;
        _status = new VlmHealthStatus
        {
            ProviderId = ProviderId,
            ModelId = model.Id,
            ModelName = model.Name,
            AdapterId = model.Adapter
        };

        var configuredEndpoint = NormalizeEndpoint(model.EndpointUrl);
        if (configuredEndpoint is not null)
        {
            _endpoint = configuredEndpoint;
            await MarkReadyWhenHealthyAsync(cancellationToken);
            return;
        }

        var modelPath = ResolvePath(model.ModelPath);
        if (string.IsNullOrWhiteSpace(model.ModelPath) || !File.Exists(modelPath))
        {
            MarkNotReady($"Model file was not found: {modelPath}");
            return;
        }

        var mmprojPath = ResolveOptionalPath(model.MmprojPath);
        if (!string.IsNullOrWhiteSpace(model.MmprojPath) && !File.Exists(mmprojPath))
        {
            MarkNotReady($"Vision projector file was not found: {mmprojPath}");
            return;
        }

        var runtimePath = ResolveRuntimePath(model.RuntimePath);
        if (runtimePath is null)
        {
            MarkNotReady("llama-server.exe was not found. Set Runtime Executable in Local AI Settings.");
            return;
        }

        _endpoint = new Uri($"http://{model.Host}:{model.Port}");
        if (!await IsHealthyAsync(_endpoint, cancellationToken))
        {
            StartServerProcess(runtimePath, modelPath, mmprojPath, model);
            await WaitForHealthyAsync(model.StartupTimeoutSeconds, cancellationToken);
        }

        await MarkReadyWhenHealthyAsync(cancellationToken);
    }

    public async Task WarmupAsync(CancellationToken cancellationToken)
    {
        if (!_status.Ready || _endpoint is null)
        {
            return;
        }

        try
        {
            using var response = await PostChatAsync(
                "Return OK.",
                Array.Empty<string>(),
                maxTokens: 4,
                temperature: 0,
                useJsonMode: false,
                cancellationToken);
            response.EnsureSuccessStatusCode();
            _status.WarmupDone = true;
            _status.Message = "Local VLM runtime is ready.";
        }
        catch
        {
            _status.WarmupDone = false;
        }
    }

    public async Task<VlmRawResponse> AnalyzeAsync(VlmPreparedRequest request, CancellationToken cancellationToken)
    {
        if (!_status.Ready || _endpoint is null)
        {
            throw new InvalidOperationException(_status.Message);
        }

        if (request.ImagePaths.Count == 0)
        {
            throw new InvalidOperationException("No frame paths were provided for Local VLM analysis.");
        }

        var stopwatch = Stopwatch.StartNew();
        using var response = await PostChatAsync(
            request.Prompt,
            request.ImagePaths,
            request.MaxOutputTokens,
            request.Temperature,
            useJsonMode: true,
            cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode && response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            using var retry = await PostChatAsync(
                request.Prompt,
                request.ImagePaths,
                request.MaxOutputTokens,
                request.Temperature,
                useJsonMode: false,
                cancellationToken);
            body = await retry.Content.ReadAsStringAsync(cancellationToken);
            EnsureSuccess(retry, body);
        }
        else
        {
            EnsureSuccess(response, body);
        }

        stopwatch.Stop();
        var (text, tokens) = ParseOpenAiCompatibleResponse(body);
        return new VlmRawResponse
        {
            Text = text,
            OutputTokens = tokens,
            TokenGenerationMs = stopwatch.Elapsed.TotalMilliseconds
        };
    }

    private static void EnsureSuccess(HttpResponseMessage response, string body)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        throw new HttpRequestException(
            $"Local VLM endpoint failed: {(int)response.StatusCode} {response.ReasonPhrase}. {body}");
    }

    public Task<VlmHealthStatus> GetStatusAsync()
    {
        return Task.FromResult(_status);
    }

    public Task UnloadAsync()
    {
        if (_ownsServerProcess && _serverProcess is { HasExited: false })
        {
            try
            {
                _serverProcess.Kill(entireProcessTree: true);
                _serverProcess.WaitForExit(3000);
            }
            catch
            {
                // The runtime may already be closing; status below remains authoritative for the UI.
            }
        }

        _serverProcess?.Dispose();
        _serverProcess = null;
        _ownsServerProcess = false;
        _endpoint = null;
        _status.ModelResident = false;
        _status.WarmupDone = false;
        _status.State = "not_ready";
        _status.Message = "Model unloaded.";
        return Task.CompletedTask;
    }

    private async Task<HttpResponseMessage> PostChatAsync(
        string prompt,
        IReadOnlyList<string> imagePaths,
        int maxTokens,
        double temperature,
        bool useJsonMode,
        CancellationToken cancellationToken)
    {
        if (_endpoint is null)
        {
            throw new InvalidOperationException("llama.cpp endpoint is not initialized.");
        }

        var content = new List<object>
        {
            new { type = "text", text = prompt }
        };

        foreach (var imagePath in imagePaths.Where(File.Exists))
        {
            content.Add(new
            {
                type = "image_url",
                image_url = new
                {
                    url = $"data:{GetMimeType(imagePath)};base64,{Convert.ToBase64String(await File.ReadAllBytesAsync(imagePath, cancellationToken))}"
                }
            });
        }

        var payload = new Dictionary<string, object?>
        {
            ["model"] = _model?.Id ?? "local-vlm",
            ["messages"] = new[]
            {
                new { role = "user", content }
            },
            ["temperature"] = temperature,
            ["max_tokens"] = maxTokens
        };

        if (useJsonMode)
        {
            payload["response_format"] = new { type = "json_object" };
        }

        return await Http.PostAsJsonAsync(
            new Uri(_endpoint, "/v1/chat/completions"),
            payload,
            cancellationToken);
    }

    private async Task MarkReadyWhenHealthyAsync(CancellationToken cancellationToken)
    {
        if (_endpoint is not null && await IsHealthyAsync(_endpoint, cancellationToken))
        {
            _status.State = "ready";
            _status.Message = $"Local VLM runtime is ready: {_endpoint}";
            _status.ModelResident = true;
            return;
        }

        MarkNotReady($"llama.cpp endpoint is not responding: {_endpoint}");
    }

    private async Task WaitForHealthyAsync(int timeoutSeconds, CancellationToken cancellationToken)
    {
        if (_endpoint is null)
        {
            return;
        }

        var timeout = TimeSpan.FromSeconds(Math.Max(10, timeoutSeconds));
        var started = Stopwatch.StartNew();
        while (started.Elapsed < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await IsHealthyAsync(_endpoint, cancellationToken))
            {
                return;
            }

            await Task.Delay(1000, cancellationToken);
        }
    }

    private static async Task<bool> IsHealthyAsync(Uri endpoint, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await Http.GetAsync(new Uri(endpoint, "/health"), cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private void StartServerProcess(string runtimePath, string modelPath, string mmprojPath, VlmModelProfile model)
    {
        var args = new List<string>
        {
            "--host", model.Host,
            "--port", model.Port.ToString(),
            "--model", modelPath,
            "--ctx-size", model.ContextSize.ToString()
        };

        if (!string.IsNullOrWhiteSpace(mmprojPath))
        {
            args.Add("--mmproj");
            args.Add(mmprojPath);
        }

        if (model.GpuLayers > 0)
        {
            args.Add("--n-gpu-layers");
            args.Add(model.GpuLayers.ToString());
        }

        if (model.Threads > 0)
        {
            args.Add("--threads");
            args.Add(model.Threads.ToString());
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = runtimePath,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(runtimePath) ?? AppContext.BaseDirectory
        };

        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        _serverProcess = Process.Start(startInfo);
        _ownsServerProcess = _serverProcess is not null;
    }

    private static (string Text, int? OutputTokens) ParseOpenAiCompatibleResponse(string body)
    {
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        string? text = null;

        if (root.TryGetProperty("choices", out var choices) &&
            choices.ValueKind == JsonValueKind.Array &&
            choices.GetArrayLength() > 0)
        {
            var choice = choices[0];
            if (choice.TryGetProperty("message", out var message))
            {
                text = ExtractContentText(message);
            }

            if (string.IsNullOrWhiteSpace(text) && choice.TryGetProperty("text", out var choiceText))
            {
                text = choiceText.GetString();
            }
        }

        if (string.IsNullOrWhiteSpace(text) && root.TryGetProperty("content", out var content))
        {
            text = content.GetString();
        }

        if (string.IsNullOrWhiteSpace(text) && root.TryGetProperty("response", out var response))
        {
            text = response.GetString();
        }

        int? tokens = null;
        if (root.TryGetProperty("usage", out var usage) &&
            usage.TryGetProperty("completion_tokens", out var completionTokens) &&
            completionTokens.TryGetInt32(out var tokenCount))
        {
            tokens = tokenCount;
        }

        return (text ?? body, tokens);
    }

    private static string? ExtractContentText(JsonElement message)
    {
        if (!message.TryGetProperty("content", out var content))
        {
            return null;
        }

        if (content.ValueKind == JsonValueKind.String)
        {
            return content.GetString();
        }

        if (content.ValueKind != JsonValueKind.Array)
        {
            return content.ToString();
        }

        var parts = new List<string>();
        foreach (var item in content.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                parts.Add(item.GetString() ?? "");
            }
            else if (item.TryGetProperty("text", out var text))
            {
                parts.Add(text.GetString() ?? "");
            }
        }

        return string.Join("", parts);
    }

    private static Uri? NormalizeEndpoint(string endpointUrl)
    {
        if (string.IsNullOrWhiteSpace(endpointUrl))
        {
            return null;
        }

        return Uri.TryCreate(endpointUrl.TrimEnd('/'), UriKind.Absolute, out var uri)
            ? uri
            : null;
    }

    private static string? ResolveRuntimePath(string configuredPath)
    {
        var candidates = new[]
        {
            configuredPath,
            "runtimes/llama.cpp/llama-server.exe",
            "llama.cpp/llama-server.exe",
            "llama-server.exe"
        };

        return candidates
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(ResolvePath)
            .FirstOrDefault(File.Exists);
    }

    private static string ResolveOptionalPath(string path)
    {
        return string.IsNullOrWhiteSpace(path) ? "" : ResolvePath(path);
    }

    private static string ResolvePath(string path)
    {
        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
    }

    private static string GetMimeType(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        return extension switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "image/jpeg"
        };
    }

    private void MarkNotReady(string message)
    {
        _status.State = "not_ready";
        _status.Message = message;
        _status.ModelResident = false;
        _status.WarmupDone = false;
    }
}
