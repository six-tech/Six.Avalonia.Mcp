namespace Six.Avalonia.Mcp.Contracts;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Shared JSON serialization options for the MCP protocol.
/// </summary>
[PublicAPI]
[ExcludeFromCodeCoverage]
public static class JsonOptions
{
  /// <summary>
  /// Default JSON serializer options: camelCase naming, ignore null values, compact output.
  /// </summary>
  public static JsonSerializerOptions Default { get; } = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false,
    PropertyNameCaseInsensitive = true
  };
}
