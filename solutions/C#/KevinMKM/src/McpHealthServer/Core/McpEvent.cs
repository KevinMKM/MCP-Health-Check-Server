namespace McpHealthServer.Core;

public record McpEvent(
    string Type,
    object Payload,
    DateTime Timestamp
);