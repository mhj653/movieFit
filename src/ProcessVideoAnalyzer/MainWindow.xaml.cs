using System.Text.Json;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using ProcessVideoAnalyzer.AI.Prompt;
using ProcessVideoAnalyzer.Bridge;
using ProcessVideoAnalyzer.Capture;
using ProcessVideoAnalyzer.Detection;
using ProcessVideoAnalyzer.Imaging;
using ProcessVideoAnalyzer.LocalAI.Core;
using ProcessVideoAnalyzer.Models;
using ProcessVideoAnalyzer.Services;
using ProcessVideoAnalyzer.Video;
using ProcessVideoAnalyzer.VlmContext;
using System.Diagnostics;

namespace ProcessVideoAnalyzer;

public partial class MainWindow : Window
{
    private const string AppHost = "appassets.processvideoanalyzer";
    private const string LocalDataHost = "localdata.processvideoanalyzer";

    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    private readonly AppLogger _logger = new();
    private readonly AnalysisHistoryService _historyService = new();
    private readonly LocalAiSettingsStore _localAiStore = new();
    private readonly VlmManager _vlmManager;
    private readonly LocalVideoServer _videoServer;
    private readonly VideoAnalysisPipeline _pipeline;
    private readonly CameraCaptureService _captureService;
    private readonly SingleImageFramePreparer _singleImageFramePreparer;
    private readonly VlmContextExperimentService _contextExperimentService;
    private readonly VlmContextExperimentStore _contextExperimentStore = new();
    private readonly VlmContextRecommendationService _contextRecommendationService = new();
    private readonly VlmContextBlockPresetStore _contextBlockPresetStore = new();
    private readonly VlmContextBlockTestService _contextBlockTestService = new();

    private AnalysisSettings _settings = new();
    private LocalAiSettings _localAiSettings = new();
    private List<AnalysisRoi> _rois = new();
    private VideoAnalysisResult? _currentResult;
    private List<ProcessSegment> _baseSegments = new();
    private readonly Stack<List<ProcessSegment>> _undoStack = new();
    private readonly Stack<List<ProcessSegment>> _redoStack = new();
    private string? _selectedVideoPath;
    private string? _selectedContextImagePath;
    private string? _advancedBlockTestImagePath;
    private string? _currentHistoryId;
    private CancellationTokenSource? _analysisCancellation;

    public MainWindow()
    {
        InitializeComponent();

        _videoServer = new LocalVideoServer(_logger);
        _captureService = new CameraCaptureService(_logger);
        _singleImageFramePreparer = new SingleImageFramePreparer();
        _contextExperimentService = new VlmContextExperimentService(_singleImageFramePreparer);
        _vlmManager = new VlmManager(_localAiStore);
        _localAiSettings = _vlmManager.Settings;
        _pipeline = new VideoAnalysisPipeline(
            new VideoMetadataService(),
            new MotionAnalysisService(),
            new EventSegmentationService(),
            new RepresentativeFrameService(),
            new ManufacturingPromptBuilder(),
            new ObjectDetectionPipeline(),
            _logger);

        Loaded += async (_, _) => await InitializeWebViewAsync();
        Closing += (_, _) =>
        {
            _analysisCancellation?.Cancel();
            _captureService.Dispose();
            _videoServer.Dispose();
            _ = _vlmManager.UnloadAsync();
        };
    }

    private async Task InitializeWebViewAsync()
    {
        try
        {
            _videoServer.Start();
            await _vlmManager.InitializeAsync();
            await Web.EnsureCoreWebView2Async();
            Web.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
            Web.CoreWebView2.NavigationCompleted += async (_, _) =>
            {
                await SendLocalAiSettingsAsync();
                await SendVlmStatusAsync();
                await SendAnalysisHistoryAsync();
                await SendAdvancedBlockPresetsAsync();
            };

            var webRoot = Path.Combine(AppContext.BaseDirectory, "Web");
            Web.CoreWebView2.SetVirtualHostNameToFolderMapping(
                AppHost,
                webRoot,
                CoreWebView2HostResourceAccessKind.Allow);

            var localData = GetLocalDataRoot();
            Directory.CreateDirectory(localData);
            Web.CoreWebView2.SetVirtualHostNameToFolderMapping(
                LocalDataHost,
                localData,
                CoreWebView2HostResourceAccessKind.Allow);

            Web.Source = new Uri($"https://{AppHost}/index.html");
        }
        catch (Exception ex)
        {
            _logger.Error("WebView2 initialization failed", ex);
            MessageBox.Show(
                "WebView2 initialization failed. Install Microsoft Edge WebView2 Runtime and try again.",
                "Process Video Analyzer",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            var message = JsonSerializer.Deserialize<WebMessage>(e.WebMessageAsJson, _jsonOptions);
            if (message is null)
            {
                return;
            }

            switch (message.Type)
            {
                case WebMessageTypes.OpenVideo:
                    await OpenVideoAsync();
                    break;
                case WebMessageTypes.AnalyzeVideo:
                    await AnalyzeVideoAsync(message.Data);
                    break;
                case WebMessageTypes.CancelAnalysis:
                    _analysisCancellation?.Cancel();
                    break;
                case WebMessageTypes.UpdateSegment:
                    UpdateSegment(message.Data);
                    break;
                case WebMessageTypes.MergeSegments:
                    await MergeSegmentsAsync(message.Data);
                    break;
                case WebMessageTypes.SplitSegment:
                    await SplitSegmentAsync(message.Data);
                    break;
                case WebMessageTypes.RestoreOriginalSegment:
                    await RestoreOriginalSegmentAsync(message.Data);
                    break;
                case WebMessageTypes.UndoSegmentEdit:
                    await UndoSegmentEditAsync();
                    break;
                case WebMessageTypes.RedoSegmentEdit:
                    await RedoSegmentEditAsync();
                    break;
                case WebMessageTypes.ReAnalyzeSegment:
                    await ReAnalyzeSegmentAsync(message.Data);
                    break;
                case WebMessageTypes.GetLocalAiSettings:
                    await SendLocalAiSettingsAsync();
                    break;
                case WebMessageTypes.SaveLocalAiSettings:
                    await SaveLocalAiSettingsAsync(message.Data);
                    break;
                case WebMessageTypes.GetVlmModels:
                    await SendVlmModelsAsync();
                    break;
                case WebMessageTypes.GetVlmStatus:
                    await SendVlmStatusAsync();
                    break;
                case WebMessageTypes.TestVlmModel:
                    await TestVlmModelAsync();
                    break;
                case WebMessageTypes.GetApiSettingsStatus:
                    await SendLocalAiSettingsAsync();
                    break;
                case WebMessageTypes.SaveApiSettings:
                    await SaveLocalAiSettingsAsync(message.Data);
                    break;
                case WebMessageTypes.TestApiKey:
                    await TestVlmModelAsync();
                    break;
                case WebMessageTypes.ExportResult:
                    ExportResult();
                    break;
                case WebMessageTypes.GetAnalysisHistory:
                    await SendAnalysisHistoryAsync();
                    break;
                case WebMessageTypes.LoadAnalysisHistory:
                    await LoadAnalysisHistoryAsync(message.Data);
                    break;
                case WebMessageTypes.DeleteAnalysisHistory:
                    DeleteAnalysisHistory(message.Data);
                    await SendAnalysisHistoryAsync();
                    break;
                case WebMessageTypes.StartCapturePreview:
                    await StartCapturePreviewAsync(message.Data);
                    break;
                case WebMessageTypes.StopCapturePreview:
                    await StopCapturePreviewAsync();
                    break;
                case WebMessageTypes.StartCaptureRecording:
                    await StartCaptureRecordingAsync(message.Data);
                    break;
                case WebMessageTypes.StopCaptureRecording:
                    await StopCaptureRecordingAndAnalyzeAsync(message.Data);
                    break;
                case WebMessageTypes.CaptureSnapshot:
                    await CaptureSnapshotAndAnalyzeAsync(message.Data);
                    break;
                case WebMessageTypes.LoadImageForAnalysis:
                    await LoadImageForAnalysisAsync(message.Data);
                    break;
                case WebMessageTypes.GetCaptureStatus:
                    await SendCaptureStatusAsync();
                    break;
                case WebMessageTypes.LoadVlmContextImage:
                    await LoadVlmContextImageAsync();
                    break;
                case WebMessageTypes.RunVlmContextCompare:
                    await RunVlmContextCompareAsync(message.Data);
                    break;
                case WebMessageTypes.MarkVlmContextBest:
                    await MarkVlmContextBestAsync(message.Data);
                    break;
                case WebMessageTypes.ApplyVlmContextBestToDefaults:
                    await ApplyVlmContextBestToDefaultsAsync(message.Data);
                    break;
                case WebMessageTypes.GetVlmContextExperiments:
                    await SendVlmContextExperimentsAsync();
                    break;
                case WebMessageTypes.GetAdvancedBlockPresets:
                    await SendAdvancedBlockPresetsAsync();
                    break;
                case WebMessageTypes.SaveAdvancedBlockPreset:
                    await SaveAdvancedBlockPresetAsync(message.Data);
                    break;
                case WebMessageTypes.DeleteAdvancedBlockPreset:
                    await DeleteAdvancedBlockPresetAsync(message.Data);
                    break;
                case WebMessageTypes.LoadAdvancedBlockTestImage:
                    await LoadAdvancedBlockTestImageAsync();
                    break;
                case WebMessageTypes.RunAdvancedBlockTest:
                    await RunAdvancedBlockTestAsync(message.Data);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.Error("Web message handling failed", ex);
            await SendErrorAsync(ex.Message);
        }
    }

    private async Task OpenVideoAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Video Files|*.mp4;*.avi;*.mov;*.mkv;*.webm|All Files|*.*",
            Title = "Open Process Video"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        _selectedVideoPath = dialog.FileName;
        var metadata = new VideoMetadataService().Read(dialog.FileName);
        var videoUrl = _videoServer.SetVideo(dialog.FileName);
        _currentResult = null;
        _baseSegments.Clear();
        _undoStack.Clear();
        _redoStack.Clear();
        _currentHistoryId = null;

        await SendAsync(WebMessageTypes.VideoLoaded, new
        {
            metadata,
            videoUrl
        });
    }

    private async Task AnalyzeVideoAsync(JsonElement data)
    {
        if (string.IsNullOrWhiteSpace(_selectedVideoPath) || !File.Exists(_selectedVideoPath))
        {
            await SendErrorAsync("Open a video first.");
            return;
        }

        ApplyAnalysisOptions(data);
        _analysisCancellation?.Cancel();
        _analysisCancellation = new CancellationTokenSource();
        _currentHistoryId = null;

        var progress = new Progress<AnalysisProgress>(p => _ = SendAsync(WebMessageTypes.AnalysisProgress, p));
        var analysisWatch = Stopwatch.StartNew();
        await SendAsync(WebMessageTypes.AnalysisStarted, new { });

        try
        {
            var openCvWatch = Stopwatch.StartNew();
            var result = await _pipeline.AnalyzeOpenCvAsync(
                _selectedVideoPath,
                _settings,
                _rois,
                progress,
                _analysisCancellation.Token);
            openCvWatch.Stop();
            EnsurePerformanceMetrics(result).OpenCvSegmentationMs = openCvWatch.Elapsed.TotalMilliseconds;

            _currentResult = result;
            _baseSegments = DeepCloneSegments(result.BaseSegments.Count > 0 ? result.BaseSegments : result.Segments);
            _undoStack.Clear();
            _redoStack.Clear();
            await SendAsync(WebMessageTypes.MotionData, new { samples = result.MotionSamples });
            await SendSegmentsAsync();

            if (!IsOpenCvOnly(_localAiSettings.DefaultProvider))
            {
                await ApplyLocalVlmAsync(result, progress, _analysisCancellation.Token);
            }

            await SaveCurrentHistoryAsync();
            analysisWatch.Stop();
            EnsurePerformanceMetrics(result).TotalMs = analysisWatch.Elapsed.TotalMilliseconds;
            await SendAsync(WebMessageTypes.AnalysisComplete, BuildSummary(result));
        }
        catch (OperationCanceledException)
        {
            await SendAsync(WebMessageTypes.AnalysisProgress, new AnalysisProgress
            {
                Stage = "Canceled",
                Percent = 0,
                Message = "Analysis canceled"
            });
        }
    }

    private async Task StartCapturePreviewAsync(JsonElement data)
    {
        var cameraIndex = ReadInt(data, "cameraIndex") ?? 0;
        await _captureService.StartPreviewAsync(cameraIndex);
        await SendCaptureStatusAsync();
    }

    private async Task StopCapturePreviewAsync()
    {
        await _captureService.StopPreviewAsync();
        await SendCaptureStatusAsync();
    }

    private async Task StartCaptureRecordingAsync(JsonElement data)
    {
        ApplyAnalysisOptions(data);
        if (!_captureService.State.PreviewRunning)
        {
            await _captureService.StartPreviewAsync(ReadInt(data, "cameraIndex") ?? 0);
        }

        await _captureService.StartRecordingAsync();
        await SendCaptureStatusAsync();
    }

    private async Task StopCaptureRecordingAndAnalyzeAsync(JsonElement data)
    {
        ApplyAnalysisOptions(data);
        var path = await _captureService.StopRecordingAsync();
        await SendCaptureStatusAsync();
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            await SendErrorAsync("Recorded video file was not created.");
            return;
        }

        _selectedVideoPath = path;
        var metadata = new VideoMetadataService().Read(path);
        var videoUrl = _videoServer.SetVideo(path);
        _currentResult = null;
        _baseSegments.Clear();
        _undoStack.Clear();
        _redoStack.Clear();
        _currentHistoryId = null;

        await SendAsync(WebMessageTypes.VideoLoaded, new
        {
            metadata,
            videoUrl
        });

        await AnalyzeVideoAsync(data);
    }

    private async Task CaptureSnapshotAndAnalyzeAsync(JsonElement data)
    {
        ApplyAnalysisOptions(data);
        if (!_captureService.State.PreviewRunning)
        {
            await _captureService.StartPreviewAsync(ReadInt(data, "cameraIndex") ?? 0);
        }

        var snapshotPath = await _captureService.CaptureSnapshotAsync();
        await SendCaptureStatusAsync();
        await AnalyzeSingleImageAsync(
            snapshotPath,
            "snapshot",
            "Snapshot",
            "Single camera snapshot analyzed by Local VLM.",
            "Analyzing Snapshot");
    }

    private async Task LoadImageForAnalysisAsync(JsonElement data)
    {
        ApplyAnalysisOptions(data);
        var dialog = new OpenFileDialog
        {
            Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp|All Files|*.*",
            Title = "Open Image For Local VLM Analysis"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        var imagePath = _captureService.ImportImageForAnalysis(dialog.FileName);
        await SendCaptureStatusAsync();
        await AnalyzeSingleImageAsync(
            imagePath,
            "loaded_image",
            "Loaded Image",
            "Loaded still image analyzed by Local VLM.",
            "Analyzing Loaded Image");
    }

    private async Task LoadVlmContextImageAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp|All Files|*.*",
            Title = "Open Image For VLM Context Builder"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        _selectedContextImagePath = _captureService.ImportImageForAnalysis(dialog.FileName);
        await SendAsync(WebMessageTypes.VlmContextImageLoaded, new
        {
            fileName = Path.GetFileName(_selectedContextImagePath),
            imageUrl = ToAssetUri(_selectedContextImagePath)
        });
    }

    private async Task RunVlmContextCompareAsync(JsonElement data)
    {
        ApplyAnalysisOptions(data);
        if (IsOpenCvOnly(_localAiSettings.DefaultProvider))
        {
            await SendErrorAsync("VLM Context Builder는 Local VLM 모델이 필요합니다. Local AI Settings에서 Local VLM을 선택하세요.");
            return;
        }

        _analysisCancellation?.Cancel();
        _analysisCancellation = new CancellationTokenSource();

        await SendAsync(WebMessageTypes.VlmContextCompareStarted, new { });
        var progress = new Progress<string>(message => _ = SendAsync(WebMessageTypes.VlmContextCompareProgress, new { message }));

        try
        {
            var source = ReadString(data, "source") ?? "image";
            var candidates = ReadVlmContextCandidates(data);
            var blockSettings = ReadVlmContextBlockSettings(data);
            List<VlmContextExperimentRun> runs;
            if (source.Equals("selectedSegment", StringComparison.OrdinalIgnoreCase))
            {
                if (_currentResult is null || !TryReadGuid(data, "segmentId", out var segmentId))
                {
                    await SendErrorAsync("Analysis에서 Segment를 먼저 선택하세요.");
                    return;
                }

                var segment = _currentResult.Segments.FirstOrDefault(x => x.Id == segmentId);
                if (segment is null)
                {
                    await SendErrorAsync("선택한 Segment를 찾을 수 없습니다.");
                    return;
                }

                runs = await _contextExperimentService.RunSegmentCompareAsync(
                    segment,
                    candidates,
                    blockSettings,
                    _settings,
                    _vlmManager,
                    _localAiSettings.ActiveModelName,
                    progress,
                    _analysisCancellation.Token);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(_selectedContextImagePath) || !File.Exists(_selectedContextImagePath))
                {
                    await SendErrorAsync("VLM Context Builder에서 먼저 이미지를 Load 하세요.");
                    return;
                }

                runs = await _contextExperimentService.RunDefaultCompareAsync(
                    _selectedContextImagePath,
                    candidates,
                    blockSettings,
                    _settings,
                    _vlmManager,
                    _localAiSettings.ActiveModelName,
                    progress,
                    _analysisCancellation.Token);
            }

            var storedRuns = _contextExperimentStore.Load();
            storedRuns.InsertRange(0, runs);
            _contextExperimentStore.Save(storedRuns);

            await SendAsync(WebMessageTypes.VlmContextCompareComplete, new
            {
                runs = runs.Select(ToVlmContextRunDto).ToList(),
                recommendation = _contextRecommendationService.Build(storedRuns)
            });
            await SendVlmContextExperimentsAsync();
        }
        catch (OperationCanceledException)
        {
            await SendAsync(WebMessageTypes.VlmContextCompareProgress, new { message = "Context compare canceled." });
        }
    }

    private async Task MarkVlmContextBestAsync(JsonElement data)
    {
        if (!TryReadGuid(data, "runId", out var runId))
        {
            return;
        }

        var runs = _contextExperimentStore.Load();
        var selected = runs.FirstOrDefault(x => x.Id == runId);
        if (selected is null)
        {
            return;
        }

        foreach (var run in runs.Where(x => x.ExperimentId == selected.ExperimentId))
        {
            run.IsBest = run.Id == runId;
        }

        _contextExperimentStore.Save(runs);
        await SendVlmContextExperimentsAsync();
    }

    private async Task ApplyVlmContextBestToDefaultsAsync(JsonElement data)
    {
        if (!TryReadGuid(data, "runId", out var runId))
        {
            return;
        }

        var runs = _contextExperimentStore.Load();
        var selected = runs.FirstOrDefault(x => x.Id == runId);
        if (selected is null)
        {
            return;
        }

        _localAiSettings.SingleImageLongEdge = selected.Candidate.ImageLongEdge;
        _localAiSettings.SingleImageMaxOutputTokens = selected.Candidate.MaxOutputTokens;
        _localAiSettings.SingleImagePromptMode = selected.Candidate.PromptMode.Equals("fast", StringComparison.OrdinalIgnoreCase)
            ? "fast"
            : "balanced";
        _localAiSettings.ImageLongEdge = selected.Candidate.ImageLongEdge;
        _localAiSettings.MaxOutputTokens = selected.Candidate.MaxOutputTokens;
        _localAiSettings.MaxFrames = Math.Clamp(selected.Candidate.FrameCount, 1, 8);
        ApplyLocalSettingsToAnalysisSettings();
        await _vlmManager.SaveSettingsAsync(_localAiSettings);
        await SendLocalAiSettingsAsync();
        await SendAsync(WebMessageTypes.VlmContextCompareProgress, new
        {
            message = $"Applied {selected.Candidate.Name} to Analysis defaults."
        });
    }

    private Task SendAdvancedBlockPresetsAsync()
    {
        return SendAsync(WebMessageTypes.AdvancedBlockPresetsUpdated, new
        {
            presets = _contextBlockPresetStore.Load()
        });
    }

    private async Task SaveAdvancedBlockPresetAsync(JsonElement data)
    {
        if (data.ValueKind != JsonValueKind.Object ||
            !data.TryGetProperty("preset", out var json) ||
            json.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        try
        {
            var preset = JsonSerializer.Deserialize<VlmContextBlockPreset>(json.GetRawText(), _jsonOptions);
            if (preset is null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(preset.Name))
            {
                preset.Name = "New Preset";
            }

            _contextBlockPresetStore.Upsert(preset);
            await SendAdvancedBlockPresetsAsync();
        }
        catch (JsonException ex)
        {
            _logger.Info($"Advanced block preset could not be parsed: {ex.Message}");
            await SendErrorAsync("Advanced Block Preset 저장값을 읽을 수 없습니다.");
        }
    }

    private async Task DeleteAdvancedBlockPresetAsync(JsonElement data)
    {
        var id = ReadString(data, "id");
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        _contextBlockPresetStore.Delete(id);
        await SendAdvancedBlockPresetsAsync();
    }

    private async Task LoadAdvancedBlockTestImageAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp|All Files|*.*",
            Title = "Open Image For Advanced Block Test"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        _advancedBlockTestImagePath = _captureService.ImportImageForAnalysis(dialog.FileName);
        await SendAsync(WebMessageTypes.AdvancedBlockTestImageLoaded, new
        {
            fileName = Path.GetFileName(_advancedBlockTestImagePath),
            imageUrl = ToAssetUri(_advancedBlockTestImagePath),
            source = "advancedImage"
        });
    }

    private async Task RunAdvancedBlockTestAsync(JsonElement data)
    {
        var source = ReadString(data, "source") ?? "advancedImage";
        var blockSettings = ReadVlmContextBlockSettings(data);
        var imagePath = ResolveAdvancedBlockTestSource(source, data);
        if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
        {
            await SendErrorAsync("Advanced Block Test에 사용할 이미지가 없습니다. 이미지를 Load 하거나 Segment를 선택하세요.");
            return;
        }

        var result = _contextBlockTestService.Run(imagePath, blockSettings);
        await SendAsync(WebMessageTypes.AdvancedBlockTestComplete, new
        {
            source,
            sourceFileName = Path.GetFileName(result.SourcePath),
            sourceUrl = ToAssetUri(result.SourcePath),
            processedFileName = Path.GetFileName(result.ProcessedPath),
            processedUrl = ToAssetUri(result.ProcessedPath),
            result.SourceWidth,
            result.SourceHeight,
            result.OutputWidth,
            result.OutputHeight,
            result.LatencyMs,
            result.Hints,
            result.Settings
        });
    }

    private string? ResolveAdvancedBlockTestSource(string source, JsonElement data)
    {
        if (source.Equals("contextImage", StringComparison.OrdinalIgnoreCase))
        {
            return _selectedContextImagePath;
        }

        if (source.Equals("selectedSegment", StringComparison.OrdinalIgnoreCase))
        {
            ProcessSegment? segment = null;
            if (_currentResult is not null && TryReadGuid(data, "segmentId", out var segmentId))
            {
                segment = _currentResult.Segments.FirstOrDefault(x => x.Id == segmentId);
            }

            segment ??= _currentResult?.Segments.FirstOrDefault();
            return ResolveSegmentPreviewPath(segment);
        }

        return _advancedBlockTestImagePath;
    }

    private static string? ResolveSegmentPreviewPath(ProcessSegment? segment)
    {
        if (segment is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(segment.ThumbnailPath) && File.Exists(segment.ThumbnailPath))
        {
            return segment.ThumbnailPath;
        }

        return segment.FramePaths.FirstOrDefault(File.Exists);
    }

    private async Task AnalyzeSingleImageAsync(
        string imagePath,
        string segmentSource,
        string motionType,
        string reason,
        string progressStage)
    {
        var progress = new Progress<AnalysisProgress>(p => _ = SendAsync(WebMessageTypes.AnalysisProgress, p));
        var watch = Stopwatch.StartNew();
        await SendAsync(WebMessageTypes.AnalysisStarted, new { });

        var segment = new ProcessSegment
        {
            Sequence = 1,
            StartTime = 0,
            EndTime = 1,
            Duration = 1,
            SegmentSource = segmentSource,
            MotionType = motionType,
            FramePaths = new List<string> { _singleImageFramePreparer.Prepare(imagePath, _settings.SingleImageLongEdge) },
            ThumbnailPath = imagePath,
            Reason = reason
        };
        VideoAnalysisPipeline.EnsureSchema(segment);

        var result = new VideoAnalysisResult
        {
            Metadata = new VideoMetadata
            {
                VideoPath = imagePath,
                FileName = Path.GetFileName(imagePath),
                DurationSeconds = 1,
                Fps = 1,
                FrameCount = 1,
                Width = 0,
                Height = 0
            },
            AnalysisSettings = _settings,
            BaseSegments = new List<ProcessSegment> { CloneSegment(segment) },
            Segments = new List<ProcessSegment> { segment },
            PerformanceMetrics = new VlmPerformanceMetrics
            {
                SegmentCount = 1,
                InputFrames = 1,
                ImageLongEdge = _settings.SingleImageLongEdge,
                VideoDurationSec = 1,
                Timestamp = DateTimeOffset.Now
            }
        };

        await SendAsync(WebMessageTypes.ImageLoaded, new
        {
            metadata = result.Metadata,
            imageUrl = ToAssetUri(imagePath)
        });

        _currentResult = result;
        _baseSegments = DeepCloneSegments(result.BaseSegments);
        _undoStack.Clear();
        _redoStack.Clear();
        _currentHistoryId = null;
        await SendAsync(WebMessageTypes.MotionData, new { samples = result.MotionSamples });
        await SendSegmentsAsync();

        if (!IsOpenCvOnly(_localAiSettings.DefaultProvider))
        {
            ((IProgress<AnalysisProgress>)progress).Report(new AnalysisProgress
            {
                Stage = progressStage,
                Percent = 70,
                Message = $"Local VLM single image · {_settings.SingleImageLongEdge}px · {_settings.SingleImageMaxOutputTokens} tokens · {_settings.SingleImagePromptMode}"
            });
            await AnalyzeSegmentWithLocalVlmAsync(segment);
        }

        watch.Stop();
        EnsurePerformanceMetrics(result).TotalMs = watch.Elapsed.TotalMilliseconds;
        EnsurePerformanceMetrics(result).JsonParsingMs = watch.Elapsed.TotalMilliseconds;
        await SendSegmentsAsync();
        await SaveCurrentHistoryAsync();
        await SendAsync(WebMessageTypes.AnalysisComplete, BuildSummary(result));
    }

    private Task SendCaptureStatusAsync()
    {
        var state = _captureService.State;
        return SendAsync(WebMessageTypes.CaptureStatus, new
        {
            state.PreviewRunning,
            state.Recording,
            state.CameraIndex,
            state.Status,
            previewUrl = string.IsNullOrWhiteSpace(state.PreviewPath) || !File.Exists(state.PreviewPath) ? "" : ToAssetUri(state.PreviewPath),
            recordingPath = state.RecordingPath,
            lastSnapshotUrl = string.IsNullOrWhiteSpace(state.LastSnapshotPath) || !File.Exists(state.LastSnapshotPath) ? "" : ToAssetUri(state.LastSnapshotPath),
            recordingStartedAt = state.RecordingStartedAt
        });
    }

    private static VlmPerformanceMetrics EnsurePerformanceMetrics(VideoAnalysisResult result)
    {
        result.PerformanceMetrics ??= new VlmPerformanceMetrics
        {
            Timestamp = DateTimeOffset.Now
        };

        result.PerformanceMetrics.VideoDurationSec = result.Metadata.DurationSeconds;
        result.PerformanceMetrics.SegmentCount = result.Segments.Count;
        result.PerformanceMetrics.InputFrames = result.Segments.Sum(x => x.FramePaths.Count);
        return result.PerformanceMetrics;
    }

    private async Task ReAnalyzeSegmentAsync(JsonElement data)
    {
        if (_currentResult is null || !TryReadGuid(data, "id", out var id))
        {
            return;
        }

        ApplyAnalysisOptions(data);
        var segment = _currentResult.Segments.FirstOrDefault(x => x.Id == id);
        if (segment is null)
        {
            return;
        }

        var provider = ReadString(data, "provider") ?? _localAiSettings.DefaultProvider;
        if (IsOpenCvOnly(provider))
        {
            await SendErrorAsync("OpenCV only mode does not run AI re-analysis. Change provider to Local VLM.");
            return;
        }

        var originalStart = segment.StartTime;
        var originalEnd = segment.EndTime;
        var analyzed = await AnalyzeSegmentWithLocalVlmAsync(segment);
        if (!analyzed)
        {
            return;
        }

        segment.StartTime = originalStart;
        segment.EndTime = originalEnd;
        segment.Duration = originalEnd - originalStart;
        await SendSegmentsAsync();
        await SaveCurrentHistoryAsync();
    }

    private async Task ApplyLocalVlmAsync(
        VideoAnalysisResult result,
        IProgress<AnalysisProgress> progress,
        CancellationToken cancellationToken)
    {
        await _pipeline.ApplyLocalVlmAsync(
            result,
            _settings,
            _vlmManager,
            progress,
            cancellationToken);
        await SendSegmentsAsync();
        await SendAsync(WebMessageTypes.PerformanceMetrics, result.PerformanceMetrics);
    }

    private async Task<bool> AnalyzeSegmentWithLocalVlmAsync(ProcessSegment segment)
    {
        await _pipeline.AnalyzeSegmentWithLocalVlmAsync(segment, _settings, _vlmManager, CancellationToken.None);
        return true;
    }

    private void UpdateSegment(JsonElement data)
    {
        if (_currentResult is null || !data.TryGetProperty("segment", out var segmentJson))
        {
            return;
        }

        var updated = JsonSerializer.Deserialize<ProcessSegment>(segmentJson.GetRawText(), _jsonOptions);
        if (updated is null)
        {
            return;
        }

        var segment = _currentResult.Segments.FirstOrDefault(x => x.Id == updated.Id);
        if (segment is null)
        {
            return;
        }

        CaptureUndoState();
        CopyEditableFields(updated, segment);
        segment.UserEdited = true;
        segment.SemanticStatus = "user_edited";
        VideoAnalysisPipeline.EnsureSchema(segment);
        _ = SendSegmentsAndSaveHistoryAsync();
    }

    private async Task MergeSegmentsAsync(JsonElement data)
    {
        if (_currentResult is null || !data.TryGetProperty("segmentIds", out var idsJson) ||
            idsJson.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        var ids = idsJson.EnumerateArray()
            .Select(x => Guid.TryParse(x.GetString(), out var id) ? id : Guid.Empty)
            .Where(x => x != Guid.Empty)
            .ToHashSet();
        if (ids.Count < 2)
        {
            return;
        }

        var indexed = _currentResult.Segments
            .Select((segment, index) => new { segment, index })
            .Where(x => ids.Contains(x.segment.Id))
            .OrderBy(x => x.index)
            .ToList();
        if (indexed.Count < 2 || indexed.Any(x => x.index != indexed[0].index + indexed.IndexOf(x)))
        {
            await SendErrorAsync("?쒕줈 ?몄젒??Segment留?蹂묓빀?????덉뒿?덈떎.");
            return;
        }

        CaptureUndoState();
        var items = indexed.Select(x => x.segment).ToList();
        var merged = CreateMergedSegment(items);
        _currentResult.Segments.RemoveRange(indexed[0].index, indexed.Count);
        _currentResult.Segments.Insert(indexed[0].index, merged);
        ResequenceSegments();
        await SendSegmentsAndSaveHistoryAsync();
    }

    private async Task SplitSegmentAsync(JsonElement data)
    {
        if (_currentResult is null ||
            !TryReadGuid(data, "segmentId", out var id) ||
            ReadDouble(data, "atSec") is not { } atSec)
        {
            return;
        }

        var index = _currentResult.Segments.FindIndex(x => x.Id == id);
        if (index < 0)
        {
            return;
        }

        var segment = _currentResult.Segments[index];
        if (atSec <= segment.StartTime + 0.02 || atSec >= segment.EndTime - 0.02)
        {
            await SendErrorAsync("Segment ?대???Playhead ?꾩튂?먯꽌留?遺꾪븷?????덉뒿?덈떎.");
            return;
        }

        CaptureUndoState();
        var left = CloneSegment(segment);
        var right = CloneSegment(segment);
        PrepareSplitSegment(left, segment.StartTime, atSec);
        PrepareSplitSegment(right, atSec, segment.EndTime);
        _currentResult.Segments.RemoveAt(index);
        _currentResult.Segments.InsertRange(index, new[] { left, right });
        ResequenceSegments();
        await SendSegmentsAndSaveHistoryAsync();
    }

    private async Task RestoreOriginalSegmentAsync(JsonElement data)
    {
        if (_currentResult is null || !TryReadGuid(data, "segmentId", out var id))
        {
            return;
        }

        var index = _currentResult.Segments.FindIndex(x => x.Id == id);
        if (index < 0)
        {
            return;
        }

        var selected = _currentResult.Segments[index];
        var sourceIds = ReadGuidList(data, "sourceSegmentIds");
        if (sourceIds.Count == 0)
        {
            sourceIds = selected.SourceSegmentIds
                .Select(x => Guid.TryParse(x, out var sourceId) ? sourceId : Guid.Empty)
                .Where(x => x != Guid.Empty)
                .ToList();
        }

        var restored = _baseSegments
            .Where(x => sourceIds.Contains(x.Id) || x.SourceSegmentIds.Any(idText => sourceIds.Any(sourceId => string.Equals(idText, sourceId.ToString(), StringComparison.OrdinalIgnoreCase))))
            .OrderBy(x => x.StartTime)
            .Select(CloneSegment)
            .ToList();
        if (restored.Count == 0)
        {
            await SendErrorAsync("蹂듦뎄???먮낯 OpenCV Segment瑜?李얠? 紐삵뻽?듬땲??");
            return;
        }

        CaptureUndoState();
        _currentResult.Segments.RemoveAt(index);
        _currentResult.Segments.InsertRange(index, restored);
        ResequenceSegments();
        await SendSegmentsAndSaveHistoryAsync();
    }

    private async Task UndoSegmentEditAsync()
    {
        if (_currentResult is null || _undoStack.Count == 0)
        {
            return;
        }

        _redoStack.Push(DeepCloneSegments(_currentResult.Segments));
        _currentResult.Segments = _undoStack.Pop();
        ResequenceSegments();
        await SendSegmentsAndSaveHistoryAsync();
    }

    private async Task RedoSegmentEditAsync()
    {
        if (_currentResult is null || _redoStack.Count == 0)
        {
            return;
        }

        _undoStack.Push(DeepCloneSegments(_currentResult.Segments));
        _currentResult.Segments = _redoStack.Pop();
        ResequenceSegments();
        await SendSegmentsAndSaveHistoryAsync();
    }

    private async Task SaveLocalAiSettingsAsync(JsonElement data)
    {
        _localAiSettings.DefaultProvider = ReadString(data, "defaultProvider") ?? "local_vlm";
        _localAiSettings.RuntimeProvider = ReadString(data, "runtimeProvider") ?? _localAiSettings.RuntimeProvider;
        _localAiSettings.ActiveModelId = ReadString(data, "activeModelId") ?? _localAiSettings.ActiveModelId;
        _localAiSettings.ActiveModelName = ReadString(data, "activeModelName") ?? _localAiSettings.ActiveModelName;
        _localAiSettings.RuntimePath = ReadString(data, "runtimePath") ?? _localAiSettings.RuntimePath;
        _localAiSettings.ModelPath = ReadString(data, "modelPath") ?? _localAiSettings.ModelPath;
        _localAiSettings.MmprojPath = ReadString(data, "mmprojPath") ?? _localAiSettings.MmprojPath;
        _localAiSettings.EndpointUrl = ReadString(data, "endpointUrl") ?? _localAiSettings.EndpointUrl;
        _localAiSettings.Host = ReadString(data, "host") ?? _localAiSettings.Host;
        _localAiSettings.Port = ReadInt(data, "port") ?? _localAiSettings.Port;
        _localAiSettings.GpuLayers = ReadInt(data, "gpuLayers") ?? _localAiSettings.GpuLayers;
        _localAiSettings.ContextSize = ReadInt(data, "contextSize") ?? _localAiSettings.ContextSize;
        _localAiSettings.StartupTimeoutSeconds = ReadInt(data, "startupTimeoutSeconds") ?? _localAiSettings.StartupTimeoutSeconds;
        _localAiSettings.Device = ReadString(data, "device") ?? _localAiSettings.Device;
        _localAiSettings.InputMode = NormalizeInputMode(ReadString(data, "inputMode") ?? _localAiSettings.InputMode);
        if (_localAiSettings.InputMode.Equals("opencvOnly", StringComparison.OrdinalIgnoreCase))
        {
            _localAiSettings.DefaultProvider = "opencv";
        }
        _localAiSettings.CandidateCount = ReadInt(data, "candidateCount") ?? _localAiSettings.CandidateCount;
        _localAiSettings.ResultLanguage = NormalizeResultLanguage(ReadString(data, "resultLanguage") ?? _localAiSettings.ResultLanguage);
        _localAiSettings.MaxFrames = Math.Clamp(ReadInt(data, "maxFrames") ?? _localAiSettings.MaxFrames, 1, 8);
        _localAiSettings.ImageLongEdge = ReadInt(data, "imageLongEdge") ?? _localAiSettings.ImageLongEdge;
        _localAiSettings.CropMode = ReadString(data, "cropMode") ?? _localAiSettings.CropMode;
        _localAiSettings.OutputMode = ReadString(data, "outputMode") ?? _localAiSettings.OutputMode;
        _localAiSettings.MaxOutputTokens = ReadInt(data, "maxOutputTokens") ?? _localAiSettings.MaxOutputTokens;
        _localAiSettings.SingleImageLongEdge = ReadInt(data, "singleImageLongEdge") ?? _localAiSettings.SingleImageLongEdge;
        _localAiSettings.SingleImageMaxOutputTokens = ReadInt(data, "singleImageMaxOutputTokens") ?? _localAiSettings.SingleImageMaxOutputTokens;
        _localAiSettings.SingleImagePromptMode = NormalizeSingleImagePromptMode(ReadString(data, "singleImagePromptMode") ?? _localAiSettings.SingleImagePromptMode);
        _localAiSettings.Temperature = ReadDouble(data, "temperature") ?? _localAiSettings.Temperature;
        _localAiSettings.StructuredOutput = ReadBool(data, "structuredOutput") ?? _localAiSettings.StructuredOutput;
        _localAiSettings.WarmupOnStart = ReadBool(data, "warmupOnStart") ?? _localAiSettings.WarmupOnStart;
        ApplyLocalSettingsToAnalysisSettings();
        await _vlmManager.SaveSettingsAsync(_localAiSettings);
        await SendLocalAiSettingsAsync();
    }

    private async Task TestVlmModelAsync()
    {
        await _vlmManager.InitializeAsync();
        var status = await _vlmManager.GetStatusAsync();
        await SendAsync(WebMessageTypes.VlmModelTestResult, new
        {
            success = status.Ready,
            message = status.Message,
            status
        });
    }

    private void ExportResult()
    {
        if (_currentResult is null)
        {
            _ = SendErrorAsync("No analysis result is available.");
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "JSON|*.json",
            FileName = $"{Path.GetFileNameWithoutExtension(_currentResult.Metadata.FileName)}_analysis.json"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        File.WriteAllText(dialog.FileName, JsonSerializer.Serialize(_currentResult, _jsonOptions));
    }

    private async Task LoadAnalysisHistoryAsync(JsonElement data)
    {
        var id = ReadString(data, "id");
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        try
        {
            var result = _historyService.LoadResult(id);
            _currentResult = result;
            _baseSegments = DeepCloneSegments(result.BaseSegments.Count > 0 ? result.BaseSegments : result.Segments);
            _undoStack.Clear();
            _redoStack.Clear();
            ResequenceSegments();
            _currentHistoryId = id;

            var fileExists = File.Exists(result.Metadata.VideoPath);
            var isImage = fileExists && IsImageFile(result.Metadata.VideoPath);
            _selectedVideoPath = fileExists && !isImage ? result.Metadata.VideoPath : null;
            var videoUrl = fileExists && !isImage ? _videoServer.SetVideo(result.Metadata.VideoPath) : "";
            var imageUrl = fileExists && isImage ? ToAssetUri(result.Metadata.VideoPath) : "";

            await SendAsync(WebMessageTypes.AnalysisHistoryLoaded, new
            {
                historyId = id,
                metadata = result.Metadata,
                mediaType = isImage ? "image" : "video",
                videoUrl,
                imageUrl,
                videoExists = fileExists && !isImage,
                samples = result.MotionSamples,
                segments = result.Segments.Select(ToSegmentDto).ToList(),
                summary = BuildSummary(result)
            });

            await SendAnalysisHistoryAsync();
        }
        catch (Exception ex)
        {
            _logger.Error("Analysis history load failed", ex);
            await SendErrorAsync($"Analysis history could not be loaded. {ex.Message}");
        }
    }

    private void DeleteAnalysisHistory(JsonElement data)
    {
        var id = ReadString(data, "id");
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        _historyService.Delete(id);
        if (string.Equals(_currentHistoryId, id, StringComparison.OrdinalIgnoreCase))
        {
            _currentHistoryId = null;
        }
    }

    private async Task SendSegmentsAndSaveHistoryAsync()
    {
        await SendSegmentsAsync();
        await SaveCurrentHistoryAsync();
    }

    private async Task SaveCurrentHistoryAsync()
    {
        if (_currentResult is null)
        {
            return;
        }

        if (_currentResult.BaseSegments.Count == 0 && _baseSegments.Count > 0)
        {
            _currentResult.BaseSegments = DeepCloneSegments(_baseSegments);
        }

        var entry = _historyService.Save(_currentResult, _currentHistoryId);
        _currentHistoryId = entry.Id;
        await SendAnalysisHistoryAsync();
    }

    private Task SendAnalysisHistoryAsync()
    {
        return SendAsync(WebMessageTypes.AnalysisHistoryUpdated, new
        {
            items = _historyService.List().Take(10).ToList(),
            currentHistoryId = _currentHistoryId
        });
    }

    private Task SendVlmContextExperimentsAsync()
    {
        var runs = _contextExperimentStore.Load();
        return SendAsync(WebMessageTypes.VlmContextExperimentsUpdated, new
        {
            runs = runs.Take(30).Select(ToVlmContextRunDto).ToList(),
            recommendation = _contextRecommendationService.Build(runs)
        });
    }

    private async Task SendLocalAiSettingsAsync()
    {
        var status = await _vlmManager.GetStatusAsync();
        await SendAsync(WebMessageTypes.LocalAiSettings, new
        {
            settings = _localAiSettings,
            status,
            models = _vlmManager.Profiles.Select(ToVlmModelDto).ToList()
        });
    }

    private Task SendVlmModelsAsync()
    {
        return SendAsync(WebMessageTypes.VlmModels, new
        {
            models = _vlmManager.Profiles.Select(ToVlmModelDto).ToList()
        });
    }

    private async Task SendVlmStatusAsync()
    {
        await SendAsync(WebMessageTypes.VlmStatus, await _vlmManager.GetStatusAsync());
    }

    private async Task SendSegmentsAsync()
    {
        if (_currentResult is null)
        {
            return;
        }

        await SendAsync(WebMessageTypes.SegmentsUpdated, new
        {
            segments = _currentResult.Segments.Select(ToSegmentDto).ToList(),
            summary = BuildSummary(_currentResult)
        });
    }

    private object BuildSummary(VideoAnalysisResult result)
    {
        var segments = result.Segments;
        var motionTime = segments
            .Where(x => !string.Equals(x.MotionType, "Idle", StringComparison.OrdinalIgnoreCase))
            .Sum(x => x.Duration);
        var analyzed = segments.Where(x => x.Confidence > 0).ToList();

        return new
        {
            videoLength = result.Metadata.DurationSeconds,
            detectedSegments = segments.Count,
            motionTime,
            idleTime = Math.Max(0, result.Metadata.DurationSeconds - motionTime),
            aiProvider = _localAiSettings.DefaultProvider,
            model = _localAiSettings.ActiveModelName,
            inputMode = _localAiSettings.InputMode,
            resultLanguage = _settings.ResultLanguage,
            performance = result.PerformanceMetrics,
            averageConfidence = analyzed.Count == 0 ? 0 : analyzed.Average(x => x.Confidence),
            needsReviewCount = segments.Count(x => x.Confidence > 0 && x.Confidence < _settings.ConfidenceThreshold),
            samplingInterval = _settings.SampleIntervalMs
        };
    }

    private object ToSegmentDto(ProcessSegment segment)
    {
        VideoAnalysisPipeline.EnsureSchema(segment);

        return new
        {
            segment.Id,
            segment.Sequence,
            segment.StartTime,
            segment.EndTime,
            segment.Duration,
            segment.SchemaVersion,
            segment.SegmentSource,
            segment.SourceSegmentIds,
            segment.IsMerged,
            segment.SemanticStatus,
            segment.MotionType,
            segment.ActionCode,
            segment.ActionName,
            segment.Confidence,
            segment.Description,
            segment.Reason,
            segment.AiReason,
            segment.AiMatchScore,
            segment.ActorType,
            segment.ProcessRole,
            segment.StateType,
            segment.TargetObject,
            segment.ToolOrActor,
            segment.InteractionType,
            segment.DependencyType,
            segment.Dependency,
            segment.WaitReason,
            segment.RepeatabilityType,
            segment.PathConsistency,
            segment.PositionConsistency,
            segment.MotionDirection,
            segment.MotionLevel,
            segment.IdleBeforeSec,
            segment.IdleAfterSec,
            segment.ActorConfidence,
            segment.ActionConfidence,
            segment.Evidence,
            segment.Uncertainties,
            segment.Timing,
            segment.Observation,
            segment.Semantic,
            segment.Interaction,
            segment.Repeatability,
            segment.Quality,
            segment.Source,
            segment.Candidates,
            segment.LastAnalysisProvider,
            segment.LastAnalysisModel,
            segment.LastAnalysisInputMode,
            segment.LastAnalysisAt,
            segment.AverageMotionScore,
            segment.FramePaths,
            segment.ThumbnailPath,
            segment.Detection,
            thumbnailUrl = string.IsNullOrWhiteSpace(segment.ThumbnailPath) ? "" : ToAssetUri(segment.ThumbnailPath),
            frameUrls = segment.FramePaths.Select(ToAssetUri).ToList(),
            aiTrace = segment.AiTrace is null ? null : new
            {
                segment.AiTrace.Provider,
                segment.AiTrace.Model,
                segment.AiTrace.Adapter,
                segment.AiTrace.PromptVersion,
                segment.AiTrace.RequestSchema,
                segment.AiTrace.Prompt,
                segment.AiTrace.RequestJson,
                segment.AiTrace.RawResponse,
                segment.AiTrace.ParsedResultJson,
                segment.AiTrace.MappingJson,
                segment.AiTrace.CreatedAt,
                frameUrls = segment.AiTrace.FramePaths.Select(ToAssetUri).ToList()
            },
            segment.Alternatives,
            segment.UserEdited,
            needsReview = segment.Confidence > 0 && segment.Confidence < _settings.ConfidenceThreshold
        };
    }

    private object ToVlmContextRunDto(VlmContextExperimentRun run)
    {
        return new
        {
            run.Id,
            run.ExperimentId,
            run.CreatedAt,
            run.InputFileName,
            run.InputType,
            run.InputLabel,
            run.ModelName,
            run.Candidate,
            run.Description,
            run.LatencyMs,
            run.PreprocessMs,
            run.VlmMs,
            run.IsBest,
            run.Prompt,
            run.RequestJson,
            run.RawResponse,
            run.BlockSettings,
            frameUrls = run.FramePaths.Select(ToAssetUri).ToList()
        };
    }

    private void ApplyAnalysisOptions(JsonElement data)
    {
        if (data.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return;
        }

        _localAiSettings.DefaultProvider = ReadString(data, "defaultProvider") ?? _localAiSettings.DefaultProvider;
        _localAiSettings.InputMode = NormalizeInputMode(ReadString(data, "inputMode") ?? _localAiSettings.InputMode);
        if (_localAiSettings.InputMode.Equals("opencvOnly", StringComparison.OrdinalIgnoreCase))
        {
            _localAiSettings.DefaultProvider = "opencv";
        }
        _localAiSettings.CandidateCount = ReadInt(data, "candidateCount") ?? _localAiSettings.CandidateCount;
        _localAiSettings.ResultLanguage = NormalizeResultLanguage(ReadString(data, "resultLanguage") ?? _localAiSettings.ResultLanguage);
        _localAiSettings.MaxFrames = Math.Clamp(ReadInt(data, "maxFrames") ?? _localAiSettings.MaxFrames, 1, 8);
        _localAiSettings.ImageLongEdge = ReadInt(data, "imageLongEdge") ?? _localAiSettings.ImageLongEdge;
        _localAiSettings.CropMode = ReadString(data, "cropMode") ?? _localAiSettings.CropMode;
        _localAiSettings.OutputMode = ReadString(data, "outputMode") ?? _localAiSettings.OutputMode;
        _localAiSettings.MaxOutputTokens = ReadInt(data, "maxOutputTokens") ?? _localAiSettings.MaxOutputTokens;
        _localAiSettings.SingleImageLongEdge = ReadInt(data, "singleImageLongEdge") ?? _localAiSettings.SingleImageLongEdge;
        _localAiSettings.SingleImageMaxOutputTokens = ReadInt(data, "singleImageMaxOutputTokens") ?? _localAiSettings.SingleImageMaxOutputTokens;
        _localAiSettings.SingleImagePromptMode = NormalizeSingleImagePromptMode(ReadString(data, "singleImagePromptMode") ?? _localAiSettings.SingleImagePromptMode);
        _localAiSettings.Temperature = ReadDouble(data, "temperature") ?? _localAiSettings.Temperature;
        ApplyLocalSettingsToAnalysisSettings();
    }

    private string ResolveModel(string provider)
    {
        if (IsOpenCvOnly(provider))
        {
            return "OpenCV";
        }

        return _localAiSettings.ActiveModelName;
    }

    private static bool IsOpenCvOnly(string provider)
    {
        return provider.Equals("opencv", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeResultLanguage(string language)
    {
        return language.Equals("en", StringComparison.OrdinalIgnoreCase) ? "en" : "ko";
    }

    private static string NormalizeInputMode(string mode)
    {
        return mode switch
        {
            "fastVlm" => "fastVlm",
            "opencvOnly" => "opencvOnly",
            "opencvSegments" => "opencvSegments",
            _ => "opencvSegments"
        };
    }

    private static string NormalizeSingleImagePromptMode(string mode)
    {
        return mode switch
        {
            "balanced" => "balanced",
            _ => "fast"
        };
    }

    private void ApplyLocalSettingsToAnalysisSettings()
    {
        _settings.DefaultProvider = _localAiSettings.DefaultProvider;
        _settings.InputMode = _localAiSettings.InputMode;
        _settings.CandidateCount = Math.Clamp(_localAiSettings.CandidateCount, 1, 5);
        _settings.ResultLanguage = _localAiSettings.ResultLanguage;
        _settings.MaxFramesPerSegment = Math.Clamp(_localAiSettings.MaxFrames, 1, 8);
        _settings.FrameResolution = _localAiSettings.ImageLongEdge;
        _settings.CropMode = _localAiSettings.CropMode;
        _settings.OutputMode = _localAiSettings.OutputMode;
        _settings.MaxOutputTokens = _localAiSettings.MaxOutputTokens;
        _settings.SingleImageLongEdge = Math.Clamp(_localAiSettings.SingleImageLongEdge, 160, 1280);
        _settings.SingleImageMaxOutputTokens = Math.Clamp(_localAiSettings.SingleImageMaxOutputTokens, 48, 512);
        _settings.SingleImagePromptMode = NormalizeSingleImagePromptMode(_localAiSettings.SingleImagePromptMode);
        _settings.Temperature = _localAiSettings.Temperature;
    }

    private bool ConfirmGeminiFallback(string failedModel, string fallbackModel, string detail)
    {
        var result = MessageBox.Show(
            this,
            $"{failedModel} 紐⑤뜽???쇱떆?곸쑝濡?怨쇰????곹깭?낅땲??\n\n" +
            "2珥? 4珥? 8珥??湲????먮룞 ?ъ떆?꾪뻽吏留?怨꾩냽 ?ㅽ뙣?덉뒿?덈떎.\n\n" +
            $"???紐⑤뜽({fallbackModel})濡??ㅼ떆 ?쒕룄?좉퉴??\n\n" +
            "Yes: ???紐⑤뜽濡??ъ떆??nNo: ?ш린??以묒?",
            "Gemini 紐⑤뜽 ?쇱떆 ?ъ슜 遺덇?",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        _logger.Info($"Gemini fallback prompt: failed={failedModel}, fallback={fallbackModel}, user={result}, detail={detail}");
        return result == MessageBoxResult.Yes;
    }

    private async Task SendApiLimitExceededAsync(string provider, Exception ex)
    {
        _logger.Error($"{provider} API quota exceeded", ex);
        await SendAsync(WebMessageTypes.ApiLimitExceeded, new
        {
            provider,
            statusCode = 0,
            message = ex.Message
        });
    }

    private static bool TryReadGuid(JsonElement data, string propertyName, out Guid value)
    {
        value = Guid.Empty;
        return data.ValueKind == JsonValueKind.Object &&
               data.TryGetProperty(propertyName, out var json) &&
               Guid.TryParse(json.GetString(), out value);
    }

    private void CaptureUndoState()
    {
        if (_currentResult is null)
        {
            return;
        }

        _undoStack.Push(DeepCloneSegments(_currentResult.Segments));
        _redoStack.Clear();
    }

    private ProcessSegment CreateMergedSegment(IReadOnlyList<ProcessSegment> segments)
    {
        var first = segments[0];
        var start = segments.Min(x => x.StartTime);
        var end = segments.Max(x => x.EndTime);
        var sourceIds = segments
            .SelectMany(x => x.SourceSegmentIds.Count > 0 ? x.SourceSegmentIds : new List<string> { x.Id.ToString() })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var merged = new ProcessSegment
        {
            Sequence = first.Sequence,
            StartTime = start,
            EndTime = end,
            Duration = Math.Max(0, end - start),
            SchemaVersion = "1.2",
            SegmentSource = "manual_edit",
            SourceSegmentIds = sourceIds,
            IsMerged = true,
            SemanticStatus = "needs_review",
            MotionType = "Merged",
            ActionCode = "NEEDS_REVIEW",
            ActionName = "?뺤씤 ?꾩슂",
            Description = "",
            AverageMotionScore = segments.Average(x => x.AverageMotionScore),
            FramePaths = segments.SelectMany(x => x.FramePaths).Where(File.Exists).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            ThumbnailPath = segments.Select(x => x.ThumbnailPath).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? "",
            LastAnalysisProvider = first.LastAnalysisProvider,
            LastAnalysisModel = first.LastAnalysisModel,
            LastAnalysisInputMode = first.LastAnalysisInputMode
        };

        VideoAnalysisPipeline.EnsureSchema(merged);
        return merged;
    }

    private static void PrepareSplitSegment(ProcessSegment segment, double start, double end)
    {
        segment.Id = Guid.NewGuid();
        segment.StartTime = start;
        segment.EndTime = end;
        segment.Duration = Math.Max(0, end - start);
        segment.SchemaVersion = "1.2";
        segment.SegmentSource = "manual_edit";
        segment.IsMerged = false;
        segment.SemanticStatus = "needs_review";
        segment.ActionCode = "NEEDS_REVIEW";
        segment.ActionName = "?뺤씤 ?꾩슂";
        segment.Description = "";
        segment.Candidates = new List<ActionCandidate>();
        segment.Alternatives = new List<string>();
        segment.AiMatchScore = 0;
        segment.Confidence = 0;
        segment.UserEdited = true;
        VideoAnalysisPipeline.EnsureSchema(segment);
    }

    private void ResequenceSegments()
    {
        if (_currentResult is null)
        {
            return;
        }

        for (var i = 0; i < _currentResult.Segments.Count; i++)
        {
            var segment = _currentResult.Segments[i];
            segment.Sequence = i + 1;
            segment.Duration = Math.Max(0, segment.EndTime - segment.StartTime);
            VideoAnalysisPipeline.EnsureSchema(segment);
        }
    }

    private static void CopyEditableFields(ProcessSegment source, ProcessSegment target)
    {
        target.SchemaVersion = string.IsNullOrWhiteSpace(source.SchemaVersion) ? "1.2" : source.SchemaVersion;
        target.ActionCode = string.IsNullOrWhiteSpace(source.ActionCode) ? target.ActionCode : source.ActionCode;
        target.ActionName = source.ActionName;
        target.Description = source.Description;
        target.Reason = source.Reason;
        target.AiReason = source.AiReason;
        target.AiMatchScore = source.AiMatchScore;
        target.Confidence = source.Confidence;
        target.ActorType = source.ActorType;
        target.ProcessRole = source.ProcessRole;
        target.StateType = source.StateType;
        target.TargetObject = source.TargetObject;
        target.ToolOrActor = source.ToolOrActor;
        target.InteractionType = source.InteractionType;
        target.DependencyType = source.DependencyType;
        target.Dependency = string.IsNullOrWhiteSpace(source.Dependency) ? source.DependencyType : source.Dependency;
        target.WaitReason = source.WaitReason;
        target.RepeatabilityType = source.RepeatabilityType;
        target.PathConsistency = source.PathConsistency;
        target.PositionConsistency = source.PositionConsistency;
        target.MotionDirection = source.MotionDirection;
        target.MotionLevel = source.MotionLevel;
        target.IdleBeforeSec = source.IdleBeforeSec;
        target.IdleAfterSec = source.IdleAfterSec;
        target.ActorConfidence = source.ActorConfidence;
        target.ActionConfidence = source.ActionConfidence;
        target.Evidence = source.Evidence ?? new List<string>();
        target.Uncertainties = source.Uncertainties ?? new List<string>();
        target.Semantic = source.Semantic;
        target.Observation = source.Observation;
        target.Interaction = source.Interaction;
        target.Repeatability = source.Repeatability;
        target.Quality = source.Quality;
        target.Source = source.Source;
        target.Candidates = source.Candidates ?? new List<ActionCandidate>();
        target.Alternatives = source.Alternatives ?? new List<string>();
    }

    private static ProcessSegment CloneSegment(ProcessSegment segment)
    {
        return DeepCloneSegments(new[] { segment })[0];
    }

    private static List<ProcessSegment> DeepCloneSegments(IReadOnlyList<ProcessSegment> segments)
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var json = JsonSerializer.Serialize(segments, options);
        return JsonSerializer.Deserialize<List<ProcessSegment>>(json, options) ?? new List<ProcessSegment>();
    }

    private static List<Guid> ReadGuidList(JsonElement data, string propertyName)
    {
        if (data.ValueKind != JsonValueKind.Object ||
            !data.TryGetProperty(propertyName, out var json) ||
            json.ValueKind != JsonValueKind.Array)
        {
            return new List<Guid>();
        }

        return json.EnumerateArray()
            .Select(x => Guid.TryParse(x.GetString(), out var id) ? id : Guid.Empty)
            .Where(x => x != Guid.Empty)
            .ToList();
    }

    private List<VlmContextCandidate>? ReadVlmContextCandidates(JsonElement data)
    {
        if (data.ValueKind != JsonValueKind.Object ||
            !data.TryGetProperty("candidates", out var json) ||
            json.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<List<VlmContextCandidate>>(json.GetRawText(), _jsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.Info($"VLM context candidates could not be parsed: {ex.Message}");
            return null;
        }
    }

    private VlmContextBlockSettings ReadVlmContextBlockSettings(JsonElement data)
    {
        if (data.ValueKind != JsonValueKind.Object ||
            !data.TryGetProperty("blockSettings", out var json) ||
            json.ValueKind != JsonValueKind.Object)
        {
            return new VlmContextBlockSettings();
        }

        try
        {
            return JsonSerializer.Deserialize<VlmContextBlockSettings>(json.GetRawText(), _jsonOptions)
                   ?? new VlmContextBlockSettings();
        }
        catch (JsonException ex)
        {
            _logger.Info($"VLM context block settings could not be parsed: {ex.Message}");
            return new VlmContextBlockSettings();
        }
    }

    private static object ToVlmModelDto(VlmModelProfile model)
    {
        return new
        {
            model.Id,
            model.Name,
            model.Provider,
            model.Adapter,
            model.ModelPath,
            model.MmprojPath,
            model.RuntimePath,
            model.EndpointUrl,
            model.Host,
            model.Port,
            model.GpuLayers,
            model.ContextSize,
            model.StartupTimeoutSeconds,
            model.Quantization,
            model.ModelVersion,
            model.Capabilities,
            model.Defaults
        };
    }

    private static string? ReadString(JsonElement data, string propertyName)
    {
        return data.ValueKind == JsonValueKind.Object &&
               data.TryGetProperty(propertyName, out var json) &&
               json.ValueKind == JsonValueKind.String
            ? json.GetString()
            : null;
    }

    private static int? ReadInt(JsonElement data, string propertyName)
    {
        if (data.ValueKind != JsonValueKind.Object ||
            !data.TryGetProperty(propertyName, out var json))
        {
            return null;
        }

        return json.ValueKind switch
        {
            JsonValueKind.Number when json.TryGetInt32(out var value) => value,
            JsonValueKind.String when int.TryParse(json.GetString(), out var value) => value,
            _ => null
        };
    }

    private static double? ReadDouble(JsonElement data, string propertyName)
    {
        if (data.ValueKind != JsonValueKind.Object ||
            !data.TryGetProperty(propertyName, out var json))
        {
            return null;
        }

        return json.ValueKind switch
        {
            JsonValueKind.Number when json.TryGetDouble(out var value) => value,
            JsonValueKind.String when double.TryParse(json.GetString(), out var value) => value,
            _ => null
        };
    }

    private static bool? ReadBool(JsonElement data, string propertyName)
    {
        if (data.ValueKind != JsonValueKind.Object ||
            !data.TryGetProperty(propertyName, out var json))
        {
            return null;
        }

        return json.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String when bool.TryParse(json.GetString(), out var value) => value,
            _ => null
        };
    }

    private Task SendErrorAsync(string message)
    {
        return SendAsync(WebMessageTypes.Error, new { message });
    }

    private Task SendAsync(string type, object? data)
    {
        var json = JsonSerializer.Serialize(new { type, data }, _jsonOptions);
        if (!Dispatcher.CheckAccess())
        {
            return Dispatcher.InvokeAsync(() => Web.CoreWebView2?.PostWebMessageAsJson(json)).Task;
        }

        Web.CoreWebView2?.PostWebMessageAsJson(json);
        return Task.CompletedTask;
    }

    private static string ToAssetUri(string path)
    {
        var localDataRoot = GetLocalDataRoot();
        var relative = Path.GetRelativePath(localDataRoot, path);
        if (!relative.StartsWith("..", StringComparison.Ordinal) && !Path.IsPathRooted(relative))
        {
            var parts = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(Uri.EscapeDataString);
            return $"https://{LocalDataHost}/{string.Join("/", parts)}";
        }

        return new Uri(path).AbsoluteUri;
    }

    private static bool IsImageFile(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        return extension is ".jpg" or ".jpeg" or ".png" or ".bmp";
    }

    private static string GetLocalDataRoot()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ProcessVideoAnalyzer");
    }

    private sealed class WebMessage
    {
        public string Type { get; set; } = "";
        public JsonElement Data { get; set; }
    }
}
