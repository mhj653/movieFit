using ProcessVideoAnalyzer.Detection;

namespace ProcessVideoAnalyzer.Models;

public sealed class AnalysisSettings
{
    public int SampleIntervalMs { get; set; } = 500;
    public double MotionStartThreshold { get; set; } = 5.0;
    public double MotionStopThreshold { get; set; } = 2.0;
    public double StateHoldMs { get; set; } = 400.0;
    public double MinimumSegmentDuration { get; set; } = 2.0;
    public string DefaultProvider { get; set; } = "local_vlm";
    public string InputMode { get; set; } = "opencvSegments";
    public int CandidateCount { get; set; } = 3;
    public string ResultLanguage { get; set; } = "ko";
    public double ConfidenceThreshold { get; set; } = 0.60;
    public int MaxFramesPerSegment { get; set; } = 2;
    public int FrameResolution { get; set; } = 320;
    public double MaxAiSegmentDuration { get; set; } = 5.0;
    public int MaxOutputTokens { get; set; } = 320;
    public double Temperature { get; set; } = 0.1;
    public string CropMode { get; set; } = "roiContext";
    public string OutputMode { get; set; } = "balanced";
    public int SingleImageLongEdge { get; set; } = 256;
    public int SingleImageMaxOutputTokens { get; set; } = 160;
    public string SingleImagePromptMode { get; set; } = "fast";
    public ObjectDetectionOptions ObjectDetection { get; set; } = new();
}
