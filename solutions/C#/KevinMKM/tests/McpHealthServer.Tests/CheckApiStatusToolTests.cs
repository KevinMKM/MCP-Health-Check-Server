using McpHealthServer.Security;
using McpHealthServer.Tools;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using System.Net;
using Xunit;

namespace McpHealthServer.Tests;

public class CheckApiStatusToolTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly ISsrfPolicy _allowAllPolicy;
    private readonly CheckApiStatusTool _tool;

    public CheckApiStatusToolTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _allowAllPolicy = new AllowAllSsrfPolicy();
        _tool = new CheckApiStatusTool(
            _httpClient,
            _allowAllPolicy,
            NullLogger<CheckApiStatusTool>.Instance);
    }

    [Fact]
    public async Task ExecuteAsync_WithSuccessfulResponse_ShouldReturnUp()
    {
        // Arrange
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK
            });

        var input = new Dictionary<string, object>
        {
            ["url"] = "https://example.com/health"
        };

        // Act
        var result = await _tool.ExecuteAsync(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("UP", result["status"]);
        Assert.Equal(200, result["http_status"]);
        Assert.Contains("latency_ms", result.Keys);
    }

    [Fact]
    public async Task ExecuteAsync_WithTimeout_ShouldReturnDown()
    {
        // Arrange
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Timeout"));

        var input = new Dictionary<string, object>
        {
            ["url"] = "https://example.com/health"
        };

        // Act
        var result = await _tool.ExecuteAsync(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("DOWN", result["status"]);
        Assert.Contains("timeout", result["error"].ToString()!,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidUrl_ShouldReturnError()
    {
        // Arrange
        var input = new Dictionary<string, object>
        {
            ["url"] = "not-a-valid-url"
        };

        // Act
        var result = await _tool.ExecuteAsync(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("DOWN", result["status"]);
        Assert.Contains("error", result.Keys);
    }

    [Fact]
    public async Task ExecuteAsync_WithHttpError_ShouldReturnDown()
    {
        // Arrange
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        var input = new Dictionary<string, object>
        {
            ["url"] = "https://example.com/health"
        };

        // Act
        var result = await _tool.ExecuteAsync(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("DOWN", result["status"]);
        Assert.Equal(500, result["http_status"]);
    }

    private class AllowAllSsrfPolicy : ISsrfPolicy
    {
        public bool IsAllowed(string url) => true;
    }
}