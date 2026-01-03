using McpHealthServer.Core.Configuration;
using McpHealthServer.Core.Interfaces;
using Microsoft.Extensions.Options;

namespace McpHealthServer.Background;

public class SessionCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SessionCleanupService> _logger;
    private readonly IOptions<McpServerOptions> _options;

    public SessionCleanupService(
        IServiceProvider serviceProvider,
        ILogger<SessionCleanupService> logger,
        IOptions<McpServerOptions> options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(
            _options.Value.SessionCleanup.CleanupIntervalMinutes);

        _logger.LogInformation(
            "Session cleanup service started. Running every {Interval} minutes",
            _options.Value.SessionCleanup.CleanupIntervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, stoppingToken);
                await CleanupExpiredSessionsAsync();
            }
            catch (OperationCanceledException)
            {
                // Expected when application is stopping
                _logger.LogInformation("Session cleanup service stopping");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during session cleanup");
            }
        }

        _logger.LogInformation("Session cleanup service stopped");
    }

    private async Task CleanupExpiredSessionsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var sessionManager = scope.ServiceProvider
            .GetRequiredService<ISessionManager>();

        var expiration = TimeSpan.FromMinutes(_options.Value.SessionCleanup.ExpirationMinutes);

        var removed = sessionManager.RemoveExpiredSessions(expiration);

        if (removed > 0)
        {
            _logger.LogInformation(
                "Cleaned up {Count} expired session(s) (TTL: {ExpirationMinutes}min)",
                removed,
                _options.Value.SessionCleanup.ExpirationMinutes);
        }

        // Log current state
        var activeCount = sessionManager.GetActiveSessionCount();
        _logger.LogDebug("Active sessions: {Count}", activeCount);

        await Task.CompletedTask; // For consistency
    }
}
