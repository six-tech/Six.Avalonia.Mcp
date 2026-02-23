namespace Six.Avalonia.Mcp.Tests.Unit.Models;

using Six.Avalonia.Mcp.Contracts;
using Six.Avalonia.Mcp.Contracts.Models;

public sealed class FindElementResultTests
{
  private static FindElementResult CreateSample()
  {
    return new FindElementResult
    {
      Type = "Button",
      Name = "submitBtn",
      Bounds = new BoundsInfo { X = 10, Y = 20, Width = 100, Height = 30 },
      Path = [0, 2, 1],
      ElementId = "path:0/2/1"
    };
  }

  [Fact]
  public void Construction_SetsAllProperties()
  {
    FindElementResult result = CreateSample();

    result.Type.Should().Be("Button");
    result.Name.Should().Be("submitBtn");
    result.Bounds.X.Should().Be(10);
    result.Path.Should().BeEquivalentTo(new[] { 0, 2, 1 });
    result.ElementId.Should().Be("path:0/2/1");
  }

  [Fact]
  public void JsonRoundTrip_PreservesAllValues()
  {
    FindElementResult original = CreateSample();

    string json = JsonSerializer.Serialize(original, JsonOptions.Default);
    FindElementResult? deserialized = JsonSerializer.Deserialize<FindElementResult>(json, JsonOptions.Default);

    deserialized.Should().BeEquivalentTo(original);
  }

  [Fact]
  public void JsonSerialization_UsesCamelCasePropertyNames()
  {
    FindElementResult result = CreateSample();

    string json = JsonSerializer.Serialize(result, JsonOptions.Default);

    json.Should().Contain("\"type\":");
    json.Should().Contain("\"name\":");
    json.Should().Contain("\"bounds\":");
    json.Should().Contain("\"path\":");
    json.Should().Contain("\"elementId\":");
  }

  [Fact]
  public void NullName_OmittedFromJson()
  {
    FindElementResult result = CreateSample() with { Name = null };

    string json = JsonSerializer.Serialize(result, JsonOptions.Default);

    json.Should().NotContain("\"name\"");
  }

  [Fact]
  public void ElementId_MatchesPathFormat()
  {
    FindElementResult result = CreateSample();

    result.ElementId.Should().StartWith("path:");
    result.ElementId.Should().Be($"path:{string.Join("/", result.Path)}");
  }

  [Fact]
  public void JsonRoundTrip_PreservesPathOrder()
  {
    FindElementResult original = CreateSample() with { Path = [3, 1, 4, 1, 5] };

    string json = JsonSerializer.Serialize(original, JsonOptions.Default);
    FindElementResult? deserialized = JsonSerializer.Deserialize<FindElementResult>(json, JsonOptions.Default);

    deserialized!.Path.Should().ContainInOrder(3, 1, 4, 1, 5);
  }

  [Fact]
  public void WindowIndex_IsNullByDefault()
  {
    FindElementResult result = CreateSample();

    result.WindowIndex.Should().BeNull();
  }

  [Fact]
  public void WindowIndex_RoundTrips()
  {
    FindElementResult original = CreateSample() with { WindowIndex = 2 };

    string json = JsonSerializer.Serialize(original, JsonOptions.Default);
    FindElementResult? deserialized = JsonSerializer.Deserialize<FindElementResult>(json, JsonOptions.Default);

    deserialized!.WindowIndex.Should().Be(2);
  }

  [Fact]
  public void WindowIndex_NullOmittedFromJson()
  {
    FindElementResult result = CreateSample();

    string json = JsonSerializer.Serialize(result, JsonOptions.Default);

    json.Should().NotContain("windowIndex");
  }
}