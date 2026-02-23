namespace Six.Avalonia.Mcp.Tests.Unit.Models;

using Six.Avalonia.Mcp.Contracts;
using Six.Avalonia.Mcp.Contracts.Models;

public sealed class ScreenshotResultTests
{
  [Fact]
  public void SuccessResult_SetsAllProperties()
  {
    ScreenshotResult result = new()
    {
      Success = true,
      FilePath = "/tmp/screenshot.png",
      Width = 1280,
      Height = 720
    };

    result.Success.Should().BeTrue();
    result.FilePath.Should().Be("/tmp/screenshot.png");
    result.Width.Should().Be(1280);
    result.Height.Should().Be(720);
    result.Error.Should().BeNull();
  }

  [Fact]
  public void FailureResult_HasErrorMessage()
  {
    ScreenshotResult result = new() { Success = false, Error = "Visual has zero size" };

    result.Success.Should().BeFalse();
    result.Error.Should().Be("Visual has zero size");
    result.FilePath.Should().BeNull();
  }

  [Fact]
  public void JsonRoundTrip_SuccessResult_PreservesAllValues()
  {
    ScreenshotResult original = new()
    {
      Success = true,
      FilePath = "/tmp/screenshots/app-001.png",
      Width = 800,
      Height = 600
    };

    string json = JsonSerializer.Serialize(original, JsonOptions.Default);
    ScreenshotResult? deserialized = JsonSerializer.Deserialize<ScreenshotResult>(json, JsonOptions.Default);

    deserialized.Should().BeEquivalentTo(original);
  }

  [Fact]
  public void JsonRoundTrip_FailureResult_PreservesAllValues()
  {
    ScreenshotResult original = new() { Success = false, Error = "Element not found" };

    string json = JsonSerializer.Serialize(original, JsonOptions.Default);
    ScreenshotResult? deserialized = JsonSerializer.Deserialize<ScreenshotResult>(json, JsonOptions.Default);

    deserialized.Should().BeEquivalentTo(original);
  }

  [Fact]
  public void NullOptionalProperties_OmittedFromJson()
  {
    ScreenshotResult result = new() { Success = false, Error = "fail" };

    string json = JsonSerializer.Serialize(result, JsonOptions.Default);

    json.Should().Contain("\"success\":");
    json.Should().Contain("\"error\":");
    json.Should().NotContain("\"filePath\"");
  }

  [Fact]
  public void JsonSerialization_UsesCamelCasePropertyNames()
  {
    ScreenshotResult result = new()
    {
      Success = true,
      FilePath = "/tmp/test.png",
      Width = 640,
      Height = 480
    };

    string json = JsonSerializer.Serialize(result, JsonOptions.Default);

    json.Should().Contain("\"success\":");
    json.Should().Contain("\"filePath\":");
    json.Should().Contain("\"width\":");
    json.Should().Contain("\"height\":");
  }
}
