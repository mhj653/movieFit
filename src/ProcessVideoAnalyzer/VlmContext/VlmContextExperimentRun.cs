namespace ProcessVideoAnalyzer.VlmContext;

public sealed class VlmContextExperimentRun
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ExperimentId { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.Now;
    public string InputPath { get; init; } = "";
    public string InputFileName { get; init; } = "";
    public string InputType { get; init; } = "image";
    public string InputLabel { get; init; } = "";
    public string ModelName { get; init; } = "";
    public VlmContextCandidate Candidate { get; init; } = new();
    public string Description { get; init; } = "";
    public double LatencyMs { get; init; }
    public double PreprocessMs { get; init; }
    public double VlmMs { get; init; }
    public bool IsBest { get; set; }
    public string Prompt { get; init; } = "";
    public string RequestJson { get; init; } = "";
    public string RawResponse { get; init; } = "";
    public VlmContextBlockSettings BlockSettings { get; init; } = new();
    public List<string> FramePaths { get; init; } = new();
}
