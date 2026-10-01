namespace ProcessVideoAnalyzer.VlmContext.Steps;

public static class VlmContextStepTypes
{
    public const string InputImage = "input.image";
    public const string Sampling = "frames.sampling";
    public const string RoiFocus = "vision.roi";
    public const string CropResize = "vision.cropResize";
    public const string YoloHints = "vision.yoloHints";
    public const string OcrHints = "vision.ocrHints";
    public const string PromptContext = "ai.promptContext";
    public const string LocalVlmAnalyze = "ai.localVlmAnalyze";
    public const string ParseResult = "ai.parseResult";
}
