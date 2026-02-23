# Six.Avalonia.Mcp.Server

Standalone MCP server that bridges AI agents and running Avalonia desktop applications. Installable as a global [dotnet tool](https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools) (`six-avalonia-mcp`).

The server speaks [Model Context Protocol](https://modelcontextprotocol.io/) (MCP) over STDIO with AI agents and [SignalR](https://learn.microsoft.com/en-us/aspnet/core/signalr/) WebSocket with connected Avalonia apps. Multiple apps can connect simultaneously.

## Install

```bash
dotnet tool install -g Six.Avalonia.Mcp.Server
```

This installs the `six-avalonia-mcp` command globally.

## Usage

### With Claude Desktop

Add to `claude_desktop_config.json`:

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

### With Claude Code

Add to `.claude/settings.json`:

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

### With VS Code

Add to `.vscode/settings.json`:

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

### Manual

```bash
six-avalonia-mcp
```

The server listens on port 52342 for Avalonia app connections via SignalR at `/inspector`.

## Architecture

```
AI Agent (Claude, VS Code, Codex)
    |
    | STDIO (JSON-RPC / MCP)
    v
six-avalonia-mcp (this server)
    |
    | SignalR WebSocket (port 52342)
    v
Avalonia App(s) (using Connector library)
```

The server maintains a registry of connected apps. When only one app is connected, it is auto-targeted. With multiple apps, use `list_connected_apps` and `set_target_app` to select one, or pass `appId` directly to any tool.

## Available Tools

16 MCP tools across 5 categories:

| Category | Tool | Description |
|----------|------|-------------|
| **Metadata** | `get_app_metadata` | Process info, window dimensions, framework versions |
| **Inspection** | `get_visual_tree` | Recursive visual tree snapshot with configurable depth |
| | `find_element` | Query by name, type, CSS class, or selector |
| | `get_element_properties` | Detailed runtime properties (geometry, state, layout) |
| | `get_element_by_path` | Get element properties by visual tree path (e.g., `0/3/1`) |
| | `get_ancestor_chain` | Parent chain from element to root |
| **Screenshot** | `take_screenshot` | Base64 PNG capture with configurable scale (0.5x-3.0x) |
| **Interaction** | `simulate_click` | Left/right/middle click with modifiers and position |
| | `simulate_type` | Character-by-character text input |
| | `set_control_state` | Focus/blur, enable/disable, show/hide |
| | `simulate_hover` | Pointer enter and move simulation |
| | `simulate_drag` | Press, interpolated moves, release |
| | `simulate_keyboard_shortcut` | Key combinations (Ctrl+S, Alt+F4, etc.) |
| **App Management** | `list_connected_apps` | List connected apps with IDs and metadata |
| | `set_target_app` | Set default target app for commands |
| | `kill_app` | Terminate a connected app gracefully |

All inspection and interaction tools accept an optional `appId` parameter for multi-app targeting.

## Configuration

| Setting | Default | Description |
|---------|---------|-------------|
| `Port` | `52342` | Kestrel listening port for SignalR connections |

Override via environment variable or command-line argument:

```bash
six-avalonia-mcp --Port 9000
```

## Connecting an Avalonia App

Add the [Six.Avalonia.Mcp.Connector](../../libs/Six.Avalonia.Mcp.Connector/) NuGet package to your Avalonia app and call `AddAvaloniaMcpConnector()`:

```csharp
using Six.Avalonia.Mcp.Connector;

Host.CreateDefaultBuilder(args)
    .AddAvaloniaMcpConnector(options =>
    {
        options.ServerUrl = "http://localhost:52342/inspector";
        options.AppName = "My App";
    })
    .Build();
```

See the [Getting Started](../../docs/getting-started.md) guide for a complete walkthrough.

## Dependencies

- **Six.Avalonia.Mcp.Contracts** -- shared DTOs and SignalR hub interfaces
- **ModelContextProtocol** 0.8.0-preview.1 -- MCP SDK with STDIO transport
- **ASP.NET Core** -- Kestrel, SignalR hub, dependency injection

## Documentation

- [Getting Started](../../docs/getting-started.md) -- installation, setup, first connection
- [Architecture](../../docs/architecture.md) -- system design, threading, transport details
- [Tool Reference](../../docs/tools/) -- detailed docs for all 16 tools
- [Configuration](../../docs/configuration.md) -- server, connector, logging, build settings
- [Troubleshooting](../../docs/troubleshooting.md) -- common issues and solutions
