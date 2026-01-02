using McpHealthServer.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace McpHealthServer.Controllers;

[ApiController]
[Route("mcp/initialize")]
public class InitializeController : ControllerBase
{
    private readonly ISessionManager _sessions;

    public InitializeController(ISessionManager sessions)
    {
        _sessions = sessions;
    }

    [HttpPost]
    public IActionResult Initialize()
    {
        var session = _sessions.Create();

        return Ok(new
        {
            session_id = session.SessionId,
            tools = new[] { "check_api_status" },
            sse_url = $"/mcp/sessions/{session.SessionId}/events"
        });
    }
}