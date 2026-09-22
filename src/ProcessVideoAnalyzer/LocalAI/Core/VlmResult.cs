using ProcessVideoAnalyzer.Models;

namespace ProcessVideoAnalyzer.LocalAI.Core;

public sealed class VlmResult
{
    public string ActorType { get; set; } = "unknown";
    public string ProcessType { get; set; } = "unknown";
    public string MotionType { get; set; } = "unknown";
    public string StateType { get; set; } = "unknown";
    public string ActionName { get; set; } = "확인 필요";
    public string Description { get; set; } = "";
    public string TargetObject { get; set; } = "";
    public string ToolOrActor { get; set; } = "";
    public string InteractionType { get; set; } = "unknown";
    public string Dependency { get; set; } = "unknown";
    public string WaitReason { get; set; } = "";
    public string RepeatabilityType { get; set; } = "unknown";
    public string PathConsistency { get; set; } = "unknown";
    public string PositionConsistency { get; set; } = "unknown";
    public double Confidence { get; set; }
    public double? ActorConfidence { get; set; }
    public double? ActionConfidence { get; set; }
    public List<string> Evidence { get; set; } = new();
    public List<string> Uncertainties { get; set; } = new();
    public List<ActionCandidate> Candidates { get; set; } = new();
    public int? OutputTokens { get; set; }
    public string TracePrompt { get; set; } = "";
    public string TraceRequestJson { get; set; } = "";
    public string TraceRawResponse { get; set; } = "";
    public List<string> TraceFramePaths { get; set; } = new();
}
