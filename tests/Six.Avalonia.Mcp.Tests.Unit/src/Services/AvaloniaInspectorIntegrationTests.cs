using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Microsoft.Extensions.Logging;
using Six.Avalonia.Mcp.Contracts.Models;
using Six.Avalonia.Mcp.Connector.Services;

namespace Six.Avalonia.Mcp.Tests.Unit.Services;

/// <summary>
/// Integration tests for AvaloniaInspector async methods using a headless test window.
/// These tests exercise the full Dispatcher.UIThread.InvokeAsync code paths
/// that can't be reached in pure unit tests (where GetTopLevel returns null).
/// </summary>
public class AvaloniaInspectorIntegrationTests
{
  /// <summary>
  /// Test subclass that overrides GetTopLevel and GetAllTopLevels to return a controlled test window.
  /// </summary>
  private sealed class TestableInspector : AvaloniaInspector
  {
    private readonly Window _window;

    public TestableInspector(Window window, ILogger<AvaloniaInspector> logger) : base(logger)
    {
      _window = window;
    }

    protected override TopLevel? GetTopLevel()
    {
      return _window;
    }

    protected override IReadOnlyList<TopLevel> GetAllTopLevels()
    {
      return [_window];
    }
  }

  private static (TestableInspector Inspector, Window Window) CreateTestableInspector()
  {
    ILogger<AvaloniaInspector>? logger = Substitute.For<ILogger<AvaloniaInspector>>();
    Window window = new() { Title = "Test Window", Width = 800, Height = 600 };

    TextBox textBox = new() { Name = "InputTextBox" };
    Button button = new() { Name = "ActionButton", Content = "Click Me" };
    CheckBox checkBox = new() { Name = "ToggleCheckBox" };
    Slider slider = new() { Name = "ValueSlider", Minimum = 0, Maximum = 100 };
    TextBlock statusText = new() { Name = "StatusText", Text = "Ready" };

    StackPanel panel = new();
    panel.Children.Add(textBox);
    panel.Children.Add(button);
    panel.Children.Add(checkBox);
    panel.Children.Add(slider);
    panel.Children.Add(statusText);

    window.Content = panel;

    // Show window to trigger layout (headless mode)
    window.Show();

    TestableInspector inspector = new(window, logger);
    return (inspector, window);
  }

  public class GetAppMetadataAsyncIntegrationTests : AvaloniaInspectorIntegrationTests
  {
    [AvaloniaFact]
    public async Task GetAppMetadataAsync_ReturnsProcessInfo()
    {
      (TestableInspector inspector, Window window) = CreateTestableInspector();

      AppMetadata metadata = await inspector.GetAppMetadataAsync();

      metadata.ProcessName.Should().NotBeNullOrEmpty();
      metadata.ProcessId.Should().BeGreaterThan(0);
      metadata.MainWindowTitle.Should().Be("Test Window");
      metadata.AvaloniaVersion.Should().NotBeNullOrEmpty();
      metadata.DotNetVersion.Should().NotBeNullOrEmpty();
      metadata.WindowCount.Should().Be(1);

      window.Close();
    }
  }

  public class GetVisualTreeAsyncIntegrationTests : AvaloniaInspectorIntegrationTests
  {
    [AvaloniaFact]
    public async Task GetVisualTreeAsync_ReturnsTree()
    {
      (TestableInspector inspector, Window window) = CreateTestableInspector();

      VisualTreeNode tree = await inspector.GetVisualTreeAsync(5);

      tree.Type.Should().Be("Window");
      tree.Children.Should().NotBeEmpty();

      window.Close();
    }

    [AvaloniaFact]
    public async Task GetVisualTreeAsync_WithElementName_ReturnsSubtree()
    {
      (TestableInspector inspector, Window window) = CreateTestableInspector();

      VisualTreeNode tree = await inspector.GetVisualTreeAsync(3, "ActionButton");

      tree.Type.Should().Be("Button");
      tree.Name.Should().Be("ActionButton");

      window.Close();
    }

    [AvaloniaFact]
    public async Task GetVisualTreeAsync_WithInvalidElementName_Throws()
    {
      (TestableInspector inspector, Window window) = CreateTestableInspector();

      Func<Task<VisualTreeNode>> act = () => inspector.GetVisualTreeAsync(3, "NonExistent");

      await act.Should().ThrowAsync<InvalidOperationException>()
        .WithMessage("*NonExistent*not found*");

      window.Close();
    }
  }

  public class FindElementAsyncIntegrationTests : AvaloniaInspectorIntegrationTests
  {
    [AvaloniaFact]
    public async Task FindElementAsync_ByName_FindsElement()
    {
      (TestableInspector inspector, Window window) = CreateTestableInspector();

      IReadOnlyList<FindElementResult> results = await inspector.FindElementAsync("ActionButton");

      results.Should().ContainSingle()
        .Which.Name.Should().Be("ActionButton");

      window.Close();
    }

    [AvaloniaFact]
    public async Task FindElementAsync_ByTypeName_FindsElements()
    {
      (TestableInspector inspector, Window window) = CreateTestableInspector();

      IReadOnlyList<FindElementResult> results = await inspector.FindElementAsync(typeName: "TextBox");

      results.Should().HaveCountGreaterThanOrEqualTo(1);
      results.Should().Contain(r => r.Type == "TextBox");

      window.Close();
    }

    [AvaloniaFact]
    public async Task FindElementAsync_BySelector_FindsElement()
    {
      (TestableInspector inspector, Window window) = CreateTestableInspector();

      IReadOnlyList<FindElementResult> results = await inspector.FindElementAsync(selector: "#ActionButton");

      results.Should().ContainSingle()
        .Which.Name.Should().Be("ActionButton");

      window.Close();
    }

    [AvaloniaFact]
    public async Task FindElementAsync_ReturnsPathAndElementId()
    {
      (TestableInspector inspector, Window window) = CreateTestableInspector();

      IReadOnlyList<FindElementResult> results = await inspector.FindElementAsync("ActionButton");

      results.Should().ContainSingle();
      FindElementResult result = results[0];
      result.ElementId.Should().StartWith("path:");
      result.Path.Should().NotBeNull();

      window.Close();
    }
  }

  public class GetAncestorChainAsyncIntegrationTests : AvaloniaInspectorIntegrationTests
  {
    [AvaloniaFact]
    public async Task GetAncestorChainAsync_ReturnsChainToRoot()
    {
      (TestableInspector inspector, Window window) = CreateTestableInspector();

      IReadOnlyList<VisualTreeNode> chain = await inspector.GetAncestorChainAsync("ActionButton");

      chain.Should().HaveCountGreaterThanOrEqualTo(2);
      chain[0].Type.Should().Be("Button");
      chain[0].Name.Should().Be("ActionButton");
      // Last ancestor should be the window
      chain.Last().Type.Should().Be("Window");

      window.Close();
    }

    [AvaloniaFact]
    public async Task GetAncestorChainAsync_ThrowsForMissingElement()
    {
      (TestableInspector inspector, Window window) = CreateTestableInspector();

      Func<Task<IReadOnlyList<VisualTreeNode>>> act = () => inspector.GetAncestorChainAsync("NonExistent");

      await act.Should().ThrowAsync<InvalidOperationException>()
        .WithMessage("*not found*");

      window.Close();
    }
  }

  public class GetElementPropertiesAsyncIntegrationTests : AvaloniaInspectorIntegrationTests
  {
    [AvaloniaFact]
    public async Task GetElementPropertiesAsync_ByName_ReturnsProperties()
    {
      (TestableInspector inspector, Window window) = CreateTestableInspector();

      ElementProperties props = await inspector.GetElementPropertiesAsync("ActionButton");

      props.Type.Should().Be("Button");
      props.Name.Should().Be("ActionButton");
      props.IsVisible.Should().BeTrue();
      props.IsEnabled.Should().BeTrue();
      props.ElementId.Should().StartWith("path:");

      window.Close();
    }

    [AvaloniaFact]
    public async Task GetElementPropertiesAsync_ByTypeName_ReturnsProperties()
    {
      (TestableInspector inspector, Window window) = CreateTestableInspector();

      ElementProperties props = await inspector.GetElementPropertiesAsync(typeName: "Slider");

      props.Type.Should().Be("Slider");
      props.Name.Should().Be("ValueSlider");

      window.Close();
    }

    [AvaloniaFact]
    public async Task GetElementPropertiesAsync_ThrowsForMissingElement()
    {
      (TestableInspector inspector, Window window) = CreateTestableInspector();

      Func<Task<ElementProperties>> act = () => inspector.GetElementPropertiesAsync("NonExistent");

      await act.Should().ThrowAsync<InvalidOperationException>();

      window.Close();
    }
  }

  public class SimulateClickAsyncIntegrationTests : AvaloniaInspectorIntegrationTests
  {
    [AvaloniaFact]
    public async Task SimulateClickAsync_LeftClick_ReturnsSuccess()
    {
      (TestableInspector inspector, Window window) = CreateTestableInspector();

      InteractionResult result = await inspector.SimulateClickAsync("ActionButton");

      result.Success.Should().BeTrue();
      result.ElementType.Should().Be("Button");
      result.ElementName.Should().Be("ActionButton");

      window.Close();
    }

    [AvaloniaFact]
    public async Task SimulateClickAsync_RightClick_ReturnsSuccess()
    {
      (TestableInspector inspector, Window window) = CreateTestableInspector();

      InteractionResult result = await inspector.SimulateClickAsync("ActionButton", "right");

      result.Success.Should().BeTrue();

      window.Close();
    }

    [AvaloniaFact]
    public async Task SimulateClickAsync_WithModifiers_ReturnsSuccess()
    {
      (TestableInspector inspector, Window window) = CreateTestableInspector();

      InteractionResult result = await inspector.SimulateClickAsync("ActionButton", modifiers: "ctrl+shift");

      result.Success.Should().BeTrue();

      window.Close();
    }

    [AvaloniaFact]
    public async Task SimulateClickAsync_WithCoordinates_ReturnsSuccess()
    {
      (TestableInspector inspector, Window window) = CreateTestableInspector();

      InteractionResult result = await inspector.SimulateClickAsync("ActionButton", x: 5, y: 5);

      result.Success.Should().BeTrue();

      window.Close();
    }

    [AvaloniaFact]
    public async Task SimulateClickAsync_ElementNotFound_Throws()
    {
      (TestableInspector inspector, Window window) = CreateTestableInspector();

      Func<Task<InteractionResult>> act = () => inspector.SimulateClickAsync("NonExistent");

      await act.Should().ThrowAsync<InvalidOperationException>()
        .WithMessage("*not found*");

      window.Close();
    }

    [AvaloniaFact]
    public async Task SimulateClickAsync_Button_FiresClickEvent()
    {
      (TestableInspector inspector, Window window) = CreateTestableInspector();

      bool clickFired = false;
      // Access button from panel children (FindControl requires name scope which headless tests lack)
      StackPanel panel = (StackPanel)window.Content!;
      Button button = (Button)panel.Children[1];
      button.Click += (_, _) => clickFired = true;

      await inspector.SimulateClickAsync("ActionButton");

      clickFired.Should().BeTrue("Button.Click should fire via the ClickEvent fallback");

      window.Close();
    }

    [AvaloniaFact]
    public async Task SimulateClickAsync_Button_InvokesCommand()
    {
      (TestableInspector inspector, Window window) = CreateTestableInspector();

      bool commandExecuted = false;
      StackPanel panel = (StackPanel)window.Content!;
      Button button = (Button)panel.Children[1];
      button.Command = new TestCommand(() => commandExecuted = true);

      await inspector.SimulateClickAsync("ActionButton");

      commandExecuted.Should().BeTrue("Button.Command should be invoked by simulate_click");

      window.Close();
    }

    [AvaloniaFact]
    public async Task SimulateClickAsync_NonButton_DoesNotThrow()
    {
      (TestableInspector inspector, Window window) = CreateTestableInspector();

      InteractionResult result = await inspector.SimulateClickAsync("InputTextBox");

      result.Success.Should().BeTrue();
      result.ElementType.Should().Be("TextBox");

      window.Close();
    }
  }

  public class SimulateTypeAsyncIntegrationTests : AvaloniaInspectorIntegrationTests
  {
    [AvaloniaFact]
    public async Task SimulateTypeAsync_RegularText_ReturnsSuccess()
    {
      (TestableInspector inspector, Window window) = CreateTestableInspector();

      InteractionResult result = await inspector.SimulateTypeAsync("InputTextBox", "Hello");

      result.Success.Should().BeTrue();
      result.ElementType.Should().Be("TextBox");

      window.Close();
    }

    [AvaloniaFact]
    public async Task SimulateTypeAsync_SpecialChars_ReturnsSuccess()
    {
      (TestableInspector inspector, Window window) = CreateTestableInspector();

      // Test backspace, newline, and tab characters
      InteractionResult result = await inspector.SimulateTypeAsync("InputTextBox", "A\b\n\t");

      result.Success.Should().BeTrue();

      window.Close();
    }

    [AvaloniaFact]
    public async Task SimulateTypeAsync_ElementNotFound_Throws()
    {
      (TestableInspector inspector, Window window) = CreateTestableInspector();

      Func<Task<InteractionResult>> act = () => inspector.SimulateTypeAsync("NonExistent", "text");

      await act.Should().ThrowAsync<InvalidOperationException>()
        .WithMessage("*not found*");

      window.Close();
    }
  }

  public class SetControlStateAsyncIntegrationTests : AvaloniaInspectorIntegrationTests
  {
    [AvaloniaFact]
    public async Task SetControlStateAsync_Focus_ReturnsSuccess()
    {
      (TestableInspector inspector, Window window) = CreateTestableInspector();

      InteractionResult result = await inspector.SetControlStateAsync("InputTextBox", "focus", true);

      result.Success.Should().BeTrue();

      window.Close();
    }

    [AvaloniaFact]
    public async Task SetControlStateAsync_Blur_ReturnsSuccess()
    {
      (TestableInspector inspector, Window window) = CreateTestableInspector();

      InteractionResult result = await inspector.SetControlStateAsync("InputTextBox", "focus", false);

      result.Success.Should().BeTrue();

      window.Close();
    }

    [AvaloniaFact]
    public async Task SetControlStateAsync_Enabled_ReturnsSuccess()
    {
      (TestableInspector inspector, Window window) = CreateTestableInspector();

      InteractionResult result = await inspector.SetControlStateAsync("ActionButton", "enabled", false);

      result.Success.Should().BeTrue();
      result.IsEnabled.Should().BeFalse();

      window.Close();
    }

    [AvaloniaFact]
    public async Task SetControlStateAsync_Visible_ReturnsSuccess()
    {
      (TestableInspector inspector, Window window) = CreateTestableInspector();

      InteractionResult result = await inspector.SetControlStateAsync("ActionButton", "visible", false);

      result.Success.Should().BeTrue();
      result.IsVisible.Should().BeFalse();

      window.Close();
    }

    [AvaloniaFact]
    public async Task SetControlStateAsync_UnknownState_Throws()
    {
      (TestableInspector inspector, Window window) = CreateTestableInspector();

      Func<Task<InteractionResult>> act = () => inspector.SetControlStateAsync("ActionButton", "unknown", true);

      await act.Should().ThrowAsync<ArgumentException>()
        .WithMessage("*Unknown state type*");

      window.Close();
    }

    [AvaloniaFact]
    public async Task SetControlStateAsync_ElementNotFound_Throws()
    {
      (TestableInspector inspector, Window window) = CreateTestableInspector();

      Func<Task<InteractionResult>> act = () => inspector.SetControlStateAsync("NonExistent", "focus", true);

      await act.Should().ThrowAsync<InvalidOperationException>();

      window.Close();
    }
  }

  public class SimulateHoverAsyncIntegrationTests : AvaloniaInspectorIntegrationTests
  {
    [AvaloniaFact]
    public async Task SimulateHoverAsync_EnterOnly_ReturnsSuccess()
    {
      (TestableInspector inspector, Window window) = CreateTestableInspector();

      InteractionResult result = await inspector.SimulateHoverAsync("ActionButton");

      result.Success.Should().BeTrue();
      result.ElementType.Should().Be("Button");

      window.Close();
    }

    [AvaloniaFact]
    public async Task SimulateHoverAsync_WithExit_ReturnsSuccess()
    {
      (TestableInspector inspector, Window window) = CreateTestableInspector();

      InteractionResult result = await inspector.SimulateHoverAsync("ActionButton", true);

      result.Success.Should().BeTrue();

      window.Close();
    }

    [AvaloniaFact]
    public async Task SimulateHoverAsync_ElementNotFound_Throws()
    {
      (TestableInspector inspector, Window window) = CreateTestableInspector();

      Func<Task<InteractionResult>> act = () => inspector.SimulateHoverAsync("NonExistent");

      await act.Should().ThrowAsync<InvalidOperationException>();

      window.Close();
    }
  }

  public class SimulateDragAsyncIntegrationTests : AvaloniaInspectorIntegrationTests
  {
    [AvaloniaFact]
    public async Task SimulateDragAsync_LeftDrag_ReturnsSuccess()
    {
      (TestableInspector inspector, Window window) = CreateTestableInspector();

      InteractionResult result = await inspector.SimulateDragAsync("ValueSlider", 10, 10, 90, 10);

      result.Success.Should().BeTrue();
      result.ElementType.Should().Be("Slider");

      window.Close();
    }

    [AvaloniaFact]
    public async Task SimulateDragAsync_RightButtonDrag_ReturnsSuccess()
    {
      (TestableInspector inspector, Window window) = CreateTestableInspector();

      InteractionResult result = await inspector.SimulateDragAsync("ValueSlider", 10, 10, 90, 10, "right", 5);

      result.Success.Should().BeTrue();

      window.Close();
    }

    [AvaloniaFact]
    public async Task SimulateDragAsync_ElementNotFound_Throws()
    {
      (TestableInspector inspector, Window window) = CreateTestableInspector();

      Func<Task<InteractionResult>> act = () => inspector.SimulateDragAsync("NonExistent", 0, 0, 100, 100);

      await act.Should().ThrowAsync<InvalidOperationException>();

      window.Close();
    }
  }

  public class SimulateKeyboardShortcutAsyncIntegrationTests : AvaloniaInspectorIntegrationTests
  {
    [AvaloniaFact]
    public async Task SimulateKeyboardShortcutAsync_WithElement_ReturnsSuccess()
    {
      (TestableInspector inspector, Window window) = CreateTestableInspector();

      InteractionResult result = await inspector.SimulateKeyboardShortcutAsync("Ctrl+A", "InputTextBox");

      result.Success.Should().BeTrue();

      window.Close();
    }

    [AvaloniaFact]
    public async Task SimulateKeyboardShortcutAsync_WithoutElement_UsesWindow()
    {
      (TestableInspector inspector, Window window) = CreateTestableInspector();

      InteractionResult result = await inspector.SimulateKeyboardShortcutAsync("Ctrl+S");

      result.Success.Should().BeTrue();

      window.Close();
    }

    [AvaloniaFact]
    public async Task SimulateKeyboardShortcutAsync_ElementNotFound_Throws()
    {
      (TestableInspector inspector, Window window) = CreateTestableInspector();

      Func<Task<InteractionResult>> act = () => inspector.SimulateKeyboardShortcutAsync("Ctrl+A", "NonExistent");

      await act.Should().ThrowAsync<InvalidOperationException>();

      window.Close();
    }

    [AvaloniaFact]
    public void SimulateKeyboardShortcutAsync_EscapeOnChild_BubblesToWindowOnKeyDown()
    {
      bool onKeyDownCalled = false;

      var window = new EscapeTestWindow(() => { onKeyDownCalled = true; });
      window.Show();

      // Focus the TextBox (simulates what happens when a dialog opens).
      var textBox = window.GetVisualDescendants().OfType<TextBox>().First();
      textBox.Focus();

      // Raise KeyDown Escape on the TextBox — should bubble to Window.OnKeyDown.
      textBox.RaiseEvent(new KeyEventArgs
      {
        RoutedEvent = InputElement.KeyDownEvent, Source = textBox, Key = Key.Escape,
      });

      onKeyDownCalled.Should().BeTrue("RaiseEvent on child should bubble to Window.OnKeyDown");

      window.Close();
    }

    [AvaloniaFact]
    public void SimulateKeyboardShortcutAsync_EscapeOnWindow_TriggersOnKeyDown()
    {
      bool onKeyDownCalled = false;

      var window = new EscapeTestWindow(() => { onKeyDownCalled = true; });
      window.Show();

      // Raise KeyDown Escape directly on the Window.
      window.RaiseEvent(new KeyEventArgs
      {
        RoutedEvent = InputElement.KeyDownEvent, Source = window, Key = Key.Escape,
      });

      onKeyDownCalled.Should().BeTrue("RaiseEvent directly on Window should trigger OnKeyDown");

      window.Close();
    }

    private sealed class EscapeTestWindow : Window
    {
      private readonly Action _callback;

      public EscapeTestWindow(Action callback)
      {
        _callback = callback;
        Width = 400;
        Height = 300;
        Content = new StackPanel
        {
          Children = { new TextBox { Name = "TestInput" } }
        };
      }

      protected override void OnKeyDown(KeyEventArgs e)
      {
        base.OnKeyDown(e);
        if (e.Key == Key.Escape)
        {
          _callback();
          e.Handled = true;
        }
      }
    }
  }

  public class TakeScreenshotAsyncIntegrationTests : AvaloniaInspectorIntegrationTests
  {
    [AvaloniaFact]
    public async Task TakeScreenshotAsync_Window_SavesFileAndReturnsResult()
    {
      (TestableInspector inspector, Window window) = CreateTestableInspector();
      string outputPath = Path.Combine(Path.GetTempPath(), "rayx-test", $"test-{Guid.NewGuid()}.png");

      try
      {
        ScreenshotResult result = await inspector.TakeScreenshotAsync(outputPath);

        // In headless mode, RenderTargetBitmap produces empty output but the file should still be created
        result.Success.Should().BeTrue();
        result.FilePath.Should().Be(outputPath);
        result.Width.Should().BeGreaterThan(0);
        result.Height.Should().BeGreaterThan(0);
        File.Exists(outputPath).Should().BeTrue();
      }
      finally
      {
        if(File.Exists(outputPath)) File.Delete(outputPath);
        window.Close();
      }
    }

    [AvaloniaFact]
    public async Task TakeScreenshotAsync_SpecificElement_SavesFileAndReturnsResult()
    {
      (TestableInspector inspector, Window window) = CreateTestableInspector();
      string outputPath = Path.Combine(Path.GetTempPath(), "rayx-test", $"test-{Guid.NewGuid()}.png");

      try
      {
        ScreenshotResult result = await inspector.TakeScreenshotAsync(outputPath, "ActionButton");

        result.Success.Should().BeTrue();
        result.FilePath.Should().Be(outputPath);
        File.Exists(outputPath).Should().BeTrue();
      }
      finally
      {
        if(File.Exists(outputPath)) File.Delete(outputPath);
        window.Close();
      }
    }

    [AvaloniaFact]
    public async Task TakeScreenshotAsync_ElementNotFound_Throws()
    {
      (TestableInspector inspector, Window window) = CreateTestableInspector();
      string outputPath = Path.Combine(Path.GetTempPath(), "rayx-test", $"test-{Guid.NewGuid()}.png");

      Func<Task<ScreenshotResult>> act = () => inspector.TakeScreenshotAsync(outputPath, "NonExistent");

      await act.Should().ThrowAsync<InvalidOperationException>();

      window.Close();
    }

    [AvaloniaFact]
    public async Task TakeScreenshotAsync_CreatesDirectoryIfNotExists()
    {
      (TestableInspector inspector, Window window) = CreateTestableInspector();
      string dir = Path.Combine(Path.GetTempPath(), "rayx-test", $"nested-{Guid.NewGuid()}");
      string outputPath = Path.Combine(dir, "screenshot.png");

      try
      {
        ScreenshotResult result = await inspector.TakeScreenshotAsync(outputPath);

        result.Success.Should().BeTrue();
        Directory.Exists(dir).Should().BeTrue();
        File.Exists(outputPath).Should().BeTrue();
      }
      finally
      {
        if(Directory.Exists(dir)) Directory.Delete(dir, true);
        window.Close();
      }
    }
  }
}

/// <summary>
/// Minimal ICommand implementation for testing Button.Command invocation.
/// </summary>
file sealed class TestCommand : ICommand
{
  private readonly Action _execute;

  public TestCommand(Action execute) => _execute = execute;

  public bool CanExecute(object? parameter) => true;
  public void Execute(object? parameter) => _execute();
#pragma warning disable CS0067 // Required by ICommand interface
  public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067
}