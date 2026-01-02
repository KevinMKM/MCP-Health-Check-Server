using FluentAssertions;
using McpHealthServer.Core;
using Xunit;

namespace McpHealthServer.Tests;

public class SessionIsolationTests
{
    [Fact]
    public async Task Events_Should_Be_Isolated_Per_Session()
    {
        var sessions = new InMemorySessionManager();

        var s1 = sessions.Create();
        var s2 = sessions.Create();

        await s1.EventChannel.Writer.WriteAsync(new McpEvent("test", new { value = 1 }, DateTime.UtcNow));

        await s2.EventChannel.Writer.WriteAsync(new McpEvent("test", new { value = 2 }, DateTime.UtcNow));

        var e1 = await s1.EventChannel.Reader.ReadAsync();
        var e2 = await s2.EventChannel.Reader.ReadAsync();

        e1.Payload.Should().BeEquivalentTo(new { value = 1 });
        e2.Payload.Should().BeEquivalentTo(new { value = 2 });
    }
}