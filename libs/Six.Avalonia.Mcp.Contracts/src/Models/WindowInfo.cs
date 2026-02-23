namespace Six.Avalonia.Mcp.Contracts.Models;

/// <summary>
/// Describes a single window (TopLevel) in the running application.
/// </summary>
[PublicAPI]
[ExcludeFromCodeCoverage]
public sealed record WindowInfo
{
  public required int Index { get; init; }

  public string? Title { get; init; }

  public required string Type { get; init; }

  public required BoundsInfo Bounds { get; init; }

  public required bool IsActive { get; init; }

  public required bool IsVisible { get; init; }
}
