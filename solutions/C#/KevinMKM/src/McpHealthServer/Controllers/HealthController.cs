using McpHealthServer.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace McpHealthServer.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly ISessionManager _sessionManager;

    public HealthController(ISessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }

    [HttpGet]
    public IActionResult Get()
    {
        var sessionCount = _sessionManager.GetActiveSessionCount();

        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            uptime_seconds = Environment.TickCount64 / 1000.0,
            active_sessions = sessionCount,
            server_info = new
            {
                name = "MCP Health Check Server",
                version = "1.0.0",
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            }
        });
    }
}