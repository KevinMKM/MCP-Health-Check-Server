using McpHealthServer.Background;
using McpHealthServer.Core;
using McpHealthServer.Core.Configuration;
using McpHealthServer.Core.Interfaces;
using McpHealthServer.Middlewares;
using McpHealthServer.Security;
using McpHealthServer.Tools;

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Services.Configure<McpServerOptions>(builder.Configuration.GetSection(McpServerOptions.SectionName));

// Core Services
builder.Services.AddSingleton<ISessionManager, InMemorySessionManager>();
builder.Services.AddSingleton<ISsrfPolicy, DefaultSsrfPolicy>();

// HTTP Client
builder.Services.AddHttpClient();

// Tools
builder.Services.AddTransient<ITool, CheckApiStatusTool>();

// Background Services
builder.Services.AddHostedService<SessionCleanupService>();

// Controllers
builder.Services.AddControllers();

// Swagger (optional, for development)
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
}

var app = builder.Build();

// Middleware Pipeline
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();