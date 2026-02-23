namespace Six.Avalonia.Mcp.Contracts.Models;

/// <summary>
/// Result of an interaction tool (click, type, hover, drag, etc.) with optional post-interaction state.
/// </summary>
[PublicAPI]
[ExcludeFromCodeCoverage]
public sealed record InteractionResult
{
  public required bool Success { get; init; }

  public string? ErrorMessage { get; init; }

  public string? ElementType { get; init; }

  public string? ElementName { get; init; }

  public bool? IsFocused { get; init; }

  public bool? IsEnabled { get; init; }

  public bool? IsVisible { get; init; }

  public bool? IsPointerOver { get; init; }
}
