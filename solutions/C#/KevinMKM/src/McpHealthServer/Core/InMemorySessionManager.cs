using System.Collections.Concurrent;
using McpHealthServer.Core.Interfaces;

namespace McpHealthServer.Core;

public class InMemorySessionManager : ISessionManager
{
    private readonly ConcurrentDictionary<string, McpSession> _sessions = new();

    public McpSession Create()
    {
        var id = Guid.NewGuid().ToString("N");
        var session = new McpSession(id);
        _sessions[id] = session;
        return session;
    }

    public McpSession Get(string sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
            throw new KeyNotFoundException("Session not found");

        session.Touch();
        return session;
    }

    public void Remove(string sessionId)
    {
        _sessions.TryRemove(sessionId, out _);
    }

    public IEnumerable<McpSession> All() => _sessions.Values;
}