namespace McpHealthServer.Core.Configuration;

public class SecurityOptions
{
    public bool EnableSsrfProtection { get; set; } = true;
    public List<string> AllowedDomains { get; set; } = new();
}
