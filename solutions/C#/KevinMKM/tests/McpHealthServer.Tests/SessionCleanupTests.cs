using McpHealthServer.Core;
using Xunit;

namespace McpHealthServer.Tests;

public class SessionCleanupTests
{
    [Fact]
    public void IsExpired_WithRecentActivity_ShouldReturnFalse()
    {
        // Arrange
        var session = new McpSession(Guid.NewGuid());
        session.UpdateActivity();
        var ttl = TimeSpan.FromMinutes(30);

        // Act
        var isExpired = session.IsExpired(ttl);

        // Assert
        Assert.False(isExpired);
    }

    [Fact]
    public void IsExpired_WithOldActivity_ShouldReturnTrue()
    {
        // Arrange
        var session = new McpSession(Guid.NewGuid());

        // Use reflection to set LastActivity to 1 hour ago
        var lastActivityProperty = typeof(McpSession)
            .GetProperty("LastActivity",
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance);

        lastActivityProperty!.SetValue(session, DateTime.UtcNow.AddHours(-1));

        var ttl = TimeSpan.FromMinutes(30);

        // Act
        var isExpired = session.IsExpired(ttl);

        // Assert
        Assert.True(isExpired);
    }

    [Fact]
    public void UpdateActivity_ShouldRefreshLastActivity()
    {
        // Arrange
        var session = new McpSession(Guid.NewGuid());
        var initialActivity = session.LastActivity;

        Thread.Sleep(100); // Ensure time passes

        // Act
        session.UpdateActivity();

        // Assert
        Assert.True(session.LastActivity > initialActivity);
    }

    [Fact]
    public void Dispose_ShouldClearAllResources()
    {
        // Arrange
        var session = new McpSession(Guid.NewGuid());
        session.Metadata["test_key"] = "test_value";
        session.LogEvent("TEST_EVENT", new { data = "test" });

        // Act
        session.Dispose();

        // Assert
        Assert.Empty(session.Metadata);
        Assert.Empty(session.Events);
        Assert.False(session.IsConnected);
    }
}