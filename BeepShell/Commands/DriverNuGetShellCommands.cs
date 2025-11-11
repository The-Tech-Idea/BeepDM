using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using BeepShell.Infrastructure;
using Spectre.Console;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;

namespace BeepShell.Commands
{
    /// <summary>
    /// Shell commands for managing driver-specific NuGet packages
    /// </summary>
    public class DriverNuGetShellCommands : IShellCommand
    {
        private IDMEEditor _editor = null!;

        public string CommandName => "driver-nuget";
        public string Description => "Manage driver-specific NuGet packages by DataSourceType";
        public string Category => "Driver Management";
        public string Version => "1.0.0";
        public string Author => "BeepDM Team";
        public string[] Aliases => new[] { "drv-nuget", "driver-pkg", "drv-pkg" };

        public void Initialize(IDMEEditor editor)
        {
            _editor = editor;
        }

        public Command BuildCommand()
        {
            var cmd = new Command("driver-nuget", "Manage driver NuGet packages");

            // driver-nuget list - List drivers and their NuGet status
            var listCmd = new Command("list", "List drivers with their NuGet package status");
            var categoryOpt = new Option<string>(new[] { "--category", "-c" }, "Filter by datasource category");
            var missingOpt = new Option<bool>(new[] { "--missing-only", "-m" }, "Show only drivers with missing NuGet packages");
            listCmd.AddOption(categoryOpt);
            listCmd.AddOption(missingOpt);
            listCmd.SetHandler((string category, bool missingOnly) => ListDriverNugets(category, missingOnly), categoryOpt, missingOpt);
            cmd.AddCommand(listCmd);

            // driver-nuget install <datasourcetype> - Install NuGet for a specific driver
            var installCmd = new Command("install", "Install NuGet package for a driver");
            var typeArg = new Argument<string>("datasourcetype", "DataSourceType enum value (e.g., SqlServer, MongoDB)");
            var sourceOpt = new Option<string>(new[] { "--source", "-s" }, "Custom NuGet source URL or path");
            var versionOpt = new Option<string>(new[] { "--version", "-v" }, "Specific version to install");
            installCmd.AddArgument(typeArg);
            installCmd.AddOption(sourceOpt);
            installCmd.AddOption(versionOpt);
            installCmd.SetHandler((string dataSourceType, string source, string version) => InstallDriverNuget(dataSourceType, source, version), typeArg, sourceOpt, versionOpt);
            cmd.AddCommand(installCmd);

            // driver-nuget update <datasourcetype> - Update NuGet for a driver
            var updateCmd = new Command("update", "Update NuGet package for a driver");
            var updateTypeArg = new Argument<string>("datasourcetype", "DataSourceType enum value");
            var updateSourceOpt = new Option<string>(new[] { "--source", "-s" }, "Custom NuGet source");
            updateCmd.AddArgument(updateTypeArg);
            updateCmd.AddOption(updateSourceOpt);
            updateCmd.SetHandler((string dataSourceType, string source) => UpdateDriverNuget(dataSourceType, source), updateTypeArg, updateSourceOpt);
            cmd.AddCommand(updateCmd);

            // driver-nuget remove <datasourcetype> - Remove/unload NuGet for a driver
            var removeCmd = new Command("remove", "Remove NuGet package for a driver");
            var removeTypeArg = new Argument<string>("datasourcetype", "DataSourceType enum value");
            removeCmd.AddArgument(removeTypeArg);
            removeCmd.SetHandler((string dataSourceType) => RemoveDriverNuget(dataSourceType), removeTypeArg);
            cmd.AddCommand(removeCmd);

            // driver-nuget info <datasourcetype> - Show detailed NuGet info for a driver
            var infoCmd = new Command("info", "Show NuGet package information for a driver");
            var infoTypeArg = new Argument<string>("datasourcetype", "DataSourceType enum value");
            infoCmd.AddArgument(infoTypeArg);
            infoCmd.SetHandler((string dataSourceType) => ShowDriverNugetInfo(dataSourceType), infoTypeArg);
            cmd.AddCommand(infoCmd);

            // driver-nuget sources - List/manage NuGet sources for drivers
            var sourcesCmd = new Command("sources", "List NuGet sources configured for drivers");
            sourcesCmd.SetHandler(() => ListDriverNugetSources());
            cmd.AddCommand(sourcesCmd);

            // driver-nuget check - Check all drivers for missing NuGets
            var checkCmd = new Command("check", "Check all drivers for missing NuGet packages");
            var autoFixOpt = new Option<bool>(new[] { "--auto-fix", "-f" }, "Automatically attempt to install missing packages");
            checkCmd.AddOption(autoFixOpt);
            checkCmd.SetHandler((bool autoFix) => CheckMissingNugets(autoFix), autoFixOpt);
            cmd.AddCommand(checkCmd);

            // driver-nuget set-source <datasourcetype> <source> - Set custom NuGet source for driver
            var setSourceCmd = new Command("set-source", "Set custom NuGet source for a driver");
            var setSourceTypeArg = new Argument<string>("datasourcetype", "DataSourceType enum value");
            var setSourcePathArg = new Argument<string>("source", "NuGet source URL or path");
            setSourceCmd.AddArgument(setSourceTypeArg);
            setSourceCmd.AddArgument(setSourcePathArg);
            setSourceCmd.SetHandler((string dataSourceType, string source) => SetDriverNugetSource(dataSourceType, source), setSourceTypeArg, setSourcePathArg);
            cmd.AddCommand(setSourceCmd);

            // driver-nuget install-missing - Interactive selection and installation of missing drivers
            var installMissingCmd = new Command("install-missing", "Interactively select and install missing driver packages");
            var installMissingSourceOpt = new Option<string>(new[] { "--source", "-s" }, "Default source directory for packages");
            installMissingCmd.AddOption(installMissingSourceOpt);
            installMissingCmd.SetHandler((string source) => InstallMissingInteractive(source), installMissingSourceOpt);
            cmd.AddCommand(installMissingCmd);

            return cmd;
        }

        private void ListDriverNugets(string category, bool missingOnly)
        {
            try
            {
                var drivers = _editor.ConfigEditor.DataDriversClasses.ToList();

                if (!string.IsNullOrWhiteSpace(category))
                {
                    drivers = drivers.Where(d => 
                        d.DatasourceCategory.ToString().Equals(category, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                if (missingOnly)
                {
                    drivers = drivers.Where(d => d.NuggetMissing).ToList();
                }

                if (!drivers.Any())
                {
                    AnsiConsole.MarkupLine("[yellow]No drivers found matching criteria[/]");
                    return;
                }

                var table = new Table();
                table.Border = TableBorder.Rounded;
                table.Title = new TableTitle($"[bold cyan]Driver NuGet Packages ({drivers.Count})[/]");
                table.AddColumn("[cyan]#[/]");
                table.AddColumn("[cyan]DataSource Type[/]");
                table.AddColumn("[cyan]Driver Class[/]");
                table.AddColumn("[cyan]Package Name[/]");
                table.AddColumn("[cyan]Version[/]");
                table.AddColumn("[cyan]Source[/]");
                table.AddColumn("[cyan]Status[/]");

                var index = 1;
                foreach (var driver in drivers.OrderBy(d => d.DatasourceType.ToString()))
                {
                    var status = driver.NuggetMissing ? "[red]Missing[/]" : "[green]Installed[/]";
                    var source = string.IsNullOrWhiteSpace(driver.NuggetSource) 
                        ? "[dim]Default[/]" 
                        : (driver.NuggetSource.Length > 30 ? "..." + driver.NuggetSource.Substring(driver.NuggetSource.Length - 30) : driver.NuggetSource);

                    table.AddRow(
                        index.ToString(),
                        driver.DatasourceType.ToString(),
                        driver.DriverClass ?? "[dim]N/A[/]",
                        driver.PackageName ?? "[dim]N/A[/]",
                        driver.NuggetVersion ?? "[dim]N/A[/]",
                        source,
                        status
                    );
                    index++;
                }

                AnsiConsole.Write(table);

                // Summary
                var missing = drivers.Count(d => d.NuggetMissing);
                if (missing > 0)
                {
                    AnsiConsole.MarkupLine($"\n[yellow]⚠[/] {missing} driver(s) have missing NuGet packages");
                    AnsiConsole.MarkupLine("[dim]Use 'driver-nuget install-missing' for interactive installation[/]");
                    AnsiConsole.MarkupLine("[dim]Or use 'driver-nuget check --auto-fix' to attempt automatic installation[/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error listing driver NuGets: {ex.Message}");
            }
        }

        private void InstallDriverNuget(string dataSourceType, string source, string version)
        {
            try
            {
                var driver = FindDriver(dataSourceType);
                if (driver == null) return;

                var packageSource = source ?? driver.NuggetSource;
                var packageVersion = version ?? driver.NuggetVersion;

                if (string.IsNullOrWhiteSpace(packageSource))
                {
                    AnsiConsole.MarkupLine($"[yellow]⚠[/] No NuGet source configured for {dataSourceType}");
                    AnsiConsole.MarkupLine("[dim]Use 'driver-nuget set-source <type> <source>' to configure[/]");
                    return;
                }

                AnsiConsole.Status()
                    .Start($"Installing NuGet package for {dataSourceType}...", ctx =>
                    {
                        ctx.Status($"Resolving package from {packageSource}...");

                        // Check if it's a local path or URL
                        if (File.Exists(packageSource))
                        {
                            // Load from local file
                            ctx.Status($"Loading from local path...");
                            var success = _editor.assemblyHandler.LoadNugget(packageSource);

                            if (success)
                            {
                                driver.NuggetMissing = false;
                                if (!string.IsNullOrWhiteSpace(version))
                                {
                                    driver.NuggetVersion = version;
                                }
                                _editor.ConfigEditor.SaveConfigValues();

                                AnsiConsole.MarkupLine($"[green]✓[/] NuGet package installed for {dataSourceType}");
                                AnsiConsole.MarkupLine($"[dim]  Package: {driver.PackageName}[/]");
                                AnsiConsole.MarkupLine($"[dim]  Version: {driver.NuggetVersion}[/]");
                            }
                            else
                            {
                                AnsiConsole.MarkupLine($"[red]✗[/] Failed to load NuGet package");
                            }
                        }
                        else if (Directory.Exists(packageSource))
                        {
                            // Search in directory
                            ctx.Status($"Searching in directory...");
                            var packageName = driver.PackageName ?? driver.DriverClass;
                            var dllFiles = Directory.GetFiles(packageSource, "*.dll", SearchOption.AllDirectories);
                            var matchingDll = dllFiles.FirstOrDefault(f => 
                                Path.GetFileNameWithoutExtension(f).Equals(packageName, StringComparison.OrdinalIgnoreCase));

                            if (matchingDll != null)
                            {
                                ctx.Status($"Loading {Path.GetFileName(matchingDll)}...");
                                var success = _editor.assemblyHandler.LoadNugget(matchingDll);

                                if (success)
                                {
                                    driver.NuggetMissing = false;
                                    driver.NuggetSource = packageSource;
                                    _editor.ConfigEditor.SaveConfigValues();

                                    AnsiConsole.MarkupLine($"[green]✓[/] NuGet package installed for {dataSourceType}");
                                    AnsiConsole.MarkupLine($"[dim]  File: {Path.GetFileName(matchingDll)}[/]");
                                }
                                else
                                {
                                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to load NuGet package");
                                }
                            }
                            else
                            {
                                AnsiConsole.MarkupLine($"[yellow]⚠[/] Package not found in directory: {packageName}");
                            }
                        }
                        else
                        {
                            // Assume it's a URL or NuGet repository
                            AnsiConsole.MarkupLine($"[yellow]⚠[/] Remote NuGet installation not yet implemented");
                            AnsiConsole.MarkupLine($"[dim]Please download the package manually and use a local path[/]");
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error installing driver NuGet: {ex.Message}");
            }
        }

        private void UpdateDriverNuget(string dataSourceType, string source)
        {
            try
            {
                var driver = FindDriver(dataSourceType);
                if (driver == null) return;

                if (driver.NuggetMissing)
                {
                    AnsiConsole.MarkupLine($"[yellow]⚠[/] NuGet package not currently installed for {dataSourceType}");
                    AnsiConsole.MarkupLine("[dim]Use 'driver-nuget install' instead[/]");
                    return;
                }

                var confirm = AnsiConsole.Confirm($"Update NuGet package for {dataSourceType}?");
                if (!confirm)
                {
                    AnsiConsole.MarkupLine("[yellow]Cancelled[/]");
                    return;
                }

                // Unload first, then install
                var packageName = driver.PackageName ?? driver.DriverClass;
                if (!string.IsNullOrWhiteSpace(packageName))
                {
                    _editor.assemblyHandler.UnloadNugget(packageName);
                }

                // Install updated version
                InstallDriverNuget(dataSourceType, source, string.Empty);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error updating driver NuGet: {ex.Message}");
            }
        }

        private void RemoveDriverNuget(string dataSourceType)
        {
            try
            {
                var driver = FindDriver(dataSourceType);
                if (driver == null) return;

                if (driver.NuggetMissing)
                {
                    AnsiConsole.MarkupLine($"[yellow]⚠[/] NuGet package not installed for {dataSourceType}[/]");
                    return;
                }

                var confirm = AnsiConsole.Confirm($"Remove NuGet package for {dataSourceType}?");
                if (!confirm)
                {
                    AnsiConsole.MarkupLine("[yellow]Cancelled[/]");
                    return;
                }

                var packageName = driver.PackageName ?? driver.DriverClass;
                if (string.IsNullOrWhiteSpace(packageName))
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Cannot determine package name");
                    return;
                }

                var success = _editor.assemblyHandler.UnloadNugget(packageName);

                if (success)
                {
                    driver.NuggetMissing = true;
                    _editor.ConfigEditor.SaveConfigValues();

                    AnsiConsole.MarkupLine($"[green]✓[/] NuGet package removed for {dataSourceType}");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[yellow]⚠[/] Package not found or cannot be unloaded");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error removing driver NuGet: {ex.Message}");
            }
        }

        private void ShowDriverNugetInfo(string dataSourceType)
        {
            try
            {
                var driver = FindDriver(dataSourceType);
                if (driver == null) return;

                var panel = new Panel(new Markup(
                    $"[cyan]DataSource Type:[/] {driver.DatasourceType}\n" +
                    $"[cyan]Category:[/] {driver.DatasourceCategory}\n" +
                    $"[cyan]Driver Class:[/] {driver.DriverClass ?? "N/A"}\n" +
                    $"[cyan]Package Name:[/] {driver.PackageName ?? "N/A"}\n" +
                    $"[cyan]Version:[/] {driver.NuggetVersion ?? "N/A"}\n" +
                    $"[cyan]Source:[/] {driver.NuggetSource ?? "Default"}\n" +
                    $"[cyan]Status:[/] {(driver.NuggetMissing ? "[red]Missing[/]" : "[green]Installed[/]")}"
                ));
                panel.Header = new PanelHeader($"[bold]Driver NuGet Info: {dataSourceType}[/]");
                panel.Border = BoxBorder.Rounded;

                AnsiConsole.Write(panel);

                // Check if assembly is loaded
                if (!driver.NuggetMissing && !string.IsNullOrWhiteSpace(driver.PackageName))
                {
                    var assembly = _editor.assemblyHandler.LoadedAssemblies
                        .FirstOrDefault(a => a.GetName().Name?.Equals(driver.PackageName, StringComparison.OrdinalIgnoreCase) == true);

                    if (assembly != null)
                    {
                        AnsiConsole.WriteLine();
                        AnsiConsole.MarkupLine($"[green]✓[/] Assembly is currently loaded");
                        AnsiConsole.MarkupLine($"[dim]  Location: {assembly.Location}[/]");
                        AnsiConsole.MarkupLine($"[dim]  Full Name: {assembly.FullName}[/]");
                    }
                    else
                    {
                        AnsiConsole.WriteLine();
                        AnsiConsole.MarkupLine($"[yellow]⚠[/] Assembly is not currently loaded");
                    }
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error showing driver NuGet info: {ex.Message}");
            }
        }

        private void ListDriverNugetSources()
        {
            try
            {
                var drivers = _editor.ConfigEditor.DataDriversClasses
                    .Where(d => !string.IsNullOrWhiteSpace(d.NuggetSource))
                    .ToList();

                if (!drivers.Any())
                {
                    AnsiConsole.MarkupLine("[yellow]No custom NuGet sources configured[/]");
                    return;
                }

                var table = new Table();
                table.Border = TableBorder.Rounded;
                table.Title = new TableTitle($"[bold cyan]Driver NuGet Sources ({drivers.Count})[/]");
                table.AddColumn("[cyan]DataSource Type[/]");
                table.AddColumn("[cyan]NuGet Source[/]");
                table.AddColumn("[cyan]Type[/]");

                foreach (var driver in drivers.OrderBy(d => d.DatasourceType.ToString()))
                {
                    var sourceType = File.Exists(driver.NuggetSource) ? "File" :
                                    Directory.Exists(driver.NuggetSource) ? "Directory" :
                                    driver.NuggetSource.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? "URL" : "Unknown";

                    table.AddRow(
                        driver.DatasourceType.ToString(),
                        driver.NuggetSource,
                        sourceType
                    );
                }

                AnsiConsole.Write(table);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error listing NuGet sources: {ex.Message}");
            }
        }

        private void CheckMissingNugets(bool autoFix)
        {
            try
            {
                var drivers = _editor.ConfigEditor.DataDriversClasses
                    .Where(d => d.NuggetMissing)
                    .ToList();

                if (!drivers.Any())
                {
                    AnsiConsole.MarkupLine("[green]✓[/] All driver NuGet packages are installed");
                    return;
                }

                AnsiConsole.MarkupLine($"[yellow]Found {drivers.Count} driver(s) with missing NuGet packages:[/]");

                var table = new Table();
                table.Border = TableBorder.Rounded;
                table.AddColumn("[cyan]DataSource Type[/]");
                table.AddColumn("[cyan]Package Name[/]");
                table.AddColumn("[cyan]Has Source?[/]");

                foreach (var driver in drivers.OrderBy(d => d.DatasourceType.ToString()))
                {
                    var hasSource = !string.IsNullOrWhiteSpace(driver.NuggetSource);
                    table.AddRow(
                        driver.DatasourceType.ToString(),
                        driver.PackageName ?? "[dim]Unknown[/]",
                        hasSource ? "[green]Yes[/]" : "[red]No[/]"
                    );
                }

                AnsiConsole.Write(table);

                if (autoFix)
                {
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine("[cyan]Attempting automatic installation...[/]");

                    var fixable = drivers.Where(d => !string.IsNullOrWhiteSpace(d.NuggetSource)).ToList();
                    var fixedCount = 0;

                    foreach (var driver in fixable)
                    {
                        try
                        {
                            AnsiConsole.MarkupLine($"[dim]  Installing {driver.DatasourceType}...[/]");
                            InstallDriverNuget(driver.DatasourceType.ToString(), string.Empty, string.Empty);
                            if (!driver.NuggetMissing)
                            {
                                fixedCount++;
                            }
                        }
                        catch
                        {
                            // Continue with next
                        }
                    }

                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine($"[green]✓[/] Fixed {fixedCount} of {fixable.Count} fixable packages");

                    if (fixedCount < drivers.Count)
                    {
                        AnsiConsole.MarkupLine($"[yellow]⚠[/] {drivers.Count - fixedCount} package(s) still missing");
                        AnsiConsole.MarkupLine("[dim]Configure sources with 'driver-nuget set-source' and try again[/]");
                    }
                }
                else
                {
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine("[dim]Use --auto-fix flag to attempt automatic installation[/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error checking missing NuGets: {ex.Message}");
            }
        }

        private void SetDriverNugetSource(string dataSourceType, string source)
        {
            try
            {
                var driver = FindDriver(dataSourceType);
                if (driver == null) return;

                // Validate source
                var sourceType = "Unknown";
                if (File.Exists(source))
                {
                    sourceType = "File";
                }
                else if (Directory.Exists(source))
                {
                    sourceType = "Directory";
                }
                else if (source.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    sourceType = "URL";
                }
                else
                {
                    AnsiConsole.MarkupLine($"[yellow]⚠[/] Warning: Source path does not exist or is not a valid URL");
                }

                driver.NuggetSource = source;
                _editor.ConfigEditor.SaveConfigValues();

                AnsiConsole.MarkupLine($"[green]✓[/] NuGet source set for {dataSourceType}");
                AnsiConsole.MarkupLine($"[dim]  Type: {sourceType}[/]");
                AnsiConsole.MarkupLine($"[dim]  Source: {source}[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error setting NuGet source: {ex.Message}");
            }
        }

        private void InstallMissingInteractive(string defaultSource)
        {
            try
            {
                var missingDrivers = _editor.ConfigEditor.DataDriversClasses
                    .Where(d => d.NuggetMissing)
                    .OrderBy(d => d.DatasourceType.ToString())
                    .ToList();

                if (!missingDrivers.Any())
                {
                    AnsiConsole.MarkupLine("[green]✓[/] All driver NuGet packages are installed");
                    return;
                }

                // Display missing drivers with numbers
                var table = new Table();
                table.Border = TableBorder.Rounded;
                table.Title = new TableTitle($"[bold yellow]Missing Driver NuGet Packages ({missingDrivers.Count})[/]");
                table.AddColumn("[cyan]#[/]");
                table.AddColumn("[cyan]DataSource Type[/]");
                table.AddColumn("[cyan]Package Name[/]");
                table.AddColumn("[cyan]Has Source?[/]");
                table.AddColumn("[cyan]Source[/]");

                for (int i = 0; i < missingDrivers.Count; i++)
                {
                    var driver = missingDrivers[i];
                    var hasSource = !string.IsNullOrWhiteSpace(driver.NuggetSource);
                    var source = hasSource 
                        ? (driver.NuggetSource.Length > 40 ? "..." + driver.NuggetSource.Substring(driver.NuggetSource.Length - 40) : driver.NuggetSource)
                        : "[dim]Not configured[/]";

                    table.AddRow(
                        (i + 1).ToString(),
                        driver.DatasourceType.ToString(),
                        driver.PackageName ?? "[dim]Unknown[/]",
                        hasSource ? "[green]Yes[/]" : "[red]No[/]",
                        source
                    );
                }

                AnsiConsole.Write(table);
                AnsiConsole.WriteLine();

                // Prompt for selection
                var selectionPrompt = new MultiSelectionPrompt<string>()
                    .Title("[cyan]Select drivers to install (use [green]space[/] to select, [green]enter[/] to confirm):[/]")
                    .PageSize(15)
                    .MoreChoicesText("[grey](Move up and down to reveal more drivers)[/]")
                    .InstructionsText("[grey](Press [blue]<space>[/] to toggle a driver, [green]<enter>[/] to accept)[/]");

                for (int i = 0; i < missingDrivers.Count; i++)
                {
                    var driver = missingDrivers[i];
                    var label = $"{i + 1}. {driver.DatasourceType} - {driver.PackageName ?? "Unknown"}";
                    selectionPrompt.AddChoice(label);
                }

                var selectedLabels = AnsiConsole.Prompt(selectionPrompt);

                if (!selectedLabels.Any())
                {
                    AnsiConsole.MarkupLine("[yellow]No drivers selected[/]");
                    return;
                }

                // Parse selected numbers and install
                var selectedIndices = new List<int>();
                foreach (var label in selectedLabels)
                {
                    var indexStr = label.Split('.')[0].Trim();
                    if (int.TryParse(indexStr, out var index))
                    {
                        selectedIndices.Add(index - 1); // Convert to 0-based
                    }
                }

                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"[cyan]Installing {selectedIndices.Count} selected driver(s)...[/]");
                AnsiConsole.WriteLine();

                var successCount = 0;
                var failedCount = 0;

                foreach (var index in selectedIndices)
                {
                    if (index >= 0 && index < missingDrivers.Count)
                    {
                        var driver = missingDrivers[index];
                        var source = driver.NuggetSource ?? defaultSource;

                        AnsiConsole.MarkupLine($"[dim]Processing {index + 1}/{selectedIndices.Count}:[/] [cyan]{driver.DatasourceType}[/]");

                        try
                        {
                            if (string.IsNullOrWhiteSpace(source))
                            {
                                AnsiConsole.MarkupLine($"  [yellow]⚠[/] No source configured - skipping");
                                failedCount++;
                                continue;
                            }

                            // Use the existing install logic
                            var packageSource = source;
                            var packageName = driver.PackageName ?? driver.DriverClass;

                            if (File.Exists(packageSource))
                            {
                                var success = _editor.assemblyHandler.LoadNugget(packageSource);
                                if (success)
                                {
                                    driver.NuggetMissing = false;
                                    _editor.ConfigEditor.SaveConfigValues();
                                    AnsiConsole.MarkupLine($"  [green]✓[/] Installed from file");
                                    successCount++;
                                }
                                else
                                {
                                    AnsiConsole.MarkupLine($"  [red]✗[/] Failed to load package");
                                    failedCount++;
                                }
                            }
                            else if (Directory.Exists(packageSource))
                            {
                                var dllFiles = Directory.GetFiles(packageSource, "*.dll", SearchOption.AllDirectories);
                                var matchingDll = dllFiles.FirstOrDefault(f =>
                                    Path.GetFileNameWithoutExtension(f).Equals(packageName, StringComparison.OrdinalIgnoreCase));

                                if (matchingDll != null)
                                {
                                    var success = _editor.assemblyHandler.LoadNugget(matchingDll);
                                    if (success)
                                    {
                                        driver.NuggetMissing = false;
                                        driver.NuggetSource = packageSource;
                                        _editor.ConfigEditor.SaveConfigValues();
                                        AnsiConsole.MarkupLine($"  [green]✓[/] Installed from directory: {Path.GetFileName(matchingDll)}");
                                        successCount++;
                                    }
                                    else
                                    {
                                        AnsiConsole.MarkupLine($"  [red]✗[/] Failed to load package");
                                        failedCount++;
                                    }
                                }
                                else
                                {
                                    AnsiConsole.MarkupLine($"  [yellow]⚠[/] Package not found in directory");
                                    failedCount++;
                                }
                            }
                            else
                            {
                                AnsiConsole.MarkupLine($"  [yellow]⚠[/] Source not accessible");
                                failedCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            AnsiConsole.MarkupLine($"  [red]✗[/] Error: {ex.Message}");
                            failedCount++;
                        }
                    }
                }

                // Summary
                AnsiConsole.WriteLine();
                var summaryTable = new Table();
                summaryTable.Border = TableBorder.Rounded;
                summaryTable.AddColumn("Result");
                summaryTable.AddColumn("Count");
                summaryTable.AddRow("[green]Successfully Installed[/]", successCount.ToString());
                summaryTable.AddRow("[red]Failed[/]", failedCount.ToString());
                summaryTable.AddRow("[cyan]Total Processed[/]", (successCount + failedCount).ToString());
                AnsiConsole.Write(summaryTable);

                if (successCount > 0)
                {
                    AnsiConsole.MarkupLine($"\n[green]✓[/] {successCount} driver package(s) installed successfully!");
                }

                if (failedCount > 0)
                {
                    AnsiConsole.MarkupLine($"[yellow]⚠[/] {failedCount} driver package(s) failed to install");
                    AnsiConsole.MarkupLine("[dim]Configure sources with 'driver-nuget set-source <type> <path>' and try again[/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error during interactive installation: {ex.Message}");
            }
        }

        private ConnectionDriversConfig? FindDriver(string dataSourceType)
        {
            if (!Enum.TryParse<DataSourceType>(dataSourceType, true, out var dsType))
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Invalid DataSourceType: {dataSourceType}");
                AnsiConsole.MarkupLine("[dim]Use 'driver list' to see available types[/]");
                return null;
            }

            var driver = _editor.ConfigEditor.DataDriversClasses
                .FirstOrDefault(d => d.DatasourceType == dsType);

            if (driver == null)
            {
                AnsiConsole.MarkupLine($"[yellow]⚠[/] Driver not found for {dataSourceType}");
                return null;
            }

            return driver;
        }

        public bool CanExecute() => _editor != null;

        public string[] GetExamples()
        {
            return new[]
            {
                "driver-nuget list",
                "driver-nuget list --category RDBMS --missing-only",
                "driver-nuget install SqlServer --source C:\\packages",
                "driver-nuget update MongoDB",
                "driver-nuget remove Oracle",
                "driver-nuget info PostgreSQL",
                "driver-nuget sources",
                "driver-nuget check --auto-fix",
                "driver-nuget set-source Redis C:\\packages\\redis"
            };
        }
    }
}
