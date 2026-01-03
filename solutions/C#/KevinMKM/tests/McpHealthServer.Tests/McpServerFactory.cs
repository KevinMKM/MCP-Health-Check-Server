using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace McpHealthServer.Tests;

public class McpServerFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Override configurations for testing if needed
            // Example: use in-memory database, mock external services, etc.
        });

        builder.UseEnvironment("Testing");
    }
}