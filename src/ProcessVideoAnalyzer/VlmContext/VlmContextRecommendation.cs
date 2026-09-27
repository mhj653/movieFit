namespace ProcessVideoAnalyzer.VlmContext;

public sealed class VlmContextRecommendation
{
    public bool HasData { get; init; }
    public int BestCount { get; init; }
    public string Summary { get; init; } = "Best 라벨을 선택하면 추천 가이드가 생성됩니다.";
    public VlmContextCandidate? RecommendedCandidate { get; init; }
    public double AverageBestLatencyMs { get; init; }
    public IReadOnlyList<string> Reasons { get; init; } = Array.Empty<string>();
}
