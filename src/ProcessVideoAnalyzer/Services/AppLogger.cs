namespace ProcessVideoAnalyzer.Services;

public sealed class AppLogger
{
    private readonly string _logFile;
    private readonly object _gate = new();

    public AppLogger()
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "Logs");
        Directory.CreateDirectory(folder);
        _logFile = Path.Combine(folder, $"process-analyzer-{DateTime.Now:yyyy-MM-dd}.log");
    }

    public void Info(string message)
    {
        Write("INFO", message);
    }

    public void Error(string message, Exception exception)
    {
        Write("ERROR", $"{message}{Environment.NewLine}{exception}");
    }

    private void Write(string level, string message)
    {
        lock (_gate)
        {
            File.AppendAllText(_logFile, $"{DateTime.Now:O} [{level}] {message}{Environment.NewLine}");
        }
    }
}
