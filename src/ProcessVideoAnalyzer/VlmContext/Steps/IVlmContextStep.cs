namespace ProcessVideoAnalyzer.VlmContext.Steps;

public interface IVlmContextStep
{
    string Type { get; }
    Task ExecuteAsync(
        VlmContextRecipeStep step,
        VlmContextPipelineInput input,
        VlmContextPipelineState state,
        CancellationToken cancellationToken);
}
