using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Six.Avalonia.Mcp.Connector.Services;

namespace Six.Avalonia.Mcp.Connector;

/// <summary>
/// Provides extension methods for adding the MCP connector to an Avalonia application host.
/// </summary>
[PublicAPI]
[ExcludeFromCodeCoverage]
public static class McpConnectorHost
{
  /// <summary>
  /// Adds the Avalonia MCP connector to the service collection.
  /// Registers the inspector, command handler, and SignalR connector service.
  /// </summary>
  public static IServiceCollection AddAvaloniaMcpConnector(
    this IServiceCollection services,
    Action<McpConnectorOptions>? configure = null)
  {
    if(configure is not null)
    {
      services.Configure(configure);
    }
    else
    {
      services.Configure<McpConnectorOptions>(_ => { });
    }

    services.TryAddSingleton<IPostConfigureOptions<McpConnectorOptions>, McpConnectorPostConfigure>();

    services.AddSingleton<IAvaloniaInspector, AvaloniaInspector>();
    services.AddSingleton<InspectorCommandHandler>();
    services.AddSingleton<ConnectorService>();
    services.AddHostedService(sp => sp.GetRequiredService<ConnectorService>());

    return services;
  }

  /// <summary>
  /// Adds the Avalonia MCP connector to the host builder.
  /// Configures logging to stderr at Trace level and registers environment variable configuration.
  /// </summary>
  public static IHostBuilder AddAvaloniaMcpConnector(
    this IHostBuilder hostBuilder,
    Action<McpConnectorOptions>? configure = null)
  {
    // Resolve the prefix: apply user's configure lambda to a temp instance to read EnvironmentPrefix.
    var tempOptions = new McpConnectorOptions();
    configure?.Invoke(tempOptions);
    string? prefix = tempOptions.EnvironmentPrefix;

    return hostBuilder
      .ConfigureAppConfiguration(builder =>
      {
        if (prefix is not null)
        {
          builder.AddEnvironmentVariables(prefix);
        }
      })
      .ConfigureLogging(logging =>
      {
        logging.ClearProviders();
        logging.AddDebug();
        logging.AddConsole(options =>
        {
          options.LogToStandardErrorThreshold = LogLevel.Trace;
        });
        logging.SetMinimumLevel(LogLevel.Debug);
      })
      .ConfigureServices((_, services) => services.AddAvaloniaMcpConnector(configure));
  }
}
