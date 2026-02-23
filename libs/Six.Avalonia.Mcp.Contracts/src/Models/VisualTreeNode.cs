namespace Six.Avalonia.Mcp.Contracts.Models;

/// <summary>
/// Recursive representation of a node in the Avalonia visual tree.
/// </summary>
[PublicAPI]
[ExcludeFromCodeCoverage]
public sealed record VisualTreeNode
{
  public required string Type { get; init; }

  public string? Name { get; init; }

  public required BoundsInfo Bounds { get; init; }

  public required bool IsVisible { get; init; }

  public required bool IsEnabled { get; init; }

  public required double Opacity { get; init; }

  public required bool IsFocused { get; init; }

  public required bool IsPointerOver { get; init; }

  public required IReadOnlyList<string> Classes { get; init; }

  public required IReadOnlyList<VisualTreeNode> Children { get; init; }
}
