using System.Text.Json;

namespace ProcessVideoAnalyzer.VlmContext;

public sealed class VlmContextExperimentStore
{
    private readonly string _storePath;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public VlmContextExperimentStore()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ProcessVideoAnalyzer");
        Directory.CreateDirectory(root);
        _storePath = Path.Combine(root, "context-experiments.json");
    }

    public List<VlmContextExperimentRun> Load()
    {
        if (!File.Exists(_storePath))
        {
            return new List<VlmContextExperimentRun>();
        }

        try
        {
            var json = File.ReadAllText(_storePath);
            return JsonSerializer.Deserialize<List<VlmContextExperimentRun>>(json, _jsonOptions)
                   ?? new List<VlmContextExperimentRun>();
        }
        catch
        {
            return new List<VlmContextExperimentRun>();
        }
    }

    public void Save(IReadOnlyList<VlmContextExperimentRun> runs)
    {
        var ordered = runs
            .OrderByDescending(x => x.CreatedAt)
            .Take(300)
            .ToList();
        File.WriteAllText(_storePath, JsonSerializer.Serialize(ordered, _jsonOptions));
    }
}
