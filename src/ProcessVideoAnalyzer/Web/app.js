const state = {
  metadata: null,
  videoUrl: "",
  segments: [],
  samples: [],
  selectedId: null,
  selectedSegmentIds: new Set(),
  activeId: null,
  total: 1,
  scale: "auto",
  dragging: false,
  segmentPlaybackEnd: null,
  apiStatus: "Ready",
  history: [],
  currentHistoryId: null,
  settings: {
    defaultProvider: "local_vlm",
    inputMode: "opencvSegments",
    candidateCount: 3,
    resultLanguage: "ko",
    activeModelId: "qwen3-vl-4b-q4km",
    activeModelName: "Qwen3-VL 4B Instruct",
    runtimeProvider: "llama.cpp",
    device: "gpu",
    maxFrames: 2,
    imageLongEdge: 320,
    cropMode: "roiContext",
    outputMode: "balanced",
    maxOutputTokens: 320,
    singleImageLongEdge: 256,
    singleImageMaxOutputTokens: 160,
    singleImagePromptMode: "fast",
    temperature: 0.1,
    structuredOutput: true,
    warmupOnStart: false
  },
  vlmStatus: { state: "not_ready", message: "Model path is not configured." },
  vlmModels: [],
  performanceMetrics: null,
  analysisStartedAtMs: null,
  mediaType: "video",
  imageUrl: "",
  context: {
    imageUrl: "",
    imageName: "",
    source: "image",
    segmentId: "",
    candidates: defaultContextCandidates("image"),
    blockSettings: defaultAdvancedBlockSettings(),
    advancedPresets: [],
    activeAdvancedPresetId: "none",
    advancedTestSource: "advancedImage",
    advancedTestImageUrl: "",
    advancedRoiDragging: false,
    advancedRoiStart: null,
    presetPickerCandidateIndex: null,
    running: false,
    runs: [],
    history: [],
    recommendation: null,
    traceRun: null,
    traceTab: "prompt"
  }
};

const el = {
  loadBtn: document.getElementById("loadBtn"),
  analyzeBtn: document.getElementById("analyzeBtn"),
  apiSettingsBtn: document.getElementById("apiSettingsBtn"),
  performanceBtn: document.getElementById("performanceBtn"),
  perfSummary: document.getElementById("perfSummary"),
  exportBtn: document.getElementById("exportBtn"),
  mainVideo: document.getElementById("mainVideo"),
  mainImage: document.getElementById("mainImage"),
  mainMediaTitle: document.getElementById("mainMediaTitle"),
  videoName: document.getElementById("videoName"),
  videoDuration: document.getElementById("videoDuration"),
  processCount: document.getElementById("processCount"),
  cycleTime: document.getElementById("cycleTime"),
  aiProvider: document.getElementById("aiProvider"),
  inputMode: document.getElementById("inputMode"),
  resultLanguageSummary: document.getElementById("resultLanguageSummary"),
  apiStatus: document.getElementById("apiStatus"),
  progressStage: document.getElementById("progressStage"),
  progressPercent: document.getElementById("progressPercent"),
  progressBar: document.getElementById("progressBar"),
  progressMessage: document.getElementById("progressMessage"),
  rows: document.getElementById("rows"),
  axis: document.getElementById("axis"),
  playhead: document.getElementById("playhead"),
  playheadTime: document.getElementById("playheadTime"),
  seekSurface: document.getElementById("seekSurface"),
  timeInfo: document.getElementById("timeInfo"),
  actorBadge: document.getElementById("actorBadge"),
  actorType: document.getElementById("actorType"),
  processType: document.getElementById("processType"),
  motionType: document.getElementById("motionType"),
  stateType: document.getElementById("stateType"),
  actionName: document.getElementById("actionName"),
  score: document.getElementById("score"),
  description: document.getElementById("description"),
  targetObject: document.getElementById("targetObject"),
  toolOrActor: document.getElementById("toolOrActor"),
  interactionType: document.getElementById("interactionType"),
  dependencyType: document.getElementById("dependencyType"),
  waitReason: document.getElementById("waitReason"),
  repeatabilityType: document.getElementById("repeatabilityType"),
  pathConsistency: document.getElementById("pathConsistency"),
  positionConsistency: document.getElementById("positionConsistency"),
  motionDirection: document.getElementById("motionDirection"),
  motionLevel: document.getElementById("motionLevel"),
  idleBefore: document.getElementById("idleBefore"),
  idleAfter: document.getElementById("idleAfter"),
  actorConfidence: document.getElementById("actorConfidence"),
  actionConfidence: document.getElementById("actionConfidence"),
  evidence: document.getElementById("evidence"),
  uncertainty: document.getElementById("uncertainty"),
  semanticSource: document.getElementById("semanticSource"),
  schemaVersion: document.getElementById("schemaVersion"),
  candidates: document.getElementById("candidates"),
  lastAnalysis: document.getElementById("lastAnalysis"),
  aiTraceBtn: document.getElementById("aiTraceBtn"),
  reanalyzeBtn: document.getElementById("reanalyzeBtn"),
  saveEditBtn: document.getElementById("saveEditBtn"),
  apiDialog: document.getElementById("apiDialog"),
  modelManagerDialog: document.getElementById("modelManagerDialog"),
  reanalyzeDialog: document.getElementById("reanalyzeDialog"),
  aiTraceDialog: document.getElementById("aiTraceDialog"),
  traceTitle: document.getElementById("traceTitle"),
  traceVideo: document.getElementById("traceVideo"),
  traceImage: document.getElementById("traceImage"),
  traceFrames: document.getElementById("traceFrames"),
  traceText: document.getElementById("traceText"),
  performanceDrawer: document.getElementById("performanceDrawer"),
  perfBackdrop: document.getElementById("perfBackdrop"),
  toast: document.getElementById("toast"),
  historyList: document.getElementById("historyList"),
  refreshHistoryBtn: document.getElementById("refreshHistoryBtn"),
  mergeSegmentsBtn: document.getElementById("mergeSegmentsBtn"),
  splitSegmentBtn: document.getElementById("splitSegmentBtn"),
  restoreSegmentBtn: document.getElementById("restoreSegmentBtn"),
  undoSegmentBtn: document.getElementById("undoSegmentBtn"),
  redoSegmentBtn: document.getElementById("redoSegmentBtn"),
  capturePreview: document.getElementById("capturePreview"),
  startPreviewBtn: document.getElementById("startPreviewBtn"),
  stopPreviewBtn: document.getElementById("stopPreviewBtn"),
  recordBtn: document.getElementById("recordBtn"),
  stopRecordBtn: document.getElementById("stopRecordBtn"),
  snapshotBtn: document.getElementById("snapshotBtn"),
  loadImageBtn: document.getElementById("loadImageBtn"),
  cameraIndex: document.getElementById("cameraIndex"),
  capturePreviewState: document.getElementById("capturePreviewState"),
  captureRecordingState: document.getElementById("captureRecordingState"),
  captureStatusText: document.getElementById("captureStatusText"),
  captureRecordingPath: document.getElementById("captureRecordingPath"),
  openVideoTabBtn: document.getElementById("openVideoTabBtn"),
  tabVideo: document.getElementById("tabVideo"),
  tabAdvanced: document.getElementById("tabAdvanced"),
  contextImage: document.getElementById("contextImage"),
  contextImageName: document.getElementById("contextImageName"),
  contextLoadImageBtn: document.getElementById("contextLoadImageBtn"),
  contextUseSegmentBtn: document.getElementById("contextUseSegmentBtn"),
  contextRunCompareBtn: document.getElementById("contextRunCompareBtn"),
  contextStatus: document.getElementById("contextStatus"),
  contextCandidateSetup: document.getElementById("contextCandidateSetup"),
  contextRows: document.getElementById("contextRows"),
  contextRecommendation: document.getElementById("contextRecommendation"),
  contextHistory: document.getElementById("contextHistory"),
  contextApplyBestBtn: document.getElementById("contextApplyBestBtn"),
  contextTraceDialog: document.getElementById("contextTraceDialog"),
  contextTraceTitle: document.getElementById("contextTraceTitle"),
  contextTraceFrames: document.getElementById("contextTraceFrames"),
  contextTraceText: document.getElementById("contextTraceText"),
  advancedPresetPickerDialog: document.getElementById("advancedPresetPickerDialog"),
  presetPickerTitle: document.getElementById("presetPickerTitle"),
  presetPickerSelect: document.getElementById("presetPickerSelect"),
  presetPickerSummary: document.getElementById("presetPickerSummary"),
  editPresetInAdvancedBtn: document.getElementById("editPresetInAdvancedBtn"),
  applyPresetToCandidateBtn: document.getElementById("applyPresetToCandidateBtn"),
  advPresetSelect: document.getElementById("advPresetSelect"),
  advPresetName: document.getElementById("advPresetName"),
  advPresetNewBtn: document.getElementById("advPresetNewBtn"),
  advPresetDuplicateBtn: document.getElementById("advPresetDuplicateBtn"),
  advPresetSaveBtn: document.getElementById("advPresetSaveBtn"),
  advPresetDeleteBtn: document.getElementById("advPresetDeleteBtn"),
  advancedRoiStage: document.getElementById("advancedRoiStage"),
  advancedRoiImage: document.getElementById("advancedRoiImage"),
  advancedRoiBox: document.getElementById("advancedRoiBox"),
  advancedLoadTestImageBtn: document.getElementById("advancedLoadTestImageBtn"),
  advancedUseContextImageBtn: document.getElementById("advancedUseContextImageBtn"),
  advancedUseSelectedSegmentBtn: document.getElementById("advancedUseSelectedSegmentBtn"),
  advancedRunBlockTestBtn: document.getElementById("advancedRunBlockTestBtn"),
  advancedSourceImage: document.getElementById("advancedSourceImage"),
  advancedProcessedImage: document.getElementById("advancedProcessedImage"),
  advancedTestStatus: document.getElementById("advancedTestStatus"),
  advancedTestHints: document.getElementById("advancedTestHints"),
  advancedApplyToCandidatesBtn: document.getElementById("advancedApplyToCandidatesBtn"),
  advancedResetBtn: document.getElementById("advancedResetBtn")
};

wireUi();
wireHost();
renderAxis();
renderRows();
renderInspector();
renderHistory();
post("getAnalysisHistory");
post("getLocalAiSettings");
post("getVlmStatus");
post("getCaptureStatus");
post("getVlmContextExperiments");
post("getAdvancedBlockPresets");
setInterval(() => post("getCaptureStatus"), 750);

function wireUi() {
  el.loadBtn.addEventListener("click", () => post("openVideo"));
  el.analyzeBtn.addEventListener("click", () => {
    state.analysisStartedAtMs = performance.now();
    post("analyzeVideo", collectAnalysisDefaults());
  });
  el.apiSettingsBtn.addEventListener("click", () => {
    post("getLocalAiSettings");
    post("getVlmStatus");
    fillSettingsDialog();
    el.apiDialog.showModal();
  });
  el.exportBtn.addEventListener("click", () => post("exportResult"));
  el.aiTraceBtn?.addEventListener("click", openAiTraceDialog);
  el.reanalyzeBtn.addEventListener("click", openReanalyzeDialog);
  el.saveEditBtn.addEventListener("click", saveEdit);
  el.refreshHistoryBtn.addEventListener("click", () => post("getAnalysisHistory"));
  el.mergeSegmentsBtn?.addEventListener("click", mergeSelectedSegments);
  el.splitSegmentBtn?.addEventListener("click", splitSelectedSegmentAtPlayhead);
  el.restoreSegmentBtn?.addEventListener("click", restoreSelectedSegment);
  el.undoSegmentBtn?.addEventListener("click", () => post("undoSegmentEdit"));
  el.redoSegmentBtn?.addEventListener("click", () => post("redoSegmentEdit"));
  el.startPreviewBtn?.addEventListener("click", () => {
    post("startCapturePreview", { cameraIndex: cameraIndex() });
  });
  el.stopPreviewBtn?.addEventListener("click", () => post("stopCapturePreview"));
  el.recordBtn?.addEventListener("click", () => {
    state.analysisStartedAtMs = performance.now();
    post("startCaptureRecording", { ...collectAnalysisDefaults(), cameraIndex: cameraIndex() });
  });
  el.stopRecordBtn?.addEventListener("click", () => {
    state.analysisStartedAtMs = performance.now();
    post("stopCaptureRecording", { ...collectAnalysisDefaults(), cameraIndex: cameraIndex() });
  });
  el.snapshotBtn?.addEventListener("click", () => {
    state.analysisStartedAtMs = performance.now();
    post("captureSnapshot", { ...collectAnalysisDefaults(), cameraIndex: cameraIndex() });
  });
  el.loadImageBtn?.addEventListener("click", () => {
    state.analysisStartedAtMs = performance.now();
    post("loadImageForAnalysis", collectAnalysisDefaults());
  });
  el.openVideoTabBtn?.addEventListener("click", () => {
    if (el.tabVideo) {
      el.tabVideo.checked = true;
    }
  });
  el.contextLoadImageBtn?.addEventListener("click", () => post("loadVlmContextImage"));
  el.contextUseSegmentBtn?.addEventListener("click", useSelectedSegmentForContext);
  el.contextRunCompareBtn?.addEventListener("click", () => {
    state.context.running = true;
    renderContextLab();
    post("runVlmContextCompare", {
      ...collectAnalysisDefaults(),
      source: state.context.source || "image",
      segmentId: state.context.segmentId || "",
      candidates: collectContextCandidates(),
      blockSettings: collectAdvancedBlockSettings()
    });
  });
  el.contextApplyBestBtn?.addEventListener("click", () => {
    const run = currentContextBestRun();
    if (!run?.id) {
      showToast("Select Best first");
      return;
    }

    post("applyVlmContextBestToDefaults", { runId: run.id });
    showToast("Applying Best settings to Analysis defaults");
  });

  document.querySelectorAll(".scale-btn").forEach((button) => {
    button.addEventListener("click", () => {
      state.scale = button.dataset.scale;
      document.querySelectorAll(".scale-btn").forEach((item) => item.classList.toggle("selected", item === button));
      renderAxis();
      renderRows();
      updatePlaybackUI();
    });
  });

  ["timeupdate", "seeking", "seeked", "play", "pause", "ended", "loadedmetadata"].forEach((eventName) => {
    el.mainVideo.addEventListener(eventName, updatePlaybackUI);
  });
  requestAnimationFrame(smoothLoop);

  el.seekSurface.addEventListener("pointerdown", (event) => {
    state.dragging = true;
    state.segmentPlaybackEnd = null;
    el.seekSurface.setPointerCapture(event.pointerId);
    seekTo(pointerTime(event));
  });
  el.seekSurface.addEventListener("pointermove", (event) => {
    if (state.dragging) {
      seekTo(pointerTime(event));
    }
  });
  el.seekSurface.addEventListener("pointerup", (event) => {
    state.dragging = false;
    try {
      el.seekSurface.releasePointerCapture(event.pointerId);
    } catch {}
  });

  document.getElementById("saveApiBtn").addEventListener("click", saveApiSettings);
  document.getElementById("testModelBtn")?.addEventListener("click", () => post("testVlmModel"));
  document.getElementById("vlmModel")?.addEventListener("change", syncVlmPathFields);
  document.getElementById("manageModelsBtn")?.addEventListener("click", () => {
    post("getVlmModels");
    el.modelManagerDialog?.showModal();
  });
  document.getElementById("runReanalyzeBtn").addEventListener("click", runReanalyze);
  document.getElementById("reanalyzeProvider").addEventListener("change", () => syncReanalyzeModelOptions());
  document.querySelectorAll(".trace-tab").forEach((button) => {
    button.addEventListener("click", () => setTraceTab(button.dataset.traceTab || "schema"));
  });
  document.querySelectorAll(".context-trace-tab").forEach((button) => {
    button.addEventListener("click", () => setContextTraceTab(button.dataset.contextTraceTab || "prompt"));
  });
  el.advancedApplyToCandidatesBtn?.addEventListener("click", () => {
    applyAdvancedToCandidates();
    if (!event.silent) {
      renderContextCandidateSetup();
    }
    showToast("Advanced block settings applied to Compare Setup");
  });
  el.advancedResetBtn?.addEventListener("click", () => {
    state.context.blockSettings = defaultAdvancedBlockSettings();
    fillAdvancedBlockSettings();
    renderAdvancedPresetSelect();
    showToast("Advanced block settings reset");
  });
  el.advPresetSelect?.addEventListener("change", () => selectAdvancedPreset(el.advPresetSelect.value));
  el.advPresetNewBtn?.addEventListener("click", newAdvancedPreset);
  el.advPresetDuplicateBtn?.addEventListener("click", duplicateAdvancedPreset);
  el.advPresetSaveBtn?.addEventListener("click", saveAdvancedPreset);
  el.advPresetDeleteBtn?.addEventListener("click", deleteAdvancedPreset);
  el.advancedLoadTestImageBtn?.addEventListener("click", () => post("loadAdvancedBlockTestImage"));
  el.advancedUseContextImageBtn?.addEventListener("click", () => setAdvancedTestSource("contextImage"));
  el.advancedUseSelectedSegmentBtn?.addEventListener("click", () => setAdvancedTestSource("selectedSegment"));
  el.advancedRunBlockTestBtn?.addEventListener("click", runAdvancedBlockTest);
  el.advancedRoiImage?.addEventListener("load", renderAdvancedRoiBox);
  el.presetPickerSelect?.addEventListener("change", renderAdvancedPresetPickerSummary);
  el.applyPresetToCandidateBtn?.addEventListener("click", applyPickerPresetToCandidate);
  el.editPresetInAdvancedBtn?.addEventListener("click", editPickerPresetInAdvanced);
  wireAdvancedRoiPicker();
  fillAdvancedBlockSettings();
}

function wireHost() {
  if (!window.chrome?.webview) {
    return;
  }

  window.chrome.webview.addEventListener("message", (event) => {
    const message = event.data ?? {};
    const data = message.data ?? {};
    switch (message.type) {
      case "videoLoaded":
        onVideoLoaded(data);
        break;
      case "imageLoaded":
        onImageLoaded(data);
        break;
      case "analysisStarted":
        state.analysisStartedAtMs ??= performance.now();
        state.segments = [];
        state.samples = [];
        state.performanceMetrics = null;
        state.selectedId = null;
        state.selectedSegmentIds.clear();
        state.currentHistoryId = null;
        state.segmentPlaybackEnd = null;
        setApiStatus("Running");
        setProgress("Preparing Video", 5, "");
        renderPerformanceMetrics(null);
        renderRows();
        renderInspector();
        break;
      case "analysisProgress":
        setProgress(data.stage, data.percent, data.message);
        break;
      case "motionData":
        state.samples = data.samples ?? [];
        break;
      case "segmentsUpdated":
        state.segments = (data.segments ?? []).map(normalizeSegment);
        if (!state.selectedId && state.segments.length > 0) {
          state.selectedId = state.segments[0].id;
        }
        renderSummary(data.summary);
        renderAxis();
        renderRows();
        renderInspector();
        updateSegmentEditButtons();
        break;
      case "analysisComplete":
        if (data.performance && state.analysisStartedAtMs) {
          const userWaitMs = performance.now() - state.analysisStartedAtMs;
          data.performance.backendTotalMs = data.performance.totalMs;
          data.performance.userWaitMs = userWaitMs;
          data.performance.totalMs = userWaitMs;
          state.analysisStartedAtMs = null;
        }
        renderSummary(data);
        if (data.performance) {
          state.performanceMetrics = data.performance;
          renderPerformanceMetrics(data.performance);
        }
        setProgress("Complete", 100, "Analysis complete");
        if (state.apiStatus !== "Quota exceeded") {
          setApiStatus("Ready");
        }
        break;
      case "apiSettingsStatus":
      case "localAiSettings":
        state.settings = { ...state.settings, ...(data.settings ?? {}) };
        state.vlmStatus = data.status ?? state.vlmStatus;
        state.vlmModels = data.models ?? state.vlmModels;
        fillSettingsDialog();
        renderVlmStatus();
        break;
      case "vlmStatus":
        state.vlmStatus = data;
        renderVlmStatus();
        break;
      case "vlmModels":
        state.vlmModels = data.models ?? [];
        break;
      case "vlmModelTestResult":
        state.vlmStatus = data.status ?? state.vlmStatus;
        renderVlmStatus();
        showToast(data.message || data.status?.message || "Model test finished");
        break;
      case "performanceMetrics":
        state.performanceMetrics = data;
        renderPerformanceMetrics(data);
        break;
      case "apiLimitExceeded":
        setApiStatus("Quota exceeded");
        setProgress("API Limit Exceeded", 100, data.message || "API quota or rate limit exceeded");
        showToast(data.message || "API quota or rate limit exceeded");
        break;
      case "analysisHistoryUpdated":
        state.history = data.items ?? [];
        state.currentHistoryId = data.currentHistoryId ?? state.currentHistoryId;
        renderHistory();
        break;
      case "analysisHistoryLoaded":
        onHistoryLoaded(data);
        break;
      case "captureStatus":
        renderCaptureStatus(data);
        break;
      case "vlmContextImageLoaded":
        state.context.imageUrl = data.imageUrl || "";
        state.context.imageName = data.fileName || "Loaded image";
        state.context.source = "image";
        state.context.segmentId = "";
        state.context.candidates = defaultContextCandidates("image");
        state.context.runs = [];
        state.context.running = false;
        renderContextLab();
        break;
      case "vlmContextCompareStarted":
        state.context.running = true;
        state.context.runs = [];
        if (el.contextStatus) el.contextStatus.textContent = "Auto compare running...";
        renderContextLab();
        break;
      case "vlmContextCompareProgress":
        if (el.contextStatus) el.contextStatus.textContent = data.message || "Running...";
        break;
      case "vlmContextCompareComplete":
        state.context.running = false;
        state.context.runs = data.runs ?? [];
        state.context.recommendation = data.recommendation ?? state.context.recommendation;
        renderContextLab();
        showToast("VLM Context compare complete");
        break;
      case "vlmContextExperimentsUpdated":
        state.context.history = data.runs ?? [];
        state.context.recommendation = data.recommendation ?? null;
        reconcileContextBestFlags();
        renderContextLab();
        break;
      case "advancedBlockPresetsUpdated":
        state.context.advancedPresets = data.presets ?? [];
        if (!state.context.advancedPresets.length) {
          state.context.advancedPresets = [{ id: "none", name: "None", settings: defaultAdvancedBlockSettings() }];
        }
        if (!state.context.advancedPresets.some((preset) => preset.id === state.context.activeAdvancedPresetId)) {
          state.context.activeAdvancedPresetId = state.context.advancedPresets[0]?.id || "none";
        }
        selectAdvancedPreset(state.context.activeAdvancedPresetId, true);
        renderAdvancedPresetSelect();
        renderContextCandidateSetup();
        break;
      case "advancedBlockTestImageLoaded":
        state.context.advancedTestSource = data.source || "advancedImage";
        state.context.advancedTestImageUrl = data.imageUrl || "";
        setAdvancedPreviewImage(data.imageUrl || "");
        if (el.advancedTestStatus) el.advancedTestStatus.textContent = `${data.fileName || "Image"} loaded for block test.`;
        break;
      case "advancedBlockTestComplete":
        setAdvancedPreviewImage(data.sourceUrl || state.context.advancedTestImageUrl || "");
        if (el.advancedProcessedImage) el.advancedProcessedImage.src = data.processedUrl ? `${data.processedUrl}?t=${Date.now()}` : "";
        if (el.advancedTestHints) el.advancedTestHints.textContent = data.hints || "No hints.";
        if (el.advancedTestStatus) {
          el.advancedTestStatus.textContent = `Processed ${data.sourceWidth || "-"}x${data.sourceHeight || "-"} -> ${data.outputWidth || "-"}x${data.outputHeight || "-"} · ${formatMs(data.latencyMs)}`;
        }
        break;
      case "error":
        showToast(data.message || "Error");
        break;
    }
  });
}

function useSelectedSegmentForContext() {
  const segment = selectedSegment();
  if (!segment) {
    showToast("Select a segment in Analysis first");
    return;
  }

  state.context.source = "selectedSegment";
  state.context.segmentId = segment.id;
  state.context.candidates = defaultContextCandidates("segment");
  state.context.imageUrl = segment.thumbnailUrl || segment.frameUrls?.[0] || "";
  state.context.imageName = `Segment ${segment.sequence ?? "-"} · ${fmt(segment.startTime)}-${fmt(segment.endTime)}s`;
  state.context.runs = [];
  renderContextLab();
}

function defaultContextCandidates(kind = "image") {
  const segmentMode = kind === "segment";
  return [
    {
      id: "fast",
      name: "Fast",
      enabled: true,
      advancedPresetId: "none",
      advancedPresetName: "None",
      blockSettings: null,
      imageLongEdge: 512,
      frameCount: 1,
      samplingMode: "middle",
      motionSummary: false,
      yoloHints: false,
      ocrHints: false,
      roiMode: "fullFrame",
      cropMode: "fullFrame",
      yoloConfidence: 0.45,
      targetLabels: "worker,hand,part,tool,machine",
      promptMode: "fast",
      maxOutputTokens: 80,
      description: segmentMode ? "One segment frame for quickest feedback." : "One image, shortest prompt."
    },
    {
      id: "balanced",
      name: "Balanced",
      enabled: true,
      advancedPresetId: "none",
      advancedPresetName: "None",
      blockSettings: null,
      imageLongEdge: 768,
      frameCount: segmentMode ? 3 : 1,
      samplingMode: "uniform",
      motionSummary: true,
      yoloHints: false,
      ocrHints: false,
      roiMode: "fullFrame",
      cropMode: "roiContext",
      yoloConfidence: 0.45,
      targetLabels: "worker,hand,part,fixture,button,tool,machine",
      promptMode: "description",
      maxOutputTokens: 120,
      description: segmentMode ? "Time sequence with motion hints." : "Balanced detail with context hints."
    },
    {
      id: "accurate",
      name: "Accurate",
      enabled: true,
      advancedPresetId: "none",
      advancedPresetName: "None",
      blockSettings: null,
      imageLongEdge: 896,
      frameCount: segmentMode ? 5 : 1,
      samplingMode: "uniform",
      motionSummary: true,
      yoloHints: false,
      ocrHints: false,
      roiMode: "fullFrame",
      cropMode: "roiContext",
      yoloConfidence: 0.45,
      targetLabels: "worker,hand,part,fixture,button,tool,machine,text,label",
      promptMode: "description",
      maxOutputTokens: 160,
      description: segmentMode ? "More frames for temporal context." : "Larger image and longer output."
    }
  ];
}

function defaultAdvancedBlockSettings() {
  return {
    yoloEnabled: false,
    yoloModelPath: "",
    yoloRuntime: "onnxruntime",
    yoloDevice: "cpu",
    yoloInputSize: 640,
    yoloConfidence: 0.45,
    yoloIou: 0.5,
    yoloLabels: "worker,hand,part,fixture,button,tool,machine",
    yoloUseBoxesAsRoi: false,
    yoloDrawOverlay: false,
    roiMode: "fullFrame",
    roiX: 0,
    roiY: 0,
    roiWidth: 0,
    roiHeight: 0,
    roiPadding: 0.12,
    cropMode: "fullFrame",
    cropPadding: 0.12,
    cropKeepAspect: true,
    cropOutputLongEdge: 768,
    ocrEnabled: false,
    ocrEngine: "future",
    ocrLanguage: "ko",
    ocrUseTextAsHint: true,
    samplingMode: "uniform",
    samplingFrameCount: 3,
    samplingIncludeTimestamp: true,
    samplingMotionPeakWindowSec: 0.25
  };
}

function fillAdvancedBlockSettings() {
  const s = state.context.blockSettings ?? defaultAdvancedBlockSettings();
  setChecked("advYoloEnabled", s.yoloEnabled);
  setInputValue("advYoloModelPath", s.yoloModelPath);
  setSelectValue("advYoloRuntime", s.yoloRuntime);
  setSelectValue("advYoloDevice", s.yoloDevice);
  setSelectValue("advYoloInputSize", String(s.yoloInputSize));
  setInputValue("advYoloConfidence", s.yoloConfidence);
  setInputValue("advYoloIou", s.yoloIou);
  setInputValue("advYoloLabels", s.yoloLabels);
  setChecked("advYoloUseBoxesAsRoi", s.yoloUseBoxesAsRoi);
  setChecked("advYoloDrawOverlay", s.yoloDrawOverlay);
  setSelectValue("advRoiMode", s.roiMode);
  setInputValue("advRoiX", s.roiX);
  setInputValue("advRoiY", s.roiY);
  setInputValue("advRoiWidth", s.roiWidth);
  setInputValue("advRoiHeight", s.roiHeight);
  setInputValue("advRoiPadding", s.roiPadding);
  setSelectValue("advCropMode", s.cropMode);
  setInputValue("advCropPadding", s.cropPadding);
  setChecked("advCropKeepAspect", s.cropKeepAspect);
  setSelectValue("advCropOutputLongEdge", String(s.cropOutputLongEdge));
  setChecked("advOcrEnabled", s.ocrEnabled);
  setSelectValue("advOcrEngine", s.ocrEngine);
  setSelectValue("advOcrLanguage", s.ocrLanguage);
  setChecked("advOcrUseTextAsHint", s.ocrUseTextAsHint);
  setSelectValue("advSamplingMode", s.samplingMode);
  setSelectValue("advSamplingFrameCount", String(s.samplingFrameCount));
  setChecked("advSamplingIncludeTimestamp", s.samplingIncludeTimestamp);
  setInputValue("advSamplingMotionPeakWindowSec", s.samplingMotionPeakWindowSec);
  renderAdvancedRoiBox();
}

function collectAdvancedBlockSettings() {
  const settings = {
    yoloEnabled: readChecked("advYoloEnabled"),
    yoloModelPath: readValueSetting("advYoloModelPath", ""),
    yoloRuntime: readValueSetting("advYoloRuntime", "onnxruntime"),
    yoloDevice: readValueSetting("advYoloDevice", "cpu"),
    yoloInputSize: readNumberSetting("advYoloInputSize", 640),
    yoloConfidence: readNumberSetting("advYoloConfidence", 0.45),
    yoloIou: readNumberSetting("advYoloIou", 0.5),
    yoloLabels: readValueSetting("advYoloLabels", "worker,hand,part,fixture,button,tool,machine"),
    yoloUseBoxesAsRoi: readChecked("advYoloUseBoxesAsRoi"),
    yoloDrawOverlay: readChecked("advYoloDrawOverlay"),
    roiMode: readValueSetting("advRoiMode", "fullFrame"),
    roiX: readNumberSetting("advRoiX", 0),
    roiY: readNumberSetting("advRoiY", 0),
    roiWidth: readNumberSetting("advRoiWidth", 0),
    roiHeight: readNumberSetting("advRoiHeight", 0),
    roiPadding: readNumberSetting("advRoiPadding", 0.12),
    cropMode: readValueSetting("advCropMode", "fullFrame"),
    cropPadding: readNumberSetting("advCropPadding", 0.12),
    cropKeepAspect: readChecked("advCropKeepAspect"),
    cropOutputLongEdge: readNumberSetting("advCropOutputLongEdge", 768),
    ocrEnabled: readChecked("advOcrEnabled"),
    ocrEngine: readValueSetting("advOcrEngine", "future"),
    ocrLanguage: readValueSetting("advOcrLanguage", "ko"),
    ocrUseTextAsHint: readChecked("advOcrUseTextAsHint"),
    samplingMode: readValueSetting("advSamplingMode", "uniform"),
    samplingFrameCount: readNumberSetting("advSamplingFrameCount", 3),
    samplingIncludeTimestamp: readChecked("advSamplingIncludeTimestamp"),
    samplingMotionPeakWindowSec: readNumberSetting("advSamplingMotionPeakWindowSec", 0.25)
  };
  state.context.blockSettings = settings;
  return settings;
}

function renderAdvancedPresetSelect() {
  if (!el.advPresetSelect) return;
  const presets = state.context.advancedPresets?.length
    ? state.context.advancedPresets
    : [{ id: "none", name: "None", settings: defaultAdvancedBlockSettings() }];
  el.advPresetSelect.innerHTML = presets.map((preset) =>
    `<option value="${escapeHtml(preset.id)}" ${preset.id === state.context.activeAdvancedPresetId ? "selected" : ""}>${escapeHtml(preset.name || preset.id)}</option>`
  ).join("");
  if (el.advPresetName) {
    const active = presets.find((preset) => preset.id === state.context.activeAdvancedPresetId) || presets[0];
    el.advPresetName.value = active?.name || "None";
  }
}

function selectAdvancedPreset(id, fill = true) {
  const presets = state.context.advancedPresets ?? [];
  const preset = presets.find((item) => item.id === id) || presets[0];
  if (!preset) return;
  state.context.activeAdvancedPresetId = preset.id;
  state.context.blockSettings = { ...defaultAdvancedBlockSettings(), ...(preset.settings ?? {}) };
  if (fill) fillAdvancedBlockSettings();
  renderAdvancedPresetSelect();
}

function newAdvancedPreset() {
  state.context.activeAdvancedPresetId = `preset_${Date.now()}`;
  state.context.blockSettings = defaultAdvancedBlockSettings();
  if (el.advPresetName) el.advPresetName.value = "New Preset";
  if (el.advPresetSelect) el.advPresetSelect.value = state.context.activeAdvancedPresetId;
  fillAdvancedBlockSettings();
}

function duplicateAdvancedPreset() {
  const current = currentAdvancedPreset();
  state.context.activeAdvancedPresetId = `preset_${Date.now()}`;
  state.context.blockSettings = { ...defaultAdvancedBlockSettings(), ...(current?.settings ?? collectAdvancedBlockSettings()) };
  if (el.advPresetName) el.advPresetName.value = `${current?.name || "Preset"} Copy`;
  fillAdvancedBlockSettings();
}

function saveAdvancedPreset() {
  const name = (el.advPresetName?.value || "New Preset").trim() || "New Preset";
  const id = state.context.activeAdvancedPresetId && state.context.activeAdvancedPresetId !== "none" && !state.context.activeAdvancedPresetId.startsWith("preset_")
    ? state.context.activeAdvancedPresetId
    : makePresetId(name);
  state.context.activeAdvancedPresetId = id;
  post("saveAdvancedBlockPreset", {
    preset: {
      id,
      name,
      settings: collectAdvancedBlockSettings()
    }
  });
  showToast("Advanced Block preset saved");
}

function deleteAdvancedPreset() {
  const id = state.context.activeAdvancedPresetId;
  if (!id || id === "none") {
    showToast("None preset cannot be deleted");
    return;
  }

  post("deleteAdvancedBlockPreset", { id });
  state.context.activeAdvancedPresetId = "none";
  showToast("Advanced Block preset deleted");
}

function currentAdvancedPreset() {
  return (state.context.advancedPresets ?? []).find((preset) => preset.id === state.context.activeAdvancedPresetId) || null;
}

function makePresetId(name) {
  const slug = String(name || "preset")
    .toLowerCase()
    .replace(/[^a-z0-9가-힣]+/g, "-")
    .replace(/^-+|-+$/g, "") || "preset";
  return `${slug}-${Date.now()}`;
}

function applyAdvancedToCandidates() {
  const settings = collectAdvancedBlockSettings();
  const preset = currentAdvancedPreset();
  state.context.candidates = (state.context.candidates ?? defaultContextCandidates("image")).map((candidate) => ({
    ...candidate,
    advancedPresetId: preset?.id || state.context.activeAdvancedPresetId || "none",
    advancedPresetName: preset?.name || el.advPresetName?.value || "Current",
    blockSettings: settings,
    yoloHints: settings.yoloEnabled,
    ocrHints: settings.ocrEnabled,
    yoloConfidence: settings.yoloConfidence,
    targetLabels: settings.yoloLabels,
    roiMode: settings.roiMode,
    cropMode: settings.cropMode,
    samplingMode: settings.samplingMode,
    frameCount: settings.samplingFrameCount
  }));
}

function setAdvancedTestSource(source) {
  state.context.advancedTestSource = source;
  if (source === "contextImage") {
    if (!state.context.imageUrl) {
      showToast("Load a VLM Context image first");
      return;
    }

    setAdvancedPreviewImage(state.context.imageUrl);
    if (el.advancedTestStatus) el.advancedTestStatus.textContent = "Using VLM Context image for block test.";
    return;
  }

  if (source === "selectedSegment") {
    const segment = selectedSegment();
    if (!segment) {
      showToast("Select a segment in Analysis first");
      return;
    }

    setAdvancedPreviewImage(segment.thumbnailUrl || segment.frameUrls?.[0] || "");
    if (el.advancedTestStatus) el.advancedTestStatus.textContent = `Using Segment ${segment.sequence ?? "-"} for block test.`;
  }
}

function runAdvancedBlockTest() {
  const source = state.context.advancedTestSource || "advancedImage";
  const payload = {
    source,
    segmentId: state.selectedId || "",
    blockSettings: collectAdvancedBlockSettings()
  };
  if (el.advancedTestStatus) el.advancedTestStatus.textContent = "Running block test...";
  post("runAdvancedBlockTest", payload);
}

function setAdvancedPreviewImage(url) {
  const src = url ? `${url}?t=${Date.now()}` : "";
  if (el.advancedRoiImage) el.advancedRoiImage.src = src;
  if (el.advancedSourceImage) el.advancedSourceImage.src = src;
  renderAdvancedRoiBox();
}

function wireAdvancedRoiPicker() {
  if (!el.advancedRoiStage || !el.advancedRoiImage) return;
  el.advancedRoiStage.addEventListener("pointerdown", (event) => {
    const point = advancedImagePoint(event);
    if (!point) return;
    state.context.advancedRoiDragging = true;
    state.context.advancedRoiStart = point;
    el.advancedRoiStage.setPointerCapture(event.pointerId);
    updateAdvancedRoiFromPoints(point, point);
  });
  el.advancedRoiStage.addEventListener("pointermove", (event) => {
    if (!state.context.advancedRoiDragging || !state.context.advancedRoiStart) return;
    const point = advancedImagePoint(event);
    if (!point) return;
    updateAdvancedRoiFromPoints(state.context.advancedRoiStart, point);
  });
  el.advancedRoiStage.addEventListener("pointerup", (event) => {
    state.context.advancedRoiDragging = false;
    state.context.advancedRoiStart = null;
    try {
      el.advancedRoiStage.releasePointerCapture(event.pointerId);
    } catch {}
    setSelectValue("advRoiMode", "manualRoi");
    collectAdvancedBlockSettings();
  });
  ["advRoiX", "advRoiY", "advRoiWidth", "advRoiHeight"].forEach((id) => {
    document.getElementById(id)?.addEventListener("input", renderAdvancedRoiBox);
  });
}

function advancedRenderedImageRect() {
  const img = el.advancedRoiImage;
  if (!img || !img.naturalWidth || !img.naturalHeight) return null;
  const rect = img.getBoundingClientRect();
  const naturalRatio = img.naturalWidth / img.naturalHeight;
  const boxRatio = rect.width / rect.height;
  let width;
  let height;
  let left;
  let top;
  if (boxRatio > naturalRatio) {
    height = rect.height;
    width = height * naturalRatio;
    left = rect.left + (rect.width - width) / 2;
    top = rect.top;
  } else {
    width = rect.width;
    height = width / naturalRatio;
    left = rect.left;
    top = rect.top + (rect.height - height) / 2;
  }
  return { left, top, width, height, naturalWidth: img.naturalWidth, naturalHeight: img.naturalHeight };
}

function advancedImagePoint(event) {
  const rect = advancedRenderedImageRect();
  if (!rect) return null;
  const x = Math.max(0, Math.min(rect.naturalWidth, ((event.clientX - rect.left) / rect.width) * rect.naturalWidth));
  const y = Math.max(0, Math.min(rect.naturalHeight, ((event.clientY - rect.top) / rect.height) * rect.naturalHeight));
  return { x: Math.round(x), y: Math.round(y) };
}

function updateAdvancedRoiFromPoints(a, b) {
  const x = Math.min(a.x, b.x);
  const y = Math.min(a.y, b.y);
  const width = Math.abs(a.x - b.x);
  const height = Math.abs(a.y - b.y);
  setInputValue("advRoiX", x);
  setInputValue("advRoiY", y);
  setInputValue("advRoiWidth", width);
  setInputValue("advRoiHeight", height);
  renderAdvancedRoiBox();
}

function renderAdvancedRoiBox() {
  if (!el.advancedRoiBox) return;
  const rect = advancedRenderedImageRect();
  const x = readNumberSetting("advRoiX", 0);
  const y = readNumberSetting("advRoiY", 0);
  const width = readNumberSetting("advRoiWidth", 0);
  const height = readNumberSetting("advRoiHeight", 0);
  if (!rect || width <= 0 || height <= 0) {
    el.advancedRoiBox.style.display = "none";
    return;
  }

  const stageRect = el.advancedRoiStage.getBoundingClientRect();
  el.advancedRoiBox.style.display = "block";
  el.advancedRoiBox.style.left = `${rect.left - stageRect.left + (x / rect.naturalWidth) * rect.width}px`;
  el.advancedRoiBox.style.top = `${rect.top - stageRect.top + (y / rect.naturalHeight) * rect.height}px`;
  el.advancedRoiBox.style.width = `${(width / rect.naturalWidth) * rect.width}px`;
  el.advancedRoiBox.style.height = `${(height / rect.naturalHeight) * rect.height}px`;
}

function renderContextLab() {
  if (el.contextImage) {
    el.contextImage.src = state.context.imageUrl || "";
  }
  if (el.contextImageName) {
    el.contextImageName.textContent = state.context.imageName || "No image loaded";
  }
  if (el.contextRunCompareBtn) {
    el.contextRunCompareBtn.disabled = !state.context.imageUrl || state.context.running;
    el.contextRunCompareBtn.textContent = state.context.running ? "Running..." : "Run Auto Compare";
  }
  if (el.contextApplyBestBtn) {
    el.contextApplyBestBtn.disabled = !currentContextBestRun();
  }
  if (el.contextStatus && !state.context.running) {
    el.contextStatus.textContent = state.context.runs.length
      ? "Compare complete. Select Best if one result is preferred."
      : state.context.source === "selectedSegment"
        ? "Selected segment is ready. Run compare."
        : "Load an image and run compare.";
  }
  renderContextRows();
  renderContextCandidateSetup();
  renderContextRecommendation();
  renderContextHistory();
}

function renderContextCandidateSetup() {
  if (!el.contextCandidateSetup) return;
  const candidates = state.context.candidates ?? defaultContextCandidates(state.context.source === "selectedSegment" ? "segment" : "image");
  state.context.candidates = candidates;
  el.contextCandidateSetup.innerHTML = candidates.map((candidate, index) => `
    <div class="context-candidate-card" data-candidate-index="${index}">
      <div class="context-candidate-head">
        <b>${escapeHtml(candidate.name)}</b>
        <label><input type="checkbox" data-candidate-field="enabled" ${candidate.enabled ? "checked" : ""}> Enabled</label>
      </div>
      <div class="context-candidate-grid">
        <div><div class="label">Image Size</div><select class="select" data-candidate-field="imageLongEdge">
          ${contextOption([384,512,640,768,896,1024], candidate.imageLongEdge, " px")}
        </select></div>
        <div class="wide preset-select-row">
          <div>
            <div class="label">Advanced Preset</div>
            <b>${escapeHtml(presetName(candidate.advancedPresetId || "none"))}</b>
            <span class="notice">${escapeHtml(advancedPresetSummary(candidate.advancedPresetId || "none"))}</span>
          </div>
          <button class="btn small-btn" data-edit-advanced-preset="${index}" type="button">Preset 선택</button>
        </div>
        <div><div class="label">Frames</div><select class="select" data-candidate-field="frameCount">
          ${contextOption([1,2,3,5,8], candidate.frameCount)}
        </select></div>
        <div><div class="label">Sampling</div><select class="select" data-candidate-field="samplingMode">
          ${contextOption(["first","middle","uniform","last"], candidate.samplingMode)}
        </select></div>
        <div><div class="label">Prompt</div><select class="select" data-candidate-field="promptMode">
          ${contextOption(["fast","description"], candidate.promptMode)}
        </select></div>
        <div><div class="label">Tokens</div><select class="select" data-candidate-field="maxOutputTokens">
          ${contextOption([80,120,160,224,320], candidate.maxOutputTokens)}
        </select></div>
      </div>
    </div>
  `).join("");

  el.contextCandidateSetup.querySelectorAll("[data-candidate-field]").forEach((control) => {
    control.addEventListener("change", updateContextCandidateFromControl);
    control.addEventListener("input", updateContextCandidateFromControl);
  });
  el.contextCandidateSetup.querySelectorAll("[data-edit-advanced-preset]").forEach((button) => {
    button.addEventListener("click", () => openAdvancedPresetPicker(Number(button.dataset.editAdvancedPreset)));
  });
}

function contextOption(values, selected, suffix = "") {
  return values.map((value) => {
    const valueText = String(value);
    return `<option value="${escapeHtml(valueText)}" ${valueText === String(selected) ? "selected" : ""}>${escapeHtml(valueText + suffix)}</option>`;
  }).join("");
}

function advancedPresetOptions(selected) {
  const presets = state.context.advancedPresets?.length
    ? state.context.advancedPresets
    : [{ id: "none", name: "None", settings: defaultAdvancedBlockSettings() }];
  return presets.map((preset) =>
    `<option value="${escapeHtml(preset.id)}" ${preset.id === selected ? "selected" : ""}>${escapeHtml(preset.name || preset.id)}</option>`
  ).join("");
}

function advancedPresetSummary(id) {
  const settings = blockSettingsForPreset(id, defaultAdvancedBlockSettings(), false);
  const parts = [
    `YOLO ${settings.yoloEnabled ? "On" : "Off"}`,
    `ROI ${settings.roiMode || "fullFrame"}`,
    `Crop ${settings.cropMode || "fullFrame"}`,
    `OCR ${settings.ocrEnabled ? "On" : "Off"}`
  ];
  return parts.join(" · ");
}

function updateContextCandidateFromControl(event) {
  const card = event.target.closest("[data-candidate-index]");
  if (!card) return;
  const index = Number(card.dataset.candidateIndex);
  const field = event.target.dataset.candidateField;
  const candidates = state.context.candidates ?? [];
  const candidate = candidates[index];
  if (!candidate || !field) return;

  if (event.target.type === "checkbox") {
    candidate[field] = event.target.checked;
  } else if (["imageLongEdge", "frameCount", "maxOutputTokens"].includes(field)) {
    candidate[field] = Number(event.target.value) || candidate[field];
  } else if (field === "yoloConfidence") {
    candidate[field] = Number(event.target.value) || 0.45;
  } else if (field === "advancedPresetId") {
    candidate[field] = event.target.value;
    const preset = (state.context.advancedPresets ?? []).find((item) => item.id === event.target.value);
    candidate.advancedPresetName = preset?.name || "None";
    if (preset?.settings) {
      candidate.blockSettings = { ...defaultAdvancedBlockSettings(), ...preset.settings };
      applyBlockSettingsToCandidate(candidate, candidate.blockSettings);
    }
    renderContextCandidateSetup();
  } else {
    candidate[field] = event.target.value;
  }
}

function collectContextCandidates() {
  if (el.contextCandidateSetup) {
    el.contextCandidateSetup.querySelectorAll("[data-candidate-field]").forEach((control) => {
      updateContextCandidateFromControl({ target: control, silent: true });
    });
  }

  return (state.context.candidates ?? [])
    .map((candidate) => {
      const settings = blockSettingsForPreset(candidate.advancedPresetId, candidate.blockSettings, false);
      const normalized = {
        ...candidate,
        advancedPresetName: presetName(candidate.advancedPresetId),
        blockSettings: settings,
        imageLongEdge: Number(candidate.imageLongEdge) || 768,
        frameCount: Number(candidate.frameCount) || 1,
        maxOutputTokens: Number(candidate.maxOutputTokens) || 120,
        yoloConfidence: Number(candidate.yoloConfidence) || 0.45
      };
      applyPresetAdvancedFieldsToCandidate(normalized, settings);
      return normalized;
    });
}

function presetName(id) {
  return (state.context.advancedPresets ?? []).find((preset) => preset.id === id)?.name || "None";
}

function blockSettingsForPreset(id, fallback = null, allowCurrent = true) {
  const preset = (state.context.advancedPresets ?? []).find((item) => item.id === id);
  const presetSettings = preset && (preset.id !== "none" || !fallback) ? preset.settings : null;
  return { ...defaultAdvancedBlockSettings(), ...(presetSettings ?? fallback ?? (allowCurrent ? collectAdvancedBlockSettings() : {})) };
}

function openAdvancedPresetPicker(index) {
  const candidate = state.context.candidates?.[index];
  if (!candidate) return;
  state.context.presetPickerCandidateIndex = index;
  if (el.presetPickerTitle) {
    el.presetPickerTitle.textContent = `${candidate.name || "Candidate"} 후보에 적용할 Advanced Blocks 프리셋을 선택합니다.`;
  }
  if (el.presetPickerSelect) {
    el.presetPickerSelect.innerHTML = advancedPresetOptions(candidate.advancedPresetId || "none");
    el.presetPickerSelect.value = candidate.advancedPresetId || "none";
  }
  renderAdvancedPresetPickerSummary();
  el.advancedPresetPickerDialog?.showModal();
}

function renderAdvancedPresetPickerSummary() {
  if (!el.presetPickerSummary) return;
  const id = el.presetPickerSelect?.value || "none";
  const settings = blockSettingsForPreset(id, defaultAdvancedBlockSettings(), false);
  const preset = (state.context.advancedPresets ?? []).find((item) => item.id === id);
  const rows = [
    ["Preset", preset?.name || "None"],
    ["YOLO", settings.yoloEnabled ? `On / ${settings.yoloLabels || "-"}` : "Off"],
    ["ROI", `${settings.roiMode || "fullFrame"} / ${settings.roiWidth || 0}x${settings.roiHeight || 0}`],
    ["Crop", `${settings.cropMode || "fullFrame"} / ${settings.cropOutputLongEdge || 768}px`],
    ["OCR", settings.ocrEnabled ? `${settings.ocrEngine || "future"} / ${settings.ocrLanguage || "ko"}` : "Off"],
    ["Sampling", `${settings.samplingMode || "uniform"} / ${settings.samplingFrameCount || 1} frame(s)`]
  ];
  el.presetPickerSummary.innerHTML = rows
    .map(([key, value]) => `<span>${escapeHtml(key)}</span><b>${escapeHtml(value)}</b>`)
    .join("");
}

function applyPickerPresetToCandidate() {
  const index = state.context.presetPickerCandidateIndex;
  if (index === null || index === undefined) return;
  applyPresetToCandidate(index, el.presetPickerSelect?.value || "none");
  el.advancedPresetPickerDialog?.close();
  renderContextCandidateSetup();
}

function editPickerPresetInAdvanced() {
  const id = el.presetPickerSelect?.value || "none";
  const index = state.context.presetPickerCandidateIndex;
  if (index !== null && index !== undefined) {
    applyPresetToCandidate(index, id);
  }
  selectAdvancedPreset(id, true);
  el.advancedPresetPickerDialog?.close();
  if (el.tabAdvanced) {
    el.tabAdvanced.checked = true;
  }
  renderContextCandidateSetup();
  showToast(`${presetName(id)} preset loaded in Advanced Blocks`);
}

function applyPresetToCandidate(index, id) {
  const candidate = state.context.candidates?.[index];
  if (!candidate) return;
  const settings = blockSettingsForPreset(id, defaultAdvancedBlockSettings(), false);
  candidate.advancedPresetId = id;
  candidate.advancedPresetName = presetName(id);
  candidate.blockSettings = settings;
  applyPresetAdvancedFieldsToCandidate(candidate, settings);
}

function applyPresetAdvancedFieldsToCandidate(candidate, settings) {
  candidate.yoloHints = !!settings.yoloEnabled;
  candidate.ocrHints = !!settings.ocrEnabled;
  candidate.yoloConfidence = Number(settings.yoloConfidence) || 0.45;
  candidate.targetLabels = settings.yoloLabels || candidate.targetLabels || "";
  candidate.roiMode = settings.roiMode || "fullFrame";
  candidate.cropMode = settings.cropMode || "fullFrame";
}

function applyBlockSettingsToCandidate(candidate, settings) {
  candidate.yoloHints = !!settings.yoloEnabled;
  candidate.ocrHints = !!settings.ocrEnabled;
  candidate.yoloConfidence = Number(settings.yoloConfidence) || 0.45;
  candidate.targetLabels = settings.yoloLabels || candidate.targetLabels || "";
  candidate.roiMode = settings.roiMode || "fullFrame";
  candidate.cropMode = settings.cropMode || "fullFrame";
  candidate.samplingMode = settings.samplingMode || "uniform";
  candidate.frameCount = Number(settings.samplingFrameCount) || candidate.frameCount || 1;
  candidate.imageLongEdge = Number(settings.cropOutputLongEdge) || candidate.imageLongEdge || 768;
}

function renderContextRows() {
  if (!el.contextRows) return;
  const rows = state.context.runs ?? [];
  if (!rows.length) {
    el.contextRows.innerHTML = `<tr><td colspan="7" class="notice">No context experiment results.</td></tr>`;
    return;
  }

  el.contextRows.innerHTML = rows.map((run) => {
    const candidate = run.candidate ?? {};
    const best = run.isBest ? `<span class="chip best-chip">Best</span>` : `<button class="btn small-btn" data-context-best="${escapeHtml(run.id)}" type="button">Best</button>`;
    return `
      <tr>
        <td><b>${escapeHtml(candidate.name || "-")}</b><div class="notice">${escapeHtml(candidate.description || "")}</div></td>
        <td>${formatContextSettingsDetailed(candidate)}</td>
        <td class="context-description">${escapeHtml(run.description || "(empty)")}</td>
        <td class="latency-cell">${formatMs(run.latencyMs)}</td>
        <td><div>Pre ${formatMs(run.preprocessMs)}</div><div>VLM ${formatMs(run.vlmMs)}</div></td>
        <td><button class="btn small-btn" data-context-trace="${escapeHtml(run.id)}" type="button">Trace</button></td>
        <td>${best}</td>
      </tr>`;
  }).join("");

  el.contextRows.querySelectorAll("[data-context-best]").forEach((button) => {
    button.addEventListener("click", () => {
      post("markVlmContextBest", { runId: button.dataset.contextBest });
    });
  });
  el.contextRows.querySelectorAll("[data-context-trace]").forEach((button) => {
    button.addEventListener("click", () => openContextTrace(button.dataset.contextTrace || ""));
  });
}

function renderContextRecommendation() {
  if (!el.contextRecommendation) return;
  const recommendation = state.context.recommendation;
  if (!recommendation?.hasData) {
    el.contextRecommendation.innerHTML = `<b>No Best label yet</b><div class="reason">결과에서 Best를 선택하면 전처리 추천 가이드가 표시됩니다.</div>`;
    return;
  }

  const candidate = recommendation.recommendedCandidate ?? {};
  const reasons = (recommendation.reasons ?? []).map((reason) => `<div class="reason">- ${escapeHtml(reason)}</div>`).join("");
  el.contextRecommendation.innerHTML = `
    <b>${escapeHtml(recommendation.summary || "Recommended settings")}</b>
    <div class="reason">추천: ${escapeHtml(formatContextSettings(candidate))}</div>
    <div class="reason">Best 누적: ${recommendation.bestCount ?? 0}건 · 평균 ${formatMs(recommendation.averageBestLatencyMs)}</div>
    ${reasons}`;
}

function renderContextHistory() {
  if (!el.contextHistory) return;
  const runs = state.context.history ?? [];
  if (!runs.length) {
    el.contextHistory.innerHTML = `<div class="notice">No saved context experiments.</div>`;
    return;
  }

  el.contextHistory.innerHTML = runs.slice(0, 20).map((run) => {
    const candidate = run.candidate ?? {};
    const mark = run.isBest ? `<span class="chip best-chip">Best</span>` : `<span></span>`;
    return `
      <div class="context-history-item">
        <b>${escapeHtml(candidate.name || "-")} · ${formatMs(run.latencyMs)} ${mark}</b>
        <span>${escapeHtml(run.inputLabel || run.inputFileName || "-")} · ${escapeHtml(run.inputType || "image")} · ${escapeHtml(run.modelName || "-")}</span>
        <span>${escapeHtml(run.description || "(empty)")}</span>
      </div>`;
  }).join("");
}

function reconcileContextBestFlags() {
  if (!state.context.runs?.length || !state.context.history?.length) return;
  const bestById = new Map(state.context.history.map((run) => [run.id, !!run.isBest]));
  state.context.runs = state.context.runs.map((run) => ({
    ...run,
    isBest: bestById.has(run.id) ? bestById.get(run.id) : run.isBest
  }));
}

function currentContextBestRun() {
  return (state.context.runs ?? []).find((run) => run.isBest) ||
    (state.context.history ?? []).find((run) => run.isBest) ||
    null;
}

function openContextTrace(runId) {
  const run = [...(state.context.runs ?? []), ...(state.context.history ?? [])].find((item) => item.id === runId);
  if (!run) {
    showToast("Trace is not available");
    return;
  }

  state.context.traceRun = run;
  state.context.traceTab = "prompt";
  if (el.contextTraceTitle) {
    const candidate = run.candidate ?? {};
    el.contextTraceTitle.textContent = `${candidate.name || "Context"} · ${run.inputLabel || run.inputFileName || "-"} · ${formatMs(run.latencyMs)}`;
  }
  if (el.contextTraceFrames) {
    const frames = run.frameUrls ?? [];
    el.contextTraceFrames.innerHTML = frames.length
      ? frames.map((url) => `<img class="trace-frame" src="${escapeHtml(url)}" alt="Context VLM frame">`).join("")
      : `<div class="notice">No input frame was recorded for this run.</div>`;
  }
  setContextTraceTab("prompt");
  el.contextTraceDialog?.showModal();
}

function setContextTraceTab(tabName) {
  state.context.traceTab = tabName;
  document.querySelectorAll(".context-trace-tab").forEach((button) => {
    button.classList.toggle("selected", button.dataset.contextTraceTab === tabName);
  });

  const run = state.context.traceRun;
  if (!el.contextTraceText) return;
  if (!run) {
    el.contextTraceText.textContent = "No context trace is selected.";
    return;
  }

  const values = {
    prompt: run.prompt || "No prompt captured.",
    request: prettyJson(run.requestJson),
    blocks: prettyJson(run.blockSettings),
    raw: prettyJson(run.rawResponse),
    description: run.description || "(empty)"
  };
  el.contextTraceText.textContent = values[tabName] ?? values.prompt;
}

function formatContextSettings(candidate) {
  return `${candidate.advancedPresetName || "None"} · ${candidate.imageLongEdge ?? "-"}px · ${candidate.frameCount ?? 1}F · ${candidate.samplingMode || "uniform"} · motion ${candidate.motionSummary ? "on" : "off"} · YOLO ${candidate.yoloHints ? "on" : "off"} · OCR ${candidate.ocrHints ? "on" : "off"} · ${candidate.maxOutputTokens ?? "-"} tokens`;
}

function formatContextSettingsDetailed(candidate) {
  const items = [
    ["Preset", candidate.advancedPresetName || "None"],
    ["Image", `${candidate.imageLongEdge ?? "-"} px`],
    ["Frames", `${candidate.frameCount ?? 1}`],
    ["Sampling", candidate.samplingMode || "uniform"],
    ["Prompt", candidate.promptMode || "description"],
    ["Tokens", candidate.maxOutputTokens ?? "-"],
    ["Motion", candidate.motionSummary ? "On" : "Off"],
    ["YOLO", candidate.yoloHints ? `On (${candidate.yoloConfidence ?? 0.45})` : "Off"],
    ["OCR", candidate.ocrHints ? "On" : "Off"],
    ["ROI", candidate.roiMode || "fullFrame"],
    ["Crop", candidate.cropMode || "fullFrame"]
  ];
  return `<div class="settings-grid">${items.map(([key, value]) => `<span>${escapeHtml(key)}</span><b>${escapeHtml(value)}</b>`).join("")}</div>`;
}

function formatMs(value) {
  const number = Number(value);
  if (!Number.isFinite(number) || number <= 0) return "-";
  return number >= 1000 ? `${(number / 1000).toFixed(2)}s` : `${number.toFixed(0)}ms`;
}

function onVideoLoaded(data) {
  state.metadata = data.metadata;
  state.videoUrl = data.videoUrl;
  state.imageUrl = "";
  state.mediaType = "video";
  state.segments = [];
  state.samples = [];
  state.selectedId = null;
  state.selectedSegmentIds.clear();
  state.currentHistoryId = null;
  state.segmentPlaybackEnd = null;
  setApiStatus("Ready");
  state.total = Number(data.metadata?.durationSeconds) || 1;
  showMainVideo(state.videoUrl);
  el.videoName.textContent = data.metadata?.fileName ?? "Video loaded";
  el.videoDuration.textContent = `${state.total.toFixed(2)}s`;
  el.loadBtn.classList.remove("primary");
  el.analyzeBtn.disabled = false;
  el.analyzeBtn.classList.add("primary");
  renderSummary();
  renderAxis();
  renderRows();
  renderInspector();
}

function onImageLoaded(data) {
  state.metadata = data.metadata;
  state.videoUrl = "";
  state.imageUrl = data.imageUrl || "";
  state.mediaType = "image";
  state.segments = [];
  state.samples = [];
  state.selectedId = null;
  state.selectedSegmentIds.clear();
  state.currentHistoryId = null;
  state.segmentPlaybackEnd = null;
  setApiStatus("Ready");
  state.total = Number(data.metadata?.durationSeconds) || 1;
  showMainImage(state.imageUrl);
  el.videoName.textContent = data.metadata?.fileName ?? "Image loaded";
  el.videoDuration.textContent = "Image";
  el.loadBtn.classList.remove("primary");
  el.analyzeBtn.disabled = true;
  el.analyzeBtn.classList.remove("primary");
  renderSummary();
  renderAxis();
  renderRows();
  renderInspector();
}

function showMainVideo(videoUrl) {
  if (el.mainMediaTitle) el.mainMediaTitle.textContent = "Main Video";
  if (el.mainImage) {
    el.mainImage.hidden = true;
    el.mainImage.removeAttribute("src");
  }
  if (el.mainVideo) {
    el.mainVideo.hidden = false;
    el.mainVideo.src = videoUrl || "";
    el.mainVideo.load();
  }
}

function showMainImage(imageUrl) {
  if (el.mainMediaTitle) el.mainMediaTitle.textContent = "Main Image";
  if (el.mainVideo) {
    el.mainVideo.pause();
    el.mainVideo.removeAttribute("src");
    el.mainVideo.load();
    el.mainVideo.hidden = true;
  }
  if (el.mainImage) {
    el.mainImage.hidden = false;
    el.mainImage.src = imageUrl ? `${imageUrl}?t=${Date.now()}` : "";
  }
}

function clearMainMedia() {
  if (el.mainMediaTitle) el.mainMediaTitle.textContent = "Main Video";
  if (el.mainImage) {
    el.mainImage.hidden = true;
    el.mainImage.removeAttribute("src");
  }
  if (el.mainVideo) {
    el.mainVideo.hidden = false;
    el.mainVideo.removeAttribute("src");
    el.mainVideo.load();
  }
}

function onHistoryLoaded(data) {
  state.metadata = data.metadata;
  state.videoUrl = data.videoUrl || "";
  state.imageUrl = data.imageUrl || "";
  state.mediaType = data.mediaType || (state.imageUrl ? "image" : "video");
  state.segments = (data.segments ?? []).map(normalizeSegment);
  state.samples = data.samples ?? [];
  state.selectedId = state.segments[0]?.id ?? null;
  state.selectedSegmentIds.clear();
  state.currentHistoryId = data.historyId || null;
  state.segmentPlaybackEnd = null;
  state.total = Number(data.metadata?.durationSeconds) || 1;
  setApiStatus("Ready");

  if (state.mediaType === "image" && state.imageUrl) {
    showMainImage(state.imageUrl);
    el.analyzeBtn.disabled = true;
    el.analyzeBtn.classList.remove("primary");
  } else if (data.videoExists && state.videoUrl) {
    showMainVideo(state.videoUrl);
    el.analyzeBtn.disabled = false;
    el.analyzeBtn.classList.add("primary");
  } else {
    clearMainMedia();
    el.analyzeBtn.disabled = true;
    el.analyzeBtn.classList.remove("primary");
  }

  el.videoName.textContent = `${data.metadata?.fileName ?? "History loaded"}${data.videoExists || state.mediaType === "image" ? "" : " (video missing)"}`;
  el.videoDuration.textContent = state.mediaType === "image" ? "Image" : `${state.total.toFixed(2)}s`;
  renderSummary(data.summary);
  renderAxis();
  renderRows();
  renderInspector();
  renderHistory();
  setProgress("History Loaded", 100, data.videoExists ? "Saved analysis restored." : "Saved result restored. Original video file was not found.");
}

function renderSummary(summary = {}) {
  const duration = Number(summary.videoLength ?? state.metadata?.durationSeconds ?? state.total ?? 0);
  el.processCount.textContent = String(summary.detectedSegments ?? state.segments.length ?? 0);
  el.cycleTime.textContent = `${duration.toFixed(2)}s`;
  el.aiProvider.textContent = providerLabel(summary.aiProvider ?? state.settings.defaultProvider);
  el.inputMode.textContent = modeLabel(summary.inputMode ?? state.settings.inputMode);
  if (el.resultLanguageSummary) {
    const lang = summary.resultLanguage ?? state.settings.resultLanguage ?? "ko";
    el.resultLanguageSummary.textContent = lang === "en" ? "English" : "한국어";
  }
  if (summary.performance) {
    state.performanceMetrics = summary.performance;
    renderPerformanceMetrics(summary.performance);
  }
  el.apiStatus.textContent = state.apiStatus;
}

function renderAxis() {
  el.axis.innerHTML = "";
  const step = stepSize();
  for (let time = 0; time <= state.total + 0.001; time += step) {
    const tick = document.createElement("div");
    tick.className = "tick";
    tick.style.left = `${pct(time)}%`;
    const label = document.createElement("span");
    label.textContent = `${time.toFixed(step < 1 ? 1 : 0)}s`;
    tick.appendChild(label);
    el.axis.appendChild(tick);
  }
}

function renderRows() {
  el.rows.innerHTML = "";
  state.segments.forEach((segment) => {
    const row = document.createElement("div");
    row.className = "process-row";

    const name = document.createElement("div");
    name.className = "process-name";
    const actor = getActorType(segment);
    const processType = normalizeEnum(segment.semantic?.processRole ?? segment.processRole, "unknown");
    name.innerHTML = `<div class="process-title-line"><input class="segment-check" type="checkbox" aria-label="Select segment ${segment.sequence}" ${state.selectedSegmentIds.has(segment.id) ? "checked" : ""}><b>${segment.sequence}. ${escapeHtml(getActionName(segment) || segment.actionCode || "-")}</b></div><div class="process-tags"><span class="actor-mini ${escapeHtml(actor)}">${escapeHtml(actorLabel(actor))}</span><span class="process-type-mini">${escapeHtml(processTypeLabel(processType))}</span></div><span>${fmt(segment.startTime)} -> ${fmt(segment.endTime)}</span>`;
    const checkbox = name.querySelector(".segment-check");
    checkbox?.addEventListener("pointerdown", (event) => event.stopPropagation());
    checkbox?.addEventListener("change", (event) => {
      if (event.target.checked) state.selectedSegmentIds.add(segment.id); else state.selectedSegmentIds.delete(segment.id);
      updateSegmentEditButtons();
    });

    const track = document.createElement("div");
    track.className = "track";

    const bar = document.createElement("div");
    bar.className = `bar${segment.id === state.selectedId ? " selected" : ""}${segment.id === state.activeId ? " active" : ""}`;
    bar.dataset.id = segment.id;
    bar.style.left = `${pct(segment.startTime)}%`;
    bar.style.width = `${Math.max(0.4, pct(segment.endTime) - pct(segment.startTime))}%`;
    bar.innerHTML = `<video muted playsinline preload="metadata" src="${escapeHtml(state.videoUrl)}"></video><span class="sync-tag">SYNC</span><span class="bar-label">${escapeHtml(getActionName(segment) || "-")}</span>`;
    bar.addEventListener("pointerdown", (event) => {
      event.stopPropagation();
      selectSegment(segment.id);
      state.segmentPlaybackEnd = Number(segment.endTime) || null;
      seekTo(segment.startTime);
    });

    track.appendChild(bar);
    row.append(name, track);
    el.rows.appendChild(row);
  });

  freezeMiniVideos();
}

function renderInspector() {
  const segment = selectedSegment();
  const disabled = !segment;
  const editable = [
    el.actorType, el.processType, el.stateType, el.actionName, el.description,
    el.targetObject, el.toolOrActor, el.interactionType, el.dependencyType, el.waitReason, el.repeatabilityType, el.pathConsistency, el.positionConsistency,
    el.motionDirection, el.motionLevel, el.idleBefore, el.idleAfter,
    el.actorConfidence, el.actionConfidence, el.evidence, el.uncertainty
  ];
  editable.forEach((control) => { if (control) control.disabled = disabled; });
  if (el.aiTraceBtn) el.aiTraceBtn.disabled = disabled;
  el.reanalyzeBtn.disabled = disabled;
  el.saveEditBtn.disabled = disabled;

  if (!segment) {
    el.timeInfo.textContent = "No segment selected";
    setControlValue(el.actorType, "unknown");
    setControlValue(el.processType, "unknown");
    el.motionType.value = "unknown";
    setControlValue(el.stateType, "unknown");
    el.actionName.value = "";
    el.score.textContent = "-";
    el.description.value = "";
    el.targetObject.value = "";
    el.toolOrActor.value = "";
    setControlValue(el.interactionType, "unknown");
    setControlValue(el.dependencyType, "unknown");
    el.waitReason.value = "";
    setControlValue(el.repeatabilityType, "unknown");
    setControlValue(el.pathConsistency, "unknown");
    setControlValue(el.positionConsistency, "unknown");
    el.motionDirection.value = "";
    setControlValue(el.motionLevel, "unknown");
    el.idleBefore.value = "";
    el.idleAfter.value = "";
    el.actorConfidence.value = "";
    el.actionConfidence.value = "";
    el.evidence.value = "";
    el.uncertainty.value = "";
    updateActorBadge("unknown");
    el.semanticSource.textContent = "-";
    el.schemaVersion.textContent = "1.1";
    el.candidates.innerHTML = "";
    el.lastAnalysis.textContent = "-";
    if (el.aiTraceBtn) el.aiTraceBtn.disabled = true;
    return;
  }

  const semantic = segment.semantic ?? {};
  const observation = segment.observation ?? {};
  const interaction = segment.interaction ?? {};
  const repeatability = segment.repeatability ?? {};
  const quality = segment.quality ?? {};
  const timing = segment.timing ?? {};

  const actorType = String(semantic.actorType ?? segment.actorType ?? "unknown");
  const motionType = normalizeEnum(semantic.motionType ?? segment.motionType, "unknown");
  const processType = String(semantic.processRole ?? segment.processRole ?? "unknown");
  const stateType = String(semantic.stateType ?? segment.stateType ?? "unknown");

  el.timeInfo.textContent = `${fmt(segment.startTime ?? timing.startSec)} -> ${fmt(segment.endTime ?? timing.endSec)} · ${Number(segment.duration ?? timing.durationSec ?? 0).toFixed(2)}s`;
  setControlValue(el.actorType, actorType);
  setControlValue(el.processType, processType);
  el.motionType.value = motionType;
  setControlValue(el.stateType, stateType);
  updateActorBadge(actorType);

  el.actionName.value = semantic.actionName ?? segment.actionName ?? "";
  el.score.textContent = scoreText(segment.aiMatchScore ?? quality.confidence);
  el.description.value = displayDescription(segment, semantic);
  el.targetObject.value = semantic.targetObject ?? segment.targetObject ?? "";
  el.toolOrActor.value = semantic.toolOrActor ?? segment.toolOrActor ?? "";
  setControlValue(el.interactionType, normalizeEnum(interaction.interactionType ?? segment.interactionType, "unknown"));
  setControlValue(el.dependencyType, normalizeEnum(interaction.dependency ?? segment.dependencyType ?? segment.dependency, "unknown"));
  el.waitReason.value = interaction.waitReason ?? segment.waitReason ?? "";
  setControlValue(el.repeatabilityType, normalizeEnum(repeatability.type ?? segment.repeatabilityType, "unknown"));
  setControlValue(el.pathConsistency, normalizeEnum(semantic.pathConsistency ?? segment.pathConsistency, "unknown"));
  setControlValue(el.positionConsistency, normalizeEnum(semantic.positionConsistency ?? segment.positionConsistency, "unknown"));
  el.motionDirection.value = observation.motionDirection ?? segment.motionDirection ?? "";
  setControlValue(el.motionLevel, normalizeEnum(observation.motionLevel ?? segment.motionLevel, "unknown"));
  el.idleBefore.value = numericOrBlank(timing.idleBeforeSec ?? segment.idleBeforeSec ?? segment.idleBefore);
  el.idleAfter.value = numericOrBlank(timing.idleAfterSec ?? segment.idleAfterSec ?? segment.idleAfter);
  el.actorConfidence.value = confidencePercent(quality.actorConfidence ?? segment.actorConfidence);
  el.actionConfidence.value = confidencePercent(quality.actionConfidence ?? segment.actionConfidence);
  el.evidence.value = linesValue(semantic.evidence ?? segment.evidence ?? segment.aiReason ?? segment.reason);
  el.uncertainty.value = linesValue(quality.uncertainties ?? segment.uncertainties ?? segment.uncertainty);
  el.semanticSource.textContent = segment.source?.actorType ?? segment.lastAnalysisProvider ?? state.settings.defaultProvider ?? "AI";
  el.schemaVersion.textContent = segment.schemaVersion ?? "1.1";

  el.lastAnalysis.textContent = [
    providerLabel(segment.lastAnalysisProvider || state.settings.defaultProvider),
    segment.lastAnalysisModel || currentModel(state.settings.defaultProvider),
    modeLabel(segment.lastAnalysisInputMode || state.settings.inputMode),
    `Top ${state.settings.candidateCount}`
  ].join(" / ");

  const candidates = Array.isArray(segment.candidates) ? segment.candidates.slice(0, state.settings.candidateCount) : [];
  el.candidates.innerHTML = candidates.map((candidate, index) => `
    <div class="candidate">
      <span class="rank">#${index + 1}</span>
      <b>${escapeHtml(candidate.action || candidate.actionName || "-")}</b>
      <span class="pct">${Math.round(Number(candidate.matchScore ?? candidate.confidence ?? 0) * ((Number(candidate.matchScore ?? candidate.confidence ?? 0) <= 1) ? 100 : 1))}%</span>
      <button class="btn small-btn" type="button" data-action="${escapeHtml(candidate.action || candidate.actionName || "")}" data-score="${Number(candidate.matchScore ?? candidate.confidence ?? 0)}">Apply</button>
    </div>
  `).join("");

  el.candidates.querySelectorAll("button").forEach((button) => {
    button.addEventListener("click", () => {
      el.actionName.value = button.dataset.action || "";
      const raw = Number(button.dataset.score) || 0;
      el.score.textContent = `${Math.round(raw <= 1 ? raw * 100 : raw)}%`;
    });
  });
}

function displayDescription(segment, semantic = segment?.semantic ?? {}) {
  return cleanDescription(semantic.description ?? segment?.description ?? "", segment);
}

function cleanDescription(value, segment = {}) {
  const text = String(value ?? "").trim();
  if (!text) return "";
  const source = String(segment?.source?.semantic || segment?.lastAnalysisProvider || "").toLowerCase();
  const status = String(segment?.semanticStatus || "").toLowerCase();
  const placeholderTexts = [
    "로컬 vlm 의미 분석 대상입니다",
    "local vlm 의미 분석",
    "opencv motion score is low",
    "ai 의미 분석에 실패",
    "local vlm 의미 분석을 실행하지 못했습니다"
  ];

  if (placeholderTexts.some((placeholder) => text.toLowerCase().includes(placeholder))) {
    return "";
  }

  if (status === "needs_review" || source === "opencv" || !source) {
    return "";
  }

  return text;
}

function updatePlaybackUI() {
  let time = el.mainVideo.currentTime || 0;
  if (state.segmentPlaybackEnd !== null && !el.mainVideo.paused && time >= state.segmentPlaybackEnd) {
    el.mainVideo.pause();
    el.mainVideo.currentTime = state.segmentPlaybackEnd;
    time = state.segmentPlaybackEnd;
    state.segmentPlaybackEnd = null;
  }

  el.playhead.style.left = `calc(155px + (100% - 155px) * ${pct(time) / 100})`;
  el.playheadTime.textContent = `${time.toFixed(2)}s`;
  const active = state.segments.find((segment) => time >= Number(segment.startTime) && time < Number(segment.endTime));
  state.activeId = active?.id ?? null;

  el.rows.querySelectorAll(".bar").forEach((bar) => {
    bar.classList.toggle("active", bar.dataset.id === state.activeId);
    bar.classList.toggle("selected", bar.dataset.id === state.selectedId);
  });
  syncMiniVideos(time, active);
  updateSegmentEditButtons();
}

function syncMiniVideos(time, active) {
  el.rows.querySelectorAll(".bar").forEach((bar) => {
    const video = bar.querySelector("video");
    const segment = state.segments.find((item) => item.id === bar.dataset.id);
    if (!video || !segment || !video.duration) {
      return;
    }

    video.pause();
    const target = active && segment.id === active.id ? time : Number(segment.startTime) || 0;
    if (Math.abs((video.currentTime || 0) - target) > 0.1) {
      try {
        video.currentTime = Math.min(target, Math.max(0, video.duration - 0.03));
      } catch {}
    }
  });
}

function freezeMiniVideos() {
  el.rows.querySelectorAll(".bar").forEach((bar) => {
    const video = bar.querySelector("video");
    const segment = state.segments.find((item) => item.id === bar.dataset.id);
    if (!video || !segment) {
      return;
    }
    video.addEventListener("loadedmetadata", () => {
      video.pause();
      try {
        video.currentTime = Math.min(Number(segment.startTime) || 0, Math.max(0, video.duration - 0.03));
      } catch {}
    }, { once: true });
  });
}

function smoothLoop() {
  if (!el.mainVideo.paused && !el.mainVideo.ended) {
    updatePlaybackUI();
  }
  requestAnimationFrame(smoothLoop);
}

function selectSegment(id) {
  state.selectedId = id;
  renderRows();
  renderInspector();
  updateSegmentEditButtons();
}

function saveEdit() {
  const segment = selectedSegment();
  if (!segment) {
    return;
  }

  const actorType = el.actorType.value || "unknown";
  const motionType = el.motionType.value || "unknown";
  const processRole = el.processType.value || "unknown";
  const stateType = el.stateType.value || "unknown";
  const interactionType = el.interactionType.value || "unknown";
  const dependencyType = el.dependencyType.value || "unknown";
  const waitReason = el.waitReason.value.trim();
  const repeatabilityType = el.repeatabilityType.value || "unknown";
  const pathConsistency = el.pathConsistency.value || "unknown";
  const positionConsistency = el.positionConsistency.value || "unknown";
  const motionLevel = el.motionLevel.value || "unknown";
  const evidence = textLines(el.evidence.value);
  const uncertainties = textLines(el.uncertainty.value);
  const actorConfidence = percentToRatio(el.actorConfidence.value);
  const actionConfidence = percentToRatio(el.actionConfidence.value);

  const updated = {
    ...segment,
    schemaVersion: segment.schemaVersion || "1.2",

    // Flat compatibility fields for the current C# backend / history format.
    actorType,
    motionType,
    processRole,
    stateType,
    actionName: el.actionName.value.trim(),
    description: el.description.value.trim(),
    targetObject: el.targetObject.value.trim(),
    toolOrActor: el.toolOrActor.value.trim(),
    interactionType,
    dependencyType,
    waitReason,
    repeatabilityType,
    pathConsistency,
    positionConsistency,
    motionDirection: el.motionDirection.value.trim(),
    motionLevel,
    idleBeforeSec: numberOrNull(el.idleBefore.value),
    idleAfterSec: numberOrNull(el.idleAfter.value),
    actorConfidence,
    actionConfidence,
    evidence,
    uncertainties,

    // Structured schema used by Optimization Guide and future exports.
    timing: {
      ...(segment.timing ?? {}),
      startSec: Number(segment.startTime ?? segment.timing?.startSec ?? 0),
      endSec: Number(segment.endTime ?? segment.timing?.endSec ?? 0),
      durationSec: Number(segment.duration ?? segment.timing?.durationSec ?? 0),
      idleBeforeSec: numberOrNull(el.idleBefore.value),
      idleAfterSec: numberOrNull(el.idleAfter.value)
    },
    observation: {
      ...(segment.observation ?? {}),
      motionDirection: el.motionDirection.value.trim(),
      motionLevel
    },
    semantic: {
      ...(segment.semantic ?? {}),
      actorType,
      motionType,
      processRole,
      stateType,
      actionName: el.actionName.value.trim(),
      targetObject: el.targetObject.value.trim(),
      toolOrActor: el.toolOrActor.value.trim(),
      description: el.description.value.trim(),
      pathConsistency,
      positionConsistency,
      evidence
    },
    interaction: {
      ...(segment.interaction ?? {}),
      interactionType,
      dependency: dependencyType,
      waitReason,
      humanPresent: actorType === "human" || actorType === "mixed",
      machinePresent: actorType === "machine" || actorType === "mixed"
    },
    repeatability: {
      ...(segment.repeatability ?? {}),
      type: repeatabilityType
    },
    quality: {
      ...(segment.quality ?? {}),
      actorConfidence,
      actionConfidence,
      uncertainties
    },
    source: {
      timing: "opencv",
      motion: segment.source?.motion || "opencv",
      semantic: segment.source?.semantic || segment.lastAnalysisProvider || state.settings.defaultProvider
    }
  };

  post("updateSegment", { segment: updated });
}

function renderHistory() {
  if (!el.historyList) {
    return;
  }

  if (!state.history.length) {
    el.historyList.innerHTML = `<div class="notice">No saved analysis history.</div>`;
    return;
  }

  el.historyList.innerHTML = state.history.map((item) => `
    <button class="history-item${item.id === state.currentHistoryId ? " active" : ""}" type="button" data-id="${escapeHtml(item.id)}">
      <span class="history-file${item.videoExists ? "" : " missing"}" title="${escapeHtml(historyTitle(item))}">${escapeHtml(item.videoFileName || "-")}</span>
      <span class="history-date">${formatHistoryDate(item.analyzedAt)}</span>
      <span class="history-score">${Math.round(Number(item.averageScore || 0))}%</span>
      <span class="btn history-delete" role="button" data-delete="${escapeHtml(item.id)}">×</span>
    </button>
  `).join("");

  el.historyList.querySelectorAll(".history-item").forEach((button) => {
    button.addEventListener("click", () => post("loadAnalysisHistory", { id: button.dataset.id }));
  });
  el.historyList.querySelectorAll("[data-delete]").forEach((button) => {
    button.addEventListener("click", (event) => {
      event.stopPropagation();
      const id = button.dataset.delete;
      if (id && confirm("Delete this analysis history?")) {
        post("deleteAnalysisHistory", { id });
      }
    });
  });
}

function openReanalyzeDialog() {
  const segment = selectedSegment();
  if (!segment) {
    return;
  }
  if (state.settings.defaultProvider === "opencv") {
    showToast("OpenCV only mode does not run AI re-analysis. Change provider to Local VLM.");
    return;
  }

  document.getElementById("reanalyzeSummary").innerHTML = `
    <div class="k">Segment</div><div>#${segment.sequence}</div>
    <div class="k">Time</div><div>${fmt(segment.startTime)} -> ${fmt(segment.endTime)} · ${Number(segment.duration || 0).toFixed(2)}s</div>
    <div class="k">Current Action</div><div>${escapeHtml(getActionName(segment) || "-")}</div>
  `;
  document.getElementById("reanalyzeProvider").value = "local";
  syncReanalyzeModelOptions("local");
  document.getElementById("reanalyzeMode").value = state.settings.inputMode;
  document.getElementById("reanalyzeCandidates").value = String(state.settings.candidateCount);
  el.reanalyzeDialog.showModal();
}

function runReanalyze() {
  const segment = selectedSegment();
  if (!segment) {
    return;
  }

  post("reAnalyzeSegment", {
    id: segment.id,
    defaultProvider: document.getElementById("reanalyzeProvider").value,
    provider: document.getElementById("reanalyzeProvider").value,
    model: document.getElementById("reanalyzeModel").value,
    inputMode: document.getElementById("reanalyzeMode").value,
    candidateCount: Number(document.getElementById("reanalyzeCandidates").value),
    imageLongEdge: readNumberSetting("imageLongEdge", state.settings.imageLongEdge || 320),
    maxOutputTokens: readNumberSetting("maxOutputTokens", state.settings.maxOutputTokens || 320),
    outputMode: readValueSetting("outputMode", state.settings.outputMode || "balanced"),
    singleImageLongEdge: readNumberSetting("singleImageLongEdge", state.settings.singleImageLongEdge || 256),
    singleImageMaxOutputTokens: readNumberSetting("singleImageMaxOutputTokens", state.settings.singleImageMaxOutputTokens || 160),
    singleImagePromptMode: readValueSetting("singleImagePromptMode", state.settings.singleImagePromptMode || "fast"),
    temperature: readNumberSetting("temperature", state.settings.temperature ?? 0.1),
    resultLanguage: state.settings.resultLanguage || "ko"
  });
  el.reanalyzeDialog.close();
}

function fillSettingsDialog() {
  setSelectValue("vlmRuntime", state.settings.runtimeProvider || "llama.cpp");
  setSelectValue("vlmModel", modelDisplayName(state.settings.activeModelId, state.settings.activeModelName));
  setSelectValue("vlmDevice", state.settings.device || "auto");
  syncVlmPathFields(false);
  setSelectValue("frameSampling", state.settings.inputMode || "opencvSegments");
  setSelectValue("maxFrames", String(state.settings.maxFrames || 2));
  setSelectValue("imageLongEdge", String(state.settings.imageLongEdge || 320));
  setSelectValue("cropMode", state.settings.cropMode || "roiContext");
  setSelectValue("outputMode", state.settings.outputMode || "balanced");
  setSelectValue("resultLanguage", state.settings.resultLanguage || "ko");
  setInputValue("maxOutputTokens", state.settings.maxOutputTokens ?? 320);
  setSelectValue("singleImageLongEdge", String(state.settings.singleImageLongEdge || 256));
  setSelectValue("singleImageMaxOutputTokens", String(state.settings.singleImageMaxOutputTokens || 160));
  setSelectValue("singleImagePromptMode", state.settings.singleImagePromptMode || "fast");
  setInputValue("temperature", state.settings.temperature ?? 0.1);
  setSelectValue("structuredOutput", String(state.settings.structuredOutput ?? true));
  setSelectValue("warmupOnStart", String(state.settings.warmupOnStart ?? false));
  renderVlmStatus();
}

function saveApiSettings() {
  state.settings = {
    ...state.settings,
    defaultProvider: document.getElementById("frameSampling").value === "opencvOnly" ? "opencv" : "local_vlm",
    runtimeProvider: document.getElementById("vlmRuntime").value,
    activeModelId: selectedModelId(),
    activeModelName: selectedModelName(),
    runtimePath: document.getElementById("vlmRuntimePath")?.value || "",
    modelPath: document.getElementById("vlmModelPath")?.value || "",
    mmprojPath: document.getElementById("vlmMmprojPath")?.value || "",
    endpointUrl: document.getElementById("vlmEndpointUrl")?.value || "",
    host: "127.0.0.1",
    port: Number(document.getElementById("vlmPort")?.value) || defaultModelPort(selectedModelId()),
    gpuLayers: Number(document.getElementById("vlmGpuLayers")?.value) || 0,
    contextSize: 4096,
    startupTimeoutSeconds: 90,
    device: document.getElementById("vlmDevice").value,
    inputMode: document.getElementById("frameSampling").value,
    candidateCount: 3,
    maxFrames: Number(document.getElementById("maxFrames").value),
    imageLongEdge: Number(document.getElementById("imageLongEdge").value),
    cropMode: document.getElementById("cropMode").value,
    outputMode: document.getElementById("outputMode").value,
    resultLanguage: document.getElementById("resultLanguage").value || "ko",
    maxOutputTokens: Number(document.getElementById("maxOutputTokens").value) || 320,
    singleImageLongEdge: Number(document.getElementById("singleImageLongEdge")?.value) || 256,
    singleImageMaxOutputTokens: Number(document.getElementById("singleImageMaxOutputTokens")?.value) || 160,
    singleImagePromptMode: document.getElementById("singleImagePromptMode")?.value || "fast",
    temperature: Number(document.getElementById("temperature").value) || 0.1,
    structuredOutput: document.getElementById("structuredOutput").value === "true",
    warmupOnStart: document.getElementById("warmupOnStart").value === "true"
  };

  post("saveLocalAiSettings", state.settings);
  el.apiDialog.close();
  renderSummary();
}

function testApiKey(provider) {
  post("testVlmModel", { provider });
}

function showApiTestResult(data) {
  showToast(data.message || data.status || "Model test finished");
}

function cameraIndex() {
  const value = Number(el.cameraIndex?.value ?? 0);
  return Number.isFinite(value) && value >= 0 ? Math.floor(value) : 0;
}

function renderCaptureStatus(data = {}) {
  const previewRunning = !!data.previewRunning;
  const recording = !!data.recording;
  if (el.capturePreviewState) el.capturePreviewState.textContent = previewRunning ? "Running" : "Stopped";
  if (el.captureRecordingState) el.captureRecordingState.textContent = recording ? "Recording" : "Idle";
  if (el.captureStatusText) el.captureStatusText.textContent = data.status || "Idle";
  if (el.captureRecordingPath) el.captureRecordingPath.textContent = data.recordingPath || "-";
  if (el.startPreviewBtn) el.startPreviewBtn.disabled = previewRunning;
  if (el.stopPreviewBtn) el.stopPreviewBtn.disabled = !previewRunning || recording;
  if (el.recordBtn) el.recordBtn.disabled = !previewRunning || recording;
  if (el.stopRecordBtn) el.stopRecordBtn.disabled = !recording;
  if (el.snapshotBtn) el.snapshotBtn.disabled = !previewRunning || recording;
  const imageUrl = data.status === "Image loaded for analysis"
    ? (data.lastSnapshotUrl || data.previewUrl || "")
    : (data.previewUrl || data.lastSnapshotUrl || "");
  if (imageUrl && el.capturePreview) {
    el.capturePreview.src = `${imageUrl}?t=${Date.now()}`;
  }
}

function collectAnalysisDefaults() {
  const inputMode = readValueSetting("frameSampling", state.settings.inputMode || "opencvSegments");
  return {
    defaultProvider: inputMode === "opencvOnly" ? "opencv" : state.settings.defaultProvider,
    inputMode,
    candidateCount: state.settings.candidateCount,
    maxFrames: readNumberSetting("maxFrames", state.settings.maxFrames || 2),
    imageLongEdge: readNumberSetting("imageLongEdge", state.settings.imageLongEdge || 320),
    cropMode: readValueSetting("cropMode", state.settings.cropMode || "roiContext"),
    outputMode: readValueSetting("outputMode", state.settings.outputMode || "balanced"),
    maxOutputTokens: readNumberSetting("maxOutputTokens", state.settings.maxOutputTokens || 320),
    singleImageLongEdge: readNumberSetting("singleImageLongEdge", state.settings.singleImageLongEdge || 256),
    singleImageMaxOutputTokens: readNumberSetting("singleImageMaxOutputTokens", state.settings.singleImageMaxOutputTokens || 160),
    singleImagePromptMode: readValueSetting("singleImagePromptMode", state.settings.singleImagePromptMode || "fast"),
    temperature: readNumberSetting("temperature", state.settings.temperature ?? 0.1),
    resultLanguage: readValueSetting("resultLanguage", state.settings.resultLanguage || "ko")
  };
}

function readValueSetting(id, fallback) {
  const control = document.getElementById(id);
  const value = control?.value;
  return value === undefined || value === null || value === "" ? fallback : value;
}

function readNumberSetting(id, fallback) {
  const raw = readValueSetting(id, fallback);
  const number = Number(raw);
  return Number.isFinite(number) ? number : fallback;
}

function post(type, data = {}) {
  if (window.chrome?.webview) {
    window.chrome.webview.postMessage({ type, data });
  }
}

function normalizeSegment(segment) {
  const timing = segment?.timing ?? {};
  const semantic = segment?.semantic ?? {};
  const quality = segment?.quality ?? {};
  return {
    ...segment,
    id: segment?.id ?? segment?.segmentId ?? `S${String(segment?.sequence ?? 0).padStart(3, "0")}`,
    startTime: Number(segment?.startTime ?? timing.startSec ?? 0),
    endTime: Number(segment?.endTime ?? timing.endSec ?? 0),
    duration: Number(segment?.duration ?? timing.durationSec ?? ((segment?.endTime ?? timing.endSec ?? 0) - (segment?.startTime ?? timing.startSec ?? 0))),
    actionName: semantic.actionName ?? segment?.actionName ?? "",
    description: cleanDescription(semantic.description ?? segment?.description ?? "", segment),
    actorType: semantic.actorType ?? segment?.actorType ?? "unknown",
    motionType: semantic.motionType ?? segment?.motionType ?? "unknown",
    stateType: semantic.stateType ?? segment?.stateType ?? "unknown",
    aiMatchScore: segment?.aiMatchScore ?? quality.confidence ?? null
  };
}

function getActionName(segment) {
  return segment?.semantic?.actionName ?? segment?.actionName ?? "";
}

function getActorType(segment) {
  return String(segment?.semantic?.actorType ?? segment?.actorType ?? "unknown");
}

function actorLabel(actor) {
  if (actor === "machine") return "MACHINE";
  if (actor === "human") return "HUMAN";
  if (actor === "mixed") return "MIXED";
  if (actor === "unknown") return "UNKNOWN";
  return String(actor || "UNKNOWN").toUpperCase();
}

function updateActorBadge(actor) {
  const raw = String(actor ?? "unknown");
  const known = ["machine", "human", "mixed", "unknown"].includes(raw) ? raw : "custom";
  el.actorBadge.className = `actor-badge ${known}`;
  el.actorBadge.textContent = actorLabel(raw);
}

function normalizeEnum(value, fallback = "unknown") {
  const normalized = String(value ?? "").trim().toLowerCase().replaceAll(" ", "_").replaceAll("-", "_");
  return normalized || fallback;
}

function setControlValue(control, value) {
  if (!control) return;
  // datalist-backed inputs intentionally accept values outside the preset list.
  if (!("options" in control) || control.tagName === "INPUT") {
    control.value = value ?? "";
    return;
  }
  const optionExists = Array.from(control.options ?? []).some((option) => option.value === value);
  control.value = optionExists ? value : (Array.from(control.options ?? []).some((option) => option.value === "unknown") ? "unknown" : value);
}

function numericOrBlank(value) {
  if (value === null || value === undefined || value === "") return "";
  const number = Number(value);
  return Number.isFinite(number) ? number : "";
}

function confidencePercent(value) {
  if (value === null || value === undefined || value === "") return "";
  const number = Number(value);
  if (!Number.isFinite(number)) return "";
  return Math.round(number <= 1 ? number * 100 : number);
}

function scoreText(value) {
  if (value === null || value === undefined || value === "") return "-";
  const number = Number(value);
  if (!Number.isFinite(number)) return "-";
  return `${Math.round(number <= 1 ? number * 100 : number)}%`;
}

function percentToRatio(value) {
  if (value === "" || value === null || value === undefined) return null;
  const number = Number(value);
  if (!Number.isFinite(number)) return null;
  return Math.max(0, Math.min(100, number)) / 100;
}

function numberOrNull(value) {
  if (value === "" || value === null || value === undefined) return null;
  const number = Number(value);
  return Number.isFinite(number) ? number : null;
}

function linesValue(value) {
  if (Array.isArray(value)) return value.join("\n");
  return String(value ?? "");
}

function textLines(value) {
  return String(value ?? "").split(/\r?\n/).map((line) => line.trim()).filter(Boolean);
}

function processTypeLabel(value) {
  const en = { unknown: "Unknown", prepare: "Prepare", load: "Load", unload: "Unload", transfer: "Transfer", position: "Position", process: "Process", fasten: "Fasten", inspect: "Inspect", wait: "Wait", return: "Return", other: "Other" };
  const ko = { unknown: "미확인", prepare: "준비", load: "투입", unload: "배출", transfer: "이송", position: "위치결정", process: "가공/처리", fasten: "체결", inspect: "검사", wait: "대기", return: "복귀", other: "기타" };
  const labels = state.settings.resultLanguage === "ko" ? ko : en;
  return labels[value] || value || labels.unknown;
}

function updateSegmentEditButtons() {
  const ids = [...state.selectedSegmentIds];
  const selected = selectedSegment();
  if (el.mergeSegmentsBtn) el.mergeSegmentsBtn.disabled = ids.length < 2 || !areSegmentsContiguous(ids);
  if (el.splitSegmentBtn) {
    const t = Number(el.mainVideo.currentTime || 0);
    el.splitSegmentBtn.disabled = !selected || !(t > Number(selected.startTime) + 0.02 && t < Number(selected.endTime) - 0.02);
  }
  if (el.restoreSegmentBtn) el.restoreSegmentBtn.disabled = !selected || !(selected.isMerged || selected.segmentSource === "manual_edit" || (selected.sourceSegmentIds?.length > 1));
}

function areSegmentsContiguous(ids) {
  const indices = ids.map((id) => state.segments.findIndex((s) => s.id === id)).filter((i) => i >= 0).sort((a,b) => a-b);
  if (indices.length < 2) return false;
  return indices.every((value, i) => i === 0 || value === indices[i-1] + 1);
}

function mergeSelectedSegments() {
  const ids = [...state.selectedSegmentIds];
  if (ids.length < 2 || !areSegmentsContiguous(ids)) { showToast("서로 인접한 Segment 2개 이상을 선택하세요."); return; }
  post("mergeSegments", { segmentIds: ids });
}

function splitSelectedSegmentAtPlayhead() {
  const segment = selectedSegment();
  if (!segment) return;
  const atSec = Number(el.mainVideo.currentTime || 0);
  if (!(atSec > Number(segment.startTime) + 0.02 && atSec < Number(segment.endTime) - 0.02)) { showToast("선택 Segment 내부에 Playhead를 놓으세요."); return; }
  post("splitSegment", { segmentId: segment.id, atSec });
}

function restoreSelectedSegment() {
  const segment = selectedSegment();
  if (!segment) return;
  post("restoreOriginalSegment", { segmentId: segment.id, sourceSegmentIds: segment.sourceSegmentIds ?? [] });
}

function openAiTraceDialog() {
  const segment = selectedSegment();
  if (!segment) {
    showToast("Select a segment first.");
    return;
  }

  const trace = segment.aiTrace || {};
  if (el.traceTitle) {
    el.traceTitle.textContent = `Segment ${segment.sequence ?? "-"} · ${segment.startTime?.toFixed?.(2) ?? "0.00"}s - ${segment.endTime?.toFixed?.(2) ?? "0.00"}s · ${trace.model || segment.lastAnalysisModel || "Local VLM"}`;
  }

  renderTraceMedia(segment, trace);
  setTraceTab("schema");
  el.aiTraceDialog?.showModal();
}

function renderTraceMedia(segment, trace = {}) {
  const frameUrls = trace.frameUrls?.length ? trace.frameUrls : (segment.frameUrls ?? []);
  if (el.traceFrames) {
    el.traceFrames.innerHTML = frameUrls.length
      ? frameUrls.map((url) => `<img class="trace-frame" src="${escapeHtml(url)}" alt="VLM input frame">`).join("")
      : `<div class="notice">No input frame was recorded for this trace.</div>`;
  }

  if (state.mediaType === "video" && state.videoUrl) {
    if (el.traceImage) {
      el.traceImage.hidden = true;
      el.traceImage.removeAttribute("src");
    }
    if (el.traceVideo) {
      el.traceVideo.hidden = false;
      el.traceVideo.src = state.videoUrl;
      el.traceVideo.load();
      el.traceVideo.onloadedmetadata = () => {
        try {
          el.traceVideo.currentTime = Math.max(0, Number(segment.startTime) || 0);
        } catch {}
      };
      el.traceVideo.ontimeupdate = () => {
        if (!el.traceVideo.paused && Number(segment.endTime) > 0 && el.traceVideo.currentTime >= Number(segment.endTime)) {
          el.traceVideo.pause();
        }
      };
    }
    return;
  }

  if (el.traceVideo) {
    el.traceVideo.pause();
    el.traceVideo.hidden = true;
    el.traceVideo.removeAttribute("src");
    el.traceVideo.load();
  }
  if (el.traceImage) {
    const imageUrl = frameUrls[0] || segment.thumbnailUrl || state.imageUrl || "";
    el.traceImage.hidden = false;
    el.traceImage.src = imageUrl ? `${imageUrl}${imageUrl.includes("?") ? "&" : "?"}trace=${Date.now()}` : "";
  }
}

function setTraceTab(tabName) {
  document.querySelectorAll(".trace-tab").forEach((button) => {
    button.classList.toggle("selected", button.dataset.traceTab === tabName);
  });
  const segment = selectedSegment();
  const trace = segment?.aiTrace ?? null;
  if (!el.traceText) return;
  if (!trace) {
    el.traceText.textContent = "No AI trace is available for this segment yet. Run Local VLM analysis or Re-analyze first.";
    return;
  }

  const values = {
    schema: trace.requestSchema || "No request schema captured.",
    prompt: trace.prompt || "No prompt captured.",
    request: prettyJson(trace.requestJson),
    raw: prettyJson(trace.rawResponse),
    parsed: prettyJson(trace.parsedResultJson),
    mapping: prettyJson(trace.mappingJson)
  };
  el.traceText.textContent = values[tabName] ?? values.schema;
}

function prettyJson(value) {
  if (value === null || value === undefined || value === "") return "";
  if (typeof value !== "string") {
    return JSON.stringify(value, null, 2);
  }
  try {
    return JSON.stringify(JSON.parse(value), null, 2);
  } catch {
    return value;
  }
}

function selectedSegment() {
  return state.segments.find((segment) => segment.id === state.selectedId) ?? null;
}

function seekTo(time) {
  if (!el.mainVideo.src) {
    return;
  }
  el.mainVideo.currentTime = Math.max(0, Math.min(Number(time) || 0, Math.max(0, (el.mainVideo.duration || state.total) - 0.001)));
  updatePlaybackUI();
}

function pointerTime(event) {
  const rect = el.seekSurface.getBoundingClientRect();
  return Math.max(0, Math.min(1, (event.clientX - rect.left) / rect.width)) * state.total;
}

function pct(time) {
  return Math.max(0, Math.min(100, (Number(time) || 0) / state.total * 100));
}

function stepSize() {
  if (state.scale !== "auto") {
    return Number(state.scale);
  }
  if (state.total <= 10) return 0.5;
  if (state.total <= 30) return 1;
  if (state.total <= 60) return 2;
  if (state.total <= 180) return 5;
  return 10;
}

function fmt(time) {
  const value = Number(time) || 0;
  const minutes = Math.floor(value / 60);
  const seconds = value - minutes * 60;
  return `${String(minutes).padStart(2, "0")}:${seconds.toFixed(2).padStart(5, "0")}`;
}

function formatDateTime(value) {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return "-";
  }

  return date.toLocaleString("ko-KR", {
    year: "2-digit",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit"
  });
}

function formatHistoryDate(value) {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return "-";
  }

  return date.toLocaleString("ko-KR", {
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit"
  });
}

function historyTitle(item) {
  return [
    item.videoFileName || "-",
    formatDateTime(item.analyzedAt),
    `${Number(item.durationSeconds || 0).toFixed(2)}s`,
    `${Number(item.segmentCount || 0)} segments`,
    providerLabel(item.provider || "opencv"),
    item.model || "-",
    item.videoExists ? "Video available" : "Video missing"
  ].join(" / ");
}

function currentModel(provider) {
  if (provider === "opencv") {
    return "OpenCV";
  }

  return state.settings.activeModelName || "Qwen3-VL 4B Instruct";
}

function providerLabel(provider) {
  if (provider === "opencv") {
    return "OpenCV";
  }

  return "Local VLM";
}

function syncProviderControls() {
  return;
}

function syncReanalyzeModelOptions(provider = document.getElementById("reanalyzeProvider").value) {
  const modelSelect = document.getElementById("reanalyzeModel");
  const options = state.vlmModels.length
    ? state.vlmModels.map((model) => model.name || model.id).filter(Boolean)
    : [state.settings.activeModelName || "Qwen3-VL 4B Instruct"];

  modelSelect.innerHTML = options
    .filter(Boolean)
    .map((model) => `<option value="${escapeHtml(model)}">${escapeHtml(model)}</option>`)
    .join("");
  modelSelect.value = currentModel(provider);
}

function normalizeLocalModelName(model) {
  return model || "Qwen3-VL 4B Instruct";
}

function syncVlmPathFields(preferProfile = true) {
  const selectedId = selectedModelId();
  const profile = activeModelProfile(selectedId);
  const useSettings = !preferProfile;
  setInputValue("vlmRuntimePath", useSettings ? (state.settings.runtimePath || profile?.runtimePath || "runtimes/llama.cpp/llama-server.exe") : (profile?.runtimePath || state.settings.runtimePath || "runtimes/llama.cpp/llama-server.exe"));
  setInputValue("vlmModelPath", useSettings ? (state.settings.modelPath || profile?.modelPath || "") : (profile?.modelPath || state.settings.modelPath || ""));
  setInputValue("vlmMmprojPath", useSettings ? (state.settings.mmprojPath || profile?.mmprojPath || "") : (profile?.mmprojPath || state.settings.mmprojPath || ""));
  setInputValue("vlmEndpointUrl", useSettings ? (state.settings.endpointUrl || profile?.endpointUrl || "") : (profile?.endpointUrl || ""));
  setInputValue("vlmPort", useSettings ? (state.settings.port || profile?.port || defaultModelPort(selectedId)) : (profile?.port || defaultModelPort(selectedId)));
  setInputValue("vlmGpuLayers", useSettings ? (state.settings.gpuLayers ?? profile?.gpuLayers ?? 0) : (profile?.gpuLayers ?? state.settings.gpuLayers ?? 0));
}

function activeModelProfile(id = selectedModelId()) {
  return state.vlmModels.find((model) => model.id === id);
}

function defaultModelPort(id) {
  return String(id || "").includes("intern") ? 18081 : 18080;
}

function modeLabel(mode) {
  if (mode === "fastVlm") return `Fast VLM / ${state.settings.maxFrames || 2}F`;
  if (mode === "opencvOnly") return "OpenCV Only";
  if (mode === "opencvSegments" || mode === "adaptive") return `OpenCV Segments + VLM / ${state.settings.maxFrames || 2}F`;
  return "Frames";
}

function selectedModelId() {
  const text = document.getElementById("vlmModel")?.value || "";
  if (text.includes("InternVL")) return "internvl3_5-4b-q4km";
  if (text.includes("Custom")) return state.settings.activeModelId || "custom-local-vlm";
  return "qwen3-vl-4b-q4km";
}

function selectedModelName() {
  const text = document.getElementById("vlmModel")?.value || "";
  if (text.includes("InternVL")) return "InternVL3.5 4B";
  if (text.includes("Custom")) return "Custom Model";
  return "Qwen3-VL 4B Instruct";
}

function modelDisplayName(id, name) {
  if (String(id || "").includes("intern")) return "InternVL3.5 4B · Q4_K_M";
  if (String(id || "").includes("custom")) return "Custom Model...";
  return `${name || "Qwen3-VL 4B Instruct"} · Q4_K_M`;
}

function setSelectValue(id, value) {
  const control = document.getElementById(id);
  if (!control) return;
  const option = Array.from(control.options || []).find((item) => item.value === String(value) || item.textContent === String(value));
  control.value = option?.value ?? control.options?.[0]?.value ?? "";
}

function setInputValue(id, value) {
  const control = document.getElementById(id);
  if (control) control.value = value ?? "";
}

function setChecked(id, value) {
  const control = document.getElementById(id);
  if (control) control.checked = !!value;
}

function readChecked(id) {
  return !!document.getElementById(id)?.checked;
}

function renderVlmStatus() {
  const stateText = state.vlmStatus?.state || "not_ready";
  const ready = stateText === "ready";
  const label = ready ? "Loaded · Warmed up" : state.vlmStatus?.message || "Not Ready";
  const modelState = document.getElementById("vlmModelState");
  if (modelState) modelState.value = label;
  document.querySelectorAll(".ai-ready").forEach((chip) => {
    chip.classList.toggle("bad", !ready);
    const model = state.settings.activeModelName || "Qwen3-VL 4B";
    chip.innerHTML = `<span class="ready-dot"></span>${escapeHtml(model)} · ${ready ? "Ready" : "Not Ready"}`;
  });
  if (el.aiProvider) {
    el.aiProvider.textContent = ready ? `${state.settings.activeModelName || "Local VLM"} · Q4` : "Local VLM · Not Ready";
  }
}

function renderPerformanceMetrics(metrics = state.performanceMetrics) {
  if (!metrics) {
    if (el.perfSummary) {
      el.perfSummary.innerHTML = `<span><span class="perf-live-dot"></span> Analysis time</span><strong>-</strong><span class="perf-target">Waiting</span><span class="perf-chevron">›</span>`;
    }
    const drawerTotal = document.getElementById("perfDrawerTotal");
    const drawerStatus = document.getElementById("perfDrawerStatus");
    const perfSub = document.getElementById("perfSub");
    const perfTip = document.getElementById("perfTip");
    if (drawerTotal) drawerTotal.textContent = "-";
    if (drawerStatus) drawerStatus.innerHTML = `<b>WAIT</b><span>Local VLM</span>`;
    if (perfSub) perfSub.textContent = "No analysis measured yet";
    if (perfTip) perfTip.innerHTML = `<b>Bottleneck</b><span>-</span><small>Run an analysis to see measured Local VLM performance.</small>`;
    return;
  }
  const totalSec = (Number(metrics.totalMs || 0) / 1000).toFixed(2);
  const totalMs = Math.max(Number(metrics.totalMs || 0), 1);
  if (el.perfSummary) {
    const segments = Number(metrics.segmentCount || state.segments.length || 0);
    el.perfSummary.innerHTML = `<span><span class="perf-live-dot"></span> Analysis time</span><strong>${totalSec} s</strong><span class="perf-target">${segments} segments</span><span class="perf-chevron">›</span>`;
  }
  const drawerTotal = document.getElementById("perfDrawerTotal");
  const drawerStatus = document.getElementById("perfDrawerStatus");
  const perfSub = document.getElementById("perfSub");
  const perfBars = document.getElementById("perfBars");
  const perfStats = document.getElementById("perfStats");
  const perfTip = document.getElementById("perfTip");
  if (drawerTotal) drawerTotal.textContent = `${totalSec} s`;
  if (drawerStatus) drawerStatus.innerHTML = `<b>DONE</b><span>${escapeHtml(metrics.modelName || "Local VLM")}</span>`;
  if (perfSub) {
    perfSub.textContent = `Last analysis · ${(Number(metrics.videoDurationSec || 0)).toFixed(2)} s video · ${Number(metrics.inputFrames || 0)} frame(s)`;
  }
  const backendTotalMs = Number(metrics.backendTotalMs || metrics.totalMs || 0);
  const rows = [
    ["Timing + Frames", "Fast windows or OpenCV timing, representative frames", Number(metrics.openCvSegmentationMs || 0)],
    ["Frame / Resize", `${Number(metrics.imageLongEdge || state.settings.imageLongEdge || 0)} px / ${Number(metrics.inputFrames || 0)} frame(s)`, Number(metrics.frameSelectionMs || 0) + Number(metrics.roiCropResizeMs || 0)],
    ["Local VLM Inference", `${Number(metrics.segmentCount || 0)} segment(s)`, Number(metrics.jsonParsingMs || 0)],
    ["Backend Processing", "WPF analysis pipeline to completion", backendTotalMs],
    ["Total User Wait", "Analyze click to result shown", Number(metrics.totalMs || 0)]
  ];
  if (perfBars) {
    perfBars.innerHTML = rows.map(([name, detail, value]) => {
      const pctValue = Math.max(1, Math.min(100, (Number(value) / totalMs) * 100));
      return `<div class="perf-row"><div><b>${escapeHtml(name)}</b><span>${escapeHtml(detail)}</span></div><strong>${formatDurationMs(Number(value))}</strong><i style="--w:${pctValue.toFixed(1)}%"></i></div>`;
    }).join("");
  }
  if (perfStats) {
    perfStats.innerHTML = `
      <div><span>Model</span><b>${escapeHtml(metrics.modelName || state.settings.activeModelName || "Local VLM")}</b></div><div><span>Provider</span><b>${escapeHtml(metrics.providerId || "llama.cpp")}</b></div>
      <div><span>Warm-up</span><b>${metrics.warmupDone ? "Done" : "Off"}</b></div><div><span>Frames</span><b>${Number(metrics.inputFrames || 0)}</b></div>
      <div><span>Resolution</span><b>${Number(metrics.imageLongEdge || state.settings.imageLongEdge || 0)} px</b></div><div><span>Segments</span><b>${Number(metrics.segmentCount || 0)}</b></div>
      <div><span>Video</span><b>${Number(metrics.videoDurationSec || 0).toFixed(2)} s</b></div><div><span>Output Tokens</span><b>${metrics.outputTokens ?? "-"}</b></div>`;
  }
  if (perfTip) {
    const slowest = rows.slice(0, 3).sort((a, b) => Number(b[2]) - Number(a[2]))[0];
    perfTip.innerHTML = `<b>Bottleneck</b><span>${escapeHtml(slowest[0])} · ${formatDurationMs(Number(slowest[2]))}</span><small>GPU runtime is used when configured. First Local VLM request can include model/graph warm-up time.</small>`;
  }
}

function formatDurationMs(value) {
  if (!Number.isFinite(value) || value <= 0) return "-";
  return value >= 1000 ? `${(value / 1000).toFixed(2)} s` : `${Math.round(value)} ms`;
}

function setProgress(stage, percent, message) {
  const value = Math.max(0, Math.min(100, Number(percent) || 0));
  el.progressStage.textContent = stage || "Working";
  el.progressPercent.textContent = `${Math.round(value)}%`;
  el.progressBar.style.width = `${value}%`;
  el.progressMessage.textContent = message || "";
}

function setApiStatus(status) {
  state.apiStatus = status || "Ready";
  el.apiStatus.textContent = state.apiStatus;
}

function setStatus(id, cls, text) {
  document.getElementById(id).innerHTML = `<span class="status-dot ${cls}"></span>${escapeHtml(text)}`;
}

function toggleKey(inputId, buttonId) {
  const input = document.getElementById(inputId);
  input.type = input.type === "password" ? "text" : "password";
  document.getElementById(buttonId).textContent = input.type === "password" ? "Show" : "Hide";
}

function showToast(message) {
  el.toast.textContent = message;
  el.toast.classList.add("show");
  window.setTimeout(() => el.toast.classList.remove("show"), 4200);
}

function escapeHtml(value) {
  return String(value ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}

// Offline Performance UI concept drawer
(() => {
  const drawer = document.getElementById('performanceDrawer');
  const backdrop = document.getElementById('perfBackdrop');
  const openers = [document.getElementById('performanceBtn'), document.getElementById('perfSummary')].filter(Boolean);
  const closeBtn = document.getElementById('closePerformanceBtn');
  const setOpen = (open) => {
    if (!drawer || !backdrop) return;
    drawer.classList.toggle('open', open);
    backdrop.classList.toggle('open', open);
    drawer.setAttribute('aria-hidden', String(!open));
  };
  openers.forEach(b => b.addEventListener('click', () => setOpen(true)));
  closeBtn?.addEventListener('click', () => setOpen(false));
  backdrop?.addEventListener('click', () => setOpen(false));
})();

// Multi-VLM model manager UI (prototype wiring; backend owns actual profiles/providers).
(() => {
  const manageBtn = document.getElementById('manageModelsBtn');
  const dialog = document.getElementById('modelManagerDialog');
  if (manageBtn && dialog) manageBtn.addEventListener('click', () => dialog.showModal());
})();
