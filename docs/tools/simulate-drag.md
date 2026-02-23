# simulate_drag

Simulate a drag operation on a named element from (startX, startY) to (endX, endY). Dispatches PointerPressed, intermediate PointerMoved events (controlled by steps), and PointerReleased.

## Parameters

| Parameter | Type | Default | Required | Description |
|-----------|------|---------|----------|-------------|
| elementName | string | -- | Yes | The `x:Name` of the target element. |
| startX | double | -- | Yes | Start X coordinate within the element. |
| startY | double | -- | Yes | Start Y coordinate within the element. |
| endX | double | -- | Yes | End X coordinate within the element. |
| endY | double | -- | Yes | End Y coordinate within the element. |
| button | string | "left" | No | Mouse button to use: `"left"`, `"right"`, or `"middle"`. |
| steps | int | 10 | No | Number of intermediate `PointerMoved` events between start and end. Minimum value is 1. |

## Return Type

Returns an `InteractionResult` object reflecting the state of the element after the drag operation completes.

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

### Request -- Drag a slider handle

```json
{
  "elementName": "VolumeSlider",
  "startX": 50.0,
  "startY": 10.0,
  "endX": 150.0,
  "endY": 10.0
}
```

### Response

```json
{
  "success": true,
  "elementType": "Slider",
  "elementName": "VolumeSlider",
  "isFocused": true,
  "isEnabled": true,
  "isVisible": true,
  "isPointerOver": false
}
```

### Request -- Drag with fewer steps and right button

```json
{
  "elementName": "Canvas",
  "startX": 10.0,
  "startY": 10.0,
  "endX": 200.0,
  "endY": 150.0,
  "button": "right",
  "steps": 5
}
```

## Error Handling

All errors return: `{ "error": "message" }`

| Scenario | Error Message |
|----------|---------------|
| No active main window | `"Main window is not available."` |
| Named element not found | `"Element 'xxx' not found."` |
| Element is not interactive | `"Element 'xxx' is not an Interactive element..."` |
| Invalid mouse button | `"Unknown mouse button: 'xxx'."` |

## Notes

- The drag operation is decomposed into discrete events: one `PointerPressed` at (startX, startY), then `steps` number of `PointerMoved` events linearly interpolated between start and end, and finally one `PointerReleased` at (endX, endY).
- Coordinates are relative to the element's bounds, not the window.
- The `steps` parameter controls the granularity of the drag. More steps produce smoother drag paths. The minimum value is 1 (which produces a single move from start to end).
- For horizontal-only drags (e.g., sliders), keep startY and endY the same.
- The element must derive from `Interactive` for the pointer events to be dispatched.
- Related tools: [simulate_click](simulate-click.md) for simple clicks, [simulate_hover](simulate-hover.md) for hover without press.
