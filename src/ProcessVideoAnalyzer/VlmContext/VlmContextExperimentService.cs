using System.Diagnostics;
using ProcessVideoAnalyzer.Imaging;
using ProcessVideoAnalyzer.LocalAI.Core;
using ProcessVideoAnalyzer.Models;
using ProcessVideoAnalyzer.Video;
using ProcessVideoAnalyzer.VlmContext.Steps;

namespace ProcessVideoAnalyzer.VlmContext;

public sealed class VlmContextExperimentService
{
    private readonly SingleImageFramePreparer _framePreparer;
    private readonly VlmContextStepRunner _stepRunner;

    public VlmContextExperimentService(SingleImageFramePreparer framePreparer)
    {
        _framePreparer = framePreparer;
        _stepRunner = new VlmContextStepRunner(framePreparer);
    }

    public async Task<List<VlmContextExperimentRun>> RunDefaultCompareAsync(
        string imagePath,
        IReadOnlyList<VlmContextCandidate>? candidates,
        VlmContextBlockSettings blockSettings,
        AnalysisSettings settings,
        VlmManager vlmManager,
        string modelName,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        var experimentId = Guid.NewGuid();
        var runs = new List<VlmContextExperimentRun>();
        foreach (var candidate in PrepareCandidates(candidates, VlmContextCandidateFactory.CreateImageCandidates()))
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report($"{candidate.Name} running...");
            runs.Add(await RunCandidateAsync(
                experimentId,
                new List<string> { imagePath },
                "image",
                Path.GetFileName(imagePath),
                Path.GetFileName(imagePath),
                null,
                candidate,
                blockSettings,
                settings,
                vlmManager,
                modelName,
                cancellationToken));
        }

        return runs;
    }

    public async Task<List<VlmContextExperimentRun>> RunSegmentCompareAsync(
        ProcessSegment sourceSegment,
        IReadOnlyList<VlmContextCandidate>? candidates,
        VlmContextBlockSettings blockSettings,
        AnalysisSettings settings,
        VlmManager vlmManager,
        string modelName,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        var sourceFrames = sourceSegment.FramePaths
            .Where(File.Exists)
            .DefaultIfEmpty(sourceSegment.ThumbnailPath)
            .Where(File.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (sourceFrames.Count == 0)
        {
            throw new InvalidOperationException("Selected segment has no available frame paths.");
        }

        var experimentId = Guid.NewGuid();
        var runs = new List<VlmContextExperimentRun>();
        var label = $"Segment {sourceSegment.Sequence} - {sourceSegment.StartTime:0.00}s to {sourceSegment.EndTime:0.00}s";
        foreach (var candidate in PrepareCandidates(candidates, VlmContextCandidateFactory.CreateSegmentCandidates()))
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report($"{candidate.Name} segment compare running...");
            runs.Add(await RunCandidateAsync(
                experimentId,
                sourceFrames,
                "segment",
                label,
                label,
                sourceSegment,
                candidate,
                blockSettings,
                settings,
                vlmManager,
                modelName,
                cancellationToken));
        }

        return runs;
    }

    private async Task<VlmContextExperimentRun> RunCandidateAsync(
        Guid experimentId,
        IReadOnlyList<string> sourceFramePaths,
        string inputType,
        string inputFileName,
        string inputLabel,
        ProcessSegment? sourceSegment,
        VlmContextCandidate candidate,
        VlmContextBlockSettings blockSettings,
        AnalysisSettings settings,
        VlmManager vlmManager,
        string modelName,
        CancellationToken cancellationToken)
    {
        var effectiveBlockSettings = candidate.BlockSettings ?? blockSettings;
        var totalWatch = Stopwatch.StartNew();
        var preprocessWatch = Stopwatch.StartNew();
        var recipe = candidate.Recipe ?? VlmContextRecipeFactory.FromLegacySettings(candidate, effectiveBlockSettings);
        var pipelineResult = await _stepRunner.RunAsync(new VlmContextPipelineInput
        {
            SourceFramePaths = sourceFramePaths,
            InputType = inputType,
            SourceSegment = sourceSegment,
            Candidate = candidate,
            BlockSettings = effectiveBlockSettings,
            Recipe = recipe
        }, cancellationToken);
        var selectedFrames = pipelineResult.SelectedFramePaths;
        var preparedFrames = pipelineResult.PreparedFramePaths;
        preprocessWatch.Stop();

        var segment = new ProcessSegment
        {
            Sequence = sourceSegment?.Sequence ?? 1,
            StartTime = sourceSegment?.StartTime ?? 0,
            EndTime = sourceSegment?.EndTime ?? 1,
            Duration = sourceSegment?.Duration ?? 1,
            SegmentSource = "vlm_context_builder",
            MotionType = sourceSegment?.MotionType ?? "Context Test",
            FramePaths = preparedFrames,
            ThumbnailPath = sourceSegment?.ThumbnailPath ?? selectedFrames.FirstOrDefault() ?? "",
            Reason = $"VLM Context Builder - {candidate.Name}",
            AverageMotionScore = sourceSegment?.AverageMotionScore ?? (candidate.MotionSummary ? 1 : 0)
        };
        VideoAnalysisPipeline.EnsureSchema(segment);

        var detectionFacts = pipelineResult.ContextText;

        var vlmWatch = Stopwatch.StartNew();
        var result = await vlmManager.AnalyzeAsync(new VlmRequest
        {
            Segment = segment,
            FramePaths = segment.FramePaths,
            OutputLanguage = settings.ResultLanguage,
            Mode = "contextBuilder",
            PromptMode = candidate.PromptMode.Equals("fast", StringComparison.OrdinalIgnoreCase)
                ? "singleImage:fast"
                : "contextDescription",
            DetectionFacts = detectionFacts,
            InputImageLongEdge = candidate.ImageLongEdge,
            MaxOutputTokens = candidate.MaxOutputTokens,
            Temperature = Math.Clamp(settings.Temperature, 0, 1)
        }, cancellationToken);
        vlmWatch.Stop();
        totalWatch.Stop();

        return new VlmContextExperimentRun
        {
            ExperimentId = experimentId,
            InputPath = sourceFramePaths.FirstOrDefault() ?? "",
            InputFileName = inputFileName,
            InputType = inputType,
            InputLabel = inputLabel,
            ModelName = modelName,
            Candidate = candidate,
            Description = result.Description,
            LatencyMs = totalWatch.Elapsed.TotalMilliseconds,
            PreprocessMs = preprocessWatch.Elapsed.TotalMilliseconds,
            VlmMs = vlmWatch.Elapsed.TotalMilliseconds,
            Prompt = result.TracePrompt,
            RequestJson = result.TraceRequestJson,
            RawResponse = result.TraceRawResponse,
            BlockSettings = effectiveBlockSettings,
            Recipe = recipe,
            StepTraces = pipelineResult.StepTraces,
            FramePaths = result.TraceFramePaths
        };
    }

    private static IReadOnlyList<VlmContextCandidate> PrepareCandidates(
        IReadOnlyList<VlmContextCandidate>? candidates,
        IReadOnlyList<VlmContextCandidate> defaults)
    {
        var source = candidates is { Count: > 0 } ? candidates : defaults;
        return source
            .Where(x => x.Enabled)
            .Select(NormalizeCandidate)
            .Take(6)
            .ToList();
    }

    private static VlmContextCandidate NormalizeCandidate(VlmContextCandidate candidate)
    {
        var id = string.IsNullOrWhiteSpace(candidate.Id)
            ? Guid.NewGuid().ToString("N")
            : candidate.Id.Trim();
        var name = string.IsNullOrWhiteSpace(candidate.Name)
            ? id
            : candidate.Name.Trim();

        return new VlmContextCandidate
        {
            Id = id,
            Name = name,
            Enabled = candidate.Enabled,
            AdvancedPresetId = string.IsNullOrWhiteSpace(candidate.AdvancedPresetId) ? "none" : candidate.AdvancedPresetId.Trim(),
            AdvancedPresetName = string.IsNullOrWhiteSpace(candidate.AdvancedPresetName) ? "None" : candidate.AdvancedPresetName.Trim(),
            BlockSettings = candidate.BlockSettings,
            Recipe = candidate.Recipe,
            ImageLongEdge = Math.Clamp(candidate.ImageLongEdge <= 0 ? 768 : candidate.ImageLongEdge, 160, 1280),
            FrameCount = Math.Clamp(candidate.FrameCount <= 0 ? 1 : candidate.FrameCount, 1, 8),
            MotionSummary = candidate.MotionSummary,
            YoloHints = candidate.YoloHints,
            OcrHints = candidate.OcrHints,
            RoiMode = NormalizeOption(candidate.RoiMode, "fullFrame"),
            CropMode = NormalizeOption(candidate.CropMode, "fullFrame"),
            SamplingMode = NormalizeOption(candidate.SamplingMode, "uniform"),
            YoloConfidence = Math.Clamp(candidate.YoloConfidence <= 0 ? 0.45 : candidate.YoloConfidence, 0.05, 0.95),
            TargetLabels = candidate.TargetLabels?.Trim() ?? "",
            PromptMode = NormalizeOption(candidate.PromptMode, "description"),
            MaxOutputTokens = Math.Clamp(candidate.MaxOutputTokens <= 0 ? 120 : candidate.MaxOutputTokens, 32, 512),
            Description = candidate.Description ?? ""
        };
    }

    private static string NormalizeOption(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static List<string> SelectFrames(IReadOnlyList<string> framePaths, int count, string samplingMode)
    {
        var existing = framePaths.Where(File.Exists).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (existing.Count <= 1 || count <= 1)
        {
            if (samplingMode.Equals("last", StringComparison.OrdinalIgnoreCase))
            {
                return existing.TakeLast(1).ToList();
            }

            return samplingMode.Equals("middle", StringComparison.OrdinalIgnoreCase)
                ? existing.Skip(existing.Count / 2).Take(1).ToList()
                : existing.Take(1).ToList();
        }

        var take = Math.Min(count, existing.Count);
        if (samplingMode.Equals("first", StringComparison.OrdinalIgnoreCase))
        {
            return existing.Take(take).ToList();
        }

        if (samplingMode.Equals("last", StringComparison.OrdinalIgnoreCase))
        {
            return existing.TakeLast(take).ToList();
        }

        if (samplingMode.Equals("middle", StringComparison.OrdinalIgnoreCase))
        {
            var center = existing.Count / 2;
            var start = Math.Max(0, center - (take / 2));
            return existing.Skip(start).Take(take).ToList();
        }

        var selected = new List<string>();
        for (var i = 0; i < take; i++)
        {
            var index = take == 1 ? 0 : (int)Math.Round(i * (existing.Count - 1) / (double)(take - 1));
            selected.Add(existing[index]);
        }

        return selected.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static string BuildContextHints(
        string inputType,
        ProcessSegment segment,
        int frameCount,
        VlmContextCandidate candidate,
        VlmContextBlockSettings blockSettings)
    {
        var lines = new List<string>
        {
            "Context hints:",
            $"- Input type: {inputType}.",
            $"- VLM receives {frameCount} representative frame(s).",
            "- Focus on the visible manufacturing action, object, worker, tool, and machine state.",
            "- Return only the actual work description, not a generic status.",
            $"- Sampling mode: {candidate.SamplingMode}.",
            $"- Advanced preset: {candidate.AdvancedPresetName}.",
            $"- ROI mode: {candidate.RoiMode}.",
            $"- Crop mode: {candidate.CropMode}."
        };

        if (inputType.Equals("segment", StringComparison.OrdinalIgnoreCase))
        {
            lines.Add($"- Segment time: {segment.StartTime:0.00}s to {segment.EndTime:0.00}s.");
            lines.Add($"- OpenCV motion type: {segment.MotionType}; motion score: {segment.AverageMotionScore:0.00}.");
            lines.Add("- Treat the frames as a time sequence from start to end.");
        }

        AddOptionalBlockHints(lines, candidate, blockSettings);
        return string.Join('\n', lines);
    }

    private static string BuildOptionalBlockHints(
        string inputType,
        int frameCount,
        VlmContextCandidate candidate,
        VlmContextBlockSettings blockSettings)
    {
        var lines = new List<string>
        {
            "Context settings:",
            $"- Input type: {inputType}.",
            $"- VLM receives {frameCount} representative frame(s).",
            $"- Sampling mode: {candidate.SamplingMode}.",
            $"- Advanced preset: {candidate.AdvancedPresetName}.",
            $"- ROI mode: {candidate.RoiMode}.",
            $"- Crop mode: {candidate.CropMode}."
        };
        AddOptionalBlockHints(lines, candidate, blockSettings);
        return string.Join('\n', lines);
    }

    private static void AddOptionalBlockHints(
        List<string> lines,
        VlmContextCandidate candidate,
        VlmContextBlockSettings blockSettings)
    {
        if (candidate.YoloHints)
        {
            lines.Add($"- YOLO hints requested. Confidence threshold: {candidate.YoloConfidence:0.00}.");
            if (!string.IsNullOrWhiteSpace(candidate.TargetLabels))
            {
                lines.Add($"- YOLO target labels: {candidate.TargetLabels}.");
            }

            lines.Add($"- YOLO block settings: runtime={blockSettings.YoloRuntime}, device={blockSettings.YoloDevice}, input={blockSettings.YoloInputSize}, iou={blockSettings.YoloIou:0.00}, useBoxesAsRoi={blockSettings.YoloUseBoxesAsRoi}.");
            if (!string.IsNullOrWhiteSpace(blockSettings.YoloModelPath))
            {
                lines.Add($"- YOLO model path configured: {Path.GetFileName(blockSettings.YoloModelPath)}.");
            }

            lines.Add("- No YOLO detector is connected yet; this option is stored for the future detection block.");
        }

        if (candidate.OcrHints)
        {
            lines.Add($"- OCR block settings: engine={blockSettings.OcrEngine}, language={blockSettings.OcrLanguage}, useTextAsHint={blockSettings.OcrUseTextAsHint}.");
            lines.Add("- OCR hints requested. No OCR engine is connected yet; this option is stored for the future OCR block.");
        }

        lines.Add($"- Advanced ROI: mode={blockSettings.RoiMode}, rect=({blockSettings.RoiX},{blockSettings.RoiY},{blockSettings.RoiWidth},{blockSettings.RoiHeight}), padding={blockSettings.RoiPadding:0.00}.");
        lines.Add($"- Advanced Crop: mode={blockSettings.CropMode}, padding={blockSettings.CropPadding:0.00}, keepAspect={blockSettings.CropKeepAspect}, outputLongEdge={blockSettings.CropOutputLongEdge}.");
        lines.Add($"- Advanced Sampling: mode={blockSettings.SamplingMode}, frames={blockSettings.SamplingFrameCount}, timestamps={blockSettings.SamplingIncludeTimestamp}, motionPeakWindow={blockSettings.SamplingMotionPeakWindowSec:0.00}s.");
    }
}
