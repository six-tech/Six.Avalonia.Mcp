using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Six.Avalonia.Mcp.Contracts;
using Six.Avalonia.Mcp.Contracts.Models;

namespace Six.Avalonia.Mcp.TestRunner;

public class McpToolClient(McpClient client)
{
    private async Task<string> CallToolAsync(string toolName, Dictionary<string, object?>? args = null, CancellationToken ct = default)
    {
        var result = await client.CallToolAsync(toolName, args, cancellationToken: ct);
        return result.Content.OfType<TextContentBlock>().FirstOrDefault()?.Text ?? "";
    }

    private async Task<T?> CallToolAsync<T>(string toolName, Dictionary<string, object?>? args = null, CancellationToken ct = default)
    {
        string json = await CallToolAsync(toolName, args, ct);
        return JsonSerializer.Deserialize<T>(json, JsonOptions.Default);
    }

    // App management
    public Task<string> ListConnectedAppsAsync(CancellationToken ct = default)
        => CallToolAsync("list_connected_apps", null, ct);

    public Task<string> SetTargetAppAsync(string appId, CancellationToken ct = default)
        => CallToolAsync("set_target_app", new() { ["appId"] = appId }, ct);

    // Metadata
    public Task<AppMetadata?> GetAppMetadataAsync(CancellationToken ct = default)
        => CallToolAsync<AppMetadata>("get_app_metadata", null, ct);

    // Inspection
    public Task<string> ListWindowsAsync(CancellationToken ct = default)
        => CallToolAsync("list_windows", null, ct);

    public Task<string> GetVisualTreeAsync(int maxDepth = 15, string? elementName = null, int? windowIndex = null, CancellationToken ct = default)
        => CallToolAsync("get_visual_tree", new() { ["maxDepth"] = maxDepth, ["elementName"] = elementName, ["windowIndex"] = windowIndex }, ct);

    public Task<string> FindElementAsync(string? name = null, string? typeName = null, string? className = null, string? selector = null, int maxResults = 10, int? windowIndex = null, CancellationToken ct = default)
        => CallToolAsync("find_element", new() { ["name"] = name, ["typeName"] = typeName, ["className"] = className, ["selector"] = selector, ["maxResults"] = maxResults, ["windowIndex"] = windowIndex }, ct);

    public Task<string> GetElementPropertiesAsync(string? elementName = null, string? typeName = null, string? path = null, int? windowIndex = null, CancellationToken ct = default)
        => CallToolAsync("get_element_properties", new() { ["elementName"] = elementName, ["typeName"] = typeName, ["path"] = path, ["windowIndex"] = windowIndex }, ct);

    public Task<string> GetElementByPathAsync(string path, int? windowIndex = null, CancellationToken ct = default)
        => CallToolAsync("get_element_by_path", new() { ["path"] = path, ["windowIndex"] = windowIndex }, ct);

    public Task<string> GetAncestorChainAsync(string elementName, int? windowIndex = null, CancellationToken ct = default)
        => CallToolAsync("get_ancestor_chain", new() { ["elementName"] = elementName, ["windowIndex"] = windowIndex }, ct);

    // Screenshots
    public Task<string> TakeScreenshotAsync(string? outputPath = null, string? elementName = null, double scaleFactor = 1.0, int? windowIndex = null, bool composite = true, CancellationToken ct = default)
        => CallToolAsync("take_screenshot", new() { ["outputPath"] = outputPath, ["elementName"] = elementName, ["scaleFactor"] = scaleFactor, ["windowIndex"] = windowIndex, ["composite"] = composite }, ct);

    // Interactions
    public Task<string> SimulateClickAsync(string elementName, string button = "left", double? x = null, double? y = null, string? modifiers = null, int clickCount = 1, int? windowIndex = null, CancellationToken ct = default)
        => CallToolAsync("simulate_click", new() { ["elementName"] = elementName, ["button"] = button, ["x"] = x, ["y"] = y, ["modifiers"] = modifiers, ["clickCount"] = clickCount, ["windowIndex"] = windowIndex }, ct);

    public Task<string> SimulateTypeAsync(string elementName, string text, int? windowIndex = null, CancellationToken ct = default)
        => CallToolAsync("simulate_type", new() { ["elementName"] = elementName, ["text"] = text, ["windowIndex"] = windowIndex }, ct);

    public Task<string> SetControlStateAsync(string elementName, string stateType, bool value, int? windowIndex = null, CancellationToken ct = default)
        => CallToolAsync("set_control_state", new() { ["elementName"] = elementName, ["stateType"] = stateType, ["value"] = value, ["windowIndex"] = windowIndex }, ct);

    public Task<string> SimulateHoverAsync(string elementName, bool exitAfter = false, int? windowIndex = null, CancellationToken ct = default)
        => CallToolAsync("simulate_hover", new() { ["elementName"] = elementName, ["exitAfter"] = exitAfter, ["windowIndex"] = windowIndex }, ct);

    public Task<string> SimulateDragAsync(string elementName, double startX, double startY, double endX, double endY, string button = "left", int steps = 10, int? windowIndex = null, CancellationToken ct = default)
        => CallToolAsync("simulate_drag", new() { ["elementName"] = elementName, ["startX"] = startX, ["startY"] = startY, ["endX"] = endX, ["endY"] = endY, ["button"] = button, ["steps"] = steps, ["windowIndex"] = windowIndex }, ct);

    public Task<string> SimulateKeyboardShortcutAsync(string keys, string? elementName = null, int? windowIndex = null, CancellationToken ct = default)
        => CallToolAsync("simulate_keyboard_shortcut", new() { ["keys"] = keys, ["elementName"] = elementName, ["windowIndex"] = windowIndex }, ct);

    public Task<string> SimulateScrollAsync(string elementName, double deltaX = 0, double deltaY = -120, string? modifiers = null, int? windowIndex = null, CancellationToken ct = default)
        => CallToolAsync("simulate_scroll", new() { ["elementName"] = elementName, ["deltaX"] = deltaX, ["deltaY"] = deltaY, ["modifiers"] = modifiers, ["windowIndex"] = windowIndex }, ct);

    public Task<string> KillAppAsync(string? appId = null, CancellationToken ct = default)
        => CallToolAsync("kill_app", appId is not null ? new() { ["appId"] = appId } : null, ct);
}
