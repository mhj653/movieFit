namespace ProcessVideoAnalyzer.Detection;

public sealed class ObjectDetectionOptions
{
    public bool Enabled { get; set; }
    public string Provider { get; set; } = "none";
    public string ModelPath { get; set; } = "";
    public int InputSize { get; set; } = 640;
    public float ConfidenceThreshold { get; set; } = 0.35f;
    public float NmsThreshold { get; set; } = 0.45f;
}

public sealed class DetectedObject
{
    public string Label { get; set; } = "";
    public float Confidence { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
}

public sealed class FrameDetectionResult
{
    public string FramePath { get; set; } = "";
    public List<DetectedObject> Objects { get; set; } = new();
}

public sealed class SegmentDetectionContext
{
    public string Provider { get; set; } = "none";
    public string ModelPath { get; set; } = "";
    public List<FrameDetectionResult> Frames { get; set; } = new();
    public string PromptFacts { get; set; } = "";
}
