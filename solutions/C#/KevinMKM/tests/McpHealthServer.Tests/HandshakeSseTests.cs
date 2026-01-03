using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace McpHealthServer.Tests;

public class HandshakeSseTests : IClassFixture<McpServerFactory>
{
    private readonly HttpClient _client;

    public HandshakeSseTests(McpServerFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Handshake_WithValidSession_ShouldOpenSseStream()
    {
        // Arrange: Create a session
        var initResponse = await _client.PostAsync("/mcp/initialize", null);
        var initResult = await initResponse.Content.ReadFromJsonAsync<InitResponse>();
        var sessionId = initResult!.SessionId;

        // Act: Connect to SSE
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/mcp/handshake?session_id={sessionId}");
        request.Headers.Add("Accept", "text/event-stream");

        var response = await _client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cts.Token);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/event-stream",
            response.Content.Headers.ContentType?.MediaType);

        // Read first event (should be handshake)
        await using var stream = await response.Content.ReadAsStreamAsync(cts.Token);
        using var reader = new StreamReader(stream);

        var line1 = await reader.ReadLineAsync(cts.Token);
        var line2 = await reader.ReadLineAsync(cts.Token);

        Assert.Contains("event:", line1);
        Assert.Contains("data:", line2);
    }

    [Fact]
    public async Task Handshake_WithInvalidSession_ShouldReturn404()
    {
        // Arrange
        var invalidSessionId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync(
            $"/mcp/handshake?session_id={invalidSessionId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private record InitResponse(Guid SessionId);
}