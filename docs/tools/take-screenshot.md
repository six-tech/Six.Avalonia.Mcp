# take_screenshot

Capture a screenshot of the main window or a specific named element as a base64-encoded PNG. Optionally specify a scale factor (0.5--3.0, default 1.0).

## Parameters

| Parameter | Type | Default | Required | Description |
|-----------|------|---------|----------|-------------|
| elementName | string? | null | No | Capture a specific named element instead of the entire main window. |
| scaleFactor | double | 1.0 | No | Scale factor for the rendered image. Clamped to the range 0.5--3.0. |

## Return Type

Returns an object containing the base64-encoded PNG image data and its dimensions.

```json
{
  "base64Png": "string",
  "width": 0,
  "height": 0
}
```

## Example

### Request -- Full window capture

```json
{}
```

### Response

```json
{
  "base64Png": "iVBORw0KGgoAAAANSUhEUgAABQAAAALQCAYAAAD...",
  "width": 1280,
  "height": 720
}
```

### Request -- Element capture at 2x scale

```json
{
  "elementName": "SubmitButton",
  "scaleFactor": 2.0
}
```

### Response

```json
{
  "base64Png": "iVBORw0KGgoAAAANSUhEUgAAAMgAAABAAAAA...",
  "width": 200,
  "height": 64
}
```

## Error Handling

All errors return: `{ "error": "message" }`

| Scenario | Error Message |
|----------|---------------|
| No active main window | `"Main window is not available."` |
| Named element not found | `"Element 'xxx' not found."` |
| Element has zero or negative dimensions | `"Visual has zero or negative size..."` |

## Notes

- The `base64Png` string can be very large for full-window captures, especially at higher scale factors. Use `elementName` to capture only the relevant portion of the UI when possible.
- The `scaleFactor` parameter controls the rendering resolution. A value of `2.0` produces a 2x resolution image (useful for HiDPI inspection). Values outside the 0.5--3.0 range are clamped.
- Headless test platforms (e.g., `Avalonia.Headless.XUnit`) may produce empty or blank output because `RenderTargetBitmap` requires a real rendering backend. This is a known limitation of headless environments.
- The `width` and `height` in the response reflect the pixel dimensions of the rendered image after applying the scale factor.
- Related tools: [get_visual_tree](get-visual-tree.md) for structural inspection, [get_element_properties](get-element-properties.md) for property-level details.
