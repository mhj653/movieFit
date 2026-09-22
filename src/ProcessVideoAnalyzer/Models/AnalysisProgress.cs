namespace ProcessVideoAnalyzer.Models;

public sealed class AnalysisProgress
{
    public string Stage { get; set; } = "";
    public double Percent { get; set; }
    public string Message { get; set; } = "";
}
