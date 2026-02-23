# simulate_type

Simulate typing text into a named element character by character. The element is focused first. Special characters: `\b`=Backspace, `\n`=Enter, `\t`=Tab.

## Parameters

| Parameter | Type | Default | Required | Description |
|-----------|------|---------|----------|-------------|
| elementName | string | -- | Yes | The `x:Name` of the target element. |
| text | string | -- | Yes | The text to type into the element. Supports special escape sequences. |

### Special Characters

| Escape Sequence | Key Dispatched |
|-----------------|----------------|
| `\b` | Backspace (`Key.Back`) |
| `\n` | Enter (`Key.Enter`) |
| `\r` | Enter (`Key.Enter`) |
| `\t` | Tab (`Key.Tab`) |

All other characters are dispatched as `TextInputEvent` events, one character at a time.

## Return Type

Returns an `InteractionResult` object reflecting the state of the element after typing completes.

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

### Request -- Type plain text

```json
{
  "elementName": "SearchBox",
  "text": "hello world"
}
```

### Response

```json
{
  "success": true,
  "elementType": "TextBox",
  "elementName": "SearchBox",
  "isFocused": true,
  "isEnabled": true,
  "isVisible": true,
  "isPointerOver": false
}
```

### Request -- Type text then press Enter

```json
{
  "elementName": "SearchBox",
  "text": "search query\n"
}
```

### Request -- Delete characters with Backspace

```json
{
  "elementName": "InputField",
  "text": "\b\b\b"
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

- The element is focused before typing begins. If the element cannot receive focus, the text input events may not be processed by the control.
- Each character is dispatched as a separate `TextInputEvent`, simulating real keystroke-by-keystroke input. This means the control's text-changed handlers fire for each character.
- Special characters (`\b`, `\n`, `\r`, `\t`) are dispatched as `KeyDown` + `KeyUp` events with the corresponding `Key` enum value, not as `TextInputEvent`.
- To clear a text field, you can combine this tool with [set_control_state](set-control-state.md) to focus the element, then use [simulate_keyboard_shortcut](simulate-keyboard-shortcut.md) with `"Ctrl+A"` to select all, followed by `simulate_type` with the replacement text.
- Related tools: [simulate_click](simulate-click.md) to click into a field first, [simulate_keyboard_shortcut](simulate-keyboard-shortcut.md) for key combinations.
