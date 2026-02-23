using System.Diagnostics;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using Six.Avalonia.Mcp.Contracts.Hub;
using Six.Avalonia.Mcp.Contracts.Messages;
using Six.Avalonia.Mcp.Contracts.Models;

namespace Six.Avalonia.Mcp.Connector;

/// <summary>
/// Hosted service that maintains a SignalR connection to the MCP server
/// and dispatches incoming commands to the <see cref="InspectorCommandHandler"/>.
/// </summary>
[PublicAPI]
public class ConnectorService : IHostedService, IAsyncDisposable
{
  private readonly McpConnectorOptions _options;
  private readonly InspectorCommandHandler _commandHandler;
  private readonly IHostApplicationLifetime _appLifetime;
  private readonly ILogger<ConnectorService> _logger;
  private HubConnection? _connection;
  private readonly string _appId = Guid.NewGuid().ToString("N")[..12];

  public ConnectorService(
    IOptions<McpConnectorOptions> options,
    InspectorCommandHandler commandHandler,
    IHostApplicationLifetime appLifetime,
    ILogger<ConnectorService> logger)
  {
    _options = options.Value;
    _commandHandler = commandHandler;
    _appLifetime = appLifetime;
    _logger = logger;
  }

  /// <summary>
  /// Starts the SignalR connection and registers with the MCP server hub.
  /// </summary>
  public async Task StartAsync(CancellationToken cancellationToken)
  {
    if(!_options.AutoConnect)
    {
      _logger.LogInformation("AutoConnect is disabled; skipping connection");
      return;
    }

    BuildConnection();
    await ConnectWithRetryAsync(cancellationToken);
  }

  private void BuildConnection()
  {
    _connection = new HubConnectionBuilder()
      .WithUrl(_options.ServerUrl)
      .WithAutomaticReconnect(new RetryPolicy(_options.ReconnectDelay))
      .Build();

    _connection.ServerTimeout = TimeSpan.FromSeconds(15);
    _connection.KeepAliveInterval = TimeSpan.FromSeconds(5);

    _connection.On<InspectorRequest, InspectorResponse>(
      nameof(IInspectorClient.ExecuteCommandAsync),
      async request =>
      {
        _logger.LogDebug("Received command: {Method} (RequestId: {RequestId})", request.Method, request.RequestId);
        return await _commandHandler.HandleAsync(request);
      });

    _connection.On<string>(
      nameof(IInspectorClient.DisconnectAsync),
      async reason =>
      {
        _logger.LogInformation("Server requested disconnect: {Reason}", reason);
        if(_connection is not null)
        {
          await _connection.StopAsync(CancellationToken.None);
        }
      });

    _connection.On<string>(
      nameof(IInspectorClient.ShutdownAsync),
      async reason =>
      {
        _logger.LogInformation("Server requested shutdown: {Reason}", reason);
        if(_connection is not null)
        {
          await _connection.StopAsync(CancellationToken.None);
        }

        _appLifetime.StopApplication();
        Environment.Exit(0);
      });

    _connection.Reconnected += async _ =>
    {
      _logger.LogInformation("Reconnected to MCP server at {Url}", _options.ServerUrl);
      await RegisterAsync();
    };

    _connection.Closed += async ex =>
    {
      if(ex is not null)
      {
        _logger.LogWarning(ex, "Connection to MCP server closed with error (State: {State})",
          _connection.State);
      }
      else
      {
        _logger.LogInformation("Connection to MCP server closed (State: {State})",
          _connection.State);
      }

      if(_appLifetime.ApplicationStopping.IsCancellationRequested)
      {
        return;
      }

      _logger.LogInformation("Rebuilding connection for reconnection attempt...");

      // Dispose the dead connection and build a fresh one
      await _connection.DisposeAsync();
      BuildConnection();
      await ConnectWithRetryAsync(_appLifetime.ApplicationStopping);
    };
  }

  private async Task ConnectWithRetryAsync(CancellationToken ct)
  {
    ResiliencePipeline pipeline = new ResiliencePipelineBuilder()
      .AddRetry(new RetryStrategyOptions
      {
        MaxRetryAttempts = _options.MaxRetryAttempts - 1,
        Delay = _options.ReconnectDelay,
        BackoffType = DelayBackoffType.Constant,
        ShouldHandle = new PredicateBuilder().Handle<Exception>(ex => ex is not OperationCanceledException),
        OnRetry = args =>
        {
          _logger.LogWarning(args.Outcome.Exception,
            "Failed to connect to MCP server (attempt {Attempt}/{Max}, state: {State}). Retrying in {Delay}...",
            args.AttemptNumber + 1, _options.MaxRetryAttempts, _connection!.State, args.RetryDelay);
          return ValueTask.CompletedTask;
        }
      })
      .Build();

    try
    {
      await pipeline.ExecuteAsync(async token =>
      {
        _logger.LogInformation("Connecting to MCP server at {Url} (state: {State})...",
          _options.ServerUrl, _connection!.State);
        await _connection!.StartAsync(token);
        _logger.LogInformation("Connected to MCP server at {Url}", _options.ServerUrl);
        await RegisterAsync();
      }, ct);
    }
    catch(OperationCanceledException) when(ct.IsCancellationRequested)
    {
      // Shutdown requested — exit silently.
    }
    catch(Exception ex)
    {
      _logger.LogWarning(ex,
        "Could not connect to MCP server at {Url} after {Max} attempt(s). " +
        "The app will start without MCP inspector connectivity.",
        _options.ServerUrl, _options.MaxRetryAttempts);
    }
  }

  private async Task RegisterAsync()
  {
    using Process process = Process.GetCurrentProcess();

    ConnectedAppInfo appInfo = new()
    {
      AppId = _appId,
      AppName = $"{_options.AppName}:{_appId}",
      ProcessId = process.Id,
      ProcessName = process.ProcessName,
      ConnectedAt = DateTimeOffset.UtcNow,
      ScreenshotDirectory = _options.ScreenshotDirectory
    };

    await _connection!.InvokeAsync(nameof(IInspectorHub.RegisterAppAsync), appInfo);
    _logger.LogInformation("Registered app '{AppName}' (AppId: {AppId}) with MCP server", appInfo.AppName, _appId);
  }

  /// <summary>
  /// Stops the SignalR connection.
  /// </summary>
  public async Task StopAsync(CancellationToken cancellationToken)
  {
    if(_connection is not null)
    {
      await _connection.StopAsync(cancellationToken);
    }
  }

  /// <inheritdoc />
  public async ValueTask DisposeAsync()
  {
    if(_connection is not null)
    {
      await _connection.DisposeAsync();
    }

    GC.SuppressFinalize(this);
  }

  /// <summary>
  /// Simple retry policy that uses a fixed delay between reconnection attempts.
  /// </summary>
  private sealed class RetryPolicy : IRetryPolicy
  {
    private readonly TimeSpan _delay;

    public RetryPolicy(TimeSpan delay) => _delay = delay;

    public TimeSpan? NextRetryDelay(RetryContext retryContext) => _delay;
  }
}
