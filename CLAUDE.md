# Six.Avalonia.Mcp

Decoupled MCP (Model Context Protocol) architecture enabling AI agents to inspect and interact with running Avalonia desktop applications. A standalone MCP server communicates with Avalonia apps via SignalR, supporting multi-app inspection.

**Architecture:** `AI Agent -> STDIO -> MCP Server Console App -> SignalR -> Avalonia App(s) (Connector + Inspector)`

## Build

```bash
# Build entire solution
dotnet build

# Build specific project
dotnet build libs/Six.Avalonia.Mcp.Contracts
dotnet build libs/Six.Avalonia.Mcp.Connector
dotnet build apps/Six.Avalonia.Mcp.Server
dotnet build apps/Six.Avalonia.Mcp
```

## Test

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/Six.Avalonia.Mcp.Tests.Unit
dotnet test tests/Six.Avalonia.Mcp.Server.Tests.Unit
dotnet test tests/Six.Avalonia.Mcp.Connector.Tests.Unit

# Run specific test class
dotnet test tests/Six.Avalonia.Mcp.Tests.Unit --filter "FullyQualifiedName~AvaloniaInspectorTests"

# Run with coverage report
dotnet test --collect:"XPlat Code Coverage"
```

Coverage threshold: 80% (line, branch, method) — configured in `tests/Directory.Build.props`.

## Run

```bash
# Start the MCP server (registers with AI harness via STDIO, listens for app connections on port 52342)
dotnet run --project apps/Six.Avalonia.Mcp.Server

# Or via dotnet tool (after installing):
six-avalonia-mcp

# Start the demo app (auto-connects to MCP server via SignalR)
dotnet run --project apps/Six.Avalonia.Mcp
```

## Install as dotnet tool

```bash
# Pack and install globally
dotnet pack apps/Six.Avalonia.Mcp.Server -c Release -o packages/
dotnet tool install --global --add-source packages/ Six.Avalonia.Mcp.Server

# Or use the install script
dotnet run scripts/install.cs
```

## Project Structure

```
Six.Avalonia.Mcp/
├── libs/
│   ├── Six.Avalonia.Mcp.Contracts/        # Shared DTOs, message envelopes, hub interfaces (zero framework deps)
│   │   └── src/
│   │       ├── JsonOptions.cs               # Shared JSON serialization (camelCase, ignore nulls)
│   │       ├── Models/                      # 7 sealed record DTOs (AppMetadata, BoundsInfo, VisualTreeNode, etc.)
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
│   ├── Six.Avalonia.Mcp/                  # Demo Avalonia app (uses Connector)
│   └── Six.Avalonia.Mcp.Server/           # Standalone MCP server + SignalR hub (packable as dotnet tool)
│       └── src/
│           ├── Hub/InspectorHub.cs          # SignalR hub managing app connections
│           ├── Services/                    # IAppRegistry, IInspectorProxy + implementations
│           └── Tools/                       # 16 MCP tools (13 inspection + 3 app management)
└── tests/
    ├── Six.Avalonia.Mcp.Tests.Unit/        # Inspector + model tests
    ├── Six.Avalonia.Mcp.Server.Tests.Unit/ # Server tool + registry tests
    └── Six.Avalonia.Mcp.Connector.Tests.Unit/ # Command handler tests
```

**Dependency graph:**
```
Contracts (zero framework deps)
   ↑              ↑
Connector         Server (MCP SDK + ASP.NET SignalR)
(Avalonia +
 SignalR Client)
   ↑
Demo App (Avalonia Desktop)
```

## Key Conventions

- **Language**: C# 14 / .NET 10
- **Nullable**: Enabled, warnings-as-errors
- **Analysis**: StyleCop + TreatWarningsAsErrors
- **Models**: Sealed records with `required` properties in `Contracts` namespace
- **JSON**: camelCase naming, null values ignored (`JsonOptions.cs` in Contracts)
- **UI thread**: All Avalonia operations dispatched via `Dispatcher.UIThread.InvokeAsync()`
- **TopLevel**: Inspector uses `TopLevel` (not `Window`) to support both desktop and single-view lifetimes
- **Logging**: Logs to stderr (stdout reserved for MCP STDIO transport)
- **DI patterns**:
  - Server: `WebApplicationBuilder` with SignalR + MCP server
  - Connector: `services.AddAvaloniaMcpConnector()` or `hostBuilder.AddAvaloniaMcpConnector()`
- **Test framework**: xUnit + FluentAssertions + NSubstitute
- **InternalsVisibleTo**: Connector grants access to Tests.Unit and Connector.Tests.Unit

## Common Patterns

### Adding a new MCP tool

1. Add method to `IAvaloniaInspector` interface (in Connector/Services)
2. Implement in `AvaloniaInspector` (dispatch to UI thread)
3. Add `InspectorMethod` enum value (in Contracts)
4. Add handler case in `InspectorCommandHandler` (in Connector)
5. Add static tool method in appropriate `*Tools.cs` in Server with `[McpServerTool]` + `[Description]`
6. Tool calls `proxy.SendCommandAsync()` with method + parameters
7. Add tests: inspector unit test, command handler test, tool delegation test

### Adding a new model

1. Create sealed record in `Contracts/Models/` with `required` properties
2. All JSON serialization uses `JsonOptions.Default` (camelCase, ignore nulls)
3. Add serialization round-trip tests

### Element resolution

Elements are resolved by: name, typeName, or path (index-based like `0/2/1`). The `ResolveElement` internal method handles this priority order.

### Multi-app targeting

All MCP tools accept an optional `appId` parameter. When only one app is connected, it's auto-targeted. With multiple apps, use `list_connected_apps` then `set_target_app`, or pass `appId` directly.

## SignalR Protocol

| Direction | Method | Purpose |
|-----------|--------|---------|
| Client -> Hub | `RegisterAppAsync(ConnectedAppInfo)` | App announces itself |
| Hub -> Client | `ExecuteCommandAsync(InspectorRequest)` | Server sends inspection command (returns response directly) |
| Hub -> Client | `DisconnectAsync(string)` | Server asks app to disconnect |
| Hub -> Client | `ShutdownAsync(string)` | Server asks app to terminate its process |

## Avalonia API Gotchas

- `IPointer` is NOT user-implementable — use `MockPointer.Create()` which returns `new Pointer(0, PointerType.Mouse, true)`
- `RaiseEvent()` is on `Interactive`, not `Visual`. Hierarchy: Visual > Layoutable > Interactive > InputElement > Control
- Pointer event constructors are positional-only (no named params) and marked `[Obsolete]` — suppress with `#pragma warning disable CS0618`
- `KeyEventArgs` and `TextInputEventArgs` use parameterless ctor + property setters
- `InputElement.Focus(NavigationMethod, KeyModifiers)` returns bool
- Headless platform (`Avalonia.Headless.XUnit`) works for DOM traversal and events but `RenderTargetBitmap` produces empty output
- `ContentPresenter` is in `Avalonia.Controls.Presenters`
- `Button.OnPointerReleased` checks `IsPointerOver` before firing Click — but `IsPointerOver` is NOT updated by synthetic `RaiseEvent()`. Workaround: raise `Button.ClickEvent` directly after pointer events

## Commit Convention

Use conventional commits for GitVersion compatibility:

- `feat:` new feature
- `fix:` bug fix
- `docs:` documentation only
- `test:` adding or updating tests
- `refactor:` code change that neither fixes a bug nor adds a feature
- `chore:` build, tooling, CI changes
