using Six.Avalonia.Mcp.Contracts;
using Six.Avalonia.Mcp.Contracts.Messages;
using Six.Avalonia.Mcp.Contracts.Models;
using Six.Avalonia.Mcp.Server.Services;
using Six.Avalonia.Mcp.Server.Tools;

namespace Six.Avalonia.Mcp.Server.Tests.Unit.Tools;

public class ScreenshotToolsTests
{
  private readonly IAppRegistry _appRegistry = Substitute.For<IAppRegistry>();

  [Fact]
  public async Task TakeScreenshot_ReturnsFilePath_OnSuccess()
  {
    IInspectorProxy proxy = Substitute.For<IInspectorProxy>();
    ScreenshotResult screenshotResult = new() { Success = true, FilePath = "/tmp/test.png", Width = 800, Height = 600 };
    proxy.SendCommandAsync(InspectorMethod.TakeScreenshot, Arg.Any<Dictionary<string, object?>?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
      .Returns(new InspectorResponse
      {
        RequestId = "test",
        Success = true,
        ResultJson = JsonSerializer.Serialize(screenshotResult, JsonOptions.Default)
      });

    string result = await ScreenshotTools.TakeScreenshot(proxy, _appRegistry, "/tmp/test.png");

    JsonDocument doc = JsonDocument.Parse(result);
    doc.RootElement.GetProperty("filePath").GetString().Should().Be("/tmp/test.png");
    doc.RootElement.GetProperty("width").GetInt32().Should().Be(800);
    doc.RootElement.GetProperty("height").GetInt32().Should().Be(600);
  }

  [Fact]
  public async Task TakeScreenshot_ReturnsErrorJson_OnFailure()
  {
    IInspectorProxy proxy = Substitute.For<IInspectorProxy>();
    proxy.SendCommandAsync(InspectorMethod.TakeScreenshot, Arg.Any<Dictionary<string, object?>?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
      .Returns(new InspectorResponse
      {
        RequestId = "test",
        Success = false,
        ErrorMessage = "No TopLevel available"
      });

    string result = await ScreenshotTools.TakeScreenshot(proxy, _appRegistry, "/tmp/test.png");

    result.Should().Contain("error");
    result.Should().Contain("No TopLevel available");
  }

  [Fact]
  public async Task TakeScreenshot_ReturnsErrorJson_OnException()
  {
    IInspectorProxy proxy = Substitute.For<IInspectorProxy>();
    proxy.SendCommandAsync(InspectorMethod.TakeScreenshot, Arg.Any<Dictionary<string, object?>?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
      .ThrowsAsync(new InvalidOperationException("No app connected"));

    string result = await ScreenshotTools.TakeScreenshot(proxy, _appRegistry, "/tmp/test.png");

    result.Should().Contain("error");
    result.Should().Contain("No app connected");
  }

  [Fact]
  public async Task TakeScreenshot_PassesOutputPathInParameters()
  {
    IInspectorProxy proxy = Substitute.For<IInspectorProxy>();
    ScreenshotResult screenshotResult = new() { Success = true, FilePath = "/custom/path.png", Width = 640, Height = 480 };
    proxy.SendCommandAsync(Arg.Any<InspectorMethod>(), Arg.Any<Dictionary<string, object?>?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
      .Returns(new InspectorResponse
      {
        RequestId = "test",
        Success = true,
        ResultJson = JsonSerializer.Serialize(screenshotResult, JsonOptions.Default)
      });

    await ScreenshotTools.TakeScreenshot(proxy, _appRegistry, "/custom/path.png", "myElement", 2.0, appId: "app1");

    await proxy.Received(1).SendCommandAsync(
      InspectorMethod.TakeScreenshot,
      Arg.Is<Dictionary<string, object?>>(d =>
        d["outputPath"]!.ToString() == "/custom/path.png" &&
        d["elementName"]!.ToString() == "myElement"),
      "app1",
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task TakeScreenshot_GeneratesDefaultPath_WhenNotProvided()
  {
    IInspectorProxy proxy = Substitute.For<IInspectorProxy>();
    ScreenshotResult screenshotResult = new() { Success = true, FilePath = "/tmp/auto.png", Width = 100, Height = 100 };
    proxy.SendCommandAsync(Arg.Any<InspectorMethod>(), Arg.Any<Dictionary<string, object?>?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
      .Returns(new InspectorResponse
      {
        RequestId = "test",
        Success = true,
        ResultJson = JsonSerializer.Serialize(screenshotResult, JsonOptions.Default)
      });

    await ScreenshotTools.TakeScreenshot(proxy, _appRegistry);

    await proxy.Received(1).SendCommandAsync(
      InspectorMethod.TakeScreenshot,
      Arg.Is<Dictionary<string, object?>>(d =>
        d["outputPath"]!.ToString()!.Contains("screenshots") &&
        d["outputPath"]!.ToString()!.EndsWith(".png")),
      Arg.Any<string?>(),
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task TakeScreenshot_UsesAppScreenshotDirectory_WhenConfigured()
  {
    IInspectorProxy proxy = Substitute.For<IInspectorProxy>();
    IAppRegistry appRegistry = Substitute.For<IAppRegistry>();
    appRegistry.GetAppInfo(null).Returns(new ConnectedAppInfo
    {
      AppId = "app1",
      AppName = "Test",
      ProcessId = 1,
      ProcessName = "test",
      ConnectedAt = DateTimeOffset.UtcNow,
      ScreenshotDirectory = "/custom/screenshots"
    });
    ScreenshotResult screenshotResult = new() { Success = true, FilePath = "/custom/screenshots/app-test.png", Width = 100, Height = 100 };
    proxy.SendCommandAsync(Arg.Any<InspectorMethod>(), Arg.Any<Dictionary<string, object?>?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
      .Returns(new InspectorResponse
      {
        RequestId = "test",
        Success = true,
        ResultJson = JsonSerializer.Serialize(screenshotResult, JsonOptions.Default)
      });

    await ScreenshotTools.TakeScreenshot(proxy, appRegistry);

    await proxy.Received(1).SendCommandAsync(
      InspectorMethod.TakeScreenshot,
      Arg.Is<Dictionary<string, object?>>(d =>
        d["outputPath"]!.ToString()!.StartsWith("/custom/screenshots")),
      Arg.Any<string?>(),
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public void GenerateDefaultPath_ContainsScreenshotDir()
  {
    string path = ScreenshotTools.GenerateDefaultPath(null);

    path.Should().Contain("screenshots");
    path.Should().EndWith(".png");
    path.Should().Contain("app-");
  }

  [Fact]
  public void GenerateDefaultPath_UsesAppIdAsPrefix()
  {
    string path = ScreenshotTools.GenerateDefaultPath("myApp");

    path.Should().Contain("myApp-");
    path.Should().EndWith(".png");
  }

  [Fact]
  public void GenerateDefaultPath_UsesCustomDirectory_WhenProvided()
  {
    string path = ScreenshotTools.GenerateDefaultPath("myApp", "/my/custom/dir");

    path.Should().StartWith("/my/custom/dir");
    path.Should().Contain("myApp-");
    path.Should().EndWith(".png");
  }

  [Fact]
  public async Task TakeScreenshot_ReturnsError_WhenScreenshotFails()
  {
    IInspectorProxy proxy = Substitute.For<IInspectorProxy>();
    ScreenshotResult screenshotResult = new() { Success = false, Error = "Visual has zero size" };
    proxy.SendCommandAsync(Arg.Any<InspectorMethod>(), Arg.Any<Dictionary<string, object?>?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
      .Returns(new InspectorResponse
      {
        RequestId = "test",
        Success = true,
        ResultJson = JsonSerializer.Serialize(screenshotResult, JsonOptions.Default)
      });

    string result = await ScreenshotTools.TakeScreenshot(proxy, _appRegistry, "/tmp/test.png");

    result.Should().Contain("error");
    result.Should().Contain("Visual has zero size");
  }
}
