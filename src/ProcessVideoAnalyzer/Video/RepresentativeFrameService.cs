using OpenCvSharp;
using ProcessVideoAnalyzer.Models;

namespace ProcessVideoAnalyzer.Video;

public sealed class RepresentativeFrameService
{
    public Task ExtractAsync(
        string videoPath,
        IReadOnlyList<ProcessSegment> segments,
        AnalysisSettings settings,
        string outputRoot,
        IProgress<AnalysisProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            Directory.CreateDirectory(outputRoot);
            using var capture = new VideoCapture(videoPath);
            if (!capture.IsOpened())
            {
                throw new InvalidOperationException("Video file could not be opened for frame extraction.");
            }

            var fps = capture.Get(VideoCaptureProperties.Fps);
            var frameCount = capture.Get(VideoCaptureProperties.FrameCount);
            var videoDuration = fps > 0 && frameCount > 0 ? frameCount / fps : (double?)null;
            var requests = new List<FrameRequest>();
            var requestOrder = 0;

            for (var i = 0; i < segments.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var segment = segments[i];
                var segmentFolder = Path.Combine(outputRoot, $"segment_{segment.Sequence:000}");
                Directory.CreateDirectory(segmentFolder);

                var times = BuildFrameTimes(segment, settings.MaxFramesPerSegment);
                segment.FramePaths.Clear();
                segment.ThumbnailPath = "";

                for (var frameIndex = 0; frameIndex < times.Count; frameIndex++)
                {
                    var framePath = Path.Combine(segmentFolder, $"frame_{frameIndex + 1:00}_{times[frameIndex]:0.000}.jpg");
                    requests.Add(new FrameRequest(
                        segment,
                        framePath,
                        times[frameIndex],
                        ToFrameIndex(times[frameIndex], fps, frameCount),
                        requestOrder++));
                }
            }

            ExtractSequential(capture, requests, settings.FrameResolution, cancellationToken);

            for (var i = 0; i < segments.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var segment = segments[i];
                if (segment.FramePaths.Count > 0)
                {
                    segment.ThumbnailPath = segment.FramePaths[segment.FramePaths.Count / 2];
                }
                else
                {
                    var segmentFolder = Path.Combine(outputRoot, $"segment_{segment.Sequence:000}");
                    var thumbnailTime = segment.StartTime + segment.Duration / 2.0;
                    var thumbnailPath = Path.Combine(segmentFolder, "thumbnail.jpg");
                    if (WriteFrame(capture, thumbnailTime, thumbnailPath, videoDuration, settings.FrameResolution))
                    {
                        segment.ThumbnailPath = thumbnailPath;
                        segment.FramePaths.Add(thumbnailPath);
                    }
                }

                progress?.Report(new AnalysisProgress
                {
                    Stage = "Extracting Frames",
                    Percent = 70 + (i + 1) / (double)Math.Max(segments.Count, 1) * 10,
                    Message = $"Segment {segment.Sequence}/{segments.Count}"
                });
            }
        }, cancellationToken);
    }

    private static void ExtractSequential(
        VideoCapture capture,
        IReadOnlyList<FrameRequest> requests,
        int imageLongEdge,
        CancellationToken cancellationToken)
    {
        if (requests.Count == 0)
        {
            return;
        }

        var ordered = requests
            .OrderBy(x => x.FrameIndex)
            .ThenBy(x => x.Order)
            .ToList();
        var failed = new List<FrameRequest>();
        var currentFrame = -1;
        capture.Set(VideoCaptureProperties.PosFrames, 0);
        using var frame = new Mat();

        for (var i = 0; i < ordered.Count;)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var targetFrame = ordered[i].FrameIndex;
            while (currentFrame + 1 < targetFrame)
            {
                if (!capture.Grab())
                {
                    failed.AddRange(ordered.Skip(i));
                    RetryFailed(capture, failed, imageLongEdge, cancellationToken);
                    return;
                }

                currentFrame++;
            }

            if (!capture.Read(frame) || frame.Empty())
            {
                failed.AddRange(ordered.Skip(i));
                break;
            }

            currentFrame++;
            while (i < ordered.Count && ordered[i].FrameIndex == targetFrame)
            {
                var request = ordered[i];
                if (WriteFrameMat(frame, request.Path, imageLongEdge))
                {
                    request.Segment.FramePaths.Add(request.Path);
                }
                else
                {
                    failed.Add(request);
                }

                i++;
            }
        }

        RetryFailed(capture, failed, imageLongEdge, cancellationToken);
    }

    private static void RetryFailed(
        VideoCapture capture,
        IReadOnlyList<FrameRequest> failed,
        int imageLongEdge,
        CancellationToken cancellationToken)
    {
        foreach (var request in failed)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (WriteFrame(capture, request.TimeSeconds, request.Path, null, imageLongEdge))
            {
                request.Segment.FramePaths.Add(request.Path);
            }
        }
    }

    private static List<double> BuildFrameTimes(ProcessSegment segment, int maxFrames)
    {
        var count = Math.Max(1, maxFrames);
        if (segment.Duration <= 0.001)
        {
            return new List<double> { segment.StartTime };
        }

        if (count == 1)
        {
            return new List<double> { segment.StartTime + segment.Duration / 2.0 };
        }

        var start = segment.StartTime;
        var end = Math.Max(segment.StartTime, segment.EndTime - 0.001);
        var step = (end - start) / (count - 1);
        return Enumerable.Range(0, count)
            .Select(i => Math.Min(end, start + step * i))
            .DistinctBy(x => Math.Round(x, 3))
            .ToList();
    }

    private static bool WriteFrame(VideoCapture capture, double timeSeconds, string path, double? videoDuration, int imageLongEdge)
    {
        foreach (var candidateTime in BuildSeekCandidates(timeSeconds, videoDuration))
        {
            if (TryWriteFrame(capture, candidateTime, path, imageLongEdge))
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<double> BuildSeekCandidates(double timeSeconds, double? videoDuration)
    {
        var offsets = new[] { 0.0, -0.05, 0.05, -0.15, 0.15, -0.3, 0.3 };
        foreach (var offset in offsets)
        {
            var candidate = Math.Max(0, timeSeconds + offset);
            if (videoDuration is > 0)
            {
                candidate = Math.Min(Math.Max(0, videoDuration.Value - 0.001), candidate);
            }

            yield return candidate;
        }
    }

    private static bool TryWriteFrame(VideoCapture capture, double timeSeconds, string path, int imageLongEdge)
    {
        capture.Set(VideoCaptureProperties.PosMsec, Math.Max(0, timeSeconds) * 1000.0);
        using var frame = new Mat();
        if (!capture.Read(frame) || frame.Empty())
        {
            return false;
        }

        return WriteFrameMat(frame, path, imageLongEdge);
    }

    private static bool WriteFrameMat(Mat frame, string path, int imageLongEdge)
    {
        using var output = ResizeForAnalysis(frame, imageLongEdge);
        return Cv2.ImWrite(path, output, new ImageEncodingParam(ImwriteFlags.JpegQuality, 82));
    }

    private static int ToFrameIndex(double timeSeconds, double fps, double frameCount)
    {
        if (fps <= 0 || frameCount <= 0)
        {
            return 0;
        }

        return Math.Clamp((int)Math.Round(Math.Max(0, timeSeconds) * fps), 0, Math.Max(0, (int)frameCount - 1));
    }

    private static Mat ResizeForAnalysis(Mat frame, int imageLongEdge)
    {
        var longEdge = Math.Clamp(imageLongEdge, 160, 1280);
        var currentLongEdge = Math.Max(frame.Width, frame.Height);
        if (currentLongEdge <= longEdge)
        {
            return frame.Clone();
        }

        var scale = longEdge / (double)currentLongEdge;
        var width = Math.Max(1, (int)Math.Round(frame.Width * scale));
        var height = Math.Max(1, (int)Math.Round(frame.Height * scale));
        var resized = new Mat();
        Cv2.Resize(frame, resized, new Size(width, height), 0, 0, InterpolationFlags.Area);
        return resized;
    }

    private sealed record FrameRequest(
        ProcessSegment Segment,
        string Path,
        double TimeSeconds,
        int FrameIndex,
        int Order);
}
