using FluentAssertions;
using McpHealthServer.Core;
using McpHealthServer.Security;
using McpHealthServer.Tools;
using System.Net;
using System.Text.Json;
using McpHealthServer.Tests.Fake;
using Xunit;

namespace McpHealthServer.Tests;

public class ToolTests
{
    [Fact]
    public async Task CheckApiStatus_Should_Return_UP_For_Valid_Url()
    {
        var session = new McpSession("test");
        var tool = new CheckApiStatusTool(
            new FakeHttpClientFactory(HttpStatusCode.OK),
            new AllowAllSsrfPolicy());

        var input = JsonDocument.Parse("""
        { "url": "https://example.com" }
        """).RootElement;

        await tool.ExecuteAsync(input, session);

        var evt = await session.EventChannel.Reader.ReadAsync();

        evt.Type.Should().Be("tool.result");
        evt.Payload.ToString().Should().Contain("UP");
    }

    [Fact]
    public async Task SsrfPolicy_Should_Block_Localhost()
    {
        var policy = new DefaultSsrfPolicy();

        Func<Task> act = () => policy.ValidateAsync("http://localhost");

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}