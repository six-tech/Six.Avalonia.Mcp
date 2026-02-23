using Six.Avalonia.Mcp.Contracts.Models;

namespace Six.Avalonia.Mcp.Connector.Services;

/// <summary>
/// Service for inspecting and interacting with a running Avalonia application's visual tree.
/// </summary>
public interface IAvaloniaInspector
{
  // Discovery

  /// <summary>
  /// Gets metadata about the running application (process info, window dimensions, versions).
  /// </summary>
  Task<AppMetadata> GetAppMetadataAsync(CancellationToken ct = default);

  /// <summary>
  /// Lists all open windows (TopLevels) in the application with their index, title, type, bounds, and state.
  /// </summary>
  Task<IReadOnlyList<WindowInfo>> ListWindowsAsync(CancellationToken ct = default);

  // Inspection

  /// <summary>
  /// Builds a recursive snapshot of the visual tree starting from the main window or a named element.
  /// </summary>
  Task<VisualTreeNode> GetVisualTreeAsync(int maxDepth = 15, string? elementName = null,
    int? windowIndex = null, CancellationToken ct = default);

  /// <summary>
  /// Finds elements in the visual tree matching the specified criteria.
  /// </summary>
  Task<IReadOnlyList<FindElementResult>> FindElementAsync(
    string? name = null,
    string? typeName = null,
    string? className = null,
    string? selector = null,
    int maxResults = 10,
    int? windowIndex = null,
    CancellationToken ct = default);

  /// <summary>
  /// Returns the ancestor chain from a named element up to the root.
  /// </summary>
  Task<IReadOnlyList<VisualTreeNode>> GetAncestorChainAsync(string elementName,
    int? windowIndex = null, CancellationToken ct = default);

  /// <summary>
  /// Gets detailed runtime properties of a specific element resolved by name, type, or path.
  /// </summary>
  Task<ElementProperties> GetElementPropertiesAsync(
    string? elementName = null,
    string? typeName = null,
    string? path = null,
    int? windowIndex = null,
    CancellationToken ct = default);

  // Screenshots

  /// <summary>
  /// Captures a screenshot of the main window or a specific element and saves it as a PNG file.
  /// </summary>
  Task<ScreenshotResult> TakeScreenshotAsync(
    string outputPath,
    string? elementName = null,
    double scaleFactor = 1.0,
    int? windowIndex = null,
    bool composite = true,
    CancellationToken ct = default);

  // Interactions

  /// <summary>
  /// Simulates a mouse click on the specified element.
  /// </summary>
  Task<InteractionResult> SimulateClickAsync(
    string elementName,
    string button = "left",
    double? x = null,
    double? y = null,
    string? modifiers = null,
    int clickCount = 1,
    int? windowIndex = null,
    CancellationToken ct = default);

  /// <summary>
  /// Simulates typing text into the specified element character by character.
  /// </summary>
  Task<InteractionResult> SimulateTypeAsync(string elementName, string text,
    int? windowIndex = null, CancellationToken ct = default);

  /// <summary>
  /// Sets a control state property (focus, enabled, visible) on the specified element.
  /// </summary>
  Task<InteractionResult> SetControlStateAsync(string elementName, string stateType, bool value,
    int? windowIndex = null, CancellationToken ct = default);

  /// <summary>
  /// Simulates a pointer hover (PointerEntered) on the specified element.
  /// </summary>
  Task<InteractionResult> SimulateHoverAsync(string elementName, bool exitAfter = false,
    int? windowIndex = null, CancellationToken ct = default);

  /// <summary>
  /// Simulates a drag operation from start to end coordinates on the specified element.
  /// </summary>
  Task<InteractionResult> SimulateDragAsync(
    string elementName,
    double startX,
    double startY,
    double endX,
    double endY,
    string button = "left",
    int steps = 10,
    int? windowIndex = null,
    CancellationToken ct = default);

  /// <summary>
  /// Simulates a keyboard shortcut (e.g., "Ctrl+S") optionally targeted at a specific element.
  /// </summary>
  Task<InteractionResult> SimulateKeyboardShortcutAsync(string keys, string? elementName = null,
    int? windowIndex = null, CancellationToken ct = default);

  /// <summary>
  /// Simulates a mouse wheel scroll on the specified element.
  /// </summary>
  Task<InteractionResult> SimulateScrollAsync(
    string elementName,
    double deltaX = 0,
    double deltaY = -120,
    string? modifiers = null,
    int? windowIndex = null,
    CancellationToken ct = default);
}
