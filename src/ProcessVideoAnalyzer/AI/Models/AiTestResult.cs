namespace ProcessVideoAnalyzer.AI.Models;

public sealed class AiTestResult
{
    public bool Success { get; set; }
    public string Status { get; set; } = "";
    public string Message { get; set; } = "";
}
