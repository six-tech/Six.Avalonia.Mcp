# IAvaloniaInspector

Service interface for inspecting and interacting with a running Avalonia application's visual tree.

- **Namespace**: `Six.Avalonia.Mcp.Connector.Services`
- **Source**: `libs/Six.Avalonia.Mcp.Connector/src/Services/IAvaloniaInspector.cs`
- **Implementation**: `libs/Six.Avalonia.Mcp.Connector/src/Services/AvaloniaInspector.cs`

---

## Discovery

### GetAppMetadataAsync

```csharp
Task<AppMetadata> GetAppMetadataAsync(CancellationToken ct = default);
```

Gets metadata about the running application including process info, window dimensions, and framework versions.

**Returns**: An `AppMetadata` record with process name, PID, window title, Avalonia version, .NET version, window count, and window dimensions.

---

## Inspection

### GetVisualTreeAsync

```csharp
Task<VisualTreeNode> GetVisualTreeAsync(
    int maxDepth = 15,
    string? elementName = null,
    CancellationToken ct = default);
```

Builds a recursive snapshot of the visual tree starting from the top-level view or a named subtree root.

| Parameter | Default | Description |
|-----------|---------|-------------|
| `maxDepth` | `15` | Maximum recursion depth. Clamped to the range 1--50. |
| `elementName` | `null` | If specified, the tree starts from the element with this `x:Name`. |

**Returns**: A `VisualTreeNode` representing the root, with `Children` populated recursively up to `maxDepth`.

---

### FindElementAsync

```csharp
Task<IReadOnlyList<FindElementResult>> FindElementAsync(
    string? name = null,
    string? typeName = null,
    string? className = null,
    string? selector = null,
    int maxResults = 10,
    CancellationToken ct = default);
```

Finds elements in the visual tree matching the specified criteria. All non-null criteria are combined with AND logic.

| Parameter | Default | Description |
|-----------|---------|-------------|
| `name` | `null` | Match by `x:Name` (exact). |
| `typeName` | `null` | Match by control type name (e.g., `"Button"`). |
| `className` | `null` | Match by CSS class (e.g., `"error"`). |
| `selector` | `null` | CSS-like selector parsed into name/type/class criteria (see below). |
| `maxResults` | `10` | Maximum number of results to return. |

**Selector syntax**:

| Pattern | Meaning |
|---------|---------|
| `Button` | Matches elements of type `Button` |
| `#myId` | Matches elements with `x:Name` equal to `myId` |
| `.error` | Matches elements with CSS class `error` |
| `Button#myBtn` | Matches `Button` elements named `myBtn` |
| `Button.primary` | Matches `Button` elements with class `primary` |

When a selector is provided, its parsed values are merged with the explicit `name`, `typeName`, and `className` parameters (explicit params take precedence).

**Returns**: A list of `FindElementResult` records, each containing the element's type, name, bounds, index path, and a stable `ElementId`.

---

### GetAncestorChainAsync

```csharp
Task<IReadOnlyList<VisualTreeNode>> GetAncestorChainAsync(
    string elementName,
    CancellationToken ct = default);
```

Returns the ancestor chain from a named element up to the visual tree root.

| Parameter | Description |
|-----------|-------------|
| `elementName` | The `x:Name` of the target element (required). |

**Returns**: A list of `VisualTreeNode` records ordered from the target element to the root. Each node has `depth=0` (no children populated).

---

### GetElementPropertiesAsync

```csharp
Task<ElementProperties> GetElementPropertiesAsync(
    string? elementName = null,
    string? typeName = null,
    string? path = null,
    CancellationToken ct = default);
```

Gets detailed runtime properties of a specific element.

| Parameter | Default | Description |
|-----------|---------|-------------|
| `elementName` | `null` | Resolve by `x:Name`. |
| `typeName` | `null` | Resolve by type name (returns the first match). |
| `path` | `null` | Resolve by child index path, e.g., `"0/2/1"`. |

**Resolution order**: `elementName` is tried first, then `typeName`, then `path`. At least one must be provided.

**Returns**: An `ElementProperties` record with 23 properties across identity, geometry, state, appearance, layout, data, and style categories.

---

## Screenshots

### TakeScreenshotAsync

```csharp
Task<(string Base64Png, int Width, int Height)> TakeScreenshotAsync(
    string? elementName = null,
    double scaleFactor = 1.0,
    CancellationToken ct = default);
```

Captures a PNG screenshot of the top-level view or a specific element.

| Parameter | Default | Description |
|-----------|---------|-------------|
| `elementName` | `null` | If specified, captures only this element. Otherwise captures the top-level view. |
| `scaleFactor` | `1.0` | Render scale multiplier. Clamped to the range 0.5--3.0. |

**Returns**: A tuple of `(Base64Png, Width, Height)` where `Base64Png` is the base64-encoded PNG data and `Width`/`Height` are the image dimensions in pixels.

---

## Interactions

### SimulateClickAsync

```csharp
Task<InteractionResult> SimulateClickAsync(
    string elementName,
    string button = "left",
    double? x = null,
    double? y = null,
    string? modifiers = null,
    int clickCount = 1,
    CancellationToken ct = default);
```

Simulates a mouse click on the specified element.

| Parameter | Default | Description |
|-----------|---------|-------------|
| `elementName` | -- | Target element `x:Name` (required). |
| `button` | `"left"` | Mouse button: `"left"`, `"right"`, or `"middle"`. |
| `x` | `null` | X coordinate relative to the element. Defaults to element center. |
| `y` | `null` | Y coordinate relative to the element. Defaults to element center. |
| `modifiers` | `null` | Key modifiers joined with `+`: `"ctrl"`, `"shift"`, `"alt"`, `"meta"`. Example: `"ctrl+shift"`. |
| `clickCount` | `1` | Number of clicks (e.g., `2` for double-click). |

**Returns**: An `InteractionResult` indicating success/failure and the element's post-interaction state.

---

### SimulateTypeAsync

```csharp
Task<InteractionResult> SimulateTypeAsync(
    string elementName,
    string text,
    CancellationToken ct = default);
```

Types text into the specified element character by character. The element is focused before typing begins.

| Parameter | Description |
|-----------|-------------|
| `elementName` | Target element `x:Name` (required). |
| `text` | The text to type. Supports special characters: `\b` (Backspace), `\n` or `\r` (Enter), `\t` (Tab). |

**Returns**: An `InteractionResult` indicating success/failure and the element's post-interaction state.

---

### SetControlStateAsync

```csharp
Task<InteractionResult> SetControlStateAsync(
    string elementName,
    string stateType,
    bool value,
    CancellationToken ct = default);
```

Sets a control state property on the specified element.

| Parameter | Description |
|-----------|-------------|
| `elementName` | Target element `x:Name` (required). |
| `stateType` | One of: `"focus"`, `"enabled"`, `"visible"`. |
| `value` | The desired state value. |

**State type details**:

| State | `true` | `false` |
|-------|--------|---------|
| `"focus"` | Focuses the `InputElement` | Blurs by focusing the parent window |
| `"enabled"` | Sets `Control.IsEnabled = true` | Sets `Control.IsEnabled = false` |
| `"visible"` | Sets `Control.IsVisible = true` | Sets `Control.IsVisible = false` |

**Returns**: An `InteractionResult` indicating success/failure and the element's post-interaction state.

---

### SimulateHoverAsync

```csharp
Task<InteractionResult> SimulateHoverAsync(
    string elementName,
    bool exitAfter = false,
    CancellationToken ct = default);
```

Simulates a pointer hover on the specified element by dispatching `PointerEntered` and `PointerMoved` events.

| Parameter | Default | Description |
|-----------|---------|-------------|
| `elementName` | -- | Target element `x:Name` (required). |
| `exitAfter` | `false` | If `true`, also dispatches `PointerExited` after the hover events. |

**Returns**: An `InteractionResult` indicating success/failure and the element's post-interaction state.

---

### SimulateDragAsync

```csharp
Task<InteractionResult> SimulateDragAsync(
    string elementName,
    double startX,
    double startY,
    double endX,
    double endY,
    string button = "left",
    int steps = 10,
    CancellationToken ct = default);
```

Simulates a drag operation from start coordinates to end coordinates on the specified element. The operation consists of a pointer press, a series of interpolated pointer moves, and a pointer release.

| Parameter | Default | Description |
|-----------|---------|-------------|
| `elementName` | -- | Target element `x:Name` (required). |
| `startX` | -- | Starting X coordinate relative to the element (required). |
| `startY` | -- | Starting Y coordinate relative to the element (required). |
| `endX` | -- | Ending X coordinate relative to the element (required). |
| `endY` | -- | Ending Y coordinate relative to the element (required). |
| `button` | `"left"` | Mouse button: `"left"`, `"right"`, or `"middle"`. |
| `steps` | `10` | Number of intermediate move events. Clamped to a minimum of 1. |

**Returns**: An `InteractionResult` indicating success/failure and the element's post-interaction state.

---

### SimulateKeyboardShortcutAsync

```csharp
Task<InteractionResult> SimulateKeyboardShortcutAsync(
    string keys,
    string? elementName = null,
    CancellationToken ct = default);
```

Simulates a keyboard shortcut by dispatching `KeyDown` and `KeyUp` events.

| Parameter | Default | Description |
|-----------|---------|-------------|
| `keys` | -- | Key combination string (required). Format: `"Ctrl+S"`, `"Alt+F4"`, `"Shift+Enter"`. |
| `elementName` | `null` | Target element `x:Name`. If not specified, uses the currently focused element or the top-level view. |

**Supported modifiers**: `ctrl`/`control`, `shift`, `alt`, `meta`/`win`/`cmd`. Modifier names are case-insensitive. The primary key must be a valid member of the `Avalonia.Input.Key` enum.

**Returns**: An `InteractionResult` indicating success/failure and the element's post-interaction state.

---

## Implementation Notes (AvaloniaInspector)

### TopLevel Support

The inspector uses `TopLevel` instead of `Window` to support both desktop and single-view lifetimes. The `GetTopLevel()` method resolves the top-level view via:

1. `IClassicDesktopStyleApplicationLifetime` -- returns the main window.
2. `ISingleViewApplicationLifetime` -- calls `TopLevel.GetTopLevel(mainView)` on the single view.

This makes the inspector compatible with desktop apps, mobile apps, and embedded views.

### Threading

All methods dispatch their work to the UI thread via `Dispatcher.UIThread.InvokeAsync()`. This means callers can safely invoke any method from a background thread.

### Extensibility

The `GetTopLevel()` method is `protected virtual`, returning `TopLevel?`. Subclasses can override it for testing scenarios or multi-window applications.

### Internal Helpers

The following internal methods are accessible via `InternalsVisibleTo` for unit testing:

| Method | Description |
|--------|-------------|
| `ResolveElement(Visual root, string? name, string? typeName, string? path)` | Finds an element by name, type, or index path. |
| `BuildPath(Visual root, Visual target)` | Builds a child index path from root to target. |
| `GenerateElementId(Visual root, Visual target)` | Creates a stable ID in `"path:0/2/1"` format. |
| `BuildTreeNode(Visual visual, int currentDepth, int maxDepth)` | Recursively builds a `VisualTreeNode`. |
| `ParseSelector(string selector)` | Parses CSS-like selector strings into name/type/class components. |
| `MatchesFilter(Visual visual, string? name, string? typeName, string? className)` | Tests whether a visual matches all non-null filter criteria (AND logic). |
| `BuildElementProperties(Visual root, Visual visual)` | Extracts all property groups into an `ElementProperties` record. |
| `MapMouseButton(string button)` | Maps `"left"`/`"right"`/`"middle"` to `MouseButton` and `PointerUpdateKind`. |
| `ParseModifiers(string? modifiers)` | Parses a modifier string like `"ctrl+shift"` to `KeyModifiers` and `RawInputModifiers`. |
| `ParseKeyCombination(string keys)` | Parses `"Ctrl+S"` into `KeyModifiers` and `Key`. |

### Pointer Handling

`IPointer` is not directly implementable in Avalonia. The inspector creates pointer instances using `new Pointer(0, PointerType.Mouse, true)` via an internal `MockPointer.Create()` helper.

---

## DI Registration

The `McpConnectorHost` static class provides two extension methods for registration in Avalonia apps:

```csharp
using Six.Avalonia.Mcp.Connector;

// On IServiceCollection
services.AddAvaloniaMcpConnector(options =>
{
    options.ServerUrl = "http://localhost:52342/inspector";
    options.AppName = "My App";
});

// Or on IHostBuilder
hostBuilder.AddAvaloniaMcpConnector(options =>
{
    options.ServerUrl = "http://localhost:52342/inspector";
    options.AppName = "My App";
});
```

`AddAvaloniaMcpConnector` registers:

- `IAvaloniaInspector` as a singleton (implemented by `AvaloniaInspector`)
- `InspectorCommandHandler` as a singleton (dispatches remote commands from the server)
- `ConnectorService` as a hosted service (manages the SignalR connection)

Note: The MCP server and STDIO transport are in the separate Server project (`apps/Six.Avalonia.Mcp.Server`), not in the Connector. The Connector only handles the app-side inspection and SignalR communication.
