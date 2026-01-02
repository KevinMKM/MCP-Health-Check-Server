using McpHealthServer.Core.Interfaces;

namespace McpHealthServer.Background;

public class SessionCleanupService : BackgroundService
{
    private readonly ISessionManager _sessions;

    public SessionCleanupService(ISessionManager sessions)
    {
        _sessions = sessions;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var s in _sessions.All())
            {
                if (DateTime.UtcNow - s.LastAccess > TimeSpan.FromMinutes(10))
                    _sessions.Remove(s.SessionId);
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}