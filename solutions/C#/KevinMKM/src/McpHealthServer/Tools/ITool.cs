using System.Text.Json;

namespace McpHealthServer.Tools;

public interface ITool
{
    string Name { get; }
    string Description { get; }
    object InputSchema { get; }

    Task<object> ExecuteAsync(JsonElement input, CancellationToken cancellationToken = default);
}