using McpHealthServer.Security;

namespace McpHealthServer.Tests.Fake;

public class AllowAllSsrfPolicy : ISsrfPolicy
{
    public Task ValidateAsync(string url) => Task.CompletedTask;
}