using OpenCvSharp;

namespace ProcessVideoAnalyzer.Imaging;

public sealed class SingleImageFramePreparer
{
    private readonly string _frameRoot;

    public SingleImageFramePreparer()
    {
        _frameRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ProcessVideoAnalyzer",
            "Captures",
            "VlmFrames");
        Directory.CreateDirectory(_frameRoot);
    }

    public string Prepare(string sourcePath, int imageLongEdge)
    {
        using var source = Cv2.ImRead(sourcePath, ImreadModes.Color);
        if (source.Empty())
        {
            return sourcePath;
        }

        var longEdge = Math.Clamp(imageLongEdge, 160, 1280);
        var currentLongEdge = Math.Max(source.Width, source.Height);
        using var output = currentLongEdge <= longEdge
            ? source.Clone()
            : Resize(source, longEdge);

        var path = Path.Combine(_frameRoot, $"single_image_{DateTimeOffset.Now:yyyyMMdd_HHmmss_fff}_{longEdge}.jpg");
        Cv2.ImWrite(path, output, new ImageEncodingParam(ImwriteFlags.JpegQuality, 82));
        return path;
    }

    private static Mat Resize(Mat source, int longEdge)
    {
        var scale = longEdge / (double)Math.Max(source.Width, source.Height);
        var width = Math.Max(1, (int)Math.Round(source.Width * scale));
        var height = Math.Max(1, (int)Math.Round(source.Height * scale));
        var resized = new Mat();
        Cv2.Resize(source, resized, new Size(width, height), 0, 0, InterpolationFlags.Area);
        return resized;
    }
}
