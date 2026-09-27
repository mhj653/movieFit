namespace ProcessVideoAnalyzer.VlmContext;

public sealed class VlmContextCandidate
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public bool Enabled { get; init; } = true;
    public string AdvancedPresetId { get; init; } = "none";
    public string AdvancedPresetName { get; init; } = "None";
    public VlmContextBlockSettings? BlockSettings { get; init; }
    public int ImageLongEdge { get; init; }
    public int FrameCount { get; init; } = 1;
    public bool MotionSummary { get; init; }
    public bool YoloHints { get; init; }
    public bool OcrHints { get; init; }
    public string RoiMode { get; init; } = "fullFrame";
    public string CropMode { get; init; } = "fullFrame";
    public string SamplingMode { get; init; } = "uniform";
    public double YoloConfidence { get; init; } = 0.45;
    public string TargetLabels { get; init; } = "";
    public string PromptMode { get; init; } = "fast";
    public int MaxOutputTokens { get; init; } = 120;
    public string Description { get; init; } = "";
}
