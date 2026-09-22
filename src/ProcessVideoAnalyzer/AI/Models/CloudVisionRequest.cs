using ProcessVideoAnalyzer.Models;

namespace ProcessVideoAnalyzer.AI.Models;

public sealed class CloudVisionRequest
{
    public required ProcessSegment Segment { get; init; }
    public required IReadOnlyList<string> FramePaths { get; init; }
    public required string Prompt { get; init; }
    public required string Provider { get; init; }
    public required string Model { get; init; }
    public required string InputMode { get; init; }
    public int CandidateCount { get; init; } = 3;
    public string ResultLanguage { get; init; } = "ko";
}
