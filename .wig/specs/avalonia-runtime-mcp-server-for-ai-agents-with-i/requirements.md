# Requirements: Avalonia Runtime MCP Server for AI Agents

## Overview

Build a runtime MCP (Model Context Protocol) server library that enables AI agents to inspect and interact with running Avalonia UI applications. The server embeds into Avalonia apps via a single DI registration call and exposes 12 MCP tools for visual tree inspection, element discovery, property extraction, screenshot capture, and human-mimicking interaction simulation.

## Functional Requirements

### FR-1: MCP Server Bootstrapping & Transport

- **FR-1.1**: Provide `AddAvaloniaMcpServer(IServiceCollection)` extension method that registers all services (inspector, MCP server, tools) with a single call.
- **FR-1.2**: Provide `AddAvaloniaMcpServer(IHostBuilder)` overload for host builder integration.
- **FR-1.3**: Use STDIO transport (stdin/stdout) for MCP JSON-RPC 2.0 communication. All application logging MUST go to stderr (already configured in demo app's `Program.cs`).
- **FR-1.4**: Implement MCP `initialize` handshake returning server name (`"Six.Avalonia.Mcp"`), version, and capabilities list.
- **FR-1.5**: Implement `tools/list` returning all registered tool definitions with names, descriptions, and JSON Schema input schemas.
- **FR-1.6**: Handle `tools/call` dispatching to the correct tool implementation and returning structured JSON results.
- **FR-1.7**: Return standard JSON-RPC 2.0 error responses for invalid requests, unknown tools, and tool execution failures.

### FR-2: App Discovery (`get_app_metadata` tool)

- **FR-2.1**: Return process name, PID, main window title, Avalonia version, and .NET runtime version.
- **FR-2.2**: Return window count and dimensions of the primary window.
- **FR-2.3**: All metadata retrieval must execute on the Avalonia UI thread via `Dispatcher.UIThread`.

### FR-3: Visual Tree Inspection (`get_visual_tree` tool)

- **FR-3.1**: Return recursive JSON tree starting from the top-level `Window` (or `VisualRoot`).
- **FR-3.2**: Each node must include: `type` (control type name), `name` (x:Name or null), `bounds` (x, y, width, height), `isVisible`, `isEnabled`, `opacity`, `isFocused`, `isPointerOver`, `classes` (pseudo-classes/style classes), `children` (array).
- **FR-3.3**: Support configurable `maxDepth` parameter (default: 15, max: 50).
- **FR-3.4**: Support optional `elementName` parameter to start traversal from a specific named element instead of root.
- **FR-3.5**: Execute on UI thread; return JSON-serializable model.

### FR-4: Element Discovery (`find_element` tool)

- **FR-4.1**: Query by `name` (exact match on `x:Name`).
- **FR-4.2**: Query by `typeName` (exact match on control type, e.g., `"Button"`, `"TextBox"`).
- **FR-4.3**: Query by `className` (style class applied to control).
- **FR-4.4**: Query by `selector` (simple CSS-like selector, e.g., `"Button#myBtn"`, `"TextBox.error"`).
- **FR-4.5**: Return array of `FindElementResult` objects, each containing: element path (indices from root), type, name, bounds, and a stable identifier for subsequent tool calls.
- **FR-4.6**: Support `maxResults` parameter (default: 10) to limit returned matches.

### FR-5: Ancestor Chain (`get_ancestor_chain` tool)

- **FR-5.1**: Given an element name or path, return array of ancestor nodes from the element to the root.
- **FR-5.2**: Each ancestor includes: type, name, bounds, index in parent.

### FR-6: Element Properties (`get_element_properties` tool)

- **FR-6.1**: Accept element identifier (name, type, or path).
- **FR-6.2**: Return geometry properties: Width, Height, Margin, Padding, Bounds (actual rendered).
- **FR-6.3**: Return state properties: IsVisible, IsEnabled, IsFocused, IsHitTestVisible, ClipToBounds, Opacity.
- **FR-6.4**: Return appearance properties: Background, Foreground, BorderBrush, BorderThickness, CornerRadius (all as string representations).
- **FR-6.5**: Return layout properties: HorizontalAlignment, VerticalAlignment, FlowDirection.
- **FR-6.6**: Return data context type name (e.g., `"MainWindowViewModel"`) — type name only, no values.
- **FR-6.7**: Return list of active pseudo-classes and style classes.
- **FR-6.8**: Do NOT expose sensitive data (connection strings, user data, etc.).

### FR-7: Screenshot Capture (`take_screenshot` tool)

- **FR-7.1**: Capture full window screenshot as base64-encoded PNG.
- **FR-7.2**: Capture specific element screenshot by name/path using `RenderTargetBitmap`.
- **FR-7.3**: Support `scaleFactor` parameter (default: 1.0, range: 0.5–3.0).
- **FR-7.4**: Return base64 string with image dimensions metadata.
- **FR-7.5**: Execute render on UI thread; encode to base64 off-thread if possible.

### FR-8: Click Simulation (`simulate_click` tool)

- **FR-8.1**: Accept element identifier, mouse button (`left`/`right`/`middle`, default: `left`), optional position (relative to element, default: center), and optional modifiers (`shift`/`ctrl`/`alt`).
- **FR-8.2**: Simulate `PointerPressed` + `PointerReleased` events using Avalonia's `RaiseEvent` API with correct `PointerPressedEventArgs`/`PointerReleasedEventArgs` constructors.
- **FR-8.3**: Optionally simulate `PointerMoved` to the element before click (hover-before-click).
- **FR-8.4**: Return `InteractionResult` with success flag, element state after interaction, and any error message.
- **FR-8.5**: Support `clickCount` parameter for double-click scenarios.

### FR-9: Text Input Simulation (`simulate_type` tool)

- **FR-9.1**: Accept element identifier and text string.
- **FR-9.2**: Auto-focus the target element before typing if not already focused.
- **FR-9.3**: Simulate `TextInput` events character-by-character for printable characters.
- **FR-9.4**: Support special keys: `Backspace`, `Enter`, `Tab`, `Escape`, `Delete`, arrow keys.
- **FR-9.5**: Support key modifiers (Ctrl, Shift, Alt) for modified input.
- **FR-9.6**: Return `InteractionResult` with success flag and element state after typing.

### FR-10: Control State Manipulation (`set_control_state` tool)

- **FR-10.1**: Set focus on element via `Focus()` method.
- **FR-10.2**: Toggle `IsEnabled` property (true/false).
- **FR-10.3**: Toggle `IsVisible` property (true/false).
- **FR-10.4**: Return `InteractionResult` with the new state values.

### FR-11: Hover Simulation (`simulate_hover` tool)

- **FR-11.1**: Simulate `PointerEntered` event on target element.
- **FR-11.2**: Simulate `PointerMoved` within element bounds.
- **FR-11.3**: Optionally simulate `PointerExited` after the hover (controlled by `exitAfter` boolean, default: false).
- **FR-11.4**: Return `InteractionResult` confirming hover state.

### FR-12: Drag Simulation (`simulate_drag` tool)

- **FR-12.1**: Accept source element, start point, end point, and mouse button (default: left).
- **FR-12.2**: Simulate: `PointerPressed` at start → multiple `PointerMoved` events along the path → `PointerReleased` at end.
- **FR-12.3**: Support configurable number of intermediate `PointerMoved` steps (default: 10).
- **FR-12.4**: Return `InteractionResult` with success flag.

### FR-13: Keyboard Shortcut Simulation (`simulate_keyboard_shortcut` tool)

- **FR-13.1**: Accept key combination string (e.g., `"Ctrl+S"`, `"Ctrl+Shift+Z"`, `"Alt+F4"`).
- **FR-13.2**: Parse modifier keys and primary key from the combination string.
- **FR-13.3**: Simulate `KeyDown` events for modifiers first, then the primary key, then `KeyUp` in reverse order.
- **FR-13.4**: Accept optional target element (default: currently focused element or main window).
- **FR-13.5**: Return `InteractionResult` with success flag.

## Non-Functional Requirements

### NFR-1: Performance

- **NFR-1.1**: `get_visual_tree` must return JSON for a typical app (100–500 controls) in < 500 ms.
- **NFR-1.2**: `take_screenshot` must complete in < 1 second for a 1920x1080 window at 1x scale.
- **NFR-1.3**: All interaction tools must complete in < 200 ms per action.
- **NFR-1.4**: MCP server startup must add < 100 ms to application launch time.

### NFR-2: Thread Safety

- **NFR-2.1**: ALL Avalonia visual tree access and event simulation MUST execute on the UI thread via `Dispatcher.UIThread.InvokeAsync`.
- **NFR-2.2**: MCP transport (STDIO reads) runs on a background thread; tool execution dispatches to UI thread.
- **NFR-2.3**: No deadlocks: never block the UI thread waiting for MCP I/O.

### NFR-3: Error Handling

- **NFR-3.1**: Element not found → return structured error with searched criteria, not crash.
- **NFR-3.2**: Screenshot failure (e.g., headless mode) → return error with explanation.
- **NFR-3.3**: Invalid parameters → return JSON-RPC error with descriptive message.
- **NFR-3.4**: All tool methods must catch exceptions and return error results, never propagate to the MCP transport layer.

### NFR-4: Testability

- **NFR-4.1**: `IAvaloniaInspector` interface allows mocking the inspector in unit tests.
- **NFR-4.2**: Tool classes accept `IAvaloniaInspector` via constructor injection.
- **NFR-4.3**: Models are pure data objects (records or POCOs) with no Avalonia dependencies.
- **NFR-4.4**: Maintain ≥ 80% code coverage (line, branch, method) as configured in `tests/Directory.Build.props`.
- **NFR-4.5**: Internal methods exposed to test assembly via `InternalsVisibleTo` in `GlobalUsings.cs`.

### NFR-5: Compatibility

- **NFR-5.1**: Target .NET 10.0 (`net10.0`).
- **NFR-5.2**: Avalonia 11.3.11 API compatibility (use correct constructor signatures per memory notes).
- **NFR-5.3**: `ModelContextProtocol` 0.8.0-preview.1 NuGet package.
- **NFR-5.4**: Zero paid dependencies; MIT-licensed.

### NFR-6: Security

- **NFR-6.1**: Do not expose DataContext property values — only type names.
- **NFR-6.2**: Do not expose internal/private Avalonia properties.
- **NFR-6.3**: STDIO transport only in Phase 1 (no network exposure).

### NFR-7: Code Quality

- **NFR-7.1**: Follow existing project conventions: C# 14, nullable enabled, warnings-as-errors.
- **NFR-7.2**: Namespace convention: `Six.Avalonia.Mcp` for library, `Six.Avalonia.Mcp.Tests.Unit` for tests.
- **NFR-7.3**: StyleCop compliance per project `.ruleset`.
- **NFR-7.4**: All public types must have XML documentation comments.

## Out of Scope (Phase 1)

- HTTP/SSE transport (Phase 2)
- Multi-window / multi-app switching
- Property change observation / subscriptions
- Visual diff computation
- Attach mode (external process connection)
- Logical tree traversal (visual tree only)
- Avalonia DevTools integration
