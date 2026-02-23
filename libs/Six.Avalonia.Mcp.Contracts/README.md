# Six.Avalonia.Mcp.Contracts

Shared contracts (DTOs, hub interfaces, and messages) for the Six.Avalonia.Mcp ecosystem. This package has zero framework dependencies and is referenced by both the Connector (Avalonia app side) and the Server (MCP server side).

## Install

```bash
dotnet add package Six.Avalonia.Mcp.Contracts
```

## Model DTOs

Seven sealed record types in `Six.Avalonia.Mcp.Contracts.Models`:

| Model | Description |
|-------|-------------|
| `AppMetadata` | Application process info, window dimensions, framework versions |
| `BoundsInfo` | Bounding rectangle (x, y, width, height) in DIPs |
| `VisualTreeNode` | Recursive visual tree node with children, bounds, and state |
| `FindElementResult` | Search result with element path and stable ID |
| `ElementProperties` | 23 detailed runtime properties across 7 categories |
| `InteractionResult` | Success/failure result with post-interaction element state |
| `ConnectedAppInfo` | App registration DTO (appId, appName, processId, connectedAt) |

All models use `required` init-only properties and are serialized with camelCase naming via `JsonOptions.Default`.

## Hub Interfaces

SignalR hub contracts in `Six.Avalonia.Mcp.Contracts.Hub`:

| Interface | Direction | Description |
|-----------|-----------|-------------|
| `IInspectorHub` | Client -> Server | App registration and response delivery |
| `IInspectorClient` | Server -> Client | Command execution and disconnect requests |

## Messages

Command/response envelope types in `Six.Avalonia.Mcp.Contracts.Messages`:

| Type | Description |
|------|-------------|
| `InspectorMethod` | Enum of all inspector operations (GetAppMetadata, GetVisualTree, etc.) |
| `InspectorRequest` | Command envelope with method, parameters, and request ID |
| `InspectorResponse` | Response envelope with result data, error info, and request ID |

## JSON Serialization

`JsonOptions.Default` provides shared serialization settings:

- camelCase property naming
- Null values omitted (`JsonIgnoreCondition.WhenWritingNull`)
- Compact (non-indented) output
- Case-insensitive deserialization

```csharp
using Six.Avalonia.Mcp.Contracts;

string json = JsonSerializer.Serialize(model, JsonOptions.Default);
var parsed = JsonSerializer.Deserialize<AppMetadata>(json, JsonOptions.Default);
```

## Documentation

See the [full documentation](../../docs/index.md) for architecture details and API reference.
