using Six.Avalonia.Mcp.Contracts;
using Six.Avalonia.Mcp.Contracts.Messages;
using Six.Avalonia.Mcp.Contracts.Models;
using Six.Avalonia.Mcp.Server.Services;
using Six.Avalonia.Mcp.Server.Tools;

namespace Six.Avalonia.Mcp.Server.Tests.Unit.Tools;

public class AppManagementToolsTests
{
  [Fact]
  public void ListConnectedApps_ReturnsEmptyList_WhenNoApps()
  {
    IAppRegistry registry = Substitute.For<IAppRegistry>();
    registry.GetAllApps().Returns(new List<(string, ConnectedAppInfo)>());
    registry.GetTargetAppId().Returns((string?)null);

    string result = AppManagementTools.ListConnectedApps(registry);

    JsonDocument doc = JsonDocument.Parse(result);
    doc.RootElement.GetProperty("count").GetInt32().Should().Be(0);
  }

  [Fact]
  public void ListConnectedApps_ReturnsConnectedApps()
  {
    IAppRegistry registry = Substitute.For<IAppRegistry>();
    registry.GetAllApps().Returns(new List<(string, ConnectedAppInfo)>
    {
      ("conn1", new ConnectedAppInfo
      {
        AppId = "app1",
        AppName = "Test App",
        ProcessId = 1234,
        ProcessName = "test",
        ConnectedAt = DateTimeOffset.UtcNow
      })
    });
    registry.GetTargetAppId().Returns("app1");

    string result = AppManagementTools.ListConnectedApps(registry);

    JsonDocument doc = JsonDocument.Parse(result);
    doc.RootElement.GetProperty("count").GetInt32().Should().Be(1);
  }

  [Fact]
  public void SetTargetApp_ReturnsSuccess()
  {
    IAppRegistry registry = Substitute.For<IAppRegistry>();

    string result = AppManagementTools.SetTargetApp(registry, "app1");

    JsonDocument doc = JsonDocument.Parse(result);
    doc.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
    doc.RootElement.GetProperty("targetAppId").GetString().Should().Be("app1");
  }

  [Fact]
  public void SetTargetApp_ReturnsError_WhenAppNotConnected()
  {
    IAppRegistry registry = Substitute.For<IAppRegistry>();
    registry.When(r => r.SetTargetApp("unknown")).Do(_ => throw new InvalidOperationException("App 'unknown' is not connected."));

    string result = AppManagementTools.SetTargetApp(registry, "unknown");

    result.Should().Contain("error");
    result.Should().Contain("not connected");
  }

  [Fact]
  public async Task KillApp_ReturnsSuccess()
  {
    IInspectorProxy proxy = Substitute.For<IInspectorProxy>();

    string result = await AppManagementTools.KillApp(proxy);

    JsonDocument doc = JsonDocument.Parse(result);
    doc.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
    doc.RootElement.GetProperty("message").GetString().Should().Contain("Shutdown");
  }

  [Fact]
  public async Task KillApp_PassesAppId()
  {
    IInspectorProxy proxy = Substitute.For<IInspectorProxy>();

    await AppManagementTools.KillApp(proxy, "app1");

    await proxy.Received(1).ShutdownAppAsync("app1", Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task KillApp_ReturnsError_WhenAppNotConnected()
  {
    IInspectorProxy proxy = Substitute.For<IInspectorProxy>();
    proxy.ShutdownAppAsync("unknown", Arg.Any<CancellationToken>())
      .ThrowsAsync(new InvalidOperationException("App 'unknown' is not connected."));

    string result = await AppManagementTools.KillApp(proxy, "unknown");

    result.Should().Contain("error");
    result.Should().Contain("not connected");
  }
}
