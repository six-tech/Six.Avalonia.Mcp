using Six.Avalonia.Mcp.Contracts;
using Six.Avalonia.Mcp.Contracts.Messages;
using Six.Avalonia.Mcp.Server.Services;
using Six.Avalonia.Mcp.Server.Tools;

namespace Six.Avalonia.Mcp.Server.Tests.Unit.Tools;

public class InspectionToolsTests
{
  public class GetElementByPathTests
  {
    [Fact]
    public async Task GetElementByPath_ReturnsResultJson_OnSuccess()
    {
      IInspectorProxy proxy = Substitute.For<IInspectorProxy>();
      proxy.SendCommandAsync(InspectorMethod.GetElementProperties, Arg.Any<Dictionary<string, object?>?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
        .Returns(new InspectorResponse
        {
          RequestId = "test",
          Success = true,
          ResultJson = "{\"type\":\"Button\",\"name\":\"ActionButton\"}"
        });

      string result = await InspectionTools.GetElementByPath(proxy, "0/1/0");

      result.Should().Contain("Button");
      result.Should().Contain("ActionButton");
    }

    [Fact]
    public async Task GetElementByPath_ReturnsErrorJson_OnFailure()
    {
      IInspectorProxy proxy = Substitute.For<IInspectorProxy>();
      proxy.SendCommandAsync(InspectorMethod.GetElementProperties, Arg.Any<Dictionary<string, object?>?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
        .Returns(new InspectorResponse
        {
          RequestId = "test",
          Success = false,
          ErrorMessage = "Element not found at path '99/99'."
        });

      string result = await InspectionTools.GetElementByPath(proxy, "99/99");

      result.Should().Contain("error");
      result.Should().Contain("Element not found");
    }

    [Fact]
    public async Task GetElementByPath_ReturnsErrorJson_OnException()
    {
      IInspectorProxy proxy = Substitute.For<IInspectorProxy>();
      proxy.SendCommandAsync(InspectorMethod.GetElementProperties, Arg.Any<Dictionary<string, object?>?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
        .ThrowsAsync(new InvalidOperationException("No app connected"));

      string result = await InspectionTools.GetElementByPath(proxy, "0/1");

      result.Should().Contain("error");
      result.Should().Contain("No app connected");
    }

    [Fact]
    public async Task GetElementByPath_PassesPathParameter()
    {
      IInspectorProxy proxy = Substitute.For<IInspectorProxy>();
      proxy.SendCommandAsync(Arg.Any<InspectorMethod>(), Arg.Any<Dictionary<string, object?>?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
        .Returns(new InspectorResponse { RequestId = "test", Success = true, ResultJson = "{}" });

      await InspectionTools.GetElementByPath(proxy, "0/3/1");

      await proxy.Received(1).SendCommandAsync(
        InspectorMethod.GetElementProperties,
        Arg.Is<Dictionary<string, object?>>(d => d.ContainsKey("path") && (string)d["path"]! == "0/3/1"),
        null,
        Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetElementByPath_PassesAppId()
    {
      IInspectorProxy proxy = Substitute.For<IInspectorProxy>();
      proxy.SendCommandAsync(Arg.Any<InspectorMethod>(), Arg.Any<Dictionary<string, object?>?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
        .Returns(new InspectorResponse { RequestId = "test", Success = true, ResultJson = "{}" });

      await InspectionTools.GetElementByPath(proxy, "0/1", appId: "myApp");

      await proxy.Received(1).SendCommandAsync(
        InspectorMethod.GetElementProperties,
        Arg.Any<Dictionary<string, object?>?>(),
        "myApp",
        Arg.Any<CancellationToken>());
    }
  }
}
