using McpHealthServer.Core;
using System.Text.Json;

namespace McpHealthServer.Tools;

public interface ITool
{
    string Name { get; }
    Task ExecuteAsync(JsonElement input, McpSession session);
}