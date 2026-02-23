using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Six.Avalonia.Mcp.App.ViewModels;
using Six.Avalonia.Mcp.App.Views;

namespace Six.Avalonia.Mcp.App;

public partial class App : Application
{
  public static IServiceProvider? Services { get; set; }

  public override void Initialize()
  {
    AvaloniaXamlLoader.Load(this);
  }

  public override void OnFrameworkInitializationCompleted()
  {
    if(ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
      MainWindowViewModel vm = Services?.GetRequiredService<MainWindowViewModel>() ?? new MainWindowViewModel();
      desktop.MainWindow = new MainWindow { DataContext = vm };
    }

    base.OnFrameworkInitializationCompleted();
  }
}