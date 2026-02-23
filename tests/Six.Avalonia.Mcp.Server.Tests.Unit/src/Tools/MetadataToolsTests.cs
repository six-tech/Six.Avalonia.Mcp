using Six.Avalonia.Mcp.Contracts;
using Six.Avalonia.Mcp.Contracts.Messages;
using Six.Avalonia.Mcp.Server.Services;
using Six.Avalonia.Mcp.Server.Tools;

namespace Six.Avalonia.Mcp.Server.Tests.Unit.Tools;

public class MetadataToolsTests
{
  [Fact]
  public async Task GetAppMetadata_ReturnsResultJson_OnSuccess()
  {
    IInspectorProxy proxy = Substitute.For<IInspectorProxy>();
    proxy.SendCommandAsync(InspectorMethod.GetAppMetadata, Arg.Any<Dictionary<string, object?>?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
      .Returns(new InspectorResponse
      {
        RequestId = "test",
        Success = true,
        ResultJson = "{\"processName\":\"TestApp\",\"processId\":1234}"
      });

    string result = await MetadataTools.GetAppMetadata(proxy);

    result.Should().Contain("TestApp");
    result.Should().Contain("1234");
  }

  [Fact]
  public async Task GetAppMetadata_ReturnsErrorJson_OnFailure()
  {
    IInspectorProxy proxy = Substitute.For<IInspectorProxy>();
    proxy.SendCommandAsync(InspectorMethod.GetAppMetadata, Arg.Any<Dictionary<string, object?>?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
      .Returns(new InspectorResponse
      {
        RequestId = "test",
        Success = false,
        ErrorMessage = "Main window is not available."
      });

    string result = await MetadataTools.GetAppMetadata(proxy);

    result.Should().Contain("error");
    result.Should().Contain("Main window is not available.");
  }

  [Fact]
  public async Task GetAppMetadata_ReturnsErrorJson_OnException()
  {
    IInspectorProxy proxy = Substitute.For<IInspectorProxy>();
    proxy.SendCommandAsync(InspectorMethod.GetAppMetadata, Arg.Any<Dictionary<string, object?>?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
      .ThrowsAsync(new InvalidOperationException("No app connected"));

    string result = await MetadataTools.GetAppMetadata(proxy);

    result.Should().Contain("error");
    result.Should().Contain("No app connected");
  }

  [Fact]
  public async Task GetAppMetadata_PassesAppId()
  {
    IInspectorProxy proxy = Substitute.For<IInspectorProxy>();
    proxy.SendCommandAsync(Arg.Any<InspectorMethod>(), Arg.Any<Dictionary<string, object?>?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
      .Returns(new InspectorResponse { RequestId = "test", Success = true, ResultJson = "{}" });

    await MetadataTools.GetAppMetadata(proxy, "myApp");

    await proxy.Received(1).SendCommandAsync(InspectorMethod.GetAppMetadata, null, "myApp", Arg.Any<CancellationToken>());
  }
}
