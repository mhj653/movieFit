using ProcessVideoAnalyzer.Imaging;

namespace ProcessVideoAnalyzer.VlmContext.Steps;

public sealed class SamplingStep : IVlmContextStep
{
    public string Type => VlmContextStepTypes.Sampling;

    public Task ExecuteAsync(
        VlmContextRecipeStep step,
        VlmContextPipelineInput input,
        VlmContextPipelineState state,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var count = VlmContextStepSettings.Int(step, "frameCount", input.Recipe.Defaults.FrameCount, 1, 8);
        var mode = VlmContextStepSettings.String(step, "mode", input.Recipe.Defaults.SamplingMode);
        state.SelectedFramePaths.Clear();
        state.SelectedFramePaths.AddRange(SelectFrames(state.SourceFramePaths, count, mode));
        state.ContextHints.Add($"- Sampling step: mode={mode}, selectedFrames={state.SelectedFramePaths.Count}.");
        return Task.CompletedTask;
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
}

public sealed class RoiFocusStep : IVlmContextStep
{
    public string Type => VlmContextStepTypes.RoiFocus;

    public Task ExecuteAsync(
        VlmContextRecipeStep step,
        VlmContextPipelineInput input,
        VlmContextPipelineState state,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var mode = VlmContextStepSettings.String(step, "mode", input.BlockSettings.RoiMode);
        var x = VlmContextStepSettings.Int(step, "x", input.BlockSettings.RoiX, 0, int.MaxValue);
        var y = VlmContextStepSettings.Int(step, "y", input.BlockSettings.RoiY, 0, int.MaxValue);
        var width = VlmContextStepSettings.Int(step, "width", input.BlockSettings.RoiWidth, 0, int.MaxValue);
        var height = VlmContextStepSettings.Int(step, "height", input.BlockSettings.RoiHeight, 0, int.MaxValue);
        var padding = VlmContextStepSettings.Double(step, "padding", input.BlockSettings.RoiPadding, 0, 1);
        state.ContextHints.Add($"- ROI step: mode={mode}, rect=({x},{y},{width},{height}), padding={padding:0.00}.");
        return Task.CompletedTask;
    }
}

public sealed class CropResizeStep : IVlmContextStep
{
    private readonly SingleImageFramePreparer _framePreparer;

    public string Type => VlmContextStepTypes.CropResize;

    public CropResizeStep(SingleImageFramePreparer framePreparer)
    {
        _framePreparer = framePreparer;
    }

    public Task ExecuteAsync(
        VlmContextRecipeStep step,
        VlmContextPipelineInput input,
        VlmContextPipelineState state,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var sourceFrames = state.SelectedFramePaths.Count > 0
            ? state.SelectedFramePaths
            : state.SourceFramePaths;
        var longEdge = VlmContextStepSettings.Int(step, "outputLongEdge", input.Recipe.Defaults.ImageLongEdge, 160, 1280);
        var cropMode = VlmContextStepSettings.String(step, "cropMode", input.BlockSettings.CropMode);

        state.PreparedFramePaths.Clear();
        state.PreparedFramePaths.AddRange(sourceFrames
            .Select(path => _framePreparer.Prepare(path, longEdge))
            .Where(File.Exists)
            .ToList());
        state.ContextHints.Add($"- Crop/Resize step: cropMode={cropMode}, outputLongEdge={longEdge}, preparedFrames={state.PreparedFramePaths.Count}.");
        return Task.CompletedTask;
    }
}

public sealed class YoloHintsStep : IVlmContextStep
{
    public string Type => VlmContextStepTypes.YoloHints;

    public Task ExecuteAsync(
        VlmContextRecipeStep step,
        VlmContextPipelineInput input,
        VlmContextPipelineState state,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var enabled = VlmContextStepSettings.Bool(step, "enabled", input.BlockSettings.YoloEnabled || input.Candidate.YoloHints);
        if (!enabled)
        {
            state.ContextHints.Add("- YOLO step: disabled.");
            return Task.CompletedTask;
        }

        var labels = VlmContextStepSettings.String(step, "labels", input.BlockSettings.YoloLabels);
        var confidence = VlmContextStepSettings.Double(step, "confidence", input.BlockSettings.YoloConfidence, 0.05, 0.95);
        var runtime = VlmContextStepSettings.String(step, "runtime", input.BlockSettings.YoloRuntime);
        var device = VlmContextStepSettings.String(step, "device", input.BlockSettings.YoloDevice);
        state.ContextHints.Add($"- YOLO step: enabled, labels={labels}, confidence={confidence:0.00}, runtime={runtime}, device={device}.");
        state.ContextHints.Add("- YOLO detector is not connected yet; use these labels as future object-detection context.");
        return Task.CompletedTask;
    }
}

public sealed class OcrHintsStep : IVlmContextStep
{
    public string Type => VlmContextStepTypes.OcrHints;

    public Task ExecuteAsync(
        VlmContextRecipeStep step,
        VlmContextPipelineInput input,
        VlmContextPipelineState state,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var enabled = VlmContextStepSettings.Bool(step, "enabled", input.BlockSettings.OcrEnabled || input.Candidate.OcrHints);
        if (!enabled)
        {
            state.ContextHints.Add("- OCR step: disabled.");
            return Task.CompletedTask;
        }

        var engine = VlmContextStepSettings.String(step, "engine", input.BlockSettings.OcrEngine);
        var language = VlmContextStepSettings.String(step, "language", input.BlockSettings.OcrLanguage);
        var useText = VlmContextStepSettings.Bool(step, "useTextAsHint", input.BlockSettings.OcrUseTextAsHint);
        state.ContextHints.Add($"- OCR step: enabled, engine={engine}, language={language}, useTextAsHint={useText}.");
        state.ContextHints.Add("- OCR engine is not connected yet; reserve this step for future text hints.");
        return Task.CompletedTask;
    }
}

public sealed class PromptContextStep : IVlmContextStep
{
    public string Type => VlmContextStepTypes.PromptContext;

    public Task ExecuteAsync(
        VlmContextRecipeStep step,
        VlmContextPipelineInput input,
        VlmContextPipelineState state,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var focus = VlmContextStepSettings.String(step, "focus", "visible manufacturing action");
        var avoidGeneric = VlmContextStepSettings.Bool(step, "avoidGenericState", true);

        state.ContextHints.Insert(0, "Context hints:");
        state.ContextHints.Insert(1, $"- Input type: {input.InputType}.");
        state.ContextHints.Insert(2, $"- Recipe: {input.Recipe.Name}.");
        state.ContextHints.Insert(3, $"- VLM receives {state.PreparedFramePaths.Count} representative frame(s).");
        state.ContextHints.Add($"- Prompt focus: {focus}.");
        if (avoidGeneric)
        {
            state.ContextHints.Add("- Return only the actual visible work description, not a generic machine state.");
        }

        if (input.SourceSegment is not null && input.InputType.Equals("segment", StringComparison.OrdinalIgnoreCase))
        {
            state.ContextHints.Add($"- Segment time: {input.SourceSegment.StartTime:0.00}s to {input.SourceSegment.EndTime:0.00}s.");
            state.ContextHints.Add($"- OpenCV motion type: {input.SourceSegment.MotionType}; motion score: {input.SourceSegment.AverageMotionScore:0.00}.");
            state.ContextHints.Add("- Treat the frames as a time sequence from start to end.");
        }

        return Task.CompletedTask;
    }
}

public sealed class NoOpTerminalStep : IVlmContextStep
{
    public NoOpTerminalStep(string type)
    {
        Type = type;
    }

    public string Type { get; }

    public Task ExecuteAsync(
        VlmContextRecipeStep step,
        VlmContextPipelineInput input,
        VlmContextPipelineState state,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        state.ContextHints.Add($"- Terminal step registered: {step.Name}.");
        return Task.CompletedTask;
    }
}
