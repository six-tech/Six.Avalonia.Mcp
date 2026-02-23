using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using ModelContextProtocol.Client;
using Six.Avalonia.Mcp.TestRunner;

// Find solution root by walking up from base directory.
string? solutionRoot = FindSolutionRoot(AppContext.BaseDirectory);
if (solutionRoot is null)
{
    Console.Error.WriteLine("Could not find solution root (Six.Avalonia.Mcp.slnx).");
    return 1;
}

// Pick a random free port.
int port = GetFreePort();

// Resolve DLLs — requires a prior `dotnet build`.
string serverDll = ResolveDll(solutionRoot, "apps/Six.Avalonia.Mcp.Server", "Six.Avalonia.Mcp.Server");
string demoDll = ResolveDll(solutionRoot, "apps/McpDemo", "McpDemo");

if (!File.Exists(serverDll))
{
    Console.Error.WriteLine($"Server DLL not found: {serverDll}");
    Console.Error.WriteLine("Run `dotnet build` first.");
    return 1;
}

if (!File.Exists(demoDll))
{
    Console.Error.WriteLine($"Demo app DLL not found: {demoDll}");
    Console.Error.WriteLine("Run `dotnet build` first.");
    return 1;
}

// ── Start MCP server via STDIO transport ───────────────────────────────
Console.WriteLine($"Starting MCP server on port {port}...");

var transport = new StdioClientTransport(new StdioClientTransportOptions
{
    Command = "dotnet",
    Arguments = ["exec", serverDll, $"--Port={port}"],
    Name = "Six.Avalonia.Mcp.Server",
});

await using var client = await McpClient.CreateAsync(transport);

Console.WriteLine($"MCP server started on port {port}");

// ── Start demo app as a background process ─────────────────────────────
Console.WriteLine("Starting demo app...");

var demoProcess = new Process();
demoProcess.StartInfo = new ProcessStartInfo
{
    FileName = "dotnet",
    Arguments = $"exec \"{demoDll}\"",
    UseShellExecute = false,
    RedirectStandardOutput = true,
    RedirectStandardError = true,
};
demoProcess.StartInfo.Environment["SIX_MCP_ServerUrl"] = $"http://localhost:{port}/inspector";
demoProcess.Start();

// ── Wait for demo app to connect ───────────────────────────────────────
var toolClient = new McpToolClient(client);

Console.WriteLine("Waiting for demo app to connect...");
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
bool appConnected = false;

try
{
    while (!cts.Token.IsCancellationRequested)
    {
        string response = await toolClient.ListConnectedAppsAsync(cts.Token);

        using var doc = JsonDocument.Parse(response);
        if (doc.RootElement.TryGetProperty("count", out var countProp) && countProp.GetInt32() > 0)
        {
            appConnected = true;
            Console.WriteLine("App connected!");
            Console.WriteLine();
            break;
        }

        await Task.Delay(2000, cts.Token);
    }
}
catch (OperationCanceledException)
{
    // Timeout reached.
}

if (!appConnected)
{
    Console.Error.WriteLine("Timed out waiting for demo app to connect (30s).");
    KillDemo(demoProcess);
    return 1;
}

// ── Run test scenarios ─────────────────────────────────────────────────
var runner = new TestRunner(toolClient);
await runner.RunAllAsync();
runner.PrintSummary();

// ── Tear down ──────────────────────────────────────────────────────────
KillDemo(demoProcess);

return runner.AllPassed ? 0 : 1;

// ── Helpers ────────────────────────────────────────────────────────────

static void KillDemo(Process process)
{
    try
    {
        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
        }
    }
    catch
    {
        // Best-effort cleanup.
    }
}

static string? FindSolutionRoot(string startDir)
{
    string? dir = startDir;
    while (dir is not null)
    {
        if (File.Exists(Path.Combine(dir, "Six.Avalonia.Mcp.slnx")))
        {
            return dir;
        }

        dir = Directory.GetParent(dir)?.FullName;
    }

    return null;
}

static string ResolveDll(string solutionRoot, string projectRelPath, string assemblyName)
{
    string projectDir = Path.Combine(solutionRoot, projectRelPath.Replace('/', Path.DirectorySeparatorChar));

    string debugDll = Path.Combine(projectDir, "bin", "Debug", "net10.0", $"{assemblyName}.dll");
    if (File.Exists(debugDll))
    {
        return debugDll;
    }

    string releaseDll = Path.Combine(projectDir, "bin", "Release", "net10.0", $"{assemblyName}.dll");
    if (File.Exists(releaseDll))
    {
        return releaseDll;
    }

    return debugDll;
}

static int GetFreePort()
{
    var listener = new TcpListener(IPAddress.Loopback, 0);
    listener.Start();
    int port = ((IPEndPoint)listener.LocalEndpoint).Port;
    listener.Stop();
    return port;
}
