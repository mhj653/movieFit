using System.Diagnostics;
using ProcessVideoAnalyzer.Imaging;

namespace ProcessVideoAnalyzer.VlmContext.Steps;

public sealed class VlmContextStepRunner
{
    private readonly SingleImageFramePreparer _framePreparer;
    private readonly Dictionary<string, IVlmContextStep> _steps;

    public VlmContextStepRunner(SingleImageFramePreparer framePreparer)
    {
        _framePreparer = framePreparer;
        var builtIns = new IVlmContextStep[]
        {
            new SamplingStep(),
            new RoiFocusStep(),
            new CropResizeStep(framePreparer),
            new YoloHintsStep(),
            new OcrHintsStep(),
            new PromptContextStep(),
            new NoOpTerminalStep(VlmContextStepTypes.LocalVlmAnalyze),
            new NoOpTerminalStep(VlmContextStepTypes.ParseResult)
        };
        _steps = builtIns.ToDictionary(x => x.Type, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<VlmContextPipelineResult> RunAsync(
        VlmContextPipelineInput input,
        CancellationToken cancellationToken)
    {
        var state = new VlmContextPipelineState
        {
            Candidate = input.Candidate,
            BlockSettings = input.BlockSettings,
            Recipe = input.Recipe,
            InputType = input.InputType
        };
        state.SourceFramePaths.AddRange(input.SourceFramePaths.Where(File.Exists));

        foreach (var step in input.Recipe.Steps.Where(x => x.Enabled))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var watch = Stopwatch.StartNew();
            if (_steps.TryGetValue(step.Type, out var executor))
            {
                await executor.ExecuteAsync(step, input, state, cancellationToken);
            }
            else
            {
                state.ContextHints.Add($"- Unknown step skipped: {step.Type}.");
            }

            watch.Stop();
            state.StepTraces.Add(new VlmContextStepTrace
            {
                StepId = step.Id,
                StepType = step.Type,
                StepName = string.IsNullOrWhiteSpace(step.Name) ? step.Type : step.Name,
                Enabled = step.Enabled,
                LatencyMs = watch.Elapsed.TotalMilliseconds,
                Summary = BuildTraceSummary(state, step)
            });
        }

        if (state.SelectedFramePaths.Count == 0)
        {
            state.SelectedFramePaths.AddRange(state.SourceFramePaths.Take(1));
        }

        if (state.PreparedFramePaths.Count == 0)
        {
            var fallbackResize = new CropResizeStep(_framePreparer);
            await fallbackResize.ExecuteAsync(new VlmContextRecipeStep
            {
                Type = VlmContextStepTypes.CropResize,
                Name = "Fallback Resize",
                Settings = new Dictionary<string, string>
                {
                    ["outputLongEdge"] = input.Recipe.Defaults.ImageLongEdge.ToString()
                }
            }, input, state, cancellationToken);
        }

        return new VlmContextPipelineResult
        {
            SelectedFramePaths = state.SelectedFramePaths.ToList(),
            PreparedFramePaths = state.PreparedFramePaths.ToList(),
            ContextText = string.Join('\n', state.ContextHints),
            StepTraces = state.StepTraces.ToList(),
            Recipe = input.Recipe
        };
    }

    private static string BuildTraceSummary(VlmContextPipelineState state, VlmContextRecipeStep step)
    {
        return step.Type switch
        {
            VlmContextStepTypes.Sampling => $"{state.SelectedFramePaths.Count} selected frame(s)",
            VlmContextStepTypes.CropResize => $"{state.PreparedFramePaths.Count} prepared frame(s)",
            _ => state.ContextHints.LastOrDefault() ?? ""
        };
    }
}
