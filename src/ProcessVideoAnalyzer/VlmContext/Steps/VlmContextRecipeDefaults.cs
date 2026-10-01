namespace ProcessVideoAnalyzer.VlmContext.Steps;

public sealed class VlmContextRecipeDefaults
{
    public int ImageLongEdge { get; set; } = 768;
    public int FrameCount { get; set; } = 1;
    public string SamplingMode { get; set; } = "uniform";
    public string PromptMode { get; set; } = "description";
    public int MaxOutputTokens { get; set; } = 120;
    public string ResultLanguage { get; set; } = "ko";
    public double Temperature { get; set; } = 0.1;
}
