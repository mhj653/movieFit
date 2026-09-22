using ProcessVideoAnalyzer.Models;

namespace ProcessVideoAnalyzer.AI.Models;

public sealed class CloudVisionResult
{
    public int Sequence { get; set; }
    public string PrimaryAction { get; set; } = "확인 필요";
    public string Description { get; set; } = "";
    public string Reason { get; set; } = "";
    public string ActorType { get; set; } = "unknown";
    public string ProcessRole { get; set; } = "unknown";
    public string MotionType { get; set; } = "unknown";
    public string StateType { get; set; } = "unknown";
    public string TargetObject { get; set; } = "";
    public string ToolOrActor { get; set; } = "";
    public string InteractionType { get; set; } = "unknown";
    public string Dependency { get; set; } = "unknown";
    public string WaitReason { get; set; } = "";
    public string RepeatabilityType { get; set; } = "unknown";
    public string PathConsistency { get; set; } = "unknown";
    public string PositionConsistency { get; set; } = "unknown";
    public string MotionDirection { get; set; } = "";
    public string MotionLevel { get; set; } = "unknown";
    public double? ActorConfidence { get; set; }
    public double? ActionConfidence { get; set; }
    public List<string> Evidence { get; set; } = new();
    public List<string> Uncertainties { get; set; } = new();
    public List<ActionCandidate> Candidates { get; set; } = new();
}
