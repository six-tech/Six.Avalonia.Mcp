using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Microsoft.Extensions.Logging;
using Six.Avalonia.Mcp.Contracts.Models;
using Six.Avalonia.Mcp.Connector.Services;

namespace Six.Avalonia.Mcp.Tests.Unit.Services;

public class ContextMenuTests
{
  private sealed class TestableInspector : AvaloniaInspector
  {
    private readonly Window _window;

    public TestableInspector(Window window, ILogger<AvaloniaInspector> logger) : base(logger)
    {
      _window = window;
    }

    protected override TopLevel? GetTopLevel() => _window;

    protected override IReadOnlyList<TopLevel> GetAllTopLevels() => [_window];
  }

  private static (TestableInspector Inspector, Window Window) CreateTestableInspector()
  {
    ILogger<AvaloniaInspector>? logger = Substitute.For<ILogger<AvaloniaInspector>>();
    Window window = new() { Title = "Test Window", Width = 800, Height = 600 };

    Button button = new() { Name = "TestButton" };
    StackPanel panel = new();
    panel.Children.Add(button);
    window.Content = panel;
    window.Show();

    return (new TestableInspector(window, logger), window);
  }

  [AvaloniaFact]
  public async Task RightClick_RaisesContextRequestedEvent()
  {
    (TestableInspector inspector, Window window) = CreateTestableInspector();

    bool contextRequested = false;
    StackPanel panel = (StackPanel)window.Content!;
    Button button = (Button)panel.Children[0];
    button.AddHandler(Control.ContextRequestedEvent, (_, _) => contextRequested = true);

    await inspector.SimulateClickAsync("TestButton", "right");

    contextRequested.Should().BeTrue("right-click should dispatch ContextRequestedEvent");

    window.Close();
  }

  [AvaloniaFact]
  public async Task LeftClick_DoesNotRaiseContextRequestedEvent()
  {
    (TestableInspector inspector, Window window) = CreateTestableInspector();

    bool contextRequested = false;
    StackPanel panel = (StackPanel)window.Content!;
    Button button = (Button)panel.Children[0];
    button.AddHandler(Control.ContextRequestedEvent, (_, _) => contextRequested = true);

    await inspector.SimulateClickAsync("TestButton", "left");

    contextRequested.Should().BeFalse("left-click should not dispatch ContextRequestedEvent");

    window.Close();
  }
}
