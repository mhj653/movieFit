# Local VLM Endpoint Contract

Process Video Analyzer talks to local VLM implementations through `IVlmAnalyzer`.

The included InternVL and Qwen analyzers use an optional localhost HTTP endpoint so the C# application is not tied to a specific Python runtime.

Configure `endpointUrl` in `Settings/ai_models.json`.

Example:

```json
{
  "id": "internvl35-4b",
  "displayName": "InternVL3.5-4B",
  "provider": "internvl",
  "path": "D:\\AI\\Models\\InternVL3.5-4B",
  "device": "auto",
  "quantization": "auto",
  "endpointUrl": "http://localhost:7860"
}
```

## Health

`GET /health`

Return any 2xx response when the model is available.

## Analyze

`POST /analyze`

The C# app sends:

```json
{
  "modelId": "internvl35-4b",
  "modelPath": "D:\\AI\\Models\\InternVL3.5-4B",
  "provider": "internvl",
  "device": "auto",
  "quantization": "auto",
  "segmentId": "guid",
  "startTime": 1.23,
  "endTime": 2.34,
  "framePaths": ["D:\\...\\frame_01.jpg"],
  "prompt": "...",
  "context": {
    "motionType": "Motion",
    "averageMotionScore": 8.4
  }
}
```

Return valid JSON matching:

```json
{
  "actionCode": "ACTUATOR_FORWARD",
  "actionName": "액추에이터 전진",
  "confidence": 0.82,
  "equipment": "linear actuator",
  "movement": "forward",
  "description": "The tool head moves toward the product.",
  "reason": "The consecutive frames show approach motion.",
  "alternatives": ["TOOL_APPROACH"]
}
```

If this endpoint is not configured or unavailable, the app keeps OpenCV motion results and marks motion segments as `Not Analyzed`.
