# Design: Avalonia Runtime MCP Server for AI Agents

## Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│  Avalonia Application (Demo App)                        │
│  Namespace: Six.Avalonia.Mcp.App                       │
│                                                         │
│  Program.cs                                             │
│    Host.CreateDefaultBuilder(args)                      │
│      .AddAvaloniaMcpServer()  ◄── single-line DI       │
│      .ConfigureLogging(stderr)                          │
│                                                         │
│  ┌───────────────────────────────────────────────────┐  │
│  │  Six.Avalonia.Mcp (NuGet Library)           │  │
│  │                                                   │  │
│  │  ┌─────────────┐   ┌──────────────────────────┐   │  │
│  │  │ McpServerHost│   │  IAvaloniaInspector      │   │  │
│  │  │ (DI Setup)   │   │  AvaloniaInspector       │   │  │
│  │  └─────────────┘   │  (UI thread dispatch)     │   │  │
│  │                     └──────────────────────────┘   │  │
│  │                              ▲                      │  │
│  │  ┌───────────────────────────┼───────────────────┐  │  │
│  │  │         MCP Tool Classes  │                   │  │  │
│  │  │  ┌──────────────┐  ┌─────┴────────┐          │  │  │
│  │  │  │InspectionTools│  │InteractionTools│         │  │  │
│  │  │  └──────────────┘  └──────────────┘          │  │  │
│  │  │  ┌──────────────┐  ┌──────────────┐          │  │  │
│  │  │  │ScreenshotTools│  │MetadataTools │          │  │  │
│  │  │  └──────────────┘  └──────────────┘          │  │  │
│  │  └───────────────────────────────────────────────┘  │  │
│  │                                                   │  │
│  │  STDIO Transport ◄──── JSON-RPC 2.0 ────► Agent  │  │
│  │  (stdin/stdout)                                   │  │
│  └───────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
```

## Project Structure

```
libs/Six.Avalonia.Mcp/
├── Six.Avalonia.Mcp.csproj
├── GlobalUsings.cs                    # Global imports + InternalsVisibleTo
├── README.md
└── src/
    ├── McpServerHost.cs               # DI extension methods
    ├── Models/
    │   ├── AppMetadata.cs             # App process/version info
    │   ├── BoundsInfo.cs              # Element bounds (x, y, w, h)
    │   ├── VisualTreeNode.cs          # Recursive tree node
    │   ├── FindElementResult.cs       # Element search result
    │   ├── ElementProperties.cs       # Runtime property bag
    │   └── InteractionResult.cs       # Action result + state
    ├── Services/
    │   ├── IAvaloniaInspector.cs      # Inspector interface
    │   └── AvaloniaInspector.cs       # Implementation (UI thread)
    └── Tools/
        ├── InspectionTools.cs         # get_visual_tree, find_element, get_ancestor_chain
        ├── ScreenshotTools.cs         # take_screenshot
        ├── InteractionTools.cs        # simulate_click, simulate_type, set_control_state,
        │                              # simulate_hover, simulate_drag, simulate_keyboard_shortcut
        └── MetadataTools.cs           # get_app_metadata

tests/Six.Avalonia.Mcp.Tests.Unit/
├── Six.Avalonia.Mcp.Tests.Unit.csproj
├── GlobalUsings.cs
└── src/
    ├── Models/
    │   ├── AppMetadataTests.cs
    │   ├── BoundsInfoTests.cs
    │   ├── VisualTreeNodeTests.cs
    │   ├── FindElementResultTests.cs
    │   ├── ElementPropertiesTests.cs
    │   └── InteractionResultTests.cs
    ├── Services/
    │   └── AvaloniaInspectorTests.cs
    └── Tools/
        ├── InspectionToolsTests.cs
        ├── ScreenshotToolsTests.cs
        ├── InteractionToolsTests.cs
        └── MetadataToolsTests.cs
```

## Data Models

All models are C# records for immutability and value semantics. They live in `Six.Avalonia.Mcp.Models` namespace and have zero Avalonia dependencies (pure serialization types).

### AppMetadata

```csharp
namespace Six.Avalonia.Mcp.Models;

/// <summary>
/// Basic metadata about the running Avalonia application.
/// </summary>
public sealed record AppMetadata
{
    public required string ProcessName { get; init; }
    public required int ProcessId { get; init; }
    public required string MainWindowTitle { get; init; }
    public required string AvaloniaVersion { get; init; }
    public required string DotNetVersion { get; init; }
    public required int WindowCount { get; init; }
    public required double WindowWidth { get; init; }
    public required double WindowHeight { get; init; }
}
```

### BoundsInfo

```csharp
/// <summary>
/// Represents the bounds of a visual element.
/// </summary>
public sealed record BoundsInfo
{
    public required double X { get; init; }
    public required double Y { get; init; }
    public required double Width { get; init; }
    public required double Height { get; init; }
}
```

### VisualTreeNode

```csharp
/// <summary>
/// Represents a node in the Avalonia visual tree.
/// </summary>
public sealed record VisualTreeNode
{
    public required string Type { get; init; }
    public string? Name { get; init; }
    public required BoundsInfo Bounds { get; init; }
    public required bool IsVisible { get; init; }
    public required bool IsEnabled { get; init; }
    public required double Opacity { get; init; }
    public required bool IsFocused { get; init; }
    public required bool IsPointerOver { get; init; }
    public required IReadOnlyList<string> Classes { get; init; }
    public required IReadOnlyList<VisualTreeNode> Children { get; init; }
}
```

### FindElementResult

```csharp
/// <summary>
/// Result of finding an element in the visual tree.
/// </summary>
public sealed record FindElementResult
{
    public required string Type { get; init; }
    public string? Name { get; init; }
    public required BoundsInfo Bounds { get; init; }
    public required IReadOnlyList<int> Path { get; init; }
    public required string ElementId { get; init; }  // Stable identifier: "path:0/2/1"
}
```

### ElementProperties

```csharp
/// <summary>
/// Represents the runtime properties of an Avalonia visual element.
/// </summary>
public sealed record ElementProperties
{
    // Identity
    public required string Type { get; init; }
    public string? Name { get; init; }
    public required string ElementId { get; init; }

    // Geometry
    public required BoundsInfo Bounds { get; init; }
    public double Width { get; init; }
    public double Height { get; init; }
    public string? Margin { get; init; }   // e.g., "10,5,10,5"
    public string? Padding { get; init; }

    // State
    public required bool IsVisible { get; init; }
    public required bool IsEnabled { get; init; }
    public required bool IsFocused { get; init; }
    public required bool IsHitTestVisible { get; init; }
    public required bool ClipToBounds { get; init; }
    public required double Opacity { get; init; }

    // Appearance (string representations)
    public string? Background { get; init; }
    public string? Foreground { get; init; }
    public string? BorderBrush { get; init; }
    public string? BorderThickness { get; init; }
    public string? CornerRadius { get; init; }

    // Layout
    public string? HorizontalAlignment { get; init; }
    public string? VerticalAlignment { get; init; }
    public string? FlowDirection { get; init; }

    // Data
    public string? DataContextType { get; init; }

    // Styles
    public required IReadOnlyList<string> Classes { get; init; }
    public required IReadOnlyList<string> PseudoClasses { get; init; }
}
```

### InteractionResult

```csharp
/// <summary>
/// Result of an interaction simulation (click, type, etc.).
/// </summary>
public sealed record InteractionResult
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ElementType { get; init; }
    public string? ElementName { get; init; }

    // Post-interaction state
    public bool? IsFocused { get; init; }
    public bool? IsEnabled { get; init; }
    public bool? IsVisible { get; init; }
    public bool? IsPointerOver { get; init; }
}
```

## Service Layer

### IAvaloniaInspector Interface

```csharp
namespace Six.Avalonia.Mcp.Services;

/// <summary>
/// Service for inspecting and interacting with a running Avalonia application's visual tree.
/// </summary>
public interface IAvaloniaInspector
{
    // Discovery
    Task<AppMetadata> GetAppMetadataAsync(CancellationToken ct = default);

    // Inspection
    Task<VisualTreeNode> GetVisualTreeAsync(int maxDepth = 15, string? elementName = null, CancellationToken ct = default);
    Task<IReadOnlyList<FindElementResult>> FindElementAsync(string? name = null, string? typeName = null, string? className = null, string? selector = null, int maxResults = 10, CancellationToken ct = default);
    Task<IReadOnlyList<VisualTreeNode>> GetAncestorChainAsync(string elementName, CancellationToken ct = default);
    Task<ElementProperties> GetElementPropertiesAsync(string? elementName = null, string? typeName = null, string? path = null, CancellationToken ct = default);

    // Screenshots
    Task<(string Base64Png, int Width, int Height)> TakeScreenshotAsync(string? elementName = null, double scaleFactor = 1.0, CancellationToken ct = default);

    // Interactions
    Task<InteractionResult> SimulateClickAsync(string elementName, string button = "left", double? x = null, double? y = null, string? modifiers = null, int clickCount = 1, CancellationToken ct = default);
    Task<InteractionResult> SimulateTypeAsync(string elementName, string text, CancellationToken ct = default);
    Task<InteractionResult> SetControlStateAsync(string elementName, string stateType, bool value, CancellationToken ct = default);
    Task<InteractionResult> SimulateHoverAsync(string elementName, bool exitAfter = false, CancellationToken ct = default);
    Task<InteractionResult> SimulateDragAsync(string elementName, double startX, double startY, double endX, double endY, string button = "left", int steps = 10, CancellationToken ct = default);
    Task<InteractionResult> SimulateKeyboardShortcutAsync(string keys, string? elementName = null, CancellationToken ct = default);
}
```

### AvaloniaInspector Implementation

The `AvaloniaInspector` class implements `IAvaloniaInspector` and is the sole bridge between MCP tools and the Avalonia visual tree. Key design decisions:

1. **UI Thread Dispatch**: Every method wraps its Avalonia-touching logic in `Dispatcher.UIThread.InvokeAsync(...)`. This is non-negotiable — Avalonia controls are thread-affine.

2. **Element Resolution Strategy**: Elements are resolved in order of preference:
   - By `elementName` → walk tree looking for matching `Name` property
   - By `typeName` → walk tree looking for matching type name
   - By `path` → navigate tree by index path (e.g., `"0/2/1"`)
   - By `selector` → simple CSS-like selector parsing (`Type#name`, `Type.class`)

3. **Window Access**: Uses `Application.Current?.ApplicationLifetime` cast to `IClassicDesktopStyleApplicationLifetime` to get `MainWindow` and `Windows` collection.

4. **Element ID Generation**: Stable path-based IDs like `"path:0/2/1"` (indices from root). These can be reused across calls within the same app state.

#### Internal Helper Methods

```csharp
// Resolve an element from various identifiers (name, type, path)
internal Visual? ResolveElement(Visual root, string? name, string? typeName, string? path);

// Build index path from root to target
internal IReadOnlyList<int> BuildPath(Visual root, Visual target);

// Generate stable element ID
internal string GenerateElementId(Visual root, Visual target);

// Walk visual tree recursively with depth limit
internal VisualTreeNode BuildTreeNode(Visual visual, int currentDepth, int maxDepth);

// Render a visual to base64 PNG
internal Task<(string Base64, int Width, int Height)> RenderToBase64Async(Visual visual, double scale);
```

## MCP Tool Classes

Each tool class is decorated with `[McpServerToolType]` (from ModelContextProtocol SDK) and contains static methods decorated with `[McpServerTool]`. Tools receive `IAvaloniaInspector` via method injection (the MCP SDK resolves from DI).

### Tool Registration Pattern

```csharp
using ModelContextProtocol.Server;

namespace Six.Avalonia.Mcp.Tools;

[McpServerToolType]
public static class InspectionTools
{
    [McpServerTool(Name = "get_visual_tree", Description = "...")]
    public static async Task<string> GetVisualTree(
        IAvaloniaInspector inspector,
        int maxDepth = 15,
        string? elementName = null,
        CancellationToken ct = default)
    {
        var tree = await inspector.GetVisualTreeAsync(maxDepth, elementName, ct);
        return JsonSerializer.Serialize(tree, JsonOptions.Default);
    }
}
```

### JSON Serialization

Use a shared `JsonSerializerOptions` instance configured with:
- `PropertyNamingPolicy = JsonNamingPolicy.CamelCase`
- `DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull`
- `WriteIndented = false` (compact for MCP transport)

Define this in a static helper:

```csharp
namespace Six.Avalonia.Mcp;

internal static class JsonOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };
}
```

### Tool Definitions

#### MetadataTools

| Tool | Method | Parameters | Returns |
|------|--------|-----------|---------|
| `get_app_metadata` | `GetAppMetadata` | none | `AppMetadata` JSON |

#### InspectionTools

| Tool | Method | Parameters | Returns |
|------|--------|-----------|---------|
| `get_visual_tree` | `GetVisualTree` | `maxDepth?`, `elementName?` | `VisualTreeNode` JSON |
| `find_element` | `FindElement` | `name?`, `typeName?`, `className?`, `selector?`, `maxResults?` | `FindElementResult[]` JSON |
| `get_element_properties` | `GetElementProperties` | `elementName?`, `typeName?`, `path?` | `ElementProperties` JSON |
| `get_ancestor_chain` | `GetAncestorChain` | `elementName` | `VisualTreeNode[]` JSON |

#### ScreenshotTools

| Tool | Method | Parameters | Returns |
|------|--------|-----------|---------|
| `take_screenshot` | `TakeScreenshot` | `elementName?`, `scaleFactor?` | JSON with `base64Png`, `width`, `height` |

#### InteractionTools

| Tool | Method | Parameters | Returns |
|------|--------|-----------|---------|
| `simulate_click` | `SimulateClick` | `elementName`, `button?`, `x?`, `y?`, `modifiers?`, `clickCount?` | `InteractionResult` JSON |
| `simulate_type` | `SimulateType` | `elementName`, `text` | `InteractionResult` JSON |
| `set_control_state` | `SetControlState` | `elementName`, `stateType`, `value` | `InteractionResult` JSON |
| `simulate_hover` | `SimulateHover` | `elementName`, `exitAfter?` | `InteractionResult` JSON |
| `simulate_drag` | `SimulateDrag` | `elementName`, `startX`, `startY`, `endX`, `endY`, `button?`, `steps?` | `InteractionResult` JSON |
| `simulate_keyboard_shortcut` | `SimulateKeyboardShortcut` | `keys`, `elementName?` | `InteractionResult` JSON |

## DI Registration (McpServerHost)

```csharp
namespace Six.Avalonia.Mcp;

/// <summary>
/// Provides extension methods for embedding the Avalonia MCP inspection server into a host.
/// </summary>
public static class McpServerHost
{
    /// <summary>
    /// Adds the Avalonia MCP inspection server to the service collection.
    /// Registers the inspector service and configures the MCP server with STDIO transport.
    /// </summary>
    public static IServiceCollection AddAvaloniaMcpServer(this IServiceCollection services)
    {
        services.AddSingleton<IAvaloniaInspector, AvaloniaInspector>();

        services.AddMcpServer(options =>
        {
            options.ServerInfo = new()
            {
                Name = "Six.Avalonia.Mcp",
                Version = typeof(McpServerHost).Assembly
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                    ?.InformationalVersion ?? "0.0.0",
            };
        })
        .WithStdioServerTransport()
        .WithToolsFromAssembly(typeof(McpServerHost).Assembly);

        return services;
    }

    /// <summary>
    /// Adds the Avalonia MCP inspection server to the host builder.
    /// </summary>
    public static IHostBuilder AddAvaloniaMcpServer(this IHostBuilder hostBuilder)
    {
        return hostBuilder.ConfigureServices((_, services) => services.AddAvaloniaMcpServer());
    }
}
```

**Key design decisions**:
- `AvaloniaInspector` is registered as **singleton** (one inspector per app lifetime).
- MCP SDK's `AddMcpServer` handles JSON-RPC 2.0 protocol, tool discovery, and invocation.
- `WithStdioServerTransport()` reads from stdin, writes to stdout.
- `WithToolsFromAssembly()` auto-discovers all `[McpServerToolType]` classes in the assembly.
- Server version is read from assembly `InformationalVersion` attribute (set by GitVersion).

## Interaction Simulation Design

### Pointer Event Construction (Avalonia 11.3.11 API)

Critical: Avalonia 11.3.11 uses positional-only constructors for pointer event args. The exact signatures are:

```csharp
// PointerPressedEventArgs — positional only, no named params
new PointerPressedEventArgs(
    source,           // object
    pointer,          // IPointer
    rootVisual,       // Visual
    rootVisualPosition, // Point
    timestamp,        // ulong
    properties,       // PointerPointProperties
    modifiers,        // KeyModifiers
    clickCount        // int (default 1)
)

// PointerReleasedEventArgs
new PointerReleasedEventArgs(
    source,           // object
    pointer,          // IPointer
    rootVisual,       // Visual
    rootVisualPosition, // Point
    timestamp,        // ulong
    properties,       // PointerPointProperties
    modifiers,        // KeyModifiers
    initialPressMouseButton // MouseButton
)

// PointerEventArgs (for Enter/Move/Exit)
new PointerEventArgs(
    routedEvent,      // RoutedEvent
    source,           // object
    pointer,          // IPointer
    rootVisual,       // Visual
    rootVisualPosition, // Point
    timestamp,        // ulong
    properties,       // PointerPointProperties
    modifiers         // KeyModifiers
)

// PointerPointProperties — simplest constructor
new PointerPointProperties(
    RawInputModifiers,  // RawInputModifiers
    PointerUpdateKind   // PointerUpdateKind
)
```

### Mock Pointer Implementation

Avalonia's `IPointer` interface is needed for event args construction. We need an internal mock:

```csharp
internal sealed class MockPointer : IPointer
{
    public int Id { get; } = 0;
    public void Capture(IInputElement? element) { }
    public IInputElement? Captured => null;
    public PointerType Type => PointerType.Mouse;
}
```

### Keyboard Event Simulation

For `simulate_type` and `simulate_keyboard_shortcut`:

```csharp
// KeyDown event
var keyArgs = new KeyEventArgs
{
    RoutedEvent = InputElement.KeyDownEvent,
    Key = key,
    KeyModifiers = modifiers,
};
element.RaiseEvent(keyArgs);

// TextInput event (for printable chars)
var textArgs = new TextInputEventArgs
{
    RoutedEvent = InputElement.TextInputEvent,
    Text = character.ToString(),
};
element.RaiseEvent(textArgs);
```

### Key Combination Parsing

For `simulate_keyboard_shortcut`, parse strings like `"Ctrl+Shift+S"`:

```csharp
internal static (KeyModifiers Modifiers, Key PrimaryKey) ParseKeyCombination(string keys)
{
    var modifiers = KeyModifiers.None;
    Key primaryKey = Key.None;

    var parts = keys.Split('+', StringSplitOptions.TrimEntries);
    foreach (var part in parts)
    {
        if (part.Equals("Ctrl", StringComparison.OrdinalIgnoreCase))
            modifiers |= KeyModifiers.Control;
        else if (part.Equals("Shift", StringComparison.OrdinalIgnoreCase))
            modifiers |= KeyModifiers.Shift;
        else if (part.Equals("Alt", StringComparison.OrdinalIgnoreCase))
            modifiers |= KeyModifiers.Alt;
        else if (Enum.TryParse<Key>(part, ignoreCase: true, out var key))
            primaryKey = key;
    }

    return (modifiers, primaryKey);
}
```

## Error Handling Strategy

### Tool-Level Error Wrapping

Every tool method follows this pattern:

```csharp
[McpServerTool(Name = "tool_name")]
public static async Task<string> ToolMethod(IAvaloniaInspector inspector, ...)
{
    try
    {
        var result = await inspector.SomeMethodAsync(...);
        return JsonSerializer.Serialize(result, JsonOptions.Default);
    }
    catch (Exception ex)
    {
        var error = new { error = ex.Message, tool = "tool_name" };
        return JsonSerializer.Serialize(error, JsonOptions.Default);
    }
}
```

### Inspector-Level Error Handling

The inspector catches Avalonia-specific exceptions and wraps them:

- `InvalidOperationException` — element not found, invalid state
- `ArgumentException` — invalid parameters (bad path, unknown key)
- `TimeoutException` — UI thread dispatch timeout
- General `Exception` — unexpected errors, logged and re-thrown

## Testing Strategy

### Unit Test Approach

1. **Model tests**: Verify record construction, serialization, and equality.
2. **Inspector tests**: Mock Avalonia's visual tree using NSubstitute for `IAvaloniaInspector`.
3. **Tool tests**: Inject mock `IAvaloniaInspector`, verify correct delegation and JSON serialization.
4. **Helper tests**: Test `ParseKeyCombination`, `BuildPath`, `GenerateElementId`, `ResolveElement` directly (exposed via `InternalsVisibleTo`).

### Test Patterns

```csharp
// Tool test pattern — mock inspector, verify JSON output
[Fact]
public async Task GetVisualTree_ReturnsSerializedTree()
{
    var inspector = Substitute.For<IAvaloniaInspector>();
    var expectedNode = new VisualTreeNode { ... };
    inspector.GetVisualTreeAsync(15, null, Arg.Any<CancellationToken>())
        .Returns(expectedNode);

    var result = await InspectionTools.GetVisualTree(inspector);
    var deserialized = JsonSerializer.Deserialize<VisualTreeNode>(result, JsonOptions.Default);

    deserialized.Should().BeEquivalentTo(expectedNode);
}
```

### JSON Assertion Pattern

Per project memory: `System.Text.Json` escapes single quotes as `\u0027`. Always use roundtrip deserialization instead of exact string matching:

```csharp
// CORRECT: Roundtrip deserialization
var deserialized = JsonSerializer.Deserialize<T>(jsonString, JsonOptions.Default);
deserialized.Should().BeEquivalentTo(expected);

// WRONG: Exact string matching (breaks on escaped chars)
// jsonString.Should().Be("{\"name\":\"it's\"}");  // \u0027 mismatch
```

## Demo App Integration

The demo app (`apps/Six.Avalonia.Mcp`) should have a richer UI to demonstrate inspection and interaction capabilities. The main window should include:

- Named controls (using `x:Name`) for easy discovery
- Various control types: Button, TextBox, CheckBox, Slider, ComboBox, ListBox
- A status bar showing last interaction received
- Layout panels (StackPanel, Grid, DockPanel)

This creates a testbed where an AI agent can:
1. Discover the visual tree
2. Find specific controls by name/type
3. Click buttons and observe state changes
4. Type in text boxes
5. Toggle checkboxes
6. Take screenshots to verify visual state
