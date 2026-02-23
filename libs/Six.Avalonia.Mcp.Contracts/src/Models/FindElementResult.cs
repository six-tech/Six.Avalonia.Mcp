namespace Six.Avalonia.Mcp.Contracts.Models;

/// <summary>
/// Result of a find_element query, identifying a matched element by path.
/// </summary>
[PublicAPI]
[ExcludeFromCodeCoverage]
public sealed record FindElementResult
{
  public required string Type { get; init; }

  public string? Name { get; init; }

  public required BoundsInfo Bounds { get; init; }

  public required IReadOnlyList<int> Path { get; init; }

  /// <summary>
  /// Stable element identifier in the format "path:0/2/1".
  /// </summary>
  public required string ElementId { get; init; }

  /// <summary>
  /// Zero-based index of the window that contains this element, when multi-window search was used.
  /// </summary>
  public int? WindowIndex { get; init; }
}
