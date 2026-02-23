# Getting Started

This guide walks you through setting up Six.Avalonia.Mcp to enable AI agents to inspect and interact with your running Avalonia desktop application.

## Architecture Overview

Six.Avalonia.Mcp uses a decoupled architecture with three separate processes:

```
AI Agent (Claude, VS Code, etc.)
    |
    | STDIO (JSON-RPC)
    v
MCP Server (standalone console app, port 52342)
    |
    | SignalR WebSocket
    v
Your Avalonia App(s) (using Connector library)
```

1. **AI Agent** -- Claude Desktop, Claude Code, VS Code, Codex, or any MCP-compatible client
2. **MCP Server** -- a standalone .NET console app that bridges AI and apps
3. **Avalonia App(s)** -- your app(s) using the Connector library to connect to the server

## Prerequisites

- **.NET 10 SDK** or later
- An **Avalonia 11.3.11** desktop application
- Basic familiarity with Avalonia controls and XAML

## Step 1: Install the MCP Server

Install the MCP server as a global dotnet tool:

```bash
dotnet tool install -g Six.Avalonia.Mcp.Server
```

This provides the `six-avalonia-mcp` command, which starts the MCP server.

## Step 2: Add the Connector to Your App

Add the Connector NuGet package to your Avalonia application project:

```bash
dotnet add package Six.Avalonia.Mcp.Connector
```

## Step 3: Register the Connector

In your `Program.cs`, call `AddAvaloniaMcpConnector()` on either the `IHostBuilder` or the `IServiceCollection`. This registers the inspector, command handler, and SignalR connector service.

```csharp
using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Six.Avalonia.Mcp.Connector;

namespace MyApp;

sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        IHost host = CreateHostBuilder(args).Build();
        App.Services = host.Services;

        _ = host.StartAsync();

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .AddAvaloniaMcpConnector()
            .ConfigureServices((context, services) =>
            {
                // Register your own view models and services here
                services.AddTransient<ViewModels.MainWindowViewModel>();
            });
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}
```

Zero configuration is needed -- the connector auto-detects the app name from the entry assembly and uses sensible defaults for all options. Environment variables with the `SIX_MCP_` prefix are automatically registered in the configuration system.

To customize, pass a lambda:

```csharp
.AddAvaloniaMcpConnector(options =>
{
    options.AppName = "My Custom Name";
})
```

### What `AddAvaloniaMcpConnector()` Registers

The extension method performs these registrations:

1. **`IAvaloniaInspector`** as a singleton (`AvaloniaInspector`) -- the service that inspects and interacts with the Avalonia visual tree
2. **`InspectorCommandHandler`** -- dispatches incoming `InspectorRequest` messages to the appropriate `IAvaloniaInspector` method
3. **`ConnectorService`** as an `IHostedService` -- manages the SignalR connection to the MCP server and routes commands
4. **`McpConnectorPostConfigure`** as `IPostConfigureOptions<McpConnectorOptions>` -- applies environment variable overrides and auto-detects `AppName` from the entry assembly

The `IHostBuilder` overload also automatically:
- Configures logging to stderr at Debug level (stdout remains available for the MCP STDIO transport)
- Registers `SIX_MCP_`-prefixed environment variables into the configuration system (so `SIX_MCP_Logging__*` and similar IConfiguration-based vars work without manual setup)

## Step 4: Name Your Controls

MCP tools resolve elements by their `x:Name` attribute. Without names, elements can only be found by type or index path, which is fragile. Add `x:Name` to every control you want to be discoverable:

```xml
<Window x:Class="MyApp.Views.MainWindow"
        xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="My Application">

    <Grid x:Name="RootGrid">
        <TextBox x:Name="InputTextBox"
                 Watermark="Type something..." />

        <Button x:Name="ActionButton"
                Content="Click Me" />

        <TextBlock x:Name="StatusText"
                   Text="Ready" />
    </Grid>
</Window>
```

See the [Integration Guide](./integration-guide.md) for detailed naming best practices.

## Step 5: Configure Your MCP Client

Point your MCP client at the `six-avalonia-mcp` dotnet tool. The MCP server communicates with clients via STDIO and with your app(s) via SignalR.

### Claude Desktop

Add the following to your Claude Desktop configuration file (`claude_desktop_config.json`):

```json
{
  "mcpServers": {
    "avalonia-inspector": {
      "command": "six-avalonia-mcp",
      "args": []
    }
  }
}
```

### Claude Code

Add the following to `.claude/settings.json`:

```json
{
  "mcpServers": {
    "avalonia-inspector": {
      "command": "six-avalonia-mcp",
      "args": []
    }
  }
}
```

### VS Code

Add the following to `.vscode/settings.json`:

```json
{
  "mcp": {
    "servers": {
      "avalonia-inspector": {
        "command": "six-avalonia-mcp"
      }
    }
  }
}
```

## Step 6: Verify the Connection

Start the components in order:

1. **Start the MCP server** -- your MCP client launches `six-avalonia-mcp` automatically when it connects
2. **Start your Avalonia app** -- the Connector auto-connects to the server via SignalR on port 52342

Once both are running, call the `get_app_metadata` tool from your MCP client to confirm the connection. You should receive a JSON response like:

```json
{
  "processName": "MyApp",
  "processId": 12345,
  "mainWindowTitle": "My Application",
  "avaloniaVersion": "11.3.0.0",
  "dotNetVersion": "10.0.0",
  "windowCount": 1,
  "windowWidth": 1280.0,
  "windowHeight": 720.0
}
```

If you see this response, the MCP server is connected to your Avalonia application. You can now use any of the 16 available tools to inspect the visual tree, capture screenshots, simulate user interactions, and manage multiple connected apps.

## Next Steps

- [Architecture](./architecture.md) -- understand the decoupled 3-process design and threading model
- [Integration Guide](./integration-guide.md) -- detailed configuration, custom inspectors, and multi-app targeting
- [Troubleshooting](./troubleshooting.md) -- common issues and solutions
