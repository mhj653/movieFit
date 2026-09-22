namespace ProcessVideoAnalyzer.Trigger;

public interface ITriggerSource : IAsyncDisposable
{
    event EventHandler? RecordRequested;
    event EventHandler? StopRequested;
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}
