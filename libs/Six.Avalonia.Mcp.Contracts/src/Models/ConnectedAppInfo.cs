namespace Six.Avalonia.Mcp.Contracts.Models;

/// <summary>
/// Registration DTO sent by an Avalonia app when it connects to the MCP server hub.
/// </summary>
[PublicAPI]
[ExcludeFromCodeCoverage]
public sealed record ConnectedAppInfo
{
  public required string AppId { get; init; }

  public required string AppName { get; init; }

  public required int ProcessId { get; init; }

  public required string ProcessName { get; init; }

  public required DateTimeOffset ConnectedAt { get; init; }

  public string? ScreenshotDirectory { get; init; }
}
