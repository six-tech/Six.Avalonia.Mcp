# set_target_app

Set the target Avalonia application for subsequent inspection commands. Use `list_connected_apps` to see available app IDs.

## Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `appId` | string | Yes | The app ID of the connected Avalonia application to target. |

The `IAppRegistry` service is injected by the framework.

## Return Type

Returns a JSON object confirming the target was set successfully.

```json
{
  "success": true,
  "targetAppId": "string"
}
```

## Example

### Request

```json
{
  "appId": "abc-123"
}
```

### Response

```json
{
  "success": true,
  "targetAppId": "abc-123"
}
```

## Error Handling

All errors return: `{ "error": "message" }`

| Scenario | Error Message |
|----------|---------------|
| App ID not found | `"App 'xxx' is not connected."` |

## Notes

- Call `list_connected_apps` first to discover available app IDs and see which app is currently targeted.
- Once a target is set, all subsequent inspection tool calls (e.g., `get_visual_tree`, `simulate_click`) will default to the targeted app when no `appId` parameter is passed.
- Alternatively, you can skip calling `set_target_app` entirely and pass the `appId` parameter directly to each individual inspection tool.
- When only one app is connected, it is automatically targeted and there is no need to call this tool.
- If the targeted app disconnects, the target is cleared and must be set again.
- This tool updates the `IAppRegistry` service directly and does not send any commands to the connected apps over SignalR.

## Related Tools

- [list_connected_apps](list-connected-apps.md) -- List all connected apps and their IDs.
