# API Reference

Six.Avalonia.Mcp is a decoupled MCP (Model Context Protocol) architecture that enables AI agents to inspect and interact with running Avalonia desktop applications. A standalone MCP server communicates with Avalonia apps via SignalR, exposing 16 tools over STDIO transport for visual tree inspection, element querying, screenshot capture, and user interaction simulation.

**Architecture:** `AI Agent -> STDIO -> MCP Server Console App -> SignalR -> Avalonia App(s) (Connector + Inspector)`

## Public API Surface

The public API consists of three parts:

1. **[IAvaloniaInspector](iavalonia-inspector.md)** -- the main service interface in the Connector library with 12 methods covering discovery, inspection, screenshots, and interactions.
2. **[Models](models.md)** -- 7 DTO records in `Six.Avalonia.Mcp.Contracts.Models` used as inputs and outputs for the inspector methods.
3. **Server-side services** -- `IInspectorProxy` and `IAppRegistry` in `Six.Avalonia.Mcp.Server.Services`, used by the 16 MCP tools to communicate with connected apps.

## Entry Points

### Connector (for Avalonia apps)

Register the connector in your Avalonia app to connect to the MCP server via SignalR:

```csharp
using Six.Avalonia.Mcp.Connector;

// On IServiceCollection
services.AddAvaloniaMcpConnector(options =>
{
    options.ServerUrl = "http://localhost:52342/inspector";
    options.AppName = "My Avalonia App";
});

// Or on IHostBuilder
hostBuilder.AddAvaloniaMcpConnector(options =>
{
    options.ServerUrl = "http://localhost:52342/inspector";
    options.AppName = "My Avalonia App";
});
```

This registers:

- `IAvaloniaInspector` as a singleton (implemented by `AvaloniaInspector`)
- `InspectorCommandHandler` as a singleton (dispatches remote commands to the inspector)
- `ConnectorService` as a hosted service (manages SignalR connection to the server)

### Server (standalone MCP server)

The MCP server is a separate process with its own DI setup:

```csharp
builder.Services.AddSignalR();
builder.Services.AddMcpServer(options => { ... })
    .WithStdioServerTransport()
    .WithToolsFromAssembly();
builder.Services.AddSingleton<IAppRegistry, AppRegistry>();
builder.Services.AddSingleton<IInspectorProxy, InspectorProxy>();
```

## Programmatic vs. MCP Usage

The 16 MCP tools (`get_app_metadata`, `get_visual_tree`, `find_element`, `get_element_by_path`, `list_connected_apps`, `set_target_app`, `kill_app`, etc.) are exposed on the Server and delegate to connected apps via `IInspectorProxy`. AI agents interact with them via the MCP protocol.

For programmatic use within your Avalonia app, resolve `IAvaloniaInspector` from the DI container and call its methods directly.

## Threading Model

All Avalonia operations inside `AvaloniaInspector` are dispatched to the UI thread via `Dispatcher.UIThread.InvokeAsync()`. Callers do not need to marshal to the UI thread themselves -- the inspector handles this internally.
