namespace ProcessVideoAnalyzer.VlmContext.Steps;

public sealed class VlmContextPipelineState
{
    public List<string> SourceFramePaths { get; } = new();
    public List<string> SelectedFramePaths { get; } = new();
    public List<string> PreparedFramePaths { get; } = new();
    public List<string> ContextHints { get; } = new();
    public List<VlmContextStepTrace> StepTraces { get; } = new();

    public VlmContextCandidate Candidate { get; init; } = new();
    public VlmContextBlockSettings BlockSettings { get; init; } = new();
    public VlmContextRecipe Recipe { get; init; } = new();
    public string InputType { get; init; } = "image";
}
