# simulate_click

Simulate a mouse click (left, right, or middle) on a named element. Optionally specify x/y offset within the element, modifier keys (ctrl, shift, alt), and click count.

## Parameters

| Parameter | Type | Default | Required | Description |
|-----------|------|---------|----------|-------------|
| elementName | string | -- | Yes | The `x:Name` of the target element. |
| button | string | "left" | No | Mouse button to simulate: `"left"`, `"right"`, or `"middle"`. |
| x | double? | null | No | X offset within the element. Defaults to the horizontal center. |
| y | double? | null | No | Y offset within the element. Defaults to the vertical center. |
| modifiers | string? | null | No | Modifier keys to hold during the click. See Modifier Syntax below. |
| clickCount | int | 1 | No | Number of clicks (e.g., `2` for double-click). |

### Modifier Syntax

The `modifiers` parameter accepts a `"+"` delimited, case-insensitive string of modifier key names:

| Modifier | Accepted Values |
|----------|-----------------|
| Control | `ctrl`, `control` |
| Shift | `shift` |
| Alt | `alt` |
| Meta | `meta` |

Example: `"ctrl+shift"`, `"Alt+Ctrl"`, `"shift"`

## Return Type

Returns an `InteractionResult` object reflecting the state of the element after the interaction.

```json
{
  "success": true,
  "elementType": "string|null",
  "elementName": "string|null",
  "isFocused": true,
  "isEnabled": true,
  "isVisible": true,
  "isPointerOver": false
}
```

## Example

### Request -- Simple left click

```json
{
  "elementName": "SubmitButton"
}
```

### Response

```json
{
  "success": true,
  "elementType": "Button",
  "elementName": "SubmitButton",
  "isFocused": true,
  "isEnabled": true,
  "isVisible": true,
  "isPointerOver": false
}
```

### Request -- Right-click with Ctrl held at specific coordinates

```json
{
  "elementName": "Canvas",
  "button": "right",
  "x": 150.0,
  "y": 200.0,
  "modifiers": "ctrl"
}
```

### Request -- Double-click

```json
{
  "elementName": "ListItem",
  "clickCount": 2
}
```

## Error Handling

All errors return: `{ "error": "message" }`

| Scenario | Error Message |
|----------|---------------|
| No active main window | `"Main window is not available."` |
| Named element not found | `"Element 'xxx' not found."` |
| Element is not interactive | `"Element 'xxx' is not an Interactive element..."` |

## Notes

- The tool raises `PointerEntered` and `PointerMoved` events first (pre-click hover), followed by `PointerPressed` and `PointerReleased` events on the target element.
- For `Button` controls, a `Button.ClickEvent` is raised after the pointer events as a fallback. This is necessary because Avalonia's `Button.OnPointerReleased` checks `IsPointerOver` before firing Click, but `IsPointerOver` is managed by Avalonia's internal input system and is not updated by synthetic `RaiseEvent()` calls. The fallback ensures `Button.Click` handlers and `Command` bindings are triggered correctly.
- If `x` and `y` are not specified, the click targets the center of the element's bounds.
- The `modifiers` string is case-insensitive. `"Ctrl+Shift"` and `"ctrl+shift"` are equivalent.
- For double-click behavior, set `clickCount` to `2`. The framework handles the click-count semantics.
- The element must derive from `Interactive` (which includes `InputElement`, `Control`, and all standard Avalonia controls) for the click to be dispatched.
- Related tools: [simulate_type](simulate-type.md) for text input, [simulate_hover](simulate-hover.md) for hover effects, [simulate_drag](simulate-drag.md) for drag operations.
