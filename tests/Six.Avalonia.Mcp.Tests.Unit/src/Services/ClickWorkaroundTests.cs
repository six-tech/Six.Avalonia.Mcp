using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Microsoft.Extensions.Logging;
using Six.Avalonia.Mcp.Contracts.Models;
using Six.Avalonia.Mcp.Connector.Services;

namespace Six.Avalonia.Mcp.Tests.Unit.Services;

public class ClickWorkaroundTests
{
  public class ApplyControlSpecificClickBehaviorTests
  {
    [AvaloniaFact]
    public void Button_RaisesClickEvent()
    {
      Button button = new() { Name = "test" };
      bool clicked = false;
      button.Click += (_, _) => clicked = true;

      AvaloniaInspector.ApplyControlSpecificClickBehavior(button, MouseButton.Left);

      clicked.Should().BeTrue();
    }

    [AvaloniaFact]
    public void Button_ExecutesCommand()
    {
      Button button = new() { Name = "test" };
      bool executed = false;
      button.Command = new TestCommand(() => executed = true);

      AvaloniaInspector.ApplyControlSpecificClickBehavior(button, MouseButton.Left);

      executed.Should().BeTrue();
    }

    [AvaloniaFact]
    public void Button_RightClick_DoesNothing()
    {
      Button button = new() { Name = "test" };
      bool clicked = false;
      button.Click += (_, _) => clicked = true;

      AvaloniaInspector.ApplyControlSpecificClickBehavior(button, MouseButton.Right);

      clicked.Should().BeFalse();
    }

    [AvaloniaFact]
    public void CheckBox_TogglesIsChecked()
    {
      CheckBox checkBox = new() { Name = "test", IsChecked = false };

      AvaloniaInspector.ApplyControlSpecificClickBehavior(checkBox, MouseButton.Left);

      checkBox.IsChecked.Should().BeTrue();
    }

    [AvaloniaFact]
    public void CheckBox_TogglesBack()
    {
      CheckBox checkBox = new() { Name = "test", IsChecked = true };

      AvaloniaInspector.ApplyControlSpecificClickBehavior(checkBox, MouseButton.Left);

      checkBox.IsChecked.Should().BeFalse();
    }

    [AvaloniaFact]
    public void RadioButton_SetsIsCheckedTrue()
    {
      RadioButton radio = new() { Name = "test", IsChecked = false };

      AvaloniaInspector.ApplyControlSpecificClickBehavior(radio, MouseButton.Left);

      radio.IsChecked.Should().BeTrue();
    }

    [AvaloniaFact]
    public void ToggleButton_TogglesIsChecked()
    {
      ToggleButton toggle = new() { Name = "test", IsChecked = false };

      AvaloniaInspector.ApplyControlSpecificClickBehavior(toggle, MouseButton.Left);

      toggle.IsChecked.Should().BeTrue();
    }

    [AvaloniaFact]
    public void Expander_TogglesIsExpanded()
    {
      Expander expander = new() { Name = "test", IsExpanded = false };

      AvaloniaInspector.ApplyControlSpecificClickBehavior(expander, MouseButton.Left);

      expander.IsExpanded.Should().BeTrue();
    }

    [AvaloniaFact]
    public void Expander_CollapsesWhenExpanded()
    {
      Expander expander = new() { Name = "test", IsExpanded = true };

      AvaloniaInspector.ApplyControlSpecificClickBehavior(expander, MouseButton.Left);

      expander.IsExpanded.Should().BeFalse();
    }

    [AvaloniaFact]
    public void ListBoxItem_SetsIsSelectedTrue()
    {
      ListBoxItem item = new() { Name = "test" };

      AvaloniaInspector.ApplyControlSpecificClickBehavior(item, MouseButton.Left);

      item.IsSelected.Should().BeTrue();
    }

    [AvaloniaFact]
    public void TreeViewItem_SetsIsSelectedTrue()
    {
      TreeViewItem item = new() { Name = "test" };

      AvaloniaInspector.ApplyControlSpecificClickBehavior(item, MouseButton.Left);

      item.IsSelected.Should().BeTrue();
    }

    [AvaloniaFact]
    public void ComboBoxItem_SetsIsSelectedTrue()
    {
      ComboBoxItem item = new() { Name = "test" };

      AvaloniaInspector.ApplyControlSpecificClickBehavior(item, MouseButton.Left);

      item.IsSelected.Should().BeTrue();
    }

    [AvaloniaFact]
    public void TabItem_SetsIsSelectedTrue()
    {
      TabItem item = new() { Name = "test" };

      AvaloniaInspector.ApplyControlSpecificClickBehavior(item, MouseButton.Left);

      item.IsSelected.Should().BeTrue();
    }

    [AvaloniaFact]
    public void MenuItem_RaisesClickEvent()
    {
      MenuItem menuItem = new() { Name = "test" };
      bool clicked = false;
      menuItem.Click += (_, _) => clicked = true;

      AvaloniaInspector.ApplyControlSpecificClickBehavior(menuItem, MouseButton.Left);

      clicked.Should().BeTrue();
    }

    [AvaloniaFact]
    public void MenuItem_ExecutesCommand()
    {
      MenuItem menuItem = new() { Name = "test" };
      bool executed = false;
      menuItem.Command = new TestCommand(() => executed = true);

      AvaloniaInspector.ApplyControlSpecificClickBehavior(menuItem, MouseButton.Left);

      executed.Should().BeTrue();
    }

    [AvaloniaFact]
    public void UnhandledControl_DoesNotThrow()
    {
      TextBlock textBlock = new() { Name = "test" };

      Action act = () => AvaloniaInspector.ApplyControlSpecificClickBehavior(textBlock, MouseButton.Left);

      act.Should().NotThrow();
    }
  }
}

file sealed class TestCommand : System.Windows.Input.ICommand
{
  private readonly Action _execute;

  public TestCommand(Action execute) => _execute = execute;

  public bool CanExecute(object? parameter) => true;
  public void Execute(object? parameter) => _execute();
#pragma warning disable CS0067
  public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067
}
