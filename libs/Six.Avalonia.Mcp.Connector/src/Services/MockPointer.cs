using Avalonia.Input;

namespace Six.Avalonia.Mcp.Connector.Services;

/// <summary>
/// Factory for creating Avalonia <see cref="Pointer"/> instances used in interaction simulation.
/// Avalonia's <see cref="IPointer"/> is not user-implementable, so we use the built-in
/// <see cref="Pointer"/> class with a fixed id of 0 and mouse pointer type.
/// </summary>
internal static class MockPointer
{
  /// <summary>
  /// Creates a new <see cref="Pointer"/> instance suitable for simulating pointer events.
  /// </summary>
  internal static Pointer Create()
  {
    return new Pointer(0, PointerType.Mouse, true);
  }
}
