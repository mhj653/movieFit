namespace ProcessVideoAnalyzer.Models;

public sealed class VideoAnalysisResult
{
    public VideoMetadata Metadata { get; set; } = new();
    public AnalysisSettings AnalysisSettings { get; set; } = new();
    public List<AnalysisRoi> Rois { get; set; } = new();
    public List<MotionSample> MotionSamples { get; set; } = new();
    public List<ProcessSegment> BaseSegments { get; set; } = new();
    public List<ProcessSegment> Segments { get; set; } = new();
    public global::ProcessVideoAnalyzer.LocalAI.Core.VlmPerformanceMetrics? PerformanceMetrics { get; set; }
}
