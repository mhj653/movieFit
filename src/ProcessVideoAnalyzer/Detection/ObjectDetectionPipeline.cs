using System.Globalization;

namespace ProcessVideoAnalyzer.Detection;

public sealed class ObjectDetectionPipeline
{
    private readonly IObjectDetector _detector;

    public ObjectDetectionPipeline(IObjectDetector? detector = null)
    {
        _detector = detector ?? new NoOpObjectDetector();
    }

    public async Task<SegmentDetectionContext> AnalyzeAsync(
        IReadOnlyList<string> framePaths,
        ObjectDetectionOptions options,
        CancellationToken cancellationToken = default)
    {
        var context = new SegmentDetectionContext
        {
            Provider = options.Enabled ? _detector.ProviderId : "none",
            ModelPath = options.ModelPath
        };

        if (!options.Enabled || framePaths.Count == 0)
        {
            return context;
        }

        foreach (var framePath in framePaths.Where(File.Exists))
        {
            context.Frames.Add(await _detector.DetectAsync(framePath, options, cancellationToken));
        }

        context.PromptFacts = BuildPromptFacts(context);
        return context;
    }

    private static string BuildPromptFacts(SegmentDetectionContext context)
    {
        var lines = new List<string>();
        for (var frameIndex = 0; frameIndex < context.Frames.Count; frameIndex++)
        {
            var frame = context.Frames[frameIndex];
            if (frame.Objects.Count == 0)
            {
                continue;
            }

            var facts = frame.Objects
                .OrderByDescending(x => x.Confidence)
                .Take(12)
                .Select(x =>
                    $"{x.Label} {Math.Round(x.Confidence * 100)}% bbox=({Fmt(x.X)},{Fmt(x.Y)},{Fmt(x.Width)},{Fmt(x.Height)})");
            lines.Add($"frame {frameIndex + 1}: {string.Join("; ", facts)}");
        }

        return lines.Count == 0
            ? ""
            : "Detected objects from local detector:\n" + string.Join("\n", lines);
    }

    private static string Fmt(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
