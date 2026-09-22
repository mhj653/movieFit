# Process Video Analyzer

C#/.NET 8 WPF desktop application for manufacturing process video analysis.

The WPF window is a WebView2 shell. The actual UI is local HTML/CSS/JavaScript without CDN dependencies.

## Folder Structure

```text
ProcessVideoAnalyzer.sln
src/ProcessVideoAnalyzer/
  App.xaml
  MainWindow.xaml
  MainWindow.xaml.cs
  Web/
    index.html
    style.css
    app.js
  Models/
  Video/
  AI/
    Interfaces/
    Models/
    Providers/
      InternVL/
      Qwen/
    Prompt/
  Bridge/
  Project/
  Services/
  Settings/
models/
docs/
```

## NuGet Packages

- Microsoft.Web.WebView2
- OpenCvSharp4
- OpenCvSharp4.runtime.win

`System.Text.Json` is used from .NET 8.

## Run in Visual Studio 2022

1. Install .NET 8 SDK.
2. Install Microsoft Edge WebView2 Runtime.
3. Open `ProcessVideoAnalyzer.sln`.
4. Set platform to `x64`.
5. Restore NuGet packages.
6. Build and run `ProcessVideoAnalyzer`.

For command line verification on a Visual Studio PC, run:

```cmd
build_vs.cmd
```

## Basic Workflow

1. Click `Open Video`.
2. Select an MP4/AVI/MOV/MKV process video.
3. Click `Analyze`.
4. OpenCV samples frames, calculates motion score, detects event boundaries, and extracts representative frames.
5. If a configured local VLM endpoint is available, motion segments are sent to the selected model for semantic analysis.
6. Review the motion graph, timeline, and editable process table.
7. Save the project as `.pvaproj`.

## OpenCV Motion Detection

The motion pipeline samples frames at `SampleIntervalMs`.

Each sampled frame is resized, converted to grayscale, blurred, compared to the previous sampled frame with `absdiff`, thresholded, cleaned with morphology, and converted to:

```text
motion score = changed pixels / total pixels * 100
```

Configured ROI objects use normalized coordinates from 0 to 1. When enabled ROIs exist, motion score is calculated inside those regions.

## Event Segment Creation

OpenCV owns all timing decisions.

The segmentation service uses separate start/stop thresholds and a hold duration to avoid single-frame noise:

- `MotionStartThreshold`
- `MotionStopThreshold`
- `StateHoldMs`
- `MinimumSegmentDuration`

Short segments are merged. Idle segments are labeled `IDLE`. Motion segments remain `NOT_ANALYZED` unless VLM analysis succeeds.

## VLM Flow

All VLM providers implement:

```csharp
IVlmAnalyzer
```

The current providers are:

- `InternVLAnalyzer`
- `QwenVLAnalyzer`

Both use the same local HTTP inference abstraction. The app now includes a local Python VLM server under `VlmServer/` and can start it from AI Settings.

Model options are loaded from:

```text
src/ProcessVideoAnalyzer/Settings/ai_models.json
```

The UI includes:

- InternVL3.5-4B
- InternVL3.5-8B
- Qwen2.5-VL
- model path
- endpoint URL
- Python executable
- auto-start local VLM server
- device
- quantization
- maximum frames per segment
- frame resolution
- confidence threshold

## Model Path Selection

Click `AI Settings`, select the model, then edit `Model Path`.

Default examples:

```text
D:\AI\Models\InternVL3.5-4B
D:\AI\Models\InternVL3.5-8B
D:\AI\Models\Qwen2.5-VL
```

Model weights are intentionally not included in this repository.

## Local VLM Auto Setup

The published app includes:

```text
VlmServer/server.py
VlmServer/requirements.txt
VlmServer/setup_vlm_env.cmd
```

When the app starts, it automatically fills the bundled Python environment path:

```text
VlmServer\.venv\Scripts\python.exe
```

When `Analyze`, `Test Model`, or `Start VLM Server` is clicked, the app automatically creates the `.venv` environment if it does not exist.

On first run this can take a long time because PyTorch, Transformers, and vision dependencies are installed. Python 3.10 or 3.11 must already be installed and reachable through `python`.

For offline PCs, prepare the same virtual environment or install the wheel files from an internal package share. Model folders must already exist locally. Model weights are not downloaded by the app.

After the environment is ready, the app starts:

```text
python VlmServer\server.py --provider internvl|qwen --model-path ...
```

The C# app then sends extracted segment frames to:

```text
http://127.0.0.1:18081/analyze
http://127.0.0.1:18082/analyze
http://127.0.0.1:18083/analyze
```

## GPU or Model Unavailable

GPU is optional for the C# application.

If CUDA, VRAM, model files, or the local inference endpoint are unavailable:

- OpenCV motion analysis still works.
- Timeline and process table are still created.
- Motion segments show `Not Analyzed`.
- Errors are displayed in the UI and written to `Logs/`.

## Offline Deployment

Publish on an internet-connected build PC after NuGet restore:

```powershell
dotnet publish .\src\ProcessVideoAnalyzer\ProcessVideoAnalyzer.csproj -c Release -r win-x64 --self-contained true
```

Copy the publish folder to the offline PC.

Also provide:

- Microsoft Edge WebView2 Runtime, or a fixed-version WebView2 Runtime package.
- Local VLM model folders, copied outside the repository.
- Optional local VLM inference server and its offline Python environment, if semantic AI analysis is needed.

OpenCV motion analysis does not require the VLM server.

## Future OpenAI Vision Adapter

Add the future adapter under:

```text
src/ProcessVideoAnalyzer/AI/Providers/OpenAI/OpenAiVisionAnalyzer.cs
```

It should implement `IVlmAnalyzer`, then `VlmAnalyzerFactory` can map a new provider id such as `openai`.

Timeline, table, project save/load, and WebView2 UI should not need model-specific changes.

## Current Limitations

- ROI data model and backend motion scoring are implemented, but ROI drawing/editing UI is not yet implemented.
- Excel/PPT buttons are present and return `Not implemented`.
- InternVL/Qwen inference requires a compatible local endpoint; model runtime code is intentionally abstracted out.
- Model settings edited in the dialog are applied in memory for the running session. Persist them by editing `ai_models.json`.
