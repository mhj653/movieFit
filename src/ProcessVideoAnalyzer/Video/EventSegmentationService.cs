using ProcessVideoAnalyzer.Models;

namespace ProcessVideoAnalyzer.Video;

public sealed class EventSegmentationService
{
    public List<ProcessSegment> BuildVlmTimingWindows(double videoDuration, AnalysisSettings settings)
    {
        if (videoDuration <= 0)
        {
            return new List<ProcessSegment>();
        }

        var windowSeconds = Math.Clamp(settings.MaxAiSegmentDuration, 3.0, 8.0);
        var segments = new List<ProcessSegment>();
        var sequence = 1;
        for (var start = 0.0; start < videoDuration; start += windowSeconds)
        {
            var end = Math.Min(videoDuration, start + windowSeconds);
            if (end <= start)
            {
                continue;
            }

            segments.Add(new ProcessSegment
            {
                Sequence = sequence++,
                StartTime = start,
                EndTime = end,
                Duration = end - start,
                MotionType = "Candidate",
                SegmentSource = "vlm_window",
                Reason = "Fast Local VLM timing window. OpenCV motion detection is bypassed for speed.",
                AverageMotionScore = 0
            });
        }

        return segments;
    }

    public List<ProcessSegment> BuildSegments(
        IReadOnlyList<MotionSample> samples,
        double videoDuration,
        AnalysisSettings settings)
    {
        if (samples.Count == 0)
        {
            return new List<ProcessSegment>();
        }

        var raw = BuildRawSegments(samples, videoDuration, settings);
        var merged = MergeShortSegments(raw, settings.MinimumSegmentDuration, videoDuration);
        var aiWindows = SplitForAiCoverage(merged, settings.MaxAiSegmentDuration, videoDuration);

        for (var i = 0; i < aiWindows.Count; i++)
        {
            var segment = aiWindows[i];
            segment.Sequence = i + 1;
            segment.Duration = Math.Max(0, segment.EndTime - segment.StartTime);
            segment.AverageMotionScore = CalculateAverage(samples, segment.StartTime, segment.EndTime);
            segment.MotionType = segment.AverageMotionScore >= settings.MotionStopThreshold ? "Motion" : "Idle";

            if (segment.MotionType == "Idle")
            {
                segment.ActionCode = "IDLE";
                segment.ActionName = "\uB300\uAE30";
                segment.Confidence = 1.0;
                segment.Description = "";
                segment.Reason = $"OpenCV motion score {segment.AverageMotionScore:0.00} is below stop threshold {settings.MotionStopThreshold:0.00}. Timing is only a candidate window.";
            }
        }

        return aiWindows;
    }

    private static List<ProcessSegment> BuildRawSegments(
        IReadOnlyList<MotionSample> samples,
        double videoDuration,
        AnalysisSettings settings)
    {
        var holdSeconds = settings.StateHoldMs / 1000.0;
        var segments = new List<ProcessSegment>();
        var isMotion = false;
        var segmentStart = 0.0;
        double? candidateStart = null;
        double? candidateStop = null;

        foreach (var sample in samples)
        {
            if (!isMotion)
            {
                if (sample.MotionScore >= settings.MotionStartThreshold)
                {
                    candidateStart ??= sample.TimeSeconds;
                    if (sample.TimeSeconds - candidateStart.Value >= holdSeconds)
                    {
                        AddSegment(segments, segmentStart, candidateStart.Value, "Idle");
                        segmentStart = candidateStart.Value;
                        isMotion = true;
                        candidateStart = null;
                    }
                }
                else
                {
                    candidateStart = null;
                }
            }
            else
            {
                if (sample.MotionScore <= settings.MotionStopThreshold)
                {
                    candidateStop ??= sample.TimeSeconds;
                    if (sample.TimeSeconds - candidateStop.Value >= holdSeconds)
                    {
                        AddSegment(segments, segmentStart, candidateStop.Value, "Motion");
                        segmentStart = candidateStop.Value;
                        isMotion = false;
                        candidateStop = null;
                    }
                }
                else
                {
                    candidateStop = null;
                }
            }
        }

        AddSegment(segments, segmentStart, videoDuration, isMotion ? "Motion" : "Idle");
        return segments.Where(x => x.EndTime > x.StartTime).ToList();
    }

    private static List<ProcessSegment> MergeShortSegments(
        List<ProcessSegment> segments,
        double minimumDuration,
        double videoDuration)
    {
        var result = new List<ProcessSegment>();

        foreach (var segment in segments)
        {
            segment.Duration = segment.EndTime - segment.StartTime;
            if (segment.Duration < minimumDuration && result.Count > 0)
            {
                result[^1].EndTime = segment.EndTime;
                result[^1].Duration = result[^1].EndTime - result[^1].StartTime;
                continue;
            }

            if (segment.Duration < minimumDuration && result.Count == 0 && segments.Count > 1)
            {
                segments[1].StartTime = segment.StartTime;
                continue;
            }

            result.Add(segment);
        }

        if (result.Count == 0 && videoDuration > 0)
        {
            result.Add(new ProcessSegment { StartTime = 0, EndTime = videoDuration, MotionType = "Idle" });
        }

        return result;
    }

    private static List<ProcessSegment> SplitForAiCoverage(
        IReadOnlyList<ProcessSegment> segments,
        double maxWindowSeconds,
        double videoDuration)
    {
        var maxWindow = Math.Clamp(maxWindowSeconds, 0.5, 10.0);
        var result = new List<ProcessSegment>();

        foreach (var segment in segments)
        {
            var duration = Math.Max(0, segment.EndTime - segment.StartTime);
            if (duration <= maxWindow)
            {
                result.Add(CloneSegment(segment, segment.StartTime, segment.EndTime));
                continue;
            }

            var windowCount = (int)Math.Ceiling(duration / maxWindow);
            var windowSize = duration / windowCount;
            for (var i = 0; i < windowCount; i++)
            {
                var start = segment.StartTime + i * windowSize;
                var end = i == windowCount - 1 ? segment.EndTime : segment.StartTime + (i + 1) * windowSize;
                result.Add(CloneSegment(segment, start, Math.Min(end, videoDuration)));
            }
        }

        return result.Where(x => x.EndTime > x.StartTime).ToList();
    }

    private static ProcessSegment CloneSegment(ProcessSegment source, double start, double end)
    {
        return new ProcessSegment
        {
            StartTime = start,
            EndTime = end,
            Duration = end - start,
            MotionType = source.MotionType,
            ActionCode = source.ActionCode,
            ActionName = source.ActionName,
            Confidence = source.Confidence,
            Description = source.Description,
            Reason = source.Reason,
            AverageMotionScore = source.AverageMotionScore
        };
    }

    private static void AddSegment(List<ProcessSegment> segments, double start, double end, string motionType)
    {
        if (end <= start)
        {
            return;
        }

        segments.Add(new ProcessSegment
        {
            StartTime = start,
            EndTime = end,
            Duration = end - start,
            MotionType = motionType
        });
    }

    private static double CalculateAverage(IReadOnlyList<MotionSample> samples, double start, double end)
    {
        var selected = samples.Where(x => x.TimeSeconds >= start && x.TimeSeconds <= end).ToList();
        return selected.Count == 0 ? 0 : selected.Average(x => x.MotionScore);
    }
}
