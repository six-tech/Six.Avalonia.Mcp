# McpDemo

Reference Avalonia desktop application demonstrating [Six.Avalonia.Mcp.Connector](../../libs/Six.Avalonia.Mcp.Connector/) integration. Use this as a starting point for adding MCP inspector support to your own Avalonia app.

## What It Does

McpDemo is a simple Avalonia app with common controls (TextBox, Button, CheckBox, Slider, ComboBox, ListBox) wired up to a ViewModel. It connects to the [Six MCP Server](../Six.Avalonia.Mcp.Server/) via SignalR, allowing AI agents to inspect the visual tree, query element properties, and simulate user interactions in real time.

## Run

```bash
# Start the MCP server first (or let your MCP client launch it)
six-avalonia-mcp

# Then start the demo app
dotnet run --project apps/McpDemo
```

The app auto-connects to the MCP server on `http://localhost:52342/inspector`.

## Integration Pattern

The demo follows the standard two-step integration described in the [Getting Started](../../docs/getting-started.md) guide:

### 1. Register the Connector in `Program.cs`

```csharp
using Six.Avalonia.Mcp.Connector;

IHost host = Host.CreateDefaultBuilder(args)
    .AddAvaloniaMcpConnector(options =>
    {
        options.AppName = "Six Demo App";
    })
    .ConfigureAppConfiguration(builder =>
    {
        builder
            .AddJsonFile("appSettings.json", true, true)
            .AddUserSecrets<Program>(true);
    })
    .ConfigureServices((context, services) =>
    {
        services.AddTransient<ViewModels.MainWindowViewModel>();
    })
    .Build();

App.Services = host.Services;
_ = host.StartAsync();

BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
```

Note: `ServerUrl` defaults to `http://localhost:52342/inspector` and `SIX_MCP_`-prefixed environment variables are automatically registered by `AddAvaloniaMcpConnector()` — no manual setup needed.

### 2. Expose `IServiceProvider` in `App.cs`

```csharp
public partial class App : Application
{
    public static IServiceProvider? Services { get; set; }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var vm = Services?.GetRequiredService<MainWindowViewModel>()
                     ?? new MainWindowViewModel();
            desktop.MainWindow = new MainWindow { DataContext = vm };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
```

### 3. Name controls with `x:Name`

```xml
<TextBox x:Name="InputTextBox" Watermark="Type something here..." />
<Button x:Name="ActionButton" Content="Click Me" />
<CheckBox x:Name="ToggleCheckBox" Content="Toggle Option" />
```

## Controls

| Control | Name | Purpose |
|---------|------|---------|
| TextBox | `InputTextBox` | Text input with data binding |
| Button | `ActionButton` | Click interaction with status update |
| CheckBox | `ToggleCheckBox` | Boolean toggle |
| Slider | `ValueSlider` | Numeric range input (0-100) |
| ComboBox | `SelectComboBox` | Dropdown selection |
| ListBox | `ItemListBox` | Scrollable item list |

## Configuration

Override connector options via environment variables (automatically registered by the connector):

```bash
export SIX_MCP_ServerUrl=http://custom-host:9999/inspector
export SIX_MCP_AppName=MyOverrideName
export SIX_MCP_AutoConnect=false
export SIX_MCP_ReconnectDelay=10
```

Or via `appSettings.json`:

```json
{
  "McpConnector": {
    "ServerUrl": "http://localhost:52342/inspector",
    "AppName": "Six Demo App"
  }
}
```

Environment variable overrides only apply when the option is at its default value — explicit code configuration always wins.

## Documentation

- [Getting Started](../../docs/getting-started.md) -- step-by-step setup guide
- [Integration Guide](../../docs/integration-guide.md) -- detailed configuration and custom inspectors
- [Tool Reference](../../docs/tools/) -- all 16 MCP tools
