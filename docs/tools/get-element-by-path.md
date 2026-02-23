# get_element_by_path

Get detailed runtime properties of an element by its index-based visual tree path (e.g., `0/3/1`). Each segment is a zero-based child index. Use `get_visual_tree` or `find_element` to discover paths.

This is a convenience wrapper around `get_element_properties` that accepts only a `path` parameter, making it easier for AI agents to look up elements by their positional address in the visual tree.

## Parameters

| Parameter | Type | Default | Required | Description |
|-----------|------|---------|----------|-------------|
| path | string | -- | Yes | Index-based visual tree path (e.g., `"0/3/1"`). Each segment is a zero-based child index. |
| appId | string? | null | No | Target a specific connected app. Auto-targets when only one app is connected. |

## Return Type

Returns an `ElementProperties` object with comprehensive runtime information (same as [get_element_properties](get-element-properties.md)).

```json
{
  "type": "string",
  "name": "string|null",
  "elementId": "path:0/3/1",
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
  "path": "0/3/1"
}
```

### Response

```json
{
  "type": "Button",
  "name": "SubmitButton",
  "elementId": "path:0/3/1",
  "bounds": { "x": 320, "y": 10, "width": 100, "height": 32 },
  "width": 100.0,
  "height": 32.0,
  "isVisible": true,
  "isEnabled": true,
  "isFocused": false,
  "isHitTestVisible": true,
  "clipToBounds": false,
  "opacity": 1.0,
  "horizontalAlignment": "Left",
  "verticalAlignment": "Center",
  "flowDirection": "LeftToRight",
  "classes": [],
  "pseudoClasses": []
}
```

## Error Handling

All errors return: `{ "error": "message" }`

| Scenario | Error Message |
|----------|---------------|
| No active main window | `"Main window is not available."` |
| Invalid path (non-numeric segment) | `"Element not found. Provide elementName, typeName, or path."` |
| Path out of bounds | `"Element not found. Provide elementName, typeName, or path."` |
| No app connected | `"No Avalonia app is connected. Start an app with the MCP connector enabled."` |

## Notes

- Paths are obtained from `find_element` (the `path` array) or `get_visual_tree` (by counting child positions).
- A path like `"0/3/1"` means: starting from the root, take child at index 0, then child at index 3, then child at index 1.
- Paths are positional and may change if the visual tree structure changes (e.g., adding/removing controls).
- This tool delegates to the same infrastructure as `get_element_properties` with a `path` parameter.
- Related tools: [find_element](find-element.md) to discover paths, [get_visual_tree](get-visual-tree.md) to browse the tree, [get_element_properties](get-element-properties.md) for resolution by name or type.
