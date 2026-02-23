using System.Text.Json;

namespace Six.Avalonia.Mcp.TestRunner.Scenarios;

public class ScreenshotScenarios(McpToolClient client) : IScenarioGroup
{
    public IEnumerable<(string Name, Func<Task> Action)> GetScenarios()
    {
        yield return ("FullWindowScreenshot", FullWindowScreenshot);
        yield return ("ElementScreenshot", ElementScreenshot);
        yield return ("ScaledScreenshot", ScaledScreenshot);
    }

    private async Task FullWindowScreenshot()
    {
        string result = await client.TakeScreenshotAsync();
        AssertScreenshotSuccess(result);
    }

    private async Task ElementScreenshot()
    {
        string result = await client.TakeScreenshotAsync(elementName: "ActionButton");
        AssertScreenshotSuccess(result);
    }

    private async Task ScaledScreenshot()
    {
        string result = await client.TakeScreenshotAsync(scaleFactor: 2.0);
        AssertScreenshotSuccess(result);
    }

    private static void AssertScreenshotSuccess(string resultJson)
    {
        using var doc = JsonDocument.Parse(resultJson);
        var root = doc.RootElement;

        // take_screenshot returns {filePath, width, height} on success, {error} on failure.
        Assert(!root.TryGetProperty("error", out _), $"Screenshot failed: {resultJson}");
        Assert(root.TryGetProperty("filePath", out var fp) && !string.IsNullOrEmpty(fp.GetString()),
            "Screenshot file path should not be empty");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"Assertion failed: {message}");
        }
    }
}
