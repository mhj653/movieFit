using System.Text.Json;
using ProcessVideoAnalyzer.Models;

namespace ProcessVideoAnalyzer.Services;

public sealed class AnalysisHistoryService
{
    private const int MaxHistoryCount = 100;

    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly string _root;
    private readonly string _indexPath;

    public AnalysisHistoryService()
    {
        _root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ProcessVideoAnalyzer",
            "History");
        _indexPath = Path.Combine(_root, "index.json");
        Directory.CreateDirectory(_root);
    }

    public IReadOnlyList<AnalysisHistoryEntry> List()
    {
        var entries = ReadIndex();
        foreach (var entry in entries)
        {
            entry.VideoExists = File.Exists(entry.VideoPath);
        }

        return entries
            .OrderByDescending(x => x.AnalyzedAt)
            .ToList();
    }

    public AnalysisHistoryEntry Save(VideoAnalysisResult result, string? existingId = null)
    {
        Directory.CreateDirectory(_root);
        var entries = ReadIndex();
        var id = string.IsNullOrWhiteSpace(existingId)
            ? CreateId(result.Metadata.FileName)
            : existingId;

        var entry = BuildEntry(id, result);
        var resultPath = GetResultPath(id);
        File.WriteAllText(resultPath, JsonSerializer.Serialize(result, _jsonOptions));

        entries.RemoveAll(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));
        entries.Insert(0, entry);
        WriteIndex(entries.Take(MaxHistoryCount).ToList());
        return entry;
    }

    public VideoAnalysisResult LoadResult(string id)
    {
        var path = GetResultPath(id);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Analysis history result file was not found.", path);
        }

        var result = JsonSerializer.Deserialize<VideoAnalysisResult>(File.ReadAllText(path), _jsonOptions);
        return result ?? throw new InvalidOperationException("Analysis history result file is empty or invalid.");
    }

    public void Delete(string id)
    {
        var path = GetResultPath(id);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        var entries = ReadIndex();
        entries.RemoveAll(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));
        WriteIndex(entries);
    }

    private AnalysisHistoryEntry BuildEntry(string id, VideoAnalysisResult result)
    {
        var segments = result.Segments;
        var analyzed = segments.Where(x => x.AiMatchScore > 0).ToList();
        var latestSegment = segments
            .Where(x => x.LastAnalysisAt.HasValue)
            .OrderByDescending(x => x.LastAnalysisAt)
            .FirstOrDefault();

        return new AnalysisHistoryEntry
        {
            Id = id,
            VideoPath = result.Metadata.VideoPath,
            VideoFileName = string.IsNullOrWhiteSpace(result.Metadata.FileName)
                ? Path.GetFileName(result.Metadata.VideoPath)
                : result.Metadata.FileName,
            DurationSeconds = result.Metadata.DurationSeconds,
            AnalyzedAt = DateTimeOffset.Now,
            Provider = latestSegment?.LastAnalysisProvider ?? result.AnalysisSettings.DefaultProvider,
            Model = latestSegment?.LastAnalysisModel ?? ResolveOpenCvModel(result),
            SegmentCount = segments.Count,
            AverageScore = analyzed.Count == 0 ? 0 : Math.Round(analyzed.Average(x => x.AiMatchScore), 1),
            VideoExists = File.Exists(result.Metadata.VideoPath)
        };
    }

    private List<AnalysisHistoryEntry> ReadIndex()
    {
        if (!File.Exists(_indexPath))
        {
            return new List<AnalysisHistoryEntry>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<AnalysisHistoryEntry>>(File.ReadAllText(_indexPath), _jsonOptions)
                ?? new List<AnalysisHistoryEntry>();
        }
        catch
        {
            return new List<AnalysisHistoryEntry>();
        }
    }

    private void WriteIndex(List<AnalysisHistoryEntry> entries)
    {
        File.WriteAllText(_indexPath, JsonSerializer.Serialize(entries, _jsonOptions));
    }

    private string GetResultPath(string id)
    {
        var safeId = Sanitize(id);
        return Path.Combine(_root, $"{safeId}.json");
    }

    private static string CreateId(string fileName)
    {
        var name = Sanitize(Path.GetFileNameWithoutExtension(fileName));
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var id = $"{DateTime.Now:yyyyMMdd_HHmmss}_{name}_{suffix}";
        return id[..Math.Min(96, id.Length)];
    }

    private static string Sanitize(string value)
    {
        var invalid = Path.GetInvalidFileNameChars().ToHashSet();
        var chars = value
            .Where(ch => !invalid.Contains(ch) && !char.IsControl(ch))
            .Select(ch => char.IsWhiteSpace(ch) ? '_' : ch)
            .ToArray();
        var sanitized = new string(chars);
        return string.IsNullOrWhiteSpace(sanitized) ? "analysis" : sanitized;
    }

    private static string ResolveOpenCvModel(VideoAnalysisResult result)
    {
        return string.Equals(result.AnalysisSettings.DefaultProvider, "opencv", StringComparison.OrdinalIgnoreCase)
            ? "OpenCV"
            : "";
    }
}
