# find_element

Find elements in the visual tree matching criteria: name, type name, CSS class, or a simple selector (e.g., `Button#myBtn`, `.error`, `#myId`). Returns matching elements with their paths and IDs.

## Parameters

| Parameter | Type | Default | Required | Description |
|-----------|------|---------|----------|-------------|
| name | string? | null | No | Match elements by `x:Name`. |
| typeName | string? | null | No | Match elements by control type name (e.g., `"TextBox"`, `"Button"`). |
| className | string? | null | No | Match elements by Avalonia CSS class (e.g., `"accent"`, `"error"`). |
| selector | string? | null | No | Simple CSS-like selector. See Selector Syntax below. |
| maxResults | int | 10 | No | Maximum number of results to return. |

## Selector Syntax

The `selector` parameter supports a simple CSS-like syntax:

| Pattern | Meaning | Example |
|---------|---------|---------|
| `Type#Name` | Match by type and name | `Button#SubmitBtn` |
| `Type.Class` | Match by type and CSS class | `TextBox.error` |
| `#Name` | Match by name only | `#SearchBox` |
| `.Class` | Match by CSS class only | `.accent` |
| `Type` | Match by type only | `Button` |

Selector values are merged with explicit `name`, `typeName`, and `className` parameters. Explicit parameter values take precedence over selector-parsed values.

## Return Type

Returns an array of `FindElementResult` objects.

```json
[
  {
    "type": "string",
    "name": "string|null",
    "bounds": { "x": 0.0, "y": 0.0, "width": 0.0, "height": 0.0 },
    "path": [0, 1, 2],
    "elementId": "path:0/1/2"
  }
]
```

- `path` is an array of child indices from the root to the matched element.
- `elementId` is a string representation of the path in the format `"path:0/1/2"`, usable with other tools that accept a `path` parameter.

## Example

### Request -- Find by name

```json
{
  "name": "SubmitButton"
}
```

### Response

```json
[
  {
    "type": "Button",
    "name": "SubmitButton",
    "bounds": { "x": 320, "y": 10, "width": 100, "height": 32 },
    "path": [0, 1],
    "elementId": "path:0/1"
  }
]
```

### Request -- Find by selector

```json
{
  "selector": "TextBox.error",
  "maxResults": 5
}
```

### Response

```json
[
  {
    "type": "TextBox",
    "name": "EmailInput",
    "bounds": { "x": 10, "y": 50, "width": 300, "height": 32 },
    "path": [0, 0, 2],
    "elementId": "path:0/0/2"
  }
]
```

## Error Handling

All errors return: `{ "error": "message" }`

| Scenario | Error Message |
|----------|---------------|
| No active main window | `"Main window is not available."` |
| No matching elements | Returns an empty array `[]` (not an error). |

## Notes

- All non-null criteria are combined with AND logic. An element must match every specified criterion to be included in the results.
- When both a `selector` and explicit parameters are provided, the explicit parameters take precedence. For example, `selector: "Button#Foo"` with `name: "Bar"` will search for a `Button` named `"Bar"`.
- The `elementId` value (e.g., `"path:0/1/2"`) can be used with [get_element_properties](get-element-properties.md) by passing the numeric portion as the `path` parameter (e.g., `"0/1/2"`).
- The `maxResults` parameter defaults to 10. Increase it if you expect many matches.
- Related tools: [get_visual_tree](get-visual-tree.md) for browsing the full tree, [get_element_properties](get-element-properties.md) for detailed properties of a found element.
