namespace ProcessVideoAnalyzer.Models;

public sealed class AnalysisRoi
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "ROI";
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public bool Enabled { get; set; } = true;
}
