using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using Six.Avalonia.Mcp.Contracts;
using Six.Avalonia.Mcp.Contracts.Messages;
using Six.Avalonia.Mcp.Server.Services;

namespace Six.Avalonia.Mcp.Server.Tools;

/// <summary>
/// MCP tools for inspecting the Avalonia visual tree.
/// </summary>
[McpServerToolType]
public static class InspectionTools
{
  [McpServerTool(Name = "list_windows")]
  [Description(
    "List all open windows (TopLevels) in the application. Returns each window's index, title, type, bounds, active state, and visibility. Use window indices with other tools to target specific windows.")]
  public static async Task<string> ListWindows(
    IInspectorProxy proxy,
    string? appId = null,
    CancellationToken ct = default)
  {
    try
    {
      Dictionary<string, object?> parameters = new();
      InspectorResponse response = await proxy.SendCommandAsync(InspectorMethod.ListWindows, parameters, appId, ct);
      return response.Success ? response.ResultJson! : JsonSerializer.Serialize(new { error = response.ErrorMessage }, JsonOptions.Default);
    }
    catch(Exception ex)
    {
      return JsonSerializer.Serialize(new { error = ex.Message }, JsonOptions.Default);
    }
  }

  [McpServerTool(Name = "get_visual_tree")]
  [Description(
    "Get a recursive snapshot of the visual tree starting from the main window or a named element. Returns type, name, bounds, visibility, classes, and children for each node.")]
  public static async Task<string> GetVisualTree(
    IInspectorProxy proxy,
    int maxDepth = 15,
    string? elementName = null,
    [Description("Zero-based window index from list_windows. If omitted, uses the active window.")]
    int? windowIndex = null,
    string? appId = null,
    CancellationToken ct = default)
  {
    try
    {
      Dictionary<string, object?> parameters = new()
      {
        ["maxDepth"] = maxDepth,
        ["elementName"] = elementName,
        ["windowIndex"] = windowIndex
      };
      InspectorResponse response = await proxy.SendCommandAsync(InspectorMethod.GetVisualTree, parameters, appId, ct);
      return response.Success ? response.ResultJson! : JsonSerializer.Serialize(new { error = response.ErrorMessage }, JsonOptions.Default);
    }
    catch(Exception ex)
    {
      return JsonSerializer.Serialize(new { error = ex.Message }, JsonOptions.Default);
    }
  }

  [McpServerTool(Name = "find_element")]
  [Description(
    "Find elements in the visual tree matching criteria: name, type name, CSS class, or a simple selector (e.g., 'Button#myBtn', '.error', '#myId'). Returns matching elements with their paths and IDs. When windowIndex is omitted, searches all windows.")]
  public static async Task<string> FindElement(
    IInspectorProxy proxy,
    string? name = null,
    string? typeName = null,
    string? className = null,
    string? selector = null,
    int maxResults = 10,
    [Description("Zero-based window index from list_windows. If omitted, searches all windows.")]
    int? windowIndex = null,
    string? appId = null,
    CancellationToken ct = default)
  {
    try
    {
      Dictionary<string, object?> parameters = new()
      {
        ["name"] = name,
        ["typeName"] = typeName,
        ["className"] = className,
        ["selector"] = selector,
        ["maxResults"] = maxResults,
        ["windowIndex"] = windowIndex
      };
      InspectorResponse response = await proxy.SendCommandAsync(InspectorMethod.FindElement, parameters, appId, ct);
      return response.Success ? response.ResultJson! : JsonSerializer.Serialize(new { error = response.ErrorMessage }, JsonOptions.Default);
    }
    catch(Exception ex)
    {
      return JsonSerializer.Serialize(new { error = ex.Message }, JsonOptions.Default);
    }
  }

  [McpServerTool(Name = "get_element_properties")]
  [Description(
    "Get detailed runtime properties of a specific element resolved by name, type, or index path. Returns identity, geometry, state, appearance, layout, data context, and style information.")]
  public static async Task<string> GetElementProperties(
    IInspectorProxy proxy,
    string? elementName = null,
    string? typeName = null,
    string? path = null,
    [Description("Zero-based window index from list_windows. If omitted, searches all windows.")]
    int? windowIndex = null,
    string? appId = null,
    CancellationToken ct = default)
  {
    try
    {
      Dictionary<string, object?> parameters = new()
      {
        ["elementName"] = elementName,
        ["typeName"] = typeName,
        ["path"] = path,
        ["windowIndex"] = windowIndex
      };
      InspectorResponse response = await proxy.SendCommandAsync(InspectorMethod.GetElementProperties, parameters, appId, ct);
      return response.Success ? response.ResultJson! : JsonSerializer.Serialize(new { error = response.ErrorMessage }, JsonOptions.Default);
    }
    catch(Exception ex)
    {
      return JsonSerializer.Serialize(new { error = ex.Message }, JsonOptions.Default);
    }
  }

  [McpServerTool(Name = "get_element_by_path")]
  [Description(
    "Get detailed runtime properties of an element by its index-based visual tree path (e.g., '0/3/1'). Each segment is a zero-based child index. Use get_visual_tree or find_element to discover paths.")]
  public static async Task<string> GetElementByPath(
    IInspectorProxy proxy,
    string path,
    [Description("Zero-based window index from list_windows. If omitted, searches all windows.")]
    int? windowIndex = null,
    string? appId = null,
    CancellationToken ct = default)
  {
    try
    {
      Dictionary<string, object?> parameters = new()
      {
        ["path"] = path,
        ["windowIndex"] = windowIndex
      };
      InspectorResponse response = await proxy.SendCommandAsync(InspectorMethod.GetElementProperties, parameters, appId, ct);
      return response.Success ? response.ResultJson! : JsonSerializer.Serialize(new { error = response.ErrorMessage }, JsonOptions.Default);
    }
    catch(Exception ex)
    {
      return JsonSerializer.Serialize(new { error = ex.Message }, JsonOptions.Default);
    }
  }

  [McpServerTool(Name = "get_ancestor_chain")]
  [Description(
    "Get the ancestor chain from a named element up to the root window. Each ancestor includes type, name, bounds, and state — no children are populated.")]
  public static async Task<string> GetAncestorChain(
    IInspectorProxy proxy,
    string elementName,
    [Description("Zero-based window index from list_windows. If omitted, searches all windows.")]
    int? windowIndex = null,
    string? appId = null,
    CancellationToken ct = default)
  {
    try
    {
      Dictionary<string, object?> parameters = new()
      {
        ["elementName"] = elementName,
        ["windowIndex"] = windowIndex
      };
      InspectorResponse response = await proxy.SendCommandAsync(InspectorMethod.GetAncestorChain, parameters, appId, ct);
      return response.Success ? response.ResultJson! : JsonSerializer.Serialize(new { error = response.ErrorMessage }, JsonOptions.Default);
    }
    catch(Exception ex)
    {
      return JsonSerializer.Serialize(new { error = ex.Message }, JsonOptions.Default);
    }
  }
}
