using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace McpHealthServer.Tests;

public class InitializeTests : IClassFixture<McpServerFactory>
{
    private readonly HttpClient _client;

    public InitializeTests(McpServerFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Initialize_ShouldReturnSessionIdAndTools()
    {
        // Act
        var response = await _client.PostAsync("/mcp/initialize", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<InitializeResponse>();
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.SessionId);
        Assert.Equal("1.0", result.ProtocolVersion);
        Assert.Contains("check_api_status", result.Tools.Select(t => t.Name));
    }

    [Fact]
    public async Task Initialize_MultipleTimes_ShouldReturnDifferentSessionIds()
    {
        // Act
        var response1 = await _client.PostAsync("/mcp/initialize", null);
        var response2 = await _client.PostAsync("/mcp/initialize", null);

        var result1 = await response1.Content.ReadFromJsonAsync<InitializeResponse>();
        var result2 = await response2.Content.ReadFromJsonAsync<InitializeResponse>();

        // Assert
        Assert.NotEqual(result1!.SessionId, result2!.SessionId);
    }

    private record InitializeResponse(
        Guid SessionId,
        string ProtocolVersion,
        List<ToolInfo> Tools);

    private record ToolInfo(string Name, string Description);
}