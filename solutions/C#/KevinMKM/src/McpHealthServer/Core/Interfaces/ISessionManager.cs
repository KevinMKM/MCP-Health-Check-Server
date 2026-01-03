namespace McpHealthServer.Core.Interfaces;

public interface ISessionManager
{
    McpSession CreateSession();
    bool TryGetSession(Guid sessionId, out McpSession? session);
    int RemoveExpiredSessions(TimeSpan expiration);
    bool Remove(Guid sessionId);
    int GetActiveSessionCount();
    IEnumerable<McpSession> All();
}