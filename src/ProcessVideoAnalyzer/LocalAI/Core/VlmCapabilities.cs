namespace ProcessVideoAnalyzer.LocalAI.Core;

public sealed class VlmCapabilities
{
    public bool Vision { get; set; } = true;
    public bool MultiImage { get; set; } = true;
    public bool StructuredOutput { get; set; } = true;
}
