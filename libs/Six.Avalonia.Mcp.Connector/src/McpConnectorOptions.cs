namespace Six.Avalonia.Mcp.Connector;

/// <summary>
/// Configuration options for the MCP connector.
/// </summary>
[PublicAPI]
public sealed class McpConnectorOptions
{
  /// <summary>
  /// Default environment variable prefix used for option overrides.
  /// </summary>
  public const string DefaultEnvironmentPrefix = "SIX_MCP_";

  internal const string DefaultServerUrl = "http://localhost:52342/inspector";
  internal const string DefaultAppName = "Avalonia App";
  internal const bool DefaultAutoConnect = true;
  internal const int DefaultMaxRetryAttempts = 5;
  internal static readonly TimeSpan DefaultReconnectDelay = TimeSpan.FromSeconds(5);

  /// <summary>
  /// The URL of the MCP server's SignalR hub.
  /// </summary>
  public string ServerUrl { get; set; } = DefaultServerUrl;

  /// <summary>
  /// Display name for this application in the server registry.
  /// </summary>
  public string AppName { get; set; } = DefaultAppName;

  /// <summary>
  /// Whether to automatically connect on startup.
  /// </summary>
  public bool AutoConnect { get; set; } = DefaultAutoConnect;

  /// <summary>
  /// Maximum number of connection attempts before giving up.
  /// </summary>
  public int MaxRetryAttempts { get; set; } = DefaultMaxRetryAttempts;

  /// <summary>
  /// Delay between reconnection attempts.
  /// </summary>
  public TimeSpan ReconnectDelay { get; set; } = DefaultReconnectDelay;

  /// <summary>
  /// Directory where screenshots are saved when no explicit output path is provided.
  /// If <c>null</c>, the server uses its own default directory.
  /// </summary>
  public string? ScreenshotDirectory { get; set; }

  /// <summary>
  /// Environment variable prefix for option overrides. Set to <c>null</c> to disable env var binding.
  /// </summary>
  public string? EnvironmentPrefix { get; set; } = DefaultEnvironmentPrefix;
}
