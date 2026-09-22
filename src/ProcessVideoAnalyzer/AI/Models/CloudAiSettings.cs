namespace ProcessVideoAnalyzer.AI.Models;

public sealed class CloudAiSettings
{
    public string DefaultProvider { get; set; } = "gemini";
    public string InputMode { get; set; } = "selectedFrames";
    public int CandidateCount { get; set; } = 3;
    public string ResultLanguage { get; set; } = "ko";
    public string GeminiModel { get; set; } = "gemini-3.5-flash-lite";
    public string GeminiCustomModel { get; set; } = "";
    public string OpenAiModel { get; set; } = "gpt-5.6-sol";
    public string OpenAiCustomModel { get; set; } = "";
}
