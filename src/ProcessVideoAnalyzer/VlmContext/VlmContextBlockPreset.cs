namespace ProcessVideoAnalyzer.VlmContext;

public sealed class VlmContextBlockPreset
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "New Preset";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.Now;
    public VlmContextBlockSettings Settings { get; set; } = new();
}
