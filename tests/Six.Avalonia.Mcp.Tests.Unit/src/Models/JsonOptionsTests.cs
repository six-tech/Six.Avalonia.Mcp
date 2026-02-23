namespace Six.Avalonia.Mcp.Tests.Unit.Models;

using System.Text.Json;
using System.Text.Json.Serialization;
using Six.Avalonia.Mcp.Contracts;

public sealed class JsonOptionsTests
{
  [Fact]
  public void Default_UsesCamelCaseNaming()
  {
    JsonOptions.Default.PropertyNamingPolicy.Should().Be(JsonNamingPolicy.CamelCase);
  }

  [Fact]
  public void Default_IgnoresNullValues()
  {
    JsonOptions.Default.DefaultIgnoreCondition.Should().Be(JsonIgnoreCondition.WhenWritingNull);
  }

  [Fact]
  public void Default_IsNotIndented()
  {
    JsonOptions.Default.WriteIndented.Should().BeFalse();
  }

  [Fact]
  public void Default_IsCaseInsensitiveForDeserialization()
  {
    JsonOptions.Default.PropertyNameCaseInsensitive.Should().BeTrue();
  }

  [Fact]
  public void Default_ReturnsSameInstance()
  {
    JsonSerializerOptions first = JsonOptions.Default;
    JsonSerializerOptions second = JsonOptions.Default;
    first.Should().BeSameAs(second);
  }

  [Fact]
  public void Default_SerializesNullPropertiesToNothing()
  {
    var obj = new { Name = (string?)null, Value = 42 };
    string json = JsonSerializer.Serialize(obj, JsonOptions.Default);

    json.Should().NotContain("name");
    json.Should().Contain("value");
  }
}