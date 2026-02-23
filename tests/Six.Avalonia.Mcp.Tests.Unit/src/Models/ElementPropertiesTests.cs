namespace Six.Avalonia.Mcp.Tests.Unit.Models;

using Six.Avalonia.Mcp.Contracts;
using Six.Avalonia.Mcp.Contracts.Models;

public sealed class ElementPropertiesTests
{
  private static ElementProperties CreateSample()
  {
    return new ElementProperties
    {
      // Identity
      Type = "TextBox",
      Name = "inputField",
      ElementId = "path:0/1/3",
      // Geometry
      Bounds = new BoundsInfo { X = 10, Y = 20, Width = 200, Height = 30 },
      Width = 200,
      Height = 30,
      Margin = "5,10,5,10",
      Padding = "2,2,2,2",
      // State
      IsVisible = true,
      IsEnabled = true,
      IsFocused = true,
      IsHitTestVisible = true,
      ClipToBounds = false,
      Opacity = 1.0,
      // Appearance
      Background = "#FFFFFF",
      Foreground = "#000000",
      BorderBrush = "#CCCCCC",
      BorderThickness = "1,1,1,1",
      CornerRadius = "4,4,4,4",
      // Layout
      HorizontalAlignment = "Stretch",
      VerticalAlignment = "Center",
      FlowDirection = "LeftToRight",
      // Data
      DataContextType = "MyViewModel",
      Text = "sample text",
      // Styles
      Classes = ["textbox", "focused"],
      PseudoClasses = [":focus", ":pointerover"]
    };
  }

  [Fact]
  public void Construction_SetsIdentityProperties()
  {
    ElementProperties props = CreateSample();

    props.Type.Should().Be("TextBox");
    props.Name.Should().Be("inputField");
    props.ElementId.Should().Be("path:0/1/3");
  }

  [Fact]
  public void Construction_SetsGeometryProperties()
  {
    ElementProperties props = CreateSample();

    props.Bounds.Width.Should().Be(200);
    props.Width.Should().Be(200);
    props.Height.Should().Be(30);
    props.Margin.Should().Be("5,10,5,10");
    props.Padding.Should().Be("2,2,2,2");
  }

  [Fact]
  public void Construction_SetsStateProperties()
  {
    ElementProperties props = CreateSample();

    props.IsVisible.Should().BeTrue();
    props.IsEnabled.Should().BeTrue();
    props.IsFocused.Should().BeTrue();
    props.IsHitTestVisible.Should().BeTrue();
    props.ClipToBounds.Should().BeFalse();
    props.Opacity.Should().Be(1.0);
  }

  [Fact]
  public void Construction_SetsAppearanceProperties()
  {
    ElementProperties props = CreateSample();

    props.Background.Should().Be("#FFFFFF");
    props.Foreground.Should().Be("#000000");
    props.BorderBrush.Should().Be("#CCCCCC");
    props.BorderThickness.Should().Be("1,1,1,1");
    props.CornerRadius.Should().Be("4,4,4,4");
  }

  [Fact]
  public void Construction_SetsLayoutProperties()
  {
    ElementProperties props = CreateSample();

    props.HorizontalAlignment.Should().Be("Stretch");
    props.VerticalAlignment.Should().Be("Center");
    props.FlowDirection.Should().Be("LeftToRight");
  }

  [Fact]
  public void Construction_SetsStyleProperties()
  {
    ElementProperties props = CreateSample();

    props.Classes.Should().BeEquivalentTo(new[] { "textbox", "focused" });
    props.PseudoClasses.Should().BeEquivalentTo(new[] { ":focus", ":pointerover" });
  }

  [Fact]
  public void JsonRoundTrip_PreservesAllValues()
  {
    ElementProperties original = CreateSample();

    string json = JsonSerializer.Serialize(original, JsonOptions.Default);
    ElementProperties? deserialized = JsonSerializer.Deserialize<ElementProperties>(json, JsonOptions.Default);

    deserialized.Should().BeEquivalentTo(original);
  }

  [Fact]
  public void NullOptionalProperties_OmittedFromJson()
  {
    ElementProperties props = CreateSample() with
    {
      Name = null,
      Margin = null,
      Padding = null,
      Background = null,
      Foreground = null,
      BorderBrush = null,
      BorderThickness = null,
      CornerRadius = null,
      HorizontalAlignment = null,
      VerticalAlignment = null,
      FlowDirection = null,
      DataContextType = null,
      Text = null
    };

    string json = JsonSerializer.Serialize(props, JsonOptions.Default);

    json.Should().NotContain("\"name\"");
    json.Should().NotContain("\"margin\"");
    json.Should().NotContain("\"padding\"");
    json.Should().NotContain("\"background\"");
    json.Should().NotContain("\"foreground\"");
    json.Should().NotContain("\"borderBrush\"");
    json.Should().NotContain("\"borderThickness\"");
    json.Should().NotContain("\"cornerRadius\"");
    json.Should().NotContain("\"horizontalAlignment\"");
    json.Should().NotContain("\"verticalAlignment\"");
    json.Should().NotContain("\"flowDirection\"");
    json.Should().NotContain("\"dataContextType\"");
    json.Should().NotContain("\"text\"");
  }

  [Fact]
  public void JsonSerialization_UsesCamelCasePropertyNames()
  {
    ElementProperties props = CreateSample();

    string json = JsonSerializer.Serialize(props, JsonOptions.Default);

    json.Should().Contain("\"elementId\":");
    json.Should().Contain("\"isHitTestVisible\":");
    json.Should().Contain("\"clipToBounds\":");
    json.Should().Contain("\"horizontalAlignment\":");
    json.Should().Contain("\"pseudoClasses\":");
  }

  [Fact]
  public void JsonDeserialization_HandlesAllNullOptionals()
  {
    ElementProperties original = CreateSample() with
    {
      Name = null,
      Margin = null,
      Padding = null,
      Background = null,
      Foreground = null,
      BorderBrush = null,
      BorderThickness = null,
      CornerRadius = null,
      HorizontalAlignment = null,
      VerticalAlignment = null,
      FlowDirection = null,
      DataContextType = null,
      Text = null
    };

    string json = JsonSerializer.Serialize(original, JsonOptions.Default);
    ElementProperties? deserialized = JsonSerializer.Deserialize<ElementProperties>(json, JsonOptions.Default);

    deserialized!.Name.Should().BeNull();
    deserialized.Margin.Should().BeNull();
    deserialized.Background.Should().BeNull();
    deserialized.DataContextType.Should().BeNull();
    deserialized.Text.Should().BeNull();
  }
}