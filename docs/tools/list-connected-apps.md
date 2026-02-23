# list_connected_apps

List all currently connected Avalonia applications with their app IDs, names, process info, and connection time.

## Parameters

This tool takes no parameters. The `IAppRegistry` service is injected by the framework.

## Return Type

Returns a JSON object containing an `apps` array and a `count` of connected applications. Each entry in the `apps` array includes the application's ID, name, process details, connection timestamp, and whether it is the current target.

```json
{
  "apps": [
    {
      "appId": "string",
      "appName": "string",
      "processId": 0,
      "processName": "string",
      "connectedAt": "2026-01-15T10:30:00+00:00",
      "isTarget": false
    }
  ],
  "count": 0
}
```

## Example

### Request

```json
{}
```

### Response (single app)

```json
{
  "apps": [
    {
      "appId": "abc-123",
      "appName": "MyAvaloniaApp",
      "processId": 12345,
      "processName": "MyAvaloniaApp",
      "connectedAt": "2026-01-15T10:30:00+00:00",
      "isTarget": true
    }
  ],
  "count": 1
}
```

### Response (multiple apps)

```json
{
  "apps": [
    {
      "appId": "abc-123",
      "appName": "MainApp",
      "processId": 12345,
      "processName": "MainApp",
      "connectedAt": "2026-01-15T10:30:00+00:00",
      "isTarget": true
    },
    {
      "appId": "def-456",
      "appName": "SettingsApp",
      "processId": 67890,
      "processName": "SettingsApp",
      "connectedAt": "2026-01-15T10:31:00+00:00",
      "isTarget": false
    }
  ],
  "count": 2
}
```

### Response (no apps connected)

```json
{
  "apps": [],
  "count": 0
}
```

## Error Handling

All errors return: `{ "error": "message" }`

This tool catches all exceptions and returns them in the standard error format. Under normal operation, errors are unlikely since the tool simply reads from the in-memory app registry.

## Notes

- When only one Avalonia application is connected, it is automatically targeted by all inspection tools. There is no need to call `set_target_app` in single-app scenarios.
- When multiple applications are connected, use this tool to discover their app IDs, then call `set_target_app` to choose a default target or pass `appId` directly to individual inspection tools.
- The `isTarget` field indicates which app is currently set as the default target for inspection commands.
- The `connectedAt` timestamp uses `DateTimeOffset` in ISO 8601 format.
- This tool queries the `IAppRegistry` service directly and does not send any commands to the connected apps over SignalR.

## Related Tools

- [set_target_app](set-target-app.md) -- Set the target app for subsequent inspection commands.
