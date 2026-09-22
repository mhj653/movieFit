namespace ProcessVideoAnalyzer.LocalAI.Core;

public sealed class VlmModelProfile
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Provider { get; set; } = "llama.cpp";
    public string Adapter { get; set; } = "generic-llamacpp-vision";
    public string ModelPath { get; set; } = "";
    public string MmprojPath { get; set; } = "";
    public string RuntimePath { get; set; } = "runtimes/llama.cpp-cuda/llama-server.exe";
    public string EndpointUrl { get; set; } = "";
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 18080;
    public int GpuLayers { get; set; } = 99;
    public int ContextSize { get; set; } = 4096;
    public int Threads { get; set; } = 0;
    public int StartupTimeoutSeconds { get; set; } = 180;
    public string Quantization { get; set; } = "Q4_K_M";
    public string ModelVersion { get; set; } = "";
    public VlmCapabilities Capabilities { get; set; } = new();
    public VlmDefaults Defaults { get; set; } = new();
}

public sealed class VlmDefaults
{
    public int MaxFrames { get; set; } = 2;
    public int ImageLongEdge { get; set; } = 320;
    public int MaxOutputTokens { get; set; } = 320;
    public double Temperature { get; set; } = 0.1;
    public bool RoiCrop { get; set; } = true;
}
