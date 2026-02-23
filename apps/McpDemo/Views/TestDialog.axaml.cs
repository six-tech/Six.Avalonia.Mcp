using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Six.Avalonia.Mcp.App.Views;

public partial class TestDialog : Window
{
  public TestDialog()
  {
    InitializeComponent();
  }

  protected override void OnKeyDown(KeyEventArgs e)
  {
    base.OnKeyDown(e);
    if(e.Key == Key.Escape)
    {
      Close(null);
      e.Handled = true;
    }
  }

  private void OkButton_Click(object? sender, RoutedEventArgs e)
  {
    Close(DialogInput.Text ?? "OK");
  }

  private void CancelButton_Click(object? sender, RoutedEventArgs e)
  {
    Close(null);
  }
}
