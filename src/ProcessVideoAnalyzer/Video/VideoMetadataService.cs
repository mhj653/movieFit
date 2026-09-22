using OpenCvSharp;
using ProcessVideoAnalyzer.Models;

namespace ProcessVideoAnalyzer.Video;

public sealed class VideoMetadataService
{
    public VideoMetadata Read(string videoPath)
    {
        using var capture = new VideoCapture(videoPath);
        if (!capture.IsOpened())
        {
            throw new InvalidOperationException("Video file could not be opened. The file may be damaged or the codec may be unsupported.");
        }

        var fps = capture.Get(VideoCaptureProperties.Fps);
        var frameCount = (int)capture.Get(VideoCaptureProperties.FrameCount);
        var duration = fps > 0 ? frameCount / fps : 0;

        return new VideoMetadata
        {
            VideoPath = videoPath,
            FileName = Path.GetFileName(videoPath),
            DurationSeconds = duration,
            Fps = fps,
            FrameCount = frameCount,
            Width = (int)capture.Get(VideoCaptureProperties.FrameWidth),
            Height = (int)capture.Get(VideoCaptureProperties.FrameHeight)
        };
    }
}
