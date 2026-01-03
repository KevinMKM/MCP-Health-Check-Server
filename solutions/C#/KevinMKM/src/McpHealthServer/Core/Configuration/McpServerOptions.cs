namespace McpHealthServer.Core.Configuration;

public class McpServerOptions
{
    public const string SectionName = "McpServer";
    public SessionCleanupOptions SessionCleanup { get; set; } = new();
    public HealthCheckOptions HealthCheck { get; set; } = new();
    public SecurityOptions Security { get; set; } = new();
}