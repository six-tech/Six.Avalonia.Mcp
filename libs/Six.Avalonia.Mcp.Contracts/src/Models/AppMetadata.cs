namespace Six.Avalonia.Mcp.Contracts.Models;

/// <summary>
/// Application process and version metadata returned by the get_app_metadata tool.
/// </summary>
[PublicAPI]
[ExcludeFromCodeCoverage]
public sealed record AppMetadata
{
  public required string ProcessName { get; init; }

  public required int ProcessId { get; init; }

  public string? MainWindowTitle { get; init; }

  public required string AvaloniaVersion { get; init; }

  public required string DotNetVersion { get; init; }

  public required int WindowCount { get; init; }

  public required double WindowWidth { get; init; }

  public required double WindowHeight { get; init; }
}
