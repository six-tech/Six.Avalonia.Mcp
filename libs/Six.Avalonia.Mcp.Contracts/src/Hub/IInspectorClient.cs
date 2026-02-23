using Six.Avalonia.Mcp.Contracts.Messages;

namespace Six.Avalonia.Mcp.Contracts.Hub;

/// <summary>
/// Client callback interface — methods the server can invoke on connected Avalonia apps.
/// </summary>
public interface IInspectorClient
{
  /// <summary>
  /// Executes an inspection command on the connected Avalonia app.
  /// </summary>
  Task<InspectorResponse> ExecuteCommandAsync(InspectorRequest request);

  /// <summary>
  /// Requests the connected app to disconnect gracefully.
  /// </summary>
  Task DisconnectAsync(string reason);

  /// <summary>
  /// Requests the connected app to terminate its process.
  /// </summary>
  Task ShutdownAsync(string reason);
}
