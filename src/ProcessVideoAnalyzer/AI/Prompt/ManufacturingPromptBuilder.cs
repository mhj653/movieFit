using ProcessVideoAnalyzer.Models;

namespace ProcessVideoAnalyzer.AI.Prompt;

public sealed class ManufacturingPromptBuilder
{
    public string Build(ProcessSegment segment, int candidateCount = 3, string resultLanguage = "ko")
    {
        var languageInstruction = resultLanguage.Equals("en", StringComparison.OrdinalIgnoreCase)
            ? "Write all user-facing text values in English."
            : "모든 사용자 표시용 텍스트 값은 한국어로 작성하세요.";

        return $$"""
제조 설비 자동화 공정 영상을 분석합니다.
OpenCV가 계산한 시간 구간은 절대 변경하지 말고, 보이는 설비/작업자 동작의 의미만 분류하세요.

Segment {{segment.Sequence}}:
- Start: {{segment.StartTime:0.000}}s
- End: {{segment.EndTime:0.000}}s
- Duration: {{segment.Duration:0.000}}s
- OpenCV motion label: {{segment.MotionType}}
- Average motion score: {{segment.AverageMotionScore:0.00}}

OpenCV label은 보조 힌트입니다. 실제 작업/설비 동작이 보이면 IDLE로 답하지 마세요.
{{languageInstruction}}
응답은 markdown 없이 JSON object 하나만 반환하세요.
Top {{Math.Clamp(candidateCount, 1, 5)}} 후보를 candidates에 포함하세요.

Schema:
{"schemaVersion":"1.2","primary_action":"체결","description":"툴이 제품 접촉 후 체결 동작을 수행하는 것으로 추정됩니다.","reason":"접촉 이후 툴의 위치가 유지되고 미세 움직임이 반복 관찰됩니다.","semantic":{"actorType":"human|machine|mixed|unknown","motionType":"move|hold|contact|release|idle|unknown","processRole":"prepare|load|unload|transfer|position|process|fasten|inspect|wait|return|other|unknown","stateType":"active|idle|waiting|transition|unknown","actionName":"체결","targetObject":"제품","toolOrActor":"전동 드라이버","description":"관찰된 동작 설명","pathConsistency":"consistent|variable|unknown","positionConsistency":"consistent|variable|unknown","evidence":["보이는 근거 1","보이는 근거 2"]},"observation":{"motionDirection":"좌에서 우","motionLevel":"low|medium|high|unknown"},"interaction":{"interactionType":"human_to_machine|machine_to_product|human_to_product|none|unknown","dependency":"previous_step|next_step|independent|waiting|unknown","waitReason":""},"repeatability":{"type":"cyclic|one_time|irregular|unknown"},"quality":{"actorConfidence":0.8,"actionConfidence":0.75,"uncertainties":["불확실한 점"]},"candidates":[{"action":"체결","match_score":78},{"action":"압입","match_score":17},{"action":"위치 고정","match_score":5}]}
""";
    }
}
