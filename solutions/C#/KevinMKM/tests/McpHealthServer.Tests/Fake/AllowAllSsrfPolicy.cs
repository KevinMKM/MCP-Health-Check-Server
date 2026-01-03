using McpHealthServer.Security;

namespace McpHealthServer.Tests.Fake;

public class AllowAllSsrfPolicy : ISsrfPolicy
{
    public bool IsAllowed(string url) => true;
}