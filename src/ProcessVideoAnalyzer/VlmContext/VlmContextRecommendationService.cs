namespace ProcessVideoAnalyzer.VlmContext;

public sealed class VlmContextRecommendationService
{
    public VlmContextRecommendation Build(IReadOnlyList<VlmContextExperimentRun> runs)
    {
        var bestRuns = runs
            .Where(x => x.IsBest)
            .OrderByDescending(x => x.CreatedAt)
            .Take(30)
            .ToList();
        if (bestRuns.Count == 0)
        {
            return new VlmContextRecommendation();
        }

        var imageLongEdge = MostCommon(bestRuns.Select(x => x.Candidate.ImageLongEdge));
        var frameCount = MostCommon(bestRuns.Select(x => x.Candidate.FrameCount));
        var promptMode = MostCommon(bestRuns.Select(x => x.Candidate.PromptMode));
        var maxTokens = MostCommon(bestRuns.Select(x => x.Candidate.MaxOutputTokens));
        var motionSummary = MostCommon(bestRuns.Select(x => x.Candidate.MotionSummary));
        var yoloHints = MostCommon(bestRuns.Select(x => x.Candidate.YoloHints));
        var ocrHints = MostCommon(bestRuns.Select(x => x.Candidate.OcrHints));
        var samplingMode = MostCommon(bestRuns.Select(x => x.Candidate.SamplingMode));
        var roiMode = MostCommon(bestRuns.Select(x => x.Candidate.RoiMode));
        var cropMode = MostCommon(bestRuns.Select(x => x.Candidate.CropMode));
        var yoloConfidence = bestRuns
            .Where(x => x.Candidate.YoloHints)
            .Select(x => x.Candidate.YoloConfidence)
            .DefaultIfEmpty(0.45)
            .Average();
        var targetLabels = bestRuns
            .Select(x => x.Candidate.TargetLabels)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .GroupBy(x => x)
            .OrderByDescending(x => x.Count())
            .Select(x => x.Key)
            .FirstOrDefault() ?? "";
        var avgLatency = bestRuns.Average(x => x.LatencyMs);

        var candidate = new VlmContextCandidate
        {
            Id = "recommended",
            Name = "Recommended",
            ImageLongEdge = imageLongEdge,
            FrameCount = frameCount,
            PromptMode = promptMode,
            MaxOutputTokens = maxTokens,
            MotionSummary = motionSummary,
            YoloHints = yoloHints,
            OcrHints = ocrHints,
            SamplingMode = samplingMode,
            RoiMode = roiMode,
            CropMode = cropMode,
            YoloConfidence = yoloConfidence,
            TargetLabels = targetLabels
        };

        var reasons = new List<string>
        {
            $"최근 Best {bestRuns.Count}건 기준 {imageLongEdge}px 이미지가 가장 자주 선택되었습니다.",
            $"{frameCount} frame 설정이 Best에서 가장 많이 선택되었습니다.",
            $"Best 평균 처리 시간은 {(avgLatency / 1000.0):0.00}s 입니다."
        };
        reasons.Add(motionSummary
            ? "Motion Summary On 설정이 Best에서 더 자주 선택되었습니다."
            : "Motion Summary Off 설정이 Best에서 더 자주 선택되었습니다.");
        reasons.Add($"Sampling은 {samplingMode}, ROI는 {roiMode}, Crop은 {cropMode} 조합이 가장 자주 선택되었습니다.");
        reasons.Add(yoloHints
            ? "YOLO Hints On 설정이 Best에서 더 자주 선택되었습니다."
            : "YOLO Hints Off 설정이 Best에서 더 자주 선택되었습니다.");
        reasons.Add(ocrHints
            ? "OCR Hints On 설정이 Best에서 더 자주 선택되었습니다."
            : "OCR Hints Off 설정이 Best에서 더 자주 선택되었습니다.");

        return new VlmContextRecommendation
        {
            HasData = true,
            BestCount = bestRuns.Count,
            Summary = $"{imageLongEdge}px / {frameCount}F / {promptMode} / {maxTokens} tokens",
            RecommendedCandidate = candidate,
            AverageBestLatencyMs = avgLatency,
            Reasons = reasons
        };
    }

    private static T MostCommon<T>(IEnumerable<T> values)
        where T : notnull
    {
        return values
            .GroupBy(x => x)
            .OrderByDescending(x => x.Count())
            .ThenBy(x => x.Key?.ToString())
            .First()
            .Key;
    }
}
