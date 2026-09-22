using ProcessVideoAnalyzer.LocalAI.Adapters;
using ProcessVideoAnalyzer.LocalAI.Providers;
using System.Text.Json;

namespace ProcessVideoAnalyzer.LocalAI.Core;

public sealed class VlmManager
{
    private readonly LocalAiSettingsStore _store;
    private readonly List<ILocalVlmProvider> _providers;
    private readonly List<IVlmModelAdapter> _adapters;
    private LocalAiSettings _settings;
    private List<VlmModelProfile> _profiles;
    private ILocalVlmProvider? _provider;
    private IVlmModelAdapter? _adapter;
    private VlmModelProfile? _profile;

    public VlmManager(LocalAiSettingsStore store)
    {
        _store = store;
        _settings = _store.LoadSettings();
        _profiles = _store.LoadProfiles();
        _providers = new List<ILocalVlmProvider> { new LlamaCppProvider() };
        _adapters = new List<IVlmModelAdapter>
        {
            new Qwen3VlAdapter(),
            new InternVlAdapter(),
            new GenericLlamaCppVisionAdapter()
        };
    }

    public LocalAiSettings Settings => _settings;
    public IReadOnlyList<VlmModelProfile> Profiles => _profiles;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _profile = _profiles.FirstOrDefault(x => string.Equals(x.Id, _settings.ActiveModelId, StringComparison.OrdinalIgnoreCase))
                   ?? _profiles.FirstOrDefault();
        if (_profile is null)
        {
            return;
        }

        _settings.ActiveModelId = _profile.Id;
        _settings.ActiveModelName = _profile.Name;
        _profile = CreateEffectiveProfile(_profile, _settings);
        _provider = _providers.FirstOrDefault(x => string.Equals(x.ProviderId, _profile.Provider, StringComparison.OrdinalIgnoreCase));
        _adapter = _adapters.FirstOrDefault(x => x.CanHandle(_profile)) ?? _adapters.OfType<GenericLlamaCppVisionAdapter>().First();
        if (_provider is null)
        {
            return;
        }

        await _provider.InitializeAsync(_profile, cancellationToken);
        if (_settings.WarmupOnStart)
        {
            await _provider.WarmupAsync(cancellationToken);
        }
    }

    public async Task SaveSettingsAsync(LocalAiSettings settings, CancellationToken cancellationToken = default)
    {
        _settings = settings;
        _store.SaveSettings(_settings);
        await InitializeAsync(cancellationToken);
    }

    public async Task<VlmHealthStatus> GetStatusAsync()
    {
        if (_provider is null || _profile is null || _adapter is null)
        {
            return new VlmHealthStatus
            {
                State = "not_ready",
                Message = "No Local VLM profile/provider is selected.",
                ProviderId = _settings.RuntimeProvider,
                ModelId = _settings.ActiveModelId,
                ModelName = _settings.ActiveModelName
            };
        }

        var status = await _provider.GetStatusAsync();
        status.AdapterId = _adapter.AdapterId;
        return status;
    }

    public async Task<VlmResult> AnalyzeAsync(VlmRequest request, CancellationToken cancellationToken = default)
    {
        if (_provider is null || _adapter is null || _profile is null)
        {
            throw new InvalidOperationException("Local VLM is not initialized.");
        }

        var prepared = _adapter.Prepare(request, _profile);
        var raw = await _provider.AnalyzeAsync(prepared, cancellationToken);
        var result = _adapter.Parse(raw, _profile);
        result.OutputTokens = raw.OutputTokens;
        result.TracePrompt = prepared.Prompt;
        result.TraceFramePaths = prepared.ImagePaths.ToList();
        result.TraceRawResponse = raw.Text;
        result.TraceRequestJson = JsonSerializer.Serialize(new
        {
            provider = _profile.Provider,
            modelId = _profile.Id,
            modelName = _profile.Name,
            adapter = _adapter.AdapterId,
            outputLanguage = request.OutputLanguage,
            mode = request.Mode,
            promptMode = request.PromptMode,
            detectionFacts = request.DetectionFacts,
            inputImageLongEdge = request.InputImageLongEdge,
            maxOutputTokens = request.MaxOutputTokens,
            temperature = request.Temperature,
            segment = new
            {
                request.Segment.Id,
                request.Segment.Sequence,
                request.Segment.StartTime,
                request.Segment.EndTime,
                request.Segment.Duration,
                request.Segment.MotionType,
                request.Segment.AverageMotionScore
            },
            imagePaths = prepared.ImagePaths
        });
        return result;
    }

    public Task UnloadAsync()
    {
        return _provider?.UnloadAsync() ?? Task.CompletedTask;
    }

    private static VlmModelProfile CreateEffectiveProfile(VlmModelProfile source, LocalAiSettings settings)
    {
        return new VlmModelProfile
        {
            Id = source.Id,
            Name = source.Name,
            Provider = source.Provider,
            Adapter = source.Adapter,
            ModelPath = string.IsNullOrWhiteSpace(settings.ModelPath) ? source.ModelPath : settings.ModelPath,
            MmprojPath = string.IsNullOrWhiteSpace(settings.MmprojPath) ? source.MmprojPath : settings.MmprojPath,
            RuntimePath = string.IsNullOrWhiteSpace(settings.RuntimePath) ? source.RuntimePath : settings.RuntimePath,
            EndpointUrl = string.IsNullOrWhiteSpace(settings.EndpointUrl) ? source.EndpointUrl : settings.EndpointUrl,
            Host = string.IsNullOrWhiteSpace(settings.Host) ? source.Host : settings.Host,
            Port = settings.Port > 0 ? settings.Port : source.Port,
            GpuLayers = settings.GpuLayers >= 0 ? settings.GpuLayers : source.GpuLayers,
            ContextSize = settings.ContextSize > 0 ? settings.ContextSize : source.ContextSize,
            Threads = source.Threads,
            StartupTimeoutSeconds = settings.StartupTimeoutSeconds > 0 ? settings.StartupTimeoutSeconds : source.StartupTimeoutSeconds,
            Quantization = source.Quantization,
            ModelVersion = source.ModelVersion,
            Capabilities = source.Capabilities,
            Defaults = source.Defaults
        };
    }
}
