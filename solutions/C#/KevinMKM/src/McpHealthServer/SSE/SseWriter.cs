using System.Text;
using System.Text.Json;

namespace McpHealthServer.SSE;
public class SseWriter
{
    private readonly HttpResponse _response;
    private readonly ILogger<SseWriter> _logger;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private bool _isDisposed;

    public SseWriter(HttpResponse response, ILogger<SseWriter> logger)
    {
        _response = response ?? throw new ArgumentNullException(nameof(response));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    public void Initialize()
    {
        _response.ContentType = "text/event-stream";
        _response.Headers.CacheControl = "no-cache";
        _response.Headers.Connection = "keep-alive";

        // Disable response buffering for real-time streaming
        _response.Headers["X-Accel-Buffering"] = "no";
    }
    public async Task SendEventAsync(string eventType, object data, CancellationToken cancellationToken = default)
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(SseWriter));
        }

        if (string.IsNullOrWhiteSpace(eventType))
        {
            throw new ArgumentException("Event type cannot be null or empty", nameof(eventType));
        }

        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            // Serialize data to JSON
            var jsonData = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            // Build SSE message according to spec:
            // event: <eventType>\n
            // data: <jsonData>\n
            // \n
            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine($"event: {eventType}");
            messageBuilder.AppendLine($"data: {jsonData}");
            messageBuilder.AppendLine(); // Empty line to mark end of message

            var messageBytes = Encoding.UTF8.GetBytes(messageBuilder.ToString());

            // Write to response stream
            await _response.Body.WriteAsync(messageBytes, cancellationToken);
            await _response.Body.FlushAsync(cancellationToken);

            _logger.LogDebug(
                "SSE event sent: {EventType}, Data length: {Length} bytes",
                eventType,
                jsonData.Length);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("SSE write cancelled for event: {EventType}", eventType);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send SSE event: {EventType}",
                eventType);
            throw;
        }
        finally
        {
            _writeLock.Release();
        }
    }
    
    public async Task SendKeepAliveAsync(CancellationToken cancellationToken = default)
    {
        if (_isDisposed)
        {
            return;
        }

        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            // SSE comment format: ": <comment>\n\n"
            var keepAliveMessage = Encoding.UTF8.GetBytes(": keepalive\n\n");
            await _response.Body.WriteAsync(keepAliveMessage, cancellationToken);
            await _response.Body.FlushAsync(cancellationToken);

            _logger.LogTrace("SSE keepalive sent");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send SSE keepalive");
        }
        finally
        {
            _writeLock.Release();
        }
    }
    
    public async Task SendEventsAsync(IEnumerable<(string eventType, object data)> events, CancellationToken cancellationToken = default)
    {
        foreach (var (eventType, data) in events)
        {
            await SendEventAsync(eventType, data, cancellationToken);
        }
    }
    public Task SendHandshakeAsync(object handshakeData, CancellationToken cancellationToken = default)
    {
        return SendEventAsync("handshake", handshakeData, cancellationToken);
    }
    
    public Task SendReadyAsync(object readyData, CancellationToken cancellationToken = default)
    {
        return SendEventAsync("ready", readyData, cancellationToken);
    }
    
    public Task SendToolResultAsync(object resultData, CancellationToken cancellationToken = default)
    {
        return SendEventAsync("tool_result", resultData, cancellationToken);
    }
    
    public Task SendErrorAsync(object errorData, CancellationToken cancellationToken = default)
    {
        return SendEventAsync("error", errorData, cancellationToken);
    }
    
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _writeLock.Dispose();

        _logger.LogDebug("SseWriter disposed");
    }
}
