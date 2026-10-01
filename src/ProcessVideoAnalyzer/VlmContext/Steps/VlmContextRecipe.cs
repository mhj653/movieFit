namespace ProcessVideoAnalyzer.VlmContext.Steps;

public sealed class VlmContextRecipe
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "New Recipe";
    public string Version { get; set; } = "1.0";
    public VlmContextRecipeDefaults Defaults { get; set; } = new();
    public List<VlmContextRecipeStep> Steps { get; set; } = new();
}
