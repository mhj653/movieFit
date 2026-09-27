namespace ProcessVideoAnalyzer.VlmContext;

public sealed class VlmContextBlockSettings
{
    public bool YoloEnabled { get; init; }
    public string YoloModelPath { get; init; } = "";
    public string YoloRuntime { get; init; } = "onnxruntime";
    public string YoloDevice { get; init; } = "cpu";
    public int YoloInputSize { get; init; } = 640;
    public double YoloConfidence { get; init; } = 0.45;
    public double YoloIou { get; init; } = 0.5;
    public string YoloLabels { get; init; } = "worker,hand,part,fixture,button,tool,machine";
    public bool YoloUseBoxesAsRoi { get; init; }
    public bool YoloDrawOverlay { get; init; }

    public string RoiMode { get; init; } = "fullFrame";
    public int RoiX { get; init; }
    public int RoiY { get; init; }
    public int RoiWidth { get; init; }
    public int RoiHeight { get; init; }
    public double RoiPadding { get; init; } = 0.12;

    public string CropMode { get; init; } = "fullFrame";
    public double CropPadding { get; init; } = 0.12;
    public bool CropKeepAspect { get; init; } = true;
    public int CropOutputLongEdge { get; init; } = 768;

    public bool OcrEnabled { get; init; }
    public string OcrEngine { get; init; } = "future";
    public string OcrLanguage { get; init; } = "ko";
    public bool OcrUseTextAsHint { get; init; } = true;

    public string SamplingMode { get; init; } = "uniform";
    public int SamplingFrameCount { get; init; } = 3;
    public bool SamplingIncludeTimestamp { get; init; } = true;
    public double SamplingMotionPeakWindowSec { get; init; } = 0.25;
}
