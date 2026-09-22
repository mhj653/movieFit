namespace ProcessVideoAnalyzer.Trigger;

public sealed class ManualTriggerSource : ITriggerSource
{
    public event EventHandler? RecordRequested;
    public event EventHandler? StopRequested;

    public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public void RequestRecord() => RecordRequested?.Invoke(this, EventArgs.Empty);

    public void RequestStop() => StopRequested?.Invoke(this, EventArgs.Empty);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
