using McpHealthServer.Core;
using McpHealthServer.Core.Interfaces;
using Xunit;

namespace McpHealthServer.Tests;

public class SessionStoreTests
{
    private readonly ISessionManager _sessionManager;

    public SessionStoreTests()
    {
        _sessionManager = new InMemorySessionManager();
    }

    [Fact]
    public void CreateSession_ShouldReturnNewSession()
    {
        // Act
        var session = _sessionManager.CreateSession();

        // Assert
        Assert.NotNull(session);
        Assert.NotEqual(Guid.Empty, session.SessionId);
        Assert.True(_sessionManager.TryGetSession(session.SessionId, out _));
    }

    [Fact]
    public void CreateSession_ShouldGenerateUniqueSessionIds()
    {
        // Act
        var session1 = _sessionManager.CreateSession();
        var session2 = _sessionManager.CreateSession();

        // Assert
        Assert.NotEqual(session1.SessionId, session2.SessionId);
    }

    [Fact]
    public void TryGetSession_WithInvalidId_ShouldReturnFalse()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var result = _sessionManager.TryGetSession(invalidId, out var session);

        // Assert
        Assert.False(result);
        Assert.Null(session);
    }

    [Fact]
    public void RemoveSession_ShouldRemoveSessionFromStore()
    {
        // Arrange
        var session = _sessionManager.CreateSession();

        // Act
        var removed = _sessionManager.Remove(session.SessionId);

        // Assert
        Assert.True(removed);
        Assert.False(_sessionManager.TryGetSession(session.SessionId, out _));
    }

    [Fact]
    public void RemoveExpiredSessions_ShouldRemoveOnlyExpiredSessions()
    {
        // Arrange
        var session1 = _sessionManager.CreateSession();
        var session2 = _sessionManager.CreateSession();

        // Simulate session1 being old
        session1.GetType()
            .GetProperty("LastActivity")!
            .SetValue(session1, DateTime.UtcNow.AddHours(-2));

        var ttl = TimeSpan.FromMinutes(30);

        // Act
        var removed = _sessionManager.RemoveExpiredSessions(ttl);

        // Assert
        Assert.Equal(1, removed);
        Assert.False(_sessionManager.TryGetSession(session1.SessionId, out _));
        Assert.True(_sessionManager.TryGetSession(session2.SessionId, out _));
    }

    [Fact]
    public void GetActiveSessionCount_ShouldReturnCorrectCount()
    {
        // Arrange
        var session1 = _sessionManager.CreateSession();
        var session2 = _sessionManager.CreateSession();

        // Act
        var count = _sessionManager.GetActiveSessionCount();

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public void ConcurrentAccess_ShouldBeThreadSafe()
    {
        // Arrange
        var tasks = new List<Task<McpSession>>();

        // Act
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(() => _sessionManager.CreateSession()));
        }

        Task.WaitAll(tasks.ToArray());
        var sessions = tasks.Select(t => t.Result).ToList();

        // Assert
        Assert.Equal(100, sessions.Count);
        Assert.Equal(100, sessions.Select(s => s.SessionId).Distinct().Count());
    }
}