# Six.Avalonia.Mcp

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET 10](https://img.shields.io/badge/.NET-10-purple.svg)](https://dotnet.microsoft.com/)
[![Avalonia 11.3.11](https://img.shields.io/badge/Avalonia-11.3.11-blue.svg)](https://avaloniaui.net/)
[![MCP](https://img.shields.io/badge/MCP-0.8.0--preview.1-green.svg)](https://modelcontextprotocol.io/)

## What Is This?

A decoupled MCP (Model Context Protocol) architecture that enables AI agents to inspect and interact with running Avalonia desktop applications. A standalone MCP server communicates with Avalonia apps via SignalR, supporting multi-app inspection from a single agent session.

Six.Avalonia.Mcp bridges AI agents and Avalonia desktop applications using the [Model Context Protocol](https://modelcontextprotocol.io/). 

It uses a **decoupled three-process architecture**:

1. **AI Agent** (Claude Desktop, Claude Code, Codex, VS Code, etc.) connects to the MCP server via STDIO
2. **MCP Server** (`six-avalonia-mcp`) is a standalone .NET console app that speaks MCP over STDIO with the agent and SignalR with the apps
3. **Avalonia App(s)** uses the `Six.Avalonia.Mcp.Connector` library to connect to the server via **SignalR WebSocket**

This means your Avalonia app is never launched as a subprocess of the AI agent. The server and app(s) run independently, and multiple Avalonia apps can connect to the same server simultaneously. The agent gets 16 MCP tools to inspect visual trees, query element properties, capture screenshots, and simulate user interactions -- all through a standardized protocol.

**The library is designed for AI-assisted testing, accessibility auditing, and automated UI exploration.** 

An AI agent connected via MCP can navigate your application's UI, understand its structure, and interact with controls just as a human user would.

## Features

- **Decoupled architecture** -- server and apps run as separate processes, connected via SignalR
- **Multi-app support** -- multiple Avalonia apps can connect to one server simultaneously
- **16 MCP tools** in 5 static classes covering inspection, screenshots, input simulation, and app management
- **Two-step integration** -- install the server as a dotnet tool, add the `Six.Avalonia.Mcp.Connector` NuGet to your app
- **Visual tree traversal** with configurable depth and subtree targeting
- **Element queries** by name, type, CSS class, or selector syntax (`Button#myBtn`, `.error`)
- **Screenshot capture** as base64 PNG with configurable scale factor (0.5x-3.0x)
- **Mouse simulation** -- click (left/right/middle), hover, drag with interpolated movement
- **Keyboard simulation** -- character-by-character typing and keyboard shortcuts (Ctrl+S, Alt+F4, etc.)
- **Control state management** -- focus/blur, enable/disable, show/hide
- **Thread-safe** -- all Avalonia operations dispatched to the UI thread
- **Structured JSON responses** with camelCase naming and null value omission
- **Installable as a dotnet tool** -- `six-avalonia-mcp`

## Available Tools

| Category           | Tool                         | Description                                                            |
|--------------------|------------------------------|------------------------------------------------------------------------|
| **Metadata**       | `get_app_metadata`           | Process info, window dimensions, framework versions                    |
| **Inspection**     | `get_visual_tree`            | Recursive visual tree snapshot with configurable depth                 |
|                    | `find_element`               | Query by name, type, CSS class, or selector                            |
|                    | `get_element_properties`     | Detailed runtime properties (geometry, state, appearance, layout)      |
|                    | `get_element_by_path`        | Get element properties by index-based visual tree path (e.g., `0/3/1`) |
|                    | `get_ancestor_chain`         | Parent chain from element to root                                      |
| **Screenshot**     | `take_screenshot`            | Base64 PNG capture with configurable scale (0.5x-3.0x)                 |
| **Interaction**    | `simulate_click`             | Left/right/middle click with modifiers and position                    |
|                    | `simulate_type`              | Character-by-character text input with special keys                    |
|                    | `set_control_state`          | Focus/blur, enable/disable, show/hide                                  |
|                    | `simulate_hover`             | PointerEntered + PointerMoved (optional exit)                          |
|                    | `simulate_drag`              | Press, interpolated moves, release                                     |
|                    | `simulate_keyboard_shortcut` | Key combinations like Ctrl+S, Alt+F4                                   |
| **App Management** | `list_connected_apps`        | List all connected Avalonia apps with IDs and metadata                 |
|                    | `set_target_app`             | Set target app for subsequent inspection commands                      |
|                    | `kill_app`                   | Terminate a connected app gracefully                                   |

All inspection and interaction tools accept an optional `appId` parameter for multi-app targeting.

## Quick Start

### 1. Install the MCP Server

```bash
dotnet tool install -g Six.Avalonia.Mcp.Server
```

This installs the `six-avalonia-mcp` command globally.

### 2. Add the Connector to Your Avalonia App

```bash
dotnet add package Six.Avalonia.Mcp.Connector
```

In your `Program.cs`, register the connector:

```csharp
using Avalonia;
using Microsoft.Extensions.Hosting;
using Six.Avalonia.Mcp.Connector;

sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        IHost host = Host.CreateDefaultBuilder(args)
            .AddAvaloniaMcpConnector()
            .Build();

        App.Services = host.Services;
        _ = host.StartAsync();

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
```

Zero configuration is needed -- the connector auto-detects the app name from the entry assembly and uses sensible defaults. To customize, pass a lambda:

```csharp
.AddAvaloniaMcpConnector(options =>
{
    options.AppName = "My App";
})
```

Options can also be overridden via environment variables with the `SIX_MCP_` prefix (e.g., `SIX_MCP_ServerUrl`, `SIX_MCP_AppName`).

### 3. Name Your Controls

Give `x:Name` attributes to controls you want to inspect or interact with:

```xml
<TextBox x:Name="InputTextBox" Watermark="Type here..." />
<Button x:Name="ActionButton" Content="Click Me" />
<CheckBox x:Name="ToggleCheckBox" Content="Toggle Option" />
```

### 4. Configure Your MCP Client

**Claude Desktop** (`claude_desktop_config.json`):

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

**Claude Code** (`.claude/settings.json`):

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

### 5. Run

Start the MCP server (via your MCP client configuration, or manually):

```bash
six-avalonia-mcp
```

Then start your Avalonia app. It will auto-connect to the server via SignalR. The AI agent can now inspect and interact with your running application.


## Architecture

```
┌───────────────────┐          STDIO          ┌───────────────────────┐        SignalR        ┌───────────────────────┐
│    AI Agent       │◄───── (JSON-RPC) ──────►│   MCP Server          │◄──── (WebSocket) ────►│   Avalonia App        │
│ (Claude, Codex,   │                         │  (six-avalonia-mcp)   │                       │  (Your App +          │
│  VS Code, etc.)   │                         │                       │                       │   Connector library)  │
└───────────────────┘                         │  ┌─────────────────┐  │                       │  ┌─────────────────┐  │
                                              │  │ MCP Tools (14)  │  │                       │  │ ConnectorService│  │
                                              │  │ (5 classes)     │  │                       │  │ (SignalR client)│  │
                                              │  └────────┬────────┘  │                       │  └────────┬────────┘  │
                                              │  ┌────────▼────────┐  │                       │  ┌────────▼────────┐  │
                                              │  │ IInspectorProxy │  │                       │  │ CommandHandler  │  │
                                              │  │ (SignalR sender)│  │                       │  └────────┬────────┘  │
                                              │  └────────┬────────┘  │                       │  ┌────────▼────────┐  │
                                              │  ┌────────▼────────┐  │                       │  │AvaloniaInspector│  │
                                              │  │ InspectorHub    │  │                       │  │(UI thread ops)  │  │
                                              │  │ (SignalR hub)   │  │                       │  └────────┬────────┘  │
                                              │  └─────────────────┘  │                       │  ┌────────▼────────┐  │
                                              │  ┌─────────────────┐  │                       │  │Avalonia Visual  │  │
                                              │  │ AppRegistry     │  │                       │  │Tree             │  │
                                              │  └─────────────────┘  │                       │  └─────────────────┘  │
                                              └───────────────────────┘                       └───────────────────────┘
```

**Three separate processes communicate over two transports:**

- **AI Agent to MCP Server**: STDIO with JSON-RPC (Model Context Protocol)
- **MCP Server to Avalonia App(s)**: SignalR WebSocket on `port 52342`

The server maintains an `AppRegistry` of connected apps. When only one app is connected, it is auto-targeted. With multiple apps, use `list_connected_apps` and `set_target_app` to select one, or pass `appId` directly to any tool.

### Dependency Graph

```
Contracts (zero framework deps)
   ↑              ↑
Connector         Server (MCP SDK + ASP.NET SignalR)
(Avalonia +
 SignalR Client)
   ↑
Demo App
```

### SignalR Protocol

| Direction | Method | Purpose |
|-----------|--------|---------|
| Client to Hub | `RegisterAppAsync(ConnectedAppInfo)` | App announces itself |
| Hub to Client | `ExecuteCommandAsync(InspectorRequest)` | Server sends inspection command (returns response directly) |
| Hub to Client | `DisconnectAsync(string)` | Server asks app to disconnect |
| Hub to Client | `ShutdownAsync(string)` | Server asks app to terminate its process |



## Project Structure

```
Six.Avalonia.Mcp/
├── libs/
│   ├── Six.Avalonia.Mcp.Contracts/        # Shared DTOs, hub interfaces, messages (zero framework deps)
│   │   └── src/
│   │       ├── JsonOptions.cs               # Shared JSON serialization (camelCase, ignore nulls)
│   │       ├── Models/                      # 7 sealed record DTOs
│   │       │   ├── AppMetadata.cs
│   │       │   ├── BoundsInfo.cs
│   │       │   ├── VisualTreeNode.cs
│   │       │   ├── FindElementResult.cs
│   │       │   ├── ElementProperties.cs
│   │       │   ├── InteractionResult.cs
│   │       │   └── ConnectedAppInfo.cs
│   │       ├── Hub/                         # IInspectorHub, IInspectorClient (SignalR contracts)
│   │       └── Messages/                    # InspectorMethod enum, InspectorRequest, InspectorResponse
│   └── Six.Avalonia.Mcp.Connector/        # Avalonia inspector + SignalR client for Avalonia apps
│       └── src/
│           ├── Services/
│           │   ├── IAvaloniaInspector.cs    # Public API interface (12 methods)
│           │   ├── AvaloniaInspector.cs     # Implementation (UI-thread dispatched, TopLevel-based)
│           │   └── MockPointer.cs           # Pointer factory for event simulation
│           ├── McpConnectorHost.cs          # DI extension: services.AddAvaloniaMcpConnector()
│           ├── McpConnectorOptions.cs       # ServerUrl, AppName, AutoConnect, ReconnectDelay, EnvironmentPrefix
│           ├── McpConnectorPostConfigure.cs # IPostConfigureOptions: env var overrides + AppName auto-detection
│           ├── ConnectorService.cs          # IHostedService: SignalR connection + command dispatch
│           └── InspectorCommandHandler.cs   # Dispatches InspectorRequest -> IAvaloniaInspector
├── apps/
│   ├── McpDemo/                             # Demo Avalonia app (uses Connector)
│   │   ├── Program.cs
│   │   └── Views/MainWindow.axaml
│   └── Six.Avalonia.Mcp.Server/           # Standalone MCP server + SignalR hub (packable as dotnet tool)
│       └── src/
│           ├── Hub/InspectorHub.cs          # SignalR hub managing app connections
│           ├── Services/                    # IAppRegistry, IInspectorProxy + implementations
│           └── Tools/                       # 16 MCP tools (5 static classes)
│               ├── MetadataTools.cs
│               ├── InspectionTools.cs
│               ├── ScreenshotTools.cs
│               ├── InteractionTools.cs
│               └── AppManagementTools.cs
├── tests/
│   ├── Six.Avalonia.Mcp.Tests.Unit/        # Inspector + model tests
│   ├── Six.Avalonia.Mcp.Server.Tests.Unit/ # Server tool + registry tests
│   └── Six.Avalonia.Mcp.Connector.Tests.Unit/ # Command handler tests
└── docs/                                    # Detailed documentation
```

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Avalonia 11.3.11](https://avaloniaui.net/) (Connector and apps)
- [ModelContextProtocol 0.8.0-preview.1](https://www.nuget.org/packages/ModelContextProtocol/) (Server only)
- [Microsoft.AspNetCore.SignalR.Client 10.0.2](https://www.nuget.org/packages/Microsoft.AspNetCore.SignalR.Client/) (Connector only)

## Documentation

See the [`docs/`](docs/) directory for detailed documentation:

- [Getting Started](docs/getting-started.md) -- installation, setup, first connection
- [Architecture](docs/architecture.md) -- system design, threading, transport, SignalR protocol
- [Integration Guide](docs/integration-guide.md) -- add the Connector to your app, configure MCP clients
- [Tool Reference](docs/tools/) -- detailed docs for all 16 tools
- [API Reference](docs/api/) -- `IAvaloniaInspector` interface and model DTOs
- [Configuration](docs/configuration.md) -- SDK, logging, build properties, connector options
- [Testing Guide](docs/testing-guide.md) -- run tests, write new tests, coverage
- [Troubleshooting](docs/troubleshooting.md) -- common issues and solutions


## License

[MIT](LICENSE)
