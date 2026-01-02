using System.Threading.Channels;

namespace McpHealthServer.Core;

public class McpSession
{
    public string SessionId { get; }
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
    public DateTime LastAccess { get; private set; } = DateTime.UtcNow;

    public Channel<McpEvent> EventChannel { get; } = Channel.CreateUnbounded<McpEvent>();

    public McpSession(string sessionId)
    {
        SessionId = sessionId;
    }

    public void Touch() => LastAccess = DateTime.UtcNow;
}