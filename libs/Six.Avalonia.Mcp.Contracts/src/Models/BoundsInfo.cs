namespace Six.Avalonia.Mcp.Contracts.Models;

/// <summary>
/// Represents the bounding rectangle of a visual element.
/// </summary>
[PublicAPI]
[ExcludeFromCodeCoverage]
public sealed record BoundsInfo
{
  public required double X { get; init; }

  public required double Y { get; init; }

  public required double Width { get; init; }

  public required double Height { get; init; }
}
