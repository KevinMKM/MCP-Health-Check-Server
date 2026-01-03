using McpHealthServer.Security;
using Microsoft.Extensions.Logging;
using Xunit;

namespace McpHealthServer.Tests;

public class SsrfPolicyTests
{
    private readonly DefaultSsrfPolicy _policy;
    private readonly ILogger<DefaultSsrfPolicy> _logger;

    public SsrfPolicyTests(ILogger<DefaultSsrfPolicy> logger)
    {
        _policy = new DefaultSsrfPolicy(logger);
    }

    [Theory]
    [InlineData("http://localhost/api")]
    [InlineData("http://127.0.0.1/api")]
    [InlineData("http://127.0.0.2/api")]
    [InlineData("http://[::1]/api")]
    [InlineData("http://[::ffff:127.0.0.1]/api")]
    public void IsAllowed_WithLoopbackAddress_ShouldReturnFalse(string url)
    {
        // Act
        var result = _policy.IsAllowed(url);

        // Assert
        Assert.False(result, $"URL {url} should be blocked (loopback)");
    }

    [Theory]
    [InlineData("http://10.0.0.1/api")]
    [InlineData("http://172.16.0.1/api")]
    [InlineData("http://192.168.1.1/api")]
    [InlineData("http://192.168.255.255/api")]
    public void IsAllowed_WithPrivateIpv4_ShouldReturnFalse(string url)
    {
        // Act
        var result = _policy.IsAllowed(url);

        // Assert
        Assert.False(result, $"URL {url} should be blocked (private IPv4)");
    }

    [Theory]
    [InlineData("http://[fc00::1]/api")]
    [InlineData("http://[fd00::1]/api")]
    [InlineData("http://[fe80::1]/api")]
    public void IsAllowed_WithPrivateIpv6_ShouldReturnFalse(string url)
    {
        // Act
        var result = _policy.IsAllowed(url);

        // Assert
        Assert.False(result, $"URL {url} should be blocked (private IPv6)");
    }

    [Theory]
    [InlineData("https://example.com/api")]
    [InlineData("https://google.com")]
    [InlineData("http://8.8.8.8/health")]
    [InlineData("https://1.1.1.1")]
    public void IsAllowed_WithPublicAddress_ShouldReturnTrue(string url)
    {
        // Act
        var result = _policy.IsAllowed(url);

        // Assert
        Assert.True(result, $"URL {url} should be allowed (public)");
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com")]
    [InlineData("")]
    public void IsAllowed_WithInvalidUrl_ShouldReturnFalse(string url)
    {
        // Act
        var result = _policy.IsAllowed(url);

        // Assert
        Assert.False(result, $"URL {url} should be blocked (invalid)");
    }
}