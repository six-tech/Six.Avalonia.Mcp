using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.Options;

namespace Six.Avalonia.Mcp.Connector;

/// <summary>
/// Applies environment variable overrides and AppName auto-detection after user configuration.
/// </summary>
/// <remarks>
/// Priority order (highest to lowest):
/// <list type="number">
///   <item>Explicit values from user's configure lambda</item>
///   <item>Environment variables (<c>SIX_MCP_ServerUrl</c>, <c>SIX_MCP_AppName</c>, etc.)</item>
///   <item>Assembly name auto-detection (AppName only)</item>
///   <item>Property defaults</item>
/// </list>
/// </remarks>
internal sealed class McpConnectorPostConfigure : IPostConfigureOptions<McpConnectorOptions>
{
  public void PostConfigure(string? name, McpConnectorOptions options)
  {
    ApplyEnvironmentVariables(options);
    ApplyAppNameAutoDetection(options);
  }

  private static void ApplyEnvironmentVariables(McpConnectorOptions options)
  {
    if (options.EnvironmentPrefix is null)
    {
      return;
    }

    string prefix = options.EnvironmentPrefix;

    if (options.ServerUrl == McpConnectorOptions.DefaultServerUrl)
    {
      string? value = Environment.GetEnvironmentVariable($"{prefix}ServerUrl");

      if (!string.IsNullOrEmpty(value))
      {
        options.ServerUrl = value;
      }
    }

    if (options.AppName == McpConnectorOptions.DefaultAppName)
    {
      string? value = Environment.GetEnvironmentVariable($"{prefix}AppName");

      if (!string.IsNullOrEmpty(value))
      {
        options.AppName = value;
      }
    }

    if (options.AutoConnect == McpConnectorOptions.DefaultAutoConnect)
    {
      string? value = Environment.GetEnvironmentVariable($"{prefix}AutoConnect");

      if (bool.TryParse(value, out bool parsed))
      {
        options.AutoConnect = parsed;
      }
    }

    if (options.ReconnectDelay == McpConnectorOptions.DefaultReconnectDelay)
    {
      string? value = Environment.GetEnvironmentVariable($"{prefix}ReconnectDelay");

      if (double.TryParse(value, CultureInfo.InvariantCulture, out double seconds) && seconds > 0)
      {
        options.ReconnectDelay = TimeSpan.FromSeconds(seconds);
      }
    }

    if (options.MaxRetryAttempts == McpConnectorOptions.DefaultMaxRetryAttempts)
    {
      string? value = Environment.GetEnvironmentVariable($"{prefix}MaxRetryAttempts");

      if (int.TryParse(value, out int parsed) && parsed > 0)
      {
        options.MaxRetryAttempts = parsed;
      }
    }

    if (options.ScreenshotDirectory is null)
    {
      string? value = Environment.GetEnvironmentVariable($"{prefix}ScreenshotDirectory");

      if (!string.IsNullOrEmpty(value))
      {
        options.ScreenshotDirectory = value;
      }
    }
  }

  private static void ApplyAppNameAutoDetection(McpConnectorOptions options)
  {
    if (options.AppName != McpConnectorOptions.DefaultAppName)
    {
      return;
    }

    string? assemblyName = Assembly.GetEntryAssembly()?.GetName().Name;

    if (!string.IsNullOrEmpty(assemblyName))
    {
      options.AppName = assemblyName;
    }
  }
}
