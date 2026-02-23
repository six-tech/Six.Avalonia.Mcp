using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Six.Avalonia.Mcp.Contracts.Hub;
using Six.Avalonia.Mcp.Contracts.Messages;
using Six.Avalonia.Mcp.Server.Hub;
using Six.Avalonia.Mcp.Server.Services;

namespace Six.Avalonia.Mcp.Server.Tests.Unit.Services;

public class InspectorProxyTests
{
  [Fact]
  public async Task SendCommandAsync_ThrowsTimeoutException_WhenCommandExceedsTimeout()
  {
    IHubContext<InspectorHub, IInspectorClient> hubContext = Substitute.For<IHubContext<InspectorHub, IInspectorClient>>();
    IAppRegistry registry = Substitute.For<IAppRegistry>();
    ILogger<InspectorProxy> logger = Substitute.For<ILogger<InspectorProxy>>();

    registry.GetConnectionId(Arg.Any<string?>()).Returns("conn1");

    IInspectorClient client = Substitute.For<IInspectorClient>();
    // Simulate a command that never completes
    client.ExecuteCommandAsync(Arg.Any<InspectorRequest>())
      .Returns(new TaskCompletionSource<InspectorResponse>().Task);

    IHubClients<IInspectorClient> clients = Substitute.For<IHubClients<IInspectorClient>>();
    clients.Client("conn1").Returns(client);
    hubContext.Clients.Returns(clients);

    InspectorProxy proxy = new(hubContext, registry, logger);

    Func<Task> act = () => proxy.SendCommandAsync(InspectorMethod.GetAppMetadata);

    await act.Should().ThrowAsync<TimeoutException>()
      .WithMessage("*timed out*");
  }
}
