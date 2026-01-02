using McpHealthServer.Core;

namespace McpHealthServer.SSE;

public static class SseWriter
{
    public static async Task WriteAsync(HttpResponse response, McpEvent evt)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(evt);

        await response.WriteAsync($"event: {evt.Type}\n");
        await response.WriteAsync($"data: {json}\n\n");
        await response.Body.FlushAsync();
    }
}