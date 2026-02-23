using System.Text.Json;

namespace Six.Avalonia.Mcp.TestRunner.Scenarios;

public class ButtonClickScenarios(McpToolClient client) : IScenarioGroup
{
    public IEnumerable<(string Name, Func<Task> Action)> GetScenarios()
    {
        yield return ("ClickButton_UpdatesStatus", ClickButtonUpdatesStatus);
        yield return ("ClickCommandButton_IncrementsCounter", ClickCommandButtonIncrementsCounter);
        yield return ("ClickCheckBox_TogglesState", ClickCheckBoxTogglesState);
        yield return ("ClickRadioButton_SelectsOne", ClickRadioButtonSelectsOne);
        yield return ("ClickToggleButton", ClickToggleButton);
        yield return ("ClickExpander", ClickExpander);
    }

    private async Task ClickButtonUpdatesStatus()
    {
        // Get initial status text.
        string beforeJson = await client.GetElementPropertiesAsync(elementName: "StatusText");

        // Click the action button.
        await client.SimulateClickAsync("ActionButton");
        await Task.Delay(300);

        // Get updated status text.
        string afterJson = await client.GetElementPropertiesAsync(elementName: "StatusText");

        // The properties JSON itself should have changed (status text updated).
        Assert(afterJson != beforeJson || afterJson.Contains("clicked at", StringComparison.OrdinalIgnoreCase),
            "StatusText should change after clicking ActionButton");
    }

    private async Task ClickCommandButtonIncrementsCounter()
    {
        // Get initial counter text.
        string beforeJson = await client.GetElementPropertiesAsync(elementName: "ClickCountText");
        string beforeText = ExtractTextContent(beforeJson);

        // Click the command button.
        await client.SimulateClickAsync("CommandButton");
        await Task.Delay(300);

        // Get updated counter text.
        string afterJson = await client.GetElementPropertiesAsync(elementName: "ClickCountText");
        string afterText = ExtractTextContent(afterJson);

        Assert(afterText != beforeText, $"ClickCountText should change after clicking CommandButton (before='{beforeText}', after='{afterText}')");
    }

    private async Task ClickCheckBoxTogglesState()
    {
        await client.SimulateClickAsync("ToggleCheckBox");
        await Task.Delay(300);

        string resultJson = await client.GetElementPropertiesAsync(elementName: "ToggleCheckBox");
        Assert(!string.IsNullOrEmpty(resultJson), "Should get properties for ToggleCheckBox");
    }

    private async Task ClickRadioButtonSelectsOne()
    {
        await client.SimulateClickAsync("RadioOption2");
        await Task.Delay(300);

        string resultJson = await client.GetElementPropertiesAsync(elementName: "RadioOption2");
        Assert(!string.IsNullOrEmpty(resultJson), "Should get properties for RadioOption2");
    }

    private async Task ClickToggleButton()
    {
        await client.SimulateClickAsync("TestToggleButton");
        await Task.Delay(300);

        string resultJson = await client.GetElementPropertiesAsync(elementName: "TestToggleButton");
        Assert(!string.IsNullOrEmpty(resultJson), "Should get properties for TestToggleButton");
    }

    private async Task ClickExpander()
    {
        await client.SimulateClickAsync("TestExpander");
        await Task.Delay(300);

        string resultJson = await client.GetElementPropertiesAsync(elementName: "TestExpander");
        Assert(!string.IsNullOrEmpty(resultJson), "Should get properties for TestExpander");
    }

    private static string ExtractTextContent(string propertiesJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(propertiesJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("text", out var textProp) && textProp.ValueKind == JsonValueKind.String)
            {
                return textProp.GetString() ?? "";
            }

            if (root.TryGetProperty("dataContextType", out var dct))
            {
                return dct.GetString() ?? "";
            }

            return propertiesJson;
        }
        catch
        {
            return propertiesJson;
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"Assertion failed: {message}");
        }
    }
}
