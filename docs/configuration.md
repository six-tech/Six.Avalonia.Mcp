# Configuration Reference

This document covers all build, runtime, and tooling configuration for the Six.Avalonia.Mcp project.

## MCP Server Configuration

The MCP server is a standalone console app (`apps/Six.Avalonia.Mcp.Server/`). It uses `ModelContextProtocol` version `0.8.0-preview.1`, configured in `Program.cs`:

```csharp
builder.Services.AddMcpServer(options =>
{
    options.ServerInfo = new Implementation
    {
        Name = "McpDemo.Server",
        Version = typeof(Program).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "0.0.0"
    };
})
.WithStdioServerTransport()
.WithToolsFromAssembly();
```

- **Server name**: `"McpDemo.Server"`
- **Server version**: Read from the assembly's `AssemblyInformationalVersionAttribute` (set by GitVersion or manually)
- **Transport**: STDIO only (HTTP transport is not configured)
- **Tool discovery**: Automatic via `WithToolsFromAssembly()`, which scans for `[McpServerToolType]` classes and `[McpServerTool]` methods in the server assembly

Tool descriptions are provided via `[System.ComponentModel.Description]` attributes, which the MCP SDK reads at runtime.

### Server Port Configuration

The server listens for SignalR connections from Avalonia apps on a configurable port:

```csharp
int port = builder.Configuration.GetValue("Port", 52342);
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(port);
});
```

- **Default port**: `52342`
- **Override via CLI**: `dotnet run --project apps/Six.Avalonia.Mcp.Server -- --Port=12345`
- **Override via environment**: `export Port=12345`
- **SignalR endpoint**: `/inspector` (mapped via `app.MapHub<InspectorHub>("/inspector")`)

### Server SignalR Configuration

The server's SignalR hub uses the shared `JsonOptions.Default` for serialization:

```csharp
builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions = JsonOptions.Default;
    });
```

### Server Services

```csharp
builder.Services.AddSingleton<IAppRegistry, AppRegistry>();
builder.Services.AddSingleton<IInspectorProxy, InspectorProxy>();
```

- **IAppRegistry / AppRegistry**: Tracks connected Avalonia apps by SignalR connection ID.
- **IInspectorProxy / InspectorProxy**: Routes MCP tool commands to the correct app and correlates responses by RequestId.

## Connector Configuration

The Connector library (`libs/Six.Avalonia.Mcp.Connector/`) is used by Avalonia applications to connect to the MCP server. Configuration is provided via `McpConnectorOptions`:

### McpConnectorOptions

| Property | Type | Default | Purpose |
|----------|------|---------|---------|
| `ServerUrl` | `string` | `"http://localhost:52342/inspector"` | URL of the MCP server's SignalR hub |
| `AppName` | `string` | Entry assembly name (fallback: `"Avalonia App"`) | Display name in the server's app registry |
| `AutoConnect` | `bool` | `true` | Whether to connect to the server on startup |
| `ReconnectDelay` | `TimeSpan` | `5 seconds` | Delay between reconnection attempts |
| `EnvironmentPrefix` | `string?` | `"SIX_MCP_"` | Env var prefix for option overrides; `null` to disable |

### Configuration Priority

Options are resolved by `McpConnectorPostConfigure` in this order (highest to lowest):

1. **Explicit values** from the user's `configure` lambda
2. **Environment variables** (`{prefix}ServerUrl`, `{prefix}AppName`, `{prefix}AutoConnect`, `{prefix}ReconnectDelay`)
3. **Assembly name auto-detection** (AppName only, from `Assembly.GetEntryAssembly()`)
4. **Property defaults**

Explicit code configuration always wins over environment variables. Malformed env var values are silently ignored.

### Registering the Connector

Use the `AddAvaloniaMcpConnector()` extension method on either `IServiceCollection` or `IHostBuilder`:

```csharp
// Option 1: Zero-config (auto-detects app name, uses defaults)
hostBuilder.AddAvaloniaMcpConnector();

// Option 2: IHostBuilder with custom options
hostBuilder.AddAvaloniaMcpConnector(options =>
{
    options.AppName = "My Avalonia App";
    options.ReconnectDelay = TimeSpan.FromSeconds(10);
});

// Option 3: IServiceCollection (with custom options)
services.AddAvaloniaMcpConnector(options =>
{
    options.ServerUrl = "http://localhost:9999/inspector";
    options.AppName = "My Avalonia App";
});
```

The `IHostBuilder` overload automatically:
- Configures logging to stderr at `Debug` level
- Registers `SIX_MCP_`-prefixed environment variables into the configuration system

### Registered Services

The `AddAvaloniaMcpConnector()` method registers:

| Service | Lifetime | Purpose |
|---------|----------|---------|
| `McpConnectorOptions` | Options pattern | Configuration via `IOptions<McpConnectorOptions>` |
| `McpConnectorPostConfigure` | Singleton | `IPostConfigureOptions`: env var overrides + AppName auto-detection |
| `IAvaloniaInspector` / `AvaloniaInspector` | Singleton | Visual tree inspection and interaction |
| `InspectorCommandHandler` | Singleton | Dispatches `InspectorRequest` to inspector methods |
| `ConnectorService` | Singleton + HostedService | SignalR client connection lifecycle |

## Logging Configuration

### Server Logging

The MCP STDIO transport uses stdout for JSON-RPC messages. All server logging **must** be directed to stderr:

```csharp
builder.Logging.ClearProviders();
builder.Logging.AddDebug();
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});
builder.Logging.SetMinimumLevel(LogLevel.Debug);
```

### Connector Logging

The `IHostBuilder` overload of `AddAvaloniaMcpConnector()` configures identical stderr logging automatically, and registers `SIX_MCP_`-prefixed environment variables into the configuration system:

```csharp
hostBuilder
    .ConfigureAppConfiguration(builder =>
    {
        if (prefix is not null)
            builder.AddEnvironmentVariables(prefix);
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddDebug();
        logging.AddConsole(options =>
        {
            options.LogToStandardErrorThreshold = LogLevel.Trace;
        });
        logging.SetMinimumLevel(LogLevel.Debug);
    })
    .ConfigureServices((_, services) => services.AddAvaloniaMcpConnector(configure));
```

When using the `IServiceCollection` overload, logging must be configured separately by the host application.

### Log Levels

| Level | Usage |
|-------|-------|
| `Trace` | Detailed diagnostics (rarely needed) |
| `Debug` | Development-time information, default minimum |
| `Information` | General operational messages |
| `Warning` | Logged when `GetTopLevel()` returns null or lifetime is wrong |
| `Error` | Exceptions during tool execution or SignalR communication |

### Changing Log Level at Runtime

Use the `SIX_MCP_` environment variable prefix (automatically registered by the `IHostBuilder` overload of `AddAvaloniaMcpConnector()`):

```bash
export SIX_MCP_Logging__LogLevel__Default=Warning
```

## Environment Variables

The `IHostBuilder` overload of `AddAvaloniaMcpConnector()` automatically registers `SIX_MCP_`-prefixed environment variables into the configuration system. No manual `AddEnvironmentVariables()` call is needed.

### Connector Option Overrides

`McpConnectorPostConfigure` checks for environment variables that override connector options (only when the option is still at its default value):

| Variable | Option | Type |
|----------|--------|------|
| `SIX_MCP_ServerUrl` | `ServerUrl` | string |
| `SIX_MCP_AppName` | `AppName` | string |
| `SIX_MCP_AutoConnect` | `AutoConnect` | bool (`true`/`false`) |
| `SIX_MCP_ReconnectDelay` | `ReconnectDelay` | seconds as double (e.g., `10.5`) |

### IConfiguration-Based Env Vars

| Variable | Maps To | Purpose |
|----------|---------|---------|
| `SIX_MCP_Logging__LogLevel__Default` | `Logging:LogLevel:Default` | Set minimum log level |
| `SIX_MCP_Logging__LogLevel__Microsoft` | `Logging:LogLevel:Microsoft` | Filter Microsoft framework logs |

The double underscore `__` is the .NET convention for the `:` hierarchy separator in environment variable names.

## Build Properties

The project uses a three-level `Directory.Build.props` hierarchy. Child files import their parent explicitly.

### Root Directory.Build.props

Located at the repository root. Applies to all projects (apps, libs, tests).

```xml
<PropertyGroup>
    <LangVersion>14</LangVersion>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <WarningsAsErrors>nullable</WarningsAsErrors>
    <NoWarn>CS1591</NoWarn>
    <SixVersion>1.0.0-alpha.1</SixVersion>
</PropertyGroup>
```

Key settings:

| Property | Value | Purpose |
|----------|-------|---------|
| `LangVersion` | `14` | C# 14 language features |
| `TargetFramework` | `net10.0` | .NET 10 |
| `Nullable` | `enable` | Nullable reference types enforced |
| `TreatWarningsAsErrors` | `true` | All warnings are build errors |
| `NoWarn` | `CS1591` | Suppress missing XML doc comments warning |
| `SixVersion` | `1.0.0-alpha.1` | Shared version property |

### apps/Directory.Build.props

Applies to application projects. Imports the root props.

```xml
<PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>Exe</OutputType>
</PropertyGroup>
```

### libs/Directory.Build.props

Applies to library projects. Imports the root props. Adds NuGet packaging, source link, and XML documentation.

```xml
<PropertyGroup>
    <IsPackable>true</IsPackable>
    <SignAssembly>true</SignAssembly>
    <GenerateDocumentationFile>True</GenerateDocumentationFile>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <Authors>Six Technology</Authors>
    <DebugType>portable</DebugType>
    <IncludeSymbols>true</IncludeSymbols>
    <SymbolPackageFormat>snupkg</SymbolPackageFormat>
</PropertyGroup>
```

### tests/Directory.Build.props

Applies to test projects. Does **not** import the root props (standalone).

```xml
<PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>14</LangVersion>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
</PropertyGroup>
```

See the [Coverage Settings](#coverage-settings) section below for the Coverlet configuration in this file.

## StyleCop Configuration

StyleCop analyzers are enabled globally in the root `Directory.Build.props`:

```xml
<PropertyGroup>
    <CodeAnalysisRuleSet>$(MSBuildThisFileDirectory).ruleset</CodeAnalysisRuleSet>
    <StyleCopTreatErrorsAsWarnings>false</StyleCopTreatErrorsAsWarnings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <AnalysisLevel>preview</AnalysisLevel>
</PropertyGroup>

<ItemGroup>
    <AdditionalFiles Include="$(MSBuildThisFileDirectory)stylecop.json" Link="stylecop.json" />
</ItemGroup>
```

- **`.ruleset`**: Located at the repository root, defines which StyleCop rules are enabled/disabled.
- **`stylecop.json`**: Located at the repository root, configures StyleCop behavior (documentation requirements, naming conventions, etc.).
- **`StyleCopTreatErrorsAsWarnings`**: Set to `false`, meaning StyleCop issues are errors, not demoted to warnings.
- Combined with `TreatWarningsAsErrors`, any StyleCop violation fails the build.

## Coverage Settings

Coverage is configured in `tests/Directory.Build.props` using Coverlet:

```xml
<PropertyGroup>
    <CollectCoverage>true</CollectCoverage>
    <CoverletOutputFormat>cobertura;html;json</CoverletOutputFormat>
    <CoverletOutput>$(MSBuildThisFileDirectory)coverage\</CoverletOutput>
    <ThresholdType>line,branch,method</ThresholdType>
    <ThresholdStat>total</ThresholdStat>
    <Threshold>80</Threshold>
</PropertyGroup>
```

| Property | Value | Purpose |
|----------|-------|---------|
| `CollectCoverage` | `true` | Enable coverage collection on every test run |
| `CoverletOutputFormat` | `cobertura;html;json` | Generate three report formats |
| `Threshold` | `80` | Minimum 80% coverage required |
| `ThresholdType` | `line,branch,method` | Apply threshold to all three metrics |
| `ThresholdStat` | `total` | Evaluate threshold across all assemblies combined |

### Exclusions

```xml
<ExcludeByAttribute>
    ExcludeFromCodeCoverage, Obsolete, GeneratedCodeAttribute, CompilerGeneratedAttribute
</ExcludeByAttribute>
<ExcludeByFile>**/bin/**/*,**/obj/**/*,**/test/**/*</ExcludeByFile>
```

Classes marked with `[ExcludeFromCodeCoverage]` (such as `McpConnectorHost`) are excluded from coverage calculations.

### Running Coverage

```bash
dotnet test --collect:"XPlat Code Coverage"
```

Reports are written to `tests/coverage/`.

## CI/CD Build Settings

Release builds enable deterministic compilation for reproducibility:

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
    <ContinuousIntegrationBuild
        Condition="'$(CI)' == 'true' or '$(GITHUB_ACTIONS)' == 'true' or '$(TF_BUILD)' == 'true'">
        true
    </ContinuousIntegrationBuild>
    <Deterministic>true</Deterministic>
    <DeterministicSourcePaths>true</DeterministicSourcePaths>
    <UseSharedCompilation>false</UseSharedCompilation>
    <DisableImplicitNuGetFallbackFolder>true</DisableImplicitNuGetFallbackFolder>
</PropertyGroup>
```

## Package References

### Contracts (`libs/Six.Avalonia.Mcp.Contracts/`)

Zero external package dependencies. Only uses `System.Text.Json` from the .NET SDK.

### Connector (`libs/Six.Avalonia.Mcp.Connector/`)

```xml
<ItemGroup>
    <PackageReference Include="Avalonia" Version="11.3.11"/>
    <PackageReference Include="Microsoft.AspNetCore.SignalR.Client" Version="10.0.2"/>
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="10.0.2"/>
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.2"/>
</ItemGroup>

<ItemGroup>
    <ProjectReference Include="..\Six.Avalonia.Mcp.Contracts\Six.Avalonia.Mcp.Contracts.csproj"/>
</ItemGroup>
```

### Server (`apps/Six.Avalonia.Mcp.Server/`)

Uses the `Microsoft.NET.Sdk.Web` SDK, which includes ASP.NET Core and SignalR server-side support:

```xml
<ItemGroup>
    <PackageReference Include="ModelContextProtocol" Version="0.8.0-preview.1"/>
</ItemGroup>

<ItemGroup>
    <ProjectReference Include="..\..\libs\Six.Avalonia.Mcp.Contracts\Six.Avalonia.Mcp.Contracts.csproj"/>
</ItemGroup>
```

The server is also packaged as a dotnet tool:

```xml
<PropertyGroup>
    <PackAsTool>true</PackAsTool>
    <ToolCommandName>six-avalonia-mcp</ToolCommandName>
    <IsPackable>true</IsPackable>
</PropertyGroup>
```

## Test Project Package References

Test-specific packages in `tests/Six.Avalonia.Mcp.Tests.Unit/`:

```xml
<ItemGroup>
    <PackageReference Include="Avalonia.Headless.XUnit" Version="11.3.11" />
    <PackageReference Include="Avalonia.Themes.Fluent" Version="11.3.11" />
    <PackageReference Include="NSubstitute" Version="5.3.0" />
</ItemGroup>
```

Shared test packages from `tests/Directory.Build.props`:

```xml
<ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.0.1" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5" />
    <PackageReference Include="coverlet.collector" Version="6.0.4" />
    <PackageReference Include="FluentAssertions" Version="8.8.0" />
</ItemGroup>
```

## Related Pages

- [Getting Started](./getting-started.md) -- initial setup
- [Architecture](./architecture.md) -- system design, threading model, and element resolution
- [Testing Guide](./testing-guide.md) -- test framework and patterns
