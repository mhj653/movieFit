using OpenCvSharp;
using ProcessVideoAnalyzer.Models;

namespace ProcessVideoAnalyzer.Video;

public sealed class MotionAnalysisService
{
    public Task<List<MotionSample>> AnalyzeAsync(
        string videoPath,
        AnalysisSettings settings,
        IReadOnlyList<AnalysisRoi> rois,
        IProgress<AnalysisProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            using var capture = new VideoCapture(videoPath);
            if (!capture.IsOpened())
            {
                throw new InvalidOperationException("Video file could not be opened for motion analysis.");
            }

            var fps = capture.Get(VideoCaptureProperties.Fps);
            var frameCount = capture.Get(VideoCaptureProperties.FrameCount);
            var duration = fps > 0 ? frameCount / fps : 0;
            if (duration <= 0)
            {
                throw new InvalidOperationException("Video duration could not be calculated.");
            }

            var samples = new List<MotionSample>();
            using var previous = new Mat();
            using var current = new Mat();
            using var diff = new Mat();
            using var thresholded = new Mat();
            using var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3));

            var intervalSeconds = Math.Max(settings.SampleIntervalMs, 20) / 1000.0;
            var sampleEveryFrames = Math.Max(1, (int)Math.Round(fps * intervalSeconds));
            var targetSize = BuildTargetSize(capture, Math.Min(settings.FrameResolution, 192));
            var enabledRois = rois.Where(x => x.Enabled && x.Width > 0 && x.Height > 0).ToList();
            var hasPrevious = false;
            var nextSampleFrame = 0;

            for (var frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (frameIndex < nextSampleFrame)
                {
                    if (!capture.Grab())
                    {
                        break;
                    }

                    continue;
                }

                using var frame = new Mat();
                if (!capture.Read(frame) || frame.Empty())
                {
                    break;
                }

                var time = frameIndex / fps;
                nextSampleFrame = frameIndex + sampleEveryFrames;
                using var resized = new Mat();
                using var gray = new Mat();
                using var blurred = new Mat();

                Cv2.Resize(frame, resized, targetSize);
                Cv2.CvtColor(resized, gray, ColorConversionCodes.BGR2GRAY);
                Cv2.GaussianBlur(gray, blurred, new Size(5, 5), 0);

                if (!hasPrevious)
                {
                    blurred.CopyTo(previous);
                    hasPrevious = true;
                    samples.Add(new MotionSample { TimeSeconds = time, MotionScore = 0, IsMotion = false });
                    continue;
                }

                Cv2.Absdiff(previous, blurred, diff);
                Cv2.Threshold(diff, thresholded, 25, 255, ThresholdTypes.Binary);
                Cv2.MorphologyEx(thresholded, thresholded, MorphTypes.Open, kernel);
                Cv2.MorphologyEx(thresholded, thresholded, MorphTypes.Close, kernel);

                var score = enabledRois.Count > 0
                    ? CalculateRoiScore(thresholded, enabledRois)
                    : CalculateScore(thresholded);

                samples.Add(new MotionSample
                {
                    TimeSeconds = time,
                    MotionScore = score,
                    IsMotion = score >= settings.MotionStartThreshold
                });

                blurred.CopyTo(previous);
                progress?.Report(new AnalysisProgress
                {
                    Stage = "Analyzing Motion",
                    Percent = Math.Min(65, 20 + (time / duration * 45)),
                    Message = $"{time:0.0}s / {duration:0.0}s"
                });
            }

            return samples;
        }, cancellationToken);
    }

    private static Size BuildTargetSize(VideoCapture capture, int frameResolution)
    {
        var width = capture.Get(VideoCaptureProperties.FrameWidth);
        var height = capture.Get(VideoCaptureProperties.FrameHeight);
        if (width <= 0 || height <= 0)
        {
            return new Size(frameResolution, frameResolution);
        }

        var scale = frameResolution / Math.Max(width, height);
        return new Size(Math.Max(1, (int)(width * scale)), Math.Max(1, (int)(height * scale)));
    }

    private static double CalculateScore(Mat thresholded)
    {
        var changedPixels = Cv2.CountNonZero(thresholded);
        return changedPixels / (double)(thresholded.Width * thresholded.Height) * 100.0;
    }

    private static double CalculateRoiScore(Mat thresholded, IReadOnlyList<AnalysisRoi> rois)
    {
        var changed = 0;
        var total = 0;

        foreach (var roi in rois)
        {
            var rect = ToRect(roi, thresholded.Width, thresholded.Height);
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                continue;
            }

            using var roiMat = new Mat(thresholded, rect);
            changed += Cv2.CountNonZero(roiMat);
            total += rect.Width * rect.Height;
        }

        return total == 0 ? CalculateScore(thresholded) : changed / (double)total * 100.0;
    }

    private static Rect ToRect(AnalysisRoi roi, int width, int height)
    {
        var x = Clamp((int)Math.Round(roi.X * width), 0, width - 1);
        var y = Clamp((int)Math.Round(roi.Y * height), 0, height - 1);
        var w = Clamp((int)Math.Round(roi.Width * width), 1, width - x);
        var h = Clamp((int)Math.Round(roi.Height * height), 1, height - y);
        return new Rect(x, y, w, h);
    }

    private static int Clamp(int value, int min, int max)
    {
        return Math.Min(Math.Max(value, min), max);
    }
}
