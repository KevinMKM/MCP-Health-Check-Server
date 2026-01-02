using McpHealthServer.Background;
using McpHealthServer.Core;
using McpHealthServer.Core.Interfaces;
using McpHealthServer.Security;
using McpHealthServer.Tools;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSingleton<ISessionManager, InMemorySessionManager>();
builder.Services.AddSingleton<ISsrfPolicy, DefaultSsrfPolicy>();

builder.Services.AddHttpClient("probe", c =>
{
    c.Timeout = TimeSpan.FromMilliseconds(3000);
});

builder.Services.AddSingleton<ITool, CheckApiStatusTool>();
builder.Services.AddHostedService<SessionCleanupService>();

var app = builder.Build();

app.MapControllers();

app.Run();