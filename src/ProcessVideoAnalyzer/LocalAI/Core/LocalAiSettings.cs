namespace ProcessVideoAnalyzer.LocalAI.Core;

public sealed class LocalAiSettings
{
    public string DefaultProvider { get; set; } = "local_vlm";
    public string RuntimeProvider { get; set; } = "llama.cpp";
    public string ActiveModelId { get; set; } = "qwen3-vl-4b-q4km";
    public string ActiveModelName { get; set; } = "Qwen3-VL 4B Instruct";
    public string RuntimePath { get; set; } = "runtimes/llama.cpp-cuda/llama-server.exe";
    public string ModelPath { get; set; } = "";
    public string MmprojPath { get; set; } = "";
    public string EndpointUrl { get; set; } = "";
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 18080;
    public int GpuLayers { get; set; } = 99;
    public int ContextSize { get; set; } = 4096;
    public int StartupTimeoutSeconds { get; set; } = 180;
    public string Device { get; set; } = "gpu";
    public string InputMode { get; set; } = "opencvSegments";
    public int CandidateCount { get; set; } = 3;
    public string ResultLanguage { get; set; } = "ko";
    public int MaxFrames { get; set; } = 2;
    public int ImageLongEdge { get; set; } = 320;
    public string CropMode { get; set; } = "roiContext";
    public string OutputMode { get; set; } = "balanced";
    public int MaxOutputTokens { get; set; } = 320;
    public int SingleImageLongEdge { get; set; } = 256;
    public int SingleImageMaxOutputTokens { get; set; } = 160;
    public string SingleImagePromptMode { get; set; } = "fast";
    public double Temperature { get; set; } = 0.1;
    public bool StructuredOutput { get; set; } = true;
    public bool WarmupOnStart { get; set; } = false;
}
