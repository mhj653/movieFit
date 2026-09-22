namespace ProcessVideoAnalyzer.Models;

public sealed class MotionSample
{
    public double TimeSeconds { get; set; }
    public double MotionScore { get; set; }
    public bool IsMotion { get; set; }
}
