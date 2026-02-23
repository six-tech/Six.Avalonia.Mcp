using System.Text.Json;

namespace Six.Avalonia.Mcp.TestRunner.Scenarios;

public class BasicInspectionScenarios(McpToolClient client) : IScenarioGroup
{
    public IEnumerable<(string Name, Func<Task> Action)> GetScenarios()
    {
        yield return ("GetMetadata", GetMetadata);
        yield return ("GetVisualTree", GetVisualTree);
        yield return ("FindElementByName", FindElementByName);
        yield return ("FindElementByType", FindElementByType);
        yield return ("GetElementProperties", GetElementProperties);
    }

    private async Task GetMetadata()
    {
        var metadata = await client.GetAppMetadataAsync();
        Assert(metadata is not null, "Metadata should not be null");
        Assert(!string.IsNullOrEmpty(metadata!.ProcessName), "ProcessName should not be empty");
        Assert(metadata.WindowWidth > 0, $"WindowWidth should be > 0 but was {metadata.WindowWidth}");
        Assert(metadata.WindowHeight > 0, $"WindowHeight should be > 0 but was {metadata.WindowHeight}");
    }

    private async Task GetVisualTree()
    {
        string json = await client.GetVisualTreeAsync(maxDepth: 3);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert(root.TryGetProperty("type", out _), "Visual tree root should have a 'type' property");
        Assert(root.TryGetProperty("children", out var children), "Visual tree root should have 'children'");
        Assert(children.GetArrayLength() > 0, "Visual tree root should have at least one child");
    }

    private async Task FindElementByName()
    {
        string json = await client.FindElementAsync(name: "ActionButton");
        using var doc = JsonDocument.Parse(json);
        var arr = doc.RootElement;

        Assert(arr.ValueKind == JsonValueKind.Array, "find_element should return an array");
        Assert(arr.GetArrayLength() > 0, "Should find element named 'ActionButton'");

        var first = arr[0];
        Assert(first.TryGetProperty("name", out var nameProp) && nameProp.GetString() == "ActionButton",
            "First result should have name 'ActionButton'");
    }

    private async Task FindElementByType()
    {
        string json = await client.FindElementAsync(typeName: "Button");
        using var doc = JsonDocument.Parse(json);
        var arr = doc.RootElement;

        Assert(arr.ValueKind == JsonValueKind.Array, "find_element should return an array");
        Assert(arr.GetArrayLength() > 0, "Should find at least one Button");
    }

    private async Task GetElementProperties()
    {
        string json = await client.GetElementPropertiesAsync(elementName: "ActionButton");
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert(root.TryGetProperty("type", out _), "Element properties should have 'type'");
        Assert(root.TryGetProperty("bounds", out var bounds), "Element properties should have 'bounds'");
        Assert(bounds.TryGetProperty("width", out var w) && w.GetDouble() > 0,
            "Bounds width should be > 0");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"Assertion failed: {message}");
        }
    }
}
