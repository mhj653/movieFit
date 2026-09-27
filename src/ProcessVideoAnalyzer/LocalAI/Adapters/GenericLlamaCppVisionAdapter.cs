using System.Text.Json;
using System.Text.RegularExpressions;
using ProcessVideoAnalyzer.LocalAI.Core;
using ProcessVideoAnalyzer.Models;

namespace ProcessVideoAnalyzer.LocalAI.Adapters;

public class GenericLlamaCppVisionAdapter : IVlmModelAdapter
{
    private const string ActionPartLoading = "\uBD80\uD488 \uD22C\uC785/\uC801\uC7AC";
    private const string ActionAlignment = "\uC704\uCE58 \uC815\uB82C";
    private const string ActionClamping = "\uD074\uB7A8\uD504/\uACE0\uC815";
    private const string ActionPressingFastening = "\uC555\uC785/\uCCB4\uACB0";
    private const string ActionTransfer = "\uC774\uC1A1/\uBC18\uC1A1";
    private const string ActionMachineOperation = "\uC124\uBE44 \uB3D9\uC791";
    private const string ActionInspection = "\uAC80\uC0AC/\uD655\uC778";
    private const string ActionOperatorHandling = "\uC791\uC5C5\uC790 \uCDE8\uAE09";
    private const string ActionWaiting = "\uB300\uAE30/\uC720\uC9C0";
    private const string ExamplePressDescription = "\uC124\uBE44 \uD5E4\uB4DC\uAC00 \uC81C\uD488 \uCABD\uC73C\uB85C \uD558\uAC15\uD574 \uC555\uC785 \uB610\uB294 \uCCB4\uACB0 \uB3D9\uC791\uC744 \uC218\uD589\uD569\uB2C8\uB2E4.";

    public virtual string AdapterId => "generic-llamacpp-vision";

    public virtual bool CanHandle(VlmModelProfile profile)
    {
        return string.Equals(profile.Adapter, AdapterId, StringComparison.OrdinalIgnoreCase);
    }

    public virtual VlmPreparedRequest Prepare(VlmRequest request, VlmModelProfile profile)
    {
        var language = request.OutputLanguage.Equals("en", StringComparison.OrdinalIgnoreCase)
            ? "English"
            : "Korean";
        if (request.PromptMode.StartsWith("singleImage:fast", StringComparison.OrdinalIgnoreCase))
        {
            var fastDetectionFacts = string.IsNullOrWhiteSpace(request.DetectionFacts)
                ? ""
                : $"\n{request.DetectionFacts}\nUse these detector labels as hints, not final truth.";
            var fastPrompt = $$"""
Return one short valid JSON object only. No markdown.
Analyze this single manufacturing image. Use {{language}} for display text.
Describe the actual visible work in taskDescription. Keep it under 60 Korean characters.
{{fastDetectionFacts}}
Required keys:
selectedAction, actionName, taskDescription, actorType, processType, stateType, targetObject, toolOrMachine, confidence, candidates.
Allowed selectedAction values: part_loading, alignment, clamping, pressing_fastening, transfer, machine_operation, inspection, operator_handling, waiting.
Example:
{"selectedAction":"machine_operation","actionName":"{{ActionMachineOperation}}","taskDescription":"설비 헤드가 제품 위에서 작업 위치를 유지합니다.","actorType":"machine","processType":"{{ActionMachineOperation}}","stateType":"operating","targetObject":"제품","toolOrMachine":"설비 헤드","confidence":0.55,"candidates":[{"action":"{{ActionMachineOperation}}","matchScore":55},{"action":"{{ActionPressingFastening}}","matchScore":30},{"action":"{{ActionWaiting}}","matchScore":15}]}
""";

            return new VlmPreparedRequest
            {
                Source = request,
                Prompt = fastPrompt,
                ImagePaths = request.FramePaths.Where(File.Exists).Take(1).ToList()
            };
        }

        if (request.PromptMode.Equals("contextDescription", StringComparison.OrdinalIgnoreCase))
        {
            var contextFacts = string.IsNullOrWhiteSpace(request.DetectionFacts)
                ? ""
                : $"\nContext hints:\n{request.DetectionFacts}\nUse these hints only to focus attention. Trust the image when there is conflict.";
            var contextPrompt = $$"""
Return one valid JSON object only. No markdown.
Use {{language}}.
Look at this manufacturing image or ordered frame sequence and describe the actual visible work.
Focus on what the worker, machine, tool, or part is doing.
Do not answer with only a generic state such as "machine is operating" or "no human intervention".
If the action is uncertain, describe only what is visibly happening.
Keep description as one concise Korean sentence.
{{contextFacts}}

Required JSON:
{"description":"작업자가 수행 중인 실제 작업을 한 문장으로 설명","confidence":0.0}
""";

            return new VlmPreparedRequest
            {
                Source = request,
                Prompt = contextPrompt,
                ImagePaths = request.FramePaths.Where(File.Exists).ToList()
            };
        }

        var detectionFacts = string.IsNullOrWhiteSpace(request.DetectionFacts)
            ? ""
            : $"\n{request.DetectionFacts}\nUse these detector labels as visual hints only. If they conflict with the image, trust the image.";
        var prompt = $$"""
Return one short valid JSON object only. Do not use markdown, code fences, XML, or extra text.
Analyze what manufacturing work is being performed in these representative frames.
Use visual evidence first. OpenCV timing/motion is only a hint.
Choose the closest action candidate even when uncertain, then lower confidence.
All display text fields must be in {{language}}.
{{detectionFacts}}

Important rule for taskDescription:
- Describe the actual visible work: "who/what performs what work on what object".
- Do not write only status/actor summaries such as "machine is operating" or "no human intervention".
- If the exact process name is uncertain, describe the visible operation and motion.
- Keep taskDescription under 80 Korean characters.

Allowed action candidates:
1. {{ActionPartLoading}} (part_loading)
2. {{ActionAlignment}} (alignment)
3. {{ActionClamping}} (clamping)
4. {{ActionPressingFastening}} (pressing_fastening)
5. {{ActionTransfer}} (transfer)
6. {{ActionMachineOperation}} (machine_operation)
7. {{ActionInspection}} (inspection)
8. {{ActionOperatorHandling}} (operator_handling)
9. {{ActionWaiting}} (waiting)

Decision hints:
- human handling object/tool -> operator_handling
- machine head, press, driver, conveyor, robot, or actuator moving -> machine_operation or the closest specific operation
- object enters fixture/table -> part_loading
- object/tool position is adjusted -> alignment
- clamp/fixture holds the part -> clamping
- vertical contact/force/driver/press motion -> pressing_fastening
- object changes location -> transfer
- camera/check/no visible contact but active station -> inspection or waiting

Segment:
id={{request.Segment.Id}}
start={{request.Segment.StartTime:0.000}}s
end={{request.Segment.EndTime:0.000}}s
duration={{request.Segment.Duration:0.000}}s
opencvMotion={{request.Segment.MotionType}}
motionScore={{request.Segment.AverageMotionScore:0.00}}

Required JSON keys:
selectedAction, actionName, operationGuess, taskDescription, actorType, processType, stateType, targetObject, toolOrMachine, observedMotion, automationMeaning, confidence, candidates.

Example:
{"selectedAction":"pressing_fastening","actionName":"{{ActionPressingFastening}}","operationGuess":"{{ActionPressingFastening}}","taskDescription":"{{ExamplePressDescription}}","actorType":"machine","processType":"{{ActionPressingFastening}}","stateType":"operating","targetObject":"","toolOrMachine":"","observedMotion":"vertical press/contact motion","automationMeaning":"repeatable machine work candidate","confidence":0.55,"candidates":[{"action":"{{ActionPressingFastening}}","matchScore":55},{"action":"{{ActionMachineOperation}}","matchScore":30},{"action":"{{ActionTransfer}}","matchScore":15}]}
""";

        return new VlmPreparedRequest
        {
            Source = request,
            Prompt = prompt,
            ImagePaths = request.FramePaths.Where(File.Exists).ToList()
        };
    }

    public virtual VlmResult Parse(VlmRawResponse response, VlmModelProfile profile)
    {
        var json = TryExtractJson(response.Text);
        if (json is null)
        {
            return ParseTextFallback(response);
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            return ParseTextFallback(response);
        }

        using (document)
        {
        var root = document.RootElement;
        var selectedActionCode = GetString(root, "selectedAction", "selected_action", "actionCode", "action_code");
        var selectedActionLabel = MapActionCode(selectedActionCode);
        var operationGuess = GetString(root, "operationGuess", "operation_guess", "processName", "process_name");
        var taskDescription = GetString(root, "taskDescription", "task_description", "operationDescription", "operation_description", "description");
        var toolOrMachine = GetString(root, "toolOrMachine", "tool_or_machine", "toolOrActor", "tool_or_actor");
        var observedMotion = GetString(root, "observedMotion", "observed_motion", "motionType", "motion_type");
        var automationMeaning = GetString(root, "automationMeaning", "automation_meaning");
        var actionName = selectedActionLabel
                         ?? GetString(root, "actionName", "action_name", "primary_action")
                         ?? operationGuess
                         ?? "unknown";

        var result = new VlmResult
        {
            ActorType = GetString(root, "actorType", "actor_type") ?? "unknown",
            ProcessType = operationGuess ?? GetString(root, "processType", "process_type", "processRole", "process_role") ?? "unknown",
            MotionType = observedMotion ?? "unknown",
            StateType = GetString(root, "stateType", "state_type") ?? "unknown",
            ActionName = actionName,
            Description = taskDescription ?? "",
            TargetObject = GetString(root, "targetObject", "target_object") ?? "",
            ToolOrActor = toolOrMachine ?? "",
            InteractionType = GetString(root, "interactionType", "interaction_type") ?? "unknown",
            Dependency = GetString(root, "dependency") ?? "unknown",
            WaitReason = GetString(root, "waitReason", "wait_reason") ?? "",
            RepeatabilityType = GetString(root, "repeatabilityType", "repeatability_type") ?? "unknown",
            PathConsistency = GetString(root, "pathConsistency", "path_consistency") ?? "unknown",
            PositionConsistency = GetString(root, "positionConsistency", "position_consistency") ?? "unknown",
            Confidence = NormalizeRatio(GetDouble(root, "confidence", "matchScore", "match_score")),
            ActorConfidence = NormalizeNullableRatio(GetNullableDouble(root, "actorConfidence", "actor_confidence")),
            ActionConfidence = NormalizeNullableRatio(GetNullableDouble(root, "actionConfidence", "action_confidence")),
            Evidence = GetStringList(root, "evidence"),
            Uncertainties = GetStringList(root, "uncertainties", "uncertainty"),
            OutputTokens = response.OutputTokens
        };
        if (!string.IsNullOrWhiteSpace(observedMotion))
        {
            result.Evidence.Add(observedMotion);
        }

        if (!string.IsNullOrWhiteSpace(automationMeaning))
        {
            result.Evidence.Add(automationMeaning);
        }

        result.Candidates = GetCandidates(root);
        if (result.Candidates.Count == 0)
        {
            result.Candidates.Add(new ActionCandidate { Action = result.ActionName, MatchScore = Math.Round(result.Confidence * 100, 1) });
        }

        return result;
        }
    }

    private static string? TryExtractJson(string text)
    {
        var first = text.IndexOf('{');
        var last = text.LastIndexOf('}');
        if (first < 0 || last <= first)
        {
            return null;
        }

        return text[first..(last + 1)];
    }

    private static VlmResult ParseTextFallback(VlmRawResponse response)
    {
        var text = CleanTextResponse(
            GetJsonStringField(response.Text, "taskDescription") ??
            GetJsonStringField(response.Text, "task_description") ??
            GetJsonStringField(response.Text, "operationDescription") ??
            GetJsonStringField(response.Text, "description") ??
            response.Text);
        var selectedActionLabel = MapActionCode(GetJsonStringField(response.Text, "selectedAction"));
        var operationGuess = GetJsonStringField(response.Text, "operationGuess") ??
                             GetJsonStringField(response.Text, "operation_guess") ??
                             GetJsonStringField(response.Text, "processName");
        var actionName = selectedActionLabel ??
                         GetJsonStringField(response.Text, "actionName") ??
                         GetJsonStringField(response.Text, "action_name") ??
                         operationGuess ??
                         "unknown";
        var observedMotion = GetJsonStringField(response.Text, "observedMotion") ??
                             GetJsonStringField(response.Text, "observed_motion");
        var automationMeaning = GetJsonStringField(response.Text, "automationMeaning") ??
                                GetJsonStringField(response.Text, "automation_meaning");
        var confidence = GetJsonNumberField(response.Text, "confidence") ?? 0;
        return new VlmResult
        {
            ActorType = GetJsonStringField(response.Text, "actorType") ?? "unknown",
            ProcessType = operationGuess ?? selectedActionLabel ?? GetJsonStringField(response.Text, "processType") ?? "unknown",
            MotionType = observedMotion ?? "unknown",
            StateType = GetJsonStringField(response.Text, "stateType") ?? "unknown",
            ActionName = actionName,
            Description = text,
            TargetObject = GetJsonStringField(response.Text, "targetObject") ?? "",
            ToolOrActor = GetJsonStringField(response.Text, "toolOrMachine") ??
                          GetJsonStringField(response.Text, "toolOrActor") ??
                          "",
            InteractionType = "unknown",
            Dependency = "unknown",
            RepeatabilityType = "unknown",
            PathConsistency = "unknown",
            PositionConsistency = "unknown",
            Confidence = confidence > 0 ? NormalizeRatio(confidence) : string.IsNullOrWhiteSpace(text) ? 0 : 0.35,
            Evidence = new[] { text, observedMotion, automationMeaning }.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!).ToList(),
            Uncertainties = new List<string> { "Local VLM returned free text instead of JSON." },
            OutputTokens = response.OutputTokens
        };
    }

    private static string? GetJsonStringField(string text, string fieldName)
    {
        var match = Regex.Match(
            text,
            $"\"{Regex.Escape(fieldName)}\"\\s*:\\s*\"(?<value>(?:\\\\.|[^\"])*)\"",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!match.Success)
        {
            return null;
        }

        var raw = match.Groups["value"].Value;
        try
        {
            return JsonSerializer.Deserialize<string>($"\"{raw}\"");
        }
        catch
        {
            return raw.Replace("\\\"", "\"").Trim();
        }
    }

    private static double? GetJsonNumberField(string text, string fieldName)
    {
        var match = Regex.Match(
            text,
            $"\"{Regex.Escape(fieldName)}\"\\s*:\\s*(?<value>-?\\d+(?:\\.\\d+)?)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return match.Success && double.TryParse(match.Groups["value"].Value, out var value) ? value : null;
    }

    private static string CleanTextResponse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "";
        }

        var cleaned = text
            .Replace("```json", "", StringComparison.OrdinalIgnoreCase)
            .Replace("```", "", StringComparison.OrdinalIgnoreCase)
            .Trim();

        return cleaned.Length <= 300 ? cleaned : cleaned[..300].Trim();
    }

    private static string? GetString(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
            {
                return value.GetString();
            }
        }

        return null;
    }

    private static List<string> GetStringList(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (!element.TryGetProperty(name, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.Array)
            {
                return value.EnumerateArray()
                    .Select(x => x.ValueKind == JsonValueKind.String ? x.GetString() : x.ToString())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x!)
                    .ToList();
            }
        }

        return new List<string>();
    }

    private static List<ActionCandidate> GetCandidates(JsonElement element)
    {
        if (!element.TryGetProperty("candidates", out var value) ||
            value.ValueKind != JsonValueKind.Array)
        {
            return new List<ActionCandidate>();
        }

        return value.EnumerateArray()
            .Select(item =>
            {
                var actionCode = GetString(item, "selectedAction", "selected_action", "actionCode", "action_code");
                var action = MapActionCode(actionCode) ?? GetString(item, "action", "actionName", "action_name") ?? "";
                var score = GetNullableDouble(item, "matchScore", "match_score", "confidence") ?? 0;
                return new ActionCandidate
                {
                    Action = action,
                    MatchScore = Math.Round(score <= 1 ? score * 100 : score, 1)
                };
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.Action))
            .OrderByDescending(x => x.MatchScore)
            .Take(5)
            .ToList();
    }

    private static string? MapActionCode(string? code)
    {
        return code?.Trim().ToLowerInvariant() switch
        {
            "part_loading" => ActionPartLoading,
            "alignment" => ActionAlignment,
            "clamping" => ActionClamping,
            "pressing_fastening" => ActionPressingFastening,
            "transfer" => ActionTransfer,
            "machine_operation" => ActionMachineOperation,
            "inspection" => ActionInspection,
            "operator_handling" => ActionOperatorHandling,
            "waiting" => ActionWaiting,
            _ => null
        };
    }
    private static double GetDouble(JsonElement element, params string[] names)
    {
        return GetNullableDouble(element, names) ?? 0;
    }

    private static double? GetNullableDouble(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var value) &&
                value.ValueKind == JsonValueKind.Number &&
                value.TryGetDouble(out var number))
            {
                return number;
            }
        }

        return null;
    }

    private static double NormalizeRatio(double value)
    {
        return value > 1 ? Math.Clamp(value / 100.0, 0, 1) : Math.Clamp(value, 0, 1);
    }

    private static double? NormalizeNullableRatio(double? value)
    {
        return value is null ? null : NormalizeRatio(value.Value);
    }
}
