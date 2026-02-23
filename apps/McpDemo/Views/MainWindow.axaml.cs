using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Six.Avalonia.Mcp.App.ViewModels;

namespace Six.Avalonia.Mcp.App.Views;

public partial class MainWindow : Window
{
  public MainWindow()
  {
    InitializeComponent();
  }

  private void ActionButton_Click(object? sender, RoutedEventArgs e)
  {
    if (DataContext is MainWindowViewModel vm)
    {
      vm.OnActionButtonClick();
    }
  }

  private async void OpenDialogButton_Click(object? sender, RoutedEventArgs e)
  {
    if (DataContext is MainWindowViewModel vm)
    {
      var dialog = new TestDialog();
      var result = await dialog.ShowDialog<string?>(this);
      vm.DialogResult = result ?? "Cancelled";
    }
  }

  private void OpenWindowButton_Click(object? sender, RoutedEventArgs e)
  {
    var window = new Window
    {
      Title = "Secondary Window",
      Width = 400,
      Height = 300,
      Content = new TextBlock
      {
        Text = "This is a secondary window",
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center
      }
    };
    window.Show(this);
  }

  private void HoverTarget_PointerEntered(object? sender, PointerEventArgs e)
  {
    if (DataContext is MainWindowViewModel vm)
    {
      vm.HoverStatus = "Hovered";
    }
  }

  private void HoverTarget_PointerExited(object? sender, PointerEventArgs e)
  {
    if (DataContext is MainWindowViewModel vm)
    {
      vm.HoverStatus = "Not hovered";
    }
  }
}
