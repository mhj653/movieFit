namespace ProcessVideoAnalyzer.VlmContext.Steps;

public sealed class VlmContextPipelineResult
{
    public List<string> SelectedFramePaths { get; init; } = new();
    public List<string> PreparedFramePaths { get; init; } = new();
    public string ContextText { get; init; } = "";
    public List<VlmContextStepTrace> StepTraces { get; init; } = new();
    public VlmContextRecipe Recipe { get; init; } = new();
}
