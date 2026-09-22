using ProcessVideoAnalyzer.Models;

namespace ProcessVideoAnalyzer.LocalAI.Core;

public sealed class VlmRequest
{
    public required ProcessSegment Segment { get; init; }
    public required IReadOnlyList<string> FramePaths { get; init; }
    public string OutputLanguage { get; init; } = "ko";
    public string Mode { get; init; } = "fast";
    public string PromptMode { get; init; } = "";
    public string DetectionFacts { get; init; } = "";
    public int InputImageLongEdge { get; init; }
    public int MaxOutputTokens { get; init; } = 64;
    public double Temperature { get; init; } = 0.1;
}

public sealed class VlmPreparedRequest
{
    public required VlmRequest Source { get; init; }
    public required string Prompt { get; init; }
    public required IReadOnlyList<string> ImagePaths { get; init; }
    public int MaxOutputTokens => Source.MaxOutputTokens;
    public double Temperature => Source.Temperature;
}

public sealed class VlmRawResponse
{
    public string Text { get; set; } = "";
    public int? OutputTokens { get; set; }
    public double? VisionEncodeMs { get; set; }
    public double? LlmPrefillMs { get; set; }
    public double? TokenGenerationMs { get; set; }
}
