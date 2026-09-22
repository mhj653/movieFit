namespace ProcessVideoAnalyzer.Models;

public sealed class ActionCandidate
{
    public string Action { get; set; } = "";
    public double MatchScore { get; set; }
}
