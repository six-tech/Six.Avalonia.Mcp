using Microsoft.Extensions.Logging;
using Six.Avalonia.Mcp.Contracts;
using Six.Avalonia.Mcp.Contracts.Messages;
using Six.Avalonia.Mcp.Contracts.Models;
using Six.Avalonia.Mcp.Connector.Services;

namespace Six.Avalonia.Mcp.Connector.Tests.Unit;

public class InspectorCommandHandlerTests
{
  private readonly IAvaloniaInspector _inspector = Substitute.For<IAvaloniaInspector>();
  private readonly InspectorCommandHandler _handler;

  public InspectorCommandHandlerTests()
  {
    ILogger<InspectorCommandHandler> logger = Substitute.For<ILogger<InspectorCommandHandler>>();
    _handler = new InspectorCommandHandler(_inspector, logger);
  }

  private static InspectorRequest CreateRequest(InspectorMethod method, Dictionary<string, object?>? parameters = null)
  {
    return new InspectorRequest
    {
      RequestId = "test-123",
      Method = method,
      ParametersJson = JsonSerializer.Serialize(parameters ?? new Dictionary<string, object?>(), JsonOptions.Default)
    };
  }

  [Fact]
  public async Task Handle_GetAppMetadata_DelegatesToInspector()
  {
    _inspector.GetAppMetadataAsync(Arg.Any<CancellationToken>()).Returns(new AppMetadata
    {
      ProcessName = "TestApp",
      ProcessId = 1234,
      AvaloniaVersion = "11.3.11",
      DotNetVersion = "10.0.0",
      WindowCount = 1,
      WindowWidth = 800,
      WindowHeight = 600
    });

    InspectorResponse response = await _handler.HandleAsync(CreateRequest(InspectorMethod.GetAppMetadata));

    response.Success.Should().BeTrue();
    response.RequestId.Should().Be("test-123");
    response.ResultJson.Should().Contain("TestApp");
    await _inspector.Received(1).GetAppMetadataAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_GetVisualTree_PassesParameters()
  {
    _inspector.GetVisualTreeAsync(Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
      .Returns(new VisualTreeNode
      {
        Type = "Window",
        Bounds = new BoundsInfo { X = 0, Y = 0, Width = 100, Height = 100 },
        IsVisible = true,
        IsEnabled = true,
        Opacity = 1.0,
        IsFocused = false,
        IsPointerOver = false,
        Classes = [],
        Children = []
      });

    InspectorResponse response = await _handler.HandleAsync(
      CreateRequest(InspectorMethod.GetVisualTree, new Dictionary<string, object?>
      {
        ["maxDepth"] = 5,
        ["elementName"] = "myPanel"
      }));

    response.Success.Should().BeTrue();
    await _inspector.Received(1).GetVisualTreeAsync(5, "myPanel", Arg.Any<int?>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_SimulateClick_PassesAllParameters()
  {
    _inspector.SimulateClickAsync(
        Arg.Any<string>(), Arg.Any<string>(), Arg.Any<double?>(), Arg.Any<double?>(),
        Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
      .Returns(new InteractionResult { Success = true });

    InspectorResponse response = await _handler.HandleAsync(
      CreateRequest(InspectorMethod.SimulateClick, new Dictionary<string, object?>
      {
        ["elementName"] = "btn1",
        ["button"] = "right",
        ["x"] = 10.0,
        ["y"] = 20.0,
        ["modifiers"] = "ctrl",
        ["clickCount"] = 2
      }));

    response.Success.Should().BeTrue();
    await _inspector.Received(1).SimulateClickAsync("btn1", "right", 10.0, 20.0, "ctrl", 2, Arg.Any<int?>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_ReturnsErrorResponse_OnException()
  {
    _inspector.GetAppMetadataAsync(Arg.Any<CancellationToken>())
      .ThrowsAsync(new InvalidOperationException("Main window is not available."));

    InspectorResponse response = await _handler.HandleAsync(CreateRequest(InspectorMethod.GetAppMetadata));

    response.Success.Should().BeFalse();
    response.ErrorMessage.Should().Contain("Main window is not available.");
    response.RequestId.Should().Be("test-123");
  }

  [Fact]
  public async Task Handle_SimulateType_DelegatesToInspector()
  {
    _inspector.SimulateTypeAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
      .Returns(new InteractionResult { Success = true });

    InspectorResponse response = await _handler.HandleAsync(
      CreateRequest(InspectorMethod.SimulateType, new Dictionary<string, object?>
      {
        ["elementName"] = "input",
        ["text"] = "hello"
      }));

    response.Success.Should().BeTrue();
    await _inspector.Received(1).SimulateTypeAsync("input", "hello", Arg.Any<int?>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_SetControlState_DelegatesToInspector()
  {
    _inspector.SetControlStateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
      .Returns(new InteractionResult { Success = true });

    InspectorResponse response = await _handler.HandleAsync(
      CreateRequest(InspectorMethod.SetControlState, new Dictionary<string, object?>
      {
        ["elementName"] = "btn",
        ["stateType"] = "enabled",
        ["value"] = true
      }));

    response.Success.Should().BeTrue();
    await _inspector.Received(1).SetControlStateAsync("btn", "enabled", true, Arg.Any<int?>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_SimulateKeyboardShortcut_DelegatesToInspector()
  {
    _inspector.SimulateKeyboardShortcutAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
      .Returns(new InteractionResult { Success = true });

    InspectorResponse response = await _handler.HandleAsync(
      CreateRequest(InspectorMethod.SimulateKeyboardShortcut, new Dictionary<string, object?>
      {
        ["keys"] = "Ctrl+S",
        ["elementName"] = "editor"
      }));

    response.Success.Should().BeTrue();
    await _inspector.Received(1).SimulateKeyboardShortcutAsync("Ctrl+S", "editor", Arg.Any<int?>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_TakeScreenshot_PassesOutputPath()
  {
    _inspector.TakeScreenshotAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<double>(), Arg.Any<int?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
      .Returns(new ScreenshotResult { Success = true, FilePath = "/tmp/test.png", Width = 800, Height = 600 });

    InspectorResponse response = await _handler.HandleAsync(
      CreateRequest(InspectorMethod.TakeScreenshot, new Dictionary<string, object?>
      {
        ["outputPath"] = "/tmp/test.png",
        ["elementName"] = "myBtn",
        ["scaleFactor"] = 2.0
      }));

    response.Success.Should().BeTrue();
    response.ResultJson.Should().Contain("filePath");
    response.ResultJson.Should().Contain("/tmp/test.png");
    await _inspector.Received(1).TakeScreenshotAsync("/tmp/test.png", "myBtn", 2.0, Arg.Any<int?>(), true, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_TakeScreenshot_RequiresOutputPath()
  {
    InspectorResponse response = await _handler.HandleAsync(
      CreateRequest(InspectorMethod.TakeScreenshot, new Dictionary<string, object?>
      {
        ["elementName"] = "myBtn"
      }));

    response.Success.Should().BeFalse();
    response.ErrorMessage.Should().Contain("outputPath is required");
  }

  [Fact]
  public async Task Handle_SimulateScroll_DelegatesToInspector()
  {
    _inspector.SimulateScrollAsync(Arg.Any<string>(), Arg.Any<double>(), Arg.Any<double>(), Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
      .Returns(new InteractionResult { Success = true });

    InspectorResponse response = await _handler.HandleAsync(
      CreateRequest(InspectorMethod.SimulateScroll, new Dictionary<string, object?>
      {
        ["elementName"] = "scrollList",
        ["deltaX"] = 0.0,
        ["deltaY"] = -120.0,
        ["modifiers"] = "ctrl"
      }));

    response.Success.Should().BeTrue();
    await _inspector.Received(1).SimulateScrollAsync("scrollList", 0.0, -120.0, "ctrl", Arg.Any<int?>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_ListWindows_DelegatesToInspector()
  {
    _inspector.ListWindowsAsync(Arg.Any<CancellationToken>())
      .Returns(new List<WindowInfo>
      {
        new()
        {
          Index = 0, Title = "Main", Type = "Window",
          Bounds = new BoundsInfo { X = 0, Y = 0, Width = 800, Height = 600 },
          IsActive = true, IsVisible = true
        }
      });

    InspectorResponse response = await _handler.HandleAsync(CreateRequest(InspectorMethod.ListWindows));

    response.Success.Should().BeTrue();
    response.ResultJson.Should().Contain("Main");
    await _inspector.Received(1).ListWindowsAsync(Arg.Any<CancellationToken>());
  }
}
