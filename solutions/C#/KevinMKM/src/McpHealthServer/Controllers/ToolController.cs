using System.Text.Json;
using McpHealthServer.Core.Interfaces;
using McpHealthServer.Tools;
using Microsoft.AspNetCore.Mvc;

namespace McpHealthServer.Controllers;

[ApiController]
[Route("mcp/sessions/{sessionId}/tools")]
public class ToolController : ControllerBase
{
    private readonly ISessionManager _sessions;
    private readonly IEnumerable<ITool> _tools;

    public ToolController(ISessionManager sessions, IEnumerable<ITool> tools)
    {
        _sessions = sessions;
        _tools = tools;
    }

    [HttpPost("invoke")]
    public async Task<IActionResult> Invoke(
        string sessionId,
        [FromBody] JsonElement body)
    {
        var session = _sessions.Get(sessionId);
        var name = body.GetProperty("name").GetString();
        var input = body.GetProperty("input");

        var tool = _tools.FirstOrDefault(t => t.Name == name);
        if (tool == null) return BadRequest("Tool not found");

        await tool.ExecuteAsync(input, session);

        return Accepted();
    }
}