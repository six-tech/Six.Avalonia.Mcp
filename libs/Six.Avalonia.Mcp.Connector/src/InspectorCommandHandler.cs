using System.Text.Json;
using Six.Avalonia.Mcp.Contracts;
using Six.Avalonia.Mcp.Contracts.Messages;
using Six.Avalonia.Mcp.Connector.Services;

namespace Six.Avalonia.Mcp.Connector;

/// <summary>
/// Handles incoming inspector commands from the MCP server by dispatching to the local <see cref="IAvaloniaInspector"/>.
/// </summary>
[PublicAPI]
public class InspectorCommandHandler
{
  private readonly IAvaloniaInspector _inspector;
  private readonly ILogger<InspectorCommandHandler> _logger;

  public InspectorCommandHandler(IAvaloniaInspector inspector, ILogger<InspectorCommandHandler> logger)
  {
    _inspector = inspector;
    _logger = logger;
  }

  /// <summary>
  /// Dispatches an <see cref="InspectorRequest"/> to the appropriate inspector method and returns the response.
  /// </summary>
  public async Task<InspectorResponse> HandleAsync(InspectorRequest request, CancellationToken ct = default)
  {
    try
    {
      Dictionary<string, JsonElement>? parameters =
        JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(request.ParametersJson, JsonOptions.Default);

      int? windowIndex = GetParam<int?>(parameters, "windowIndex", null);

      string resultJson = request.Method switch
      {
        InspectorMethod.GetAppMetadata => JsonSerializer.Serialize(
          await _inspector.GetAppMetadataAsync(ct), JsonOptions.Default),

        InspectorMethod.ListWindows => JsonSerializer.Serialize(
          await _inspector.ListWindowsAsync(ct), JsonOptions.Default),

        InspectorMethod.GetVisualTree => JsonSerializer.Serialize(
          await _inspector.GetVisualTreeAsync(
            GetParam<int>(parameters, "maxDepth", 15),
            GetParam<string?>(parameters, "elementName", null),
            windowIndex,
            ct), JsonOptions.Default),

        InspectorMethod.FindElement => JsonSerializer.Serialize(
          await _inspector.FindElementAsync(
            GetParam<string?>(parameters, "name", null),
            GetParam<string?>(parameters, "typeName", null),
            GetParam<string?>(parameters, "className", null),
            GetParam<string?>(parameters, "selector", null),
            GetParam<int>(parameters, "maxResults", 10),
            windowIndex,
            ct), JsonOptions.Default),

        InspectorMethod.GetElementProperties => JsonSerializer.Serialize(
          await _inspector.GetElementPropertiesAsync(
            GetParam<string?>(parameters, "elementName", null),
            GetParam<string?>(parameters, "typeName", null),
            GetParam<string?>(parameters, "path", null),
            windowIndex,
            ct), JsonOptions.Default),

        InspectorMethod.GetAncestorChain => JsonSerializer.Serialize(
          await _inspector.GetAncestorChainAsync(
            GetParam<string>(parameters, "elementName", null!) ?? throw new ArgumentException("elementName is required"),
            windowIndex,
            ct), JsonOptions.Default),

        InspectorMethod.TakeScreenshot => JsonSerializer.Serialize(
          await _inspector.TakeScreenshotAsync(
            GetParam<string>(parameters, "outputPath", null!) ?? throw new ArgumentException("outputPath is required"),
            GetParam<string?>(parameters, "elementName", null),
            GetParam<double>(parameters, "scaleFactor", 1.0),
            windowIndex,
            GetParam<bool>(parameters, "composite", true),
            ct), JsonOptions.Default),

        InspectorMethod.SimulateClick => JsonSerializer.Serialize(
          await _inspector.SimulateClickAsync(
            GetParam<string>(parameters, "elementName", null!) ?? throw new ArgumentException("elementName is required"),
            GetParam<string>(parameters, "button", "left")!,
            GetParam<double?>(parameters, "x", null),
            GetParam<double?>(parameters, "y", null),
            GetParam<string?>(parameters, "modifiers", null),
            GetParam<int>(parameters, "clickCount", 1),
            windowIndex,
            ct), JsonOptions.Default),

        InspectorMethod.SimulateType => JsonSerializer.Serialize(
          await _inspector.SimulateTypeAsync(
            GetParam<string>(parameters, "elementName", null!) ?? throw new ArgumentException("elementName is required"),
            GetParam<string>(parameters, "text", null!) ?? throw new ArgumentException("text is required"),
            windowIndex,
            ct), JsonOptions.Default),

        InspectorMethod.SetControlState => JsonSerializer.Serialize(
          await _inspector.SetControlStateAsync(
            GetParam<string>(parameters, "elementName", null!) ?? throw new ArgumentException("elementName is required"),
            GetParam<string>(parameters, "stateType", null!) ?? throw new ArgumentException("stateType is required"),
            GetParam<bool>(parameters, "value", false),
            windowIndex,
            ct), JsonOptions.Default),

        InspectorMethod.SimulateHover => JsonSerializer.Serialize(
          await _inspector.SimulateHoverAsync(
            GetParam<string>(parameters, "elementName", null!) ?? throw new ArgumentException("elementName is required"),
            GetParam<bool>(parameters, "exitAfter", false),
            windowIndex,
            ct), JsonOptions.Default),

        InspectorMethod.SimulateDrag => JsonSerializer.Serialize(
          await _inspector.SimulateDragAsync(
            GetParam<string>(parameters, "elementName", null!) ?? throw new ArgumentException("elementName is required"),
            GetParam<double>(parameters, "startX", 0),
            GetParam<double>(parameters, "startY", 0),
            GetParam<double>(parameters, "endX", 0),
            GetParam<double>(parameters, "endY", 0),
            GetParam<string>(parameters, "button", "left")!,
            GetParam<int>(parameters, "steps", 10),
            windowIndex,
            ct), JsonOptions.Default),

        InspectorMethod.SimulateKeyboardShortcut => JsonSerializer.Serialize(
          await _inspector.SimulateKeyboardShortcutAsync(
            GetParam<string>(parameters, "keys", null!) ?? throw new ArgumentException("keys is required"),
            GetParam<string?>(parameters, "elementName", null),
            windowIndex,
            ct), JsonOptions.Default),

        InspectorMethod.SimulateScroll => JsonSerializer.Serialize(
          await _inspector.SimulateScrollAsync(
            GetParam<string>(parameters, "elementName", null!) ?? throw new ArgumentException("elementName is required"),
            GetParam<double>(parameters, "deltaX", 0),
            GetParam<double>(parameters, "deltaY", -120),
            GetParam<string?>(parameters, "modifiers", null),
            windowIndex,
            ct), JsonOptions.Default),

        _ => throw new ArgumentOutOfRangeException(nameof(request), $"Unknown method: {request.Method}")
      };

      return new InspectorResponse
      {
        RequestId = request.RequestId,
        Success = true,
        ResultJson = resultJson
      };
    }
    catch(Exception ex)
    {
      _logger.LogError(ex, "Error handling command {Method} (RequestId: {RequestId})", request.Method, request.RequestId);
      return new InspectorResponse
      {
        RequestId = request.RequestId,
        Success = false,
        ErrorMessage = ex.Message
      };
    }
  }

  private static T? GetParam<T>(Dictionary<string, JsonElement>? parameters, string key, T? defaultValue)
  {
    if(parameters is null || !parameters.TryGetValue(key, out JsonElement element))
    {
      return defaultValue;
    }

    if(element.ValueKind == JsonValueKind.Null)
    {
      return defaultValue;
    }

    return element.Deserialize<T>(JsonOptions.Default);
  }
}
