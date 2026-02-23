# Six.Avalonia.Mcp.Connector

SignalR connector and Avalonia inspector for connecting Avalonia applications to the [Six MCP server](../../apps/Six.Avalonia.Mcp.Server/). Enables AI agents to inspect visual trees, query element properties, capture screenshots, and simulate user interactions in your running Avalonia app.

## Install

```bash
dotnet add package Six.Avalonia.Mcp.Connector
```

## Quick Setup

```csharp
using Six.Avalonia.Mcp.Connector;

// Zero-config: auto-detects app name, uses defaults
hostBuilder.AddAvaloniaMcpConnector();

// Or with custom options
hostBuilder.AddAvaloniaMcpConnector(options =>
{
    options.AppName = "My Avalonia App";
});

// Also works on IServiceCollection
services.AddAvaloniaMcpConnector();
```

This registers:

- **IAvaloniaInspector** (singleton) -- 12-method interface for visual tree inspection and interaction simulation
- **InspectorCommandHandler** (singleton) -- dispatches remote commands from the MCP server to the inspector
- **ConnectorService** (hosted service) -- manages the SignalR connection to the MCP server with auto-reconnect
- **McpConnectorPostConfigure** (singleton) -- applies env var overrides and auto-detects AppName from the entry assembly

## Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `ServerUrl` | `string` | `"http://localhost:52342/inspector"` | SignalR hub URL of the MCP server |
| `AppName` | `string` | Entry assembly name (fallback: `"Avalonia App"`) | Human-readable name for this app connection |
| `AutoConnect` | `bool` | `true` | Connect to the server on startup |
| `ReconnectDelay` | `TimeSpan` | 5 seconds | Delay between reconnection attempts |
| `EnvironmentPrefix` | `string?` | `"SIX_MCP_"` | Env var prefix for option overrides; `null` to disable |

Options can be overridden via environment variables (e.g., `SIX_MCP_ServerUrl`, `SIX_MCP_AppName`). Explicit code configuration always takes priority over env vars.

## How It Works

1. Your Avalonia app starts and the `ConnectorService` connects to the MCP server via SignalR.
2. The app registers itself with the server hub, providing metadata (app name, process ID, etc.).
3. AI agents send commands to the MCP server via STDIO.
4. The server forwards commands to your app via SignalR.
5. The `InspectorCommandHandler` dispatches commands to `IAvaloniaInspector`, which executes them on the UI thread.
6. Results flow back through SignalR to the server and then to the AI agent.

## Dependencies

- **Six.Avalonia.Mcp.Contracts** -- shared DTOs and hub interfaces
- **Avalonia** -- UI framework
- **Microsoft.AspNetCore.SignalR.Client** -- SignalR client
- **Microsoft.Extensions.Hosting** -- hosted service infrastructure

## Documentation

See the [full documentation](../../docs/index.md) for architecture details, integration guide, and troubleshooting.
