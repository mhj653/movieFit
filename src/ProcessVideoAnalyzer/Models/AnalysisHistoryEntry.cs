namespace ProcessVideoAnalyzer.Models;

public sealed class AnalysisHistoryEntry
{
    public string Id { get; set; } = "";
    public string VideoPath { get; set; } = "";
    public string VideoFileName { get; set; } = "";
    public double DurationSeconds { get; set; }
    public DateTimeOffset AnalyzedAt { get; set; }
    public string Provider { get; set; } = "";
    public string Model { get; set; } = "";
    public int SegmentCount { get; set; }
    public double AverageScore { get; set; }
    public bool VideoExists { get; set; }
}
