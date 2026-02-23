# get_app_metadata

Get metadata about the running Avalonia application including process info, window dimensions, and framework versions.

## Parameters

This tool takes no user-supplied parameters. The `IAvaloniaInspector` and `CancellationToken` are injected by the framework.

## Return Type

Returns an `AppMetadata` object containing process information, window dimensions, and framework versions.

```json
{
  "processName": "string",
  "processId": 0,
  "mainWindowTitle": "string",
  "avaloniaVersion": "string",
  "dotNetVersion": "string",
  "windowCount": 0,
  "windowWidth": 0.0,
  "windowHeight": 0.0
}
```

## Example

### Request

```json
{}
```

### Response

```json
{
  "processName": "MyApp",
  "processId": 12345,
  "mainWindowTitle": "Six Avalonia MCP Server",
  "avaloniaVersion": "11.3.0.0",
  "dotNetVersion": "10.0.0",
  "windowCount": 1,
  "windowWidth": 1280.0,
  "windowHeight": 720.0
}
```

## Error Handling

All errors return: `{ "error": "message" }`

| Scenario | Error Message |
|----------|---------------|
| No active main window | `"Main window is not available."` |

## Notes

- This tool is a good starting point for an agent session. It confirms that the application is running and provides window dimensions useful for screenshot and interaction planning.
- The `avaloniaVersion` reflects the runtime assembly version, not the NuGet package version.
- The `dotNetVersion` reports the runtime version (e.g., `"10.0.0"`), not the target framework moniker.
- `windowCount` reports the total number of open windows, not just the main window.
