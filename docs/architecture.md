# Process Video Analyzer Architecture

## Current module split

- `Video/`
  - OpenCV metadata, motion sampling, event segmentation, representative frame extraction, and the main analysis pipeline.
- `LocalAI/`
  - Local VLM settings, model profiles, llama.cpp provider, and model adapters.
- `Imaging/`
  - Single image frame preparation for Snapshot / Load Image analysis.
- `Detection/`
  - Future object detection extension point for YOLO or other local detectors.
- `Capture/`
  - Webcam preview, snapshot, recording, and imported image handling.
- `Trigger/`
  - Trigger abstraction. Manual trigger exists now; PLC socket trigger can implement the same interface later.
- `Services/`
  - History, logging, and local video serving.
- `Web/`
  - WebView UI shell.

## YOLO extension point

The app now has a detector-neutral interface:

- `IObjectDetector`
- `ObjectDetectionPipeline`
- `ObjectDetectionOptions`
- `SegmentDetectionContext`

The current detector is `NoOpObjectDetector`, so behavior is unchanged.

To add YOLO later:

1. Add a class such as `YoloOnnxObjectDetector : IObjectDetector`.
2. Load an ONNX model in that detector.
3. Return `FrameDetectionResult` with detected labels, confidence, and bounding boxes.
4. Register the detector in `VideoAnalysisPipeline` construction.
5. Enable `AnalysisSettings.ObjectDetection.Enabled`.

When enabled, detection facts are attached to `ProcessSegment.Detection` and inserted into the Local VLM prompt as visual hints. They are also available in AI Trace through the VLM request JSON.

## Single image analysis path

Snapshot and Load Image analysis now use:

1. Original image for display/history.
2. `SingleImageFramePreparer` to create a resized VLM input JPG.
3. `AnalysisSettings.SingleImageLongEdge`.
4. `AnalysisSettings.SingleImageMaxOutputTokens`.
5. `AnalysisSettings.SingleImagePromptMode`.
6. Local VLM prompt mode `singleImage:fast` or `singleImage:balanced`.

This keeps UI display quality separate from VLM input cost.
