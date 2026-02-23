using Avalonia.Input;
using Six.Avalonia.Mcp.Connector.Services;

namespace Six.Avalonia.Mcp.Tests.Unit.Services;

public class ParseKeyCombinationTests
{
  [Theory]
  [InlineData("Ctrl+S", KeyModifiers.Control, Key.S)]
  [InlineData("ctrl+s", KeyModifiers.Control, Key.S)]
  [InlineData("CTRL+S", KeyModifiers.Control, Key.S)]
  [InlineData("Ctrl+Shift+Z", KeyModifiers.Control | KeyModifiers.Shift, Key.Z)]
  [InlineData("Alt+F4", KeyModifiers.Alt, Key.F4)]
  [InlineData("Meta+A", KeyModifiers.Meta, Key.A)]
  [InlineData("Ctrl+Shift+Alt+Delete", KeyModifiers.Control | KeyModifiers.Shift | KeyModifiers.Alt, Key.Delete)]
  [InlineData("Ctrl+Shift+Alt+S", KeyModifiers.Control | KeyModifiers.Shift | KeyModifiers.Alt, Key.S)]
  [InlineData("Enter", KeyModifiers.None, Key.Enter)]
  [InlineData("Escape", KeyModifiers.None, Key.Escape)]
  [InlineData("Tab", KeyModifiers.None, Key.Tab)]
  [InlineData("F1", KeyModifiers.None, Key.F1)]
  [InlineData("F5", KeyModifiers.None, Key.F5)]
  [InlineData("F12", KeyModifiers.None, Key.F12)]
  [InlineData("Space", KeyModifiers.None, Key.Space)]
  public void ParseKeyCombination_ParsesValidCombinations(string keys, KeyModifiers expectedModifiers, Key expectedKey)
  {
    (KeyModifiers modifiers, Key key) = AvaloniaInspector.ParseKeyCombination(keys);

    modifiers.Should().Be(expectedModifiers);
    key.Should().Be(expectedKey);
  }

  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  public void ParseKeyCombination_ThrowsOnEmptyInput(string keys)
  {
    Func<(KeyModifiers Modifiers, Key PrimaryKey)> act = () => AvaloniaInspector.ParseKeyCombination(keys);

    act.Should().Throw<ArgumentException>().WithMessage("*empty*");
  }

  [Fact]
  public void ParseKeyCombination_ThrowsOnModifiersOnly()
  {
    Func<(KeyModifiers Modifiers, Key PrimaryKey)> act = () => AvaloniaInspector.ParseKeyCombination("Ctrl+Shift");

    act.Should().Throw<ArgumentException>().WithMessage("*No primary key*");
  }

  [Fact]
  public void ParseKeyCombination_ThrowsOnSingleModifierOnly()
  {
    Func<(KeyModifiers Modifiers, Key PrimaryKey)> act = () => AvaloniaInspector.ParseKeyCombination("Ctrl");

    act.Should().Throw<ArgumentException>().WithMessage("*No primary key*");
  }

  [Fact]
  public void ParseKeyCombination_ThrowsOnUnknownKey()
  {
    Func<(KeyModifiers Modifiers, Key PrimaryKey)> act = () => AvaloniaInspector.ParseKeyCombination("Ctrl+NotAKey");

    act.Should().Throw<ArgumentException>().WithMessage("*Unknown key*");
  }
}

public class MapMouseButtonTests
{
  [Theory]
  [InlineData("left", MouseButton.Left)]
  [InlineData("Left", MouseButton.Left)]
  [InlineData("LEFT", MouseButton.Left)]
  [InlineData("right", MouseButton.Right)]
  [InlineData("middle", MouseButton.Middle)]
  public void MapMouseButton_MapsValidButtons(string input, MouseButton expected)
  {
    (MouseButton button, _, _) = AvaloniaInspector.MapMouseButton(input);
    button.Should().Be(expected);
  }

  [Fact]
  public void MapMouseButton_ReturnsCorrectPressedKinds()
  {
    (_, PointerUpdateKind pressed, PointerUpdateKind released) = AvaloniaInspector.MapMouseButton("left");
    pressed.Should().Be(PointerUpdateKind.LeftButtonPressed);
    released.Should().Be(PointerUpdateKind.LeftButtonReleased);
  }

  [Fact]
  public void MapMouseButton_ThrowsOnUnknownButton()
  {
    Func<(MouseButton Button, PointerUpdateKind Pressed, PointerUpdateKind Released)> act = () =>
      AvaloniaInspector.MapMouseButton("xbutton");

    act.Should().Throw<ArgumentException>().WithMessage("*Unknown mouse button*");
  }
}

public class ParseModifiersTests
{
  [Theory]
  [InlineData(null, KeyModifiers.None)]
  [InlineData("", KeyModifiers.None)]
  [InlineData("ctrl", KeyModifiers.Control)]
  [InlineData("Ctrl", KeyModifiers.Control)]
  [InlineData("control", KeyModifiers.Control)]
  [InlineData("shift", KeyModifiers.Shift)]
  [InlineData("alt", KeyModifiers.Alt)]
  [InlineData("meta", KeyModifiers.Meta)]
  [InlineData("win", KeyModifiers.Meta)]
  [InlineData("cmd", KeyModifiers.Meta)]
  [InlineData("ctrl+shift", KeyModifiers.Control | KeyModifiers.Shift)]
  [InlineData("ctrl+shift+alt", KeyModifiers.Control | KeyModifiers.Shift | KeyModifiers.Alt)]
  public void ParseModifiers_ParsesValidModifiers(string? input, KeyModifiers expected)
  {
    (KeyModifiers keyMods, _) = AvaloniaInspector.ParseModifiers(input);
    keyMods.Should().Be(expected);
  }

  [Fact]
  public void ParseModifiers_SetsRawModifiersCorrectly()
  {
    (_, RawInputModifiers rawMods) = AvaloniaInspector.ParseModifiers("ctrl+shift");
    rawMods.Should().HaveFlag(RawInputModifiers.Control);
    rawMods.Should().HaveFlag(RawInputModifiers.Shift);
  }

  [Fact]
  public void ParseModifiers_ThrowsOnUnknownModifier()
  {
    Func<(KeyModifiers Key, RawInputModifiers Raw)> act = () => AvaloniaInspector.ParseModifiers("ctrl+unknown");

    act.Should().Throw<ArgumentException>().WithMessage("*Unknown modifier*");
  }
}
