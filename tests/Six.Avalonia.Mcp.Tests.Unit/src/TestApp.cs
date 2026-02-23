using Avalonia;
using Avalonia.Headless;
using Avalonia.Themes.Fluent;

[assembly: AvaloniaTestApplication(typeof(Six.Avalonia.Mcp.Tests.Unit.TestAppBuilder))]

namespace Six.Avalonia.Mcp.Tests.Unit;

public class TestApp : Application
{
  public override void Initialize()
  {
    Styles.Add(new FluentTheme());
  }
}

public class TestAppBuilder
{
  public static AppBuilder BuildAvaloniaApp()
  {
    return AppBuilder.Configure<TestApp>()
      .UseHeadless(new AvaloniaHeadlessPlatformOptions());
  }
}