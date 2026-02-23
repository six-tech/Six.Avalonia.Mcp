# 2026-02-17 18:11:31 UTC | PRD: avalonia-runtime-mcp-server-for-ai-agents-with-i | Task: sprint-2-task-7 | Sprint: Inspector Interface, Element Resolution, and DI Registration

**Description**: Create unit tests for element resolution helpers in `tests/Six.Avalonia.Mcp.Tests.Unit/src/Services/AvaloniaInspectorTests.cs`. Since Avalonia controls can't be easily instantiated in unit tests without a platform, test `ParseKeyCombination` (Sprint 5) and model-level logic. For `ResolveElement`/`BuildPath`/`GenerateElementId`, add tests that document the expected behavior (may need to be integration-level or use Avalonia headless platform). Verify the solution builds and tests pass.

Avalonia headless testing setup: Add `Avalonia.Headless.XUnit` and `Avalonia.Themes.Fluent` NuGet packages to test project. Create TestApp class extending Application with FluentTheme, and TestAppBuilder with `AppBuilder.Configure<TestApp>().UseHeadless(...)`. Add `[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]`. Use `[AvaloniaFact]` instead of `[Fact]` for tests that create Avalonia controls. Panel.Children.Add() properly populates VisualChildren for headless tests.

# 2026-02-17 18:11:31 UTC | PRD: avalonia-runtime-mcp-server-for-ai-agents-with-i | Task: sprint-2-task-7 | Sprint: Inspector Interface, Element Resolution, and DI Registration

**Description**: Create unit tests for element resolution helpers in `tests/Six.Avalonia.Mcp.Tests.Unit/src/Services/AvaloniaInspectorTests.cs`. Since Avalonia controls can't be easily instantiated in unit tests without a platform, test `ParseKeyCombination` (Sprint 5) and model-level logic. For `ResolveElement`/`BuildPath`/`GenerateElementId`, add tests that document the expected behavior (may need to be integration-level or use Avalonia headless platform). Verify the solution builds and tests pass.

MCP SDK 0.8.0-preview.1 API: `services.AddMcpServer(options => { options.ServerInfo = new() { Name = "...", Version = "..." }; })` returns `IMcpServerBuilder`. Chain `.WithStdioServerTransport()` (no params) and `.WithToolsFromAssembly(assembly)` (takes Assembly, optionally JsonSerializerOptions). Namespace for extension methods: `Microsoft.Extensions.DependencyInjection`.

# 2026-02-17 18:26:25 UTC | PRD: avalonia-runtime-mcp-server-for-ai-agents-with-i | Task: sprint-3-task-8 | Sprint: Inspection Tools (Visual Tree, Find Element, Properties, Ancestors)

**Description**: Create `libs/Six.Avalonia.Mcp/src/Tools/InspectionTools.cs` — `[McpServerToolType]` static class with 4 tools: `get_visual_tree`, `find_element`, `get_element_properties`, `get_ancestor_chain`. Each takes `IAvaloniaInspector` + parameters, delegates to inspector, serializes result. All wrapped in try/catch. Verify solution builds. Add unit tests for tool classes in `tests/Six.Avalonia.Mcp.Tests.Unit/src/Tools/InspectionToolsTests.cs` and `MetadataToolsTests.cs` — mock `IAvaloniaInspector`, verify JSON serialization and error handling.

Avalonia 11.3.11: `StyledElement.PseudoClasses` is protected — cannot access from outside. Use `StyledElement.Classes` which contains both style classes and pseudo-classes (pseudo-classes start with `:`). In headless tests, `Application.Current.ApplicationLifetime` is null — no `IClassicDesktopStyleApplicationLifetime` available, so `GetMainWindow()` returns null.

# 2026-02-17 18:26:25 UTC | PRD: avalonia-runtime-mcp-server-for-ai-agents-with-i | Task: sprint-3-task-8 | Sprint: Inspection Tools (Visual Tree, Find Element, Properties, Ancestors)

**Description**: Create `libs/Six.Avalonia.Mcp/src/Tools/InspectionTools.cs` — `[McpServerToolType]` static class with 4 tools: `get_visual_tree`, `find_element`, `get_element_properties`, `get_ancestor_chain`. Each takes `IAvaloniaInspector` + parameters, delegates to inspector, serializes result. All wrapped in try/catch. Verify solution builds. Add unit tests for tool classes in `tests/Six.Avalonia.Mcp.Tests.Unit/src/Tools/InspectionToolsTests.cs` and `MetadataToolsTests.cs` — mock `IAvaloniaInspector`, verify JSON serialization and error handling.

MCP SDK 0.8.0-preview.1: `McpServerToolAttribute` has `Name` property but no `Description` property. Use `[System.ComponentModel.Description("...")]` as a separate attribute for tool descriptions. The SDK reads it from there.

# 2026-02-17 18:39:45 UTC | PRD: avalonia-runtime-mcp-server-for-ai-agents-with-i | Task: sprint-5-task-7 | Sprint: Basic Interaction Tools (Click, Type, Control State)

**Description**: Create unit tests: `tests/Six.Avalonia.Mcp.Tests.Unit/src/Tools/InteractionToolsTests.cs` — mock inspector, verify delegation and JSON output for click/type/state tools. Also test `ParseKeyCombination` directly (internal access). Test edge cases: unknown button name, empty text, invalid state type. Verify solution builds and tests pass.

Avalonia 11.3.11: `IPointer` is NOT user-implementable — it has a hidden interface method `(This interface or abstract class is -not- implementable by user code !)()`. Use `new Pointer(id: 0, type: PointerType.Mouse, isPrimary: true)` from `Avalonia.Input` instead. The `Pointer` class is concrete with constructor `(int id, PointerType type, bool isPrimary)`.

# 2026-02-17 18:44:33 UTC | PRD: avalonia-runtime-mcp-server-for-ai-agents-with-i | Task: sprint-6-task-5 | Sprint: Advanced Interaction Tools (Hover, Drag, Keyboard Shortcuts)

**Description**: Create/extend unit tests in `InteractionToolsTests.cs` — test hover, drag, and keyboard shortcut tool methods with mocked inspector. Test `ParseKeyCombination` edge cases: single key ("F5"), modifier-only ("Ctrl"), triple modifier ("Ctrl+Shift+Alt+S"), case insensitivity. Verify solution builds and all tests pass.

Avalonia 11.3.11: To get FocusManager, use `(window as TopLevel)?.FocusManager?.GetFocusedElement()`. The static `FocusManager.GetFocusManager()` does NOT exist. The property is on `TopLevel`.

# 2026-02-17 20:06:34 UTC | PRD: avalonia-runtime-mcp-server-for-ai-agents-with-i | Task: sprint-7-task-6 | Sprint: Demo App UI and End-to-End Verification

**Description**: Verify MCP tool discovery works: confirm that running the demo app outputs MCP JSON-RPC responses on stdout when MCP initialize/list_tools requests are sent on stdin. Document any issues found.

**Avalonia Headless Testing - Screenshot Limitation**: In headless mode (`Avalonia.Headless.XUnit`), `RenderTargetBitmap.Render()` produces empty output (0 bytes). Screenshot tests should verify non-throwing execution rather than asserting non-empty base64 content.

**FluentAssertions 8.x API**: Use `HaveCountGreaterThanOrEqualTo(n)` instead of the old `HaveCountGreaterOrEqualTo(n)`.

**AvaloniaInspector Testability**: The class was unsealed and `GetMainWindow()` changed from `private` to `internal virtual` to allow test subclasses to override window resolution. This enables integration testing of all async methods without requiring `IClassicDesktopStyleApplicationLifetime`.

**MCP SDK STDIO Protocol**: The MCP SDK 0.8.0-preview.1 uses newline-delimited JSON (NDJSON) for STDIO transport, not HTTP-style Content-Length framing. Send one JSON-RPC message per line.

**Code Coverage Strategy**: `ExcludeFromCodeCoverage` on `McpServerHost` (DI wiring) + integration tests with `TestableInspector` subclass brought coverage from 47.7% to 95.5% line / 80.5% branch.

