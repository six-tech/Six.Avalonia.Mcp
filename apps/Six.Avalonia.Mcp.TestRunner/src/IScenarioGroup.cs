namespace Six.Avalonia.Mcp.TestRunner;

public interface IScenarioGroup
{
    IEnumerable<(string Name, Func<Task> Action)> GetScenarios();
}
