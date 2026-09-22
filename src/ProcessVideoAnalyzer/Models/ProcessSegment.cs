using ProcessVideoAnalyzer.Detection;

namespace ProcessVideoAnalyzer.Models;

public sealed class ProcessSegment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int Sequence { get; set; }
    public double StartTime { get; set; }
    public double EndTime { get; set; }
    public double Duration { get; set; }
    public string SchemaVersion { get; set; } = "1.2";
    public string SegmentSource { get; set; } = "opencv";
    public List<string> SourceSegmentIds { get; set; } = new();
    public bool IsMerged { get; set; }
    public string SemanticStatus { get; set; } = "needs_review";
    public string MotionType { get; set; } = "Idle";
    public string ActionCode { get; set; } = "NOT_ANALYZED";
    public string ActionName { get; set; } = "Not Analyzed";
    public double Confidence { get; set; }
    public string Description { get; set; } = "";
    public string Reason { get; set; } = "";
    public string AiReason { get; set; } = "";
    public double AiMatchScore { get; set; }
    public string ActorType { get; set; } = "unknown";
    public string ProcessRole { get; set; } = "unknown";
    public string StateType { get; set; } = "unknown";
    public string TargetObject { get; set; } = "";
    public string ToolOrActor { get; set; } = "";
    public string InteractionType { get; set; } = "unknown";
    public string DependencyType { get; set; } = "unknown";
    public string Dependency { get; set; } = "unknown";
    public string WaitReason { get; set; } = "";
    public string RepeatabilityType { get; set; } = "unknown";
    public string PathConsistency { get; set; } = "unknown";
    public string PositionConsistency { get; set; } = "unknown";
    public string MotionDirection { get; set; } = "";
    public string MotionLevel { get; set; } = "unknown";
    public double? IdleBeforeSec { get; set; }
    public double? IdleAfterSec { get; set; }
    public double? ActorConfidence { get; set; }
    public double? ActionConfidence { get; set; }
    public List<string> Evidence { get; set; } = new();
    public List<string> Uncertainties { get; set; } = new();
    public SegmentTiming? Timing { get; set; }
    public SegmentObservation? Observation { get; set; }
    public SegmentSemantic? Semantic { get; set; }
    public SegmentInteraction? Interaction { get; set; }
    public SegmentRepeatability? Repeatability { get; set; }
    public SegmentQuality? Quality { get; set; }
    public SegmentSourceInfo? Source { get; set; }
    public List<ActionCandidate> Candidates { get; set; } = new();
    public string LastAnalysisProvider { get; set; } = "";
    public string LastAnalysisModel { get; set; } = "";
    public string LastAnalysisInputMode { get; set; } = "";
    public DateTimeOffset? LastAnalysisAt { get; set; }
    public double AverageMotionScore { get; set; }
    public List<string> FramePaths { get; set; } = new();
    public string ThumbnailPath { get; set; } = "";
    public List<string> Alternatives { get; set; } = new();
    public SegmentDetectionContext? Detection { get; set; }
    public SegmentAiTrace? AiTrace { get; set; }
    public bool UserEdited { get; set; }
}

public sealed class SegmentTiming
{
    public double StartSec { get; set; }
    public double EndSec { get; set; }
    public double DurationSec { get; set; }
    public double? IdleBeforeSec { get; set; }
    public double? IdleAfterSec { get; set; }
}

public sealed class SegmentObservation
{
    public bool MotionDetected { get; set; }
    public double MotionDistancePx { get; set; }
    public string MotionDirection { get; set; } = "";
    public string MotionLevel { get; set; } = "unknown";
}

public sealed class SegmentSemantic
{
    public string ActorType { get; set; } = "unknown";
    public string MotionType { get; set; } = "unknown";
    public string ProcessRole { get; set; } = "unknown";
    public string StateType { get; set; } = "unknown";
    public string ActionName { get; set; } = "";
    public string TargetObject { get; set; } = "";
    public string ToolOrActor { get; set; } = "";
    public string Description { get; set; } = "";
    public string PathConsistency { get; set; } = "unknown";
    public string PositionConsistency { get; set; } = "unknown";
    public List<string> Evidence { get; set; } = new();
}

public sealed class SegmentInteraction
{
    public string InteractionType { get; set; } = "unknown";
    public string Dependency { get; set; } = "unknown";
    public string WaitReason { get; set; } = "";
    public bool HumanPresent { get; set; }
    public bool MachinePresent { get; set; }
}

public sealed class SegmentRepeatability
{
    public string Type { get; set; } = "unknown";
}

public sealed class SegmentQuality
{
    public double? Confidence { get; set; }
    public double? ActorConfidence { get; set; }
    public double? ActionConfidence { get; set; }
    public List<string> Uncertainties { get; set; } = new();
}

public sealed class SegmentSourceInfo
{
    public string Timing { get; set; } = "opencv";
    public string Motion { get; set; } = "opencv";
    public string Semantic { get; set; } = "";
    public string ActorType { get; set; } = "";
    public string ProviderId { get; set; } = "";
    public string ModelId { get; set; } = "";
    public string AdapterId { get; set; } = "";
    public string ModelVersion { get; set; } = "";
    public string PromptVersion { get; set; } = "";
}

public sealed class SegmentAiTrace
{
    public string Provider { get; set; } = "";
    public string Model { get; set; } = "";
    public string Adapter { get; set; } = "";
    public string PromptVersion { get; set; } = "";
    public string RequestSchema { get; set; } = "";
    public string Prompt { get; set; } = "";
    public string RequestJson { get; set; } = "";
    public string RawResponse { get; set; } = "";
    public string ParsedResultJson { get; set; } = "";
    public string MappingJson { get; set; } = "";
    public List<string> FramePaths { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
}
