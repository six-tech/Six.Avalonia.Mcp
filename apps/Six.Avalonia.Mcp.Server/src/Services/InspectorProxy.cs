using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Six.Avalonia.Mcp.Contracts;
using Six.Avalonia.Mcp.Contracts.Hub;
using Six.Avalonia.Mcp.Contracts.Messages;

namespace Six.Avalonia.Mcp.Server.Services;

/// <summary>
/// Sends inspector commands to connected Avalonia apps via SignalR.
/// </summary>
public class InspectorProxy : IInspectorProxy
{
  private static readonly TimeSpan CommandTimeout = TimeSpan.FromSeconds(30);

  private readonly IHubContext<Hub.InspectorHub, IInspectorClient> _hubContext;
  private readonly IAppRegistry _registry;
  private readonly ILogger<InspectorProxy> _logger;

  public InspectorProxy(
    IHubContext<Hub.InspectorHub, IInspectorClient> hubContext,
    IAppRegistry registry,
    ILogger<InspectorProxy> logger)
  {
    _hubContext = hubContext;
    _registry = registry;
    _logger = logger;
  }

  /// <inheritdoc />
  public async Task<InspectorResponse> SendCommandAsync(
    InspectorMethod method,
    Dictionary<string, object?>? parameters = null,
    string? appId = null,
    CancellationToken ct = default)
  {
    string? connectionId = _registry.GetConnectionId(appId);
    if(connectionId is null)
    {
      string message = appId is not null
        ? $"App '{appId}' is not connected."
        : "No Avalonia app is connected. Start an app with the MCP connector enabled.";
      throw new InvalidOperationException(message);
    }

    string requestId = Guid.NewGuid().ToString("N")[..12];
    InspectorRequest request = new()
    {
      RequestId = requestId,
      Method = method,
      ParametersJson = JsonSerializer.Serialize(parameters ?? new Dictionary<string, object?>(), JsonOptions.Default)
    };

    using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
    timeoutCts.CancelAfter(CommandTimeout);

    try
    {
      _logger.LogDebug("Sending {Method} to connection {ConnectionId} (RequestId: {RequestId})",
        method, connectionId, requestId);

      return await _hubContext.Clients.Client(connectionId).ExecuteCommandAsync(request)
        .WaitAsync(timeoutCts.Token);
    }
    catch(OperationCanceledException) when(!ct.IsCancellationRequested)
    {
      _logger.LogError("Command {Method} timed out after {Timeout}s (RequestId: {RequestId})",
        method, CommandTimeout.TotalSeconds, requestId);
      throw new TimeoutException(
        $"Command {method} timed out after {CommandTimeout.TotalSeconds}s (RequestId: {requestId}). " +
        "The Avalonia app may be unresponsive.");
    }
    catch(Exception ex)
    {
      _logger.LogError(ex, "Error sending command {Method} (RequestId: {RequestId})", method, requestId);
      throw;
    }
  }

  /// <inheritdoc />
  public async Task ShutdownAppAsync(string? appId = null, CancellationToken ct = default)
  {
    string? connectionId = _registry.GetConnectionId(appId);
    if(connectionId is null)
    {
      string message = appId is not null
        ? $"App '{appId}' is not connected."
        : "No Avalonia app is connected. Start an app with the MCP connector enabled.";
      throw new InvalidOperationException(message);
    }

    _logger.LogInformation("Sending shutdown to connection {ConnectionId}", connectionId);
    await _hubContext.Clients.Client(connectionId).ShutdownAsync("kill_app");
  }
}
