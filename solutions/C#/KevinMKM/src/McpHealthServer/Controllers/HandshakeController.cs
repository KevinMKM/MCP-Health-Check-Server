using McpHealthServer.Core;
using McpHealthServer.Core.Interfaces;
using McpHealthServer.SSE;
using Microsoft.AspNetCore.Mvc;

namespace McpHealthServer.Controllers;

[ApiController]
[Route("mcp/sessions")]
public class HandshakeController : ControllerBase
{
    private readonly ISessionManager _sessions;

    public HandshakeController(ISessionManager sessions)
    {
        _sessions = sessions;
    }

    [HttpGet("{sessionId}/events")]
    public async Task Events(string sessionId)
    {
        var session = _sessions.Get(sessionId);

        Response.Headers.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";

        await SseWriter.WriteAsync(Response,
            new McpEvent("handshake.ready", new { sessionId }, DateTime.UtcNow));

        await foreach (var evt in session.EventChannel.Reader.ReadAllAsync(HttpContext.RequestAborted))
        {
            await SseWriter.WriteAsync(Response, evt);
        }
    }
}