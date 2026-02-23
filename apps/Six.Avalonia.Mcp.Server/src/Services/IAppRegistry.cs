using Six.Avalonia.Mcp.Contracts.Models;

namespace Six.Avalonia.Mcp.Server.Services;

/// <summary>
/// Registry of connected Avalonia applications.
/// </summary>
public interface IAppRegistry
{
  /// <summary>
  /// Registers a connected app with its SignalR connection ID.
  /// </summary>
  void Register(string connectionId, ConnectedAppInfo appInfo);

  /// <summary>
  /// Removes a connected app by its SignalR connection ID.
  /// </summary>
  void Deregister(string connectionId);

  /// <summary>
  /// Gets all currently connected apps.
  /// </summary>
  IReadOnlyList<(string ConnectionId, ConnectedAppInfo AppInfo)> GetAllApps();

  /// <summary>
  /// Gets the connection ID for a specific app ID, or the current target app if appId is null.
  /// </summary>
  string? GetConnectionId(string? appId = null);

  /// <summary>
  /// Sets the target app for subsequent commands (when multiple apps are connected).
  /// </summary>
  void SetTargetApp(string appId);

  /// <summary>
  /// Gets the current target app ID, or null if none is set.
  /// </summary>
  string? GetTargetAppId();

  /// <summary>
  /// Gets the app info for a specific app ID, or the current target/single app if appId is null.
  /// </summary>
  ConnectedAppInfo? GetAppInfo(string? appId = null);

  /// <summary>
  /// Removes registry entries whose process is no longer running.
  /// </summary>
  IReadOnlyList<string> PurgeStaleApps();
}
