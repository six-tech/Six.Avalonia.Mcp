using Six.Avalonia.Mcp.Contracts.Messages;

namespace Six.Avalonia.Mcp.Server.Services;

/// <summary>
/// Proxy for sending inspector commands to connected Avalonia apps via SignalR.
/// </summary>
public interface IInspectorProxy
{
  /// <summary>
  /// Sends an inspector command to the specified (or default target) app and awaits the response.
  /// </summary>
  Task<InspectorResponse> SendCommandAsync(
    InspectorMethod method,
    Dictionary<string, object?>? parameters = null,
    string? appId = null,
    CancellationToken ct = default);

  /// <summary>
  /// Requests the specified (or default target) app to shut down.
  /// </summary>
  Task ShutdownAppAsync(string? appId = null, CancellationToken ct = default);
}
