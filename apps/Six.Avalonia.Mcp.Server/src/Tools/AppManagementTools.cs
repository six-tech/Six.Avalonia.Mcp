using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using Six.Avalonia.Mcp.Contracts;
using Six.Avalonia.Mcp.Contracts.Messages;
using Six.Avalonia.Mcp.Server.Services;

namespace Six.Avalonia.Mcp.Server.Tools;

/// <summary>
/// MCP tools for managing connected Avalonia applications.
/// </summary>
[McpServerToolType]
public static class AppManagementTools
{
  [McpServerTool(Name = "list_connected_apps")]
  [Description(
    "List all currently connected Avalonia applications with their app IDs, names, process info, and connection time.")]
  public static string ListConnectedApps(IAppRegistry registry)
  {
    try
    {
      var apps = registry.GetAllApps()
        .Select(a => new
        {
          a.AppInfo.AppId,
          a.AppInfo.AppName,
          a.AppInfo.ProcessId,
          a.AppInfo.ProcessName,
          a.AppInfo.ConnectedAt,
          isTarget = a.AppInfo.AppId == registry.GetTargetAppId()
        })
        .ToList();

      return JsonSerializer.Serialize(new { apps, count = apps.Count }, JsonOptions.Default);
    }
    catch(Exception ex)
    {
      return JsonSerializer.Serialize(new { error = ex.Message }, JsonOptions.Default);
    }
  }

  [McpServerTool(Name = "set_target_app")]
  [Description(
    "Set the target Avalonia application for subsequent inspection commands. Use list_connected_apps to see available app IDs.")]
  public static string SetTargetApp(IAppRegistry registry, string appId)
  {
    try
    {
      registry.SetTargetApp(appId);
      return JsonSerializer.Serialize(new { success = true, targetAppId = appId }, JsonOptions.Default);
    }
    catch(Exception ex)
    {
      return JsonSerializer.Serialize(new { error = ex.Message }, JsonOptions.Default);
    }
  }

  [McpServerTool(Name = "kill_app")]
  [Description(
    "Terminate a connected Avalonia application. The app will shut down gracefully. Use list_connected_apps to find the target app ID.")]
  public static async Task<string> KillApp(
    IInspectorProxy proxy,
    string? appId = null,
    CancellationToken ct = default)
  {
    try
    {
      await proxy.ShutdownAppAsync(appId, ct);
      return JsonSerializer.Serialize(new { success = true, message = "Shutdown signal sent." }, JsonOptions.Default);
    }
    catch(Exception ex)
    {
      return JsonSerializer.Serialize(new { error = ex.Message }, JsonOptions.Default);
    }
  }
}
