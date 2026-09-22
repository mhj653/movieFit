using System.Text.Json;
using ProcessVideoAnalyzer.Models;

namespace ProcessVideoAnalyzer.Project;

public sealed class ProjectFileService
{
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public void Save(string path, VideoAnalysisResult result)
    {
        var document = new ProjectDocument
        {
            VideoPath = result.Metadata.VideoPath,
            VideoDuration = result.Metadata.DurationSeconds,
            AnalysisSettings = result.AnalysisSettings,
            Rois = result.Rois,
            MotionSamples = result.MotionSamples,
            Segments = result.Segments
        };

        File.WriteAllText(path, JsonSerializer.Serialize(document, _jsonOptions));
    }

    public ProjectDocument Load(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<ProjectDocument>(json, _jsonOptions)
            ?? throw new InvalidOperationException("Project file is invalid.");
    }
}
