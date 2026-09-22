namespace ProcessVideoAnalyzer.LocalAI.Core;

public interface ILocalVlmProvider
{
    string ProviderId { get; }
    Task InitializeAsync(VlmModelProfile model, CancellationToken cancellationToken);
    Task WarmupAsync(CancellationToken cancellationToken);
    Task<VlmRawResponse> AnalyzeAsync(VlmPreparedRequest request, CancellationToken cancellationToken);
    Task<VlmHealthStatus> GetStatusAsync();
    Task UnloadAsync();
}
