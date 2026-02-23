using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using Six.Avalonia.Mcp.Contracts;
using Six.Avalonia.Mcp.Contracts.Messages;
using Six.Avalonia.Mcp.Server.Services;

namespace Six.Avalonia.Mcp.Server.Tools;

/// <summary>
/// MCP tools for retrieving application metadata.
/// </summary>
[McpServerToolType]
public static class MetadataTools
{
  [McpServerTool(Name = "get_app_metadata")]
  [Description(
    "Get metadata about the running Avalonia application including process info, window dimensions, and framework versions.")]
  public static async Task<string> GetAppMetadata(
    IInspectorProxy proxy,
    string? appId = null,
    CancellationToken ct = default)
  {
    try
    {
      InspectorResponse response = await proxy.SendCommandAsync(InspectorMethod.GetAppMetadata, null, appId, ct);
      return response.Success ? response.ResultJson! : JsonSerializer.Serialize(new { error = response.ErrorMessage }, JsonOptions.Default);
    }
    catch(Exception ex)
    {
      return JsonSerializer.Serialize(new { error = ex.Message }, JsonOptions.Default);
    }
  }
}
