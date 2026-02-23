# get_ancestor_chain

Get the ancestor chain from a named element up to the root window. Each ancestor includes type, name, bounds, and state -- no children are populated.

## Parameters

| Parameter | Type | Default | Required | Description |
|-----------|------|---------|----------|-------------|
| elementName | string | -- | Yes | The `x:Name` of the element to start from. |

## Return Type

Returns an array of `VisualTreeNode` objects representing the path from the target element up to the root window. The `children` array is always empty for each node (depth=0 traversal).

```json
[
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
]
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
[
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
  },
  {
    "type": "StackPanel",
    "bounds": { "x": 0, "y": 0, "width": 1280, "height": 50 },
    "isVisible": true,
    "isEnabled": true,
    "opacity": 1.0,
    "isFocused": false,
    "isPointerOver": false,
    "classes": [],
    "children": []
  },
  {
    "type": "Grid",
    "bounds": { "x": 0, "y": 0, "width": 1280, "height": 720 },
    "isVisible": true,
    "isEnabled": true,
    "opacity": 1.0,
    "isFocused": false,
    "isPointerOver": false,
    "classes": [],
    "children": []
  },
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
    "children": []
  }
]
```

## Error Handling

All errors return: `{ "error": "message" }`

| Scenario | Error Message |
|----------|---------------|
| No active main window | `"Main window is not available."` |
| Named element not found | `"Element 'xxx' not found."` |

## Notes

- The first element in the returned array is the target element itself. The last element is the root window.
- The `children` array is always empty for every node in the chain. This tool only reports the linear path from element to root, not the full subtree at each level.
- This tool is useful for understanding where an element sits in the layout hierarchy, which panels or containers wrap it, and what naming or styling is applied at each level.
- Related tools: [get_visual_tree](get-visual-tree.md) for full subtree traversal, [get_element_properties](get-element-properties.md) for detailed properties of a single element.
