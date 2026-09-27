namespace ProcessVideoAnalyzer.VlmContext;

public static class VlmContextCandidateFactory
{
    public static IReadOnlyList<VlmContextCandidate> CreateDefaultCandidates()
    {
        return CreateImageCandidates();
    }

    public static IReadOnlyList<VlmContextCandidate> CreateImageCandidates()
    {
        return new[]
        {
            new VlmContextCandidate
            {
                Id = "fast",
                Name = "Fast",
                ImageLongEdge = 512,
                FrameCount = 1,
                MotionSummary = false,
                PromptMode = "fast",
                MaxOutputTokens = 80,
                SamplingMode = "middle",
                RoiMode = "fullFrame",
                CropMode = "fullFrame",
                Description = "Fast check. Prioritizes latency over detail."
            },
            new VlmContextCandidate
            {
                Id = "balanced",
                Name = "Balanced",
                ImageLongEdge = 768,
                FrameCount = 1,
                MotionSummary = true,
                PromptMode = "description",
                MaxOutputTokens = 120,
                SamplingMode = "uniform",
                RoiMode = "fullFrame",
                CropMode = "roiContext",
                Description = "Balanced description quality and latency."
            },
            new VlmContextCandidate
            {
                Id = "accurate",
                Name = "Accurate",
                ImageLongEdge = 896,
                FrameCount = 1,
                MotionSummary = true,
                PromptMode = "description",
                MaxOutputTokens = 160,
                SamplingMode = "uniform",
                RoiMode = "fullFrame",
                CropMode = "roiContext",
                Description = "Larger image and longer response for quality."
            }
        };
    }

    public static IReadOnlyList<VlmContextCandidate> CreateSegmentCandidates()
    {
        return new[]
        {
            new VlmContextCandidate
            {
                Id = "fast",
                Name = "Fast",
                ImageLongEdge = 512,
                FrameCount = 1,
                MotionSummary = false,
                PromptMode = "fast",
                MaxOutputTokens = 80,
                SamplingMode = "middle",
                RoiMode = "fullFrame",
                CropMode = "fullFrame",
                Description = "Uses one segment frame for the fastest result."
            },
            new VlmContextCandidate
            {
                Id = "balanced",
                Name = "Balanced",
                ImageLongEdge = 768,
                FrameCount = 3,
                MotionSummary = true,
                PromptMode = "description",
                MaxOutputTokens = 120,
                SamplingMode = "uniform",
                RoiMode = "fullFrame",
                CropMode = "roiContext",
                Description = "Uses start/middle/end style frames when available."
            },
            new VlmContextCandidate
            {
                Id = "accurate",
                Name = "Accurate",
                ImageLongEdge = 896,
                FrameCount = 5,
                MotionSummary = true,
                PromptMode = "description",
                MaxOutputTokens = 160,
                SamplingMode = "uniform",
                RoiMode = "fullFrame",
                CropMode = "roiContext",
                Description = "Uses more segment frames for better temporal context."
            }
        };
    }
}
