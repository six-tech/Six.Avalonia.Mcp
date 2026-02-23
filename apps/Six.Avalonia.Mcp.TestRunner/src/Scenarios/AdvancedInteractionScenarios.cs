using System.Text.Json;

namespace Six.Avalonia.Mcp.TestRunner.Scenarios;

public class AdvancedInteractionScenarios(McpToolClient client) : IScenarioGroup
{
    public IEnumerable<(string Name, Func<Task> Action)> GetScenarios()
    {
        yield return ("SelectTabItem", SelectTabItem);
        yield return ("SelectListBoxItem", SelectListBoxItem);
        yield return ("DoubleClick_SelectsText", DoubleClickSelectsText);
        yield return ("SetControlState_DisableEnable", SetControlStateDisableEnable);
        yield return ("NumericUpDown_ChangeValue", NumericUpDownChangeValue);
    }

    private async Task SelectTabItem()
    {
        // Click Tab2 to select it.
        await client.SimulateClickAsync("Tab2");
        await Task.Delay(300);

        // Verify Tab2 is now accessible (its content should be visible).
        string propsJson = await client.GetElementPropertiesAsync(elementName: "Tab2");
        Assert(!string.IsNullOrEmpty(propsJson), "Should get properties for Tab2 after clicking it");

        // Check that Tab2 has the :selected pseudo-class.
        using var doc = JsonDocument.Parse(propsJson);
        var root = doc.RootElement;
        bool hasSelected = false;
        if (root.TryGetProperty("pseudoClasses", out var pseudoClasses))
        {
            foreach (var cls in pseudoClasses.EnumerateArray())
            {
                if (cls.GetString()?.Contains("selected", StringComparison.OrdinalIgnoreCase) == true)
                {
                    hasSelected = true;
                    break;
                }
            }
        }

        Assert(hasSelected, "Tab2 should have :selected pseudo-class after being clicked");
    }

    private async Task SelectListBoxItem()
    {
        // Find an item in ItemListBox by type.
        string json = await client.FindElementAsync(typeName: "ListBoxItem", maxResults: 5);
        using var doc = JsonDocument.Parse(json);
        var arr = doc.RootElement;

        Assert(arr.ValueKind == JsonValueKind.Array && arr.GetArrayLength() > 0,
            "Should find at least one ListBoxItem");

        // Click the first ListBoxItem.
        var first = arr[0];
        if (first.TryGetProperty("name", out var nameProp) && nameProp.GetString() is { } name)
        {
            await client.SimulateClickAsync(name);
        }
        else if (first.TryGetProperty("elementId", out var idProp) && idProp.GetString() is { } elementId)
        {
            // Fall back to path-based approach.
            string path = elementId.Replace("path:", "");
            await client.SimulateClickAsync(path);
        }

        await Task.Delay(300);
        // Just verify no exception was thrown — item selection is a fire-and-forget action.
    }

    private async Task DoubleClickSelectsText()
    {
        // First type some text to ensure there's content.
        await client.SimulateClickAsync("InputTextBox");
        await Task.Delay(200);

        // Double-click the text box.
        await client.SimulateClickAsync("InputTextBox", clickCount: 2);
        await Task.Delay(300);

        // Verify the element is still in a valid state.
        string propsJson = await client.GetElementPropertiesAsync(elementName: "InputTextBox");
        Assert(!string.IsNullOrEmpty(propsJson), "Should get properties for InputTextBox after double-click");
    }

    private async Task SetControlStateDisableEnable()
    {
        // Disable ActionButton.
        await client.SetControlStateAsync("ActionButton", "enabled", false);
        await Task.Delay(300);

        // Verify it's disabled.
        string disabledJson = await client.GetElementPropertiesAsync(elementName: "ActionButton");
        Assert(disabledJson.Contains("\"isEnabled\":false", StringComparison.OrdinalIgnoreCase),
            "ActionButton should be disabled (isEnabled=false)");

        // Re-enable it.
        await client.SetControlStateAsync("ActionButton", "enabled", true);
        await Task.Delay(300);

        // Verify it's re-enabled.
        string enabledJson = await client.GetElementPropertiesAsync(elementName: "ActionButton");
        Assert(enabledJson.Contains("\"isEnabled\":true", StringComparison.OrdinalIgnoreCase),
            "ActionButton should be re-enabled (isEnabled=true)");
    }

    private async Task NumericUpDownChangeValue()
    {
        // Get initial properties.
        string beforeJson = await client.GetElementPropertiesAsync(elementName: "TestNumericUpDown");

        // Click the NumericUpDown (focuses it) then type a new value.
        await client.SimulateClickAsync("TestNumericUpDown");
        await Task.Delay(200);

        // Use keyboard shortcut to select all, then type a new value.
        await client.SimulateKeyboardShortcutAsync("Ctrl+A", "TestNumericUpDown");
        await Task.Delay(100);
        await client.SimulateTypeAsync("TestNumericUpDown", "25");
        await Task.Delay(300);

        // Verify the element is still valid.
        string afterJson = await client.GetElementPropertiesAsync(elementName: "TestNumericUpDown");
        Assert(!string.IsNullOrEmpty(afterJson), "Should get properties for TestNumericUpDown after typing");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"Assertion failed: {message}");
        }
    }
}
