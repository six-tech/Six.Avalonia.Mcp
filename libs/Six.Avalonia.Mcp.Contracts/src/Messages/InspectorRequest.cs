namespace Six.Avalonia.Mcp.Contracts.Messages;

/// <summary>
/// A command request sent from the MCP server to a connected Avalonia app.
/// </summary>
[PublicAPI]
[ExcludeFromCodeCoverage]
public sealed record InspectorRequest
{
  /// <summary>
  /// Unique correlation ID for matching responses.
  /// </summary>
  public required string RequestId { get; init; }

  /// <summary>
  /// The inspector method to invoke.
  /// </summary>
  public required InspectorMethod Method { get; init; }

  /// <summary>
  /// Serialized parameters as JSON (a dictionary of parameter names to values).
  /// </summary>
  public required string ParametersJson { get; init; }
}
