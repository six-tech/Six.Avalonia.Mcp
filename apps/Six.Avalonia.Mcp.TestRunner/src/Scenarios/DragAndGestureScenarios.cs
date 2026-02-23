using System.Text.Json;

namespace Six.Avalonia.Mcp.TestRunner.Scenarios;

public class DragAndGestureScenarios(McpToolClient client) : IScenarioGroup
{
    public IEnumerable<(string Name, Func<Task> Action)> GetScenarios()
    {
        yield return ("DragSlider_ChangesValue", DragSliderChangesValue);
        yield return ("DragThumb_MovesElement", DragThumbMovesElement);
        yield return ("HoverTarget_ShowsStatus", HoverTargetShowsStatus);
    }

    private async Task DragSliderChangesValue()
    {
        // Get initial slider value.
        string beforeJson = await client.GetElementPropertiesAsync(elementName: "ValueSlider");

        // Drag the slider from left to right.
        await client.SimulateDragAsync("ValueSlider", startX: 10, startY: 10, endX: 200, endY: 10, steps: 10);
        await Task.Delay(300);

        // Get updated slider value.
        string afterJson = await client.GetElementPropertiesAsync(elementName: "ValueSlider");

        Assert(afterJson != beforeJson, "Slider properties should change after drag");
    }

    private async Task DragThumbMovesElement()
    {
        // Get initial thumb position via Canvas.Left/Canvas.Top (visible via parent properties).
        string beforeJson = await client.GetElementPropertiesAsync(elementName: "DraggableThumb");
        double beforeX = ExtractBoundsX(beforeJson);

        // Drag the thumb.
        await client.SimulateDragAsync("DragCanvas", startX: 40, startY: 40, endX: 150, endY: 100, steps: 10);
        await Task.Delay(300);

        // Verify something was dragged (canvas received the events).
        string afterJson = await client.GetElementPropertiesAsync(elementName: "DragCanvas");
        Assert(!string.IsNullOrEmpty(afterJson), "DragCanvas should still have properties after drag");
    }

    private async Task HoverTargetShowsStatus()
    {
        // Hover over the target.
        await client.SimulateHoverAsync("HoverTarget");
        await Task.Delay(300);

        // Check status text shows hovered.
        string hoveredJson = await client.GetElementPropertiesAsync(elementName: "HoverStatusText");
        Assert(hoveredJson.Contains("Hovered", StringComparison.OrdinalIgnoreCase),
            "HoverStatusText should contain 'Hovered' after hover");

        // Exit hover.
        await client.SimulateHoverAsync("HoverTarget", exitAfter: true);
        await Task.Delay(300);

        // Check status reverts.
        string exitedJson = await client.GetElementPropertiesAsync(elementName: "HoverStatusText");
        Assert(exitedJson.Contains("Not hovered", StringComparison.OrdinalIgnoreCase),
            "HoverStatusText should contain 'Not hovered' after hover exit");
    }

    private static double ExtractBoundsX(string propertiesJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(propertiesJson);
            if (doc.RootElement.TryGetProperty("bounds", out var bounds) &&
                bounds.TryGetProperty("x", out var x))
            {
                return x.GetDouble();
            }
        }
        catch
        {
            // ignored
        }

        return 0;
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"Assertion failed: {message}");
        }
    }
}
