namespace Six.Avalonia.Mcp.Contracts.Messages;

/// <summary>
/// A command response sent from a connected Avalonia app back to the MCP server.
/// </summary>
[PublicAPI]
[ExcludeFromCodeCoverage]
public sealed record InspectorResponse
{
  /// <summary>
  /// Correlation ID matching the original request.
  /// </summary>
  public required string RequestId { get; init; }

  /// <summary>
  /// Whether the command executed successfully.
  /// </summary>
  public required bool Success { get; init; }

  /// <summary>
  /// Serialized result as JSON (on success).
  /// </summary>
  public string? ResultJson { get; init; }

  /// <summary>
  /// Error message (on failure).
  /// </summary>
  public string? ErrorMessage { get; init; }
}
