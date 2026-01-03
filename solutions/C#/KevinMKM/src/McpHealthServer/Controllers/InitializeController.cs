using McpHealthServer.Core.Interfaces;
using McpHealthServer.Tools;
using Microsoft.AspNetCore.Mvc;

namespace McpHealthServer.Controllers;

[ApiController]
[Route("mcp/initialize")]
public class InitializeController : ControllerBase
{
    private readonly ISessionManager _sessionManager;
    private readonly ILogger<InitializeController> _logger;

    public InitializeController(ISessionManager sessions, ILogger<InitializeController> logger)
    {
        _logger = logger;
        _sessionManager = sessions;
    }

    [HttpPost]
    public IActionResult Initialize()
    {
        try
        {
            var session = _sessionManager.CreateSession();

            _logger.LogInformation(
                "New session initialized: {SessionId}",
                session.SessionId);

            return Ok(new
            {
                protocol = "mcp/0.1",
                session_id = session.SessionId,
                server_info = new
                {
                    name = "MCP Health Check Server",
                    version = "1.0.0"
                },
                capabilities = new
                {
                    tools = new { }
                },
                tools = new List<CheckApiStatusTool>{new()},
                sse_endpoint = $"/mcp/handshake/{session.SessionId}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize session");
            return StatusCode(500, new { error = "Failed to create session" });
        }
    }
}