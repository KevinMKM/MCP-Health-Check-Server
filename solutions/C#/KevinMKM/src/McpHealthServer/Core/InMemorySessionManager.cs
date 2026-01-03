using System.Collections.Concurrent;
using McpHealthServer.Core.Interfaces;

namespace McpHealthServer.Core;

public class InMemorySessionManager : ISessionManager
{
    private readonly ConcurrentDictionary<Guid, McpSession> _sessions = new();

    public McpSession Create()
    {
        var id = Guid.NewGuid();
        var session = new McpSession(id);
        _sessions[id] = session;
        return session;
    }

    public McpSession? Get(Guid sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
            throw new KeyNotFoundException("Session not found");

        session.UpdateActivity();
        return session;
    }

    public bool Remove(Guid sessionId)
    {
        return _sessions.TryRemove(sessionId, out _);
    }

    public IEnumerable<McpSession> All() => _sessions.Values;

    public McpSession CreateSession()
    {
        return Create();
    }

    public bool TryGetSession(Guid sessionId, out McpSession? session)
    {
        try
        {
            session = Get(sessionId);
            return true;
        }
        catch
        {
            session = null;
            return false;
        }
    }

    public int RemoveExpiredSessions(TimeSpan expiration)
    {
        return _sessions.Where(s => s.Value.CreatedAt < DateTime.Now.AddMinutes(-1 * expiration.TotalMinutes))
            .Select(s => s.Key)
            .Sum(key => _sessions.Remove(key, out _) ? 1 : 0);
    }

    public int GetActiveSessionCount()
    {
        return _sessions.Count;
    }
}