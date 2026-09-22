namespace ProcessVideoAnalyzer.Detection;

public interface IObjectDetector
{
    string ProviderId { get; }
    Task<FrameDetectionResult> DetectAsync(
        string imagePath,
        ObjectDetectionOptions options,
        CancellationToken cancellationToken = default);
}
