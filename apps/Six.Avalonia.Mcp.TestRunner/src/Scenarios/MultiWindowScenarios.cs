using System.Text.Json;

namespace Six.Avalonia.Mcp.TestRunner.Scenarios;

public class MultiWindowScenarios(McpToolClient client) : IScenarioGroup
{
    public IEnumerable<(string Name, Func<Task> Action)> GetScenarios()
    {
        yield return ("OpenDialog_ListsWindows", OpenDialogListsWindows);
        yield return ("ScreenshotDialog", ScreenshotDialog);
        yield return ("ScreenshotMainWhileDialogOpen", ScreenshotMainWhileDialogOpen);
        yield return ("DialogClose_BackToOneWindow", DialogCloseBackToOneWindow);
        yield return ("FindElementInDialog", FindElementInDialog);
        yield return ("OpenNonModalWindow", OpenNonModalWindow);
        yield return ("ScreenshotNonModalWindow", ScreenshotNonModalWindow);
        yield return ("ScreenshotMainWhileNonModalOpen", ScreenshotMainWhileNonModalOpen);
        yield return ("ScreenshotDialogElement", ScreenshotDialogElement);
        yield return ("ScaledScreenshotDialog", ScaledScreenshotDialog);
        yield return ("CompositeScreenshotDialog", CompositeScreenshotDialog);
        yield return ("CompositeScreenshotNonModal", CompositeScreenshotNonModal);
    }

    private async Task OpenDialogListsWindows()
    {
        await client.SimulateClickAsync("OpenDialogButton");
        await Task.Delay(1000);

        string windowsJson = await client.ListWindowsAsync();
        int windowCount = CountWindows(windowsJson);
        Assert(windowCount >= 2, $"Should have at least 2 windows after opening dialog, got {windowCount}");

        await CloseTopWindow();
    }

    private async Task ScreenshotDialog()
    {
        await client.SimulateClickAsync("OpenDialogButton");
        await Task.Delay(1000);

        string result = await client.TakeScreenshotAsync(windowIndex: 1);
        AssertScreenshotSuccess(result);

        await CloseTopWindow();
    }

    private async Task ScreenshotMainWhileDialogOpen()
    {
        await client.SimulateClickAsync("OpenDialogButton");
        await Task.Delay(1000);

        string result = await client.TakeScreenshotAsync(windowIndex: 0);
        AssertScreenshotSuccess(result);

        await CloseTopWindow();
    }

    private async Task DialogCloseBackToOneWindow()
    {
        await client.SimulateClickAsync("OpenDialogButton");
        await Task.Delay(1000);

        await client.SimulateKeyboardShortcutAsync("Escape", windowIndex: 0);
        await Task.Delay(500);

        string windowsJson = await client.ListWindowsAsync();
        int windowCount = CountWindows(windowsJson);
        Assert(windowCount == 1, $"Should have 1 window after closing dialog, got {windowCount}");
    }

    private async Task FindElementInDialog()
    {
        await client.SimulateClickAsync("OpenDialogButton");
        await Task.Delay(1000);

        string json = await client.FindElementAsync(name: "DialogTitle");
        using var doc = JsonDocument.Parse(json);
        var arr = doc.RootElement;
        Assert(arr.ValueKind == JsonValueKind.Array && arr.GetArrayLength() > 0,
            "Should find 'DialogTitle' element in dialog");

        await CloseTopWindow();
    }

    private async Task OpenNonModalWindow()
    {
        await client.SimulateClickAsync("OpenWindowButton");
        await Task.Delay(1000);

        string windowsJson = await client.ListWindowsAsync();
        int windowCount = CountWindows(windowsJson);
        Assert(windowCount >= 2, $"Should have at least 2 windows after opening non-modal window, got {windowCount}");

        await CloseTopWindow();
    }

    private async Task ScreenshotNonModalWindow()
    {
        await client.SimulateClickAsync("OpenWindowButton");
        await Task.Delay(1000);

        string result = await client.TakeScreenshotAsync(windowIndex: 0);
        AssertScreenshotSuccess(result);

        await CloseTopWindow();
    }

    private async Task ScreenshotMainWhileNonModalOpen()
    {
        await client.SimulateClickAsync("OpenWindowButton");
        await Task.Delay(1000);

        string result = await client.TakeScreenshotAsync(windowIndex: 1);
        AssertScreenshotSuccess(result);

        await CloseTopWindow();
    }

    private async Task ScreenshotDialogElement()
    {
        await client.SimulateClickAsync("OpenDialogButton");
        await Task.Delay(1000);

        string result = await client.TakeScreenshotAsync(elementName: "DialogTitle", windowIndex: 0);
        AssertScreenshotSuccess(result);

        await CloseTopWindow();
    }

    private async Task ScaledScreenshotDialog()
    {
        await client.SimulateClickAsync("OpenDialogButton");
        await Task.Delay(1000);

        string result = await client.TakeScreenshotAsync(scaleFactor: 2.0, windowIndex: 0);
        AssertScreenshotSuccess(result);

        using var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;
        Assert(root.TryGetProperty("width", out var w) && w.GetInt32() > 0, "Scaled screenshot should report width");
        Assert(root.TryGetProperty("height", out var h) && h.GetInt32() > 0, "Scaled screenshot should report height");

        await CloseTopWindow();
    }

    private async Task CompositeScreenshotDialog()
    {
        await client.SimulateClickAsync("OpenDialogButton");
        await Task.Delay(1000);

        string result = await client.TakeScreenshotAsync();
        AssertScreenshotSuccess(result);

        using var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;
        // Composite (default) should be at least as large as the main window
        Assert(root.TryGetProperty("width", out var w) && w.GetInt32() > 400, "Composite screenshot should be wider than a dialog alone");
        Assert(root.TryGetProperty("height", out var h) && h.GetInt32() > 400, "Composite screenshot should be taller than a dialog alone");

        await CloseTopWindow();
    }

    private async Task CompositeScreenshotNonModal()
    {
        await client.SimulateClickAsync("OpenWindowButton");
        await Task.Delay(1000);

        string result = await client.TakeScreenshotAsync();
        AssertScreenshotSuccess(result);

        await CloseTopWindow();
    }

    private async Task CloseTopWindow()
    {
        try
        {
            // Try Escape first (works for dialogs).
            // Index 0 = topmost (most recently opened) window since GetAllTopLevels() returns topmost-first.
            string windowsJson = await client.ListWindowsAsync();
            int count = CountWindows(windowsJson);
            if (count > 1)
            {
                await client.SimulateKeyboardShortcutAsync("Escape", windowIndex: 0);
                await Task.Delay(500);
            }

            // If still multiple windows, try Alt+F4.
            windowsJson = await client.ListWindowsAsync();
            if (CountWindows(windowsJson) > 1)
            {
                await client.SimulateKeyboardShortcutAsync("Alt+F4", windowIndex: 0);
                await Task.Delay(500);
            }
        }
        catch
        {
            // Best-effort cleanup.
        }
    }

    private static int CountWindows(string windowsJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(windowsJson);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Array)
            {
                return root.GetArrayLength();
            }

            if (root.TryGetProperty("windows", out var windows))
            {
                return windows.GetArrayLength();
            }

            if (root.TryGetProperty("count", out var count))
            {
                return count.GetInt32();
            }

            return 0;
        }
        catch
        {
            return 0;
        }
    }

    private static void AssertScreenshotSuccess(string resultJson)
    {
        using var doc = JsonDocument.Parse(resultJson);
        var root = doc.RootElement;

        // take_screenshot returns {filePath, width, height} on success, {error} on failure.
        Assert(!root.TryGetProperty("error", out _), $"Screenshot failed: {resultJson}");
        Assert(root.TryGetProperty("filePath", out _), "Screenshot should return filePath on success");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"Assertion failed: {message}");
        }
    }
}
