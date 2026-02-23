namespace Six.Avalonia.Mcp.TestRunner.Scenarios;

public class TextInputScenarios(McpToolClient client) : IScenarioGroup
{
    public IEnumerable<(string Name, Func<Task> Action)> GetScenarios()
    {
        yield return ("TypeText", TypeText);
        yield return ("TypeSpecialChars", TypeSpecialChars);
    }

    private async Task TypeText()
    {
        // Clear any existing text first by selecting all and deleting.
        await client.SimulateClickAsync("InputTextBox");
        await Task.Delay(200);
        await client.SimulateKeyboardShortcutAsync("Ctrl+A", elementName: "InputTextBox");
        await Task.Delay(100);
        await client.SimulateKeyboardShortcutAsync("Delete", elementName: "InputTextBox");
        await Task.Delay(200);

        await client.SimulateTypeAsync("InputTextBox", "Hello World");
        await Task.Delay(300);

        string mirrorJson = await client.GetElementPropertiesAsync(elementName: "InputMirrorText");
        Assert(mirrorJson.Contains("Hello World", StringComparison.OrdinalIgnoreCase),
            "InputMirrorText should contain 'Hello World' after typing");
    }

    private async Task TypeSpecialChars()
    {
        // Clear existing text.
        await client.SimulateClickAsync("InputTextBox");
        await Task.Delay(200);
        await client.SimulateKeyboardShortcutAsync("Ctrl+A", elementName: "InputTextBox");
        await Task.Delay(100);
        await client.SimulateKeyboardShortcutAsync("Delete", elementName: "InputTextBox");
        await Task.Delay(200);

        // Type "abc" then backspace (should result in "ab").
        await client.SimulateTypeAsync("InputTextBox", "abc\b");
        await Task.Delay(300);

        string mirrorJson = await client.GetElementPropertiesAsync(elementName: "InputMirrorText");

        // Verify text is "ab" (backspace removed last char).
        Assert(mirrorJson.Contains("ab", StringComparison.OrdinalIgnoreCase),
            "InputMirrorText should contain 'ab' after typing 'abc' + backspace");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"Assertion failed: {message}");
        }
    }
}
