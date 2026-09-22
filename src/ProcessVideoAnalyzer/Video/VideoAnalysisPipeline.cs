using ProcessVideoAnalyzer.AI.Interfaces;
using ProcessVideoAnalyzer.AI.Models;
using ProcessVideoAnalyzer.AI.Prompt;
using ProcessVideoAnalyzer.Detection;
using ProcessVideoAnalyzer.LocalAI.Core;
using ProcessVideoAnalyzer.Models;
using ProcessVideoAnalyzer.Services;
using System.Diagnostics;
using System.Text.Json;

namespace ProcessVideoAnalyzer.Video;

public sealed class VideoAnalysisPipeline
{
    private readonly VideoMetadataService _metadataService;
    private readonly MotionAnalysisService _motionAnalysisService;
    private readonly EventSegmentationService _eventSegmentationService;
    private readonly RepresentativeFrameService _representativeFrameService;
    private readonly ManufacturingPromptBuilder _promptBuilder;
    private readonly ObjectDetectionPipeline _objectDetectionPipeline;
    private readonly AppLogger _logger;

    public VideoAnalysisPipeline(
        VideoMetadataService metadataService,
        MotionAnalysisService motionAnalysisService,
        EventSegmentationService eventSegmentationService,
        RepresentativeFrameService representativeFrameService,
        ManufacturingPromptBuilder promptBuilder,
        ObjectDetectionPipeline? objectDetectionPipeline,
        AppLogger logger)
    {
        _metadataService = metadataService;
        _motionAnalysisService = motionAnalysisService;
        _eventSegmentationService = eventSegmentationService;
        _representativeFrameService = representativeFrameService;
        _promptBuilder = promptBuilder;
        _objectDetectionPipeline = objectDetectionPipeline ?? new ObjectDetectionPipeline();
        _logger = logger;
    }

    public async Task<VideoAnalysisResult> AnalyzeOpenCvAsync(
        string videoPath,
        AnalysisSettings settings,
        IReadOnlyList<AnalysisRoi> rois,
        IProgress<AnalysisProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        progress?.Report(new AnalysisProgress { Stage = "Preparing Video", Percent = 5, Message = Path.GetFileName(videoPath) });
        _logger.Info($"Analysis start: {videoPath}");

        var metadata = _metadataService.Read(videoPath);
        List<MotionSample> motionSamples;
        List<ProcessSegment> segments;
        if (UseFastVlmTimingWindows(settings))
        {
            progress?.Report(new AnalysisProgress { Stage = "Creating Timing Windows", Percent = 35, Message = "Fast Local VLM scan" });
            motionSamples = new List<MotionSample>();
            segments = _eventSegmentationService.BuildVlmTimingWindows(metadata.DurationSeconds, settings);
        }
        else
        {
            progress?.Report(new AnalysisProgress { Stage = "Analyzing Motion", Percent = 20, Message = "OpenCV frame sampling" });
            motionSamples = await _motionAnalysisService.AnalyzeAsync(videoPath, settings, rois, progress, cancellationToken);
            segments = _eventSegmentationService.BuildSegments(motionSamples, metadata.DurationSeconds, settings);
        }

        _logger.Info($"Detected segments: {segments.Count}");

        progress?.Report(new AnalysisProgress { Stage = "Detecting Segments", Percent = 68, Message = $"{segments.Count} segments" });

        var outputRoot = BuildFrameOutputRoot(videoPath);
        await _representativeFrameService.ExtractAsync(videoPath, segments, settings, outputRoot, progress, cancellationToken);
        InitializeSegmentSchema(segments);

        progress?.Report(new AnalysisProgress { Stage = "OpenCV Complete", Percent = 80, Message = $"{segments.Count} segments" });

        return new VideoAnalysisResult
        {
            Metadata = metadata,
            AnalysisSettings = settings,
            Rois = rois.ToList(),
            MotionSamples = motionSamples,
            BaseSegments = CloneSegments(segments),
            Segments = segments
        };
    }

    public async Task ApplyCloudAiAsync(
        VideoAnalysisResult result,
        AnalysisSettings settings,
        string provider,
        string model,
        ICloudVisionAnalyzer analyzer,
        string apiKey,
        IProgress<AnalysisProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (result.Segments.Count == 0)
        {
            return;
        }

        progress?.Report(new AnalysisProgress
        {
            Stage = "Running Cloud AI Batch",
            Percent = 84,
            Message = $"{provider}: {result.Segments.Count} segments in one request"
        });

        var requests = result.Segments.Select(segment => new CloudVisionRequest
        {
            Segment = segment,
            FramePaths = segment.FramePaths,
            Prompt = _promptBuilder.Build(segment, settings.CandidateCount, settings.ResultLanguage),
            ResultLanguage = settings.ResultLanguage,
            Provider = provider,
            Model = model,
            InputMode = settings.InputMode,
            CandidateCount = settings.CandidateCount
        }).ToList();

        try
        {
            var aiResults = await analyzer.AnalyzeBatchAsync(requests, apiKey, cancellationToken);
            foreach (var segment in result.Segments)
            {
                var aiResult = aiResults.FirstOrDefault(x => x.Sequence == segment.Sequence);
                if (aiResult is null)
                {
                    MarkAiFailure(segment, provider, model, settings.InputMode, "AI batch response did not include this segment.");
                    continue;
                }

                ApplyCloudResult(segment, aiResult, provider, model, settings.InputMode);
            }

            progress?.Report(new AnalysisProgress
            {
                Stage = "Cloud AI Complete",
                Percent = 98,
                Message = $"{provider}: batch result mapped"
            });
        }
        catch (ApiQuotaExceededException)
        {
            throw;
        }
        catch (ApiModelUnavailableException)
        {
            throw;
        }
        catch (Exception ex)
        {
            foreach (var segment in result.Segments)
            {
                MarkAiFailure(segment, provider, model, settings.InputMode, ex.Message);
            }

            _logger.Error("Cloud AI batch inference failed", ex);
        }
    }

    public async Task ApplyLocalVlmAsync(
        VideoAnalysisResult result,
        AnalysisSettings settings,
        VlmManager vlmManager,
        IProgress<AnalysisProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var total = Stopwatch.StartNew();
        var metrics = new VlmPerformanceMetrics
        {
            OpenCvSegmentationMs = result.PerformanceMetrics?.OpenCvSegmentationMs ?? 0,
            VideoDurationSec = result.Metadata.DurationSeconds,
            SegmentCount = result.Segments.Count,
            ImageLongEdge = settings.FrameResolution,
            InputFrames = result.Segments.Sum(x => x.FramePaths.Count),
            Timestamp = DateTimeOffset.Now
        };

        var status = await vlmManager.GetStatusAsync();
        metrics.ProviderId = status.ProviderId;
        metrics.ModelId = status.ModelId;
        metrics.ModelName = status.ModelName;
        metrics.AdapterId = status.AdapterId;
        metrics.ModelResident = status.ModelResident;
        metrics.WarmupDone = status.WarmupDone;

        if (!status.Ready)
        {
            foreach (var segment in result.Segments)
            {
                MarkLocalVlmFailure(segment, status, status.Message);
            }

            metrics.TotalMs = total.Elapsed.TotalMilliseconds;
            result.PerformanceMetrics = metrics;
            progress?.Report(new AnalysisProgress
            {
                Stage = "Local VLM Not Ready",
                Percent = 100,
                Message = status.Message
            });
            return;
        }

        progress?.Report(new AnalysisProgress
        {
            Stage = "Running Local VLM",
            Percent = 84,
            Message = $"{status.ModelName}: representative frames only"
        });

        for (var index = 0; index < result.Segments.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var segment = result.Segments[index];
            progress?.Report(new AnalysisProgress
            {
                Stage = "Running Local VLM",
                Percent = 84 + (index / (double)Math.Max(result.Segments.Count, 1)) * 14,
                Message = $"{status.ModelName}: segment {index + 1}/{result.Segments.Count}"
            });

            try
            {
                await AttachDetectionAsync(segment, settings, cancellationToken);
                var request = new VlmRequest
                {
                    Segment = segment,
                    FramePaths = segment.FramePaths.Take(settings.MaxFramesPerSegment).ToList(),
                    OutputLanguage = settings.ResultLanguage,
                    Mode = settings.OutputMode,
                    PromptMode = "videoSegment",
                    DetectionFacts = segment.Detection?.PromptFacts ?? "",
                    InputImageLongEdge = settings.FrameResolution,
                    MaxOutputTokens = settings.MaxOutputTokens,
                    Temperature = settings.Temperature
                };

                var parseWatch = Stopwatch.StartNew();
                var vlmResult = await vlmManager.AnalyzeAsync(request, cancellationToken);
                parseWatch.Stop();
                metrics.JsonParsingMs += parseWatch.Elapsed.TotalMilliseconds;
                metrics.OutputTokens = (metrics.OutputTokens ?? 0) + (vlmResult.OutputTokens ?? 0);
                ApplyLocalVlmResult(segment, vlmResult, status);
            }
            catch (Exception ex)
            {
                MarkLocalVlmFailure(segment, status, ex.Message);
                _logger.Error($"Local VLM inference failed for segment {segment.Sequence}", ex);
            }
        }

        metrics.TotalMs = total.Elapsed.TotalMilliseconds;
        result.PerformanceMetrics = metrics;
        progress?.Report(new AnalysisProgress
        {
            Stage = "Local VLM Complete",
            Percent = 98,
            Message = $"{status.ModelName}: semantic mapping complete"
        });
    }

    public async Task AnalyzeSegmentWithLocalVlmAsync(
        ProcessSegment segment,
        AnalysisSettings settings,
        VlmManager vlmManager,
        CancellationToken cancellationToken = default)
    {
        var status = await vlmManager.GetStatusAsync();
        if (!status.Ready)
        {
            MarkLocalVlmFailure(segment, status, status.Message);
            return;
        }

        try
        {
            var isSingleImage = segment.SegmentSource.Equals("snapshot", StringComparison.OrdinalIgnoreCase) ||
                                segment.SegmentSource.Equals("loaded_image", StringComparison.OrdinalIgnoreCase);
            await AttachDetectionAsync(segment, settings, cancellationToken);
            var request = new VlmRequest
            {
                Segment = segment,
                FramePaths = segment.FramePaths.Take(settings.MaxFramesPerSegment).ToList(),
                OutputLanguage = settings.ResultLanguage,
                Mode = isSingleImage ? settings.SingleImagePromptMode : settings.OutputMode,
                PromptMode = isSingleImage ? $"singleImage:{settings.SingleImagePromptMode}" : "videoSegment",
                DetectionFacts = segment.Detection?.PromptFacts ?? "",
                InputImageLongEdge = isSingleImage ? settings.SingleImageLongEdge : settings.FrameResolution,
                MaxOutputTokens = isSingleImage ? settings.SingleImageMaxOutputTokens : settings.MaxOutputTokens,
                Temperature = settings.Temperature
            };
            var result = await vlmManager.AnalyzeAsync(request, cancellationToken);
            ApplyLocalVlmResult(segment, result, status);
        }
        catch (Exception ex)
        {
            MarkLocalVlmFailure(segment, status, ex.Message);
            _logger.Error($"Local VLM inference failed for segment {segment.Sequence}", ex);
        }
    }

    private async Task AttachDetectionAsync(
        ProcessSegment segment,
        AnalysisSettings settings,
        CancellationToken cancellationToken)
    {
        if (!settings.ObjectDetection.Enabled)
        {
            segment.Detection = null;
            return;
        }

        segment.Detection = await _objectDetectionPipeline.AnalyzeAsync(
            segment.FramePaths,
            settings.ObjectDetection,
            cancellationToken);
    }

    public async Task AnalyzeSegmentWithCloudAiAsync(
        ProcessSegment segment,
        AnalysisSettings settings,
        string provider,
        string model,
        ICloudVisionAnalyzer analyzer,
        string apiKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new CloudVisionRequest
            {
                Segment = segment,
                FramePaths = segment.FramePaths,
                Prompt = _promptBuilder.Build(segment, settings.CandidateCount, settings.ResultLanguage),
                Provider = provider,
                Model = model,
                InputMode = settings.InputMode,
                CandidateCount = settings.CandidateCount,
                ResultLanguage = settings.ResultLanguage
            };

            var aiResult = await analyzer.AnalyzeAsync(request, apiKey, cancellationToken);
            ApplyCloudResult(segment, aiResult, provider, model, settings.InputMode);
        }
        catch (ApiQuotaExceededException)
        {
            throw;
        }
        catch (ApiModelUnavailableException)
        {
            throw;
        }
        catch (Exception ex)
        {
            MarkAiFailure(segment, provider, model, settings.InputMode, ex.Message);
            _logger.Error($"Cloud AI inference failed for segment {segment.Sequence}", ex);
        }
    }

    private static void MarkAiFailure(
        ProcessSegment segment,
        string provider,
        string model,
        string inputMode,
        string reason)
    {
        segment.ActionCode = "NOT_ANALYZED";
        segment.ActionName = "遺꾩꽍 ?ㅽ뙣";
        segment.Description = "";
        segment.Reason = reason;
        segment.AiReason = reason;
        segment.AiMatchScore = 0;
        segment.Confidence = 0;
        segment.Candidates = new List<ActionCandidate>();
        segment.LastAnalysisProvider = provider;
        segment.LastAnalysisModel = model;
        segment.LastAnalysisInputMode = inputMode;
        segment.LastAnalysisAt = DateTimeOffset.Now;
        segment.SemanticStatus = "needs_review";
        EnsureSchema(segment);
        segment.Semantic!.Description = "";
        segment.Quality!.Confidence = 0;
        segment.Source!.Semantic = provider;
    }

    private static void MarkLocalVlmFailure(ProcessSegment segment, VlmHealthStatus status, string reason)
    {
        segment.ActionCode = "NEEDS_REVIEW";
        segment.ActionName = "?뺤씤 ?꾩슂";
        segment.Description = "";
        segment.Reason = reason;
        segment.AiReason = reason;
        segment.AiMatchScore = 0;
        segment.Confidence = 0;
        segment.Candidates = new List<ActionCandidate>();
        segment.LastAnalysisProvider = "local_vlm";
        segment.LastAnalysisModel = status.ModelName;
        segment.LastAnalysisInputMode = "adaptive";
        segment.LastAnalysisAt = DateTimeOffset.Now;
        segment.SemanticStatus = "needs_review";
        EnsureSchema(segment);
        segment.Semantic!.Description = "";
        segment.Source!.Semantic = "local_vlm";
        segment.Source.ProviderId = status.ProviderId;
        segment.Source.ModelId = status.ModelId;
        segment.Source.AdapterId = status.AdapterId;
    }

    private static void ApplyLocalVlmResult(ProcessSegment segment, VlmResult result, VlmHealthStatus status)
    {
        var action = IsUnknown(result.ActionName) ? FallbackActionName(segment) : result.ActionName.Trim();
        var confidence = result.Confidence <= 0 && !IsUnknown(action)
            ? FallbackConfidence(segment)
            : result.Confidence;
        var score = Math.Round(Math.Clamp(confidence, 0, 1) * 100, 1);
        segment.ActionCode = MakeActionCode(action);
        segment.ActionName = action;
        segment.Description = NormalizeLocalVlmDescription(result.Description, action);
        segment.ActorType = result.ActorType;
        segment.ProcessRole = result.ProcessType;
        segment.StateType = result.StateType;
        segment.TargetObject = result.TargetObject;
        segment.ToolOrActor = result.ToolOrActor;
        segment.InteractionType = result.InteractionType;
        segment.DependencyType = result.Dependency;
        segment.Dependency = result.Dependency;
        segment.WaitReason = result.WaitReason;
        segment.RepeatabilityType = result.RepeatabilityType;
        segment.PathConsistency = result.PathConsistency;
        segment.PositionConsistency = result.PositionConsistency;
        segment.ActorConfidence = result.ActorConfidence;
        segment.ActionConfidence = result.ActionConfidence;
        segment.Evidence = result.Evidence;
        segment.Uncertainties = result.Uncertainties;
        segment.AiReason = result.Evidence.Count == 0 ? "" : string.Join(Environment.NewLine, result.Evidence);
        segment.AiMatchScore = score;
        segment.Confidence = confidence;
        segment.Candidates = result.Candidates.Count == 0
            ? new List<ActionCandidate> { new() { Action = action, MatchScore = score } }
            : result.Candidates;
        segment.Alternatives = segment.Candidates.Select(x => x.Action).ToList();
        segment.LastAnalysisProvider = "local_vlm";
        segment.LastAnalysisModel = status.ModelName;
        segment.LastAnalysisInputMode = "adaptive";
        segment.LastAnalysisAt = DateTimeOffset.Now;
        segment.SemanticStatus = "ai_generated";
        segment.UserEdited = false;
        EnsureSchema(segment);
        segment.Semantic!.ActorType = segment.ActorType;
        segment.Semantic.MotionType = result.MotionType;
        segment.Semantic.ProcessRole = segment.ProcessRole;
        segment.Semantic.StateType = segment.StateType;
        segment.Semantic.ActionName = segment.ActionName;
        segment.Semantic.Description = segment.Description;
        segment.Semantic.TargetObject = segment.TargetObject;
        segment.Semantic.ToolOrActor = segment.ToolOrActor;
        segment.Semantic.PathConsistency = segment.PathConsistency;
        segment.Semantic.PositionConsistency = segment.PositionConsistency;
        segment.Semantic.Evidence = segment.Evidence;
        segment.Interaction!.InteractionType = segment.InteractionType;
        segment.Interaction.Dependency = segment.DependencyType;
        segment.Interaction.WaitReason = segment.WaitReason;
        segment.Repeatability!.Type = segment.RepeatabilityType;
        segment.Quality!.Confidence = segment.Confidence;
        segment.Quality.ActorConfidence = segment.ActorConfidence;
        segment.Quality.ActionConfidence = segment.ActionConfidence;
        segment.Quality.Uncertainties = segment.Uncertainties;
        segment.Source!.Semantic = "local_vlm";
        segment.Source.ProviderId = status.ProviderId;
        segment.Source.ModelId = status.ModelId;
        segment.Source.AdapterId = status.AdapterId;
        segment.Source.PromptVersion = "manufacturing-fast-v1";
        segment.AiTrace = new SegmentAiTrace
        {
            Provider = "local_vlm",
            Model = status.ModelName,
            Adapter = status.AdapterId,
            PromptVersion = "manufacturing-fast-v1",
            RequestSchema = "selectedAction, actionName, operationGuess, taskDescription, actorType, processType, stateType, targetObject, toolOrMachine, observedMotion, automationMeaning, confidence, candidates",
            Prompt = result.TracePrompt,
            RequestJson = result.TraceRequestJson,
            RawResponse = result.TraceRawResponse,
            ParsedResultJson = JsonSerializer.Serialize(new
            {
                result.ActorType,
                result.ProcessType,
                result.MotionType,
                result.StateType,
                result.ActionName,
                result.Description,
                result.TargetObject,
                result.ToolOrActor,
                result.InteractionType,
                result.Dependency,
                result.WaitReason,
                result.RepeatabilityType,
                result.PathConsistency,
                result.PositionConsistency,
                result.Confidence,
                result.ActorConfidence,
                result.ActionConfidence,
                result.Evidence,
                result.Uncertainties,
                result.Candidates,
                result.OutputTokens
            }),
            MappingJson = JsonSerializer.Serialize(new
            {
                Description = "semantic.description / description",
                ActionName = "semantic.actionName / actionName",
                Actor = "semantic.actorType / actorType",
                ProcessType = "semantic.processRole / processRole",
                State = "semantic.stateType / stateType",
                TargetObject = "semantic.targetObject / targetObject",
                ToolOrActor = "semantic.toolOrActor / toolOrActor",
                Evidence = "semantic.evidence / evidence",
                AiMatchScore = "quality.confidence / aiMatchScore",
                Candidates = "candidates"
            }),
            FramePaths = result.TraceFramePaths.ToList(),
            CreatedAt = DateTimeOffset.Now
        };
    }

    private static bool IsUnknown(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var normalized = value.Trim().ToLowerInvariant();
        return normalized is "unknown" or "not_analyzed" or "needs_review" or "?";
    }

    private static string FallbackActionName(ProcessSegment segment)
    {
        return string.Equals(segment.MotionType, "Idle", StringComparison.OrdinalIgnoreCase) ||
               segment.AverageMotionScore < 2.0
            ? "대기/유지"
            : "설비 동작";
    }

    private static double FallbackConfidence(ProcessSegment segment)
    {
        return string.Equals(segment.MotionType, "Idle", StringComparison.OrdinalIgnoreCase) ? 0.45 : 0.5;
    }

    private static string NormalizeLocalVlmDescription(string? description, string action)
    {
        var text = description?.Trim() ?? "";
        if (!IsBadLocalVlmText(text))
        {
            return text;
        }

        return action switch
        {
            "부품 투입/적재" => "프레임에서 부품을 투입하거나 적재하는 동작으로 판단됩니다.",
            "위치 정렬" => "프레임에서 대상물의 위치를 맞추는 동작으로 판단됩니다.",
            "클램프/고정" => "프레임에서 대상물을 고정하는 동작으로 판단됩니다.",
            "압입/체결" => "프레임에서 압입 또는 체결 동작으로 판단됩니다.",
            "이송/반송" => "프레임에서 대상물이 이동하거나 반송되는 동작으로 판단됩니다.",
            "설비 동작" => "프레임에서 설비가 동작하는 장면으로 판단됩니다.",
            "검사/확인" => "프레임에서 검사 또는 확인 동작으로 판단됩니다.",
            "작업자 취급" => "프레임에서 작업자가 대상물을 취급하는 동작으로 판단됩니다.",
            "대기/유지" => "프레임에서 뚜렷한 작업 변화가 적은 대기 상태로 판단됩니다.",
            _ => ""
        };
    }

    private static bool UseFastVlmTimingWindows(AnalysisSettings settings)
    {
        return !settings.DefaultProvider.Equals("opencv", StringComparison.OrdinalIgnoreCase) &&
               settings.InputMode.Equals("fastVlm", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsBadLocalVlmText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        var signalChars = text.Count(char.IsLetterOrDigit);
        var questionMarks = text.Count(x => x is '?' or '\uFFFD');
        if (questionMarks > 0 && questionMarks >= Math.Max(2, signalChars / 2))
        {
            return true;
        }

        var normalized = text.Trim().ToLowerInvariant();
        return normalized.Contains("machine is operating", StringComparison.OrdinalIgnoreCase) ||
               normalized.Contains("no human", StringComparison.OrdinalIgnoreCase) ||
               normalized.Contains("\uAE30\uACC4\uAC00 \uB3D9\uC791", StringComparison.OrdinalIgnoreCase) ||
               normalized.Contains("\uC124\uBE44\uAC00 \uB3D9\uC791", StringComparison.OrdinalIgnoreCase) ||
               normalized.Contains("\uC778\uAC04\uC774 \uC9C1\uC811 \uAC1C\uC785\uD558\uC9C0 \uC54A\uC74C", StringComparison.OrdinalIgnoreCase);
    }

    private static void ApplyCloudResult(
        ProcessSegment segment,
        CloudVisionResult result,
        string provider,
        string model,
        string inputMode)
    {
        var candidates = result.Candidates
            .Where(x => !string.IsNullOrWhiteSpace(x.Action))
            .Select(x => new ActionCandidate
            {
                Action = x.Action.Trim(),
                MatchScore = NormalizeScore(x.MatchScore)
            })
            .OrderByDescending(x => x.MatchScore)
            .Take(5)
            .ToList();

        var primaryAction = string.IsNullOrWhiteSpace(result.PrimaryAction)
            ? candidates.FirstOrDefault()?.Action ?? "?뺤씤 ?꾩슂"
            : result.PrimaryAction.Trim();

        if (candidates.Count == 0)
        {
            candidates.Add(new ActionCandidate { Action = primaryAction, MatchScore = 0 });
        }

        var score = candidates.FirstOrDefault(x => string.Equals(x.Action, primaryAction, StringComparison.OrdinalIgnoreCase))?.MatchScore
            ?? candidates[0].MatchScore;

        segment.ActionCode = MakeActionCode(primaryAction);
        segment.ActionName = primaryAction;
        segment.Description = result.Description ?? "";
        segment.Reason = result.Reason ?? "";
        segment.ActorType = NormalizeToken(result.ActorType);
        segment.ProcessRole = NormalizeToken(result.ProcessRole);
        segment.StateType = NormalizeToken(result.StateType);
        segment.TargetObject = result.TargetObject ?? "";
        segment.ToolOrActor = result.ToolOrActor ?? "";
        segment.InteractionType = NormalizeToken(result.InteractionType);
        segment.DependencyType = NormalizeToken(result.Dependency);
        segment.Dependency = segment.DependencyType;
        segment.WaitReason = result.WaitReason ?? "";
        segment.RepeatabilityType = NormalizeToken(result.RepeatabilityType);
        segment.PathConsistency = NormalizeToken(result.PathConsistency);
        segment.PositionConsistency = NormalizeToken(result.PositionConsistency);
        segment.MotionDirection = result.MotionDirection ?? "";
        segment.MotionLevel = NormalizeToken(result.MotionLevel);
        segment.ActorConfidence = NormalizeRatio(result.ActorConfidence);
        segment.ActionConfidence = NormalizeRatio(result.ActionConfidence);
        segment.Evidence = result.Evidence.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        segment.Uncertainties = result.Uncertainties.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        segment.AiReason = segment.Evidence.Count > 0 ? string.Join(Environment.NewLine, segment.Evidence) : segment.Reason;
        segment.AiMatchScore = score;
        segment.Confidence = score / 100.0;
        segment.Candidates = candidates;
        segment.Alternatives = candidates.Select(x => x.Action).ToList();
        segment.LastAnalysisProvider = provider;
        segment.LastAnalysisModel = model;
        segment.LastAnalysisInputMode = inputMode;
        segment.LastAnalysisAt = DateTimeOffset.Now;
        segment.SemanticStatus = "ai_generated";
        segment.UserEdited = false;
        EnsureSchema(segment);
        segment.Semantic!.ActorType = segment.ActorType;
        segment.Semantic.MotionType = string.IsNullOrWhiteSpace(result.MotionType) ? segment.Semantic.MotionType : NormalizeToken(result.MotionType);
        segment.Semantic.ProcessRole = segment.ProcessRole;
        segment.Semantic.StateType = segment.StateType;
        segment.Semantic.ActionName = segment.ActionName;
        segment.Semantic.TargetObject = segment.TargetObject;
        segment.Semantic.ToolOrActor = segment.ToolOrActor;
        segment.Semantic.Description = segment.Description;
        segment.Semantic.PathConsistency = segment.PathConsistency;
        segment.Semantic.PositionConsistency = segment.PositionConsistency;
        segment.Semantic.Evidence = segment.Evidence;
        segment.Observation!.MotionDirection = segment.MotionDirection;
        segment.Observation.MotionLevel = segment.MotionLevel;
        segment.Interaction!.InteractionType = segment.InteractionType;
        segment.Interaction.Dependency = segment.DependencyType;
        segment.Interaction.WaitReason = segment.WaitReason;
        segment.Interaction.HumanPresent = segment.ActorType is "human" or "mixed";
        segment.Interaction.MachinePresent = segment.ActorType is "machine" or "mixed";
        segment.Repeatability!.Type = segment.RepeatabilityType;
        segment.Quality!.Confidence = segment.Confidence;
        segment.Quality.ActorConfidence = segment.ActorConfidence;
        segment.Quality.ActionConfidence = segment.ActionConfidence;
        segment.Quality.Uncertainties = segment.Uncertainties;
        segment.Source!.Semantic = provider;
        segment.Source.ActorType = provider;
    }

    private static void InitializeSegmentSchema(IReadOnlyList<ProcessSegment> segments)
    {
        foreach (var segment in segments)
        {
            EnsureSchema(segment);
        }
    }

    public static void EnsureSchema(ProcessSegment segment)
    {
        segment.SchemaVersion = string.IsNullOrWhiteSpace(segment.SchemaVersion) ? "1.2" : segment.SchemaVersion;
        segment.SegmentSource = string.IsNullOrWhiteSpace(segment.SegmentSource) ? "opencv" : segment.SegmentSource;
        if (segment.SourceSegmentIds.Count == 0)
        {
            segment.SourceSegmentIds.Add(segment.Id.ToString());
        }

        segment.Timing ??= new SegmentTiming();
        segment.Timing.StartSec = segment.StartTime;
        segment.Timing.EndSec = segment.EndTime;
        segment.Timing.DurationSec = segment.Duration;
        segment.Timing.IdleBeforeSec = segment.IdleBeforeSec;
        segment.Timing.IdleAfterSec = segment.IdleAfterSec;

        segment.Observation ??= new SegmentObservation();
        segment.Observation.MotionDetected = !string.Equals(segment.MotionType, "Idle", StringComparison.OrdinalIgnoreCase);
        segment.Observation.MotionDistancePx = segment.AverageMotionScore;
        segment.Observation.MotionDirection = segment.MotionDirection;
        segment.Observation.MotionLevel = segment.MotionLevel;

        segment.Semantic ??= new SegmentSemantic();
        segment.Semantic.ActorType = string.IsNullOrWhiteSpace(segment.Semantic.ActorType) ? segment.ActorType : segment.Semantic.ActorType;
        segment.Semantic.ProcessRole = string.IsNullOrWhiteSpace(segment.Semantic.ProcessRole) ? segment.ProcessRole : segment.Semantic.ProcessRole;
        segment.Semantic.StateType = string.IsNullOrWhiteSpace(segment.Semantic.StateType) ? segment.StateType : segment.Semantic.StateType;
        segment.Semantic.ActionName = string.IsNullOrWhiteSpace(segment.Semantic.ActionName) ? segment.ActionName : segment.Semantic.ActionName;
        segment.Semantic.Description = string.IsNullOrWhiteSpace(segment.Semantic.Description) ? segment.Description : segment.Semantic.Description;
        segment.Semantic.TargetObject = string.IsNullOrWhiteSpace(segment.Semantic.TargetObject) ? segment.TargetObject : segment.Semantic.TargetObject;
        segment.Semantic.ToolOrActor = string.IsNullOrWhiteSpace(segment.Semantic.ToolOrActor) ? segment.ToolOrActor : segment.Semantic.ToolOrActor;
        segment.Semantic.PathConsistency = string.IsNullOrWhiteSpace(segment.Semantic.PathConsistency) ? segment.PathConsistency : segment.Semantic.PathConsistency;
        segment.Semantic.PositionConsistency = string.IsNullOrWhiteSpace(segment.Semantic.PositionConsistency) ? segment.PositionConsistency : segment.Semantic.PositionConsistency;
        if (segment.Semantic.Evidence.Count == 0 && segment.Evidence.Count > 0)
        {
            segment.Semantic.Evidence = segment.Evidence;
        }

        segment.Interaction ??= new SegmentInteraction();
        segment.Interaction.InteractionType = string.IsNullOrWhiteSpace(segment.Interaction.InteractionType) ? segment.InteractionType : segment.Interaction.InteractionType;
        segment.Interaction.Dependency = string.IsNullOrWhiteSpace(segment.Interaction.Dependency) ? segment.DependencyType : segment.Interaction.Dependency;
        segment.Interaction.WaitReason = string.IsNullOrWhiteSpace(segment.Interaction.WaitReason) ? segment.WaitReason : segment.Interaction.WaitReason;

        segment.Repeatability ??= new SegmentRepeatability { Type = segment.RepeatabilityType };
        segment.Quality ??= new SegmentQuality();
        segment.Quality.Confidence = segment.Confidence;
        segment.Quality.ActorConfidence = segment.ActorConfidence;
        segment.Quality.ActionConfidence = segment.ActionConfidence;
        if (segment.Quality.Uncertainties.Count == 0 && segment.Uncertainties.Count > 0)
        {
            segment.Quality.Uncertainties = segment.Uncertainties;
        }

        segment.Source ??= new SegmentSourceInfo();
    }

    private static double NormalizeScore(double value)
    {
        return value <= 1
            ? Math.Round(Math.Clamp(value, 0, 1) * 100, 1)
            : Math.Round(Math.Clamp(value, 0, 100), 1);
    }

    private static double? NormalizeRatio(double? value)
    {
        if (value is null)
        {
            return null;
        }

        return value <= 1
            ? Math.Round(Math.Clamp(value.Value, 0, 1), 3)
            : Math.Round(Math.Clamp(value.Value, 0, 100) / 100.0, 3);
    }

    private static string NormalizeToken(string? value)
    {
        var token = value?.Trim().ToLowerInvariant().Replace(' ', '_').Replace('-', '_');
        return string.IsNullOrWhiteSpace(token) ? "unknown" : token;
    }

    private static List<ProcessSegment> CloneSegments(IReadOnlyList<ProcessSegment> segments)
    {
        var json = JsonSerializer.Serialize(segments, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        return JsonSerializer.Deserialize<List<ProcessSegment>>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? new List<ProcessSegment>();
    }

    private static string MakeActionCode(string action)
    {
        var chars = action
            .Normalize()
            .Select(ch => char.IsLetterOrDigit(ch) ? char.ToUpperInvariant(ch) : '_')
            .ToArray();
        var code = string.Join("", new string(chars).Split('_', StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrWhiteSpace(code) ? "UNKNOWN" : code;
    }

    private static string BuildFrameOutputRoot(string videoPath)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var folderName = $"{Path.GetFileNameWithoutExtension(videoPath)}_{DateTime.Now:yyyyMMdd_HHmmss}";
        return Path.Combine(appData, "ProcessVideoAnalyzer", "Frames", folderName);
    }
}
