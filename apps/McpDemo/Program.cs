using Avalonia;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Six.Avalonia.Mcp.Connector;

namespace Six.Avalonia.Mcp.App;

sealed class Program
{
  [STAThread]
  public static void Main(string[] args)
  {
    using IHost host = CreateHostBuilder(args).Build();
    App.Services = host.Services;

    _ = host.StartAsync();

    BuildAvaloniaApp()
      .StartWithClassicDesktopLifetime(args);
  }

  public static IHostBuilder CreateHostBuilder(string[] args)
  {
    return Host.CreateDefaultBuilder(args)
      .AddAvaloniaMcpConnector(options =>
      {
        options.AppName = "Six Demo App";
        options.ScreenshotDirectory = "screenshots";
      })
      .ConfigureAppConfiguration(builder =>
      {
        builder
          .AddJsonFile("appSettings.json", true, true)
          .AddUserSecrets<Program>(true);
      })
      .ConfigureServices((context, services) =>
      {
        services.AddTransient<ViewModels.MainWindowViewModel>();
      });
  }

  public static AppBuilder BuildAvaloniaApp()
  {
    return AppBuilder.Configure<App>()
      .UsePlatformDetect()
      .WithInterFont()
      .LogToTrace();
  }
}
