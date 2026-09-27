namespace ProcessVideoAnalyzer.VlmContext;

public sealed class VlmContextBlockTestResult
{
    public string SourcePath { get; init; } = "";
    public string ProcessedPath { get; init; } = "";
    public int SourceWidth { get; init; }
    public int SourceHeight { get; init; }
    public int OutputWidth { get; init; }
    public int OutputHeight { get; init; }
    public double LatencyMs { get; init; }
    public string Hints { get; init; } = "";
    public VlmContextBlockSettings Settings { get; init; } = new();
}
