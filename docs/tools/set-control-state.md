# set_control_state

Set a control state property on a named element. State types: `focus` (focus/blur), `enabled` (enable/disable), `visible` (show/hide). Value is true to enable the state or false to disable it.

## Parameters

| Parameter | Type | Default | Required | Description |
|-----------|------|---------|----------|-------------|
| elementName | string | -- | Yes | The `x:Name` of the target element. |
| stateType | string | -- | Yes | The state property to set: `"focus"`, `"enabled"`, or `"visible"`. |
| value | bool | -- | Yes | `true` to enable the state, `false` to disable it. |

### State Types

| State Type | value=true | value=false | Required Base Type |
|------------|-----------|-------------|-------------------|
| `focus` | Focus the element | Blur the element (focuses the window instead) | `InputElement` |
| `enabled` | Set `IsEnabled = true` | Set `IsEnabled = false` | `Control` |
| `visible` | Set `IsVisible = true` | Set `IsVisible = false` | `Control` |

## Return Type

Returns an `InteractionResult` object reflecting the state of the element after the change.

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

### Request -- Focus an element

```json
{
  "elementName": "EmailInput",
  "stateType": "focus",
  "value": true
}
```

### Response

```json
{
  "success": true,
  "elementType": "TextBox",
  "elementName": "EmailInput",
  "isFocused": true,
  "isEnabled": true,
  "isVisible": true,
  "isPointerOver": false
}
```

### Request -- Disable a button

```json
{
  "elementName": "SubmitButton",
  "stateType": "enabled",
  "value": false
}
```

### Response

```json
{
  "success": true,
  "elementType": "Button",
  "elementName": "SubmitButton",
  "isFocused": false,
  "isEnabled": false,
  "isVisible": true,
  "isPointerOver": false
}
```

### Request -- Hide a panel

```json
{
  "elementName": "SettingsPanel",
  "stateType": "visible",
  "value": false
}
```

## Error Handling

All errors return: `{ "error": "message" }`

| Scenario | Error Message |
|----------|---------------|
| No active main window | `"Main window is not available."` |
| Named element not found | `"Element 'xxx' not found."` |
| Focus on non-InputElement | `"Element 'xxx' is not an InputElement..."` |
| Enabled/visible on non-Control | `"Element 'xxx' is not a Control..."` |
| Invalid state type | `"Unknown state type: 'xxx'."` |

## Notes

- Setting `focus` to `false` does not simply unfocus the element. Instead, it focuses the main window, which effectively blurs the target element. This is because Avalonia requires focus to be on some element at all times.
- The `enabled` state affects the entire subtree: disabling a parent container disables all its children.
- The `visible` state controls whether the element is rendered and participates in layout. A hidden element does not occupy space.
- The `stateType` value is case-sensitive. Use lowercase: `"focus"`, `"enabled"`, `"visible"`.
- Related tools: [simulate_click](simulate-click.md) for focusing via click, [get_element_properties](get-element-properties.md) to verify state changes.
