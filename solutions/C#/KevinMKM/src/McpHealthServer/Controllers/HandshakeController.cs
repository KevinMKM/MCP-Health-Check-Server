using McpHealthServer.Core.Interfaces;
using McpHealthServer.SSE;
using Microsoft.AspNetCore.Mvc;

namespace McpHealthServer.Controllers;

[ApiController]
[Route("mcp/sessions")]
public class HandshakeController : ControllerBase
{
    private readonly ISessionManager _sessionManager;
    private readonly ILogger<HandshakeController> _logger;
    private readonly ILogger<SseWriter> _sseWriterLogger;

    public HandshakeController(ISessionManager sessions, ILogger<HandshakeController> logger, ILogger<SseWriter> sseWriterLogger)
    {
        _sessionManager = sessions;
        _logger = logger;
        _sseWriterLogger = sseWriterLogger;
    }

    [HttpGet("{sessionId:guid}")]
    public async Task<IActionResult> Handshake(Guid sessionId)
    {
        if (!_sessionManager.TryGetSession(sessionId, out var session))
        {
            _logger.LogWarning(
                "Handshake attempted for non-existent session: {SessionId}",
                sessionId);
            return NotFound(new { error = "Session not found" });
        }

        Response.Headers.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        var writer = new SseWriter(Response, _sseWriterLogger);
        session?.AttachSseWriter(writer);

        _logger.LogInformation(
            "SSE connection established for session: {SessionId}",
            sessionId);

        // Send handshake event
        await writer.SendEventAsync("handshake", new { status = "ready", timestamp = DateTime.UtcNow });

        // Keep connection alive
        try
        {
            await writer.SendKeepAliveAsync(HttpContext.RequestAborted);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation(
                "SSE connection closed for session: {SessionId}",
                sessionId);
        }

        return new EmptyResult();
    }
}