# Integration Guide

This guide covers how to integrate Six.Avalonia.Mcp into your Avalonia application, configure MCP clients, customize the inspector, and work with multiple connected apps.

## Architecture

Six.Avalonia.Mcp uses a decoupled two-step architecture. The MCP server runs as a standalone process, and your Avalonia app connects to it via SignalR:

```
AI Agent  <--STDIO-->  MCP Server (port 52342)  <--SignalR-->  Avalonia App(s)
```

This means your app does **not** embed the MCP server or STDIO transport. Instead, it uses the lightweight Connector library to connect to the server over SignalR WebSocket.

## Step 1: Install the MCP Server

The MCP server is distributed as a dotnet tool:

```bash
dotnet tool install -g Six.Avalonia.Mcp.Server
```

This installs the `six-avalonia-mcp` command. The server:

- Communicates with AI agents via STDIO (JSON-RPC)
- Listens for Avalonia app connections on port 52342 via SignalR
- Exposes 16 MCP tools (13 inspection + 3 app management)

Alternatively, you can build and run the server from source:

```bash
dotnet run --project apps/Six.Avalonia.Mcp.Server
```

## Step 2: Add the Connector to Your Avalonia App

### Add the Package Reference

```bash
dotnet add package Six.Avalonia.Mcp.Connector
```

Or add it directly to your `.csproj`:

```xml
<PackageReference Include="Six.Avalonia.Mcp.Connector" />
```

### Set Up the Host Builder

The Connector requires a `Microsoft.Extensions.Hosting` host. In your `Program.cs`, create a host builder and call `AddAvaloniaMcpConnector()`:

```csharp
using Avalonia;
using Microsoft.Extensions.Hosting;
using Six.Avalonia.Mcp.Connector;

sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Build the host (registers inspector + SignalR connector)
        IHost host = Host.CreateDefaultBuilder(args)
            .AddAvaloniaMcpConnector()
            .Build();

        // Make DI container available to your App class
        App.Services = host.Services;

        // Start the host (begins SignalR connection to MCP server)
        _ = host.StartAsync();

        // Start Avalonia with classic desktop lifetime
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}
```

Zero configuration is needed. The connector auto-detects the app name from the entry assembly and uses sensible defaults. Environment variables with the `SIX_MCP_` prefix are automatically registered. To customize:

```csharp
.AddAvaloniaMcpConnector(options =>
{
    options.AppName = "My App";
})
```

### Expose the Service Provider

Your `App` class needs a static `Services` property so that the host's DI container is accessible:

```csharp
public partial class App : Application
{
    public static IServiceProvider? Services { get; set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services?.GetService<MainWindowViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
```

## Using IHostBuilder vs IServiceCollection

Two extension method overloads are available:

### IHostBuilder Extension (Recommended)

```csharp
// Zero-config: auto-detects app name, uses default server URL
IHost host = Host.CreateDefaultBuilder(args)
    .AddAvaloniaMcpConnector()
    .Build();

// With custom options
IHost host = Host.CreateDefaultBuilder(args)
    .AddAvaloniaMcpConnector(options =>
    {
        options.AppName = "My App";
    })
    .Build();
```

This overload automatically:
- Configures logging to stderr at Debug level (stdout is reserved for the MCP STDIO transport)
- Registers `SIX_MCP_`-prefixed environment variables into the configuration system
- Registers `McpConnectorPostConfigure` for env var overrides and app name auto-detection

It internally calls `ConfigureServices` and delegates to the `IServiceCollection` overload.

### IServiceCollection Extension

If you already have a service collection (e.g., in a custom host setup), you can register directly:

```csharp
services.AddAvaloniaMcpConnector(options =>
{
    options.AppName = "My App";
});
```

Note: this overload does **not** configure logging or register environment variables into IConfiguration. If you need those, configure them separately or use the `IHostBuilder` overload.

### What `AddAvaloniaMcpConnector()` Registers

Both overloads register the same services:

1. **`IAvaloniaInspector`** as a singleton (`AvaloniaInspector`) -- inspects and interacts with the Avalonia visual tree
2. **`InspectorCommandHandler`** -- dispatches incoming `InspectorRequest` messages to the appropriate `IAvaloniaInspector` method
3. **`ConnectorService`** as an `IHostedService` -- manages the SignalR connection to the MCP server and routes commands
4. **`McpConnectorPostConfigure`** as `IPostConfigureOptions<McpConnectorOptions>` -- applies environment variable overrides and auto-detects `AppName` from the entry assembly

### McpConnectorOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ServerUrl` | `string` | `"http://localhost:52342/inspector"` | URL of the MCP server's SignalR hub |
| `AppName` | `string` | Entry assembly name (fallback: `"Avalonia App"`) | Display name for this app in the server registry |
| `AutoConnect` | `bool` | `true` | Whether to automatically connect on startup |
| `ReconnectDelay` | `TimeSpan` | 5 seconds | Delay between reconnection attempts |
| `EnvironmentPrefix` | `string?` | `"SIX_MCP_"` | Env var prefix for option overrides; set to `null` to disable |

### Configuration Priority

Options are resolved in this order (highest to lowest priority):

1. **Explicit values** from the user's `configure` lambda
2. **Environment variables** (`SIX_MCP_ServerUrl`, `SIX_MCP_AppName`, `SIX_MCP_AutoConnect`, `SIX_MCP_ReconnectDelay`)
3. **Assembly name auto-detection** (AppName only, from `Assembly.GetEntryAssembly()`)
4. **Property defaults**

This means code-level configuration always wins over environment variables, and env vars win over auto-detection.

## Naming Controls for Discoverability

MCP tools use `x:Name` to locate controls. Follow these best practices:

### Name Every Interactive Control

```xml
<!-- Good: Named controls are discoverable -->
<TextBox x:Name="SearchTextBox" />
<Button x:Name="SubmitButton" Content="Submit" />
<CheckBox x:Name="AgreeCheckBox" Content="I agree" />

<!-- Bad: Unnamed controls can only be found by type or path -->
<TextBox />
<Button Content="Submit" />
```

### Use Descriptive, Unique Names

```xml
<!-- Good: Clear purpose, unique across the window -->
<TextBox x:Name="EmailInputTextBox" />
<TextBox x:Name="PasswordInputTextBox" />
<Button x:Name="LoginButton" />

<!-- Bad: Ambiguous or generic -->
<TextBox x:Name="TextBox1" />
<TextBox x:Name="TextBox2" />
<Button x:Name="Btn" />
```

### Name Layout Containers Too

Naming layout containers helps agents understand the visual hierarchy:

```xml
<Grid x:Name="RootGrid">
    <StackPanel x:Name="InputPanel">
        <!-- Input controls -->
    </StackPanel>
    <StackPanel x:Name="OutputPanel">
        <!-- Output controls -->
    </StackPanel>
</Grid>
```

### Name Status and Output Elements

```xml
<Border x:Name="StatusBar">
    <TextBlock x:Name="StatusText" Text="{Binding StatusMessage}" />
</Border>
```

## MCP Client Configuration

Point your MCP client at the `six-avalonia-mcp` dotnet tool. The MCP client launches the server process; the server communicates with your app(s) via SignalR.

### Claude Desktop

Create or edit the Claude Desktop configuration file. On macOS this is `~/Library/Application Support/Claude/claude_desktop_config.json`. On Windows it is `%APPDATA%\Claude\claude_desktop_config.json`.

```json
{
  "mcpServers": {
    "avalonia-inspector": {
      "command": "six-avalonia-mcp",
      "args": []
    }
  }
}
```

### Claude Code

Add to `.claude/settings.json` in your project or home directory:

```json
{
  "mcpServers": {
    "avalonia-inspector": {
      "command": "six-avalonia-mcp",
      "args": []
    }
  }
}
```

### VS Code

Add to `.vscode/settings.json`:

```json
{
  "mcp": {
    "servers": {
      "avalonia-inspector": {
        "command": "six-avalonia-mcp"
      }
    }
  }
}
```

### Codex

Configure via the Codex MCP settings to run the `six-avalonia-mcp` command.

### Programmatic MCP Client

Any MCP client that supports STDIO transport can connect by spawning the `six-avalonia-mcp` process and communicating over stdin/stdout using newline-delimited JSON-RPC (NDJSON):

```csharp
var process = new Process
{
    StartInfo = new ProcessStartInfo
    {
        FileName = "six-avalonia-mcp",
        RedirectStandardInput = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    }
};
process.Start();

// Send a JSON-RPC request (NDJSON: one JSON object per line)
string request = """{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"get_app_metadata","arguments":{}}}""";
await process.StandardInput.WriteLineAsync(request);

// Read the response
string response = await process.StandardOutput.ReadLineAsync();
```

## Custom Inspector Subclass

The `AvaloniaInspector` class (in namespace `Six.Avalonia.Mcp.Connector.Services`) has a `protected virtual` method `GetTopLevel()` that you can override. This is useful for:

- **Multi-window applications**: Return a different top-level based on context.
- **Testing**: Inject a controlled test window (see [Testing Guide](./testing-guide.md)).
- **Custom resolution logic**: Add fallback behavior when the top-level is unavailable.

The default implementation checks for `IClassicDesktopStyleApplicationLifetime` (returns `MainWindow`) and `ISingleViewApplicationLifetime` (returns `TopLevel.GetTopLevel(MainView)`).

```csharp
using Avalonia.Controls;
using Microsoft.Extensions.Logging;
using Six.Avalonia.Mcp.Connector.Services;

public class MultiWindowInspector : AvaloniaInspector
{
    private readonly Func<TopLevel?> _topLevelSelector;

    public MultiWindowInspector(
        Func<TopLevel?> topLevelSelector,
        ILogger<AvaloniaInspector> logger) : base(logger)
    {
        _topLevelSelector = topLevelSelector;
    }

    protected override TopLevel? GetTopLevel()
    {
        return _topLevelSelector();
    }
}
```

Register your custom inspector instead of the default:

```csharp
services.AddAvaloniaMcpConnector(options =>
{
    options.AppName = "My Multi-Window App";
});

// Override the default IAvaloniaInspector registration (must come AFTER AddAvaloniaMcpConnector)
services.AddSingleton<IAvaloniaInspector>(sp =>
    new MultiWindowInspector(
        () => GetActiveTopLevel(),
        sp.GetRequiredService<ILogger<AvaloniaInspector>>()
    ));
```

Note: The custom `IAvaloniaInspector` registration must come **after** `AddAvaloniaMcpConnector()` to override the default.

## Multi-App Targeting

The MCP server supports multiple Avalonia apps connected simultaneously via SignalR. Each app registers with its `AppName` and receives a unique `appId`.

### How It Works

- When only **one app** is connected, all MCP tool calls are automatically routed to it.
- When **multiple apps** are connected, you can target a specific app in two ways:

**Option 1: Pass `appId` directly to any tool call**

All 12 inspection MCP tools accept an optional `appId` parameter. Pass the app's unique ID to target it directly.

**Option 2: Use the app management tools**

1. Call `list_connected_apps` to see all connected apps with their IDs and names.
2. Call `set_target_app` with the desired `appId` to set the default target.
3. Subsequent tool calls without an explicit `appId` will use the default target.

## Environment Variable Configuration

The `IHostBuilder` overload of `AddAvaloniaMcpConnector()` automatically registers `SIX_MCP_`-prefixed environment variables into the .NET configuration system. No manual `AddEnvironmentVariables()` call is needed.

### Connector Option Overrides

The `McpConnectorPostConfigure` checks for environment variables that match connector options (only when the option is still at its default value):

| Environment Variable | Option | Type |
|---------------------|--------|------|
| `SIX_MCP_ServerUrl` | `ServerUrl` | string |
| `SIX_MCP_AppName` | `AppName` | string |
| `SIX_MCP_AutoConnect` | `AutoConnect` | bool (`true`/`false`) |
| `SIX_MCP_ReconnectDelay` | `ReconnectDelay` | seconds as double (e.g., `10.5`) |

Malformed values are silently ignored. Explicit code configuration always wins over env vars.

### IConfiguration-Based Env Vars

Any environment variable starting with `SIX_MCP_` is also loaded into the `IConfiguration` system:

| Environment Variable | Configuration Key |
|---------------------|-------------------|
| `SIX_MCP_Logging__LogLevel__Default` | `Logging:LogLevel:Default` |
| `SIX_MCP_MyCustomSetting` | `MyCustomSetting` |

Double underscores (`__`) are translated to the `:` hierarchy separator in .NET configuration.

### Custom Prefix

To change or disable the environment variable prefix:

```csharp
// Use a custom prefix
.AddAvaloniaMcpConnector(options =>
{
    options.EnvironmentPrefix = "MYAPP_";
})

// Disable env var binding entirely
.AddAvaloniaMcpConnector(options =>
{
    options.EnvironmentPrefix = null;
})
```

## Related Pages

- [Getting Started](./getting-started.md) -- quick setup walkthrough
- [Architecture](./architecture.md) -- system design and threading model
- [Configuration](./configuration.md) -- build properties and tooling
- [Troubleshooting](./troubleshooting.md) -- common issues and solutions
