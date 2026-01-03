using McpHealthServer.Core.Configuration;
using McpHealthServer.Security;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text.Json;

namespace McpHealthServer.Tools;

public class CheckApiStatusTool : ITool
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISsrfPolicy _ssrfPolicy;
    private readonly IOptions<McpServerOptions> _options;
    private readonly ILogger<CheckApiStatusTool> _logger;

    public string Name => "check_api_status";

    public string Description =>
        "Checks the availability and health of an HTTP/HTTPS endpoint";

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            url = new
            {
                type = "string",
                description = "The URL to check (must be HTTP or HTTPS)",
                format = "uri"
            }
        },
        required = new[] { "url" }
    };

    public CheckApiStatusTool()
    {

    }

    public CheckApiStatusTool(IHttpClientFactory httpClientFactory, ISsrfPolicy ssrfPolicy, IOptions<McpServerOptions> options, ILogger<CheckApiStatusTool> logger)
    {
        _httpClientFactory = httpClientFactory;
        _ssrfPolicy = ssrfPolicy;
        _options = options;
        _logger = logger;
    }

    public async Task<object> ExecuteAsync(
        JsonElement input,
        CancellationToken cancellationToken = default)
    {
        if (!input.TryGetProperty("url", out var urlElement))
        {
            return new
            {
                status = "ERROR",
                error = "Missing required field: url",
                checked_at = DateTime.UtcNow
            };
        }

        var url = urlElement.GetString();
        if (string.IsNullOrWhiteSpace(url))
        {
            return new
            {
                status = "ERROR",
                error = "URL cannot be empty",
                checked_at = DateTime.UtcNow
            };
        }

        // Validate URL format
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "http" && uri.Scheme != "https"))
        {
            return new
            {
                url,
                status = "ERROR",
                error = "Invalid URL or unsupported scheme (must be HTTP/HTTPS)",
                checked_at = DateTime.UtcNow
            };
        }

        // SSRF Protection
        if (!_ssrfPolicy.IsAllowed(url))
        {
            _logger.LogWarning("SSRF blocked: {Url}", url);
            return new
            {
                url,
                status = "BLOCKED",
                error = "URL blocked by security policy",
                checked_at = DateTime.UtcNow
            };
        }

        // Perform health check
        var sw = Stopwatch.StartNew();
        try
        {
            var timeout = TimeSpan.FromMilliseconds(
                _options.Value.HealthCheck.TimeoutMilliseconds);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            var client = _httpClientFactory.CreateClient();
            client.Timeout = timeout;

            var response = await client.GetAsync(url, cts.Token);
            sw.Stop();

            _logger.LogInformation(
                "Health check: {Url} returned {StatusCode} in {ElapsedMs}ms",
                url,
                (int)response.StatusCode,
                sw.ElapsedMilliseconds);

            return new
            {
                url,
                status = response.IsSuccessStatusCode ? "UP" : "DOWN",
                http_status = (int)response.StatusCode,
                latency_ms = sw.ElapsedMilliseconds,
                checked_at = DateTime.UtcNow
            };
        }
        catch (TaskCanceledException)
        {
            sw.Stop();
            _logger.LogWarning("Health check timeout: {Url}", url);
            return new
            {
                url,
                status = "DOWN",
                error = $"Timeout after {sw.ElapsedMilliseconds}ms",
                checked_at = DateTime.UtcNow
            };
        }
        catch (HttpRequestException ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "Health check failed: {Url}", url);
            return new
            {
                url,
                status = "DOWN",
                error = ex.Message,
                latency_ms = sw.ElapsedMilliseconds,
                checked_at = DateTime.UtcNow
            };
        }
    }
}
