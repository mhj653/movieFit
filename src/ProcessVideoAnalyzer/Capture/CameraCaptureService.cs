using OpenCvSharp;
using ProcessVideoAnalyzer.Services;

namespace ProcessVideoAnalyzer.Capture;

public sealed class CameraCaptureService : IDisposable
{
    private readonly AppLogger _logger;
    private readonly object _sync = new();
    private readonly string _captureRoot;
    private CancellationTokenSource? _previewCancellation;
    private Task? _previewTask;
    private VideoCapture? _capture;
    private VideoWriter? _writer;
    private CaptureSessionState _state = new();
    private Mat? _latestFrame;
    private int _writerWidth;
    private int _writerHeight;

    public CameraCaptureService(AppLogger logger)
    {
        _logger = logger;
        _captureRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ProcessVideoAnalyzer",
            "Captures");
        Directory.CreateDirectory(_captureRoot);
    }

    public CaptureSessionState State
    {
        get
        {
            lock (_sync)
            {
                return CloneState();
            }
        }
    }

    public Task StartPreviewAsync(int cameraIndex = 0, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            if (_state.PreviewRunning)
            {
                return Task.CompletedTask;
            }

            _previewCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _state = new CaptureSessionState
            {
                CameraIndex = cameraIndex,
                PreviewRunning = true,
                Status = "Starting camera preview",
                PreviewPath = Path.Combine(_captureRoot, "preview.jpg")
            };
            _previewTask = Task.Run(() => PreviewLoop(cameraIndex, _previewCancellation.Token), CancellationToken.None);
        }

        return Task.CompletedTask;
    }

    public async Task StopPreviewAsync()
    {
        CancellationTokenSource? cancellation;
        Task? task;
        lock (_sync)
        {
            cancellation = _previewCancellation;
            task = _previewTask;
            _previewCancellation = null;
            _previewTask = null;
        }

        if (cancellation is not null)
        {
            await cancellation.CancelAsync();
            cancellation.Dispose();
        }

        if (task is not null)
        {
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
            }
        }
    }

    public Task<string> StartRecordingAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            if (!_state.PreviewRunning)
            {
                throw new InvalidOperationException("Start camera preview before recording.");
            }

            if (_state.Recording)
            {
                return Task.FromResult(_state.RecordingPath);
            }

            var timestamp = DateTimeOffset.Now.ToString("yyyyMMdd_HHmmss");
            var path = Path.Combine(_captureRoot, $"capture_{timestamp}.mp4");
            _state.RecordingPath = path;
            _state.Recording = true;
            _state.RecordingStartedAt = DateTimeOffset.Now;
            _state.Status = "Recording";
            return Task.FromResult(path);
        }
    }

    public Task<string> StopRecordingAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            if (!_state.Recording)
            {
                throw new InvalidOperationException("Recording is not running.");
            }

            _state.Recording = false;
            _state.Status = "Recording stopped";
            _writer?.Release();
            _writer?.Dispose();
            _writer = null;
            _writerWidth = 0;
            _writerHeight = 0;
            return Task.FromResult(_state.RecordingPath);
        }
    }

    public Task<string> CaptureSnapshotAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            if (_latestFrame is null || _latestFrame.Empty())
            {
                throw new InvalidOperationException("No camera frame is available for snapshot.");
            }

            var timestamp = DateTimeOffset.Now.ToString("yyyyMMdd_HHmmss_fff");
            var path = Path.Combine(_captureRoot, $"snapshot_{timestamp}.jpg");
            Cv2.ImWrite(path, _latestFrame, new ImageEncodingParam(ImwriteFlags.JpegQuality, 88));
            _state.LastSnapshotPath = path;
            _state.Status = "Snapshot captured";
            return Task.FromResult(path);
        }
    }

    public string ImportImageForAnalysis(string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
        {
            throw new FileNotFoundException("Image file was not found.", sourcePath);
        }

        using var image = Cv2.ImRead(sourcePath, ImreadModes.Color);
        if (image.Empty())
        {
            throw new InvalidOperationException("Selected image could not be decoded.");
        }

        var timestamp = DateTimeOffset.Now.ToString("yyyyMMdd_HHmmss_fff");
        var targetPath = Path.Combine(_captureRoot, $"loaded_image_{timestamp}.jpg");
        Cv2.ImWrite(targetPath, image, new ImageEncodingParam(ImwriteFlags.JpegQuality, 90));

        lock (_sync)
        {
            _state.LastSnapshotPath = targetPath;
            _state.Status = "Image loaded for analysis";
        }

        return targetPath;
    }

    public void Dispose()
    {
        _previewCancellation?.Cancel();
        _previewCancellation?.Dispose();
        _writer?.Dispose();
        _capture?.Dispose();
        _latestFrame?.Dispose();
    }

    private void PreviewLoop(int cameraIndex, CancellationToken cancellationToken)
    {
        try
        {
            using var capture = new VideoCapture(cameraIndex);
            if (!capture.IsOpened())
            {
                lock (_sync)
                {
                    _state.PreviewRunning = false;
                    _state.Status = $"Camera {cameraIndex} could not be opened.";
                }

                return;
            }

            lock (_sync)
            {
                _capture = capture;
                _state.Status = "Preview running";
            }

            using var frame = new Mat();
            var lastPreviewWrite = DateTimeOffset.MinValue;
            while (!cancellationToken.IsCancellationRequested)
            {
                if (!capture.Read(frame) || frame.Empty())
                {
                    Thread.Sleep(50);
                    continue;
                }

                lock (_sync)
                {
                    _latestFrame?.Dispose();
                    _latestFrame = frame.Clone();
                    if (_state.Recording)
                    {
                        EnsureWriter(frame);
                        _writer?.Write(frame);
                    }

                    if ((DateTimeOffset.Now - lastPreviewWrite).TotalMilliseconds >= 250)
                    {
                        Cv2.ImWrite(_state.PreviewPath, frame, new ImageEncodingParam(ImwriteFlags.JpegQuality, 80));
                        lastPreviewWrite = DateTimeOffset.Now;
                    }
                }

                Thread.Sleep(10);
            }
        }
        catch (Exception ex)
        {
            _logger.Error("Camera preview loop failed", ex);
            lock (_sync)
            {
                _state.Status = ex.Message;
            }
        }
        finally
        {
            lock (_sync)
            {
                _writer?.Release();
                _writer?.Dispose();
                _writer = null;
                _capture = null;
                _state.PreviewRunning = false;
                _state.Recording = false;
                if (_state.Status is "Preview running" or "Recording")
                {
                    _state.Status = "Preview stopped";
                }
            }
        }
    }

    private void EnsureWriter(Mat frame)
    {
        if (_writer is not null && _writerWidth == frame.Width && _writerHeight == frame.Height)
        {
            return;
        }

        _writer?.Release();
        _writer?.Dispose();
        _writerWidth = frame.Width;
        _writerHeight = frame.Height;
        var fps = 20.0;
        _writer = new VideoWriter(
            _state.RecordingPath,
            FourCC.MP4V,
            fps,
            new Size(frame.Width, frame.Height));
        if (!_writer.IsOpened())
        {
            _writer.Dispose();
            var fallback = Path.ChangeExtension(_state.RecordingPath, ".avi");
            _state.RecordingPath = fallback;
            _writer = new VideoWriter(
                fallback,
                FourCC.MJPG,
                fps,
                new Size(frame.Width, frame.Height));
        }

        if (!_writer.IsOpened())
        {
            throw new InvalidOperationException("Camera recording writer could not be opened.");
        }
    }

    private CaptureSessionState CloneState()
    {
        return new CaptureSessionState
        {
            PreviewRunning = _state.PreviewRunning,
            Recording = _state.Recording,
            CameraIndex = _state.CameraIndex,
            Status = _state.Status,
            PreviewPath = _state.PreviewPath,
            RecordingPath = _state.RecordingPath,
            LastSnapshotPath = _state.LastSnapshotPath,
            RecordingStartedAt = _state.RecordingStartedAt
        };
    }
}
