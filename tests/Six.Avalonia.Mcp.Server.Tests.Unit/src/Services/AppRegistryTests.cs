using System.Diagnostics;
using Six.Avalonia.Mcp.Contracts.Models;
using Six.Avalonia.Mcp.Server.Services;

namespace Six.Avalonia.Mcp.Server.Tests.Unit.Services;

public class AppRegistryTests
{
  private static ConnectedAppInfo CreateAppInfo(string appId = "app1", string appName = "Test App", int processId = 1234)
  {
    return new ConnectedAppInfo
    {
      AppId = appId,
      AppName = appName,
      ProcessId = processId,
      ProcessName = "testapp",
      ConnectedAt = DateTimeOffset.UtcNow
    };
  }

  [Fact]
  public void Register_AddsAppToRegistry()
  {
    AppRegistry registry = new();
    ConnectedAppInfo appInfo = CreateAppInfo();

    registry.Register("conn1", appInfo);

    registry.GetAllApps().Should().HaveCount(1);
    registry.GetAllApps()[0].AppInfo.AppId.Should().Be("app1");
  }

  [Fact]
  public void Deregister_RemovesAppFromRegistry()
  {
    AppRegistry registry = new();
    registry.Register("conn1", CreateAppInfo());

    registry.Deregister("conn1");

    registry.GetAllApps().Should().BeEmpty();
  }

  [Fact]
  public void GetConnectionId_ReturnsSingleApp_WhenOnlyOneConnected()
  {
    AppRegistry registry = new();
    registry.Register("conn1", CreateAppInfo());

    string? connectionId = registry.GetConnectionId();

    connectionId.Should().Be("conn1");
  }

  [Fact]
  public void GetConnectionId_ReturnsNull_WhenNoAppsConnected()
  {
    AppRegistry registry = new();

    string? connectionId = registry.GetConnectionId();

    connectionId.Should().BeNull();
  }

  [Fact]
  public void GetConnectionId_ReturnsNull_WhenMultipleAppsAndNoTarget()
  {
    AppRegistry registry = new();
    registry.Register("conn1", CreateAppInfo("app1"));
    registry.Register("conn2", CreateAppInfo("app2"));

    string? connectionId = registry.GetConnectionId();

    connectionId.Should().BeNull();
  }

  [Fact]
  public void SetTargetApp_AllowsGetConnectionIdToResolve()
  {
    AppRegistry registry = new();
    registry.Register("conn1", CreateAppInfo("app1"));
    registry.Register("conn2", CreateAppInfo("app2"));

    registry.SetTargetApp("app2");

    registry.GetConnectionId().Should().Be("conn2");
  }

  [Fact]
  public void SetTargetApp_ThrowsForUnknownApp()
  {
    AppRegistry registry = new();

    Action act = () => registry.SetTargetApp("unknown");

    act.Should().Throw<InvalidOperationException>();
  }

  [Fact]
  public void GetConnectionId_ByAppId_ReturnsCorrectConnection()
  {
    AppRegistry registry = new();
    registry.Register("conn1", CreateAppInfo("app1"));
    registry.Register("conn2", CreateAppInfo("app2"));

    registry.GetConnectionId("app1").Should().Be("conn1");
    registry.GetConnectionId("app2").Should().Be("conn2");
  }

  [Fact]
  public void Deregister_ClearsTargetIfMatching()
  {
    AppRegistry registry = new();
    registry.Register("conn1", CreateAppInfo("app1"));
    registry.SetTargetApp("app1");

    registry.Deregister("conn1");

    registry.GetTargetAppId().Should().BeNull();
  }

  [Fact]
  public void PurgeStaleApps_RemovesDeadProcessEntries()
  {
    AppRegistry registry = new();
    // Use a PID that certainly doesn't exist
    registry.Register("conn1", CreateAppInfo("app1", processId: int.MaxValue));

    IReadOnlyList<string> purged = registry.PurgeStaleApps();

    purged.Should().ContainSingle().Which.Should().Be("app1");
    registry.GetAllApps().Should().BeEmpty();
  }

  [Fact]
  public void PurgeStaleApps_KeepsLiveProcessEntries()
  {
    AppRegistry registry = new();
    int currentPid = Environment.ProcessId;
    registry.Register("conn1", CreateAppInfo("app1", processId: currentPid));

    IReadOnlyList<string> purged = registry.PurgeStaleApps();

    purged.Should().BeEmpty();
    registry.GetAllApps().Should().HaveCount(1);
  }

  [Fact]
  public void PurgeStaleApps_ClearsTargetIfPurged()
  {
    AppRegistry registry = new();
    // Register a live app so SetTargetApp succeeds, then swap to dead PID
    int currentPid = Environment.ProcessId;
    registry.Register("conn1", CreateAppInfo("app1", processId: currentPid));
    registry.Register("conn2", CreateAppInfo("app2", processId: int.MaxValue));
    registry.SetTargetApp("app2");

    registry.PurgeStaleApps();

    registry.GetTargetAppId().Should().BeNull();
  }

  [Fact]
  public void GetAppInfo_ReturnsSingleApp_WhenOnlyOneConnected()
  {
    AppRegistry registry = new();
    registry.Register("conn1", CreateAppInfo());

    ConnectedAppInfo? appInfo = registry.GetAppInfo();

    appInfo.Should().NotBeNull();
    appInfo!.AppId.Should().Be("app1");
  }

  [Fact]
  public void GetAppInfo_ReturnsNull_WhenNoAppsConnected()
  {
    AppRegistry registry = new();

    ConnectedAppInfo? appInfo = registry.GetAppInfo();

    appInfo.Should().BeNull();
  }

  [Fact]
  public void GetAppInfo_ByAppId_ReturnsCorrectApp()
  {
    AppRegistry registry = new();
    registry.Register("conn1", CreateAppInfo("app1"));
    registry.Register("conn2", CreateAppInfo("app2", appName: "Second App"));

    ConnectedAppInfo? appInfo = registry.GetAppInfo("app2");

    appInfo.Should().NotBeNull();
    appInfo!.AppName.Should().Be("Second App");
  }

  [Fact]
  public void GetAppInfo_ReturnsNull_WhenMultipleAppsAndNoTarget()
  {
    AppRegistry registry = new();
    registry.Register("conn1", CreateAppInfo("app1"));
    registry.Register("conn2", CreateAppInfo("app2"));

    ConnectedAppInfo? appInfo = registry.GetAppInfo();

    appInfo.Should().BeNull();
  }

  [Fact]
  public void GetAppInfo_ReturnsTargetApp_WhenTargetIsSet()
  {
    AppRegistry registry = new();
    registry.Register("conn1", CreateAppInfo("app1"));
    registry.Register("conn2", CreateAppInfo("app2", appName: "Second App"));
    registry.SetTargetApp("app2");

    ConnectedAppInfo? appInfo = registry.GetAppInfo();

    appInfo.Should().NotBeNull();
    appInfo!.AppName.Should().Be("Second App");
  }

  [Fact]
  public void PurgeStaleApps_ReturnsEmptyWhenNoApps()
  {
    AppRegistry registry = new();

    IReadOnlyList<string> purged = registry.PurgeStaleApps();

    purged.Should().BeEmpty();
  }
}
