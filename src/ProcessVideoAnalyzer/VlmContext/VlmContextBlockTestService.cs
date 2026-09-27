using System.Diagnostics;
using OpenCvSharp;

namespace ProcessVideoAnalyzer.VlmContext;

public sealed class VlmContextBlockTestService
{
    private readonly string _outputRoot;

    public VlmContextBlockTestService()
    {
        _outputRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ProcessVideoAnalyzer",
            "ContextBlockTests");
        Directory.CreateDirectory(_outputRoot);
    }

    public VlmContextBlockTestResult Run(string sourcePath, VlmContextBlockSettings settings)
    {
        var watch = Stopwatch.StartNew();
        using var source = Cv2.ImRead(sourcePath, ImreadModes.Color);
        if (source.Empty())
        {
            throw new InvalidOperationException("The selected test image could not be read.");
        }

        var rect = ResolveRoi(source, settings);
        using var cropped = ShouldCrop(settings)
            ? new Mat(source, rect).Clone()
            : source.Clone();
        using var output = ResizeIfNeeded(cropped, settings.CropOutputLongEdge);
        var outputPath = Path.Combine(_outputRoot, $"block_test_{DateTimeOffset.Now:yyyyMMdd_HHmmss_fff}.jpg");
        Cv2.ImWrite(outputPath, output, new ImageEncodingParam(ImwriteFlags.JpegQuality, 86));
        watch.Stop();

        return new VlmContextBlockTestResult
        {
            SourcePath = sourcePath,
            ProcessedPath = outputPath,
            SourceWidth = source.Width,
            SourceHeight = source.Height,
            OutputWidth = output.Width,
            OutputHeight = output.Height,
            LatencyMs = watch.Elapsed.TotalMilliseconds,
            Settings = settings,
            Hints = BuildHints(settings, rect, output)
        };
    }

    private static bool ShouldCrop(VlmContextBlockSettings settings)
    {
        return !settings.CropMode.Equals("none", StringComparison.OrdinalIgnoreCase) &&
               !settings.CropMode.Equals("fullFrame", StringComparison.OrdinalIgnoreCase) &&
               settings.RoiWidth > 0 &&
               settings.RoiHeight > 0;
    }

    private static Rect ResolveRoi(Mat source, VlmContextBlockSettings settings)
    {
        if (!settings.RoiMode.Equals("manualRoi", StringComparison.OrdinalIgnoreCase) ||
            settings.RoiWidth <= 0 ||
            settings.RoiHeight <= 0)
        {
            return new Rect(0, 0, source.Width, source.Height);
        }

        var paddingX = (int)Math.Round(settings.RoiWidth * Math.Clamp(settings.RoiPadding, 0, 1));
        var paddingY = (int)Math.Round(settings.RoiHeight * Math.Clamp(settings.RoiPadding, 0, 1));
        var x = Math.Clamp(settings.RoiX - paddingX, 0, source.Width - 1);
        var y = Math.Clamp(settings.RoiY - paddingY, 0, source.Height - 1);
        var right = Math.Clamp(settings.RoiX + settings.RoiWidth + paddingX, x + 1, source.Width);
        var bottom = Math.Clamp(settings.RoiY + settings.RoiHeight + paddingY, y + 1, source.Height);
        return new Rect(x, y, right - x, bottom - y);
    }

    private static Mat ResizeIfNeeded(Mat source, int longEdge)
    {
        var target = Math.Clamp(longEdge <= 0 ? 768 : longEdge, 160, 1280);
        var current = Math.Max(source.Width, source.Height);
        if (current <= target)
        {
            return source.Clone();
        }

        var scale = target / (double)current;
        var size = new Size(
            Math.Max(1, (int)Math.Round(source.Width * scale)),
            Math.Max(1, (int)Math.Round(source.Height * scale)));
        var resized = new Mat();
        Cv2.Resize(source, resized, size, 0, 0, InterpolationFlags.Area);
        return resized;
    }

    private static string BuildHints(VlmContextBlockSettings settings, Rect roi, Mat output)
    {
        var lines = new List<string>
        {
            $"ROI: mode={settings.RoiMode}, x={roi.X}, y={roi.Y}, width={roi.Width}, height={roi.Height}",
            $"Crop: mode={settings.CropMode}, output={output.Width}x{output.Height}, keepAspect={settings.CropKeepAspect}",
            $"Sampling: mode={settings.SamplingMode}, frames={settings.SamplingFrameCount}",
            $"YOLO: enabled={settings.YoloEnabled}, labels={settings.YoloLabels}, confidence={settings.YoloConfidence:0.00}",
            $"OCR: enabled={settings.OcrEnabled}, engine={settings.OcrEngine}, language={settings.OcrLanguage}"
        };
        return string.Join(Environment.NewLine, lines);
    }
}
