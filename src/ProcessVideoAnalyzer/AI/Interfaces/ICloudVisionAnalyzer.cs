using ProcessVideoAnalyzer.AI.Models;

namespace ProcessVideoAnalyzer.AI.Interfaces;

public interface ICloudVisionAnalyzer
{
    string Provider { get; }

    Task<CloudVisionResult> AnalyzeAsync(
        CloudVisionRequest request,
        string apiKey,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CloudVisionResult>> AnalyzeBatchAsync(
        IReadOnlyList<CloudVisionRequest> requests,
        string apiKey,
        CancellationToken cancellationToken = default);

    Task<AiTestResult> TestAsync(
        string apiKey,
        string model,
        CancellationToken cancellationToken = default);
}
