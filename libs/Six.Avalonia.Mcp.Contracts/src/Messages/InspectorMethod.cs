namespace Six.Avalonia.Mcp.Contracts.Messages;

/// <summary>
/// Enum of all inspector methods that can be invoked remotely.
/// </summary>
public enum InspectorMethod
{
  GetAppMetadata,
  GetVisualTree,
  FindElement,
  GetElementProperties,
  GetAncestorChain,
  TakeScreenshot,
  SimulateClick,
  SimulateType,
  SetControlState,
  SimulateHover,
  SimulateDrag,
  SimulateKeyboardShortcut,
  ListWindows,
  SimulateScroll
}
