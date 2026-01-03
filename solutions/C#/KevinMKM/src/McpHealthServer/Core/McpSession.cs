using System.Collections.Concurrent;
using McpHealthServer.SSE;

namespace McpHealthServer.Core;

public class McpSession
{
    private readonly object _lock = new();
    private SseWriter? _sseWriter;
    private DateTime _lastActivity;
    public Guid SessionId { get; }
    public DateTime CreatedAt { get; }
    public DateTime LastActivity
    {
        get
        {
            lock (_lock)
            {
                return _lastActivity;
            }
        }
        private set
        {
            lock (_lock)
            {
                _lastActivity = value;
            }
        }
    }
    public bool IsConnected
    {
        get
        {
            lock (_lock)
            {
                return _sseWriter != null;
            }
        }
    }
    public ConcurrentDictionary<string, object> Metadata { get; }
    public ConcurrentQueue<McpEvent> Events { get; }
    
    public McpSession(Guid sessionId)
    {
        SessionId = sessionId;
        CreatedAt = DateTime.UtcNow;
        LastActivity = CreatedAt;
        Metadata = new ConcurrentDictionary<string, object>();
        Events = new ConcurrentQueue<McpEvent>();
    }
    public void AttachSseWriter(SseWriter sseWriter)
    {
        if (sseWriter == null)
        {
            throw new ArgumentNullException(nameof(sseWriter));
        }

        lock (_lock)
        {
            if (_sseWriter != null)
            {
                throw new InvalidOperationException(
                    $"Session {SessionId} already has an attached SSE writer");
            }

            _sseWriter = sseWriter;
            UpdateActivity();
        }

        LogEvent("SSE_CONNECTED", new { timestamp = DateTime.UtcNow });
    }
    public void DetachSseWriter()
    {
        lock (_lock)
        {
            if (_sseWriter != null)
            {
                _sseWriter.Dispose();
                _sseWriter = null;
                UpdateActivity();
            }
        }

        LogEvent("SSE_DISCONNECTED", new { timestamp = DateTime.UtcNow });
    }
    public async Task SendEventAsync(string eventType, object data, CancellationToken cancellationToken = default)
    {
        SseWriter? writer;
        lock (_lock)
        {
            writer = _sseWriter;
            UpdateActivity();
        }

        if (writer == null)
        {
            throw new InvalidOperationException(
                $"Session {SessionId} has no attached SSE writer");
        }

        await writer.SendEventAsync(eventType, data, cancellationToken);
        LogEvent(eventType, data);
    }
    public void UpdateActivity()
    {
        LastActivity = DateTime.UtcNow;
    }
    public bool IsExpired(TimeSpan ttl)
    {
        return DateTime.UtcNow - LastActivity > ttl;
    }
    public void LogEvent(string eventType, object? data = null)
    {
        var mcpEvent = new McpEvent(eventType,data,DateTime.UtcNow);

        Events.Enqueue(mcpEvent);

        // Keep only last 100 events to prevent memory leak
        while (Events.Count > 100)
        {
            Events.TryDequeue(out _);
        }
    }
    public object GetStatistics()
    {
        return new
        {
            session_id = SessionId,
            created_at = CreatedAt,
            last_activity = LastActivity,
            is_connected = IsConnected,
            event_count = Events.Count,
            age_seconds = (DateTime.UtcNow - CreatedAt).TotalSeconds,
            idle_seconds = (DateTime.UtcNow - LastActivity).TotalSeconds
        };
    }
    public void Dispose()
    {
        DetachSseWriter();
        Metadata.Clear();

        // Clear events
        while (Events.TryDequeue(out _)) { }

        LogEvent("SESSION_DISPOSED", new { timestamp = DateTime.UtcNow });
    }
}
