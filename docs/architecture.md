# Architecture

This document describes the internal architecture of Six.Avalonia.Mcp, including the decoupled multi-process design, transport layers, threading model, and element resolution strategy.

## System Overview

Six.Avalonia.Mcp uses a decoupled architecture with three separate processes communicating over two transport layers:

```
+------------------+     STDIO (stdin/stdout)     +----------------------------+    SignalR WebSocket     +----------------------------+
|                  |  <-- JSON-RPC (NDJSON) ---->  |  MCP Server                |  <-- /inspector ----->  |  Avalonia App(s)           |
|   AI Agent       |                               |  (standalone console app)  |                         |                            |
|  (Claude, etc.)  |     stderr                    |                            |                         |  +----------------------+  |
|                  |  <-- Logging ---------------  |  +----------------------+  |                         |  | ConnectorService     |  |
+------------------+                               |  | MCP Tools            |  |                         |  | (SignalR client)     |  |
                                                   |  | (5 static classes)   |  |                         |  +----------+-----------+  |
                                                   |  +----------+-----------+  |                         |             |               |
                                                   |             |               |                         |  +----------v-----------+  |
                                                   |  +----------v-----------+  |                         |  | InspectorCommand-    |  |
                                                   |  | IInspectorProxy      |  |                         |  |   Handler            |  |
                                                   |  | (InspectorProxy)     |  |                         |  +----------+-----------+  |
                                                   |  +----------+-----------+  |                         |             |               |
                                                   |             |               |                         |  +----------v-----------+  |
                                                   |  +----------v-----------+  |                         |  | IAvaloniaInspector   |  |
                                                   |  | SignalR Hub          |  |                         |  | (AvaloniaInspector)  |  |
                                                   |  | (InspectorHub)       |  |                         |  +----------+-----------+  |
                                                   |  +----------+-----------+  |                         |             |               |
                                                   |             |               |                         |  +----------v-----------+  |
                                                   |  +----------v-----------+  |                         |  | Avalonia Visual Tree |  |
                                                   |  | IAppRegistry         |  |                         |  | (UI Thread)          |  |
                                                   |  | (AppRegistry)        |  |                         |  +----------------------+  |
                                                   |  +----------------------+  |                         +----------------------------+
                                                   +----------------------------+
                                                          port 52342
```

### Three Processes

1. **AI Agent** (Claude Desktop, Claude Code, etc.) -- communicates with the MCP server via STDIO.
2. **MCP Server** (`apps/Six.Avalonia.Mcp.Server/`) -- standalone .NET console app. Hosts the MCP STDIO transport, a SignalR hub, and 16 MCP tools across 5 static classes. Routes commands from the AI agent to connected Avalonia apps via SignalR.
3. **Avalonia App(s)** -- any Avalonia application that references the Connector library (`libs/Six.Avalonia.Mcp.Connector/`). Connects to the MCP server's SignalR hub, receives inspection commands, and executes them against its own visual tree.

### Two Transport Layers

| Transport | Direction | Protocol | Purpose |
|-----------|-----------|----------|---------|
| STDIO | Agent <-> Server | JSON-RPC (NDJSON) | MCP tool calls and responses |
| SignalR WebSocket | Server <-> App(s) | SignalR JSON protocol | Inspector commands and results |

## Project Structure

```
Contracts (zero framework deps)
   ^              ^
   |              |
Connector         Server (MCP SDK + ASP.NET SignalR)
(Avalonia +       (standalone console app)
 SignalR Client)
   ^
   |
Demo App / Your App (Avalonia Desktop)
```

### Contracts (`libs/Six.Avalonia.Mcp.Contracts/`)

Shared library with zero external dependencies. Contains:

- **Models/** -- 7 sealed record DTOs (`AppMetadata`, `BoundsInfo`, `VisualTreeNode`, `FindElementResult`, `ElementProperties`, `InteractionResult`, `ConnectedAppInfo`)
- **Hub/** -- SignalR contract interfaces (`IInspectorHub`, `IInspectorClient`)
- **Messages/** -- `InspectorMethod` enum, `InspectorRequest`, `InspectorResponse`
- **JsonOptions.cs** -- shared JSON serialization settings

### Server (`apps/Six.Avalonia.Mcp.Server/`)

Standalone .NET console app packaged as a dotnet tool (`six-avalonia-mcp`). Contains:

- **Hub/InspectorHub.cs** -- SignalR hub managing app connections
- **Services/IAppRegistry, AppRegistry** -- tracks connected Avalonia apps by connection ID
- **Services/IInspectorProxy, InspectorProxy** -- routes commands to the correct app, correlates request/response by GUID
- **Tools/** -- 5 static classes with 16 MCP tools (see below)

### Connector (`libs/Six.Avalonia.Mcp.Connector/`)

Library referenced by Avalonia applications. Contains:

- **Services/IAvaloniaInspector, AvaloniaInspector** -- 12 async inspection methods, all dispatched to the UI thread
- **Services/MockPointer.cs** -- pointer factory for event simulation
- **ConnectorService.cs** -- `IHostedService` that manages the SignalR client connection to the server
- **InspectorCommandHandler.cs** -- dispatches `InspectorRequest` to the appropriate `IAvaloniaInspector` method
- **McpConnectorHost.cs** -- DI extension methods (`services.AddAvaloniaMcpConnector()`)
- **McpConnectorOptions.cs** -- configuration (ServerUrl, AppName, AutoConnect, ReconnectDelay, EnvironmentPrefix)
- **McpConnectorPostConfigure.cs** -- `IPostConfigureOptions`: env var overrides + AppName auto-detection

## MCP Server Architecture

### MCP Tools (5 Static Classes, 14 Tools)

The tool layer is the MCP-facing surface. Each tool is a `static` method decorated with `[McpServerTool]` and `[Description]` attributes. Tools receive `IInspectorProxy` via dependency injection, delegate all work through the proxy to the connected Avalonia app, and return results as JSON.

| Class | Tools | Responsibility |
|-------|-------|----------------|
| `MetadataTools` | `get_app_metadata` | Application process and version info |
| `InspectionTools` | `get_visual_tree`, `find_element`, `get_element_properties`, `get_element_by_path`, `get_ancestor_chain` | Visual tree queries |
| `ScreenshotTools` | `take_screenshot` | PNG screenshot capture |
| `InteractionTools` | `simulate_click`, `simulate_type`, `set_control_state`, `simulate_hover`, `simulate_drag`, `simulate_keyboard_shortcut` | User interaction simulation |
| `AppManagementTools` | `list_connected_apps`, `set_target_app`, `kill_app` | Multi-app management |

All inspection tools follow the same pattern:

```csharp
[McpServerTool(Name = "tool_name")]
[Description("Tool description for the AI agent.")]
public static async Task<string> ToolMethod(
    IInspectorProxy proxy,
    /* params */,
    string? appId = null,
    CancellationToken ct = default)
{
    try
    {
        InspectorResponse response = await proxy.SendCommandAsync(
            InspectorMethod.Method,
            new Dictionary<string, object?> { ... },
            appId,
            ct);
        return response.Success
            ? response.ResultJson!
            : JsonSerializer.Serialize(new { error = response.ErrorMessage }, JsonOptions.Default);
    }
    catch (Exception ex)
    {
        return JsonSerializer.Serialize(new { error = ex.Message }, JsonOptions.Default);
    }
}
```

Errors are returned as JSON `{ "error": "message" }` rather than thrown, so the MCP client always receives a valid response.

### InspectorProxy

The proxy is the bridge between MCP tools and connected Avalonia apps. When a tool calls `SendCommandAsync()`:

1. The proxy creates an `InspectorRequest` with a unique `RequestId` (GUID).
2. It looks up the target app's SignalR connection ID (from `IAppRegistry`).
3. It sends the request to the app via the SignalR hub's `ExecuteCommandAsync()` method.
4. It awaits the response, correlated by `RequestId`.

### AppRegistry

Tracks connected Avalonia applications. Each app registers itself when it connects via `RegisterAppAsync()`, providing a `ConnectedAppInfo` record (app name, process ID, etc.). The registry maps connection IDs to app metadata.

### Server DI Wiring

```csharp
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Logging to stderr (stdout is reserved for MCP STDIO transport)
builder.Logging.ClearProviders();
builder.Logging.AddDebug();
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// SignalR with shared JSON serialization
builder.Services.AddSignalR()
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

// Kestrel configuration
int port = builder.Configuration.GetValue("Port", 52342);
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(port);
});

WebApplication app = builder.Build();
app.MapHub<InspectorHub>("/inspector");
await app.RunAsync();
```

## Connector Architecture

### IAvaloniaInspector / AvaloniaInspector

The inspector implements 12 async methods that perform the actual inspection and interaction with the Avalonia visual tree. Every method dispatches its work to the Avalonia UI thread via `Dispatcher.UIThread.InvokeAsync()`.

Key responsibilities:

- Resolving elements by name, type, or index path
- Building visual tree snapshots (`VisualTreeNode`)
- Constructing detailed property records (`ElementProperties`)
- Dispatching pointer and keyboard events for interaction simulation
- Rendering screenshots via `RenderTargetBitmap`

The `GetTopLevel()` method is `protected virtual`, returning a `TopLevel?`. It uses `TopLevel.GetTopLevel(view)` to resolve from any view, supporting both desktop (`ClassicDesktopStyleApplicationLifetime`) and single-view lifetimes. Test subclasses override this to inject a controlled window.

### ConnectorService

An `IHostedService` that manages the SignalR client connection to the MCP server. On startup, it:

1. Builds a `HubConnection` pointing at the server's `/inspector` endpoint.
2. Registers handlers for `ExecuteCommandAsync`, `DisconnectAsync`, and `ShutdownAsync`.
3. Calls `RegisterAppAsync()` to announce the app to the server.
4. Handles reconnection with configurable delay.

### InspectorCommandHandler

Receives `InspectorRequest` messages from the `ConnectorService` and dispatches them to the appropriate `IAvaloniaInspector` method based on the `InspectorMethod` enum value. Returns an `InspectorResponse` with the result or error.

### Connector DI Wiring

```csharp
public static IServiceCollection AddAvaloniaMcpConnector(
    this IServiceCollection services,
    Action<McpConnectorOptions>? configure = null)
{
    if (configure is not null)
        services.Configure(configure);
    else
        services.Configure<McpConnectorOptions>(_ => { });

    services.TryAddSingleton<IPostConfigureOptions<McpConnectorOptions>, McpConnectorPostConfigure>();

    services.AddSingleton<IAvaloniaInspector, AvaloniaInspector>();
    services.AddSingleton<InspectorCommandHandler>();
    services.AddSingleton<ConnectorService>();
    services.AddHostedService(sp => sp.GetRequiredService<ConnectorService>());

    return services;
}

public static IHostBuilder AddAvaloniaMcpConnector(
    this IHostBuilder hostBuilder,
    Action<McpConnectorOptions>? configure = null)
{
    var tempOptions = new McpConnectorOptions();
    configure?.Invoke(tempOptions);
    string? prefix = tempOptions.EnvironmentPrefix;

    return hostBuilder
        .ConfigureAppConfiguration(builder =>
        {
            if (prefix is not null)
                builder.AddEnvironmentVariables(prefix);
        })
        .ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddDebug();
            logging.AddConsole(options =>
            {
                options.LogToStandardErrorThreshold = LogLevel.Trace;
            });
            logging.SetMinimumLevel(LogLevel.Debug);
        })
        .ConfigureServices((_, services) => services.AddAvaloniaMcpConnector(configure));
}
```

The `McpConnectorPostConfigure` (`IPostConfigureOptions<McpConnectorOptions>`) runs after the user's `configure` lambda and:

1. **Applies environment variable overrides** -- for each option still at its default value, checks for a matching `{prefix}{PropertyName}` env var
2. **Auto-detects AppName** -- if still the default after env var processing, resolves from `Assembly.GetEntryAssembly()?.GetName().Name`

## SignalR Protocol

Communication between the MCP server's SignalR hub and connected Avalonia apps:

| Direction | Method | Purpose |
|-----------|--------|---------|
| Client -> Hub | `RegisterAppAsync(ConnectedAppInfo)` | App announces itself on connection |
| Hub -> Client | `ExecuteCommandAsync(InspectorRequest)` | Server sends inspection command (returns response directly) |
| Hub -> Client | `DisconnectAsync(string)` | Server asks app to disconnect |
| Hub -> Client | `ShutdownAsync(string)` | Server asks app to terminate its process |

The `InspectorProxy` on the server side creates the request with a unique `RequestId`, sends it via the hub's strongly-typed client invocation, and receives the response directly from the return value.

## Multi-App Targeting

The architecture supports multiple Avalonia apps connected simultaneously:

1. **AppRegistry** tracks all connected apps by connection ID and app metadata.
2. **Single app**: When only one app is connected, it is auto-targeted. No `appId` parameter is needed.
3. **Multiple apps**: Use `list_connected_apps` to see all connected apps, then either:
   - Call `set_target_app` to set a default target, or
   - Pass `appId` directly to any inspection tool.
4. **InspectorProxy** routes each command to the correct app based on the resolved target.

All inspection tools accept an optional `appId` parameter for explicit targeting.

## Threading Model

The system has two distinct threading contexts:

### Server Side

MCP tool calls arrive on thread-pool threads from the STDIO transport listener. The server's `InspectorProxy` sends commands over SignalR and awaits responses. No UI thread constraints exist on the server.

### Connector Side (Avalonia App)

Avalonia requires all UI operations to happen on the UI thread. The inspector enforces this by wrapping every operation in `Dispatcher.UIThread.InvokeAsync()`:

```csharp
public async Task<AppMetadata> GetAppMetadataAsync(CancellationToken ct = default)
{
    return await Dispatcher.UIThread.InvokeAsync(() =>
    {
        TopLevel topLevel = GetTopLevel()
                            ?? throw new InvalidOperationException("TopLevel is not available.");
        // ... build metadata on the UI thread ...
    });
}
```

This matters because:

1. **SignalR messages arrive on thread-pool threads** in the Avalonia app.
2. **Visual tree access is not thread-safe.** Reading `Bounds`, `IsVisible`, `GetVisualChildren()`, etc. from a background thread causes race conditions or crashes.
3. **Event dispatch must happen on the UI thread.** `RaiseEvent()` for pointer and keyboard simulation must be called from the dispatcher thread.

The `InvokeAsync` call marshals the work to the UI thread and returns a `Task` that completes when the UI thread finishes the operation.

## Transport: STDIO and NDJSON

Communication between the AI agent and the MCP server uses **STDIO transport**:

- **stdin**: The MCP client sends JSON-RPC requests to the server's standard input.
- **stdout**: The MCP server writes JSON-RPC responses to standard output.
- **stderr**: Application logs are written here to avoid corrupting the MCP message stream.

Messages use **NDJSON** (Newline-Delimited JSON) framing. Each JSON-RPC message is a single line terminated by `\n`. This is simpler than HTTP-style `Content-Length` headers, but requires that nothing else writes to stdout.

This is why logging must be configured to use stderr:

```csharp
logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});
```

## Element Resolution Strategy

When an inspector method needs to find a specific control in the visual tree, it uses a priority-based resolution order:

1. **Name** (`elementName`): Searches the tree recursively for a control whose `Name` property matches. This is the most common and reliable approach.

2. **Type name** (`typeName`): Searches for the first control whose `GetType().Name` matches (e.g., `"Button"`, `"TextBox"`). Useful when you know the type but not the name.

3. **Index path** (`path`): Navigates the tree using a slash-separated string of child indices (e.g., `"0/2/1"` means child 0, then child 2, then child 1). This is a stable positional address but fragile to layout changes.

The resolution is implemented in `AvaloniaInspector.ResolveElement()`:

```csharp
internal static Visual? ResolveElement(Visual root, string? name, string? typeName, string? path)
{
    if (name is not null)    return FindByName(root, name);
    if (typeName is not null) return FindByTypeName(root, typeName);
    if (path is not null)    return NavigateByPath(root, path);
    return null;
}
```

### Selector Syntax

The `find_element` tool also supports a simple CSS-like selector syntax parsed by `ParseSelector()`:

| Selector | Meaning |
|----------|---------|
| `Button` | Find by type name |
| `#myBtn` | Find by name |
| `.error` | Find by CSS class |
| `Button#myBtn` | Find by type AND name |
| `TextBox.error` | Find by type AND class |

### Element IDs

Each element gets a stable ID in the format `"path:0/2/1"` generated by `GenerateElementId()`. This ID encodes the child index path from the root to the element.

## JSON Serialization

All JSON serialization uses the shared `JsonOptions.Default` instance (defined in Contracts):

```csharp
public static JsonSerializerOptions Default { get; } = new()
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false,
    PropertyNameCaseInsensitive = true
};
```

Key characteristics:
- **camelCase** property names in output (`processName`, not `ProcessName`)
- **Null values are omitted** from output (reduces payload size)
- **Compact output** (no indentation or extra whitespace)
- **Case-insensitive** deserialization (accepts both `processName` and `ProcessName`)

Both the Server (SignalR hub JSON protocol) and the Connector (SignalR client) use `JsonOptions.Default` to ensure consistent serialization across the wire.

## Data Models (7 DTOs)

All models are sealed records with `required` properties, defined in `Contracts/Models/`.

| Model | Purpose |
|-------|---------|
| `AppMetadata` | Process name/ID, window title, Avalonia/.NET versions, window dimensions |
| `BoundsInfo` | X, Y, Width, Height of an element's bounds rectangle |
| `VisualTreeNode` | Type, name, bounds, visibility, enabled, opacity, focused, pointer-over, classes, children |
| `FindElementResult` | Type, name, bounds, index path, element ID |
| `ElementProperties` | Full property set including geometry, state, appearance, layout, data context, pseudo-classes |
| `InteractionResult` | Success flag, error message, post-interaction element state |
| `ConnectedAppInfo` | App name, process ID, and metadata for multi-app registration |

## Related Pages

- [Getting Started](./getting-started.md) -- setup and first connection
- [Integration Guide](./integration-guide.md) -- embedding in your app
- [Configuration](./configuration.md) -- build settings and environment variables
