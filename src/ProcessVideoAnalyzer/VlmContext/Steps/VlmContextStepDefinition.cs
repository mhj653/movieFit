namespace ProcessVideoAnalyzer.VlmContext.Steps;

public sealed class VlmContextStepDefinition
{
    public string Type { get; init; } = "";
    public string Name { get; init; } = "";
    public string Category { get; init; } = "";
    public string Description { get; init; } = "";
    public IReadOnlyList<string> Inputs { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Outputs { get; init; } = Array.Empty<string>();
    public IReadOnlyDictionary<string, string> DefaultSettings { get; init; } = new Dictionary<string, string>();
}
