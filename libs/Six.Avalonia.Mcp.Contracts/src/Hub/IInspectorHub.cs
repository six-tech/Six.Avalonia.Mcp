using Six.Avalonia.Mcp.Contracts.Models;

namespace Six.Avalonia.Mcp.Contracts.Hub;

/// <summary>
/// Server hub interface — methods the Avalonia client can invoke on the server.
/// </summary>
public interface IInspectorHub
{
  /// <summary>
  /// Registers a connected Avalonia application with the hub.
  /// </summary>
  Task RegisterAppAsync(ConnectedAppInfo appInfo);
}
