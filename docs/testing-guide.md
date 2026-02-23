# Testing Guide

This document covers the test framework, project structure, testing patterns, and how to add new tests to the Six.Avalonia.Mcp project.

## Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run a specific test project
dotnet test tests/Six.Avalonia.Mcp.Tests.Unit
dotnet test tests/Six.Avalonia.Mcp.Server.Tests.Unit
dotnet test tests/Six.Avalonia.Mcp.Connector.Tests.Unit

# Run a specific test class
dotnet test --filter "FullyQualifiedName~MetadataToolsTests"

# Run tests with detailed output
dotnet test --verbosity normal
```

## Test Framework Stack

| Package | Version | Purpose |
|---------|---------|---------|
| xUnit | 2.9.3 | Test framework and runner |
| xunit.runner.visualstudio | 3.1.5 | Visual Studio / `dotnet test` integration |
| FluentAssertions | 8.8.0 | Expressive assertion syntax |
| NSubstitute | 5.3.0 | Mocking / substitute framework |
| Coverlet | 6.0.4 | Code coverage collector |
| Avalonia.Headless.XUnit | 11.3.11 | Headless Avalonia test platform |
| Avalonia.Themes.Fluent | 11.3.11 | Theme for headless rendering |
| Microsoft.NET.Test.Sdk | 18.0.1 | .NET test infrastructure |

## Test Project Structure

There are three test projects, each covering a different layer of the architecture:

```
tests/
  Six.Avalonia.Mcp.Tests.Unit/          # Inspector + model tests (headless Avalonia)
    GlobalUsings.cs                       # Shared imports (FluentAssertions, NSubstitute, xUnit)
    src/
      TestApp.cs                          # Headless Avalonia test application bootstrap
      Models/
        AppMetadataTests.cs               # DTO serialization/deserialization tests
        BoundsInfoTests.cs
        ElementPropertiesTests.cs
        FindElementResultTests.cs
        InteractionResultTests.cs
        JsonOptionsTests.cs
        VisualTreeNodeTests.cs
      Services/
        AvaloniaInspectorTests.cs         # Unit tests for internal static methods
        AvaloniaInspectorIntegrationTests.cs  # Integration tests with headless window

  Six.Avalonia.Mcp.Server.Tests.Unit/   # Server tool + registry tests
    src/
      Tools/                              # Tool delegation tests (mock IInspectorProxy)
        MetadataToolsTests.cs
        InspectionToolsTests.cs
        ScreenshotToolsTests.cs
        InteractionToolsTests.cs
        AppManagementToolsTests.cs
      Services/
        AppRegistryTests.cs               # App registry tests
        InspectorProxyTests.cs            # Inspector proxy tests

  Six.Avalonia.Mcp.Connector.Tests.Unit/ # Command handler tests
    src/
      InspectorCommandHandlerTests.cs     # Dispatch logic tests
```

### GlobalUsings.cs

Shared imports are declared once and available to all test files:

```csharp
global using System.Collections.Immutable;
global using System.Text.Json;
global using FluentAssertions;
global using NSubstitute;
global using NSubstitute.ExceptionExtensions;
global using Xunit;
```

### TestApp.cs

Bootstraps a headless Avalonia application for integration tests:

```csharp
[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

public class TestApp : Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
    }
}

public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<TestApp>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
    }
}
```

## Unit Test Patterns

### Server Tool Tests (mocking IInspectorProxy)

Server tool tests mock `IInspectorProxy` and verify that tools correctly delegate commands and handle responses:

```csharp
[Fact]
public async Task GetAppMetadata_SendsCommandAndReturnsResult()
{
    IInspectorProxy proxy = Substitute.For<IInspectorProxy>();
    proxy.SendCommandAsync(Arg.Any<InspectorMethod>(), Arg.Any<Dictionary<string, object?>>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>())
        .Returns("""{"processName":"TestApp","processId":1234}""");

    string result = await MetadataTools.GetAppMetadata(proxy);

    result.Should().Contain("TestApp");
}
```

### Inspector Integration Tests (using IAvaloniaInspector directly)

Inspector tests in `Tests.Unit` use a testable subclass with a headless window to test the actual inspection logic:

```csharp
[Fact]
public async Task GetAppMetadata_ReturnsSerializedJson()
{
    IAvaloniaInspector inspector = Substitute.For<IAvaloniaInspector>();
    inspector.GetAppMetadataAsync(Arg.Any<CancellationToken>()).Returns(new AppMetadata
    {
        ProcessName = "TestApp",
        ProcessId = 1234,
        MainWindowTitle = "Test Window",
        AvaloniaVersion = "11.3.11",
        DotNetVersion = "10.0.0",
        WindowCount = 1,
        WindowWidth = 800,
        WindowHeight = 600
    });

    // Use roundtrip deserialization instead of exact string matching
    AppMetadata? roundTripped = JsonSerializer.Deserialize<AppMetadata>(
        JsonSerializer.Serialize(await inspector.GetAppMetadataAsync(), JsonOptions.Default),
        JsonOptions.Default);
    roundTripped.Should().NotBeNull();
    roundTripped!.ProcessName.Should().Be("TestApp");
    roundTripped.ProcessId.Should().Be(1234);
}
```

### Connector Command Handler Tests

Command handler tests in `Connector.Tests.Unit` verify that `InspectorCommandHandler` correctly dispatches `InspectorRequest` messages to `IAvaloniaInspector` methods:

```csharp
[Fact]
public async Task HandleCommand_GetAppMetadata_DelegatesToInspector()
{
    IAvaloniaInspector inspector = Substitute.For<IAvaloniaInspector>();
    var handler = new InspectorCommandHandler(inspector);

    var request = new InspectorRequest
    {
        RequestId = Guid.NewGuid().ToString(),
        Method = InspectorMethod.GetAppMetadata
    };

    InspectorResponse response = await handler.HandleAsync(request);

    response.Should().NotBeNull();
    await inspector.Received(1).GetAppMetadataAsync(Arg.Any<CancellationToken>());
}
```

### Testing Error Handling

Tools catch exceptions and return them as JSON. Verify this behavior:

```csharp
[Fact]
public async Task GetAppMetadata_ReturnsErrorJsonOnFailure()
{
    IAvaloniaInspector inspector = Substitute.For<IAvaloniaInspector>();
    inspector.GetAppMetadataAsync(Arg.Any<CancellationToken>())
        .ThrowsAsync(new InvalidOperationException("TopLevel is not available."));

    // Inspector-level error handling
    // Tool-level: proxy.SendCommandAsync throws -> tool returns error JSON
}
```

### Testing Internal Methods

The `AvaloniaInspector` class has several `internal static` methods (like `ResolveElement`, `BuildPath`, `ParseSelector`, `MapMouseButton`, `ParseModifiers`, `ParseKeyCombination`, etc.) that are tested directly.

Access is granted via `InternalsVisibleTo` configured in the Connector library's `GlobalUsings.cs`:

```csharp
[assembly: InternalsVisibleTo("McpDemo.Tests.Unit")]
[assembly: InternalsVisibleTo("McpDemo.Connector.Tests.Unit")]
```

Test these methods with standard xUnit `[Fact]` tests:

```csharp
[Fact]
public void ParseSelector_HashSelector_ReturnsName()
{
    var (type, name, className) = AvaloniaInspector.ParseSelector("#myBtn");

    type.Should().BeNull();
    name.Should().Be("myBtn");
    className.Should().BeNull();
}
```

## Integration Test Patterns

### Testable Subclass

Integration tests use a `TestableInspector` subclass that overrides `GetTopLevel()` to return a controlled test window. This avoids depending on `Application.Current` and `IClassicDesktopStyleApplicationLifetime`:

```csharp
private sealed class TestableInspector : AvaloniaInspector
{
    private readonly Window _window;

    public TestableInspector(Window window, ILogger<AvaloniaInspector> logger) : base(logger)
    {
        _window = window;
    }

    protected override TopLevel? GetTopLevel()
    {
        return _window;
    }
}
```

Note: `GetTopLevel()` returns `TopLevel?` (not `Window?`). The inspector uses `TopLevel` to support both desktop and single-view lifetimes. Since `Window` inherits from `TopLevel`, returning a `Window` is valid.

### Creating a Test Window

Build a window with named controls that mirror your application:

```csharp
private static (TestableInspector Inspector, Window Window) CreateTestableInspector()
{
    ILogger<AvaloniaInspector> logger = Substitute.For<ILogger<AvaloniaInspector>>();
    Window window = new() { Title = "Test Window", Width = 800, Height = 600 };

    TextBox textBox = new() { Name = "InputTextBox" };
    Button button = new() { Name = "ActionButton", Content = "Click Me" };
    CheckBox checkBox = new() { Name = "ToggleCheckBox" };
    Slider slider = new() { Name = "ValueSlider", Minimum = 0, Maximum = 100 };
    TextBlock statusText = new() { Name = "StatusText", Text = "Ready" };

    StackPanel panel = new();
    panel.Children.Add(textBox);
    panel.Children.Add(button);
    panel.Children.Add(checkBox);
    panel.Children.Add(slider);
    panel.Children.Add(statusText);

    window.Content = panel;
    window.Show();  // Triggers layout in headless mode

    TestableInspector inspector = new(window, logger);
    return (inspector, window);
}
```

### Using AvaloniaFact

Integration tests that touch the Avalonia UI thread must use `[AvaloniaFact]` instead of `[Fact]`. This attribute (from `Avalonia.Headless.XUnit`) ensures the test runs on the Avalonia dispatcher thread:

```csharp
[AvaloniaFact]
public async Task SimulateClickAsync_LeftClick_ReturnsSuccess()
{
    (TestableInspector inspector, Window window) = CreateTestableInspector();

    InteractionResult result = await inspector.SimulateClickAsync("ActionButton");

    result.Success.Should().BeTrue();
    result.ElementType.Should().Be("Button");
    result.ElementName.Should().Be("ActionButton");

    window.Close();
}
```

Always call `window.Close()` at the end of each integration test to clean up.

## JSON Testing Tip

`System.Text.Json` escapes certain characters (for example, single quotes become `\u0027`). Instead of matching exact JSON strings, always **roundtrip deserialize** the result and assert on the deserialized object:

```csharp
// BAD: Fragile, breaks on encoding differences
result.Should().Be("""{"processName":"TestApp"}""");

// GOOD: Roundtrip deserialization
AppMetadata? parsed = JsonSerializer.Deserialize<AppMetadata>(result, JsonOptions.Default);
parsed!.ProcessName.Should().Be("TestApp");
```

Also verify that null properties are omitted from serialized output:

```csharp
[Fact]
public async Task GetAppMetadata_OmitsNullMainWindowTitle()
{
    var metadata = new AppMetadata
    {
        ProcessName = "TestApp", ProcessId = 1234,
        MainWindowTitle = null,  // null value
        AvaloniaVersion = "11.3.11", DotNetVersion = "10.0.0",
        WindowCount = 1, WindowWidth = 800, WindowHeight = 600
    };

    string result = JsonSerializer.Serialize(metadata, JsonOptions.Default);

    result.Should().NotContain("mainWindowTitle");
}
```

## Coverage Threshold

The project enforces 80% coverage across three metrics:

| Metric | Threshold |
|--------|-----------|
| Line coverage | 80% |
| Branch coverage | 80% |
| Method coverage | 80% |

These are configured in `tests/Directory.Build.props`:

```xml
<ThresholdType>line,branch,method</ThresholdType>
<ThresholdStat>total</ThresholdStat>
<Threshold>80</Threshold>
```

If coverage drops below 80% in any metric, the `dotnet test` command fails. Classes marked with `[ExcludeFromCodeCoverage]` (like `McpConnectorHost`) are excluded from the calculation.

## Adding New Tests

### Adding a New Server Tool Test

1. Create a new test class in `tests/Six.Avalonia.Mcp.Server.Tests.Unit/src/Tools/`.
2. Mock `IInspectorProxy` with NSubstitute.
3. Call the static tool method directly with the mock.
4. Assert on the returned result string.

```csharp
public class MyNewToolTests
{
    [Fact]
    public async Task MyTool_DelegatesToProxy()
    {
        IInspectorProxy proxy = Substitute.For<IInspectorProxy>();
        proxy.SendCommandAsync(Arg.Any<InspectorMethod>(), Arg.Any<Dictionary<string, object?>>(),
                Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("""{"result":"ok"}""");

        string result = await MyToolClass.MyTool(proxy);

        result.Should().Contain("ok");
    }
}
```

### Adding a New Inspector Integration Test

1. Add a new nested class inside `AvaloniaInspectorIntegrationTests`.
2. Use `[AvaloniaFact]` for the test attribute.
3. Call `CreateTestableInspector()` to get a test window and inspector.
4. Call the inspector method and assert on the result.
5. Close the window at the end.

```csharp
public class MyNewMethodIntegrationTests : AvaloniaInspectorIntegrationTests
{
    [AvaloniaFact]
    public async Task MyMethod_ReturnsExpectedResult()
    {
        (TestableInspector inspector, Window window) = CreateTestableInspector();

        var result = await inspector.MyMethodAsync("ActionButton");

        result.Should().NotBeNull();
        // Additional assertions

        window.Close();
    }
}
```

### Adding a New Command Handler Test

1. Create a test in `tests/Six.Avalonia.Mcp.Connector.Tests.Unit/src/`.
2. Mock `IAvaloniaInspector` with NSubstitute.
3. Create an `InspectorCommandHandler` with the mock inspector.
4. Build an `InspectorRequest` with the target method and parameters.
5. Assert on the `InspectorResponse`.

### Adding a New Model Test

1. Create a test class in `tests/Six.Avalonia.Mcp.Tests.Unit/src/Models/`.
2. Test serialization roundtrip with `JsonOptions.Default`.
3. Verify null property omission.
4. Test record equality if applicable.

### InternalsVisibleTo

If your new tests need to access `internal` methods on `AvaloniaInspector` or other classes, ensure the `InternalsVisibleTo` assembly attribute in the Connector library's `GlobalUsings.cs` includes the test project:

```csharp
[assembly: InternalsVisibleTo("McpDemo.Tests.Unit")]
[assembly: InternalsVisibleTo("McpDemo.Connector.Tests.Unit")]
```

Both test projects are already configured.

## Headless Platform Limitations

The Avalonia headless test platform (`Avalonia.Headless.XUnit`) supports:
- Control hierarchy creation and layout
- Event dispatching (pointer, keyboard, text input)
- Property reads (bounds, visibility, focus state)
- Visual tree traversal

It does **not** produce meaningful output for:
- `RenderTargetBitmap` rendering (screenshots return empty or minimal data)
- Visual rendering validation

Screenshot tests should verify that the code path executes without throwing, rather than asserting on the content of the captured image:

```csharp
[AvaloniaFact]
public async Task TakeScreenshotAsync_Window_DoesNotThrow()
{
    (TestableInspector inspector, Window window) = CreateTestableInspector();

    (string base64, int width, int height) = await inspector.TakeScreenshotAsync();

    base64.Should().NotBeNull();  // May be empty in headless mode

    window.Close();
}
```

## Related Pages

- [Architecture](./architecture.md) -- understanding the layers being tested
- [Configuration](./configuration.md) -- coverage and build settings
- [Troubleshooting](./troubleshooting.md) -- debugging test failures
