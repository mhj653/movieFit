namespace ProcessVideoAnalyzer.Detection;

public sealed class NoOpObjectDetector : IObjectDetector
{
    public string ProviderId => "none";

    public Task<FrameDetectionResult> DetectAsync(
        string imagePath,
        ObjectDetectionOptions options,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new FrameDetectionResult
        {
            FramePath = imagePath
        });
    }
}
