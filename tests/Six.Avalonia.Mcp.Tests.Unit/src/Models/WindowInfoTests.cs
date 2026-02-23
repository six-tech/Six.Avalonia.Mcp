using Six.Avalonia.Mcp.Contracts;
using Six.Avalonia.Mcp.Contracts.Models;

namespace Six.Avalonia.Mcp.Tests.Unit.Models;

public class WindowInfoTests
{
  [Fact]
  public void WindowInfo_RoundTrips_ThroughJson()
  {
    WindowInfo original = new()
    {
      Index = 0,
      Title = "Main Window",
      Type = "Window",
      Bounds = new BoundsInfo { X = 0, Y = 0, Width = 800, Height = 600 },
      IsActive = true,
      IsVisible = true
    };

    string json = JsonSerializer.Serialize(original, JsonOptions.Default);
    WindowInfo? deserialized = JsonSerializer.Deserialize<WindowInfo>(json, JsonOptions.Default);

    deserialized.Should().NotBeNull();
    deserialized!.Index.Should().Be(0);
    deserialized.Title.Should().Be("Main Window");
    deserialized.Type.Should().Be("Window");
    deserialized.Bounds.Width.Should().Be(800);
    deserialized.IsActive.Should().BeTrue();
    deserialized.IsVisible.Should().BeTrue();
  }

  [Fact]
  public void WindowInfo_NullTitle_IsOmittedInJson()
  {
    WindowInfo info = new()
    {
      Index = 1,
      Type = "TopLevel",
      Bounds = new BoundsInfo { X = 0, Y = 0, Width = 400, Height = 300 },
      IsActive = false,
      IsVisible = true
    };

    string json = JsonSerializer.Serialize(info, JsonOptions.Default);

    json.Should().NotContain("title");
  }
}
