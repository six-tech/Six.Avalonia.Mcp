using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using Six.Avalonia.Mcp.Contracts;
using Six.Avalonia.Mcp.Contracts.Messages;
using Six.Avalonia.Mcp.Server.Services;

namespace Six.Avalonia.Mcp.Server.Tools;

/// <summary>
/// MCP tools for simulating user interactions with Avalonia controls.
/// </summary>
[McpServerToolType]
public static class InteractionTools
{
  [McpServerTool(Name = "simulate_click")]
  [Description(
    "Simulate a mouse click (left, right, or middle) on a named element. Optionally specify x/y offset within the element, modifier keys (ctrl, shift, alt), and click count. Right-click dispatches ContextRequestedEvent for context menus.")]
  public static async Task<string> SimulateClick(
    IInspectorProxy proxy,
    string elementName,
    string button = "left",
    double? x = null,
    double? y = null,
    string? modifiers = null,
    int clickCount = 1,
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
        ["button"] = button,
        ["x"] = x,
        ["y"] = y,
        ["modifiers"] = modifiers,
        ["clickCount"] = clickCount,
        ["windowIndex"] = windowIndex
      };
      InspectorResponse response = await proxy.SendCommandAsync(InspectorMethod.SimulateClick, parameters, appId, ct);
      return response.Success ? response.ResultJson! : JsonSerializer.Serialize(new { error = response.ErrorMessage }, JsonOptions.Default);
    }
    catch(Exception ex)
    {
      return JsonSerializer.Serialize(new { error = ex.Message }, JsonOptions.Default);
    }
  }

  [McpServerTool(Name = "simulate_type")]
  [Description(
    "Simulate typing text into a named element character by character. The element is focused first. Special characters: \\b=Backspace, \\n=Enter, \\t=Tab.")]
  public static async Task<string> SimulateType(
    IInspectorProxy proxy,
    string elementName,
    string text,
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
        ["text"] = text,
        ["windowIndex"] = windowIndex
      };
      InspectorResponse response = await proxy.SendCommandAsync(InspectorMethod.SimulateType, parameters, appId, ct);
      return response.Success ? response.ResultJson! : JsonSerializer.Serialize(new { error = response.ErrorMessage }, JsonOptions.Default);
    }
    catch(Exception ex)
    {
      return JsonSerializer.Serialize(new { error = ex.Message }, JsonOptions.Default);
    }
  }

  [McpServerTool(Name = "set_control_state")]
  [Description(
    "Set a control state property on a named element. State types: 'focus' (focus/blur), 'enabled' (enable/disable), 'visible' (show/hide). Value is true to enable the state or false to disable it.")]
  public static async Task<string> SetControlState(
    IInspectorProxy proxy,
    string elementName,
    string stateType,
    bool value,
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
        ["stateType"] = stateType,
        ["value"] = value,
        ["windowIndex"] = windowIndex
      };
      InspectorResponse response = await proxy.SendCommandAsync(InspectorMethod.SetControlState, parameters, appId, ct);
      return response.Success ? response.ResultJson! : JsonSerializer.Serialize(new { error = response.ErrorMessage }, JsonOptions.Default);
    }
    catch(Exception ex)
    {
      return JsonSerializer.Serialize(new { error = ex.Message }, JsonOptions.Default);
    }
  }

  [McpServerTool(Name = "simulate_hover")]
  [Description(
    "Simulate a pointer hover (PointerEntered + PointerMoved) on a named element. Set exitAfter to true to also dispatch PointerExited.")]
  public static async Task<string> SimulateHover(
    IInspectorProxy proxy,
    string elementName,
    bool exitAfter = false,
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
        ["exitAfter"] = exitAfter,
        ["windowIndex"] = windowIndex
      };
      InspectorResponse response = await proxy.SendCommandAsync(InspectorMethod.SimulateHover, parameters, appId, ct);
      return response.Success ? response.ResultJson! : JsonSerializer.Serialize(new { error = response.ErrorMessage }, JsonOptions.Default);
    }
    catch(Exception ex)
    {
      return JsonSerializer.Serialize(new { error = ex.Message }, JsonOptions.Default);
    }
  }

  [McpServerTool(Name = "simulate_drag")]
  [Description(
    "Simulate a drag operation on a named element from (startX, startY) to (endX, endY). Dispatches PointerPressed, intermediate PointerMoved events (controlled by steps), and PointerReleased.")]
  public static async Task<string> SimulateDrag(
    IInspectorProxy proxy,
    string elementName,
    double startX,
    double startY,
    double endX,
    double endY,
    string button = "left",
    int steps = 10,
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
        ["startX"] = startX,
        ["startY"] = startY,
        ["endX"] = endX,
        ["endY"] = endY,
        ["button"] = button,
        ["steps"] = steps,
        ["windowIndex"] = windowIndex
      };
      InspectorResponse response = await proxy.SendCommandAsync(InspectorMethod.SimulateDrag, parameters, appId, ct);
      return response.Success ? response.ResultJson! : JsonSerializer.Serialize(new { error = response.ErrorMessage }, JsonOptions.Default);
    }
    catch(Exception ex)
    {
      return JsonSerializer.Serialize(new { error = ex.Message }, JsonOptions.Default);
    }
  }

  [McpServerTool(Name = "simulate_keyboard_shortcut")]
  [Description(
    "Simulate a keyboard shortcut (e.g. 'Ctrl+S', 'Alt+F4', 'F5') optionally targeted at a named element. If no element is specified, uses the currently focused element or the main window.")]
  public static async Task<string> SimulateKeyboardShortcut(
    IInspectorProxy proxy,
    string keys,
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
        ["keys"] = keys,
        ["elementName"] = elementName,
        ["windowIndex"] = windowIndex
      };
      InspectorResponse response = await proxy.SendCommandAsync(InspectorMethod.SimulateKeyboardShortcut, parameters, appId, ct);
      return response.Success ? response.ResultJson! : JsonSerializer.Serialize(new { error = response.ErrorMessage }, JsonOptions.Default);
    }
    catch(Exception ex)
    {
      return JsonSerializer.Serialize(new { error = ex.Message }, JsonOptions.Default);
    }
  }

  [McpServerTool(Name = "simulate_scroll")]
  [Description(
    "Simulate a mouse wheel scroll on a named element. deltaY < 0 scrolls down, deltaY > 0 scrolls up. deltaX for horizontal scrolling. Optionally specify modifier keys (ctrl, shift, alt).")]
  public static async Task<string> SimulateScroll(
    IInspectorProxy proxy,
    string elementName,
    double deltaX = 0,
    double deltaY = -120,
    string? modifiers = null,
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
        ["deltaX"] = deltaX,
        ["deltaY"] = deltaY,
        ["modifiers"] = modifiers,
        ["windowIndex"] = windowIndex
      };
      InspectorResponse response = await proxy.SendCommandAsync(InspectorMethod.SimulateScroll, parameters, appId, ct);
      return response.Success ? response.ResultJson! : JsonSerializer.Serialize(new { error = response.ErrorMessage }, JsonOptions.Default);
    }
    catch(Exception ex)
    {
      return JsonSerializer.Serialize(new { error = ex.Message }, JsonOptions.Default);
    }
  }
}
