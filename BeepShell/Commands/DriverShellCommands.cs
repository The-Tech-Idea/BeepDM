using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Net.Http;
using BeepShell.Infrastructure;
using Spectre.Console;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Helpers;

namespace BeepShell.Commands
{
    /// <summary>
    /// Driver management commands for BeepShell
    /// Uses persistent DMEEditor for driver operations
    /// </summary>
    public class DriverShellCommands : IShellCommand
    {
        private IDMEEditor _editor = null!;
        private DriverPackageTracker? _tracker;
        private string _driversDirectory = null!;

        public string CommandName => "driver";
        public string Description => "Manage database drivers";
        public string Category => "Configuration";
        public string Version => "1.0.0";
        public string Author => "BeepDM Team";
        public string[] Aliases => new[] { "drv", "drivers" };

        public void Initialize(IDMEEditor editor)
        {
            _editor = editor;
            
            // Use ConfigEditor's ConnectionDrivers path
            _driversDirectory = _editor.ConfigEditor.Config.ConnectionDriversPath;
            
            // Ensure the directory exists
            if (string.IsNullOrEmpty(_driversDirectory))
            {
                _driversDirectory = Path.Combine(_editor.ConfigEditor.ExePath, "ConnectionDrivers");
            }
            
            if (!Directory.Exists(_driversDirectory))
            {
                Directory.CreateDirectory(_driversDirectory);
            }
            
            // Initialize tracker
            _tracker = new DriverPackageTracker(_editor.ConfigEditor.ExePath);
        }

        public Command BuildCommand()
        {
            var driverCommand = new Command("driver", Description);

            // driver list
            var listCommand = new Command("list", "List all available drivers");
            var categoryOpt = new Option<string>(new[] { "--category", "-c" }, "Filter by datasource category");
            var missingOpt = new Option<bool>(new[] { "--missing-only", "-m" }, "Show only drivers with missing packages");
            listCommand.AddOption(categoryOpt);
            listCommand.AddOption(missingOpt);
            listCommand.SetHandler((category, missingOnly) => ListDrivers(category, missingOnly), categoryOpt, missingOpt);
            driverCommand.AddCommand(listCommand);

            // driver info
            var infoCommand = new Command("info", "Show driver information");
            var nameArg = new Argument<string>("name", "Driver name or DataSourceType");
            infoCommand.AddArgument(nameArg);
            infoCommand.SetHandler((name) => ShowDriverInfo(name), nameArg);
            driverCommand.AddCommand(infoCommand);

            // driver test
            var testCommand = new Command("test", "Test driver availability");
            var testNameArg = new Argument<string>("name", "Driver name or DataSourceType");
            testCommand.AddArgument(testNameArg);
            testCommand.SetHandler((name) => TestDriver(name), testNameArg);
            driverCommand.AddCommand(testCommand);

            // driver install - Interactive installation
            var installCommand = new Command("install", "Install driver package interactively");
            installCommand.SetHandler(() => InstallDriverInteractive());
            driverCommand.AddCommand(installCommand);

            // driver install-from-file - Install from file system
            var installFileCommand = new Command("install-from-file", "Install driver from file system");
            var filePathArg = new Argument<string>("path", "Path to driver DLL or directory");
            var dsTypeOpt = new Option<string>(new[] { "--type", "-t" }, "DataSourceType (optional, will auto-detect if not provided)");
            installFileCommand.AddArgument(filePathArg);
            installFileCommand.AddOption(dsTypeOpt);
            installFileCommand.SetHandler((path, dsType) => InstallDriverFromFile(path, dsType), filePathArg, dsTypeOpt);
            driverCommand.AddCommand(installFileCommand);

            // driver install-missing - Batch install missing drivers
            var installMissingCommand = new Command("install-missing", "Install all missing drivers interactively");
            var autoOpt = new Option<bool>(new[] { "--auto", "-a" }, "Automatically install without confirmation");
            installMissingCommand.AddOption(autoOpt);
            installMissingCommand.SetHandler((auto) => InstallMissingDriversInteractive(auto), autoOpt);
            driverCommand.AddCommand(installMissingCommand);

            // driver update - Update an installed driver or check for updates
            var updateCommand = new Command("update", "Update an installed driver or check for updates");
            var updateNameOpt = new Option<string>(new[] { "--name", "-n" }, "Driver name or DataSourceType (if not provided, will show selection list)");
            var checkUpdatesOpt = new Option<bool>(new[] { "--check", "-c" }, "Check for available updates without installing");
            updateCommand.AddOption(updateNameOpt);
            updateCommand.AddOption(checkUpdatesOpt);
            updateCommand.SetHandler((name, checkOnly) => UpdateDriverInteractive(name, checkOnly), updateNameOpt, checkUpdatesOpt);
            driverCommand.AddCommand(updateCommand);

            // driver remove - Remove/unload a driver
            var removeCommand = new Command("remove", "Remove/unload a driver package");
            var removeNameOpt = new Option<string>(new[] { "--name", "-n" }, "Driver name or DataSourceType (if not provided, will show selection list)");
            removeCommand.AddOption(removeNameOpt);
            removeCommand.SetHandler((name) => RemoveDriverInteractive(name), removeNameOpt);
            driverCommand.AddCommand(removeCommand);

            // driver check - Check driver status
            var checkCommand = new Command("check", "Check all drivers for missing packages");
            checkCommand.SetHandler(() => CheckDriverStatus());
            driverCommand.AddCommand(checkCommand);

            // driver init - Initialize/populate drivers from ConnectionHelper
            var initCommand = new Command("init", "Initialize drivers from ConnectionHelper defaults");
            var overwriteOpt = new Option<bool>(new[] { "--overwrite", "-o" }, "Overwrite existing drivers");
            initCommand.AddOption(overwriteOpt);
            initCommand.SetHandler((overwrite) => InitializeDriversFromHelper(overwrite), overwriteOpt);
            driverCommand.AddCommand(initCommand);

            // driver clean - Clean ConnectionDrivers folder
            var cleanCommand = new Command("clean", "Clean ConnectionDrivers folder (removes all driver DLLs)");
            var forceOpt = new Option<bool>(new[] { "--force", "-f" }, "Force clean without confirmation");
            cleanCommand.AddOption(forceOpt);
            cleanCommand.SetHandler((force) => CleanConnectionDrivers(force), forceOpt);
            driverCommand.AddCommand(cleanCommand);

            // driver browse - Browse available packages from NuGet source
            var browseCommand = new Command("browse", "Browse available NuGet packages for a driver from its source");
            var browseNameOpt = new Option<string>(new[] { "--name", "-n" }, "Driver name or DataSourceType (if not provided, will show selection list)");
            browseCommand.AddOption(browseNameOpt);
            browseCommand.SetHandler((name) => BrowseDriverPackages(name), browseNameOpt);
            driverCommand.AddCommand(browseCommand);

            return driverCommand;
        }

        private void ListDrivers(string category, bool missingOnly)
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
                    // Use tracker for filtering
                    drivers = drivers.Where(d => !_tracker!.IsInstalled(d.DatasourceType)).ToList();
                }

                if (drivers == null || drivers.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]No drivers found matching criteria[/]");
                    return;
                }

                var table = new Table();
                table.Border = TableBorder.Rounded;
                table.Title = new TableTitle($"[bold cyan]Available Drivers ({drivers.Count})[/]");
                table.AddColumn("[cyan]#[/]");
                table.AddColumn("[cyan]DataSource Type[/]");
                table.AddColumn("[cyan]Driver Name[/]");
                table.AddColumn("[cyan]Class Name[/]");
                table.AddColumn("[cyan]Category[/]");
                table.AddColumn("[cyan]Package[/]");
                table.AddColumn("[cyan]Version[/]");
                table.AddColumn("[cyan]Status[/]");

                var index = 1;
                var missing = 0;
                var installed = 0;

                foreach (var driver in drivers)
                {
                    // Use tracker for status
                    var isInstalled = _tracker!.IsInstalled(driver.DatasourceType);
                    var installInfo = _tracker.GetInfo(driver.DatasourceType);
                    
                    var status = isInstalled ? "[green]Installed[/]" : "[red]Not Installed[/]";
                    var version = isInstalled && installInfo != null ? installInfo.PackageVersion : "-";
                    
                    if (isInstalled) installed++; else missing++;

                    table.AddRow(
                        index.ToString(),
                        driver.DatasourceType.ToString(),
                        driver.DriverClass ?? "-",
                        driver.classHandler ?? "-",
                        driver.DatasourceCategory.ToString(),
                        installInfo?.PackageName ?? driver.PackageName ?? "-",
                        version,
                        status
                    );
                    index++;
                }

                AnsiConsole.Write(table);
                AnsiConsole.MarkupLine($"\n[dim]Total: {drivers.Count} | [green]Installed: {installed}[/] | [red]Not Installed: {missing}[/][/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error listing drivers: {ex.Message}");
            }
        }

        private void ShowDriverInfo(string driverName)
        {
            try
            {
                var driver = _editor.ConfigEditor.DataDriversClasses
                    ?.FirstOrDefault(d => d.DriverClass.Equals(driverName, StringComparison.OrdinalIgnoreCase));

                if (driver == null)
                {
                    AnsiConsole.MarkupLine($"[yellow]Driver '{driverName}' not found[/]");
                    return;
                }

                var panel = new Panel(new Markup(
                    $"[cyan]Driver Name:[/] {driver.DriverClass}\n" +
                    $"[cyan]Class Handler:[/] {driver.classHandler}\n" +
                    $"[cyan]Category:[/] {driver.DatasourceCategory}\n" +
                    $"[cyan]Version:[/] {driver.version ?? "N/A"}\n" +
                    $"[cyan]Package:[/] {driver.PackageName ?? "N/A"}\n" +
                    $"[cyan]ADO Type:[/] {driver.ADOType}\n" +
                    $"[cyan]Extension:[/] {driver.extensionstoHandle ?? "N/A"}\n" +
                    $"[cyan]Create Local DB:[/] {driver.CreateLocal}"
                ));
                panel.Header = new PanelHeader($"[green]Driver: {driverName}[/]");
                panel.Border = BoxBorder.Rounded;

                AnsiConsole.Write(panel);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
            }
        }

        private void TestDriver(string driverName)
        {
            try
            {
                var driver = _editor.ConfigEditor.DataDriversClasses
                    ?.FirstOrDefault(d => d.DriverClass.Equals(driverName, StringComparison.OrdinalIgnoreCase));

                if (driver == null)
                {
                    AnsiConsole.MarkupLine($"[yellow]Driver '{driverName}' not found[/]");
                    return;
                }

                AnsiConsole.Status()
                    .Start($"Testing driver '{driverName}'...", ctx =>
                    {
                        // Check if driver class is loaded in assemblies
                        var loadedAssemblies = _editor.ConfigEditor.LoadedAssemblies;
                        var driverFound = false;

                        foreach (var assembly in loadedAssemblies)
                        {
                            try
                            {
                                var type = assembly.GetType(driver.classHandler);
                                if (type != null)
                                {
                                    driverFound = true;
                                    break;
                                }
                            }
                            catch { }
                        }

                        if (driverFound)
                        {
                            AnsiConsole.MarkupLine($"[green]✓[/] Driver '{driverName}' is available");
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[yellow]![/] Driver '{driverName}' configured but class not found in loaded assemblies");
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
            }
        }

        public bool CanExecute() => _editor != null;

        private void InstallDriverInteractive()
        {
            try
            {
                // Get all drivers
                var allDrivers = _editor.ConfigEditor.DataDriversClasses?.ToList();
                
                if (allDrivers == null || !allDrivers.Any())
                {
                    AnsiConsole.MarkupLine("[yellow]⚠[/] No drivers configured in the system");
                    AnsiConsole.MarkupLine($"[dim]Config path: {_editor.ConfigEditor.Config.ConfigPath}[/]");
                    AnsiConsole.MarkupLine("[dim]Drivers must be configured in the DataDriversClasses section of the config file.[/]");
                    AnsiConsole.MarkupLine("[dim]Try running 'driver list' to see if any drivers are available.[/]");
                    return;
                }

                // Use tracker to determine which drivers are not installed
                var missingDrivers = allDrivers
                    .Where(d => !_tracker!.IsInstalled(d.DatasourceType))
                    .ToList();

                AnsiConsole.MarkupLine($"[dim]Total drivers configured: {allDrivers.Count}[/]");
                AnsiConsole.MarkupLine($"[dim]Drivers not installed (per tracker): {missingDrivers.Count}[/]\n");

                if (!missingDrivers.Any())
                {
                    AnsiConsole.MarkupLine("[green]✓[/] All driver packages are installed!");
                    AnsiConsole.MarkupLine($"[dim]Total drivers: {allDrivers.Count}[/]");
                    
                    if (AnsiConsole.Confirm("\nWould you like to see the list of installed drivers?"))
                    {
                        ListDrivers(string.Empty, false);
                    }
                    return;
                }

                AnsiConsole.MarkupLine($"[yellow]Found {missingDrivers.Count} driver(s) not yet installed[/]\n");

                // Let user select a driver - show ALL drivers with installation status
                var allDriversForSelection = _editor.ConfigEditor.DataDriversClasses.ToList();
                
                var selectedDriver = AnsiConsole.Prompt(
                    new SelectionPrompt<ConnectionDriversConfig>()
                        .Title("[cyan]Select a driver to install:[/]")
                        .PageSize(15)
                        .MoreChoicesText("[grey](Move up and down to see more drivers)[/]")
                        .AddChoices(allDriversForSelection)
                        .UseConverter(d => 
                        {
                            var isInstalled = _tracker!.IsInstalled(d.DatasourceType);
                            var status = isInstalled ? "[green]✓ INSTALLED[/]" : "[yellow]⚠ NOT INSTALLED[/]";
                            return $"{status} {d.DatasourceType} - {d.PackageName ?? "Unknown"}";
                        }));

                var isAlreadyInstalled = _tracker!.IsInstalled(selectedDriver.DatasourceType);
                
                if (isAlreadyInstalled)
                {
                    var installInfo = _tracker.GetInfo(selectedDriver.DatasourceType);
                    
                    AnsiConsole.MarkupLine($"\n[green]✓[/] Driver [cyan]{selectedDriver.DatasourceType}[/] is already installed");
                    
                    if (installInfo != null)
                    {
                        AnsiConsole.MarkupLine($"[dim]  Package: {installInfo.PackageName}[/]");
                        AnsiConsole.MarkupLine($"[dim]  Version: {installInfo.PackageVersion}[/]");
                        AnsiConsole.MarkupLine($"[dim]  Installed: {installInfo.InstalledDate:yyyy-MM-dd HH:mm}[/]");
                        AnsiConsole.MarkupLine($"[dim]  Location: {installInfo.SourcePath}[/]");
                    }
                    
                    var action = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("\n[cyan]What would you like to do?[/]")
                            .AddChoices(new[] 
                            {
                                "Reinstall (overwrite existing)",
                                "Update to latest version",
                                "Cancel"
                            }));
                    
                    if (action == "Cancel")
                    {
                        AnsiConsole.MarkupLine("[yellow]Installation cancelled[/]");
                        return;
                    }
                    
                    if (action == "Update to latest version")
                    {
                        AnsiConsole.MarkupLine("[cyan]Updating will download the latest version from NuGet.org...[/]");
                    }
                }

                AnsiConsole.MarkupLine($"\n[cyan]Selected:[/] {selectedDriver.DatasourceType}\n");

                // Check if driver has a configured NuggetSource
                var hasConfiguredSource = !string.IsNullOrWhiteSpace(selectedDriver.NuggetSource);
                
                // Build installation method options
                var methodChoices = new List<string>
                {
                    "From File System (Browse for DLL or directory)",
                    "From NuGet Source (Specify path or URL)"
                };
                
                if (hasConfiguredSource)
                {
                    methodChoices.Insert(0, $"Browse Configured Source ({selectedDriver.NuggetSource})");
                }
                
                methodChoices.Add("Cancel");

                // Ask for installation method
                var method = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[cyan]How would you like to install the driver?[/]")
                        .AddChoices(methodChoices));

                if (method == "Cancel")
                {
                    AnsiConsole.MarkupLine("[yellow]Installation cancelled[/]");
                    return;
                }

                if (method.StartsWith("Browse Configured Source"))
                {
                    // Use the browse feature directly
                    AnsiConsole.MarkupLine($"[cyan]Opening browser for:[/] {selectedDriver.NuggetSource}\n");
                    
                    if (Directory.Exists(selectedDriver.NuggetSource))
                    {
                        BrowseDirectoryPackages(selectedDriver.NuggetSource, selectedDriver.PackageName, selectedDriver);
                    }
                    else if (File.Exists(selectedDriver.NuggetSource))
                    {
                        BrowseFilePackage(selectedDriver.NuggetSource, selectedDriver.PackageName, selectedDriver);
                    }
                    else if (selectedDriver.NuggetSource.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    {
                        BrowseOnlinePackages(selectedDriver.NuggetSource, selectedDriver.PackageName);
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[red]✗[/] Invalid source: {selectedDriver.NuggetSource}");
                        AnsiConsole.MarkupLine("[yellow]Falling back to manual entry...[/]");
                        var source = AnsiConsole.Ask<string>("[cyan]Enter NuGet source path or URL:[/]");
                        if (!string.IsNullOrWhiteSpace(source))
                        {
                            InstallDriverFromNuGetSource(source, selectedDriver);
                        }
                    }
                }
                else if (method.StartsWith("From File System"))
                {
                    var browseChoice = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("[cyan]Choose file system option:[/]")
                            .AddChoices(new[] 
                            {
                                "Browse directory for packages",
                                "Enter path manually",
                                "Back"
                            }));
                    
                    if (browseChoice == "Browse directory for packages")
                    {
                        var directory = AnsiConsole.Ask<string>("[cyan]Enter directory path to browse:[/]", Environment.CurrentDirectory);
                        
                        if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                        {
                            BrowseDirectoryPackages(directory, selectedDriver.PackageName, selectedDriver);
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[red]✗[/] Directory not found: {directory}");
                        }
                    }
                    else if (browseChoice == "Enter path manually")
                    {
                        var path = AnsiConsole.Ask<string>("[cyan]Enter path to driver DLL or directory:[/]");
                        
                        if (string.IsNullOrWhiteSpace(path))
                        {
                            AnsiConsole.MarkupLine("[red]✗[/] Path is required");
                            return;
                        }

                        InstallDriverFromFileWithDriver(path, selectedDriver);
                    }
                }
                else if (method.StartsWith("From NuGet Source"))
                {
                    // First, let user search for packages or enter manually
                    var searchChoice = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("[cyan]How would you like to find the package?[/]")
                            .AddChoices(new[] 
                            {
                                "Search NuGet.org (recommended)",
                                "Enter package name manually",
                                "Back"
                            }));
                    
                    if (searchChoice == "Back")
                    {
                        return;
                    }
                    
                    string? packageName = null;
                    
                    if (searchChoice == "Search NuGet.org (recommended)")
                    {
                        // Search NuGet.org for packages matching the driver type
                        var searchTerm = AnsiConsole.Ask<string>(
                            $"[cyan]Search term for {selectedDriver.DatasourceType} driver:[/]",
                            selectedDriver.PackageName ?? selectedDriver.DatasourceType.ToString());
                        
                        packageName = SearchAndSelectPackage(searchTerm);
                        
                        if (packageName == null)
                        {
                            AnsiConsole.MarkupLine("[yellow]Search cancelled[/]");
                            return;
                        }
                    }
                    else
                    {
                        packageName = AnsiConsole.Ask<string>(
                            "[cyan]Enter NuGet package name (e.g., Oracle.ManagedDataAccess):[/]",
                            selectedDriver.PackageName ?? selectedDriver.NuggetSource ?? "");
                    }

                    if (string.IsNullOrWhiteSpace(packageName))
                    {
                        AnsiConsole.MarkupLine("[red]✗[/] Package name is required");
                        return;
                    }

                    // Now select version
                    string? selectedVersion = null;
                    if (IsPackageName(packageName))
                    {
                        selectedVersion = SelectPackageVersion(packageName);
                        if (selectedVersion == "CANCELLED")
                        {
                            AnsiConsole.MarkupLine("[yellow]Installation cancelled[/]");
                            return;
                        }
                        
                        // Update driver version
                        selectedDriver.version = selectedVersion;
                    }

                    InstallDriverFromNuGetSource(packageName, selectedDriver);
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error in InstallDriverInteractive: {ex.Message}");
                AnsiConsole.MarkupLine($"[dim]{ex.StackTrace}[/]");
            }
        }

        private void InstallDriverFromFile(string path, string dataSourceType)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    AnsiConsole.MarkupLine("[red]✗[/] Path is required");
                    return;
                }

                if (!File.Exists(path) && !Directory.Exists(path))
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Path not found: {path}");
                    return;
                }

                ConnectionDriversConfig? driver = null;

                if (!string.IsNullOrWhiteSpace(dataSourceType))
                {
                    // Find specific driver
                    if (Enum.TryParse<DataSourceType>(dataSourceType, true, out var dsType))
                    {
                        driver = _editor.ConfigEditor.DataDriversClasses
                            .FirstOrDefault(d => d.DatasourceType == dsType);
                    }
                }

                if (driver == null)
                {
                    // Auto-detect or let user choose
                    var missingDrivers = _editor.ConfigEditor.DataDriversClasses
                        .Where(d => d.NuggetMissing)
                        .ToList();

                    if (!missingDrivers.Any())
                    {
                        AnsiConsole.MarkupLine("[yellow]No missing drivers to install[/]");
                        return;
                    }

                    driver = AnsiConsole.Prompt(
                        new SelectionPrompt<ConnectionDriversConfig>()
                            .Title("[cyan]Select target driver for this package:[/]")
                            .AddChoices(missingDrivers)
                            .UseConverter(d => $"{d.DatasourceType} - {d.PackageName ?? "Unknown"}"));
                }

                InstallDriverFromFileWithDriver(path, driver);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
            }
        }

        private void InstallDriverFromFileWithDriver(string path, ConnectionDriversConfig driver)
        {
            AnsiConsole.Status()
                .Start($"Installing driver package for [cyan]{driver.DatasourceType}[/]...", ctx =>
                {
                    try
                    {
                        bool success = false;

                        if (File.Exists(path))
                        {
                            ctx.Status($"Loading from file: {Path.GetFileName(path)}");
                            success = _editor.assemblyHandler.LoadNugget(path);
                        }
                        else if (Directory.Exists(path))
                        {
                            ctx.Status($"Loading from directory: {path}");
                            
                            // Find DLL files in directory
                            var dllFiles = Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories);
                            
                            if (!dllFiles.Any())
                            {
                                AnsiConsole.MarkupLine("[red]✗[/] No DLL files found in directory");
                                return;
                            }

                            // Try to find matching package DLL
                            var packageDll = dllFiles.FirstOrDefault(f => 
                                Path.GetFileNameWithoutExtension(f).Equals(driver.PackageName, StringComparison.OrdinalIgnoreCase));

                            if (packageDll != null)
                            {
                                success = _editor.assemblyHandler.LoadNugget(packageDll);
                            }
                            else
                            {
                                // Load first DLL or let user choose
                                var selectedDll = AnsiConsole.Prompt(
                                    new SelectionPrompt<string>()
                                        .Title("[cyan]Select DLL to load:[/]")
                                        .PageSize(10)
                                        .AddChoices(dllFiles)
                                        .UseConverter(f => Path.GetFileName(f)));

                                success = _editor.assemblyHandler.LoadNugget(selectedDll);
                            }
                        }

                        if (success)
                        {
                            driver.NuggetMissing = false;
                            _editor.ConfigEditor.SaveDataconnectionsValues();
                            
                            // Track installation
                            var actualPath = File.Exists(path) ? path : Directory.Exists(path) ? path : string.Empty;
                            _tracker!.MarkAsInstalled(
                                driver.DatasourceType,
                                driver.PackageName ?? "Unknown",
                                actualPath,
                                ""  // Version not available in ConnectionDriversConfig
                            );
                            
                            AnsiConsole.MarkupLine($"[green]✓[/] Driver package installed successfully for {driver.DatasourceType}");
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[red]✗[/] Failed to install driver package");
                        }
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine($"[red]✗[/] Error loading package: {ex.Message}");
                    }
                });
        }

        private void InstallDriverFromNuGetSource(string source, ConnectionDriversConfig driver)
        {
            AnsiConsole.Status()
                .Start($"Installing from NuGet source for [cyan]{driver.DatasourceType}[/]...", ctx =>
                {
                    try
                    {
                        // Update driver config with source
                        driver.NuggetSource = source;
                        
                        ctx.Status("Analyzing source...");
                        
                        // Build package path based on source type
                        string? packagePath = null;

        // Check if this is a package name (e.g., "System.Data.SQLite")
                        if (IsPackageName(source))
                        {
                            // Download from NuGet.org WITH DEPENDENCIES
                            ctx.Status($"Downloading [cyan]{source}[/] and dependencies from NuGet.org...");
                            
                            // Download to temp directory first
                            var tempDownloadDir = Path.Combine(Path.GetTempPath(), "BeepShell", "packages");
                            var downloader = new NuGetPackageDownloader(tempDownloadDir);
                            
                            // Download package WITH DEPENDENCIES
                            var downloadTask = downloader.DownloadPackageWithDependenciesAsync(source, driver.version);
                            downloadTask.Wait();
                            var allPackages = downloadTask.Result;
                            
                            if (allPackages.Any())
                            {
                                AnsiConsole.MarkupLine($"[green]✓[/] Downloaded {allPackages.Count} packages (including dependencies)");
                                
                                // Create driver-specific folder in ConnectionDrivers directory
                                var driverFolder = Path.Combine(_driversDirectory, driver.DatasourceType.ToString());
                                
                                // Check if driver is already loaded and unload it first
                                var existingDriver = _editor.DataSources.FirstOrDefault(ds => ds.DatasourceType == driver.DatasourceType);
                                if (existingDriver != null)
                                {
                                    ctx.Status("Unloading existing driver from memory...");
                                    try
                                    {
                                        existingDriver.Closeconnection();
                                        // Give the system time to release file locks
                                        System.Threading.Thread.Sleep(500);
                                    }
                                    catch { }
                                }
                                
                                if (!Directory.Exists(driverFolder))
                                {
                                    Directory.CreateDirectory(driverFolder);
                                }
                                
                                ctx.Status("Copying all DLLs to ConnectionDrivers folder...");
                                
                                // Copy ALL DLLs from all packages (main + dependencies)
                                // The allPackages dictionary contains packageName -> compatibleFrameworkFolderPath
                                var copiedFiles = new List<string>();
                                
                                foreach (var package in allPackages)
                                {
                                    var packageName = package.Key;
                                    var compatibleFrameworkPath = package.Value; // This is already the compatible framework folder
                                    
                                    if (string.IsNullOrEmpty(compatibleFrameworkPath) || !Directory.Exists(compatibleFrameworkPath))
                                    {
                                        AnsiConsole.MarkupLine($"[yellow]⚠[/] No compatible framework folder found for {packageName}");
                                        continue;
                                    }
                                    
                                    // Find all DLL files in the compatible framework folder
                                    var dllFiles = Directory.GetFiles(compatibleFrameworkPath, "*.dll", SearchOption.TopDirectoryOnly);
                                    
                                    foreach (var dllPath in dllFiles)
                                    {
                                        var fileName = Path.GetFileName(dllPath);
                                        var targetPath = Path.Combine(driverFolder, fileName);
                                        
                                        try
                                        {
                                            File.Copy(dllPath, targetPath, overwrite: true);
                                            copiedFiles.Add(fileName);
                                        }
                                        catch (IOException copyEx) when (copyEx.Message.Contains("being used by another process"))
                                        {
                                            AnsiConsole.MarkupLine($"[yellow]⚠[/] {fileName} is locked - restart shell to update");
                                        }
                                        catch (Exception copyEx)
                                        {
                                            AnsiConsole.MarkupLine($"[yellow]⚠[/] Could not copy {fileName}: {copyEx.Message}");
                                        }
                                    }
                                }
                                
                                if (copiedFiles.Any())
                                {
                                    AnsiConsole.MarkupLine($"[green]✓[/] Copied {copiedFiles.Count} DLLs to {driverFolder}");
                                    AnsiConsole.MarkupLine($"[dim]  → {string.Join(", ", copiedFiles.Take(5))}{(copiedFiles.Count > 5 ? $" and {copiedFiles.Count - 5} more..." : "")}[/]");
                                    
                                    // Set package path to the driver folder
                                    packagePath = driverFolder;
                                    AnsiConsole.MarkupLine($"[green]✓[/] Driver and dependencies installed to: ConnectionDrivers\\{driver.DatasourceType}");
                                }
                                else
                                {
                                    AnsiConsole.MarkupLine($"[red]✗[/] No DLL files found in downloaded packages");
                                }
                            }
                            else
                            {
                                AnsiConsole.MarkupLine($"[red]✗[/] Failed to download package from NuGet.org");
                                AnsiConsole.MarkupLine($"[dim]Try: dotnet add package {source}[/]");
                                return;
                            }
                        }
                        else if (Directory.Exists(source))
                        {
                            // Local directory - search for package
                            var packageName = driver.PackageName;
                            var dllFiles = Directory.GetFiles(source, $"{packageName}*.dll", SearchOption.AllDirectories);
                            
                            if (dllFiles.Any())
                            {
                                packagePath = dllFiles.First();
                            }
                        }
                        else if (File.Exists(source))
                        {
                            // Direct file path
                            packagePath = source;
                        }
                        else if (source.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                        {
                            AnsiConsole.MarkupLine("[yellow]⚠[/] URL-based NuGet sources require NuGet.exe integration");
                            AnsiConsole.MarkupLine("[dim]Please download the package manually and use install-from-file instead[/]");
                            return;
                        }

                        if (string.IsNullOrEmpty(packagePath))
                        {
                            AnsiConsole.MarkupLine($"[red]✗[/] Package not found at source: {source}");
                            AnsiConsole.MarkupLine($"[dim]Hint: Enter a package name (e.g., System.Data.SQLite), file path, or directory[/]");
                            return;
                        }

                        ctx.Status($"Loading package: {Path.GetFileName(packagePath)}");
                        
                        // Verify runtime compatibility before loading
                        if (!VerifyRuntimeCompatibility(packagePath))
                        {
                            AnsiConsole.MarkupLine($"[yellow]⚠[/] Warning: Driver may have runtime compatibility issues with BeepShell (net8.0)");
                            var proceed = AnsiConsole.Confirm("Do you want to try loading it anyway?");
                            if (!proceed)
                            {
                                AnsiConsole.MarkupLine($"[dim]Installation cancelled by user[/]");
                                return;
                            }
                        }
                        
                        var loadSuccess = _editor.assemblyHandler.LoadNugget(packagePath);

                        if (loadSuccess)
                        {
                            driver.NuggetMissing = false;
                            _editor.ConfigEditor.SaveDataconnectionsValues();
                            
                            // Track installation
                            _tracker!.MarkAsInstalled(
                                driver.DatasourceType,
                                driver.PackageName ?? "Unknown",
                                packagePath,
                                driver.version ?? ""
                            );
                            
                            AnsiConsole.MarkupLine($"[green]✓[/] Driver package installed and loaded at runtime");
                            AnsiConsole.MarkupLine($"[dim]Note: Driver is loaded into BeepShell's runtime (net8.0)[/]");
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[red]✗[/] Failed to install driver package");
                            AnsiConsole.MarkupLine($"[dim]Possible causes: Runtime incompatibility, missing dependencies, or invalid assembly[/]");
                        }
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
                    }
                });
        }

        private void InstallMissingDriversInteractive(bool auto)
        {
            try
            {
                // Use tracker to find missing drivers
                var allDrivers = _editor.ConfigEditor.DataDriversClasses?.ToList();
                if (allDrivers == null || !allDrivers.Any())
                {
                    AnsiConsole.MarkupLine("[yellow]⚠[/] No drivers configured in the system");
                    AnsiConsole.MarkupLine($"[dim]Config path: {_editor.ConfigEditor.Config.ConfigPath}[/]");
                    AnsiConsole.MarkupLine("[dim]Use 'driver list' to see available drivers or check your configuration.[/]");
                    return;
                }

                var missingDrivers = allDrivers
                    .Where(d => !_tracker!.IsInstalled(d.DatasourceType))
                    .ToList();

                if (!missingDrivers.Any())
                {
                    AnsiConsole.MarkupLine("[green]✓[/] All drivers are already installed!");
                    return;
                }

                AnsiConsole.MarkupLine($"[yellow]Found {missingDrivers.Count} missing driver(s)[/]\n");

                // Show table of missing drivers
                var table = new Table();
                table.Border = TableBorder.Rounded;
                table.AddColumn("[cyan]#[/]");
                table.AddColumn("[cyan]DataSource Type[/]");
                table.AddColumn("[cyan]Package Name[/]");
                table.AddColumn("[cyan]Source[/]");

                var index = 1;
                foreach (var driver in missingDrivers)
                {
                    table.AddRow(
                        index.ToString(),
                        driver.DatasourceType.ToString(),
                        driver.PackageName ?? "Unknown",
                        driver.NuggetSource ?? "[dim]Not configured[/]"
                    );
                    index++;
                }

                AnsiConsole.Write(table);

                if (!auto)
                {
                    // Let user select which drivers to install
                    var selectedDrivers = AnsiConsole.Prompt(
                        new MultiSelectionPrompt<ConnectionDriversConfig>()
                            .Title("[cyan]Select drivers to install:[/]")
                            .PageSize(15)
                            .InstructionsText("[grey](Press [blue]space[/] to select, [green]enter[/] to confirm)[/]")
                            .AddChoices(missingDrivers)
                            .UseConverter(d => $"{d.DatasourceType} - {d.PackageName ?? "Unknown"}"));

                    if (!selectedDrivers.Any())
                    {
                        AnsiConsole.MarkupLine("[yellow]No drivers selected[/]");
                        return;
                    }

                    missingDrivers = selectedDrivers.ToList();
                }

                // Ask for installation mode
                var installMode = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[cyan]Select installation mode:[/]")
                        .AddChoices(new[] 
                        {
                            "Browse and install interactively (recommended)",
                            "Auto-install from configured sources",
                            "Specify default source directory",
                            "Cancel"
                        }));

                if (installMode == "Cancel")
                {
                    AnsiConsole.MarkupLine("[yellow]Installation cancelled[/]");
                    return;
                }

                if (installMode.StartsWith("Browse and install"))
                {
                    // Interactive browse mode for each driver
                    var successCount = 0;
                    var failedCount = 0;
                    var skippedCount = 0;

                    foreach (var driver in missingDrivers)
                    {
                        AnsiConsole.MarkupLine($"\n[bold cyan]═══ Installing {driver.DatasourceType} ({successCount + failedCount + skippedCount + 1}/{missingDrivers.Count}) ═══[/]");
                        
                        if (!string.IsNullOrWhiteSpace(driver.NuggetSource))
                        {
                            AnsiConsole.MarkupLine($"[dim]Configured source: {driver.NuggetSource}[/]");
                            
                            var useConfigured = AnsiConsole.Confirm($"Use configured source to browse for {driver.PackageName}?", true);
                            
                            if (useConfigured)
                            {
                                try
                                {
                                    if (Directory.Exists(driver.NuggetSource))
                                    {
                                        BrowseDirectoryPackages(driver.NuggetSource, driver.PackageName, driver);
                                    }
                                    else if (File.Exists(driver.NuggetSource))
                                    {
                                        BrowseFilePackage(driver.NuggetSource, driver.PackageName, driver);
                                    }
                                    else
                                    {
                                        AnsiConsole.MarkupLine($"[yellow]⚠[/] Source not accessible: {driver.NuggetSource}");
                                        failedCount++;
                                        continue;
                                    }
                                    
                                    // Check if installed after browse
                                    if (_tracker!.IsInstalled(driver.DatasourceType))
                                    {
                                        successCount++;
                                    }
                                    else
                                    {
                                        skippedCount++;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
                                    failedCount++;
                                }
                                continue;
                            }
                        }
                        
                        // No configured source or user declined - ask for path
                        var skipDriver = !AnsiConsole.Confirm($"Specify source for {driver.DatasourceType}?", true);
                        if (skipDriver)
                        {
                            AnsiConsole.MarkupLine($"[yellow]Skipped {driver.DatasourceType}[/]");
                            skippedCount++;
                            continue;
                        }
                        
                        var manualSource = AnsiConsole.Ask<string>("[cyan]Enter directory path to browse:[/]");
                        
                        if (!string.IsNullOrWhiteSpace(manualSource) && Directory.Exists(manualSource))
                        {
                            try
                            {
                                BrowseDirectoryPackages(manualSource, driver.PackageName, driver);
                                
                                if (_tracker!.IsInstalled(driver.DatasourceType))
                                {
                                    successCount++;
                                }
                                else
                                {
                                    skippedCount++;
                                }
                            }
                            catch (Exception ex)
                            {
                                AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
                                failedCount++;
                            }
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[yellow]⚠[/] Invalid directory, skipping {driver.DatasourceType}");
                            skippedCount++;
                        }
                    }

                    // Show summary
                    var summaryPanel = new Panel(new Markup(
                        $"[green]✓ Installed:[/] {successCount}\n" +
                        $"[red]✗ Failed:[/] {failedCount}\n" +
                        $"[yellow]⊘ Skipped:[/] {skippedCount}\n" +
                        $"[cyan]Total:[/] {missingDrivers.Count}"
                    ));
                    summaryPanel.Header = new PanelHeader("[bold cyan]Installation Summary[/]");
                    summaryPanel.Border = BoxBorder.Rounded;
                    AnsiConsole.Write(summaryPanel);
                    
                    return;
                }
                else if (installMode.StartsWith("Auto-install"))
                {
                    // Original auto-install logic using configured sources
                    var successCount = 0;
                    var failedCount = 0;

                    foreach (var driver in missingDrivers)
                    {
                        AnsiConsole.MarkupLine($"\n[cyan]Installing {driver.DatasourceType}...[/]");

                        var source = driver.NuggetSource;

                        if (string.IsNullOrWhiteSpace(source))
                        {
                            AnsiConsole.MarkupLine($"[yellow]⚠[/] No source configured for {driver.DatasourceType}, skipping...");
                            failedCount++;
                            continue;
                        }

                        try
                        {
                            InstallDriverFromNuGetSource(source, driver);
                            if (_tracker!.IsInstalled(driver.DatasourceType))
                            {
                                successCount++;
                            }
                            else
                            {
                                failedCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            AnsiConsole.MarkupLine($"[red]✗[/] Error installing {driver.DatasourceType}: {ex.Message}");
                            failedCount++;
                        }
                    }

                    AnsiConsole.MarkupLine($"\n[bold]Installation complete:[/] {successCount} succeeded, {failedCount} failed");
                    return;
                }

                // Specify default source directory mode
                var defaultSource = AnsiConsole.Ask<string>(
                    "[cyan]Enter default source directory:[/]",
                    "");

                if (string.IsNullOrWhiteSpace(defaultSource))
                {
                    AnsiConsole.MarkupLine("[red]✗[/] Default source is required for this mode");
                    return;
                }

                var successCount2 = 0;
                var failedCount2 = 0;

                foreach (var driver in missingDrivers)
                {
                    AnsiConsole.MarkupLine($"\n[cyan]Installing {driver.DatasourceType}...[/]");

                    var source = driver.NuggetSource ?? defaultSource;

                    if (string.IsNullOrWhiteSpace(source))
                    {
                        AnsiConsole.MarkupLine($"[yellow]⚠[/] No source configured for {driver.DatasourceType}, skipping...");
                        failedCount2++;
                        continue;
                    }

                    try
                    {
                        InstallDriverFromNuGetSource(source, driver);
                        if (_tracker!.IsInstalled(driver.DatasourceType))
                        {
                            successCount2++;
                        }
                        else
                        {
                            failedCount2++;
                        }
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine($"[red]✗[/] Failed to install {driver.DatasourceType}: {ex.Message}");
                        failedCount2++;
                    }
                }

                AnsiConsole.MarkupLine($"\n[bold]Installation complete:[/] {successCount2} succeeded, {failedCount2} failed");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
            }
        }

        private void CheckDriverStatus()
        {
            try
            {
                var drivers = _editor.ConfigEditor.DataDriversClasses.ToList();
                
                // Use tracker to determine actual status
                var installedDrivers = drivers.Where(d => _tracker!.IsInstalled(d.DatasourceType)).ToList();
                var missingDrivers = drivers.Where(d => !_tracker!.IsInstalled(d.DatasourceType)).ToList();

                var panel = new Panel(new Markup(
                    $"[cyan]Total Drivers:[/] {drivers.Count}\n" +
                    $"[green]Installed:[/] {installedDrivers.Count}\n" +
                    $"[red]Not Installed:[/] {missingDrivers.Count}\n" +
                    $"[dim]Tracker File:[/] [dim]{_tracker!.GetType().GetField("_trackingFilePath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_tracker)}[/]"
                ));
                panel.Header = new PanelHeader("[bold yellow]Driver Status[/]");
                panel.Border = BoxBorder.Rounded;

                AnsiConsole.Write(panel);

                if (missingDrivers.Any())
                {
                    AnsiConsole.MarkupLine("\n[yellow]Drivers not yet installed:[/]");
                    foreach (var driver in missingDrivers)
                    {
                        AnsiConsole.MarkupLine($"  [red]✗[/] {driver.DatasourceType} - {driver.PackageName ?? "Unknown"}");
                    }

                    if (AnsiConsole.Confirm("\nWould you like to install missing drivers now?"))
                    {
                        InstallMissingDriversInteractive(false);
                    }
                }
                else
                {
                    AnsiConsole.MarkupLine("\n[green]✓ All drivers are installed![/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
            }
        }

        private void UpdateDriverInteractive(string driverName, bool checkOnly = false)
        {
            try
            {
                ConnectionDriversConfig? driver = null;

                if (string.IsNullOrWhiteSpace(driverName))
                {
                    // Show selection list of installed drivers (use tracker)
                    var installedDrivers = _editor.ConfigEditor.DataDriversClasses
                        .Where(d => _tracker!.IsInstalled(d.DatasourceType))
                        .ToList();

                    if (!installedDrivers.Any())
                    {
                        AnsiConsole.MarkupLine("[yellow]⚠[/] No installed drivers to update");
                        return;
                    }

                    driver = AnsiConsole.Prompt(
                        new SelectionPrompt<ConnectionDriversConfig>()
                            .Title(checkOnly ? "[cyan]Select a driver to check for updates:[/]" : "[cyan]Select a driver to update:[/]")
                            .PageSize(15)
                            .MoreChoicesText("[grey](Move up and down to see more drivers)[/]")
                            .AddChoices(installedDrivers)
                            .UseConverter(d =>
                            {
                                var info = _tracker!.GetInfo(d.DatasourceType);
                                var version = info?.PackageVersion ?? "unknown";
                                return $"{d.DatasourceType} - {info?.PackageName ?? d.PackageName ?? "Unknown"} v{version}";
                            }));
                }
                else
                {
                    // Find driver by name or DataSourceType
                    driver = FindDriverByNameOrType(driverName);

                    if (driver == null)
                    {
                        AnsiConsole.MarkupLine($"[yellow]⚠[/] Driver not found: {driverName}");
                        return;
                    }
                }

                if (!_tracker!.IsInstalled(driver.DatasourceType))
                {
                    AnsiConsole.MarkupLine($"[yellow]⚠[/] Driver {driver.DatasourceType} is not currently installed");
                    if (AnsiConsole.Confirm("Would you like to install it instead?"))
                    {
                        InstallDriverInteractive();
                    }
                    return;
                }

                var installInfo = _tracker.GetInfo(driver.DatasourceType);
                var currentVersion = installInfo?.PackageVersion ?? "unknown";
                var packageName = installInfo?.PackageName ?? driver.PackageName;

                // Check for updates on NuGet.org
                if (checkOnly || !string.IsNullOrEmpty(packageName))
                {
                    AnsiConsole.MarkupLine($"\n[cyan]Current version:[/] {packageName} v{currentVersion}");
                    
                    var latestVersion = GetLatestPackageVersion(packageName!);
                    
                    if (latestVersion == null)
                    {
                        AnsiConsole.MarkupLine("[yellow]⚠[/] Could not check for updates");
                    }
                    else if (latestVersion == currentVersion)
                    {
                        AnsiConsole.MarkupLine("[green]✓[/] You have the latest version!");
                        if (checkOnly) return;
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[yellow]⚠[/] Update available: v{latestVersion}");
                        
                        if (checkOnly)
                        {
                            if (AnsiConsole.Confirm($"Would you like to update to v{latestVersion} now?"))
                            {
                                checkOnly = false; // Proceed with update
                                driver.version = latestVersion;
                            }
                            else
                            {
                                return;
                            }
                        }
                        else
                        {
                            driver.version = latestVersion;
                        }
                    }
                }
                
                if (checkOnly) return; // Only checking, not updating

                // Confirm update
                if (!AnsiConsole.Confirm($"[yellow]Update driver package for {driver.DatasourceType}?[/]"))
                {
                    AnsiConsole.MarkupLine("[yellow]Update cancelled[/]");
                    return;
                }

                // Ask for new source
                var updateMethod = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[cyan]How would you like to update the driver?[/]")
                        .AddChoices(new[]
                        {
                            "From File System (Browse for new DLL or directory)",
                            "From Current NuGet Source",
                            "From New NuGet Source",
                            "Cancel"
                        }));

                if (updateMethod == "Cancel")
                {
                    AnsiConsole.MarkupLine("[yellow]Update cancelled[/]");
                    return;
                }

                // First, try to unload the current version
                AnsiConsole.Status()
                    .Start($"Unloading current version of {driver.DatasourceType}...", ctx =>
                    {
                        try
                        {
                            if (!string.IsNullOrWhiteSpace(packageName))
                            {
                                var unloaded = _editor.assemblyHandler.UnloadNugget(packageName);
                                if (unloaded)
                                {
                                    AnsiConsole.MarkupLine($"[green]✓[/] Unloaded current version");
                                }
                                else
                                {
                                    AnsiConsole.MarkupLine($"[yellow]⚠[/] Could not unload current version (will attempt to reload)");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            AnsiConsole.MarkupLine($"[yellow]⚠[/] Error unloading: {ex.Message}");
                        }
                    });

                // Now install the new version
                if (updateMethod.StartsWith("From File System"))
                {
                    var path = AnsiConsole.Ask<string>("[cyan]Enter path to new driver DLL or directory:[/]");

                    if (string.IsNullOrWhiteSpace(path))
                    {
                        AnsiConsole.MarkupLine("[red]✗[/] Path is required");
                        return;
                    }

                    InstallDriverFromFileWithDriver(path, driver);
                }
                else if (updateMethod == "From Current NuGet Source")
                {
                    if (string.IsNullOrWhiteSpace(driver.NuggetSource))
                    {
                        AnsiConsole.MarkupLine("[yellow]⚠[/] No NuGet source configured for this driver");
                        var source = AnsiConsole.Ask<string>("[cyan]Enter NuGet source path:[/]");
                        InstallDriverFromNuGetSource(source, driver);
                    }
                    else
                    {
                        InstallDriverFromNuGetSource(driver.NuggetSource, driver);
                    }
                }
                else if (updateMethod == "From New NuGet Source")
                {
                    var source = AnsiConsole.Ask<string>(
                        "[cyan]Enter new NuGet source path or URL:[/]",
                        driver.NuggetSource ?? "");

                    if (string.IsNullOrWhiteSpace(source))
                    {
                        AnsiConsole.MarkupLine("[red]✗[/] Source is required");
                        return;
                    }

                    InstallDriverFromNuGetSource(source, driver);
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
            }
        }

        private void RemoveDriverInteractive(string driverName)
        {
            try
            {
                ConnectionDriversConfig? driver = null;

                if (string.IsNullOrWhiteSpace(driverName))
                {
                    // Show selection list of installed drivers (use tracker)
                    var installedDrivers = _editor.ConfigEditor.DataDriversClasses
                        .Where(d => _tracker!.IsInstalled(d.DatasourceType))
                        .ToList();

                    if (!installedDrivers.Any())
                    {
                        AnsiConsole.MarkupLine("[yellow]⚠[/] No installed drivers to remove");
                        return;
                    }

                    driver = AnsiConsole.Prompt(
                        new SelectionPrompt<ConnectionDriversConfig>()
                            .Title("[cyan]Select a driver to remove:[/]")
                            .PageSize(15)
                            .MoreChoicesText("[grey](Move up and down to see more drivers)[/]")
                            .AddChoices(installedDrivers)
                            .UseConverter(d => $"{d.DatasourceType} - {d.PackageName ?? "Unknown"}"));
                }
                else
                {
                    // Find driver by name or DataSourceType
                    driver = FindDriverByNameOrType(driverName);

                    if (driver == null)
                    {
                        AnsiConsole.MarkupLine($"[yellow]⚠[/] Driver not found: {driverName}");
                        return;
                    }
                }

                if (!_tracker!.IsInstalled(driver.DatasourceType))
                {
                    AnsiConsole.MarkupLine($"[yellow]⚠[/] Driver {driver.DatasourceType} is not currently installed");
                    return;
                }

                // Confirm removal
                if (!AnsiConsole.Confirm($"[red]Are you sure you want to remove driver package for {driver.DatasourceType}?[/]"))
                {
                    AnsiConsole.MarkupLine("[yellow]Removal cancelled[/]");
                    return;
                }

                AnsiConsole.Status()
                    .Start($"Removing driver package for [cyan]{driver.DatasourceType}[/]...", ctx =>
                    {
                        try
                        {
                            var packageName = driver.PackageName;
                            
                            if (string.IsNullOrWhiteSpace(packageName))
                            {
                                AnsiConsole.MarkupLine("[yellow]⚠[/] No package name configured for this driver");
                                return;
                            }

                            // Unload from memory
                            ctx.Status("Unloading driver from memory...");
                            var success = _editor.assemblyHandler.UnloadNugget(packageName);

                            if (success)
                            {
                                AnsiConsole.MarkupLine($"[green]✓[/] Driver package unloaded from memory");
                            }
                            else
                            {
                                AnsiConsole.MarkupLine($"[yellow]⚠[/] Could not unload from memory (may already be unloaded)");
                            }

                            // Delete physical DLL folder from ConnectionDrivers
                            ctx.Status("Removing DLL files from ConnectionDrivers folder...");
                            var driverFolder = Path.Combine(_driversDirectory, driver.DatasourceType.ToString());
                            
                            if (Directory.Exists(driverFolder))
                            {
                                try
                                {
                                    Directory.Delete(driverFolder, recursive: true);
                                    AnsiConsole.MarkupLine($"[green]✓[/] Deleted driver folder: ConnectionDrivers\\{driver.DatasourceType}");
                                }
                                catch (Exception deleteEx)
                                {
                                    AnsiConsole.MarkupLine($"[yellow]⚠[/] Could not delete folder (files may be in use): {deleteEx.Message}");
                                    AnsiConsole.MarkupLine($"[dim]You may need to manually delete: {driverFolder}[/]");
                                }
                            }
                            else
                            {
                                AnsiConsole.MarkupLine($"[dim]Driver folder not found (already removed): {driverFolder}[/]");
                            }

                            // Update configuration
                            driver.NuggetMissing = true;
                            _editor.ConfigEditor.SaveDataconnectionsValues();
                            
                            // Track removal and update log
                            _tracker!.MarkAsRemoved(driver.DatasourceType);
                            
                            AnsiConsole.MarkupLine($"[green]✓[/] Driver {driver.DatasourceType} removed successfully");
                            AnsiConsole.MarkupLine($"[dim]Updated: installed_drivers.json[/]");
                        }
                        catch (Exception ex)
                        {
                            AnsiConsole.MarkupLine($"[red]✗[/] Error removing package: {ex.Message}");
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
            }
        }

        private ConnectionDriversConfig? FindDriverByNameOrType(string nameOrType)
        {
            // Try to find by DataSourceType first
            if (Enum.TryParse<DataSourceType>(nameOrType, true, out var dsType))
            {
                var driver = _editor.ConfigEditor.DataDriversClasses
                    .FirstOrDefault(d => d.DatasourceType == dsType);
                
                if (driver != null)
                    return driver;
            }

            // Try to find by driver name or package name
            return _editor.ConfigEditor.DataDriversClasses
                .FirstOrDefault(d => 
                    d.DriverClass?.Equals(nameOrType, StringComparison.OrdinalIgnoreCase) == true ||
                    d.PackageName?.Equals(nameOrType, StringComparison.OrdinalIgnoreCase) == true);
        }

        private void InitializeDriversFromHelper(bool overwrite)
        {
            try
            {
                AnsiConsole.MarkupLine("[cyan]Initializing drivers from ConnectionHelper...[/]");
                
                // Get all default driver configurations from ConnectionHelper
                var helperDrivers = ConnectionHelper.GetAllConnectionConfigs();
                
                AnsiConsole.MarkupLine($"[dim]Found {helperDrivers.Count} driver definitions in ConnectionHelper[/]");
                
                // Ensure DataDriversClasses is initialized
                if (_editor.ConfigEditor.DataDriversClasses == null)
                {
                    // Initialize the collection if it's null
                    var field = _editor.ConfigEditor.GetType().GetProperty("DataDriversClasses");
                    if (field != null)
                    {
                        field.SetValue(_editor.ConfigEditor, new List<ConnectionDriversConfig>());
                    }
                }
                
                // Get existing drivers
                var existingDrivers = _editor.ConfigEditor.DataDriversClasses?.ToList() ?? new List<ConnectionDriversConfig>();
                
                var added = 0;
                var skipped = 0;
                var updated = 0;

                foreach (var helperDriver in helperDrivers)
                {
                    var existing = existingDrivers.FirstOrDefault(d => d.DatasourceType == helperDriver.DatasourceType);
                    
                    if (existing != null)
                    {
                        if (overwrite)
                        {
                            // Update existing driver
                            _editor.ConfigEditor.DataDriversClasses?.Remove(existing);
                            _editor.ConfigEditor.DataDriversClasses?.Add(helperDriver);
                            updated++;
                        }
                        else
                        {
                            skipped++;
                        }
                    }
                    else
                    {
                        // Add new driver
                        _editor.ConfigEditor.DataDriversClasses?.Add(helperDriver);
                        added++;
                    }
                }

                // Save the configuration
                _editor.ConfigEditor.SaveDataconnectionsValues();
                
                var totalDrivers = _editor.ConfigEditor.DataDriversClasses?.Count ?? 0;
                
                var panel = new Panel(new Markup(
                    $"[green]✓ Drivers initialized successfully[/]\n\n" +
                    $"[cyan]Added:[/] {added}\n" +
                    $"[yellow]Updated:[/] {updated}\n" +
                    $"[dim]Skipped (existing):[/] {skipped}\n" +
                    $"[cyan]Total in config:[/] {totalDrivers}"
                ));
                panel.Header = new PanelHeader("[bold cyan]Driver Initialization Complete[/]");
                panel.Border = BoxBorder.Rounded;
                
                AnsiConsole.Write(panel);
                
                if (added > 0 || updated > 0)
                {
                    AnsiConsole.MarkupLine("\n[dim]Configuration saved to: {0}[/]", _editor.ConfigEditor.Config.ConfigPath);
                    AnsiConsole.MarkupLine("[dim]Use 'driver list' to see all configured drivers[/]");
                    AnsiConsole.MarkupLine("[dim]Use 'driver install' to install driver packages[/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error initializing drivers: {ex.Message}");
                AnsiConsole.WriteException(ex);
            }
        }

        private void BrowseDriverPackages(string driverName)
        {
            try
            {
                ConnectionDriversConfig driver;

                // If name not provided, show selection list
                if (string.IsNullOrWhiteSpace(driverName))
                {
                    var drivers = _editor.ConfigEditor.DataDriversClasses.ToList();
                    if (drivers == null || drivers.Count == 0)
                    {
                        AnsiConsole.MarkupLine("[red]✗[/] No drivers configured in the system");
                        return;
                    }

                    var driverNames = drivers
                        .Select(d => $"{d.DatasourceType} - {d.PackageName}")
                        .ToArray();

                    var selected = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("[cyan]Select a driver to browse packages:[/]")
                            .PageSize(15)
                            .AddChoices(driverNames));

                    var index = Array.IndexOf(driverNames, selected);
                    driver = drivers[index];
                }
                else
                {
                    var foundDriver = FindDriverByNameOrType(driverName);
                    if (foundDriver == null)
                    {
                        AnsiConsole.MarkupLine($"[red]✗[/] Driver not found: {driverName}");
                        return;
                    }
                    driver = foundDriver;
                }

                AnsiConsole.MarkupLine($"\n[bold cyan]Browsing packages for {driver.DatasourceType}[/]");
                AnsiConsole.MarkupLine($"[dim]Package: {driver.PackageName}[/]");
                
                // Check installation status
                var isInstalled = _tracker!.IsInstalled(driver.DatasourceType);
                if (isInstalled)
                {
                    var info = _tracker.GetInfo(driver.DatasourceType);
                    AnsiConsole.MarkupLine($"[green]✓ Currently Installed:[/] {info?.PackageVersion ?? "Unknown version"}");
                    if (!string.IsNullOrEmpty(info?.SourcePath))
                    {
                        AnsiConsole.MarkupLine($"[dim]Installed from: {info.SourcePath}[/]");
                    }
                }
                else
                {
                    AnsiConsole.MarkupLine($"[yellow]⚠ Status:[/] Not installed");
                }

                // Check if NuggetSource is configured
                if (string.IsNullOrWhiteSpace(driver.NuggetSource))
                {
                    AnsiConsole.MarkupLine("\n[yellow]⚠[/] No NuGet source configured for this driver");
                    
                    var configureSource = AnsiConsole.Confirm("Would you like to configure a NuGet source now?");
                    if (!configureSource)
                    {
                        return;
                    }

                    var source = AnsiConsole.Ask<string>("[cyan]Enter NuGet source path or URL:[/]");
                    if (string.IsNullOrWhiteSpace(source))
                    {
                        AnsiConsole.MarkupLine("[red]✗[/] Source is required");
                        return;
                    }

                    driver.NuggetSource = source;
                    _editor.ConfigEditor.SaveDataconnectionsValues();
                    AnsiConsole.MarkupLine("[green]✓[/] Source configured");
                }

                AnsiConsole.MarkupLine($"[cyan]Source:[/] {driver.NuggetSource}\n");

                // Determine source type and browse
                if (Directory.Exists(driver.NuggetSource))
                {
                    BrowseDirectoryPackages(driver.NuggetSource, driver.PackageName, driver);
                }
                else if (File.Exists(driver.NuggetSource))
                {
                    BrowseFilePackage(driver.NuggetSource, driver.PackageName, driver);
                }
                else if (driver.NuggetSource.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    BrowseOnlinePackages(driver.NuggetSource, driver.PackageName);
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Invalid source: {driver.NuggetSource}");
                    AnsiConsole.MarkupLine("[dim]Source must be a valid directory path, file path, or URL[/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error browsing packages: {ex.Message}");
                AnsiConsole.WriteException(ex);
            }
        }

        private void BrowseDirectoryPackages(string directory, string packageName, ConnectionDriversConfig driver)
        {
            AnsiConsole.Status()
                .Start("Scanning directory for packages...", ctx =>
                {
                    try
                    {
                        // Search for packages matching the name
                        var searchPatterns = new[] 
                        { 
                            $"{packageName}*.dll",
                            $"{packageName}*.nupkg",
                            "*.dll"
                        };

                        var packages = new List<(string Path, string Name, long Size, DateTime Modified)>();

                        foreach (var pattern in searchPatterns)
                        {
                            var files = Directory.GetFiles(directory, pattern, SearchOption.AllDirectories);
                            foreach (var file in files)
                            {
                                var fileInfo = new FileInfo(file);
                                packages.Add((file, fileInfo.Name, fileInfo.Length, fileInfo.LastWriteTime));
                            }
                        }

                        if (packages.Count == 0)
                        {
                            AnsiConsole.MarkupLine($"[yellow]⚠[/] No packages found in: {directory}");
                            AnsiConsole.MarkupLine($"[dim]Searched for patterns: {string.Join(", ", searchPatterns)}[/]");
                            return;
                        }

                        // Remove duplicates
                        packages = packages.DistinctBy(p => p.Path).OrderBy(p => p.Name).ToList();

                        var table = new Table();
                        table.Border = TableBorder.Rounded;
                        table.Title = new TableTitle($"[bold cyan]Found {packages.Count} Package(s) in Directory[/]");
                        table.AddColumn("[cyan]#[/]");
                        table.AddColumn("[cyan]Package Name[/]");
                        table.AddColumn("[cyan]Size[/]");
                        table.AddColumn("[cyan]Modified[/]");
                        table.AddColumn("[cyan]Path[/]");

                        for (int i = 0; i < packages.Count; i++)
                        {
                            var pkg = packages[i];
                            var sizeStr = pkg.Size < 1024 * 1024 
                                ? $"{pkg.Size / 1024:N0} KB" 
                                : $"{pkg.Size / (1024.0 * 1024.0):N2} MB";

                            var nameMarkup = pkg.Name.Contains(packageName, StringComparison.OrdinalIgnoreCase)
                                ? $"[green]{pkg.Name}[/]"
                                : pkg.Name;

                            table.AddRow(
                                (i + 1).ToString(),
                                nameMarkup,
                                sizeStr,
                                pkg.Modified.ToString("yyyy-MM-dd HH:mm"),
                                pkg.Path.Length > 60 ? "..." + pkg.Path.Substring(pkg.Path.Length - 60) : pkg.Path
                            );
                        }

                        AnsiConsole.Write(table);

                        // Offer actions
                        var actions = new[] 
                        { 
                            "Install package now",
                            "View package details",
                            "Show install command",
                            "Exit"
                        };

                        var action = AnsiConsole.Prompt(
                            new SelectionPrompt<string>()
                                .Title("\n[cyan]What would you like to do?[/]")
                                .AddChoices(actions));

                        if (action == "Install package now")
                        {
                            var pkgChoices = packages.Select((p, i) => $"{i + 1}. {p.Name}").ToArray();
                            var selected = AnsiConsole.Prompt(
                                new SelectionPrompt<string>()
                                    .Title("[cyan]Select package to install:[/]")
                                    .PageSize(15)
                                    .AddChoices(pkgChoices));

                            var index = int.Parse(selected.Split('.')[0]) - 1;
                            var pkg = packages[index];

                            AnsiConsole.MarkupLine($"\n[cyan]Installing package:[/] {pkg.Name}");
                            AnsiConsole.MarkupLine($"[dim]Path: {pkg.Path}[/]");
                            
                            // Call the install method directly
                            InstallDriverFromFileWithDriver(pkg.Path, driver);
                        }
                        else if (action == "View package details")
                        {
                            var pkgChoices = packages.Select((p, i) => $"{i + 1}. {p.Name}").ToArray();
                            var selected = AnsiConsole.Prompt(
                                new SelectionPrompt<string>()
                                    .Title("[cyan]Select package to view:[/]")
                                    .PageSize(15)
                                    .AddChoices(pkgChoices));

                            var index = int.Parse(selected.Split('.')[0]) - 1;
                            var pkg = packages[index];

                            var panel = new Panel(new Markup(
                                $"[cyan]Name:[/] {pkg.Name}\n" +
                                $"[cyan]Path:[/] {pkg.Path}\n" +
                                $"[cyan]Size:[/] {(pkg.Size < 1024 * 1024 ? $"{pkg.Size / 1024:N0} KB" : $"{pkg.Size / (1024.0 * 1024.0):N2} MB")}\n" +
                                $"[cyan]Modified:[/] {pkg.Modified:F}\n" +
                                $"[cyan]Driver:[/] {driver.DatasourceType}\n" +
                                $"[cyan]Package:[/] {driver.PackageName}\n"
                            ));
                            panel.Header = new PanelHeader("[bold cyan]Package Details[/]");
                            panel.Border = BoxBorder.Rounded;
                            AnsiConsole.Write(panel);
                            
                            // Ask if they want to install after viewing
                            if (AnsiConsole.Confirm("\nInstall this package?"))
                            {
                                InstallDriverFromFileWithDriver(pkg.Path, driver);
                            }
                        }
                        else if (action == "Show install command")
                        {
                            var pkgChoices = packages.Select((p, i) => $"{i + 1}. {p.Name}").ToArray();
                            var selected = AnsiConsole.Prompt(
                                new SelectionPrompt<string>()
                                    .Title("[cyan]Select package:[/]")
                                    .PageSize(15)
                                    .AddChoices(pkgChoices));

                            var index = int.Parse(selected.Split('.')[0]) - 1;
                            var pkg = packages[index];

                            AnsiConsole.MarkupLine($"\n[cyan]Install command:[/]");
                            AnsiConsole.MarkupLine($"[yellow]driver install-from-file \"{pkg.Path}\" --type {driver.DatasourceType}[/]");
                            AnsiConsole.MarkupLine($"\n[dim]Or simply:[/]");
                            AnsiConsole.MarkupLine($"[yellow]driver install-from-file \"{pkg.Path}\"[/]");
                        }
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine($"[red]✗[/] Error scanning directory: {ex.Message}");
                    }
                });
        }

        private void BrowseFilePackage(string filePath, string packageName, ConnectionDriversConfig driver)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] File not found: {filePath}");
                    return;
                }

                var fileInfo = new FileInfo(filePath);
                
                // Check if already installed
                var isInstalled = _tracker!.IsInstalled(driver.DatasourceType);
                var statusMarkup = isInstalled ? "[green]✓ Installed[/]" : "[yellow]⚠ Not Installed[/]";
                
                var panel = new Panel(new Markup(
                    $"[cyan]Package Name:[/] {fileInfo.Name}\n" +
                    $"[cyan]Full Path:[/] {fileInfo.FullName}\n" +
                    $"[cyan]Size:[/] {(fileInfo.Length < 1024 * 1024 ? $"{fileInfo.Length / 1024:N0} KB" : $"{fileInfo.Length / (1024.0 * 1024.0):N2} MB")}\n" +
                    $"[cyan]Modified:[/] {fileInfo.LastWriteTime:F}\n" +
                    $"[cyan]Directory:[/] {fileInfo.DirectoryName}\n" +
                    $"[cyan]Driver:[/] {driver.DatasourceType}\n" +
                    $"[cyan]Status:[/] {statusMarkup}\n"
                ));
                panel.Header = new PanelHeader("[bold cyan]Package File Information[/]");
                panel.Border = BoxBorder.Rounded;
                
                AnsiConsole.Write(panel);

                if (isInstalled)
                {
                    var info = _tracker.GetInfo(driver.DatasourceType);
                    AnsiConsole.MarkupLine($"\n[dim]Currently installed from: {info?.SourcePath ?? "Unknown"}[/]");
                    
                    var reinstall = AnsiConsole.Confirm($"Would you like to reinstall/update from this package?");
                    if (reinstall)
                    {
                        InstallDriverFromFileWithDriver(fileInfo.FullName, driver);
                    }
                }
                else
                {
                    var install = AnsiConsole.Confirm($"Would you like to install this package?");
                    if (install)
                    {
                        InstallDriverFromFileWithDriver(fileInfo.FullName, driver);
                    }
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error reading file: {ex.Message}");
            }
        }

        private void BrowseOnlinePackages(string url, string packageName)
        {
            try
            {
                var panel = new Panel(new Markup(
                    $"[cyan]Package:[/] {packageName}\n" +
                    $"[cyan]NuGet Source:[/] {url}\n\n" +
                    $"[yellow]⚠ Online package browsing requires NuGet CLI integration[/]\n\n" +
                    $"[dim]To browse packages online:[/]\n" +
                    $"[dim]1. Visit: https://www.nuget.org/packages/{packageName}[/]\n" +
                    $"[dim]2. Or use NuGet CLI: nuget list {packageName} -Source {url}[/]\n" +
                    $"[dim]3. Download package and use: driver install-from-file <path>[/]\n\n" +
                    $"[dim]Common NuGet sources:[/]\n" +
                    $"[dim]• NuGet.org: https://api.nuget.org/v3/index.json[/]\n" +
                    $"[dim]• Local: C:\\Users\\{Environment.UserName}\\source\\repos\\LocalNugetFiles[/]\n"
                ));
                panel.Header = new PanelHeader("[bold cyan]Online Package Information[/]");
                panel.Border = BoxBorder.Rounded;
                
                AnsiConsole.Write(panel);

                var openBrowser = AnsiConsole.Confirm("Would you like guidance on finding this package online?");
                if (openBrowser)
                {
                    AnsiConsole.MarkupLine($"\n[cyan]Visit:[/] https://www.nuget.org/packages/{packageName}");
                    AnsiConsole.MarkupLine($"[dim]Search for different versions and dependencies[/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Determines if a source string is a package name rather than a file path or URL
        /// </summary>
        private bool IsPackageName(string source)
        {
            // Empty or whitespace - not a package name
            if (string.IsNullOrWhiteSpace(source))
                return false;
            
            // Contains path separators - likely a file path
            if (source.Contains('\\') || source.Contains('/'))
                return false;
            
            // Starts with http/https - it's a URL
            if (source.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                return false;
            
            // Already exists as file or directory - treat as path
            if (File.Exists(source) || Directory.Exists(source))
                return false;
            
            // If it looks like a typical package name pattern (e.g., System.Data.SQLite, Npgsql, MongoDB.Driver)
            // It should contain dots or be a simple word without special characters
            return true;
        }

        /// <summary>
        /// Searches NuGet.org for packages and lets user select one
        /// </summary>
        /// <param name="searchTerm">Search term (e.g., "Oracle", "MySQL", "PostgreSQL")</param>
        /// <returns>Selected package name, or null if cancelled</returns>
        private string? SearchAndSelectPackage(string searchTerm)
        {
            try
            {
                List<(string Id, string Description, long Downloads)>? packages = null;
                
                AnsiConsole.Status()
                    .Start($"Searching NuGet.org for '[cyan]{searchTerm}[/]'...", ctx =>
                    {
                        try
                        {
                            using var httpClient = new HttpClient();
                            httpClient.Timeout = TimeSpan.FromSeconds(15);
                            
                            // Use NuGet.org Search API v3
                            var searchUrl = $"https://azuresearch-usnc.nuget.org/query?q={Uri.EscapeDataString(searchTerm)}&take=20";
                            
                            var searchTask = httpClient.GetStringAsync(searchUrl);
                            searchTask.Wait();
                            var searchJson = System.Text.Json.JsonDocument.Parse(searchTask.Result);
                            
                            packages = searchJson.RootElement
                                .GetProperty("data")
                                .EnumerateArray()
                                .Select(pkg => (
                                    Id: pkg.GetProperty("id").GetString() ?? "",
                                    Description: pkg.TryGetProperty("description", out var desc) ? (desc.GetString() ?? "") : "",
                                    Downloads: pkg.TryGetProperty("totalDownloads", out var dl) ? dl.GetInt64() : 0
                                ))
                                .Where(p => !string.IsNullOrEmpty(p.Id))
                                .OrderByDescending(p => p.Downloads)
                                .ToList();
                            
                            ctx.Status($"Found {packages.Count} packages");
                        }
                        catch (Exception ex)
                        {
                            ctx.Status($"Search failed: {ex.Message}");
                        }
                    });
                
                if (packages == null || !packages.Any())
                {
                    AnsiConsole.MarkupLine($"[yellow]⚠[/] No packages found for '{searchTerm}'");
                    AnsiConsole.MarkupLine("[dim]Try a different search term or enter the package name manually[/]");
                    return null;
                }
                
                AnsiConsole.MarkupLine($"[green]✓[/] Found {packages.Count} package(s)\n");
                
                // Show packages with download counts
                var choices = packages
                    .Select(p => $"{p.Id} ({FormatDownloads(p.Downloads)} downloads)")
                    .ToList();
                choices.Add("Cancel");
                
                var selection = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title($"[cyan]Select a package for {searchTerm}:[/]")
                        .PageSize(15)
                        .MoreChoicesText("[grey](Move up and down to see more packages)[/]")
                        .AddChoices(choices));
                
                if (selection == "Cancel")
                {
                    return null;
                }
                
                // Extract package ID from selection (before the download count)
                var packageId = selection.Split(new[] { " (" }, StringSplitOptions.None)[0];
                
                // Show description of selected package
                var selectedPackage = packages.First(p => p.Id == packageId);
                if (!string.IsNullOrEmpty(selectedPackage.Description))
                {
                    AnsiConsole.MarkupLine($"\n[dim]{selectedPackage.Description}[/]\n");
                }
                
                return packageId;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[yellow]⚠[/] Search failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets the latest version of a package from NuGet.org
        /// </summary>
        /// <param name="packageName">Name of the NuGet package</param>
        /// <returns>Latest version string, or null if not found</returns>
        private string? GetLatestPackageVersion(string packageName)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(10);
                
                var serviceIndexUrl = "https://api.nuget.org/v3/index.json";
                var serviceIndexTask = httpClient.GetStringAsync(serviceIndexUrl);
                serviceIndexTask.Wait();
                var serviceIndexJson = System.Text.Json.JsonDocument.Parse(serviceIndexTask.Result);
                
                var packageBaseAddress = serviceIndexJson.RootElement
                    .GetProperty("resources")
                    .EnumerateArray()
                    .FirstOrDefault(r => r.GetProperty("@type").GetString() == "PackageBaseAddress/3.0.0")
                    .GetProperty("@id").GetString();
                
                if (string.IsNullOrEmpty(packageBaseAddress))
                {
                    return null;
                }
                
                var packageId = packageName.ToLowerInvariant();
                var versionsUrl = $"{packageBaseAddress}{packageId}/index.json";
                
                var versionsTask = httpClient.GetStringAsync(versionsUrl);
                versionsTask.Wait();
                var versionsJson = System.Text.Json.JsonDocument.Parse(versionsTask.Result);
                
                var latestVersion = versionsJson.RootElement
                    .GetProperty("versions")
                    .EnumerateArray()
                    .Select(v => v.GetString())
                    .Where(v => !string.IsNullOrEmpty(v))
                    .LastOrDefault(); // Last is latest
                
                return latestVersion;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Formats download count for display (e.g., 1.5M, 234K)
        /// </summary>
        private string FormatDownloads(long downloads)
        {
            if (downloads >= 1_000_000)
                return $"{downloads / 1_000_000.0:F1}M";
            if (downloads >= 1_000)
                return $"{downloads / 1_000.0:F1}K";
            return downloads.ToString();
        }

        /// <summary>
        /// Queries NuGet.org for available versions of a package and lets user select one
        /// </summary>
        /// <param name="packageName">Name of the NuGet package</param>
        /// <returns>Selected version string, null for latest, or "CANCELLED" if cancelled</returns>
        private string? SelectPackageVersion(string packageName)
        {
            try
            {
                AnsiConsole.Status()
                    .Start($"Searching NuGet.org for [cyan]{packageName}[/] versions...", ctx =>
                    {
                        // Query NuGet.org API v3 for package versions
                        using var httpClient = new HttpClient();
                        httpClient.Timeout = TimeSpan.FromSeconds(10);
                        
                        var serviceIndexUrl = "https://api.nuget.org/v3/index.json";
                        var serviceIndexTask = httpClient.GetStringAsync(serviceIndexUrl);
                        serviceIndexTask.Wait();
                        var serviceIndexJson = System.Text.Json.JsonDocument.Parse(serviceIndexTask.Result);
                        
                        // Find the package metadata resource
                        var packageBaseAddress = serviceIndexJson.RootElement
                            .GetProperty("resources")
                            .EnumerateArray()
                            .FirstOrDefault(r => r.GetProperty("@type").GetString() == "PackageBaseAddress/3.0.0")
                            .GetProperty("@id").GetString();
                        
                        if (string.IsNullOrEmpty(packageBaseAddress))
                        {
                            ctx.Status("Package metadata service not found");
                            return;
                        }
                        
                        // Query for package versions
                        var packageId = packageName.ToLowerInvariant();
                        var versionsUrl = $"{packageBaseAddress}{packageId}/index.json";
                        
                        var versionsTask = httpClient.GetStringAsync(versionsUrl);
                        versionsTask.Wait();
                        var versionsJson = System.Text.Json.JsonDocument.Parse(versionsTask.Result);
                        
                        var versions = versionsJson.RootElement
                            .GetProperty("versions")
                            .EnumerateArray()
                            .Select(v => v.GetString())
                            .Where(v => !string.IsNullOrEmpty(v))
                            .ToList();
                        
                        ctx.Status($"Found {versions.Count} versions");
                    });
                
                // Re-query to get versions (outside of Status context)
                using var httpClient2 = new HttpClient();
                httpClient2.Timeout = TimeSpan.FromSeconds(10);
                
                var serviceIndexUrl2 = "https://api.nuget.org/v3/index.json";
                var serviceIndexJson2 = System.Text.Json.JsonDocument.Parse(httpClient2.GetStringAsync(serviceIndexUrl2).Result);
                
                var packageBaseAddress2 = serviceIndexJson2.RootElement
                    .GetProperty("resources")
                    .EnumerateArray()
                    .FirstOrDefault(r => r.GetProperty("@type").GetString() == "PackageBaseAddress/3.0.0")
                    .GetProperty("@id").GetString();
                
                if (string.IsNullOrEmpty(packageBaseAddress2))
                {
                    AnsiConsole.MarkupLine("[yellow]⚠[/] Could not find package metadata service");
                    return null;
                }
                
                var packageId2 = packageName.ToLowerInvariant();
                var versionsUrl2 = $"{packageBaseAddress2}{packageId2}/index.json";
                var versionsJson2 = System.Text.Json.JsonDocument.Parse(httpClient2.GetStringAsync(versionsUrl2).Result);
                
                var versions2 = versionsJson2.RootElement
                    .GetProperty("versions")
                    .EnumerateArray()
                    .Select(v => v.GetString())
                    .Where(v => !string.IsNullOrEmpty(v))
                    .Reverse() // Show newest first
                    .ToList();
                
                if (!versions2.Any())
                {
                    AnsiConsole.MarkupLine($"[yellow]⚠[/] No versions found for {packageName}");
                    return null;
                }
                
                AnsiConsole.MarkupLine($"[green]✓[/] Found {versions2.Count} version(s) of {packageName}\n");
                
                // Add option to use latest
                var choices = new List<string> { "Latest (recommended)" };
                choices.AddRange(versions2.Where(v => v != null).Take(20)!); // Show top 20 versions
                choices.Add("Cancel");
                
                var selectedVersion = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title($"[cyan]Select version of {packageName} to install:[/]")
                        .PageSize(15)
                        .MoreChoicesText("[grey](Move up and down to see more versions)[/]")
                        .AddChoices(choices));
                
                if (selectedVersion == "Cancel")
                {
                    return "CANCELLED";
                }
                
                if (selectedVersion == "Latest (recommended)")
                {
                    return null; // null means use latest
                }
                
                return selectedVersion;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[yellow]⚠[/] Could not query NuGet.org: {ex.Message}");
                AnsiConsole.MarkupLine("[dim]Will use latest version...[/]");
                return null; // Fall back to latest
            }
        }

        /// <summary>
        /// Cleans the ConnectionDrivers folder by removing all driver DLLs
        /// </summary>
        private void CleanConnectionDrivers(bool force)
        {
            try
            {
                if (!Directory.Exists(_driversDirectory))
                {
                    AnsiConsole.MarkupLine("[yellow]ConnectionDrivers folder does not exist[/]");
                    return;
                }

                // Get all subdirectories (driver type folders)
                var driverFolders = Directory.GetDirectories(_driversDirectory);

                if (driverFolders.Length == 0)
                {
                    AnsiConsole.MarkupLine("[dim]ConnectionDrivers folder is already empty[/]");
                    return;
                }

                // Show what will be deleted
                AnsiConsole.MarkupLine($"\n[yellow]⚠[/] The following driver folders will be marked for deletion:");
                foreach (var folder in driverFolders)
                {
                    var folderName = Path.GetFileName(folder);
                    var dllCount = Directory.GetFiles(folder, "*.dll", SearchOption.AllDirectories).Length;
                    AnsiConsole.MarkupLine($"  [red]✗[/] {folderName} ({dllCount} DLL files)");
                }

                // Confirm deletion unless forced
                if (!force)
                {
                    AnsiConsole.MarkupLine("\n[yellow]Warning:[/] All drivers will be removed on next startup (before loading).");
                    AnsiConsole.MarkupLine("[cyan]→[/] You will need to restart BeepShell and reinstall drivers.");
                    var confirmed = AnsiConsole.Confirm("Mark all driver folders for deletion on next startup?", false);
                    
                    if (!confirmed)
                    {
                        AnsiConsole.MarkupLine("[dim]Clean cancelled[/]");
                        return;
                    }
                }

                // Create cleanup marker file for next startup
                var markerFile = Path.Combine(_editor.ConfigEditor.ExePath, ".cleanup_drivers");
                try
                {
                    // Write the list of folders to delete
                    File.WriteAllLines(markerFile, driverFolders);
                    
                    AnsiConsole.MarkupLine($"\n[green]✓[/] Driver folders marked for deletion");
                    AnsiConsole.MarkupLine($"[cyan]→[/] [yellow]Restart BeepShell[/] to complete cleanup");
                    AnsiConsole.MarkupLine($"[dim]   Folders will be deleted before any drivers are loaded[/]");
                    
                    // Clear the tracker now
                    _tracker?.Clear();
                    AnsiConsole.MarkupLine($"[green]✓[/] Driver installation tracker cleared");
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Error creating cleanup marker: {ex.Message}");
                    AnsiConsole.MarkupLine($"[yellow]Alternative:[/] Manually delete: {_driversDirectory}");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error marking drivers for cleanup: {ex.Message}");
            }
        }

        /// <summary>
        /// Executes pending driver cleanup (called on startup before loading assemblies)
        /// </summary>
        public static void ExecutePendingCleanup(string appPath)
        {
            var markerFile = Path.Combine(appPath, ".cleanup_drivers");
            
            if (!File.Exists(markerFile))
            {
                return; // No pending cleanup
            }

            try
            {
                AnsiConsole.MarkupLine("[yellow]⚠[/] Pending driver cleanup detected...");
                
                var foldersToDelete = File.ReadAllLines(markerFile)
                    .Where(f => !string.IsNullOrWhiteSpace(f))
                    .ToArray();

                if (foldersToDelete.Length == 0)
                {
                    File.Delete(markerFile);
                    return;
                }

                int successCount = 0;
                int failedCount = 0;

                foreach (var folder in foldersToDelete)
                {
                    if (!Directory.Exists(folder))
                    {
                        continue; // Already deleted
                    }

                    var folderName = Path.GetFileName(folder);
                    try
                    {
                        Directory.Delete(folder, recursive: true);
                        AnsiConsole.MarkupLine($"[green]✓[/] Deleted {folderName}");
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine($"[red]✗[/] Failed to delete {folderName}: {ex.Message}");
                        failedCount++;
                    }
                }

                // Remove marker file
                File.Delete(markerFile);
                
                // Delete installed_drivers.json tracker to reset installation state
                var trackerFile = Path.Combine(appPath, "installed_drivers.json");
                if (File.Exists(trackerFile))
                {
                    try
                    {
                        File.Delete(trackerFile);
                        AnsiConsole.MarkupLine("[green]✓[/] Cleared driver installation tracker");
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine($"[yellow]⚠[/] Could not delete tracker: {ex.Message}");
                    }
                }

                // Summary
                if (successCount > 0)
                {
                    AnsiConsole.MarkupLine($"[green]✓[/] Cleanup complete: Deleted {successCount} driver folder(s)");
                }
                
                if (failedCount > 0)
                {
                    AnsiConsole.MarkupLine($"[yellow]⚠[/] {failedCount} folder(s) could not be deleted");
                }

                if (successCount > 0)
                {
                    AnsiConsole.MarkupLine("[cyan]→[/] Use [yellow]driver install[/] to reinstall drivers");
                }
                
                AnsiConsole.WriteLine();
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error during cleanup: {ex.Message}");
                
                // Try to remove marker anyway to prevent infinite loops
                try { File.Delete(markerFile); } catch { }
            }
        }

        /// <summary>
        /// Verifies if a DLL or directory has runtime compatibility with BeepShell (net8.0)
        /// </summary>
        private bool VerifyRuntimeCompatibility(string path)
        {
            try
            {
                // Check if it's a directory containing DLLs
                if (Directory.Exists(path))
                {
                    var dirName = Path.GetFileName(path).ToLowerInvariant();
                    
                    // Check if directory name indicates framework version
                    if (dirName.Contains("net8.0") || dirName.Contains("net7.0") || 
                        dirName.Contains("net6.0") || dirName.Contains("netstandard2"))
                    {
                        return true; // Compatible framework
                    }
                    
                    // Check for .NET Framework versions (may have issues)
                    if (dirName.Contains("net4") || dirName.StartsWith("net4"))
                    {
                        return false; // Likely .NET Framework, not compatible
                    }
                    
                    // Try to load and check the first DLL in the directory
                    var dllFiles = Directory.GetFiles(path, "*.dll");
                    if (dllFiles.Any())
                    {
                        return VerifyDllCompatibility(dllFiles.First());
                    }
                }
                
                // Check if it's a DLL file
                if (File.Exists(path) && path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    return VerifyDllCompatibility(path);
                }
                
                return true; // Assume compatible if we can't determine
            }
            catch
            {
                return true; // Don't block installation on verification errors
            }
        }

        /// <summary>
        /// Verifies if a specific DLL is compatible with the current runtime
        /// </summary>
        private bool VerifyDllCompatibility(string dllPath)
        {
            try
            {
                // Try to load assembly name to check basic compatibility
                var assemblyName = System.Reflection.AssemblyName.GetAssemblyName(dllPath);
                
                // If we can get the assembly name, it's likely compatible
                // The actual loading into runtime happens via LoadNugget
                return true;
            }
            catch (BadImageFormatException)
            {
                // Not a valid .NET assembly or wrong platform (x86/x64/AnyCPU mismatch)
                return false;
            }
            catch (FileLoadException)
            {
                // Assembly or one of its dependencies was found but could not be loaded
                return false;
            }
            catch
            {
                // Other errors - assume incompatible
                return false;
            }
        }

        public string[] GetExamples()
        {
            return new[]
            {
                "driver list",
                "driver list --category RDBMS",
                "driver list --missing-only",
                "driver info SqlServer",
                "driver test PostgreSQL",
                "driver init",
                "driver init --overwrite",
                "driver browse",
                "driver browse --name SqlServer",
                "driver install",
                "driver install --from-nuget System.Data.SQLite",
                "driver install-from-file C:\\packages\\MongoDB.Driver.dll",
                "driver install-from-file C:\\packages\\drivers --type SqlServer",
                "driver install-missing",
                "driver install-missing --auto",
                "driver update",
                "driver update --name SqlServer",
                "driver remove",
                "driver remove --name MongoDB",
                "driver clean",
                "driver clean --force",
                "driver check"
            };
        }
    }
}
