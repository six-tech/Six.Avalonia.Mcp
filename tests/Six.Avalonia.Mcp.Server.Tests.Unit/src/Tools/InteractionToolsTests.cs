using Six.Avalonia.Mcp.Contracts;
using Six.Avalonia.Mcp.Contracts.Messages;
using Six.Avalonia.Mcp.Server.Services;
using Six.Avalonia.Mcp.Server.Tools;

namespace Six.Avalonia.Mcp.Server.Tests.Unit.Tools;

public class InteractionToolsTests
{
  [Fact]
  public async Task SimulateScroll_ReturnsResultJson_OnSuccess()
  {
    IInspectorProxy proxy = Substitute.For<IInspectorProxy>();
    proxy.SendCommandAsync(InspectorMethod.SimulateScroll, Arg.Any<Dictionary<string, object?>?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
      .Returns(new InspectorResponse
      {
        RequestId = "test",
        Success = true,
        ResultJson = "{\"success\":true,\"elementType\":\"ListBox\",\"elementName\":\"ScrollableListBox\"}"
      });

    string result = await InteractionTools.SimulateScroll(proxy, "ScrollableListBox", deltaY: -120);

    result.Should().Contain("ScrollableListBox");
    result.Should().Contain("success");
  }

  [Fact]
  public async Task SimulateScroll_ReturnsErrorJson_OnFailure()
  {
    IInspectorProxy proxy = Substitute.For<IInspectorProxy>();
    proxy.SendCommandAsync(InspectorMethod.SimulateScroll, Arg.Any<Dictionary<string, object?>?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
      .Returns(new InspectorResponse
      {
        RequestId = "test",
        Success = false,
        ErrorMessage = "Element not found."
      });

    string result = await InteractionTools.SimulateScroll(proxy, "nonexistent");

    result.Should().Contain("error");
    result.Should().Contain("Element not found");
  }

  [Fact]
  public async Task SimulateScroll_ReturnsErrorJson_OnException()
  {
    IInspectorProxy proxy = Substitute.For<IInspectorProxy>();
    proxy.SendCommandAsync(InspectorMethod.SimulateScroll, Arg.Any<Dictionary<string, object?>?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
      .ThrowsAsync(new InvalidOperationException("No app connected"));

    string result = await InteractionTools.SimulateScroll(proxy, "list");

    result.Should().Contain("error");
    result.Should().Contain("No app connected");
  }

  [Fact]
  public async Task SimulateScroll_PassesAllParameters()
  {
    IInspectorProxy proxy = Substitute.For<IInspectorProxy>();
    proxy.SendCommandAsync(Arg.Any<InspectorMethod>(), Arg.Any<Dictionary<string, object?>?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
      .Returns(new InspectorResponse { RequestId = "test", Success = true, ResultJson = "{}" });

    await InteractionTools.SimulateScroll(proxy, "myList", deltaX: 10, deltaY: -200, modifiers: "ctrl", windowIndex: 1, appId: "app1");

    await proxy.Received(1).SendCommandAsync(
      InspectorMethod.SimulateScroll,
      Arg.Is<Dictionary<string, object?>>(d =>
        (string)d["elementName"]! == "myList" &&
        (double)d["deltaX"]! == 10 &&
        (double)d["deltaY"]! == -200 &&
        (string)d["modifiers"]! == "ctrl" &&
        (int)d["windowIndex"]! == 1),
      "app1",
      Arg.Any<CancellationToken>());
  }
}
