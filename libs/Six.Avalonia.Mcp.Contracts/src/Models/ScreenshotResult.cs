namespace Six.Avalonia.Mcp.Contracts.Models;

/// <summary>
/// Result of a screenshot capture operation, including the file path where the image was saved.
/// </summary>
[PublicAPI]
[ExcludeFromCodeCoverage]
public sealed record ScreenshotResult
{
  public required bool Success { get; init; }

  public string? FilePath { get; init; }

  public int Width { get; init; }

  public int Height { get; init; }

  public string? Error { get; init; }
}
