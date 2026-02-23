using System.Text.Json;

namespace Six.Avalonia.Mcp.TestRunner.Scenarios;

public class ContextMenuScenarios(McpToolClient client) : IScenarioGroup
{
    public IEnumerable<(string Name, Func<Task> Action)> GetScenarios()
    {
        yield return ("RightClick_OpensContextMenu", RightClickOpensContextMenu);
        yield return ("ClickContextMenuItem", ClickContextMenuItem);
        yield return ("ScreenshotWithContextMenu", ScreenshotWithContextMenu);
    }

    private async Task RightClickOpensContextMenu()
    {
        await client.SimulateClickAsync("ContextMenuTarget", button: "right");
        await Task.Delay(500);

        string json = await client.FindElementAsync(name: "CopyMenuItem");
        using var doc = JsonDocument.Parse(json);
        var arr = doc.RootElement;

        Assert(arr.ValueKind == JsonValueKind.Array && arr.GetArrayLength() > 0,
            "Should find 'CopyMenuItem' after right-clicking context menu target");

        // Dismiss the context menu.
        await client.SimulateKeyboardShortcutAsync("Escape");
        await Task.Delay(300);
    }

    private async Task ClickContextMenuItem()
    {
        await client.SimulateClickAsync("ContextMenuTarget", button: "right");
        await Task.Delay(500);

        await client.SimulateClickAsync("CopyMenuItem");
        await Task.Delay(500);

        string resultJson = await client.GetElementPropertiesAsync(elementName: "ContextMenuResultText");
        Assert(resultJson.Contains("Copied", StringComparison.OrdinalIgnoreCase),
            "ContextMenuResultText should contain 'Copied' after clicking CopyMenuItem");
    }

    private async Task ScreenshotWithContextMenu()
    {
        await client.SimulateClickAsync("ContextMenuTarget", button: "right");
        await Task.Delay(500);

        string result = await client.TakeScreenshotAsync();
        using var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;

        // take_screenshot returns {filePath, width, height} on success, {error} on failure.
        Assert(!root.TryGetProperty("error", out _), $"Screenshot failed: {result}");
        Assert(root.TryGetProperty("filePath", out _), "Screenshot with context menu open should succeed");

        // Dismiss the context menu.
        await client.SimulateKeyboardShortcutAsync("Escape");
        await Task.Delay(300);
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"Assertion failed: {message}");
        }
    }
}
