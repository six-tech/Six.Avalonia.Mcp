namespace Six.Avalonia.Mcp.Tests.Unit.Models;

using Six.Avalonia.Mcp.Contracts;
using Six.Avalonia.Mcp.Contracts.Models;

public sealed class BoundsInfoTests
{
  [Fact]
  public void Construction_SetsAllProperties()
  {
    BoundsInfo bounds = new() { X = 10.5, Y = 20.3, Width = 100, Height = 200 };

    bounds.X.Should().Be(10.5);
    bounds.Y.Should().Be(20.3);
    bounds.Width.Should().Be(100);
    bounds.Height.Should().Be(200);
  }

  [Fact]
  public void JsonRoundTrip_PreservesAllValues()
  {
    BoundsInfo original = new() { X = 1.5, Y = 2.5, Width = 300.7, Height = 400.9 };

    string json = JsonSerializer.Serialize(original, JsonOptions.Default);
    BoundsInfo? deserialized = JsonSerializer.Deserialize<BoundsInfo>(json, JsonOptions.Default);

    deserialized.Should().BeEquivalentTo(original);
  }

  [Fact]
  public void JsonSerialization_UsesCamelCasePropertyNames()
  {
    BoundsInfo bounds = new() { X = 0, Y = 0, Width = 10, Height = 20 };

    string json = JsonSerializer.Serialize(bounds, JsonOptions.Default);

    json.Should().Contain("\"x\":");
    json.Should().Contain("\"y\":");
    json.Should().Contain("\"width\":");
    json.Should().Contain("\"height\":");
  }

  [Fact]
  public void RecordEquality_WorksCorrectly()
  {
    BoundsInfo a = new() { X = 1, Y = 2, Width = 3, Height = 4 };
    BoundsInfo b = new() { X = 1, Y = 2, Width = 3, Height = 4 };

    a.Should().Be(b);
  }

  [Fact]
  public void With_CreatesModifiedCopy()
  {
    BoundsInfo original = new() { X = 1, Y = 2, Width = 3, Height = 4 };
    BoundsInfo modified = original with { Width = 99 };

    modified.Width.Should().Be(99);
    original.Width.Should().Be(3);
  }
}