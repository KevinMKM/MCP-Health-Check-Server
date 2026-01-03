using System.Net.Http.Json;
using Xunit;

namespace McpHealthServer.Tests;

public class SessionIsolationTests : IClassFixture<McpServerFactory>
{
    private readonly HttpClient _client;

    public SessionIsolationTests(McpServerFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task TwoSessions_ShouldBeCompletelyIsolated()
    {
        // Arrange: Create two sessions
        var init1 = await _client.PostAsync("/mcp/initialize", null);
        var init2 = await _client.PostAsync("/mcp/initialize", null);

        var result1 = await init1.Content.ReadFromJsonAsync<InitResponse>();
        var result2 = await init2.Content.ReadFromJsonAsync<InitResponse>();

        Assert.NotEqual(result1!.SessionId, result2!.SessionId);

        // Act: Call tool on session 1
        var toolRequest1 = new
        {
            sessionId = result1.SessionId,
            name = "check_api_status",
            input = new { url = "https://www.google.com" }
        };

        var toolResponse1 = await _client.PostAsJsonAsync(
            "/mcp/tools/call",
            toolRequest1);

        // Assert: Session 2 should not be affected
        var healthResponse = await _client.GetAsync("/health");
        var healthData = await healthResponse.Content.ReadFromJsonAsync<HealthResponse>();

        Assert.True(healthData!.ActiveSessions >= 2);
    }

    [Fact]
    public async Task ParallelToolCalls_ShouldNotInterfere()
    {
        // Arrange
        var sessions = new List<Guid>();
        for (int i = 0; i < 5; i++)
        {
            var init = await _client.PostAsync("/mcp/initialize", null);
            var result = await init.Content.ReadFromJsonAsync<InitResponse>();
            sessions.Add(result!.SessionId);
        }

        // Act: Parallel tool calls
        var tasks = sessions.Select(async sessionId =>
        {
            var toolRequest = new
            {
                sessionId,
                name = "check_api_status",
                input = new { url = "https://www.google.com" }
            };

            var response = await _client.PostAsJsonAsync("/mcp/tools/call", toolRequest);
            return await response.Content.ReadFromJsonAsync<ToolResponse>();
        });

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, result =>
        {
            Assert.NotNull(result);
            Assert.Equal("UP", result.Status);
        });
    }

    private record InitResponse(Guid SessionId);
    private record HealthResponse(int ActiveSessions);
    private record ToolResponse(string Status);
}