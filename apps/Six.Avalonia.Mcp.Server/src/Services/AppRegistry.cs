using System.Collections.Concurrent;
using System.Diagnostics;
using Six.Avalonia.Mcp.Contracts.Models;

namespace Six.Avalonia.Mcp.Server.Services;

/// <summary>
/// Thread-safe registry of connected Avalonia applications.
/// </summary>
public class AppRegistry : IAppRegistry
{
  private readonly ConcurrentDictionary<string, (string ConnectionId, ConnectedAppInfo AppInfo)> _apps = new();
  private readonly ConcurrentDictionary<string, string> _connectionToApp = new();
  private volatile string? _targetAppId;

  /// <inheritdoc />
  public void Register(string connectionId, ConnectedAppInfo appInfo)
  {
    _apps[appInfo.AppId] = (connectionId, appInfo);
    _connectionToApp[connectionId] = appInfo.AppId;
  }

  /// <inheritdoc />
  public void Deregister(string connectionId)
  {
    if(_connectionToApp.TryRemove(connectionId, out string? appId))
    {
      _apps.TryRemove(appId, out _);

      if(_targetAppId == appId)
      {
        _targetAppId = null;
      }
    }
  }

  /// <inheritdoc />
  public IReadOnlyList<(string ConnectionId, ConnectedAppInfo AppInfo)> GetAllApps()
  {
    return _apps.Values.ToList();
  }

  /// <inheritdoc />
  public string? GetConnectionId(string? appId = null)
  {
    string? targetId = appId ?? _targetAppId;

    if(targetId is not null && _apps.TryGetValue(targetId, out var entry))
    {
      return entry.ConnectionId;
    }

    // Auto-select if only one app is connected
    if(targetId is null && _apps.Count == 1)
    {
      return _apps.Values.First().ConnectionId;
    }

    return null;
  }

  /// <inheritdoc />
  public void SetTargetApp(string appId)
  {
    if(!_apps.ContainsKey(appId))
    {
      throw new InvalidOperationException($"App '{appId}' is not connected.");
    }

    _targetAppId = appId;
  }

  /// <inheritdoc />
  public string? GetTargetAppId()
  {
    return _targetAppId;
  }

  /// <inheritdoc />
  public ConnectedAppInfo? GetAppInfo(string? appId = null)
  {
    string? targetId = appId ?? _targetAppId;

    if(targetId is not null && _apps.TryGetValue(targetId, out var entry))
    {
      return entry.AppInfo;
    }

    // Auto-select if only one app is connected
    if(targetId is null && _apps.Count == 1)
    {
      return _apps.Values.First().AppInfo;
    }

    return null;
  }

  /// <inheritdoc />
  public IReadOnlyList<string> PurgeStaleApps()
  {
    List<string> purged = [];

    foreach(var kvp in _apps)
    {
      string appId = kvp.Key;
      int processId = kvp.Value.AppInfo.ProcessId;

      bool isAlive;
      try
      {
        using Process process = Process.GetProcessById(processId);
        isAlive = !process.HasExited;
      }
      catch(ArgumentException)
      {
        // Process with this ID does not exist
        isAlive = false;
      }

      if(!isAlive)
      {
        string connectionId = kvp.Value.ConnectionId;
        _apps.TryRemove(appId, out _);
        _connectionToApp.TryRemove(connectionId, out _);

        if(_targetAppId == appId)
        {
          _targetAppId = null;
        }

        purged.Add(appId);
      }
    }

    return purged;
  }
}
