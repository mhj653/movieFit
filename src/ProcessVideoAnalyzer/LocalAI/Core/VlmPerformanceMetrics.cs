namespace ProcessVideoAnalyzer.LocalAI.Core;

public sealed class VlmPerformanceMetrics
{
    public double OpenCvSegmentationMs { get; set; }
    public double FrameSelectionMs { get; set; }
    public double RoiCropResizeMs { get; set; }
    public double? VisionEncodeMs { get; set; }
    public double? LlmPrefillMs { get; set; }
    public double? TokenGenerationMs { get; set; }
    public double JsonParsingMs { get; set; }
    public double TotalMs { get; set; }
    public string ProviderId { get; set; } = "";
    public string ModelId { get; set; } = "";
    public string ModelName { get; set; } = "";
    public string AdapterId { get; set; } = "";
    public string Quantization { get; set; } = "";
    public bool ModelResident { get; set; }
    public bool WarmupDone { get; set; }
    public int InputFrames { get; set; }
    public int ImageLongEdge { get; set; }
    public int? OutputTokens { get; set; }
    public double? PeakVramMb { get; set; }
    public double? PeakRamMb { get; set; }
    public double VideoDurationSec { get; set; }
    public int SegmentCount { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;
}
