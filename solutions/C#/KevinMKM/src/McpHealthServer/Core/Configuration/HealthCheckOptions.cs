namespace McpHealthServer.Core.Configuration;

public class HealthCheckOptions
{
    public int TimeoutMilliseconds { get; set; } = 3000;
    public int MaxRedirects { get; set; } = 3;
}