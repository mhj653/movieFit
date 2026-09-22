using System.Text.Json;
using ProcessVideoAnalyzer.AI.Models;
using ProcessVideoAnalyzer.Models;

namespace ProcessVideoAnalyzer.AI.Providers.Cloud;

internal static class CloudVisionJsonParser
{
    public static CloudVisionResult Parse(string text)
    {
        var json = ExtractJson(text);
        using var document = JsonDocument.Parse(json);
        return ParseSegment(document.RootElement);
    }

    public static IReadOnlyList<CloudVisionResult> ParseBatch(string text)
    {
        var json = ExtractJson(text);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (!root.TryGetProperty("segments", out var segmentsJson) ||
            segmentsJson.ValueKind != JsonValueKind.Array)
        {
            return new[] { ParseSegment(root) };
        }

        return segmentsJson.EnumerateArray().Select(ParseSegment).ToList();
    }

    private static CloudVisionResult ParseSegment(JsonElement item)
    {
        var semantic = GetObject(item, "semantic");
        var observation = GetObject(item, "observation");
        var interaction = GetObject(item, "interaction");
        var repeatability = GetObject(item, "repeatability");
        var quality = GetObject(item, "quality");

        return new CloudVisionResult
        {
            Sequence = GetInt(item, "sequence", "segment", "segment_no", "id"),
            PrimaryAction =
                GetString(semantic, "actionName", "action_name", "primary_action", "primaryAction") ??
                GetString(item, "primary_action", "primaryAction", "action", "actionName") ??
                "확인 필요",
            Description =
                GetString(semantic, "description") ??
                GetString(item, "description") ??
                "",
            Reason = GetString(item, "reason") ?? "",
            ActorType =
                GetString(semantic, "actorType", "actor_type") ??
                GetString(item, "actorType", "actor_type") ??
                "unknown",
            ProcessRole =
                GetString(semantic, "processRole", "process_role") ??
                GetString(item, "processRole", "process_role", "processType", "process_type") ??
                "unknown",
            MotionType =
                GetString(semantic, "motionType", "motion_type") ??
                GetString(item, "motionType", "motion_type") ??
                "unknown",
            StateType =
                GetString(semantic, "stateType", "state_type") ??
                GetString(item, "stateType", "state_type") ??
                "unknown",
            TargetObject =
                GetString(semantic, "targetObject", "target_object") ??
                GetString(item, "targetObject", "target_object") ??
                "",
            ToolOrActor =
                GetString(semantic, "toolOrActor", "tool_or_actor") ??
                GetString(item, "toolOrActor", "tool_or_actor") ??
                "",
            InteractionType =
                GetString(interaction, "interactionType", "interaction_type") ??
                GetString(item, "interactionType", "interaction_type") ??
                "unknown",
            Dependency =
                GetString(interaction, "dependency", "dependencyType", "dependency_type") ??
                GetString(item, "dependency", "dependencyType", "dependency_type") ??
                "unknown",
            WaitReason =
                GetString(interaction, "waitReason", "wait_reason") ??
                GetString(item, "waitReason", "wait_reason") ??
                "",
            RepeatabilityType =
                GetString(repeatability, "type", "repeatabilityType", "repeatability_type") ??
                GetString(item, "repeatabilityType", "repeatability_type") ??
                "unknown",
            PathConsistency =
                GetString(semantic, "pathConsistency", "path_consistency") ??
                GetString(item, "pathConsistency", "path_consistency") ??
                "unknown",
            PositionConsistency =
                GetString(semantic, "positionConsistency", "position_consistency") ??
                GetString(item, "positionConsistency", "position_consistency") ??
                "unknown",
            MotionDirection =
                GetString(observation, "motionDirection", "motion_direction") ??
                GetString(item, "motionDirection", "motion_direction") ??
                "",
            MotionLevel =
                GetString(observation, "motionLevel", "motion_level") ??
                GetString(item, "motionLevel", "motion_level") ??
                "unknown",
            ActorConfidence =
                GetNullableDouble(quality, "actorConfidence", "actor_confidence") ??
                GetNullableDouble(item, "actorConfidence", "actor_confidence"),
            ActionConfidence =
                GetNullableDouble(quality, "actionConfidence", "action_confidence", "confidence") ??
                GetNullableDouble(item, "actionConfidence", "action_confidence", "confidence"),
            Evidence =
                GetStringList(semantic, "evidence") ??
                GetStringList(item, "evidence") ??
                new List<string>(),
            Uncertainties =
                GetStringList(quality, "uncertainties", "uncertainty") ??
                GetStringList(item, "uncertainties", "uncertainty") ??
                new List<string>(),
            Candidates = ReadCandidates(item)
        };
    }

    private static List<ActionCandidate> ReadCandidates(JsonElement root)
    {
        if (!root.TryGetProperty("candidates", out var candidatesJson) ||
            candidatesJson.ValueKind != JsonValueKind.Array)
        {
            return new List<ActionCandidate>();
        }

        var candidates = new List<ActionCandidate>();
        foreach (var item in candidatesJson.EnumerateArray())
        {
            var action = GetString(item, "action", "name", "primary_action", "actionName", "action_name");
            if (string.IsNullOrWhiteSpace(action))
            {
                continue;
            }

            var score = GetDouble(item, "match_score", "matchScore", "score", "confidence");
            candidates.Add(new ActionCandidate
            {
                Action = action,
                MatchScore = NormalizeScore(score)
            });
        }

        return candidates;
    }

    private static string ExtractJson(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNewLine = trimmed.IndexOf('\n');
            var lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
            if (firstNewLine >= 0 && lastFence > firstNewLine)
            {
                trimmed = trimmed[(firstNewLine + 1)..lastFence].Trim();
            }
        }

        var firstBrace = trimmed.IndexOf('{');
        var lastBrace = trimmed.LastIndexOf('}');
        if (firstBrace < 0 || lastBrace <= firstBrace)
        {
            throw new JsonException("AI response did not contain a JSON object.");
        }

        return trimmed[firstBrace..(lastBrace + 1)];
    }

    private static JsonElement GetObject(JsonElement element, string name)
    {
        return element.ValueKind == JsonValueKind.Object &&
               element.TryGetProperty(name, out var value) &&
               value.ValueKind == JsonValueKind.Object
            ? value
            : default;
    }

    private static string? GetString(JsonElement element, params string[] names)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var value))
            {
                if (value.ValueKind == JsonValueKind.String)
                {
                    return value.GetString();
                }

                if (value.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
                {
                    return value.ToString();
                }
            }
        }

        return null;
    }

    private static List<string>? GetStringList(JsonElement element, params string[] names)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

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

            if (value.ValueKind == JsonValueKind.String)
            {
                return value.GetString()?
                    .Split(new[] { "\r\n", "\n", ";" }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                    .ToList() ?? new List<string>();
            }
        }

        return null;
    }

    private static double GetDouble(JsonElement element, params string[] names)
    {
        return GetNullableDouble(element, names) ?? 0;
    }

    private static double? GetNullableDouble(JsonElement element, params string[] names)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (var name in names)
        {
            if (!element.TryGetProperty(name, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number))
            {
                return number;
            }

            if (value.ValueKind == JsonValueKind.String && double.TryParse(value.GetString(), out number))
            {
                return number;
            }
        }

        return null;
    }

    private static int GetInt(JsonElement element, params string[] names)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return 0;
        }

        foreach (var name in names)
        {
            if (!element.TryGetProperty(name, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
            {
                return number;
            }

            if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out number))
            {
                return number;
            }
        }

        return 0;
    }

    private static double NormalizeScore(double value)
    {
        return value <= 1
            ? Math.Round(Math.Clamp(value, 0, 1) * 100, 1)
            : Math.Round(Math.Clamp(value, 0, 100), 1);
    }
}
