using McpHealthServer.Core;
using McpHealthServer.Security;
using System.Diagnostics;
using System.Text.Json;

namespace McpHealthServer.Tools;

public class CheckApiStatusTool : ITool
{
    public string Name => "check_api_status";

    private readonly IHttpClientFactory _http;
    private readonly ISsrfPolicy _ssrf;

    public CheckApiStatusTool(
        IHttpClientFactory http,
        ISsrfPolicy ssrf)
    {
        _http = http;
        _ssrf = ssrf;
    }

    public async Task ExecuteAsync(JsonElement input, McpSession session)
    {
        var url = input.GetProperty("url").GetString()!;
        await _ssrf.ValidateAsync(url);

        var client = _http.CreateClient("probe");
        var sw = Stopwatch.StartNew();

        try
        {
            var resp = await client.GetAsync(url);
            sw.Stop();

            await session.EventChannel.Writer.WriteAsync(
                new McpEvent("tool.result", new
                {
                    url,
                    status = resp.IsSuccessStatusCode ? "UP" : "DOWN",
                    http_status = (int)resp.StatusCode,
                    latency_ms = sw.ElapsedMilliseconds,
                    checked_at = DateTime.UtcNow
                }, DateTime.UtcNow));
        }
        catch (Exception ex)
        {
            await session.EventChannel.Writer.WriteAsync(
                new McpEvent("tool.result", new
                {
                    url,
                    status = "DOWN",
                    error = ex.Message,
                    checked_at = DateTime.UtcNow
                }, DateTime.UtcNow));
        }
    }
}