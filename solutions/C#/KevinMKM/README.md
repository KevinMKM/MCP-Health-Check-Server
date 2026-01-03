# MCP Health Check Server - KevinMKM Solution

## Overview

A production-ready, standalone MCP (Model Context Protocol) server implementation using ASP.NET Core 8.0.
This server enables AI models (like Claude) to query the health status of cloud services over the public internet through a standardized HTTP + SSE interface.

### Key Focus Areas
- **Extensibility**: Interface-based design for all core components
- **Reliability**: Automated session cleanup with configurable TTL
- **Security**: Pluggable SSRF protection policies
- **Testability**: Comprehensive unit tests with fake implementations
----------------------------------------------------------------------------------------------------------------------------------------------------------
## 🎯 Key Features

**Complete MCP Lifecycle**
- Initialize endpoint (session creation)
- Handshake via Server-Sent Events (SSE)
- Tool execution framework

**Interface-Driven Architecture**
- `ISessionManager` for session operations
- `ISsrfPolicy` for pluggable security
- `ITool` for extensible tool system

**Production-Ready Features**
- Background `SessionCleanupService` (TTL-based)
- Thread-safe `ConcurrentDictionary` session storage
- Request/response logging middleware
- Global error handling middleware
- Health check endpoint

**Built-in Tool**: `check_api_status`
- HTTP/HTTPS endpoint health verification
- Configurable timeout (default: 3000ms)
- Latency measurement
- Comprehensive error reporting

**Security First**
- SSRF protection (blocks loopback, private ranges, link-local)
- Input validation
- Session isolation
- Configurable security policies

**Testing**
- Unit tests with fake implementations
- Session isolation tests
- SSRF policy tests
----------------------------------------------------------------------------------------------------------------------------------------------------------
## Architecture
┌─────────────────────────────────────────┐

│ HTTP Layer │

│ ┌──────────────────────────────────┐ │

│ │ Middlewares │ │

│ │ • ErrorHandling │ │

│ │ • RequestLogging │ │

│ └──────────────────────────────────┘ │

└─────────────────────────────────────────┘

↓

┌─────────────────────────────────────────┐

│ Controllers Layer │

│ ┌──────────┬──────────┬──────────┐ │

│ │Initialize│Handshake │ Tools │ │

│ └──────────┴──────────┴──────────┘ │

└─────────────────────────────────────────┘

↓

┌─────────────────────────────────────────┐

│ Business Logic (Interfaces) │

│ ┌──────────────┬──────────────────┐ │

│ │ISessionMgr │ ITool[] │ │

│ │(DI-based) │ (extensible) │ │

│ └──────────────┴──────────────────┘ │

└─────────────────────────────────────────┘

↓

┌─────────────────────────────────────────┐

│ Background Services │

│ ┌──────────────────────────────────┐ │

│ │ SessionCleanupService │ │

│ │ (runs every 5min, TTL: 30min) │ │

│ └──────────────────────────────────┘ │

└─────────────────────────────────────────┘

Design Principles
Dependency Injection: All core services injected via interfaces
Single Responsibility: Each component has one clear purpose
Open/Closed: Extend via interfaces (ISsrfPolicy, ITool) without modifying core
Thread Safety: ConcurrentDictionary + lock-free reads
----------------------------------------------------------------------------------------------------------------------------------------------------------
## Project Structure
McpHealthServer/

├── Controllers/

│ ├── InitializeController.cs # POST /mcp/initialize

│ ├── HandshakeController.cs # GET /mcp/handshake/{sessionId}

│ └── HealthController.cs # GET /health

│

├── Core/

│ ├── Interfaces/

│ │ └── ISessionManager.cs # Session operations contract

│ ├── InMemorySessionManager.cs # Thread-safe implementation

│ ├── McpSession.cs # Session state + SSE writer

│ └── McpEvent.cs # SSE event wrapper

│

├── Background/

│ └── SessionCleanupService.cs # Periodic cleanup (IHostedService)

│

├── Middlewares/

│ ├── ErrorHandlingMiddleware.cs # Global exception handler

│ └── RequestLoggingMiddleware.cs # Request/response logging

│

├── Security/

│ ├── ISsrfPolicy.cs # SSRF protection contract

│ └── DefaultSsrfPolicy.cs # Blocks private/loopback IPs

│

├── SSE/

│ └── SseWriter.cs # Server-Sent Events helper

│

├── Tools/

│ ├── ITool.cs # Tool contract (Name, Schema, Execute)

│ └── CheckApiStatusTool.cs # Health check implementation

│

├── Configuration/

│ └── McpServerOptions.cs # Typed configuration

│

└── Program.cs # Startup + DI registration
----------------------------------------------------------------------------------------------------------------------------------------------------------

## MCP Protocol Flow
-Initialize (Create Session)
Request:

http
POST /mcp/initialize
Content-Type: application/json
Response:
json
{
“protocol”: “mcp/0.1”,
“session_id”: “3fa85f64-5717-4562-b3fc-2c963f66afa6”,
“server_info”: {
“name”: “MCP Health Check Server”,
“version”: “1.0.0”
},

“capabilities”: {
“tools”: {}
},
“tools”: [
{
“name”: “check_api_status”,
“description”: “Checks the availability and health of an HTTP/HTTPS endpoint”,
“input_schema”: {
“type”: “object”,
“properties”: {
“url”: {
“type”: “string”,
“description”: “The URL to check (must be HTTP or HTTPS)”
}
},

“required”: [“url”]
}
}
],
“sse_endpoint”: “/mcp/handshake/3fa85f64-5717-4562-b3fc-2c963f66afa6”
}

-Handshake (Open SSE Connection)
Request:
http
GET /mcp/handshake/3fa85f64-5717-4562-b3fc-2c963f66afa6
Accept: text/event-stream
SSE Stream:
event: handshake
data: {“status”:“ready”,“timestamp”:“2026-01-03T12:00:00Z”}
event: keepalive
data: {}
(connection stays open)

-Tool Call: check_api_status
Request:
http
POST /mcp/tools/call
Content-Type: application/json
{
“sessionId”: “3fa85f64-5717-4562-b3fc-2c963f66afa6”,
“name”: “check_api_status”,
“input”: {
“url”: “https://httpbin.org/status/200”
}
}

-Success Response (via SSE):
event: tool_result
data: {“url”:“https://httpbin.org/status/200",“status”:“UP”,“http_status”:200,“latency_ms”:87,“checked_at”:"2026-01-03T12:00:15Z”}
Failure Response (via SSE):
event: tool_result
data: {“url”:“https://httpbin.org/delay/10",“status”:“DOWN”,“error”:"Timeout after 3000ms”,“checked_at”:“2026-01-03T12:00:15Z”}
----------------------------------------------------------------------------------------------------------------------------------------------------------

## Security Features
SSRF Protection (ISsrfPolicy)
Default Policy Blocks:

❌ Loopback: 127.0.0.0/8, ::1
❌ Private IPv4: 10.0.0.0/8, 172.16.0.0/12, 192.168.0.0/16
❌ Link-local: 169.254.0.0/16, fe80::/10
❌ IPv6 Unique Local: fc00::/7

## Session Isolation
Each session has its own ID, SSE writer, and event queue
No data leakage between concurrent sessions
Thread-safe operations via ConcurrentDictionary
----------------------------------------------------------------------------------------------------------------------------------------------------------

## Testing
Run All Tests
bash

dotnet test

Test Coverage
- Session creation and retrieval
- Session isolation (concurrent clients)
- Session expiration (TTL)
- SSRF policy enforcement
- Tool execution (with fake HTTP client)
----------------------------------------------------------------------------------------------------------------------------------------------------------

## Quick Start
Prerequisites
.NET 8.0 SDK or higher
(Optional) Visual Studio 2022 / VS Code / Rider
Run Server
bash

cd src/McpHealthServer

dotnet restore

dotnet run

Server starts at: http://localhost:5000

Manual Testing (using .http file)
http

1. Initialize
POST http://localhost:5000/mcp/initialize

2. Handshake (use session_id from step 1)
GET http://localhost:5000/mcp/handshake/YOUR-SESSION-ID

Accept: text/event-stream

3. Call Tool
POST http://localhost:5000/mcp/tools/call

Content-Type: application/json

{

“sessionId”: “YOUR-SESSION-ID”,

“name”: “check_api_status”,

“input”: {

“url”: “https://httpbin.org/status/200”

}

}

4. Health Check
GET http://localhost:5000/health
----------------------------------------------------------------------------------------------------------------------------------------------------------

## Extensibility Guide
Add a New Tool
Implement ITool:
csharp

public class MyCustomTool : ITool

{

public string Name => “my_custom_tool”;

public string Description => “Does something useful”;

public object InputSchema => new

{

type = “object”,

properties = new

{

param1 = new { type = “string” }

},

required = new[] { “param1” }

};

public async Task<object> ExecuteAsync(

JsonElement input,

CancellationToken ct)

{

var param1 = input.GetProperty(“param1”).GetString();

// Your logic here

return new { result = $“Processed: {param1}” };

}

}

Register in Program.cs:
csharp

builder.Services.AddTransient<ITool, MyCustomTool>();

Done! The tool will automatically appear in the Initialize response.
----------------------------------------------------------------------------------------------------------------------------------------------------------

## Performance Considerations
Concurrent Sessions: Thread-safe via ConcurrentDictionary<Guid, McpSession>
Memory Leaks: Prevented by SessionCleanupService (runs every 5min)
HTTP Timeouts: Configurable per tool (default: 3000ms)
SSE Buffering: Minimal latency with Response.Body direct writes