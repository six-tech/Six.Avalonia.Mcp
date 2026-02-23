using System.Reflection;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using Six.Avalonia.Mcp.Contracts;
using Six.Avalonia.Mcp.Server.Services;

// Redirect Console.Out to stderr so ASP.NET logging doesn't corrupt MCP STDIO transport.
// The MCP SDK uses Console.OpenStandardOutput() directly (raw FD 1), not Console.Out.
Console.SetOut(Console.Error);

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Logging to stderr (stdout is reserved for MCP STDIO transport)
builder.Logging.ClearProviders();
builder.Logging.AddDebug();
builder.Logging.AddConsole(options =>
{
  options.LogToStandardErrorThreshold = LogLevel.Trace;
});
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// SignalR (1 MB max — screenshots are saved to disk, only metadata travels through SignalR)
builder.Services.AddSignalR(hubOptions =>
  {
    hubOptions.MaximumReceiveMessageSize = 1024 * 1024;
    hubOptions.KeepAliveInterval = TimeSpan.FromSeconds(5);
    hubOptions.ClientTimeoutInterval = TimeSpan.FromSeconds(15);
  })
  .AddJsonProtocol(options =>
  {
    options.PayloadSerializerOptions = JsonOptions.Default;
  });

// MCP Server with STDIO transport
builder.Services.AddMcpServer(options =>
  {
    options.ServerInfo = new Implementation
    {
      Name = "McpDemo.Server",
      Version = typeof(Program).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
        ?.InformationalVersion ?? "0.0.0"
    };
  })
  .WithStdioServerTransport()
  .WithToolsFromAssembly();

// App services
builder.Services.AddSingleton<IAppRegistry, AppRegistry>();
builder.Services.AddSingleton<IInspectorProxy, InspectorProxy>();

// Graceful shutdown: notify connected apps before tearing down SignalR.
// Hosted services stop in reverse order, so registering last ensures this runs first.
builder.Services.AddHostedService<GracefulShutdownService>();

// Kestrel configuration
int port = builder.Configuration.GetValue("Port", 52342);
builder.WebHost.ConfigureKestrel(options =>
{
  options.ListenLocalhost(port);
});

WebApplication app = builder.Build();

app.MapHub<Six.Avalonia.Mcp.Server.Hub.InspectorHub>("/inspector");

await app.RunAsync();
