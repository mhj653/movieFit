namespace ProcessVideoAnalyzer.VlmContext.Steps;

public sealed class VlmContextStepTrace
{
    public string StepId { get; init; } = "";
    public string StepType { get; init; } = "";
    public string StepName { get; init; } = "";
    public bool Enabled { get; init; }
    public double LatencyMs { get; init; }
    public string Summary { get; init; } = "";
}
