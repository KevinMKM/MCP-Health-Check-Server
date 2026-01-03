namespace McpHealthServer.Core.Configuration;

public class SessionCleanupOptions
{
    public int ExpirationMinutes { get; set; } = 30;
    public int CleanupIntervalMinutes { get; set; } = 5;
}