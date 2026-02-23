using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using Six.Avalonia.Mcp.Contracts;
using Six.Avalonia.Mcp.Contracts.Messages;
using Six.Avalonia.Mcp.Contracts.Models;
using Six.Avalonia.Mcp.Server.Services;

namespace Six.Avalonia.Mcp.Server.Tools;

/// <summary>
/// MCP tools for capturing screenshots of Avalonia visual elements.
/// </summary>
[McpServerToolType]
public static class ScreenshotTools
{
  [McpServerTool(Name = "take_screenshot")]
  [Description(
    "Capture a screenshot of the main window or a specific named element as a PNG file. Returns the file path, width, and height. Optionally specify an output path and scale factor (0.5\u20133.0, default 1.0).")]
  public static async Task<string> TakeScreenshot(
    IInspectorProxy proxy,
    IAppRegistry appRegistry,
    [Description("File path where the PNG should be saved. If not provided, saves to a temp directory.")]
    string? outputPath = null,
    [Description("Name of the element to capture. If not provided, captures the full window.")]
    string? elementName = null,
    [Description("Render scale factor (0.5\u20133.0, default 1.0).")]
    double scaleFactor = 1.0,
    [Description("Zero-based window index from list_windows. If omitted, uses the active window.")]
    int? windowIndex = null,
    [Description("When true (default), captures all windows composited into a single image with dialogs overlaid on the main window. Ignores windowIndex and elementName. Set to false to capture only the active window.")]
    bool composite = true,
    string? appId = null,
    CancellationToken ct = default)
  {
    try
    {
      string? screenshotDir = appRegistry.GetAppInfo(appId)?.ScreenshotDirectory;
      string resolvedPath = outputPath ?? GenerateDefaultPath(appId, screenshotDir);

      Dictionary<string, object?> parameters = new()
      {
        ["outputPath"] = resolvedPath,
        ["elementName"] = elementName,
        ["scaleFactor"] = scaleFactor,
        ["windowIndex"] = windowIndex,
        ["composite"] = composite
      };
      InspectorResponse response = await proxy.SendCommandAsync(InspectorMethod.TakeScreenshot, parameters, appId, ct);

      if(!response.Success)
      {
        return JsonSerializer.Serialize(new { error = response.ErrorMessage }, JsonOptions.Default);
      }

      ScreenshotResult? result = JsonSerializer.Deserialize<ScreenshotResult>(response.ResultJson!, JsonOptions.Default);
      if(result is null || !result.Success)
      {
        return JsonSerializer.Serialize(new { error = result?.Error ?? "Unknown error" }, JsonOptions.Default);
      }

      return JsonSerializer.Serialize(
        new { filePath = result.FilePath, width = result.Width, height = result.Height },
        JsonOptions.Default);
    }
    catch(Exception ex)
    {
      return JsonSerializer.Serialize(new { error = ex.Message }, JsonOptions.Default);
    }
  }

  public static string GenerateDefaultPath(string? appId, string? screenshotDirectory = null)
  {
    string screenshotDir = screenshotDirectory ?? Path.Combine(Directory.GetCurrentDirectory(), "screenshots");
    string timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
    string prefix = appId ?? "app";
    return Path.Combine(screenshotDir, $"{prefix}-{timestamp}.png");
  }
}
