# get_visual_tree

Get a recursive snapshot of the visual tree starting from the main window or a named element. Returns type, name, bounds, visibility, classes, and children for each node.

## Parameters

| Parameter | Type | Default | Required | Description |
|-----------|------|---------|----------|-------------|
| maxDepth | int | 15 | No | Maximum depth of tree traversal. Clamped to the range 1--50. |
| elementName | string? | null | No | Start traversal from a named element instead of the main window. |

## Return Type

Returns a recursive `VisualTreeNode` object. Each node contains its children, forming a tree structure.

```json
{
  "type": "string",
  "name": "string|null",
  "bounds": { "x": 0.0, "y": 0.0, "width": 0.0, "height": 0.0 },
  "isVisible": true,
  "isEnabled": true,
  "opacity": 1.0,
  "isFocused": false,
  "isPointerOver": false,
  "classes": [],
  "children": []
}
```

## Example

### Request

```json
{
  "maxDepth": 2
}
```

### Response

```json
{
  "type": "Window",
  "name": "MainWindow",
  "bounds": { "x": 0, "y": 0, "width": 1280, "height": 720 },
  "isVisible": true,
  "isEnabled": true,
  "opacity": 1.0,
  "isFocused": false,
  "isPointerOver": false,
  "classes": ["window"],
  "children": [
    {
      "type": "Grid",
      "bounds": { "x": 0, "y": 0, "width": 1280, "height": 720 },
      "isVisible": true,
      "isEnabled": true,
      "opacity": 1.0,
      "isFocused": false,
      "isPointerOver": false,
      "classes": [],
      "children": [
        {
          "type": "TextBox",
          "name": "SearchBox",
          "bounds": { "x": 10, "y": 10, "width": 300, "height": 32 },
          "isVisible": true,
          "isEnabled": true,
          "opacity": 1.0,
          "isFocused": true,
          "isPointerOver": false,
          "classes": ["textBox"],
          "children": []
        },
        {
          "type": "Button",
          "name": "SubmitButton",
          "bounds": { "x": 320, "y": 10, "width": 100, "height": 32 },
          "isVisible": true,
          "isEnabled": true,
          "opacity": 1.0,
          "isFocused": false,
          "isPointerOver": false,
          "classes": ["button"],
          "children": []
        }
      ]
    }
  ]
}
```

### Request with elementName

```json
{
  "elementName": "SearchBox",
  "maxDepth": 3
}
```

## Error Handling

All errors return: `{ "error": "message" }`

| Scenario | Error Message |
|----------|---------------|
| No active main window | `"Main window is not available."` |
| Named element not found | `"Element 'xxx' not found."` |

## Notes

- Large visual trees can generate very large JSON responses. Use `maxDepth` to limit the depth of traversal when you only need the top-level structure.
- Use `elementName` to focus on a specific subtree rather than dumping the entire window tree.
- The `children` array is always present but may be empty when `maxDepth` is reached or the node is a leaf.
- Null-valued properties (such as `name` when unnamed) are omitted from the JSON output.
- The `classes` array contains Avalonia CSS classes applied to the control (e.g., `["button", "accent"]`).
- Related tools: [find_element](find-element.md) for searching by criteria, [get_element_properties](get-element-properties.md) for detailed properties of a single element.
