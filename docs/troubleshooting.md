# Troubleshooting

This guide covers 12 common issues you may encounter when using Six.Avalonia.Mcp, along with their symptoms, causes, and solutions.

---

## 1. "TopLevel is not available"

### Symptom

MCP tools return an error:

```json
{"error": "TopLevel is not available."}
```

### Cause

The `AvaloniaInspector.GetTopLevel()` method checks for `IClassicDesktopStyleApplicationLifetime` (desktop) or `ISingleViewApplicationLifetime` (single-view). This error occurs when:

- `Application.Current` is null (the app has not fully initialized yet).
- The MCP tool is called before the application lifetime has started.
- Neither a desktop window nor a single view is available.

### Solution

Ensure your `Program.cs` starts the application lifetime before MCP tools are invoked. The connector will wait for the app to initialize, but the TopLevel must be available when commands arrive.

The inspector now supports both desktop and single-view lifetimes via `TopLevel.GetTopLevel(view)`. For desktop apps:

```csharp
BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
```

If you need a custom lifetime or multi-window support, subclass `AvaloniaInspector` and override `GetTopLevel()`:

```csharp
protected override TopLevel? GetTopLevel()
{
    // Return your TopLevel from a custom source
    return MyWindowManager.ActiveWindow;
}
```

---

## 2. Element Not Found

### Symptom

Tools that accept `elementName` return:

```json
{"error": "Element 'MyButton' not found."}
```

### Cause

The element cannot be located in the visual tree. Common reasons:

- The control does not have an `x:Name` attribute in XAML.
- The control name is misspelled (names are case-sensitive).
- The control has not been added to the visual tree yet (e.g., it is inside a `DataTemplate` that has not been realized, or in a lazy-loaded tab).
- The control is in a different window than the top-level view.

### Solution

1. **Add `x:Name` to the control** in your XAML:

```xml
<!-- Before: not discoverable by name -->
<Button Content="Submit" />

<!-- After: discoverable as "SubmitButton" -->
<Button x:Name="SubmitButton" Content="Submit" />
```

2. **Verify the name** by calling `get_visual_tree` first to see the full tree with all named elements.

3. **Wait for the control to be realized.** If the element is inside a `DataTemplate`, `TabControl`, or similar container, it may not exist in the visual tree until it is displayed. Navigate to the relevant view first.

4. **For multi-window apps**, override `GetTopLevel()` in a subclass to return the correct window.

---

## 3. MCP Client Cannot Connect

### Symptom

The MCP client times out, receives no response, or gets garbled data when trying to connect.

### Cause

In the decoupled architecture, the MCP client connects to the **Server process** (not the Avalonia app directly). The Server uses STDIO for MCP communication and SignalR for app communication. Common issues:

- The Server process is not running.
- Application logging or other output is being written to stdout in the Server process, which corrupts the MCP JSON-RPC message stream.
- The MCP client is misconfigured (pointing to the wrong process).

### Solution

1. **Start the MCP server first**:

```bash
# Via dotnet tool
six-avalonia-mcp

# Or via project
dotnet run --project apps/Six.Avalonia.Mcp.Server
```

2. **Configure your MCP client** to launch the server process. The server uses STDIO transport -- stdout is reserved for MCP messages.

3. **Redirect all logging to stderr** in the server (this is already done by default):

```csharp
logging.ClearProviders();
logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});
```

4. Ensure that no code in the Server writes to stdout via `Console.WriteLine()`.

---

## 4. SignalR Connection Failed

### Symptom

The Avalonia app logs errors to stderr:

```
Failed to connect to MCP server at http://localhost:52342/inspector
```

Or the connector keeps retrying the connection.

### Cause

The Connector in the Avalonia app cannot establish a SignalR connection to the MCP server. Common reasons:

- The MCP server is not running.
- The server is running on a different port.
- A firewall is blocking the connection.
- The `ServerUrl` option is misconfigured.

### Solution

1. **Start the MCP server first** before launching the Avalonia app:

```bash
six-avalonia-mcp
```

2. **Verify the port** -- the default is 52342. Check with:

```bash
curl http://localhost:52342/inspector/negotiate?negotiateVersion=1
```

3. **Check the `ServerUrl` option** in your connector configuration (defaults to `http://localhost:52342/inspector`):

```csharp
services.AddAvaloniaMcpConnector(options =>
{
    options.ServerUrl = "http://localhost:52342/inspector"; // this is the default
});
```

Or override via environment variable: `export SIX_MCP_ServerUrl=http://localhost:52342/inspector`

4. **Check firewall rules** to ensure localhost connections on port 52342 are allowed.

5. The connector has built-in reconnection logic. Check stderr logs for detailed connection status.

---

## 5. Multi-App Targeting

### Symptom

When multiple Avalonia apps are connected to the same MCP server, commands are sent to the wrong app, or you receive:

```json
{"error": "Multiple apps connected. Specify appId or use set_target_app."}
```

### Cause

When more than one app is connected, the server does not know which app should receive the command unless you specify a target.

### Solution

1. **List connected apps** to see all available targets:

```
list_connected_apps
```

2. **Set a default target** so all subsequent commands go to a specific app:

```
set_target_app with appId: "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
```

3. **Pass `appId` directly** to any tool call to target a specific app without changing the default:

```
get_visual_tree with appId: "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
```

When only one app is connected, it is automatically targeted and no `appId` is needed.

---

## 6. Empty Screenshot

### Symptom

The `take_screenshot` tool returns a base64 string, but the decoded image is blank or has zero content.

### Cause

This is a known limitation of the Avalonia headless platform. `RenderTargetBitmap` produces empty output when running without a real windowing system. This typically happens in:

- Headless test environments (`Avalonia.Headless.XUnit`).
- CI/CD pipelines without a display server.
- WSL without X11/Wayland forwarding.

### Solution

Screenshots require a real windowing system. Ensure the application is running with a visible desktop:

- On **Linux CI/CD**, use `xvfb-run` to provide a virtual display:
  ```bash
  xvfb-run dotnet run --project MyApp.csproj
  ```

- On **Windows/macOS**, screenshots work when the application is displayed on screen.

- In **tests**, do not assert on screenshot content. Instead, verify the code path executes without throwing:
  ```csharp
  (string base64, int width, int height) = await inspector.TakeScreenshotAsync();
  base64.Should().NotBeNull();  // May be empty in headless mode
  ```

---

## 7. "Not an Interactive Element"

### Symptom

Interaction tools (`simulate_click`, `simulate_type`, `simulate_hover`, `simulate_drag`) return:

```json
{"error": "Element 'MyElement' (Panel) is not an Interactive element and cannot receive pointer events."}
```

### Cause

The element inherits from `Visual` but not from `Interactive`. In Avalonia's class hierarchy:

```
Visual -> Layoutable -> Interactive -> InputElement -> Control
```

Only elements that inherit from `Interactive` (or lower) have a `RaiseEvent()` method and can receive pointer and keyboard events. Pure `Visual` or `Layoutable` elements (such as `Panel`, `Canvas`, or `Decorator` when used as base types) cannot.

### Solution

Target an interactive child control instead. Use `get_visual_tree` to find the nearest interactive descendant:

```
get_visual_tree with elementName: "MyPanel"
```

Then target a specific child control by name:

```
simulate_click with elementName: "ButtonInsidePanel"
```

If you need the panel itself to receive events, consider wrapping its content in a `Button` with a transparent style, or using an `InputElement`-derived container.

---

## 8. Click Does Not Trigger Handler

### Symptom

`simulate_click` returns `{"success": true}` but the button's click handler does not execute.

### Cause

The click event was dispatched but the control cannot process it. Possible reasons:

- **Element is disabled**: `IsEnabled` is `false` (or a parent's `IsEnabled` is `false`, making it effectively disabled).
- **Element is not visible**: `IsVisible` is `false`, so it does not participate in hit testing.
- **Element is not hit-test visible**: `IsHitTestVisible` is `false`.
- **Coordinates are outside the element**: If you specified custom `x`/`y` values that fall outside the element's bounds.

### Solution

1. **Check element state** before clicking:

```
get_element_properties with elementName: "MyButton"
```

Verify that `isEnabled`, `isVisible`, and `isHitTestVisible` are all `true`.

2. **Enable the element** if needed:

```
set_control_state with elementName: "MyButton", stateType: "enabled", value: true
```

3. **Omit coordinates** to use the element center (the default):

```
simulate_click with elementName: "MyButton"
```

4. **Check parent controls.** A disabled parent disables all children. Use `get_ancestor_chain` to inspect parent states.

---

## 9. Type Simulation Not Working

### Symptom

`simulate_type` returns success but the text does not appear in the control.

### Cause

- The target element is not a text input control (e.g., it is a `Button` or `TextBlock` rather than a `TextBox`).
- The element was not focused before typing. While `simulate_type` calls `Focus()` automatically, focus may fail if the element is disabled or not visible.
- The element does not process `TextInputEvent` (some custom controls override input handling).

### Solution

1. **Ensure the target is a text input** (e.g., `TextBox`, `AutoCompleteBox`).

2. **Focus the element explicitly** before typing:

```
set_control_state with elementName: "InputTextBox", stateType: "focus", value: true
```

3. **Check focus state** after the focus call:

```
get_element_properties with elementName: "InputTextBox"
```

Verify `isFocused` is `true`.

4. **Use special character escapes** for non-printable characters:
   - `\b` for Backspace
   - `\n` for Enter
   - `\t` for Tab

---

## 10. Keyboard Shortcut Key Not Recognized

### Symptom

`simulate_keyboard_shortcut` throws:

```json
{"error": "Unknown key: 'Delete'. Must be a valid Avalonia.Input.Key enum value."}
```

### Cause

The key name in the shortcut string does not match an `Avalonia.Input.Key` enum value. The parser uses `Enum.TryParse<Key>(keyString, ignoreCase: true)`, so the name must be a valid member of the `Key` enum.

### Solution

Use the correct `Avalonia.Input.Key` enum name. Common mappings:

| Desired Key | Correct Name | Wrong Names |
|-------------|-------------|-------------|
| Delete | `Delete` | `Del`, `Remove` |
| Backspace | `Back` | `Backspace`, `BS` |
| Enter/Return | `Enter` or `Return` | `CR`, `Newline` |
| Escape | `Escape` | `Esc` |
| Space | `Space` | `Spacebar`, ` ` |
| Arrow keys | `Left`, `Right`, `Up`, `Down` | `ArrowLeft`, `LeftArrow` |
| Function keys | `F1` through `F24` | `Fn1` |
| Page Up/Down | `PageUp`, `PageDown` | `PgUp`, `PgDn` |
| Home/End | `Home`, `End` | -- |

Example of correct usage:

```
simulate_keyboard_shortcut with keys: "Ctrl+S"
simulate_keyboard_shortcut with keys: "Alt+F4"
simulate_keyboard_shortcut with keys: "Ctrl+Shift+Z"
simulate_keyboard_shortcut with keys: "F5"
```

---

## 11. Unknown Modifier

### Symptom

`simulate_click` with modifiers or `simulate_keyboard_shortcut` throws:

```json
{"error": "Unknown modifier: 'command'. Use 'ctrl', 'shift', 'alt', or 'meta'."}
```

### Cause

The modifier string contains an unrecognized modifier name. The parser only accepts specific lowercase names.

### Solution

Use only the supported modifier names:

| Modifier | Accepted Names |
|----------|---------------|
| Control | `ctrl`, `control` |
| Shift | `shift` |
| Alt | `alt` |
| Meta (Win/Cmd) | `meta`, `win`, `cmd` |

Combine modifiers with `+`:

```
simulate_click with elementName: "MyButton", modifiers: "ctrl+shift"
simulate_keyboard_shortcut with keys: "Ctrl+Shift+S"
```

Note that modifier names are case-insensitive (`Ctrl`, `ctrl`, and `CTRL` all work), but the names must be exactly one of the accepted values listed above. `command` is not accepted -- use `cmd` or `meta` instead.

---

## 12. JSON Property Names Are Wrong

### Symptom

The JSON returned by MCP tools uses unexpected property names (e.g., `ProcessName` instead of `processName`), or null properties appear in the output.

### Cause

The tool or code is not using `JsonOptions.Default` for serialization. The shared options enforce:

- `camelCase` property naming
- Null value omission (`JsonIgnoreCondition.WhenWritingNull`)
- Compact (non-indented) output

### Solution

Always use `JsonOptions.Default` when serializing:

```csharp
return JsonSerializer.Serialize(result, JsonOptions.Default);
```

The `JsonOptions.Default` instance is defined in `JsonOptions.cs` (in the Contracts library):

```csharp
public static JsonSerializerOptions Default { get; } = new()
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false,
    PropertyNameCaseInsensitive = true
};
```

If you are writing custom tools or extending the inspector, ensure you pass this options instance to both `Serialize` and `Deserialize` calls. Using the default `JsonSerializerOptions` (without the `CamelCase` policy) will produce PascalCase property names that may confuse MCP clients.

For deserialization, `PropertyNameCaseInsensitive = true` means that incoming JSON with either `processName` or `ProcessName` will be correctly mapped.

---

## Getting Help

If your issue is not covered here:

1. Check the [Architecture](./architecture.md) page to understand the decoupled system design (Server + Connector + Contracts).
2. Use `list_connected_apps` to verify your app is connected to the server.
3. Use `get_visual_tree` and `get_element_properties` to diagnose control state.
4. Check stderr output for warning or error log messages from `AvaloniaInspector` (Connector) or the MCP Server.
5. Review the [Testing Guide](./testing-guide.md) for test patterns that may help reproduce the issue in isolation.
