using Microsoft.AspNetCore.SignalR;
using Six.Avalonia.Mcp.Contracts.Hub;
using Six.Avalonia.Mcp.Server.Hub;

namespace Six.Avalonia.Mcp.Server.Services;

/// <summary>
/// Sends graceful disconnect messages to all connected Avalonia apps when the server shuts down.
/// </summary>
public class GracefulShutdownService : IHostedService
{
  private readonly IHubContext<InspectorHub, IInspectorClient> _hubContext;
  private readonly IAppRegistry _registry;
  private readonly ILogger<GracefulShutdownService> _logger;

  public GracefulShutdownService(
    IHubContext<InspectorHub, IInspectorClient> hubContext,
    IAppRegistry registry,
    ILogger<GracefulShutdownService> logger)
  {
    _hubContext = hubContext;
    _registry = registry;
    _logger = logger;
  }

  public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

  public async Task StopAsync(CancellationToken cancellationToken)
  {
    var apps = _registry.GetAllApps();
    if(apps.Count == 0)
    {
      return;
    }

    _logger.LogInformation("Notifying {Count} connected app(s) of server shutdown", apps.Count);

    Task[] tasks = apps
      .Select(app => NotifyAppAsync(app.ConnectionId, app.AppInfo.AppName, cancellationToken))
      .ToArray();

    await Task.WhenAll(tasks);
  }

  private async Task NotifyAppAsync(string connectionId, string appName, CancellationToken ct)
  {
    try
    {
      await _hubContext.Clients.Client(connectionId).DisconnectAsync("Server shutting down");
      _logger.LogDebug("Sent disconnect to '{AppName}'", appName);
    }
    catch(Exception ex)
    {
      _logger.LogDebug(ex, "Failed to notify '{AppName}' (may have already disconnected)", appName);
    }
  }
}
