# kill_app

Terminate a connected Avalonia application. The app will shut down gracefully. Use `list_connected_apps` to find the target app ID.

## Parameters

| Parameter | Type | Default | Required | Description |
|-----------|------|---------|----------|-------------|
| appId | string? | null | No | The app ID of the application to terminate. Auto-targets when only one app is connected. |

## Return Type

Returns a JSON object confirming the shutdown signal was sent.

```json
{
  "success": true,
  "message": "Shutdown signal sent."
}
```

## Example

### Request -- Kill the default target app

```json
{}
```

### Response

```json
{
  "success": true,
  "message": "Shutdown signal sent."
}
```

### Request -- Kill a specific app

```json
{
  "appId": "abc-123"
}
```

## Error Handling

All errors return: `{ "error": "message" }`

| Scenario | Error Message |
|----------|---------------|
| No app connected | `"No Avalonia app is connected. Start an app with the MCP connector enabled."` |
| Specified app not found | `"App 'xxx' is not connected."` |

## Notes

- The server sends a `ShutdownAsync` signal to the connected app via SignalR. The app's `ConnectorService` handles this by stopping the SignalR connection and calling `IHostApplicationLifetime.StopApplication()` to terminate the process gracefully.
- This is distinct from the `DisconnectAsync` signal used during server shutdown (via `GracefulShutdownService`), which only drops the SignalR connection without terminating the app process.
- The tool returns immediately after sending the shutdown signal. The actual app termination happens asynchronously.
- After calling `kill_app`, the app will be removed from the connected apps list once its SignalR connection closes.
- Related tools: [list_connected_apps](list-connected-apps.md) to find app IDs, [set_target_app](set-target-app.md) to set the default target.
