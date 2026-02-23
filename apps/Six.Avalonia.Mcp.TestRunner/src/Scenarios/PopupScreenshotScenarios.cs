using System.Text.Json;

namespace Six.Avalonia.Mcp.TestRunner.Scenarios;

public class PopupScreenshotScenarios(McpToolClient client) : IScenarioGroup
{
    public IEnumerable<(string Name, Func<Task> Action)> GetScenarios()
    {
        yield return ("CompositeScreenshot_ContextMenu", CompositeScreenshotContextMenu);
        yield return ("CompositeScreenshot_ComboBoxDropdown", CompositeScreenshotComboBoxDropdown);
        yield return ("CompositeScreenshot_Flyout", CompositeScreenshotFlyout);
        yield return ("CompositeScreenshot_MenuFlyout", CompositeScreenshotMenuFlyout);
        yield return ("CompositeScreenshot_DatePicker", CompositeScreenshotDatePicker);
    }

    private async Task CompositeScreenshotContextMenu()
    {
        await client.SimulateClickAsync("ContextMenuTarget", button: "right");
        await Task.Delay(500);

        string result = await client.TakeScreenshotAsync();
        AssertScreenshotSuccess(result, "ContextMenu");

        await client.SimulateKeyboardShortcutAsync("Escape");
        await Task.Delay(300);
    }

    private async Task CompositeScreenshotComboBoxDropdown()
    {
        // Click the ComboBox to open its dropdown popup
        await client.SimulateClickAsync("SelectComboBox");
        await Task.Delay(500);

        string result = await client.TakeScreenshotAsync();
        AssertScreenshotSuccess(result, "ComboBox");

        // Dismiss by pressing Escape
        await client.SimulateKeyboardShortcutAsync("Escape");
        await Task.Delay(300);
    }

    private async Task CompositeScreenshotFlyout()
    {
        await client.SimulateClickAsync("FlyoutButton");
        await Task.Delay(500);

        string result = await client.TakeScreenshotAsync();
        AssertScreenshotSuccess(result, "Flyout");

        await client.SimulateKeyboardShortcutAsync("Escape");
        await Task.Delay(300);
    }

    private async Task CompositeScreenshotMenuFlyout()
    {
        await client.SimulateClickAsync("MenuFlyoutButton");
        await Task.Delay(500);

        string result = await client.TakeScreenshotAsync();
        AssertScreenshotSuccess(result, "MenuFlyout");

        await client.SimulateKeyboardShortcutAsync("Escape");
        await Task.Delay(300);
    }

    private async Task CompositeScreenshotDatePicker()
    {
        // DatePicker opens its calendar popup via an internal template button (PART_FlyoutButton),
        // not by clicking the DatePicker control itself. Find and click that button.
        string elements = await client.FindElementAsync(name: "PART_FlyoutButton");
        using var elemDoc = JsonDocument.Parse(elements);
        var arr = elemDoc.RootElement;
        Assert(arr.ValueKind == JsonValueKind.Array && arr.GetArrayLength() > 0,
            "Should find PART_FlyoutButton inside DatePicker");

        await client.SimulateClickAsync("PART_FlyoutButton");
        await Task.Delay(500);

        string result = await client.TakeScreenshotAsync();
        AssertScreenshotSuccess(result, "DatePicker");

        await client.SimulateKeyboardShortcutAsync("Escape");
        await Task.Delay(300);
    }

    private static void AssertScreenshotSuccess(string resultJson, string popupType)
    {
        using var doc = JsonDocument.Parse(resultJson);
        var root = doc.RootElement;

        Assert(!root.TryGetProperty("error", out _),
            $"Composite screenshot with {popupType} failed: {resultJson}");
        Assert(root.TryGetProperty("filePath", out _),
            $"Composite screenshot with {popupType} should return filePath");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"Assertion failed: {message}");
        }
    }
}
