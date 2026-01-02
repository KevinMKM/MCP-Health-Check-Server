namespace McpHealthServer.Security;

public interface ISsrfPolicy
{
    Task ValidateAsync(string url);
}