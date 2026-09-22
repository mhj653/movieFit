using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ProcessVideoAnalyzer.Services;

public sealed class LocalVideoServer : IDisposable
{
    private readonly AppLogger _logger;
    private readonly CancellationTokenSource _cancellation = new();
    private TcpListener? _listener;
    private Task? _serverTask;
    private string? _videoPath;

    public LocalVideoServer(AppLogger logger)
    {
        _logger = logger;
    }

    public int Port { get; private set; }

    public void Start()
    {
        if (_listener is not null)
        {
            return;
        }

        _listener = new TcpListener(IPAddress.Loopback, 0);
        _listener.Start();
        Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        _serverTask = Task.Run(() => AcceptLoopAsync(_cancellation.Token));
        _logger.Info($"Local video server started on 127.0.0.1:{Port}");
    }

    public string SetVideo(string videoPath)
    {
        _videoPath = videoPath;
        return $"http://127.0.0.1:{Port}/video/{Uri.EscapeDataString(Path.GetFileName(videoPath))}?v={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
    }

    public void Dispose()
    {
        _cancellation.Cancel();
        _listener?.Stop();
        _cancellation.Dispose();
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        if (_listener is null)
        {
            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var client = await _listener.AcceptTcpClientAsync(cancellationToken);
                _ = Task.Run(() => HandleClientAsync(client, cancellationToken), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.Error("Local video server accept failed", ex);
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        using (client)
        {
            try
            {
                using var stream = client.GetStream();
                using var reader = new StreamReader(stream, Encoding.ASCII, false, 8192, leaveOpen: true);
                var requestLine = await reader.ReadLineAsync(cancellationToken);
                if (string.IsNullOrWhiteSpace(requestLine))
                {
                    return;
                }

                string? rangeHeader = null;
                string? line;
                while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync(cancellationToken)))
                {
                    if (line.StartsWith("Range:", StringComparison.OrdinalIgnoreCase))
                    {
                        rangeHeader = line["Range:".Length..].Trim();
                    }
                }

                if (_videoPath is null || !File.Exists(_videoPath))
                {
                    await WriteTextResponseAsync(stream, 404, "Not Found", "Video file is not available.", cancellationToken);
                    return;
                }

                await WriteVideoResponseAsync(stream, _videoPath, rangeHeader, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Error("Local video request failed", ex);
            }
        }
    }

    private static async Task WriteVideoResponseAsync(
        NetworkStream stream,
        string videoPath,
        string? rangeHeader,
        CancellationToken cancellationToken)
    {
        var fileInfo = new FileInfo(videoPath);
        var totalLength = fileInfo.Length;
        var start = 0L;
        var end = totalLength - 1;
        var partial = TryParseRange(rangeHeader, totalLength, out var parsedStart, out var parsedEnd);

        if (partial)
        {
            start = parsedStart;
            end = parsedEnd;
        }

        var contentLength = end - start + 1;
        var status = partial ? "206 Partial Content" : "200 OK";
        var header = new StringBuilder()
            .Append("HTTP/1.1 ").Append(status).Append("\r\n")
            .Append("Content-Type: ").Append(GetContentType(videoPath)).Append("\r\n")
            .Append("Accept-Ranges: bytes\r\n")
            .Append("Cache-Control: no-store\r\n")
            .Append("Content-Length: ").Append(contentLength).Append("\r\n");

        if (partial)
        {
            header.Append("Content-Range: bytes ").Append(start).Append('-').Append(end).Append('/').Append(totalLength).Append("\r\n");
        }

        header.Append("Connection: close\r\n\r\n");
        var headerBytes = Encoding.ASCII.GetBytes(header.ToString());
        await stream.WriteAsync(headerBytes, cancellationToken);

        var buffer = new byte[1024 * 128];
        await using var file = new FileStream(videoPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        file.Position = start;
        var remaining = contentLength;

        while (remaining > 0)
        {
            var read = await file.ReadAsync(buffer.AsMemory(0, (int)Math.Min(buffer.Length, remaining)), cancellationToken);
            if (read <= 0)
            {
                break;
            }

            await stream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            remaining -= read;
        }
    }

    private static bool TryParseRange(string? rangeHeader, long totalLength, out long start, out long end)
    {
        start = 0;
        end = totalLength - 1;

        if (string.IsNullOrWhiteSpace(rangeHeader) || !rangeHeader.StartsWith("bytes=", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var range = rangeHeader["bytes=".Length..];
        var parts = range.Split('-', 2);
        if (parts.Length != 2)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(parts[0]) && long.TryParse(parts[0], out var parsedStart))
        {
            start = Math.Clamp(parsedStart, 0, totalLength - 1);
        }

        if (!string.IsNullOrWhiteSpace(parts[1]) && long.TryParse(parts[1], out var parsedEnd))
        {
            end = Math.Clamp(parsedEnd, start, totalLength - 1);
        }

        return true;
    }

    private static async Task WriteTextResponseAsync(
        NetworkStream stream,
        int code,
        string reason,
        string message,
        CancellationToken cancellationToken)
    {
        var body = Encoding.UTF8.GetBytes(message);
        var header = Encoding.ASCII.GetBytes(
            $"HTTP/1.1 {code} {reason}\r\nContent-Type: text/plain; charset=utf-8\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n");

        await stream.WriteAsync(header, cancellationToken);
        await stream.WriteAsync(body, cancellationToken);
    }

    private static string GetContentType(string path)
    {
        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".mp4" => "video/mp4",
            ".m4v" => "video/mp4",
            ".mov" => "video/quicktime",
            ".webm" => "video/webm",
            ".avi" => "video/x-msvideo",
            ".wmv" => "video/x-ms-wmv",
            ".mkv" => "video/x-matroska",
            _ => "application/octet-stream"
        };
    }
}
