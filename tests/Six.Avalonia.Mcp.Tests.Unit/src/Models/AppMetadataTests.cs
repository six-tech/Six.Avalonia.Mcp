namespace Six.Avalonia.Mcp.Tests.Unit.Models;

using Six.Avalonia.Mcp.Contracts;
using Six.Avalonia.Mcp.Contracts.Models;

public sealed class AppMetadataTests
{
  private static AppMetadata CreateSample()
  {
    return new AppMetadata
    {
      ProcessName = "TestApp",
      ProcessId = 1234,
      MainWindowTitle = "Main Window",
      AvaloniaVersion = "11.3.11",
      DotNetVersion = "10.0.0",
      WindowCount = 1,
      WindowWidth = 800,
      WindowHeight = 600
    };
  }

  [Fact]
  public void Construction_SetsAllProperties()
  {
    AppMetadata meta = CreateSample();

    meta.ProcessName.Should().Be("TestApp");
    meta.ProcessId.Should().Be(1234);
    meta.MainWindowTitle.Should().Be("Main Window");
    meta.AvaloniaVersion.Should().Be("11.3.11");
    meta.DotNetVersion.Should().Be("10.0.0");
    meta.WindowCount.Should().Be(1);
    meta.WindowWidth.Should().Be(800);
    meta.WindowHeight.Should().Be(600);
  }

  [Fact]
  public void JsonRoundTrip_PreservesAllValues()
  {
    AppMetadata original = CreateSample();

    string json = JsonSerializer.Serialize(original, JsonOptions.Default);
    AppMetadata? deserialized = JsonSerializer.Deserialize<AppMetadata>(json, JsonOptions.Default);

    deserialized.Should().BeEquivalentTo(original);
  }

  [Fact]
  public void JsonSerialization_UsesCamelCasePropertyNames()
  {
    AppMetadata meta = CreateSample();

    string json = JsonSerializer.Serialize(meta, JsonOptions.Default);

    json.Should().Contain("\"processName\":");
    json.Should().Contain("\"processId\":");
    json.Should().Contain("\"mainWindowTitle\":");
    json.Should().Contain("\"avaloniaVersion\":");
    json.Should().Contain("\"dotNetVersion\":");
    json.Should().Contain("\"windowCount\":");
    json.Should().Contain("\"windowWidth\":");
    json.Should().Contain("\"windowHeight\":");
  }

  [Fact]
  public void NullMainWindowTitle_OmittedFromJson()
  {
    AppMetadata meta = CreateSample() with { MainWindowTitle = null };

    string json = JsonSerializer.Serialize(meta, JsonOptions.Default);

    json.Should().NotContain("mainWindowTitle");
  }

  [Fact]
  public void JsonDeserialization_HandlesNullMainWindowTitle()
  {
    AppMetadata original = CreateSample() with { MainWindowTitle = null };

    string json = JsonSerializer.Serialize(original, JsonOptions.Default);
    AppMetadata? deserialized = JsonSerializer.Deserialize<AppMetadata>(json, JsonOptions.Default);

    deserialized!.MainWindowTitle.Should().BeNull();
  }
}