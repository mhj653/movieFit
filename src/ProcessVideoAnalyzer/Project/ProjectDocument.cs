using ProcessVideoAnalyzer.Models;

namespace ProcessVideoAnalyzer.Project;

public sealed class ProjectDocument
{
    public string Version { get; set; } = "0.1";
    public string VideoPath { get; set; } = "";
    public double VideoDuration { get; set; }
    public AnalysisSettings AnalysisSettings { get; set; } = new();
    public List<AnalysisRoi> Rois { get; set; } = new();
    public List<MotionSample> MotionSamples { get; set; } = new();
    public List<ProcessSegment> Segments { get; set; } = new();
}
