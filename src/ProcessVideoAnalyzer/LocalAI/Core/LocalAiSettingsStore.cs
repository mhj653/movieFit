using System.Text.Json;

namespace ProcessVideoAnalyzer.LocalAI.Core;

public sealed class LocalAiSettingsStore
{
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _root;
    private readonly string _settingsPath;
    private readonly string _profilesPath;

    public LocalAiSettingsStore()
    {
        _root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ProcessVideoAnalyzer",
            "LocalAI");
        _settingsPath = Path.Combine(_root, "local-ai-settings.json");
        _profilesPath = Path.Combine(_root, "vlm-profiles.json");
        Directory.CreateDirectory(_root);
        EnsureProfilesFile();
    }

    public LocalAiSettings LoadSettings()
    {
        if (!File.Exists(_settingsPath))
        {
            return new LocalAiSettings();
        }

        try
        {
            return JsonSerializer.Deserialize<LocalAiSettings>(File.ReadAllText(_settingsPath), _jsonOptions)
                   ?? new LocalAiSettings();
        }
        catch
        {
            return new LocalAiSettings();
        }
    }

    public void SaveSettings(LocalAiSettings settings)
    {
        Directory.CreateDirectory(_root);
        File.WriteAllText(_settingsPath, JsonSerializer.Serialize(settings, _jsonOptions));
    }

    public List<VlmModelProfile> LoadProfiles()
    {
        EnsureProfilesFile();
        try
        {
            var profiles = JsonSerializer.Deserialize<List<VlmModelProfile>>(File.ReadAllText(_profilesPath), _jsonOptions)
                           ?? DefaultProfiles();
            var normalized = NormalizeProfiles(profiles);
            SaveProfiles(normalized);
            return normalized;
        }
        catch
        {
            var defaults = DefaultProfiles();
            SaveProfiles(defaults);
            return defaults;
        }
    }

    public void SaveProfiles(IReadOnlyList<VlmModelProfile> profiles)
    {
        Directory.CreateDirectory(_root);
        File.WriteAllText(_profilesPath, JsonSerializer.Serialize(profiles, _jsonOptions));
    }

    private void EnsureProfilesFile()
    {
        if (!File.Exists(_profilesPath))
        {
            SaveProfiles(DefaultProfiles());
        }
    }

    private static List<VlmModelProfile> DefaultProfiles()
    {
        return new List<VlmModelProfile>
        {
            new()
            {
                Id = "qwen3-vl-4b-q4km",
                Name = "Qwen3-VL 4B Instruct",
                Provider = "llama.cpp",
                Adapter = "qwen3vl",
                ModelPath = "models/qwen3-vl-4b/model.gguf",
                MmprojPath = "models/qwen3-vl-4b/mmproj.gguf",
                RuntimePath = "runtimes/llama.cpp-cuda/llama-server.exe",
                Host = "127.0.0.1",
                Port = 18080,
                GpuLayers = 99,
                ContextSize = 4096,
                StartupTimeoutSeconds = 180,
                Quantization = "Q4_K_M"
            },
            new()
            {
                Id = "internvl3_5-4b-q4km",
                Name = "InternVL3.5 4B",
                Provider = "llama.cpp",
                Adapter = "internvl",
                ModelPath = "models/internvl3.5-4b/model.gguf",
                MmprojPath = "models/internvl3.5-4b/mmproj.gguf",
                RuntimePath = "runtimes/llama.cpp-cuda/llama-server.exe",
                Host = "127.0.0.1",
                Port = 18081,
                GpuLayers = 99,
                ContextSize = 4096,
                StartupTimeoutSeconds = 180,
                Quantization = "Q4_K_M"
            }
        };
    }

    private static List<VlmModelProfile> NormalizeProfiles(List<VlmModelProfile> profiles)
    {
        var defaults = DefaultProfiles();
        foreach (var profile in profiles)
        {
            var defaultProfile = defaults.FirstOrDefault(x =>
                string.Equals(x.Id, profile.Id, StringComparison.OrdinalIgnoreCase));
            if (defaultProfile is null)
            {
                continue;
            }

            profile.Name = string.IsNullOrWhiteSpace(profile.Name) ? defaultProfile.Name : profile.Name;
            profile.Provider = string.IsNullOrWhiteSpace(profile.Provider) ? defaultProfile.Provider : profile.Provider;
            profile.Adapter = string.IsNullOrWhiteSpace(profile.Adapter) ? defaultProfile.Adapter : profile.Adapter;
            profile.ModelPath = string.IsNullOrWhiteSpace(profile.ModelPath) ? defaultProfile.ModelPath : profile.ModelPath;
            profile.MmprojPath = string.IsNullOrWhiteSpace(profile.MmprojPath) ? defaultProfile.MmprojPath : profile.MmprojPath;
            profile.RuntimePath = string.IsNullOrWhiteSpace(profile.RuntimePath) ? defaultProfile.RuntimePath : profile.RuntimePath;
            profile.Host = string.IsNullOrWhiteSpace(profile.Host) ? defaultProfile.Host : profile.Host;
            profile.Port = profile.Port <= 0 ? defaultProfile.Port : profile.Port;
            profile.GpuLayers = profile.GpuLayers < 0 ? defaultProfile.GpuLayers : profile.GpuLayers;
            profile.ContextSize = profile.ContextSize <= 0 ? defaultProfile.ContextSize : profile.ContextSize;
            profile.StartupTimeoutSeconds = profile.StartupTimeoutSeconds <= 0 ? defaultProfile.StartupTimeoutSeconds : profile.StartupTimeoutSeconds;
            profile.Quantization = string.IsNullOrWhiteSpace(profile.Quantization) ? defaultProfile.Quantization : profile.Quantization;
            profile.Defaults ??= defaultProfile.Defaults;
            profile.Capabilities ??= defaultProfile.Capabilities;
        }

        foreach (var defaultProfile in defaults)
        {
            if (profiles.All(x => !string.Equals(x.Id, defaultProfile.Id, StringComparison.OrdinalIgnoreCase)))
            {
                profiles.Add(defaultProfile);
            }
        }

        return profiles;
    }
}
