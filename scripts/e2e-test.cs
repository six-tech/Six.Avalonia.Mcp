#!/usr/bin/env dotnet
#:sdk Microsoft.NET.Sdk
#:property LangVersion=13

#:package CliWrap@3.6.6
#:package Spectre.Console@0.49.1

using System;
using System.Diagnostics;
using System.IO;
using CliWrap;
using CliWrap.Buffered;
using Spectre.Console;

// Set working directory to solution root.
if (Directory.GetCurrentDirectory().EndsWith("scripts"))
{
    Directory.SetCurrentDirectory("..");
}

var solutionRoot = Directory.GetCurrentDirectory();

AnsiConsole.MarkupLine("[cyan bold]Six.Avalonia.Mcp — E2E Test[/]");
AnsiConsole.MarkupLine("[grey]Builds solution, then runs the test runner[/]");
AnsiConsole.WriteLine();

// ── 1. Build everything ────────────────────────────────────────────────

var buildResult = await AnsiConsole.Status()
    .Spinner(Spinner.Known.Dots)
    .StartAsync("[yellow]Building solution...[/]", async _ =>
    {
        return await Cli.Wrap("dotnet")
            .WithArguments(["build", "--nologo", "-v", "q"])
            .WithWorkingDirectory(solutionRoot)
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync();
    });

if (buildResult.ExitCode != 0)
{
    AnsiConsole.MarkupLine("[red bold]Build failed:[/]");
    AnsiConsole.WriteLine(buildResult.StandardError);
    return 1;
}

AnsiConsole.MarkupLine("[green]Build succeeded.[/]");
AnsiConsole.WriteLine();

// ── 2. Resolve test runner DLL ─────────────────────────────────────────

var testRunnerDll = ResolveDll(solutionRoot, "apps/Six.Avalonia.Mcp.TestRunner", "Six.Avalonia.Mcp.TestRunner");
if (!File.Exists(testRunnerDll))
{
    AnsiConsole.MarkupLine($"[red]Test runner DLL not found: {testRunnerDll}[/]");
    return 1;
}

// ── 3. Run the test runner ─────────────────────────────────────────────
// The test runner starts the MCP server (STDIO) and demo app internally.
// We run it with inherited stdin/stdout/stderr so output goes straight
// to the terminal — no piping, no buffering issues.

AnsiConsole.MarkupLine("[yellow]Starting test runner...[/]");
AnsiConsole.WriteLine();

var process = new Process();
process.StartInfo = new ProcessStartInfo
{
    FileName = "dotnet",
    Arguments = $"exec \"{testRunnerDll}\"",
    WorkingDirectory = solutionRoot,
    UseShellExecute = false,
    // Do NOT redirect streams — inherit the terminal directly.
};
process.Start();
await process.WaitForExitAsync();

return process.ExitCode;

// ── Helpers ────────────────────────────────────────────────────────────

static string ResolveDll(string solutionRoot, string projectRelPath, string assemblyName)
{
    var projectDir = Path.Combine(solutionRoot, projectRelPath.Replace('/', Path.DirectorySeparatorChar));
    var debugDll = Path.Combine(projectDir, "bin", "Debug", "net10.0", $"{assemblyName}.dll");
    if (File.Exists(debugDll))
    {
        return debugDll;
    }

    var releaseDll = Path.Combine(projectDir, "bin", "Release", "net10.0", $"{assemblyName}.dll");
    if (File.Exists(releaseDll))
    {
        return releaseDll;
    }

    return debugDll;
}
