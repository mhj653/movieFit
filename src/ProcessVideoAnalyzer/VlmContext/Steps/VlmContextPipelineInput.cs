using ProcessVideoAnalyzer.Models;

namespace ProcessVideoAnalyzer.VlmContext.Steps;

public sealed class VlmContextPipelineInput
{
    public required IReadOnlyList<string> SourceFramePaths { get; init; }
    public required string InputType { get; init; }
    public required VlmContextCandidate Candidate { get; init; }
    public required VlmContextBlockSettings BlockSettings { get; init; }
    public required VlmContextRecipe Recipe { get; init; }
    public ProcessSegment? SourceSegment { get; init; }
}
