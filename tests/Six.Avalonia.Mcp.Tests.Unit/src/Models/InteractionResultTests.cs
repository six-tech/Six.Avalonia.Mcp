namespace Six.Avalonia.Mcp.Tests.Unit.Models;

using Six.Avalonia.Mcp.Contracts;
using Six.Avalonia.Mcp.Contracts.Models;

public sealed class InteractionResultTests
{
  [Fact]
  public void SuccessResult_SetsAllProperties()
  {
    InteractionResult result = new()
    {
      Success = true,
      ElementType = "Button",
      ElementName = "submitBtn",
      IsFocused = true,
      IsEnabled = true,
      IsVisible = true,
      IsPointerOver = false
    };

    result.Success.Should().BeTrue();
    result.ErrorMessage.Should().BeNull();
    result.ElementType.Should().Be("Button");
    result.ElementName.Should().Be("submitBtn");
    result.IsFocused.Should().BeTrue();
    result.IsEnabled.Should().BeTrue();
    result.IsVisible.Should().BeTrue();
    result.IsPointerOver.Should().BeFalse();
  }

  [Fact]
  public void FailureResult_HasErrorMessage()
  {
    InteractionResult result = new() { Success = false, ErrorMessage = "Element not found" };

    result.Success.Should().BeFalse();
    result.ErrorMessage.Should().Be("Element not found");
  }

  [Fact]
  public void JsonRoundTrip_SuccessResult_PreservesAllValues()
  {
    InteractionResult original = new()
    {
      Success = true,
      ElementType = "TextBox",
      ElementName = "input",
      IsFocused = true,
      IsEnabled = true,
      IsVisible = true,
      IsPointerOver = true
    };

    string json = JsonSerializer.Serialize(original, JsonOptions.Default);
    InteractionResult? deserialized = JsonSerializer.Deserialize<InteractionResult>(json, JsonOptions.Default);

    deserialized.Should().BeEquivalentTo(original);
  }

  [Fact]
  public void JsonRoundTrip_FailureResult_PreservesAllValues()
  {
    InteractionResult original = new() { Success = false, ErrorMessage = "Element is disabled" };

    string json = JsonSerializer.Serialize(original, JsonOptions.Default);
    InteractionResult? deserialized = JsonSerializer.Deserialize<InteractionResult>(json, JsonOptions.Default);

    deserialized.Should().BeEquivalentTo(original);
  }

  [Fact]
  public void NullOptionalProperties_OmittedFromJson()
  {
    InteractionResult result = new() { Success = false, ErrorMessage = "fail" };

    string json = JsonSerializer.Serialize(result, JsonOptions.Default);

    json.Should().Contain("\"success\":");
    json.Should().Contain("\"errorMessage\":");
    json.Should().NotContain("\"elementType\"");
    json.Should().NotContain("\"elementName\"");
    json.Should().NotContain("\"isFocused\"");
    json.Should().NotContain("\"isEnabled\"");
    json.Should().NotContain("\"isVisible\"");
    json.Should().NotContain("\"isPointerOver\"");
  }

  [Fact]
  public void JsonSerialization_UsesCamelCasePropertyNames()
  {
    InteractionResult result = new()
    {
      Success = true,
      ElementType = "Button",
      ElementName = "btn",
      IsFocused = false,
      IsEnabled = true,
      IsVisible = true,
      IsPointerOver = false
    };

    string json = JsonSerializer.Serialize(result, JsonOptions.Default);

    json.Should().Contain("\"success\":");
    json.Should().Contain("\"elementType\":");
    json.Should().Contain("\"elementName\":");
    json.Should().Contain("\"isFocused\":");
    json.Should().Contain("\"isEnabled\":");
    json.Should().Contain("\"isVisible\":");
    json.Should().Contain("\"isPointerOver\":");
  }

  [Fact]
  public void NullableBools_SerializeCorrectly()
  {
    InteractionResult result = new()
    {
      Success = true,
      IsFocused = null,
      IsEnabled = true,
      IsVisible = false,
      IsPointerOver = null
    };

    string json = JsonSerializer.Serialize(result, JsonOptions.Default);
    InteractionResult? deserialized = JsonSerializer.Deserialize<InteractionResult>(json, JsonOptions.Default);

    deserialized!.IsFocused.Should().BeNull();
    deserialized.IsEnabled.Should().BeTrue();
    deserialized.IsVisible.Should().BeFalse();
    deserialized.IsPointerOver.Should().BeNull();
  }
}