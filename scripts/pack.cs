#!/usr/bin/env dotnet
#:sdk Microsoft.NET.Sdk
#:property LangVersion=13

#:package CliWrap@3.6.6
#:package Spectre.Console@0.49.1

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CliWrap;
using Spectre.Console;

// Set working directory to solution root if we're in the build directory
if (Directory.GetCurrentDirectory().EndsWith("scripts"))
{
    Directory.SetCurrentDirectory("..");
}

// Configuration
var searchDirectories = new List<string>
{
    "apps",
    "libs" // Default to libs directory
};

var ignoredProjects = new List<string>
{
    // Add project names to ignore here
    // Example: "Six.Example.TestProject"
    "McpDemo"
};

var buildConfig = args.Contains("--debug") ? "Debug" : "Release";
var includeSymbols = !args.Contains("--no-symbols"); // Include symbols by default, opt-out with --no-symbols
var installTools = args.Contains("--install");
var publishMode = GetPublishMode(args);
var versionOverride = GetVersionOverride(args); // --version X.Y.Z overrides $(SixVersion)

// Environment variables
var localFeedUrl = Environment.GetEnvironmentVariable("NUGET_FEED_LOCAL_URL");
var devFeedUrl = Environment.GetEnvironmentVariable("NUGET_FEED_DEV_URL");
var devFeedUsername = Environment.GetEnvironmentVariable("NUGET_FEED_DEV_USERNAME");
var devFeedPat = Environment.GetEnvironmentVariable("NUGET_FEED_DEV_PAT");
var stagingFeedUrl = Environment.GetEnvironmentVariable("NUGET_FEED_STAGING_URL");
var stagingFeedUsername = Environment.GetEnvironmentVariable("NUGET_FEED_STAGING_USERNAME");
var stagingFeedPat = Environment.GetEnvironmentVariable("NUGET_FEED_STAGING_PAT");
var productionFeedUrl = Environment.GetEnvironmentVariable("NUGET_FEED_PRODUCTION_URL");
var productionFeedUsername = Environment.GetEnvironmentVariable("NUGET_FEED_PRODUCTION_USERNAME");
var productionFeedPat = Environment.GetEnvironmentVariable("NUGET_FEED_PRODUCTION_PAT");

AnsiConsole.Write(new FigletText("Six NuGet Packager").Color(Color.Green));
AnsiConsole.WriteLine();

await AnsiConsole.Status()
    .Spinner(Spinner.Known.Dots)
    .StartAsync("Processing NuGet packaging...", async ctx =>
    {
        try
        {
            // Validate environment based on publish mode
            ValidateEnvironment();

            // Discover projects
            ctx.Status("Discovering projects...");
            var projects = DiscoverProjects(searchDirectories, ignoredProjects);
            
            if (projects.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]No projects found to package![/]");
                return;
            }

            AnsiConsole.MarkupLine($"[green]Found {projects.Count} project(s) to package[/]");
            foreach (var project in projects)
            {
                AnsiConsole.MarkupLine($"[dim]  • {Path.GetFileNameWithoutExtension(project)}[/]");
            }
            AnsiConsole.WriteLine();

            // Build and pack projects in parallel
            ctx.Status("Building and packing projects...");
            var outputDir = GetOutputDirectory();
            var packResults = await BuildAndPackProjectsAsync(projects, buildConfig, outputDir, versionOverride);

            // Report packed projects
            ReportPackedProjects(packResults);

            // Publish packages to target feed
            List<PublishResult> publishResults = new();
            if (publishMode == PublishMode.Local)
            {
                ctx.Status("Copying packages to local feed...");
                publishResults = await CopyPackagesToLocalFeedAsync(packResults, localFeedUrl!, outputDir);
                ReportPublishedProjects(publishResults);
            }
            else
            {
                // Get remote feed configuration
                var feedData = GetFeedData();
                
                ctx.Status($"Publishing packages to {publishMode.ToString().ToLower()} feed...");
                publishResults = await PublishPackagesAsync(packResults, publishMode, feedData.Url, feedData.Pat, outputDir);
                ReportPublishedProjects(publishResults);
            }

            // Install tools globally if --install flag was passed
            List<InstallResult> installResults = new();
            if(installTools)
            {
                ctx.Status("Installing tools globally...");
                installResults = await InstallToolsAsync(searchDirectories, outputDir);
                ReportInstalledTools(installResults);
            }

            // Final summary
            ctx.Status("Generating summary...");
            DisplaySummary(packResults, publishResults, installResults, publishMode, outputDir);

            // Always cleanup packages directory (it's temporary)
            if (Directory.Exists(outputDir))
            {
                ctx.Status("Cleaning up temporary packages directory...");
                try
                {
                    Directory.Delete(outputDir, true);
                    AnsiConsole.MarkupLine("[dim]✓ Cleaned up temporary packages directory[/]");
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[yellow]Warning: Could not clean up packages directory: {ex.Message}[/]");
                }
            }
            
            ctx.Status("Completed!");
            await Task.Delay(100); // Brief pause to show completion
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            Environment.Exit(1);
        }
    });

// Clear any remaining status and show completion message
AnsiConsole.WriteLine();
AnsiConsole.Write(new Rule("[green]✅ NuGet Packaging Completed![/]").LeftJustified());

// Helper Methods
string? GetVersionOverride(string[] args)
{
    var idx = Array.IndexOf(args, "--version");
    if (idx < 0 || idx + 1 >= args.Length) return null;
    var value = args[idx + 1];
    return string.IsNullOrWhiteSpace(value) ? null : value;
}

PublishMode GetPublishMode(string[] args)
{
    if (args.Contains("--local")) return PublishMode.Local;
    if (args.Contains("--dev") || args.Contains("--development")) return PublishMode.Development;
    if (args.Contains("--staging")) return PublishMode.Staging;
    if (args.Contains("--prod") || args.Contains("--production")) return PublishMode.Production;
    return PublishMode.Local; // Default to local feed
}

void ValidateEnvironment()
{
    switch (publishMode)
    {
        case PublishMode.Local:
            if (string.IsNullOrEmpty(localFeedUrl))
                throw new InvalidOperationException("NUGET_FEED_LOCAL_URL environment variable is required for local publishing");
            break;
        
        case PublishMode.Development:
            if (string.IsNullOrEmpty(devFeedUrl))
                throw new InvalidOperationException("NUGET_FEED_DEV_URL environment variable is required for DEVELOPMENT feed publishing");
            if (string.IsNullOrEmpty(devFeedUsername))
                throw new InvalidOperationException("NUGET_FEED_DEV_USERNAME environment variable is required for DEVELOPMENT feed publishing");
            if (string.IsNullOrEmpty(devFeedPat))
                throw new InvalidOperationException("NUGET_FEED_DEV_PAT environment variable is required for DEVELOPMENT feed publishing");
            break;
        
        case PublishMode.Staging:
          if (string.IsNullOrEmpty(stagingFeedUrl))
            throw new InvalidOperationException("NUGET_FEED_STAGING_URL environment variable is required for STAGING feed publishing");
          if (string.IsNullOrEmpty(stagingFeedUsername))
            throw new InvalidOperationException("NUGET_FEED_STAGING_USERNAME environment variable is required for STAGING feed publishing");
          if (string.IsNullOrEmpty(stagingFeedPat))
            throw new InvalidOperationException("NUGET_FEED_STAGING_PAT environment variable is required for STAGING feed publishing");
          break;
        
        case PublishMode.Production:
          if (string.IsNullOrEmpty(productionFeedUrl))
            throw new InvalidOperationException("NUGET_FEED_PRODUCTION_URL environment variable is required for PRODUCTION feed publishing");
          if (string.IsNullOrEmpty(productionFeedUsername))
            throw new InvalidOperationException("NUGET_FEED_PRODUCTION_USERNAME environment variable is required for PRODUCTION feed publishing");
          if (string.IsNullOrEmpty(productionFeedPat))
            throw new InvalidOperationException("NUGET_FEED_PRODUCTION_PAT environment variable is required for PRODUCTION feed publishing");
          break;
    }
}

(string Url, string Pat) GetFeedData()
{
  switch (publishMode)
  {
    case PublishMode.Local:
      return (localFeedUrl ?? throw new InvalidOperationException("Local feed URL is null"), string.Empty);
        
    case PublishMode.Development:
      return (devFeedUrl ?? throw new InvalidOperationException("Development feed URL is null"), 
              devFeedPat ?? throw new InvalidOperationException("Development feed PAT is null"));
        
    case PublishMode.Staging:
      return (stagingFeedUrl ?? throw new InvalidOperationException("Staging feed URL is null"), 
              stagingFeedPat ?? throw new InvalidOperationException("Staging feed PAT is null"));
        
    case PublishMode.Production:
      return (productionFeedUrl ?? throw new InvalidOperationException("Production feed URL is null"), 
              productionFeedPat ?? throw new InvalidOperationException("Production feed PAT is null"));
        
    default:
      throw new InvalidOperationException($"Unknown publish mode: {publishMode}");
  }
}


List<string> DiscoverProjects(List<string> searchDirs, List<string> ignored)
{
    var projects = new List<string>();
    
    foreach (var dir in searchDirs)
    {
        if (!Directory.Exists(dir))
        {
            AnsiConsole.MarkupLine($"[yellow]Warning: Directory '{dir}' does not exist, skipping...[/]");
            continue;
        }

        var foundProjects = Directory.GetFiles(dir, "*.csproj", SearchOption.AllDirectories)
            .Where(p => !ignored.Any(ignore => Path.GetFileNameWithoutExtension(p).Equals(ignore, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        projects.AddRange(foundProjects);
    }

    return projects.OrderBy(p => Path.GetFileNameWithoutExtension(p)).ToList();
}

string GetOutputDirectory()
{
    // Always use packages directory for initial packaging
    // Local feed copying happens separately
    return Path.Combine(Directory.GetCurrentDirectory(), "packages");
}

async Task<List<PackResult>> BuildAndPackProjectsAsync(List<string> projects, string config, string outputDir, string? version)
{
    // Ensure output directory exists
    Directory.CreateDirectory(outputDir);

    var results = new List<PackResult>();
    var versionArg = version is not null ? $" -p:Version=\"{version}\"" : "";

    // Build and pack sequentially to avoid file lock issues with dependencies
    foreach (var project in projects)
    {
        var projectName = Path.GetFileNameWithoutExtension(project);

        // Extract PackageId from csproj (falls back to project name)
        var csprojContent = File.ReadAllText(project);
        var packageIdMatch = System.Text.RegularExpressions.Regex.Match(csprojContent, @"<PackageId>(.+?)</PackageId>");
        var packageId = packageIdMatch.Success ? packageIdMatch.Groups[1].Value : projectName;

        try
        {
            // Build with output capture
            var buildOutput = new StringBuilder();
            var buildError = new StringBuilder();

            var buildResult = await Cli.Wrap("dotnet")
                .WithArguments($"build \"{project}\" -c {config}{versionArg} --verbosity normal")
                .WithValidation(CommandResultValidation.None)
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(buildOutput))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(buildError))
                .ExecuteAsync();

            if (buildResult.ExitCode != 0)
            {
                var errorMessage = GetErrorMessage(buildError.ToString(), buildOutput.ToString(), "Build failed");
                results.Add(new PackResult(projectName, packageId, false, errorMessage));
                continue;
            }

            // Pack with or without symbols based on argument
            var packOutput = new StringBuilder();
            var packError = new StringBuilder();

            var packArgs = includeSymbols
                ? $"pack \"{project}\" -c {config} -o \"{outputDir}\" --no-build --include-symbols -p:SymbolPackageFormat=snupkg{versionArg} --verbosity normal"
                : $"pack \"{project}\" -c {config} -o \"{outputDir}\" --no-build{versionArg} --verbosity normal";
            
            var packResult = await Cli.Wrap("dotnet")
                .WithArguments(packArgs)
                .WithValidation(CommandResultValidation.None)
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(packOutput))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(packError))
                .ExecuteAsync();

            if (packResult.ExitCode != 0)
            {
                var errorMessage = GetErrorMessage(packError.ToString(), packOutput.ToString(), "Pack failed");
                results.Add(new PackResult(projectName, packageId, false, errorMessage));
                continue;
            }

            results.Add(new PackResult(projectName, packageId, true, "Success"));
        }
        catch (Exception ex)
        {
            results.Add(new PackResult(projectName, packageId, false, ex.Message));
        }
    }

    return results;
}


Task<List<PublishResult>> CopyPackagesToLocalFeedAsync(List<PackResult> packResults, string localFeedPath, string outputDir)
{
    var successfulPacks = packResults.Where(r => r.Success).ToList();

    // Ensure local feed directory exists
    Directory.CreateDirectory(localFeedPath);

    var results = new List<PublishResult>();

    foreach (var pack in successfulPacks)
    {
        try
        {
            var packageFiles = FindPackageFiles(outputDir, pack.PackageId, ".nupkg");

            if (packageFiles.Length == 0)
            {
                results.Add(new PublishResult(pack.ProjectName, false, "No .nupkg files found"));
                continue;
            }

            var latestNugetPackage = packageFiles[0];
            var packageBaseName = Path.GetFileNameWithoutExtension(latestNugetPackage);

            var snupkgFiles = FindPackageFiles(outputDir, pack.PackageId, ".snupkg")
                .Where(f => Path.GetFileNameWithoutExtension(f) == packageBaseName)
                .ToArray();

            // Copy main package file
            var mainFileName = Path.GetFileName(latestNugetPackage);
            var mainDestPath = Path.Combine(localFeedPath, mainFileName);
            File.Copy(latestNugetPackage, mainDestPath, true);
            
            var copiedFiles = 1;
            
            // Copy symbols package if it exists
            if (snupkgFiles.Length > 0)
            {
                var latestSnupkgFile = snupkgFiles[0];
                var symbolFileName = Path.GetFileName(latestSnupkgFile);
                var symbolDestPath = Path.Combine(localFeedPath, symbolFileName);
                File.Copy(latestSnupkgFile, symbolDestPath, true);
                copiedFiles++;
            }

            var message = copiedFiles == 1 
                ? $"Copied package to {localFeedPath}" 
                : $"Copied package and symbols to {localFeedPath}";
                
            results.Add(new PublishResult(pack.ProjectName, true, message));
        }
        catch (Exception ex)
        {
            results.Add(new PublishResult(pack.ProjectName, false, ex.Message));
        }
    }
    
    return Task.FromResult(results);
}

async Task<List<PublishResult>> PublishPackagesAsync(
  List<PackResult> packResults, 
  PublishMode mode, 
  string feedUrl, 
  string apiKey,
  string outputDir)
{
    var successfulPacks = packResults.Where(r => r.Success).ToList();

    var tasks = successfulPacks.Select(async pack =>
    {
        try
        {
            var packageFiles = FindPackageFiles(outputDir, pack.PackageId, ".nupkg");

            if (packageFiles.Length == 0)
            {
                return new PublishResult(pack.ProjectName, false, "No package file found");
            }

            var latestNugetPackage = packageFiles[0];

            var packageBaseName = Path.GetFileNameWithoutExtension(latestNugetPackage);
            var snupkgFiles = FindPackageFiles(outputDir, pack.PackageId, ".snupkg")
                .Where(f => Path.GetFileNameWithoutExtension(f) == packageBaseName)
                .ToArray();

            // Push main package
            var pushOutput = new StringBuilder();
            var pushError = new StringBuilder();
            
            var pushResult = await Cli.Wrap("dotnet")
                .WithArguments($"nuget push \"{latestNugetPackage}\" --skip-duplicate --api-key \"{apiKey}\" --source \"{feedUrl}\"")
                .WithValidation(CommandResultValidation.None)
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(pushOutput))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(pushError))
                .ExecuteAsync();

            if (pushResult.ExitCode != 0)
            {
                var errorMessage = GetErrorMessage(pushError.ToString(), pushOutput.ToString(), "Push failed");
                return new PublishResult(pack.ProjectName, false, errorMessage);
            }

            // Push symbols if exists (matching the PowerShell script logic)
            if (snupkgFiles.Length > 0)
            {
                var latestSnupkgFile = snupkgFiles[0];
                var symbolPushOutput = new StringBuilder();
                var symbolPushError = new StringBuilder();
                
                var symbolPushResult = await Cli.Wrap("dotnet")
                    .WithArguments($"nuget push \"{latestSnupkgFile}\" --skip-duplicate --api-key \"{apiKey}\" --source \"{feedUrl}\"")
                    .WithValidation(CommandResultValidation.None)
                    .WithStandardOutputPipe(PipeTarget.ToStringBuilder(symbolPushOutput))
                    .WithStandardErrorPipe(PipeTarget.ToStringBuilder(symbolPushError))
                    .ExecuteAsync();

                // Symbol push failure is not critical
                if (symbolPushResult.ExitCode != 0)
                {
                    return new PublishResult(pack.ProjectName, true, "Package published (symbols failed)");
                }
                
                return new PublishResult(pack.ProjectName, true, "Published with symbols");
            }

            return new PublishResult(pack.ProjectName, true, "Published successfully");
        }
        catch (Exception ex)
        {
            return new PublishResult(pack.ProjectName, false, ex.Message);
        }
    });

    return (await Task.WhenAll(tasks)).ToList();
}

void ReportPackedProjects(List<PackResult> results)
{
    AnsiConsole.WriteLine();
    AnsiConsole.Write(new Rule("[green]📦 Packaging Results[/]").LeftJustified());
    
    var table = new Table()
        .Border(TableBorder.Rounded)
        .AddColumn("Project")
        .AddColumn("Status")
        .AddColumn(new TableColumn("Details").Width(60));

    foreach (var result in results.OrderBy(r => r.ProjectName))
    {
        var status = result.Success ? "[green]✓ Success[/]" : "[red]✗ Failed[/]";
        var details = result.Success ? "[dim]Packaged successfully[/]" : $"[red]{TruncateMessage(result.Message, 55).EscapeMarkup()}[/]";
        
        table.AddRow(result.ProjectName, status, details);
        
        // If there's a failure with a long message, show it separately
        if (!result.Success && result.Message.Length > 55)
        {
            AnsiConsole.MarkupLine($"[red]  {result.ProjectName} Error Details:[/] {result.Message.EscapeMarkup()}");
        }
    }

    AnsiConsole.Write(table);
    
    var successful = results.Count(r => r.Success);
    var failed = results.Count(r => !r.Success);
    
    AnsiConsole.MarkupLine($"\n[green]✓ {successful} project(s) packaged successfully[/]");
    if (failed > 0)
    {
        AnsiConsole.MarkupLine($"[red]✗ {failed} project(s) failed to package[/]");
    }
}

void ReportPublishedProjects(List<PublishResult> results)
{
    if (results.Count == 0) return;

    AnsiConsole.WriteLine();
    AnsiConsole.Write(new Rule("[blue]🚀 Publishing Results[/]").LeftJustified());
    
    var table = new Table()
        .Border(TableBorder.Rounded)
        .AddColumn("Project")
        .AddColumn("Status")
        .AddColumn(new TableColumn("Details").Width(60));

    foreach (var result in results.OrderBy(r => r.ProjectName))
    {
        var status = result.Success ? "[green]✓ Published[/]" : "[red]✗ Failed[/]";
        var details = result.Success ? "[dim]Published successfully[/]" : $"[red]{TruncateMessage(result.Message, 55).EscapeMarkup()}[/]";
        
        table.AddRow(result.ProjectName, status, details);
        
        // If there's a failure with a long message, show it separately
        if (!result.Success && result.Message.Length > 55)
        {
            AnsiConsole.MarkupLine($"[red]  {result.ProjectName} Publish Error Details:[/] {result.Message.EscapeMarkup()}");
        }
    }

    AnsiConsole.Write(table);
    
    var successful = results.Count(r => r.Success);
    var failed = results.Count(r => !r.Success);
    
    AnsiConsole.MarkupLine($"\n[green]✓ {successful} project(s) published successfully[/]");
    if (failed > 0)
    {
        AnsiConsole.MarkupLine($"[red]✗ {failed} project(s) failed to publish[/]");
    }
}

void DisplaySummary(List<PackResult> packResults, List<PublishResult> publishResults, List<InstallResult> installResults, PublishMode mode, string outputDir)
{
    AnsiConsole.WriteLine();
    AnsiConsole.Write(new Rule("[yellow]📋 Summary[/]").LeftJustified());

    var publishedTo = mode switch
    {
        PublishMode.Local => "LOCAL FEED",
        PublishMode.Development => "DEVELOPMENT FEED",
        PublishMode.Staging => "STAGING FEED",
        PublishMode.Production => "PRODUCTION FEED",
        _ => "Unknown"
    };

    var versionDisplay = versionOverride ?? "$(SixVersion) from Directory.Build.props";
    var panel = new Panel(
        $"""
        [green]Build Configuration:[/] {buildConfig}
        [green]Version:[/] {versionDisplay}
        [green]Publish Mode:[/] {mode}
        [green]Output Directory:[/] {outputDir}
        [green]Packed Projects:[/] {packResults.Count(r => r.Success)}/{packResults.Count}
        {(publishResults.Count > 0 ? $"[green]Published Projects:[/] {publishResults.Count(r => r.Success)}/{publishResults.Count}" : "")}
        {(publishResults.Count > 0 && !string.IsNullOrEmpty(publishedTo) ? $"[green]Published To:[/] {publishedTo}" : "")}
        {(installResults.Count > 0 ? $"[green]Installed Tools:[/] {installResults.Count(r => r.Success)}/{installResults.Count}" : "")}
        """)
        .Border(BoxBorder.Rounded)
        .BorderColor(Color.Green)
        .Header("[green]NuGet Packaging Summary[/]");

    AnsiConsole.Write(panel);

    AnsiConsole.WriteLine();
    AnsiConsole.Write(new Text("Usage: dotnet run scripts/pack.cs [--debug] [--no-symbols] [--install] [--version X.Y.Z] [--local|--dev/development|--staging|--prod/production] (default: --local, version from SixVersion, symbols included)", new Style(Color.Grey)));
    AnsiConsole.WriteLine();
}

string GetErrorMessage(string stderr, string stdout, string defaultMessage)
{
    // Combine stderr and stdout
    var allOutput = $"{stderr}\n{stdout}".Trim();
    
    if (string.IsNullOrWhiteSpace(allOutput))
        return defaultMessage;
    
    // Look for error lines
    var lines = allOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);
    var errorLines = lines.Where(line => 
        line.Contains("error", StringComparison.OrdinalIgnoreCase) ||
        line.Contains("failed", StringComparison.OrdinalIgnoreCase) ||
        line.Contains("exception", StringComparison.OrdinalIgnoreCase))
        .Take(3) // Limit to first 3 error lines
        .ToArray();
    
    if (errorLines.Length > 0)
    {
        // Clean up the error messages
        var cleanErrors = errorLines.Select(line => 
            line.Trim()
                .Replace("error:", "", StringComparison.OrdinalIgnoreCase)
                .Replace("Error:", "", StringComparison.OrdinalIgnoreCase)
                .Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();
        
        if (cleanErrors.Length > 0)
        {
            return string.Join(" | ", cleanErrors);
        }
    }
    
    // If no specific errors found, return first few lines of output
    var firstLines = lines.Take(2).Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
    return firstLines.Length > 0 ? string.Join(" | ", firstLines) : defaultMessage;
}

string TruncateMessage(string message, int maxLength)
{
    if (string.IsNullOrEmpty(message) || message.Length <= maxLength)
        return message;
    
    return message.Substring(0, maxLength - 3) + "...";
}



async Task<List<InstallResult>> InstallToolsAsync(List<string> searchDirs, string outputDir)
{
    var results = new List<InstallResult>();

    foreach(var dir in searchDirs)
    {
        if(!Directory.Exists(dir)) continue;

        var csprojFiles = Directory.GetFiles(dir, "*.csproj", SearchOption.AllDirectories);
        foreach(var csproj in csprojFiles)
        {
            var content = File.ReadAllText(csproj);
            if(!content.Contains("<PackAsTool>true</PackAsTool>")) continue;

            // Extract ToolCommandName for display
            var toolNameMatch = System.Text.RegularExpressions.Regex.Match(content, @"<ToolCommandName>(.+?)</ToolCommandName>");
            if(!toolNameMatch.Success) continue;

            var toolName = toolNameMatch.Groups[1].Value;
            var projectName = Path.GetFileNameWithoutExtension(csproj);

            // Extract PackageId (dotnet tool install uses PackageId, not ToolCommandName)
            var packageIdMatch = System.Text.RegularExpressions.Regex.Match(content, @"<PackageId>(.+?)</PackageId>");
            var packageId = packageIdMatch.Success ? packageIdMatch.Groups[1].Value : projectName;

            try
            {
                // Try update first (works if already installed)
                var output = new StringBuilder();
                var error = new StringBuilder();

                var updateResult = await Cli.Wrap("dotnet")
                    .WithArguments($"tool update --global --add-source \"{outputDir}\" {packageId}")
                    .WithValidation(CommandResultValidation.None)
                    .WithStandardOutputPipe(PipeTarget.ToStringBuilder(output))
                    .WithStandardErrorPipe(PipeTarget.ToStringBuilder(error))
                    .ExecuteAsync();

                if(updateResult.ExitCode == 0)
                {
                    results.Add(new InstallResult(toolName, projectName, true, "Updated"));
                    continue;
                }

                // Fall back to install
                output.Clear();
                error.Clear();

                var installResult = await Cli.Wrap("dotnet")
                    .WithArguments($"tool install --global --add-source \"{outputDir}\" {packageId}")
                    .WithValidation(CommandResultValidation.None)
                    .WithStandardOutputPipe(PipeTarget.ToStringBuilder(output))
                    .WithStandardErrorPipe(PipeTarget.ToStringBuilder(error))
                    .ExecuteAsync();

                if(installResult.ExitCode == 0)
                {
                    results.Add(new InstallResult(toolName, projectName, true, "Installed"));
                }
                else
                {
                    var errorMessage = GetErrorMessage(error.ToString(), output.ToString(), "Install failed");
                    results.Add(new InstallResult(toolName, projectName, false, errorMessage));
                }
            }
            catch(Exception ex)
            {
                results.Add(new InstallResult(toolName, projectName, false, ex.Message));
            }
        }
    }

    return results;
}

void ReportInstalledTools(List<InstallResult> results)
{
    if(results.Count == 0) return;

    AnsiConsole.WriteLine();
    AnsiConsole.Write(new Rule("[cyan]🔧 Tool Installation Results[/]").LeftJustified());

    var table = new Table()
        .Border(TableBorder.Rounded)
        .AddColumn("Tool")
        .AddColumn("Project")
        .AddColumn("Status")
        .AddColumn(new TableColumn("Details").Width(50));

    foreach(var result in results.OrderBy(r => r.ToolName))
    {
        var status = result.Success ? "[green]✓ OK[/]" : "[red]✗ Failed[/]";
        var details = result.Success
            ? $"[dim]{result.Message}[/]"
            : $"[red]{TruncateMessage(result.Message, 45).EscapeMarkup()}[/]";

        table.AddRow(result.ToolName, result.ProjectName, status, details);
    }

    AnsiConsole.Write(table);

    var successful = results.Count(r => r.Success);
    var failed = results.Count(r => !r.Success);

    AnsiConsole.MarkupLine($"\n[green]✓ {successful} tool(s) installed successfully[/]");
    if(failed > 0)
    {
        AnsiConsole.MarkupLine($"[red]✗ {failed} tool(s) failed to install[/]");
    }
}

// Match nupkg/snupkg files by PackageId precisely.
// Filenames follow {PackageId}.{Version}{ext} — the char after PackageId must be a dot followed by a digit
// to avoid "Wig" matching "Wig.Cli" or "Wig.Core".
string[] FindPackageFiles(string directory, string packageId, string extension)
{
    return Directory.GetFiles(directory, $"{packageId}.*{extension}")
        .Where(f =>
        {
            var fileName = Path.GetFileName(f);
            // After "{PackageId}." the next char must be a digit (start of version)
            return fileName.Length > packageId.Length + 1
                && fileName[packageId.Length] == '.'
                && char.IsDigit(fileName[packageId.Length + 1]);
        })
        .OrderByDescending(f => File.GetLastWriteTime(f))
        .ToArray();
}

// Data Models
public record PackResult(string ProjectName, string PackageId, bool Success, string Message);
public record PublishResult(string ProjectName, bool Success, string Message);
public record InstallResult(string ToolName, string ProjectName, bool Success, string Message);

public enum PublishMode
{
    Local,
    Development,
    Staging,
    Production
}
