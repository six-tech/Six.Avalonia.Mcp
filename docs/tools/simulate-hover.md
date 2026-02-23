# simulate_hover

Simulate a pointer hover (PointerEntered + PointerMoved) on a named element. Set `exitAfter` to true to also dispatch PointerExited.

## Parameters

| Parameter | Type | Default | Required | Description |
|-----------|------|---------|----------|-------------|
| elementName | string | -- | Yes | The `x:Name` of the target element. |
| exitAfter | bool | false | No | If `true`, also dispatch a `PointerExited` event after the hover. |

## Return Type

Returns an `InteractionResult` object reflecting the state of the element after the hover.

```json
{
  "success": true,
  "elementType": "string|null",
  "elementName": "string|null",
  "isFocused": false,
  "isEnabled": true,
  "isVisible": true,
  "isPointerOver": true
}
```

## Example

### Request -- Hover to trigger tooltip

```json
{
  "elementName": "HelpIcon"
}
```

### Response

```json
{
  "success": true,
  "elementType": "Image",
  "elementName": "HelpIcon",
  "isFocused": false,
  "isEnabled": true,
  "isVisible": true,
  "isPointerOver": true
}
```

### Request -- Hover and exit

```json
{
  "elementName": "MenuItem",
  "exitAfter": true
}
```

### Response

```json
{
  "success": true,
  "elementType": "MenuItem",
  "elementName": "MenuItem",
  "isFocused": false,
  "isEnabled": true,
  "isVisible": true,
  "isPointerOver": false
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

- The hover targets the center of the element's bounds.
- The tool dispatches `PointerEntered` followed by `PointerMoved`. If `exitAfter` is `true`, a `PointerExited` event is also dispatched.
- When `exitAfter` is `false` (default), the `isPointerOver` property in the result will typically be `true`. When `exitAfter` is `true`, it will be `false` because the pointer has already exited.
- This tool is useful for triggering hover-dependent UI behavior such as tooltips, hover styles (`:pointerover` pseudo-class), and dropdown menus.
- The element must derive from `Interactive` for the pointer events to be dispatched.
- Related tools: [simulate_click](simulate-click.md) for clicking after hover, [take_screenshot](take-screenshot.md) to capture hover state visuals.
