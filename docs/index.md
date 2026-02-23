# Six.Avalonia.Mcp Documentation

Six.Avalonia.Mcp is a decoupled MCP (Model Context Protocol) architecture that enables AI agents to inspect and interact with running Avalonia desktop applications. A standalone MCP server communicates with Avalonia apps via SignalR, exposing 16 tools over STDIO transport for visual tree inspection, element querying, screenshot capture, and user interaction simulation. Multiple apps can connect simultaneously for multi-app inspection.

**Architecture:** `AI Agent -> STDIO -> MCP Server Console App -> SignalR -> Avalonia App(s) (Connector + Inspector)`

## Documentation Pages

| Page | Description |
|------|-------------|
| [Getting Started](./getting-started.md) | Prerequisites, installation, basic setup, and verifying the connection |
| [Architecture](./architecture.md) | System design, threading model, transport, DI wiring, and element resolution |
| [Integration Guide](./integration-guide.md) | Embedding in your Avalonia app, naming controls, MCP client configuration |
| [Configuration](./configuration.md) | Build properties, logging, environment variables, StyleCop, and coverage |
| [Testing Guide](./testing-guide.md) | Test framework, patterns, coverage thresholds, and adding new tests |
| [Troubleshooting](./troubleshooting.md) | Common issues with symptoms, causes, and solutions |
| [API Reference](./api/index.md) | IAvaloniaInspector interface, 7 model DTOs, 16 MCP tools |

## Quick Reference

### MCP Tools (16 total)

| Tool | Category | Description |
|------|----------|-------------|
| `get_app_metadata` | Metadata | Process info, window dimensions, framework versions |
| `get_visual_tree` | Inspection | Recursive visual tree snapshot with configurable depth |
| `find_element` | Inspection | Query by name, type, CSS class, or selector |
| `get_element_properties` | Inspection | Detailed runtime properties of a specific element |
| `get_element_by_path` | Inspection | Get element properties by visual tree path (e.g., `0/3/1`) |
| `get_ancestor_chain` | Inspection | Parent chain from element to root window |
| `take_screenshot` | Screenshot | Base64 PNG capture with scale 0.5--3.0 |
| `simulate_click` | Interaction | Left/right/middle click with modifiers |
| `simulate_type` | Interaction | Character-by-character text input |
| `set_control_state` | Interaction | Focus/blur, enable/disable, show/hide |
| `simulate_hover` | Interaction | PointerEntered + PointerMoved events |
| `simulate_drag` | Interaction | Press + interpolated moves + release |
| `simulate_keyboard_shortcut` | Interaction | Key combos like Ctrl+S, Alt+F4 |
| `list_connected_apps` | App Management | Lists all Avalonia apps connected via SignalR |
| `set_target_app` | App Management | Sets the default target app for multi-app inspection |
| `kill_app` | App Management | Terminate a connected app gracefully |

### Project Structure

```
Six.Avalonia.Mcp/
  apps/
    McpDemo/                                # Demo application (Six.Avalonia.Mcp.App)
    Six.Avalonia.Mcp.Server/              # Standalone MCP server + SignalR hub
      src/
        Hub/                               # InspectorHub (SignalR)
        Services/                          # IAppRegistry, IInspectorProxy
        Tools/                             # 16 MCP tools in 5 classes
  libs/
    Six.Avalonia.Mcp.Contracts/           # Shared DTOs, hub interfaces, messages (zero deps)
      src/
        Models/                            # 7 DTOs: AppMetadata, BoundsInfo, ConnectedAppInfo, etc.
        Hub/                               # IInspectorHub, IInspectorClient
        Messages/                          # InspectorMethod, InspectorRequest, InspectorResponse
        JsonOptions.cs                     # Shared JSON serializer settings
    Six.Avalonia.Mcp.Connector/           # Avalonia inspector + SignalR client
      src/
        Services/                          # IAvaloniaInspector, AvaloniaInspector, MockPointer
        McpConnectorHost.cs                # DI extension: AddAvaloniaMcpConnector()
        McpConnectorOptions.cs             # ServerUrl, AppName, AutoConnect, ReconnectDelay
        ConnectorService.cs                # IHostedService: SignalR connection + command dispatch
        InspectorCommandHandler.cs         # Dispatches InspectorRequest -> IAvaloniaInspector
  tests/
    Six.Avalonia.Mcp.Tests.Unit/          # Inspector + model tests (headless Avalonia)
    Six.Avalonia.Mcp.Server.Tests.Unit/   # Server tool + registry tests
    Six.Avalonia.Mcp.Connector.Tests.Unit/ # Command handler tests
  docs/                                     # This documentation
```

### Key Dependencies

| Package | Version | Used By | Purpose |
|---------|---------|---------|---------|
| Avalonia | 11.3.11 | Connector | UI framework |
| ModelContextProtocol | 0.8.0-preview.1 | Server | MCP server SDK |
| Microsoft.AspNetCore.SignalR.Client | 10.0.2 | Connector | SignalR client for server communication |
| xUnit | 2.9.3 | Tests | Test framework |
| FluentAssertions | 8.8.0 | Tests | Test assertions |
| NSubstitute | 5.3.0 | Tests | Mocking framework |
| Coverlet | 6.0.4 | Tests | Code coverage |

### License

MIT
