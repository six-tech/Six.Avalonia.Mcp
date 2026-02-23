using System.Text.Json;

namespace Six.Avalonia.Mcp.TestRunner.Scenarios;

public class ScrollScenarios(McpToolClient client) : IScenarioGroup
{
    public IEnumerable<(string Name, Func<Task> Action)> GetScenarios()
    {
        yield return ("ScrollListBox_Down", ScrollListBoxDown);
        yield return ("ScrollMainWindow", ScrollMainWindow);
    }

    private async Task ScrollListBoxDown()
    {
        // Scroll the scrollable list box downward.
        string result = await client.SimulateScrollAsync("ScrollableListBox", deltaY: -120);
        await Task.Delay(300);

        // Verify the call succeeded (no error).
        using var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;
        Assert(!root.TryGetProperty("error", out _), $"SimulateScroll failed: {result}");
        Assert(root.TryGetProperty("success", out var success) && success.GetBoolean(),
            "SimulateScroll should return success=true");
    }

    private async Task ScrollMainWindow()
    {
        // Scroll the right panel (a ScrollViewer's child).
        string result = await client.SimulateScrollAsync("RightPanel", deltaY: -120);
        await Task.Delay(300);

        using var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;
        Assert(!root.TryGetProperty("error", out _), $"SimulateScroll on RightPanel failed: {result}");
        Assert(root.TryGetProperty("success", out var success) && success.GetBoolean(),
            "SimulateScroll on RightPanel should return success=true");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"Assertion failed: {message}");
        }
    }
}
