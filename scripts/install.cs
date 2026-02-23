#!/usr/bin/env dotnet
#:sdk Microsoft.NET.Sdk
#:property LangVersion=13

#:package CliWrap@3.6.6
#:package Spectre.Console@0.49.1

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CliWrap;
using Spectre.Console;

// Set working directory to solution root if we're in the scripts directory
if(Directory.GetCurrentDirectory().EndsWith("scripts"))
{
    Directory.SetCurrentDirectory("..");
}

var tools = new (string PackageId, string Command)[]
{
    ("McpDemo.Server", "six-avalonia-mcp"),
};

var packDir = Path.Combine(Directory.GetCurrentDirectory(), "packages");

// Pack first so we have fresh nupkg files
AnsiConsole.MarkupLine("[dim]Packing projects...[/]");
var packOutput = new StringBuilder();
var packError = new StringBuilder();
var packResult = await Cli.Wrap("dotnet")
    .WithArguments($"pack McpDemo.slnx -c Release -o \"{packDir}\"")
    .WithValidation(CommandResultValidation.None)
    .WithStandardOutputPipe(PipeTarget.ToStringBuilder(packOutput))
    .WithStandardErrorPipe(PipeTarget.ToStringBuilder(packError))
    .ExecuteAsync();

if(packResult.ExitCode != 0)
{
    AnsiConsole.MarkupLine("[red]Pack failed:[/]");
    AnsiConsole.WriteLine(packError.ToString());
    Environment.Exit(1);
}

AnsiConsole.MarkupLine("[green]Pack succeeded[/]");
AnsiConsole.WriteLine();

// Install each tool
var table = new Table()
    .Border(TableBorder.Rounded)
    .AddColumn("Tool")
    .AddColumn("Package")
    .AddColumn("Status");

foreach(var (packageId, command) in tools)
{
    var output = new StringBuilder();
    var error = new StringBuilder();

    // Try update first (works if already installed)
    var result = await Cli.Wrap("dotnet")
        .WithArguments($"tool update --global --add-source \"{packDir}\" {packageId}")
        .WithValidation(CommandResultValidation.None)
        .WithStandardOutputPipe(PipeTarget.ToStringBuilder(output))
        .WithStandardErrorPipe(PipeTarget.ToStringBuilder(error))
        .ExecuteAsync();

    if(result.ExitCode != 0)
    {
        // Fall back to install
        output.Clear();
        error.Clear();

        result = await Cli.Wrap("dotnet")
            .WithArguments($"tool install --global --add-source \"{packDir}\" {packageId}")
            .WithValidation(CommandResultValidation.None)
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(output))
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(error))
            .ExecuteAsync();
    }

    if(result.ExitCode == 0)
    {
        table.AddRow(command, packageId, "[green]OK[/]");
    }
    else
    {
        var msg = error.ToString().Trim().Split('\n')[0];
        table.AddRow(command, packageId, $"[red]{msg.EscapeMarkup()}[/]");
    }
}

AnsiConsole.Write(table);

// Cleanup
if(Directory.Exists(packDir))
{
    Directory.Delete(packDir, true);
}
