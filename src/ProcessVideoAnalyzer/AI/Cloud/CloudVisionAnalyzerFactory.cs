using System.Net.Http;
using ProcessVideoAnalyzer.AI.Interfaces;
using ProcessVideoAnalyzer.AI.Providers.Cloud;

namespace ProcessVideoAnalyzer.AI.Cloud;

public sealed class CloudVisionAnalyzerFactory
{
    private readonly HttpClient _httpClient;

    public CloudVisionAnalyzerFactory(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public ICloudVisionAnalyzer Create(string provider)
    {
        return provider.ToLowerInvariant() switch
        {
            "gemini" => new GeminiVisionAnalyzer(_httpClient),
            "openai" => new OpenAiVisionAnalyzer(_httpClient),
            _ => throw new NotSupportedException($"Unsupported AI provider: {provider}")
        };
    }
}
