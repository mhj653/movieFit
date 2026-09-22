namespace ProcessVideoAnalyzer.Models;

public sealed class VideoMetadata
{
    public string VideoPath { get; set; } = "";
    public string FileName { get; set; } = "";
    public double DurationSeconds { get; set; }
    public double Fps { get; set; }
    public int FrameCount { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}
