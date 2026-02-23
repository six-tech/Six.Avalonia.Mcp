using Microsoft.AspNetCore.SignalR;
using Six.Avalonia.Mcp.Contracts.Hub;
using Six.Avalonia.Mcp.Contracts.Models;
using Six.Avalonia.Mcp.Server.Services;

namespace Six.Avalonia.Mcp.Server.Hub;

/// <summary>
/// SignalR hub that manages connections from Avalonia apps and routes inspector commands.
/// </summary>
public class InspectorHub : Hub<IInspectorClient>, IInspectorHub
{
  private readonly IAppRegistry _registry;
  private readonly ILogger<InspectorHub> _logger;

  public InspectorHub(IAppRegistry registry, ILogger<InspectorHub> logger)
  {
    _registry = registry;
    _logger = logger;
  }

  /// <inheritdoc />
  public Task RegisterAppAsync(ConnectedAppInfo appInfo)
  {
    IReadOnlyList<string> purged = _registry.PurgeStaleApps();
    foreach(string staleAppId in purged)
    {
      _logger.LogInformation("Purged stale app registration: {AppId}", staleAppId);
    }

    _registry.Register(Context.ConnectionId, appInfo);
    _logger.LogInformation("App registered: {AppName} (AppId: {AppId}, ConnectionId: {ConnectionId})",
      appInfo.AppName, appInfo.AppId, Context.ConnectionId);
    return Task.CompletedTask;
  }

  /// <inheritdoc />
  public override Task OnDisconnectedAsync(Exception? exception)
  {
    _registry.Deregister(Context.ConnectionId);
    _logger.LogInformation("App disconnected (ConnectionId: {ConnectionId})", Context.ConnectionId);
    return base.OnDisconnectedAsync(exception);
  }
}
