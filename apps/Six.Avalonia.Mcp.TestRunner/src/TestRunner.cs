using Six.Avalonia.Mcp.TestRunner.Scenarios;

namespace Six.Avalonia.Mcp.TestRunner;

public record ScenarioResult(string Name, bool Passed, string? Error = null);

public class TestRunner(McpToolClient client)
{
    private readonly List<ScenarioResult> _results = [];

    public async Task RunAllAsync()
    {
        await RunGroupAsync("Basic Inspection", new BasicInspectionScenarios(client));
        await RunGroupAsync("Button Clicks", new ButtonClickScenarios(client));
        await RunGroupAsync("Multi-Window", new MultiWindowScenarios(client));
        await RunGroupAsync("Context Menu", new ContextMenuScenarios(client));
        await RunGroupAsync("Screenshots", new ScreenshotScenarios(client));
        await RunGroupAsync("Text Input", new TextInputScenarios(client));
        await RunGroupAsync("Drag & Gestures", new DragAndGestureScenarios(client));
        await RunGroupAsync("Scrolling", new ScrollScenarios(client));
        await RunGroupAsync("Advanced Interactions", new AdvancedInteractionScenarios(client));
        await RunGroupAsync("Popup Screenshots", new PopupScreenshotScenarios(client));
    }

    private async Task RunGroupAsync(string groupName, IScenarioGroup group)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"=== {groupName} ===");
        Console.ResetColor();

        foreach (var (name, action) in group.GetScenarios())
        {
            try
            {
                await action();
                _results.Add(new ScenarioResult(name, true));
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  PASS  {name}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                _results.Add(new ScenarioResult(name, false, ex.Message));
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  FAIL  {name}");
                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"        {ex.Message}");
                Console.ResetColor();
            }
        }
    }

    public void PrintSummary()
    {
        int passed = _results.Count(r => r.Passed);
        int failed = _results.Count(r => !r.Passed);

        Console.WriteLine();
        Console.WriteLine(new string('=', 50));
        Console.ForegroundColor = passed == _results.Count ? ConsoleColor.Green : ConsoleColor.Yellow;
        Console.WriteLine($"Results: {passed} passed, {failed} failed, {_results.Count} total");
        Console.ResetColor();

        if (failed > 0)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Failed scenarios:");
            foreach (var result in _results.Where(r => !r.Passed))
            {
                Console.WriteLine($"  - {result.Name}: {result.Error}");
            }

            Console.ResetColor();
        }
    }

    public bool AllPassed => _results.All(r => r.Passed);
}
