# Models

All model DTOs used by `IAvaloniaInspector` as method inputs and outputs, plus the `ConnectedAppInfo` registration DTO used by the SignalR hub.

- **Namespace**: `Six.Avalonia.Mcp.Contracts.Models`
- **Serialization**: `System.Text.Json` with camelCase property naming; null values are omitted from output.
- **Type**: All models are `sealed record` types using `required` init-only properties.

---

## AppMetadata

Application process and version metadata.

**Source**: `libs/Six.Avalonia.Mcp.Contracts/src/Models/AppMetadata.cs`

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `ProcessName` | `string` | Yes | Name of the running process. |
| `ProcessId` | `int` | Yes | OS process identifier. |
| `MainWindowTitle` | `string?` | No | Title of the main window, if set. |
| `AvaloniaVersion` | `string` | Yes | Avalonia framework version string. |
| `DotNetVersion` | `string` | Yes | .NET runtime version string. |
| `WindowCount` | `int` | Yes | Number of open windows (currently always 1). |
| `WindowWidth` | `double` | Yes | Main window width in device-independent pixels (DIPs). |
| `WindowHeight` | `double` | Yes | Main window height in device-independent pixels (DIPs). |

**JSON example**:

```json
{
  "processName": "McpDemo",
  "processId": 12480,
  "mainWindowTitle": "Six Avalonia MCP Demo",
  "avaloniaVersion": "11.3.11",
  "dotNetVersion": "10.0.0",
  "windowCount": 1,
  "windowWidth": 800.0,
  "windowHeight": 450.0
}
```

---

## BoundsInfo

Bounding rectangle of a visual element in device-independent pixels.

**Source**: `libs/Six.Avalonia.Mcp.Contracts/src/Models/BoundsInfo.cs`

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `X` | `double` | Yes | Left edge position relative to parent. |
| `Y` | `double` | Yes | Top edge position relative to parent. |
| `Width` | `double` | Yes | Width in DIPs. |
| `Height` | `double` | Yes | Height in DIPs. |

**JSON example**:

```json
{
  "x": 16.0,
  "y": 32.0,
  "width": 200.0,
  "height": 40.0
}
```

---

## VisualTreeNode

Recursive representation of a node in the Avalonia visual tree. Returned by `GetVisualTreeAsync` and `GetAncestorChainAsync`.

**Source**: `libs/Six.Avalonia.Mcp.Contracts/src/Models/VisualTreeNode.cs`

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `Type` | `string` | Yes | Control type name (e.g., `"TextBox"`, `"Button"`, `"StackPanel"`). |
| `Name` | `string?` | No | The element's `x:Name`, if set. |
| `Bounds` | `BoundsInfo` | Yes | Position and size of the element. |
| `IsVisible` | `bool` | Yes | Whether the control is visible. |
| `IsEnabled` | `bool` | Yes | Whether the control is effectively enabled (accounts for parent state). |
| `Opacity` | `double` | Yes | Opacity value in the range 0.0--1.0. |
| `IsFocused` | `bool` | Yes | Whether the element currently has keyboard focus. |
| `IsPointerOver` | `bool` | Yes | Whether the pointer is currently over this element. |
| `Classes` | `IReadOnlyList<string>` | Yes | CSS style classes applied to the element. |
| `Children` | `IReadOnlyList<VisualTreeNode>` | Yes | Child nodes in the visual tree. Empty when max depth is reached. |

**JSON example**:

```json
{
  "type": "StackPanel",
  "name": "mainPanel",
  "bounds": { "x": 0.0, "y": 0.0, "width": 800.0, "height": 450.0 },
  "isVisible": true,
  "isEnabled": true,
  "opacity": 1.0,
  "isFocused": false,
  "isPointerOver": false,
  "classes": [],
  "children": [
    {
      "type": "TextBlock",
      "name": "titleText",
      "bounds": { "x": 16.0, "y": 8.0, "width": 200.0, "height": 24.0 },
      "isVisible": true,
      "isEnabled": true,
      "opacity": 1.0,
      "isFocused": false,
      "isPointerOver": false,
      "classes": ["h1"],
      "children": []
    }
  ]
}
```

---

## FindElementResult

Result of a find query, identifying a matched element by its path in the visual tree.

**Source**: `libs/Six.Avalonia.Mcp.Contracts/src/Models/FindElementResult.cs`

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `Type` | `string` | Yes | Control type name. |
| `Name` | `string?` | No | The element's `x:Name`, if set. |
| `Bounds` | `BoundsInfo` | Yes | Position and size of the element. |
| `Path` | `IReadOnlyList<int>` | Yes | Child index path from the root (e.g., `[0, 2, 1]` means root's 1st child, then its 3rd child, then its 2nd child). |
| `ElementId` | `string` | Yes | Stable identifier in the format `"path:0/2/1"`. Can be used as the `path` parameter in `GetElementPropertiesAsync`. |

**JSON example**:

```json
{
  "type": "Button",
  "name": "submitBtn",
  "bounds": { "x": 300.0, "y": 200.0, "width": 120.0, "height": 36.0 },
  "path": [0, 2, 1],
  "elementId": "path:0/2/1"
}
```

---

## ElementProperties

Detailed runtime properties of a visual element. Returned by `GetElementPropertiesAsync`.

**Source**: `libs/Six.Avalonia.Mcp.Contracts/src/Models/ElementProperties.cs`

Properties are organized into categories:

### Identity

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `Type` | `string` | Yes | Control type name. |
| `Name` | `string?` | No | The element's `x:Name`, if set. |
| `ElementId` | `string` | Yes | Stable path-based identifier (e.g., `"path:0/2/1"`). |

### Geometry

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `Bounds` | `BoundsInfo` | Yes | Bounding rectangle. |
| `Width` | `double` | Yes | Actual rendered width. |
| `Height` | `double` | Yes | Actual rendered height. |
| `Margin` | `string?` | No | Margin as string (e.g., `"8,4,8,4"`). |
| `Padding` | `string?` | No | Padding as string (e.g., `"12,6"`). |

### State

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `IsVisible` | `bool` | Yes | Whether the control is visible. |
| `IsEnabled` | `bool` | Yes | Whether the control is enabled. |
| `IsFocused` | `bool` | Yes | Whether the element has keyboard focus. |
| `IsHitTestVisible` | `bool` | Yes | Whether the element participates in hit testing. |
| `ClipToBounds` | `bool` | Yes | Whether content is clipped to the element's bounds. |
| `Opacity` | `double` | Yes | Opacity value (0.0--1.0). |

### Appearance

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `Background` | `string?` | No | Background brush as string (e.g., `"#FF0000"`, `"SolidColorBrush"`). |
| `Foreground` | `string?` | No | Foreground brush as string. |
| `BorderBrush` | `string?` | No | Border brush as string. |
| `BorderThickness` | `string?` | No | Border thickness as string (e.g., `"1,1,1,1"`). |
| `CornerRadius` | `string?` | No | Corner radius as string (e.g., `"4"`). |

### Layout

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `HorizontalAlignment` | `string?` | No | Horizontal alignment (e.g., `"Center"`, `"Stretch"`). |
| `VerticalAlignment` | `string?` | No | Vertical alignment (e.g., `"Top"`, `"Center"`). |
| `FlowDirection` | `string?` | No | Flow direction (`"LeftToRight"` or `"RightToLeft"`). |

### Data

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `DataContextType` | `string?` | No | Full type name of the element's `DataContext`, if bound. |

### Styles

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `Classes` | `IReadOnlyList<string>` | Yes | CSS style classes applied to the element. |
| `PseudoClasses` | `IReadOnlyList<string>` | Yes | Active pseudo-classes (e.g., `":pointerover"`, `":focus"`, `":pressed"`). |

**JSON example**:

```json
{
  "type": "TextBox",
  "name": "emailInput",
  "elementId": "path:0/1/3",
  "bounds": { "x": 100.0, "y": 120.0, "width": 250.0, "height": 32.0 },
  "width": 250.0,
  "height": 32.0,
  "margin": "8,4,8,4",
  "padding": "4,2",
  "isVisible": true,
  "isEnabled": true,
  "isFocused": true,
  "isHitTestVisible": true,
  "clipToBounds": false,
  "opacity": 1.0,
  "background": "#FFFFFFFF",
  "foreground": "#FF000000",
  "borderBrush": "#FF666666",
  "borderThickness": "1,1,1,1",
  "cornerRadius": "4",
  "horizontalAlignment": "Stretch",
  "verticalAlignment": "Center",
  "flowDirection": "LeftToRight",
  "dataContextType": "Six.Avalonia.Mcp.App.ViewModels.MainWindowViewModel",
  "classes": ["textBox"],
  "pseudoClasses": [":focus", ":pointerover"]
}
```

---

## InteractionResult

Result of an interaction operation (click, type, hover, drag, keyboard shortcut, or state change). Includes optional post-interaction state of the target element.

**Source**: `libs/Six.Avalonia.Mcp.Contracts/src/Models/InteractionResult.cs`

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `Success` | `bool` | Yes | Whether the interaction completed successfully. |
| `ErrorMessage` | `string?` | No | Error description if the interaction failed. |
| `ElementType` | `string?` | No | Type name of the interacted element. |
| `ElementName` | `string?` | No | `x:Name` of the interacted element. |
| `IsFocused` | `bool?` | No | Focus state after the interaction. |
| `IsEnabled` | `bool?` | No | Enabled state after the interaction. |
| `IsVisible` | `bool?` | No | Visibility state after the interaction. |
| `IsPointerOver` | `bool?` | No | Pointer-over state after the interaction. |

**JSON example (success)**:

```json
{
  "success": true,
  "elementType": "Button",
  "elementName": "submitBtn",
  "isFocused": true,
  "isEnabled": true,
  "isVisible": true,
  "isPointerOver": false
}
```

**JSON example (failure)**:

```json
{
  "success": false,
  "errorMessage": "Element 'nonExistent' not found in the visual tree."
}
```

---

## ConnectedAppInfo

Registration DTO sent by an Avalonia app when it connects to the MCP server hub via SignalR.

**Source**: `libs/Six.Avalonia.Mcp.Contracts/src/Models/ConnectedAppInfo.cs`

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `AppId` | `string` | Yes | Unique identifier for this app connection (typically a GUID). |
| `AppName` | `string` | Yes | Human-readable application name (configured via `McpConnectorOptions.AppName`). |
| `ProcessId` | `int` | Yes | OS process identifier. |
| `ProcessName` | `string` | Yes | Name of the running process. |
| `ConnectedAt` | `DateTimeOffset` | Yes | Timestamp when the app connected to the server. |

**JSON example**:

```json
{
  "appId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "appName": "My Avalonia App",
  "processId": 12480,
  "processName": "McpDemo",
  "connectedAt": "2026-01-15T10:30:00+00:00"
}
```
