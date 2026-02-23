# MCP Tools Reference

This document provides an overview of all 16 MCP tools available in the Six Avalonia MCP Server. These tools enable AI agents to inspect and interact with running Avalonia desktop applications over the Model Context Protocol.

## Tool Categories

### Metadata

| Tool | Description |
|------|-------------|
| [get_app_metadata](get-app-metadata.md) | Get metadata about the running Avalonia application including process info, window dimensions, and framework versions. |

### Inspection

| Tool | Description |
|------|-------------|
| [get_visual_tree](get-visual-tree.md) | Get a recursive snapshot of the visual tree starting from the main window or a named element. |
| [find_element](find-element.md) | Find elements in the visual tree matching criteria: name, type name, CSS class, or a simple selector. |
| [get_element_properties](get-element-properties.md) | Get detailed runtime properties of a specific element resolved by name, type, or index path. |
| [get_element_by_path](get-element-by-path.md) | Get detailed runtime properties of an element by its index-based visual tree path (e.g., `0/3/1`). |
| [get_ancestor_chain](get-ancestor-chain.md) | Get the ancestor chain from a named element up to the root window. |

### Screenshot

| Tool | Description |
|------|-------------|
| [take_screenshot](take-screenshot.md) | Capture a screenshot of the main window or a specific named element as a base64-encoded PNG. |

### Interaction

| Tool | Description |
|------|-------------|
| [simulate_click](simulate-click.md) | Simulate a mouse click (left, right, or middle) on a named element. |
| [simulate_type](simulate-type.md) | Simulate typing text into a named element character by character. |
| [set_control_state](set-control-state.md) | Set a control state property on a named element (focus, enabled, visible). |
| [simulate_hover](simulate-hover.md) | Simulate a pointer hover on a named element. |
| [simulate_drag](simulate-drag.md) | Simulate a drag operation on a named element. |
| [simulate_keyboard_shortcut](simulate-keyboard-shortcut.md) | Simulate a keyboard shortcut optionally targeted at a named element. |

### App Management

| Tool | Description |
|------|-------------|
| [list_connected_apps](list-connected-apps.md) | List all currently connected Avalonia applications with their app IDs, names, process info, and connection time. |
| [set_target_app](set-target-app.md) | Set the target Avalonia application for subsequent inspection commands. |
| [kill_app](kill-app.md) | Terminate a connected Avalonia application gracefully. |

## Common Patterns

### Multi-App Targeting

All inspection tools (Metadata, Inspection, Screenshot, and Interaction categories) accept an optional `appId` parameter. When only one app is connected, it is auto-targeted. When multiple apps are connected, use `list_connected_apps` to discover available app IDs, then either pass `appId` directly to individual tools or call `set_target_app` to set a default target for subsequent commands.

### JSON Response Format

All tools return JSON responses using camelCase property naming. Null values are omitted from the output.

### Error Handling

All errors across every tool are returned in a consistent format:

```json
{ "error": "Descriptive error message." }
```

Common error scenarios shared by most tools:

- `"Main window is not available."` -- The application has no active main window.
- `"Element 'xxx' not found."` -- The specified named element does not exist in the visual tree.
- `"Element 'xxx' is not an Interactive element..."` -- The resolved element does not support the requested interaction.
- `"App 'xxx' is not connected."` -- The specified app ID does not match any connected application.

### Dependency Injection

Tools are defined as static methods in the `Six.Avalonia.Mcp.Server.Tools` namespace and receive their dependencies via DI:

- **Inspection tools** (Metadata, Inspection, Screenshot, Interaction) receive `IInspectorProxy`, which sends commands to connected Avalonia apps over SignalR.
- **App Management tools** receive `IAppRegistry`, which tracks connected apps and the current target app.

Registration is handled in the Server's `Program.cs`:

```csharp
services.AddMcpServer(options => { ... })
  .WithStdioServerTransport()
  .WithToolsFromAssembly();

services.AddSingleton<IAppRegistry, AppRegistry>();
services.AddSingleton<IInspectorProxy, InspectorProxy>();
```

### Transport

The MCP Server uses two separate transport layers:

- **STDIO (Agent <-> Server):** The MCP server communicates with the AI agent over STDIO using JSON-RPC. Each message is a single line of JSON terminated by a newline character.
- **SignalR WebSocket (Server <-> App):** The server communicates with connected Avalonia applications over SignalR WebSocket on port 52342 (`/inspector` endpoint). Tools call `IInspectorProxy.SendCommandAsync()` which routes commands to the appropriate app connection.
