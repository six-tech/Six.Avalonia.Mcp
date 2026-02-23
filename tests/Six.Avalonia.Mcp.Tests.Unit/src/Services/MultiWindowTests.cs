using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Microsoft.Extensions.Logging;
using Six.Avalonia.Mcp.Contracts.Models;
using Six.Avalonia.Mcp.Connector.Services;

namespace Six.Avalonia.Mcp.Tests.Unit.Services;

public class MultiWindowTests
{
  private sealed class MultiWindowInspector : AvaloniaInspector
  {
    private readonly IReadOnlyList<Window> _windows;

    public MultiWindowInspector(IReadOnlyList<Window> windows, ILogger<AvaloniaInspector> logger)
      : base(logger)
    {
      _windows = windows;
    }

    protected override TopLevel? GetTopLevel() => _windows.Count > 0 ? _windows[0] : null;

    protected override IReadOnlyList<TopLevel> GetAllTopLevels()
    {
      return _windows.ToList<TopLevel>();
    }
  }

  private static (MultiWindowInspector Inspector, Window Window1, Window Window2) CreateMultiWindowInspector()
  {
    ILogger<AvaloniaInspector>? logger = Substitute.For<ILogger<AvaloniaInspector>>();

    Window window1 = new() { Title = "Main Window", Width = 800, Height = 600 };
    Button btn1 = new() { Name = "MainButton" };
    window1.Content = btn1;
    window1.Show();

    Window window2 = new() { Title = "Dialog", Width = 400, Height = 300 };
    Button btn2 = new() { Name = "DialogButton" };
    window2.Content = btn2;
    window2.Show();

    MultiWindowInspector inspector = new([window1, window2], logger);
    return (inspector, window1, window2);
  }

  public class ListWindowsAsyncTests : MultiWindowTests
  {
    [AvaloniaFact]
    public async Task ListWindowsAsync_ReturnsAllWindows()
    {
      (MultiWindowInspector inspector, Window w1, Window w2) = CreateMultiWindowInspector();

      IReadOnlyList<WindowInfo> windows = await inspector.ListWindowsAsync();

      windows.Should().HaveCount(2);
      windows[0].Index.Should().Be(0);
      windows[0].Title.Should().Be("Main Window");
      windows[0].Type.Should().Be("Window");
      windows[1].Index.Should().Be(1);
      windows[1].Title.Should().Be("Dialog");

      w1.Close();
      w2.Close();
    }

    [AvaloniaFact]
    public async Task ListWindowsAsync_ReturnsBounds()
    {
      (MultiWindowInspector inspector, Window w1, Window w2) = CreateMultiWindowInspector();

      IReadOnlyList<WindowInfo> windows = await inspector.ListWindowsAsync();

      windows[0].Bounds.Should().NotBeNull();
      windows[1].Bounds.Should().NotBeNull();

      w1.Close();
      w2.Close();
    }
  }

  public class CrossWindowElementResolutionTests : MultiWindowTests
  {
    [AvaloniaFact]
    public async Task FindElementAsync_SearchesAllWindows()
    {
      (MultiWindowInspector inspector, Window w1, Window w2) = CreateMultiWindowInspector();

      IReadOnlyList<FindElementResult> results = await inspector.FindElementAsync("DialogButton");

      results.Should().ContainSingle()
        .Which.Name.Should().Be("DialogButton");

      w1.Close();
      w2.Close();
    }

    [AvaloniaFact]
    public async Task FindElementAsync_SetsWindowIndexOnResults()
    {
      (MultiWindowInspector inspector, Window w1, Window w2) = CreateMultiWindowInspector();

      IReadOnlyList<FindElementResult> results = await inspector.FindElementAsync(typeName: "Button", maxResults: 10);

      results.Should().HaveCountGreaterThanOrEqualTo(2);
      results.Should().Contain(r => r.WindowIndex == 0);
      results.Should().Contain(r => r.WindowIndex == 1);

      w1.Close();
      w2.Close();
    }

    [AvaloniaFact]
    public async Task FindElementAsync_WithWindowIndex_SearchesOnlyThatWindow()
    {
      (MultiWindowInspector inspector, Window w1, Window w2) = CreateMultiWindowInspector();

      IReadOnlyList<FindElementResult> results = await inspector.FindElementAsync("MainButton", windowIndex: 0);

      results.Should().ContainSingle()
        .Which.Name.Should().Be("MainButton");

      w1.Close();
      w2.Close();
    }

    [AvaloniaFact]
    public async Task FindElementAsync_WithWindowIndex_DoesNotFindElementInOtherWindow()
    {
      (MultiWindowInspector inspector, Window w1, Window w2) = CreateMultiWindowInspector();

      IReadOnlyList<FindElementResult> results = await inspector.FindElementAsync("DialogButton", windowIndex: 0);

      results.Should().BeEmpty();

      w1.Close();
      w2.Close();
    }
  }

  public class GetVisualTreeAsyncWindowIndexTests : MultiWindowTests
  {
    [AvaloniaFact]
    public async Task GetVisualTreeAsync_WithWindowIndex_ReturnsCorrectWindow()
    {
      (MultiWindowInspector inspector, Window w1, Window w2) = CreateMultiWindowInspector();

      VisualTreeNode tree = await inspector.GetVisualTreeAsync(3, windowIndex: 1);

      tree.Type.Should().Be("Window");

      w1.Close();
      w2.Close();
    }

    [AvaloniaFact]
    public async Task GetVisualTreeAsync_WithInvalidWindowIndex_Throws()
    {
      (MultiWindowInspector inspector, Window w1, Window w2) = CreateMultiWindowInspector();

      Func<Task<VisualTreeNode>> act = () => inspector.GetVisualTreeAsync(3, windowIndex: 99);

      await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
        .WithMessage("*out of range*");

      w1.Close();
      w2.Close();
    }
  }

  public class TakeScreenshotAsyncWindowIndexTests : MultiWindowTests
  {
    [AvaloniaFact]
    public async Task TakeScreenshotAsync_WithWindowIndex_CapturesCorrectWindow()
    {
      (MultiWindowInspector inspector, Window w1, Window w2) = CreateMultiWindowInspector();
      string outputPath = Path.Combine(Path.GetTempPath(), "rayx-test", $"test-{Guid.NewGuid()}.png");

      try
      {
        ScreenshotResult result = await inspector.TakeScreenshotAsync(outputPath, windowIndex: 1);

        result.Success.Should().BeTrue();
        File.Exists(outputPath).Should().BeTrue();
      }
      finally
      {
        if(File.Exists(outputPath)) File.Delete(outputPath);
        w1.Close();
        w2.Close();
      }
    }
  }

  public class SimulateClickAcrossWindowsTests : MultiWindowTests
  {
    [AvaloniaFact]
    public async Task SimulateClickAsync_FindsElementInSecondWindow()
    {
      (MultiWindowInspector inspector, Window w1, Window w2) = CreateMultiWindowInspector();

      InteractionResult result = await inspector.SimulateClickAsync("DialogButton");

      result.Success.Should().BeTrue();
      result.ElementName.Should().Be("DialogButton");

      w1.Close();
      w2.Close();
    }
  }

  public class GetAppMetadataWindowCountTests : MultiWindowTests
  {
    [AvaloniaFact]
    public async Task GetAppMetadataAsync_ReportsCorrectWindowCount()
    {
      (MultiWindowInspector inspector, Window w1, Window w2) = CreateMultiWindowInspector();

      AppMetadata metadata = await inspector.GetAppMetadataAsync();

      metadata.WindowCount.Should().Be(2);

      w1.Close();
      w2.Close();
    }
  }
}
