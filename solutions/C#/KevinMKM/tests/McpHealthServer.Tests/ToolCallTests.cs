using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace McpHealthServer.Tests;

public class ToolCallTests : IClassFixture<McpServerFactory>
{
    private readonly HttpClient _client;

    public ToolCallTests(McpServerFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ToolCall_CheckApiStatus_WithValidUrl_ShouldReturnUp()
    {
        // Arrange: Create session
        var initResponse = await _client.PostAsync("/mcp/initialize", null);
        var initResult = await initResponse.Content.ReadFromJsonAsync<InitResponse>();
        var sessionId = initResult!.SessionId;

        var toolRequest = new
        {
            sessionId,
            name = "check_api_status",
            input = new { url = "https://www.google.com" }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/mcp/tools/call", toolRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ToolResponse>();
        Assert.NotNull(result);
        Assert.Equal("UP", result.Status);
        Assert.Equal(200, result.HttpStatus);
        Assert.True(result.LatencyMs >= 0);
    }

    [Fact]
    public async Task ToolCall_WithInvalidUrl_ShouldReturnDown()
    {
        // Arrange
        var initResponse = await _client.PostAsync("/mcp/initialize", null);
        var initResult = await initResponse.Content.ReadFromJsonAsync<InitResponse>();
        var sessionId = initResult!.SessionId;

        var toolRequest = new
        {
            sessionId,
            name = "check_api_status",
            input = new { url = "http://invalid-domain-12345.com" }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/mcp/tools/call", toolRequest);

        // Assert
        var result = await response.Content.ReadFromJsonAsync<ToolResponse>();
        Assert.NotNull(result);
        Assert.Equal("DOWN", result.Status);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task ToolCall_WithBlockedUrl_ShouldReturnError()
    {
        // Arrange
        var initResponse = await _client.PostAsync("/mcp/initialize", null);
        var initResult = await initResponse.Content.ReadFromJsonAsync<InitResponse>();
        var sessionId = initResult!.SessionId;

        var toolRequest = new
        {
            sessionId,
            name = "check_api_status",
            input = new { url = "http://localhost:5000" }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/mcp/tools/call", toolRequest);

        // Assert
        var result = await response.Content.ReadFromJsonAsync<ToolResponse>();
        Assert.NotNull(result);
        Assert.Equal("DOWN", result.Status);
        Assert.Contains("blocked", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    private record InitResponse(Guid SessionId);
    private record ToolResponse(
        string Status,
        int? HttpStatus,
        int? LatencyMs,
        string? Error);
}