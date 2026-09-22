namespace ProcessVideoAnalyzer.Capture;

public sealed class CaptureSessionState
{
    public bool PreviewRunning { get; set; }
    public bool Recording { get; set; }
    public int CameraIndex { get; set; }
    public string Status { get; set; } = "Idle";
    public string PreviewPath { get; set; } = "";
    public string RecordingPath { get; set; } = "";
    public string LastSnapshotPath { get; set; } = "";
    public DateTimeOffset? RecordingStartedAt { get; set; }
}
