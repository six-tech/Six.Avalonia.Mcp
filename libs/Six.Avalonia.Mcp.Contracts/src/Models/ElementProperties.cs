namespace Six.Avalonia.Mcp.Contracts.Models;

/// <summary>
/// Detailed runtime properties of a visual element, returned by the get_element_properties tool.
/// </summary>
[PublicAPI]
[ExcludeFromCodeCoverage]
public sealed record ElementProperties
{
  // Identity
  public required string Type { get; init; }

  public string? Name { get; init; }

  public required string ElementId { get; init; }

  // Geometry
  public required BoundsInfo Bounds { get; init; }

  public required double Width { get; init; }

  public required double Height { get; init; }

  public string? Margin { get; init; }

  public string? Padding { get; init; }

  // State
  public required bool IsVisible { get; init; }

  public required bool IsEnabled { get; init; }

  public required bool IsFocused { get; init; }

  public required bool IsHitTestVisible { get; init; }

  public required bool ClipToBounds { get; init; }

  public required double Opacity { get; init; }

  // Appearance (string representations)
  public string? Background { get; init; }

  public string? Foreground { get; init; }

  public string? BorderBrush { get; init; }

  public string? BorderThickness { get; init; }

  public string? CornerRadius { get; init; }

  // Layout
  public string? HorizontalAlignment { get; init; }

  public string? VerticalAlignment { get; init; }

  public string? FlowDirection { get; init; }

  // Data
  public string? DataContextType { get; init; }

  // Text content
  public string? Text { get; init; }

  // Styles
  public required IReadOnlyList<string> Classes { get; init; }

  public required IReadOnlyList<string> PseudoClasses { get; init; }
}
