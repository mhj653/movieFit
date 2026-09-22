namespace ProcessVideoAnalyzer.LocalAI.Core;

public sealed class VlmHealthStatus
{
    public string State { get; set; } = "not_ready";
    public string Message { get; set; } = "Model path is not configured.";
    public string ProviderId { get; set; } = "llama.cpp";
    public string ModelId { get; set; } = "";
    public string ModelName { get; set; } = "";
    public string AdapterId { get; set; } = "";
    public bool ModelResident { get; set; }
    public bool WarmupDone { get; set; }
    public bool Ready => State.Equals("ready", StringComparison.OrdinalIgnoreCase);
}
