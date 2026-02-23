# get_element_properties

Get detailed runtime properties of a specific element resolved by name, type, or index path. Returns identity, geometry, state, appearance, layout, data context, and style information.

## Parameters

| Parameter | Type | Default | Required | Description |
|-----------|------|---------|----------|-------------|
| elementName | string? | null | No | Resolve the element by its `x:Name`. |
| typeName | string? | null | No | Resolve the element by its control type name (e.g., `"TextBox"`). Returns the first match. |
| path | string? | null | No | Resolve the element by its index path (e.g., `"0/2/1"`). Obtained from [find_element](find-element.md). |

At least one of `elementName`, `typeName`, or `path` must be provided.

**Resolution order:** `elementName` is tried first, then `typeName`, then `path`. The first successful resolution is used.

## Return Type

Returns an `ElementProperties` object with comprehensive runtime information.

```json
{
  "type": "string",
  "name": "string|null",
  "elementId": "path:0/1/2",
  "bounds": { "x": 0.0, "y": 0.0, "width": 0.0, "height": 0.0 },
  "width": 0.0,
  "height": 0.0,
  "margin": "string|null",
  "padding": "string|null",
  "isVisible": true,
  "isEnabled": true,
  "isFocused": false,
  "isHitTestVisible": true,
  "clipToBounds": false,
  "opacity": 1.0,
  "background": "string|null",
  "foreground": "string|null",
  "borderBrush": "string|null",
  "borderThickness": "string|null",
  "cornerRadius": "string|null",
  "horizontalAlignment": "string|null",
  "verticalAlignment": "string|null",
  "flowDirection": "string|null",
  "dataContextType": "string|null",
  "classes": [],
  "pseudoClasses": []
}
```

## Example

### Request

```json
{
  "elementName": "SubmitButton"
}
```

### Response

```json
{
  "type": "Button",
  "name": "SubmitButton",
  "elementId": "path:0/1",
  "bounds": { "x": 320, "y": 10, "width": 100, "height": 32 },
  "width": 100.0,
  "height": 32.0,
  "margin": "4,4,4,4",
  "padding": "8,2,8,2",
  "isVisible": true,
  "isEnabled": true,
  "isFocused": false,
  "isHitTestVisible": true,
  "clipToBounds": false,
  "opacity": 1.0,
  "background": "#FF1E90FF",
  "foreground": "#FFFFFFFF",
  "borderBrush": "#FF1565C0",
  "borderThickness": "1,1,1,1",
  "cornerRadius": "4,4,4,4",
  "horizontalAlignment": "Left",
  "verticalAlignment": "Center",
  "flowDirection": "LeftToRight",
  "dataContextType": "MyApp.ViewModels.MainViewModel",
  "classes": ["button", "accent"],
  "pseudoClasses": [":pointerover"]
}
```

### Request -- Resolve by path

```json
{
  "path": "0/1"
}
```

## Error Handling

All errors return: `{ "error": "message" }`

| Scenario | Error Message |
|----------|---------------|
| No active main window | `"Main window is not available."` |
| No resolution parameter provided | `"Element not found. Provide elementName, typeName, or path."` |
| Element not found by any resolver | `"Element not found. Provide elementName, typeName, or path."` |

## Notes

- The `elementId` in the response (e.g., `"path:0/1"`) matches the format returned by [find_element](find-element.md) and can be used to re-resolve the same element later.
- Properties like `background`, `foreground`, `borderBrush` are returned as string representations of the brush (e.g., hex color `"#FF1E90FF"`). They are null if not set on the control.
- The `pseudoClasses` array includes Avalonia pseudo-classes such as `:pointerover`, `:pressed`, `:focus`, `:disabled`.
- The `dataContextType` reports the fully qualified type name of the element's `DataContext`, which is useful for understanding MVVM bindings.
- Null-valued properties are omitted from the JSON output.
- Related tools: [find_element](find-element.md) to locate elements, [get_visual_tree](get-visual-tree.md) to browse the tree.
