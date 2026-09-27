using System.Text.Json;

namespace ProcessVideoAnalyzer.VlmContext;

public sealed class VlmContextBlockPresetStore
{
    private readonly string _storePath;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public VlmContextBlockPresetStore()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ProcessVideoAnalyzer");
        Directory.CreateDirectory(root);
        _storePath = Path.Combine(root, "advanced-block-presets.json");
    }

    public List<VlmContextBlockPreset> Load()
    {
        if (!File.Exists(_storePath))
        {
            return DefaultPresets();
        }

        try
        {
            var json = File.ReadAllText(_storePath);
            var items = JsonSerializer.Deserialize<List<VlmContextBlockPreset>>(json, _jsonOptions)
                        ?? new List<VlmContextBlockPreset>();
            return items.Count == 0 ? DefaultPresets() : items;
        }
        catch
        {
            return DefaultPresets();
        }
    }

    public void Save(IReadOnlyList<VlmContextBlockPreset> presets)
    {
        File.WriteAllText(_storePath, JsonSerializer.Serialize(presets, _jsonOptions));
    }

    public List<VlmContextBlockPreset> Upsert(VlmContextBlockPreset preset)
    {
        var presets = Load();
        if (string.IsNullOrWhiteSpace(preset.Id))
        {
            preset.Id = Guid.NewGuid().ToString("N");
        }

        var index = presets.FindIndex(x => x.Id.Equals(preset.Id, StringComparison.OrdinalIgnoreCase));
        preset.UpdatedAt = DateTimeOffset.Now;
        if (index >= 0)
        {
            preset.CreatedAt = presets[index].CreatedAt;
            presets[index] = preset;
        }
        else
        {
            preset.CreatedAt = DateTimeOffset.Now;
            presets.Insert(0, preset);
        }

        Save(presets);
        return presets;
    }

    public List<VlmContextBlockPreset> Delete(string id)
    {
        var presets = Load();
        presets.RemoveAll(x => x.Id.Equals(id, StringComparison.OrdinalIgnoreCase) && x.Id != "none");
        if (presets.Count == 0)
        {
            presets = DefaultPresets();
        }

        Save(presets);
        return presets;
    }

    private static List<VlmContextBlockPreset> DefaultPresets()
    {
        return new List<VlmContextBlockPreset>
        {
            new()
            {
                Id = "none",
                Name = "None",
                Settings = new VlmContextBlockSettings()
            },
            new()
            {
                Id = "worker-hand",
                Name = "Worker Hand Detection",
                Settings = new VlmContextBlockSettings
                {
                    YoloEnabled = true,
                    YoloLabels = "worker,hand,part,tray,fixture,button,tool",
                    YoloUseBoxesAsRoi = true,
                    RoiMode = "futureYoloRoi",
                    CropMode = "roiContext",
                    SamplingMode = "uniform",
                    SamplingFrameCount = 3
                }
            },
            new()
            {
                Id = "machine-state",
                Name = "Machine State",
                Settings = new VlmContextBlockSettings
                {
                    YoloEnabled = true,
                    YoloLabels = "machine,actuator,head,conveyor,part,fixture",
                    RoiMode = "fullFrame",
                    CropMode = "fullFrame",
                    SamplingMode = "startEnd",
                    SamplingFrameCount = 3
                }
            }
        };
    }
}
