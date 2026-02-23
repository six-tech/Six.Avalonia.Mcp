namespace Six.Avalonia.Mcp.Tests.Unit.Models;

using Six.Avalonia.Mcp.Contracts;
using Six.Avalonia.Mcp.Contracts.Models;

public sealed class VisualTreeNodeTests
{
  private static BoundsInfo SampleBounds => new() { X = 0, Y = 0, Width = 100, Height = 50 };

  private static VisualTreeNode CreateLeaf(string type = "Button", string? name = "btn1")
  {
    return new VisualTreeNode
    {
      Type = type,
      Name = name,
      Bounds = SampleBounds,
      IsVisible = true,
      IsEnabled = true,
      Opacity = 1.0,
      IsFocused = false,
      IsPointerOver = false,
      Classes = ["default"],
      Children = []
    };
  }

  [Fact]
  public void Construction_SetsAllProperties()
  {
    VisualTreeNode node = CreateLeaf();

    node.Type.Should().Be("Button");
    node.Name.Should().Be("btn1");
    node.Bounds.Should().Be(SampleBounds);
    node.IsVisible.Should().BeTrue();
    node.IsEnabled.Should().BeTrue();
    node.Opacity.Should().Be(1.0);
    node.IsFocused.Should().BeFalse();
    node.IsPointerOver.Should().BeFalse();
    node.Classes.Should().ContainSingle().Which.Should().Be("default");
    node.Children.Should().BeEmpty();
  }

  [Fact]
  public void JsonRoundTrip_PreservesAllValues()
  {
    VisualTreeNode original = CreateLeaf();

    string json = JsonSerializer.Serialize(original, JsonOptions.Default);
    VisualTreeNode? deserialized = JsonSerializer.Deserialize<VisualTreeNode>(json, JsonOptions.Default);

    deserialized.Should().BeEquivalentTo(original);
  }

  [Fact]
  public void JsonRoundTrip_PreservesNestedChildren()
  {
    VisualTreeNode child = CreateLeaf("TextBlock", "label1");
    VisualTreeNode parent = CreateLeaf("StackPanel", "panel1") with { Children = [child] };

    string json = JsonSerializer.Serialize(parent, JsonOptions.Default);
    VisualTreeNode? deserialized = JsonSerializer.Deserialize<VisualTreeNode>(json, JsonOptions.Default);

    deserialized!.Children.Should().HaveCount(1);
    deserialized.Children[0].Should().BeEquivalentTo(child);
  }

  [Fact]
  public void JsonSerialization_UsesCamelCasePropertyNames()
  {
    VisualTreeNode node = CreateLeaf();

    string json = JsonSerializer.Serialize(node, JsonOptions.Default);

    json.Should().Contain("\"type\":");
    json.Should().Contain("\"isVisible\":");
    json.Should().Contain("\"isEnabled\":");
    json.Should().Contain("\"opacity\":");
    json.Should().Contain("\"isFocused\":");
    json.Should().Contain("\"isPointerOver\":");
    json.Should().Contain("\"classes\":");
    json.Should().Contain("\"children\":");
  }

  [Fact]
  public void NullName_OmittedFromJson()
  {
    VisualTreeNode node = CreateLeaf() with { Name = null };

    string json = JsonSerializer.Serialize(node, JsonOptions.Default);

    json.Should().NotContain("\"name\"");
  }

  [Fact]
  public void EmptyClasses_SerializesAsEmptyArray()
  {
    VisualTreeNode node = CreateLeaf() with { Classes = [] };

    string json = JsonSerializer.Serialize(node, JsonOptions.Default);
    VisualTreeNode? deserialized = JsonSerializer.Deserialize<VisualTreeNode>(json, JsonOptions.Default);

    deserialized!.Classes.Should().BeEmpty();
  }
}