namespace McpHealthServer.Core.Interfaces;

public interface ISessionManager
{
    McpSession Create();
    McpSession Get(string sessionId);
    void Remove(string sessionId);
    IEnumerable<McpSession> All();
}