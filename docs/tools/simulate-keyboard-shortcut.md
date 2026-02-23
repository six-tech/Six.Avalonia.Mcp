# simulate_keyboard_shortcut

Simulate a keyboard shortcut (e.g. `Ctrl+S`, `Alt+F4`, `F5`) optionally targeted at a named element. If no element is specified, uses the currently focused element or the main window.

## Parameters

| Parameter | Type | Default | Required | Description |
|-----------|------|---------|----------|-------------|
| keys | string | -- | Yes | Key combination to simulate (e.g., `"Ctrl+S"`, `"Ctrl+Shift+Z"`, `"Alt+F4"`, `"F5"`). |
| elementName | string? | null | No | Optional target element by `x:Name`. If not specified, the shortcut is dispatched to the currently focused element or the main window. |

### Key Combination Syntax

The `keys` parameter is a `"+"` delimited string of modifier keys and a primary key.

**Recognized modifiers** (case-insensitive):

| Modifier | Accepted Values |
|----------|-----------------|
| Control | `ctrl`, `control` |
| Shift | `shift` |
| Alt | `alt` |
| Meta / Windows / Command | `meta`, `win`, `cmd` |

**Primary key:** Any valid value from the `Avalonia.Input.Key` enumeration. Common examples:

| Category | Keys |
|----------|------|
| Letters | `A`--`Z` |
| Numbers | `D0`--`D9` (main keyboard), `NumPad0`--`NumPad9` |
| Function | `F1`--`F24` |
| Navigation | `Up`, `Down`, `Left`, `Right`, `Home`, `End`, `PageUp`, `PageDown` |
| Editing | `Back`, `Delete`, `Insert`, `Enter`, `Tab`, `Space`, `Escape` |
| Symbols | `OemPlus`, `OemMinus`, `OemPeriod`, `OemComma` |

## Return Type

Returns an `InteractionResult` object reflecting the state of the target element after the shortcut is dispatched.

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

### Request -- Save shortcut

```json
{
  "keys": "Ctrl+S"
}
```

### Response

```json
{
  "success": true,
  "elementType": "Window",
  "elementName": "MainWindow",
  "isFocused": true,
  "isEnabled": true,
  "isVisible": true,
  "isPointerOver": false
}
```

### Request -- Undo in a specific text box

```json
{
  "keys": "Ctrl+Z",
  "elementName": "CodeEditor"
}
```

### Response

```json
{
  "success": true,
  "elementType": "TextBox",
  "elementName": "CodeEditor",
  "isFocused": true,
  "isEnabled": true,
  "isVisible": true,
  "isPointerOver": false
}
```

### Request -- Select All

```json
{
  "keys": "Ctrl+A",
  "elementName": "SearchBox"
}
```

### Request -- Function key without modifiers

```json
{
  "keys": "F5"
}
```

## Error Handling

All errors return: `{ "error": "message" }`

| Scenario | Error Message |
|----------|---------------|
| No active main window | `"Main window is not available."` |
| Named element not found | `"Element 'xxx' not found."` |
| Element is not interactive | `"Element 'xxx' is not an Interactive element..."` |
| Empty key combination | `"Key combination cannot be empty."` |
| No primary key found | `"No primary key found..."` |
| Unrecognized key name | `"Unknown key: 'xxx'."` |

## Notes

- The tool dispatches a `KeyDown` event followed by a `KeyUp` event. Modifier keys are included in the event's `KeyModifiers` property, not as separate key events.
- When no `elementName` is provided, the shortcut is dispatched to the currently focused element. If no element has focus, it falls back to the main window.
- Key names are case-insensitive. `"ctrl+s"`, `"Ctrl+S"`, and `"CTRL+S"` are all equivalent.
- The primary key must be a valid member of the `Avalonia.Input.Key` enumeration. If you are unsure of the exact name, refer to the Avalonia documentation for the `Key` enum.
- Multiple modifiers can be combined: `"Ctrl+Shift+Alt+S"`.
- Related tools: [simulate_type](simulate-type.md) for typing text, [simulate_click](simulate-click.md) for mouse interactions.
