using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using BeepShell.Infrastructure;
using Spectre.Console;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;

namespace BeepShell.Commands
{
    /// <summary>
    /// Shell commands for managing NuGet packages
    /// </summary>
    public class NuGetShellCommands : IShellCommand
    {
        private IDMEEditor _editor = null!;

        public string CommandName => "nuget";
        public string Description => "Manage NuGet packages (load, unload, list, install)";
        public string Category => "Package Management";
        public string Version => "1.0.0";
        public string Author => "BeepDM Team";
        public string[] Aliases => new[] { "nugget", "pkg", "package" };

        public void Initialize(IDMEEditor editor)
        {
            _editor = editor;
        }

        public Command BuildCommand()
        {
            var cmd = new Command("nuget", "Manage NuGet packages");

            // nuget list - List all loaded nuggets
            var listCmd = new Command("list", "List loaded NuGet packages");
            var showDetailsOpt = new Option<bool>(new[] { "--details", "-d" }, "Show detailed information");
            var filterOpt = new Option<string>(new[] { "--filter", "-f" }, "Filter by name pattern");
            listCmd.AddOption(showDetailsOpt);
            listCmd.AddOption(filterOpt);
            listCmd.SetHandler((details, filter) => ListNuggets(details, filter), showDetailsOpt, filterOpt);
            cmd.AddCommand(listCmd);

            // nuget install - Interactive install
            var installCmd = new Command("install", "Install/load a NuGet package interactively");
            installCmd.SetHandler(() => InstallNuggetInteractive());
            cmd.AddCommand(installCmd);

            // nuget install-from-file - Install from file system
            var installFileCmd = new Command("install-from-file", "Install a NuGet package from file path");
            var filePathArg = new Argument<string>("path", "Path to the NuGet package, DLL, or directory");
            installFileCmd.AddArgument(filePathArg);
            installFileCmd.SetHandler((path) => InstallFromFile(path), filePathArg);
            cmd.AddCommand(installFileCmd);

            // nuget install-from-directory - Discover and install from directory
            var installDirCmd = new Command("install-from-directory", "Discover and install packages from directory");
            var dirPathArg = new Argument<string>("directory", () => Environment.CurrentDirectory, "Directory to scan");
            var recursiveOpt = new Option<bool>(new[] { "--recursive", "-r" }, "Search recursively");
            var autoOpt = new Option<bool>(new[] { "--auto", "-a" }, "Auto-install all found packages");
            installDirCmd.AddArgument(dirPathArg);
            installDirCmd.AddOption(recursiveOpt);
            installDirCmd.AddOption(autoOpt);
            installDirCmd.SetHandler((dir, recursive, auto) => InstallFromDirectory(dir, recursive, auto), 
                dirPathArg, recursiveOpt, autoOpt);
            cmd.AddCommand(installDirCmd);

            // nuget update - Update a loaded package
            var updateCmd = new Command("update", "Update/reload a NuGet package");
            var updateNameOpt = new Option<string>(new[] { "--name", "-n" }, "Package name to update");
            updateCmd.AddOption(updateNameOpt);
            updateCmd.SetHandler((name) => UpdateNuggetInteractive(name), updateNameOpt);
            cmd.AddCommand(updateCmd);

            // nuget remove - Remove/unload a package
            var removeCmd = new Command("remove", "Remove/unload a NuGet package");
            var removeNameOpt = new Option<string>(new[] { "--name", "-n" }, "Package name to remove");
            removeCmd.AddOption(removeNameOpt);
            removeCmd.SetHandler((name) => RemoveNuggetInteractive(name), removeNameOpt);
            cmd.AddCommand(removeCmd);

            // nuget info <name> - Get information about a loaded nugget
            var infoCmd = new Command("info", "Get information about a loaded NuGet package");
            var infoNameArg = new Argument<string>("name", "Name of the NuGet package");
            infoCmd.AddArgument(infoNameArg);
            infoCmd.SetHandler((string name) => ShowNuggetInfo(name), infoNameArg);
            cmd.AddCommand(infoCmd);

            // nuget search <directory> - Search for NuGet packages in directory
            var searchCmd = new Command("search", "Search for NuGet packages in directory");
            var searchDirArg = new Argument<string>("directory", () => Environment.CurrentDirectory, "Directory to search");
            var searchRecursiveOpt = new Option<bool>(new[] { "--recursive", "-r" }, "Search recursively");
            searchCmd.AddArgument(searchDirArg);
            searchCmd.AddOption(searchRecursiveOpt);
            searchCmd.SetHandler((string directory, bool recursive) => SearchNuggets(directory, recursive), 
                searchDirArg, searchRecursiveOpt);
            cmd.AddCommand(searchCmd);

            // nuget validate <path> - Validate a NuGet package before loading
            var validateCmd = new Command("validate", "Validate a NuGet package");
            var validatePathArg = new Argument<string>("path", "Path to the NuGet package");
            validateCmd.AddArgument(validatePathArg);
            validateCmd.SetHandler((string path) => ValidateNugget(path), validatePathArg);
            cmd.AddCommand(validateCmd);

            // nuget status - Show status overview
            var statusCmd = new Command("status", "Show NuGet packages status overview");
            statusCmd.SetHandler(() => ShowStatus());
            cmd.AddCommand(statusCmd);

            return cmd;
        }

        private void ListNuggets(bool showDetails = false, string? filter = null)
        {
            try
            {
                var assemblies = _editor.assemblyHandler.LoadedAssemblies;

                if (assemblies == null || assemblies.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]No NuGet packages loaded[/]");
                    return;
                }

                // Apply filter if provided
                var filteredAssemblies = assemblies.AsEnumerable();
                if (!string.IsNullOrWhiteSpace(filter))
                {
                    filteredAssemblies = assemblies.Where(a => 
                        a.GetName().Name?.Contains(filter, StringComparison.OrdinalIgnoreCase) == true);
                }

                var assemblyList = filteredAssemblies.ToList();

                if (assemblyList.Count == 0)
                {
                    AnsiConsole.MarkupLine($"[yellow]No packages found matching filter: {filter}[/]");
                    return;
                }

                var table = new Table();
                table.Border = TableBorder.Rounded;
                table.Title = new TableTitle($"[bold cyan]Loaded NuGet Packages ({assemblyList.Count})[/]");
                table.AddColumn("[cyan]Name[/]");
                table.AddColumn("[cyan]Version[/]");
                
                if (showDetails)
                {
                    table.AddColumn("[cyan]Location[/]");
                    table.AddColumn("[cyan]Types[/]");
                    table.AddColumn("[cyan]Culture[/]");
                }

                foreach (var asm in assemblyList)
                {
                    var name = asm.GetName();
                    var location = string.IsNullOrEmpty(asm.Location) ? "[dim]In Memory[/]" : Path.GetFileName(asm.Location);
                    var typeCount = 0;
                    
                    try
                    {
                        typeCount = asm.GetTypes().Length;
                    }
                    catch { }

                    if (showDetails)
                    {
                        table.AddRow(
                            name.Name ?? "[dim]Unknown[/]",
                            name.Version?.ToString() ?? "[dim]N/A[/]",
                            location,
                            typeCount.ToString(),
                            name.CultureName ?? "Neutral"
                        );
                    }
                    else
                    {
                        table.AddRow(
                            name.Name ?? "[dim]Unknown[/]",
                            name.Version?.ToString() ?? "[dim]N/A[/]"
                        );
                    }
                }

                AnsiConsole.Write(table);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error listing NuGet packages: {ex.Message}");
            }
        }

        private void InstallNuggetInteractive()
        {
            try
            {
                var installMethod = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[cyan]How would you like to install the package?[/]")
                        .AddChoices(new[]
                        {
                            "From File (Browse for DLL or directory)",
                            "From Directory (Scan and choose)",
                            "Cancel"
                        }));

                if (installMethod == "Cancel")
                {
                    AnsiConsole.MarkupLine("[yellow]Installation cancelled[/]");
                    return;
                }

                if (installMethod.StartsWith("From File"))
                {
                    var path = AnsiConsole.Ask<string>("[cyan]Enter path to DLL or directory:[/]");
                    InstallFromFile(path);
                }
                else if (installMethod.StartsWith("From Directory"))
                {
                    var directory = AnsiConsole.Ask<string>(
                        "[cyan]Enter directory to scan:[/]", 
                        Environment.CurrentDirectory);
                    
                    var recursive = AnsiConsole.Confirm("Search recursively?", true);
                    
                    InstallFromDirectory(directory, recursive, false);
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
            }
        }

        private void InstallFromFile(string path)
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

                AnsiConsole.Status()
                    .Start($"Installing package from {Path.GetFileName(path)}...", ctx =>
                    {
                        var success = _editor.assemblyHandler.LoadNugget(path);

                        if (success)
                        {
                            AnsiConsole.MarkupLine($"[green]✓[/] Package installed successfully: {Path.GetFileName(path)}");
                            
                            // Try to get assembly info
                            var fileName = Path.GetFileNameWithoutExtension(path);
                            var loadedAsm = _editor.assemblyHandler.LoadedAssemblies
                                .FirstOrDefault(a => a.GetName().Name?.Equals(fileName, StringComparison.OrdinalIgnoreCase) == true);
                            
                            if (loadedAsm != null)
                            {
                                var typeCount = 0;
                                try { typeCount = loadedAsm.GetTypes().Length; } catch { }
                                AnsiConsole.MarkupLine($"[dim]  Version: {loadedAsm.GetName().Version}[/]");
                                AnsiConsole.MarkupLine($"[dim]  Types: {typeCount}[/]");
                            }
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[red]✗[/] Failed to install package");
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error installing package: {ex.Message}");
            }
        }

        private void InstallFromDirectory(string directory, bool recursive, bool auto)
        {
            try
            {
                if (!Directory.Exists(directory))
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Directory not found: {directory}");
                    return;
                }

                var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                var dllFiles = Directory.GetFiles(directory, "*.dll", searchOption);
                var nupkgFiles = Directory.GetFiles(directory, "*.nupkg", searchOption);
                
                var allFiles = dllFiles.Concat(nupkgFiles).ToList();

                if (allFiles.Count == 0)
                {
                    AnsiConsole.MarkupLine($"[yellow]No packages found in {directory}[/]");
                    return;
                }

                AnsiConsole.MarkupLine($"[cyan]Found {allFiles.Count} package(s)[/]");

                List<string> filesToInstall;

                if (auto)
                {
                    filesToInstall = allFiles;
                    AnsiConsole.MarkupLine("[yellow]Auto-installing all packages...[/]");
                }
                else
                {
                    // Show selection prompt
                    filesToInstall = AnsiConsole.Prompt(
                        new MultiSelectionPrompt<string>()
                            .Title("[cyan]Select packages to install:[/]")
                            .PageSize(15)
                            .MoreChoicesText("[grey](Move up and down to see more packages)[/]")
                            .InstructionsText("[grey](Press [blue]<space>[/] to toggle, [green]<enter>[/] to confirm)[/]")
                            .AddChoices(allFiles)
                            .UseConverter(f => $"{Path.GetFileName(f)} ({new FileInfo(f).Length / 1024:N0} KB)"));
                }

                if (!filesToInstall.Any())
                {
                    AnsiConsole.MarkupLine("[yellow]No packages selected[/]");
                    return;
                }

                int successCount = 0;
                int failCount = 0;

                foreach (var file in filesToInstall)
                {
                    try
                    {
                        var success = _editor.assemblyHandler.LoadNugget(file);
                        if (success)
                        {
                            successCount++;
                            AnsiConsole.MarkupLine($"[green]✓[/] Installed: {Path.GetFileName(file)}");
                        }
                        else
                        {
                            failCount++;
                            AnsiConsole.MarkupLine($"[red]✗[/] Failed: {Path.GetFileName(file)}");
                        }
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        AnsiConsole.MarkupLine($"[red]✗[/] Error: {Path.GetFileName(file)} - {ex.Message}");
                    }
                }

                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"[bold]Results:[/] [green]{successCount} succeeded[/], [red]{failCount} failed[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
            }
        }

        private void UpdateNuggetInteractive(string packageName)
        {
            try
            {
                System.Reflection.Assembly? targetAssembly = null;

                if (string.IsNullOrWhiteSpace(packageName))
                {
                    // Show selection list of loaded packages
                    var loadedPackages = _editor.assemblyHandler.LoadedAssemblies.ToList();

                    if (!loadedPackages.Any())
                    {
                        AnsiConsole.MarkupLine("[yellow]⚠[/] No packages loaded to update");
                        return;
                    }

                    targetAssembly = AnsiConsole.Prompt(
                        new SelectionPrompt<System.Reflection.Assembly>()
                            .Title("[cyan]Select a package to update:[/]")
                            .PageSize(15)
                            .MoreChoicesText("[grey](Move up and down to see more packages)[/]")
                            .AddChoices(loadedPackages)
                            .UseConverter(a => $"{a.GetName().Name} v{a.GetName().Version}"));
                }
                else
                {
                    // Find package by name
                    targetAssembly = _editor.assemblyHandler.LoadedAssemblies
                        .FirstOrDefault(a => a.GetName().Name?.Equals(packageName, StringComparison.OrdinalIgnoreCase) == true);

                    if (targetAssembly == null)
                    {
                        AnsiConsole.MarkupLine($"[yellow]⚠[/] Package not found: {packageName}");
                        return;
                    }
                }

                var asmName = targetAssembly.GetName().Name;
                var location = targetAssembly.Location;

                if (string.IsNullOrEmpty(location))
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Cannot update in-memory assembly");
                    return;
                }

                // Confirm update
                if (!AnsiConsole.Confirm($"[yellow]Update package '{asmName}'?[/]"))
                {
                    AnsiConsole.MarkupLine("[yellow]Update cancelled[/]");
                    return;
                }

                // Ask for new source
                var updateMethod = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[cyan]Update from:[/]")
                        .AddChoices(new[]
                        {
                            "Reload from current location",
                            "Browse for new file",
                            "Cancel"
                        }));

                if (updateMethod == "Cancel")
                {
                    AnsiConsole.MarkupLine("[yellow]Update cancelled[/]");
                    return;
                }

                AnsiConsole.Status()
                    .Start($"Updating package '{asmName}'...", ctx =>
                    {
                        ctx.Status("Unloading current version...");
                        var unloaded = _editor.assemblyHandler.UnloadNugget(asmName);

                        if (!unloaded)
                        {
                            AnsiConsole.MarkupLine($"[yellow]⚠[/] Could not unload current version (will attempt to reload)");
                        }

                        System.Threading.Thread.Sleep(500); // Brief pause

                        string newPath = location;
                        if (updateMethod.StartsWith("Browse"))
                        {
                            newPath = AnsiConsole.Ask<string>("[cyan]Enter path to new package:[/]", location);
                        }

                        ctx.Status("Loading new version...");
                        var loaded = _editor.assemblyHandler.LoadNugget(newPath);

                        if (loaded)
                        {
                            AnsiConsole.MarkupLine($"[green]✓[/] Package updated successfully: {asmName}");
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[red]✗[/] Failed to load updated package");
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
            }
        }

        private void RemoveNuggetInteractive(string packageName)
        {
            try
            {
                System.Reflection.Assembly? targetAssembly = null;

                if (string.IsNullOrWhiteSpace(packageName))
                {
                    // Show selection list of loaded packages
                    var loadedPackages = _editor.assemblyHandler.LoadedAssemblies.ToList();

                    if (!loadedPackages.Any())
                    {
                        AnsiConsole.MarkupLine("[yellow]⚠[/] No packages loaded to remove");
                        return;
                    }

                    targetAssembly = AnsiConsole.Prompt(
                        new SelectionPrompt<System.Reflection.Assembly>()
                            .Title("[cyan]Select a package to remove:[/]")
                            .PageSize(15)
                            .MoreChoicesText("[grey](Move up and down to see more packages)[/]")
                            .AddChoices(loadedPackages)
                            .UseConverter(a => $"{a.GetName().Name} v{a.GetName().Version}"));
                }
                else
                {
                    // Find package by name
                    targetAssembly = _editor.assemblyHandler.LoadedAssemblies
                        .FirstOrDefault(a => a.GetName().Name?.Equals(packageName, StringComparison.OrdinalIgnoreCase) == true);

                    if (targetAssembly == null)
                    {
                        AnsiConsole.MarkupLine($"[yellow]⚠[/] Package not found: {packageName}");
                        return;
                    }
                }

                var asmName = targetAssembly.GetName().Name;

                // Confirm removal
                if (!AnsiConsole.Confirm($"[red]Are you sure you want to remove '{asmName}'?[/]"))
                {
                    AnsiConsole.MarkupLine("[yellow]Removal cancelled[/]");
                    return;
                }

                AnsiConsole.Status()
                    .Start($"Removing package '{asmName}'...", ctx =>
                    {
                        var success = _editor.assemblyHandler.UnloadNugget(asmName);

                        if (success)
                        {
                            AnsiConsole.MarkupLine($"[green]✓[/] Package removed successfully: {asmName}");
                            AnsiConsole.MarkupLine("[dim]Note: The assembly may still be loaded in memory until application restart[/]");
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[yellow]⚠[/] Package not found or cannot be removed: {asmName}");
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
            }
        }

        private void ShowStatus()
        {
            try
            {
                var assemblies = _editor.assemblyHandler.LoadedAssemblies?.ToList() ?? new List<System.Reflection.Assembly>();
                
                var totalSize = 0L;
                var inMemoryCount = 0;
                var fileBasedCount = 0;

                foreach (var asm in assemblies)
                {
                    if (string.IsNullOrEmpty(asm.Location))
                    {
                        inMemoryCount++;
                    }
                    else
                    {
                        fileBasedCount++;
                        try
                        {
                            var fileInfo = new FileInfo(asm.Location);
                            totalSize += fileInfo.Length;
                        }
                        catch { }
                    }
                }

                var panel = new Panel(new Markup(
                    $"[cyan]Total Packages:[/] {assemblies.Count}\n" +
                    $"[green]File-Based:[/] {fileBasedCount}\n" +
                    $"[yellow]In-Memory:[/] {inMemoryCount}\n" +
                    $"[blue]Total Size:[/] {totalSize / (1024 * 1024):N2} MB"
                ));
                panel.Header = new PanelHeader("[bold yellow]NuGet Packages Status[/]");
                panel.Border = BoxBorder.Rounded;

                AnsiConsole.Write(panel);

                if (assemblies.Any())
                {
                    if (AnsiConsole.Confirm("\nWould you like to see the package list?"))
                    {
                        ListNuggets(false, string.Empty);
                    }
                }
                else
                {
                    AnsiConsole.MarkupLine("\n[yellow]⚠ No packages currently loaded[/]");
                    if (AnsiConsole.Confirm("Would you like to install packages now?"))
                    {
                        InstallNuggetInteractive();
                    }
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
            }
        }

        private void SearchNuggets(string directory, bool recursive)
        {
            try
            {
                if (!Directory.Exists(directory))
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Directory not found: {directory}");
                    return;
                }

                var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                var dllFiles = Directory.GetFiles(directory, "*.dll", searchOption);
                var nupkgFiles = Directory.GetFiles(directory, "*.nupkg", searchOption);
                
                var allFiles = dllFiles.Concat(nupkgFiles).ToList();

                if (allFiles.Count == 0)
                {
                    AnsiConsole.MarkupLine($"[yellow]No NuGet packages or DLLs found in {directory}[/]");
                    return;
                }

                var table = new Table();
                table.Border = TableBorder.Rounded;
                table.Title = new TableTitle($"[bold cyan]Found Packages ({allFiles.Count})[/]");
                table.AddColumn("[cyan]Name[/]");
                table.AddColumn("[cyan]Type[/]");
                table.AddColumn("[cyan]Size[/]");
                table.AddColumn("[cyan]Path[/]");

                foreach (var file in allFiles.OrderBy(f => Path.GetFileName(f)))
                {
                    var fileInfo = new FileInfo(file);
                    var fileType = Path.GetExtension(file).TrimStart('.').ToUpper();
                    var size = fileInfo.Length < 1024 * 1024 
                        ? $"{fileInfo.Length / 1024:N0} KB" 
                        : $"{fileInfo.Length / (1024 * 1024):N2} MB";

                    table.AddRow(
                        Path.GetFileName(file),
                        fileType,
                        size,
                        file.Length > 60 ? "..." + file.Substring(file.Length - 60) : file
                    );
                }

                AnsiConsole.Write(table);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error searching for NuGet packages: {ex.Message}");
            }
        }

        private void ShowNuggetInfo(string name)
        {
            try
            {
                var assembly = _editor.assemblyHandler.LoadedAssemblies
                    .FirstOrDefault(a => a.GetName().Name?.Equals(name, StringComparison.OrdinalIgnoreCase) == true);

                if (assembly == null)
                {
                    AnsiConsole.MarkupLine($"[yellow]NuGet package not found: {name}[/]");
                    return;
                }

                var asmName = assembly.GetName();
                var panel = new Panel(new Markup(
                    $"[cyan]Name:[/] {asmName.Name}\n" +
                    $"[cyan]Version:[/] {asmName.Version}\n" +
                    $"[cyan]Culture:[/] {asmName.CultureName ?? "Neutral"}\n" +
                    $"[cyan]Location:[/] {(string.IsNullOrEmpty(assembly.Location) ? "In Memory" : assembly.Location)}\n" +
                    $"[cyan]Full Name:[/] {asmName.FullName}"
                ));
                panel.Header = new PanelHeader($"[bold green]NuGet Package: {name}[/]");
                panel.Border = BoxBorder.Rounded;
                AnsiConsole.Write(panel);

                // List types
                try
                {
                    var types = assembly.GetTypes().Take(50).ToList();
                    
                    if (types.Count > 0)
                    {
                        AnsiConsole.WriteLine();
                        var typeTable = new Table();
                        typeTable.Border = TableBorder.Rounded;
                        typeTable.Title = new TableTitle($"[cyan]Exported Types (showing {types.Count})[/]");
                        typeTable.AddColumn("Type Name");
                        typeTable.AddColumn("Namespace");

                        foreach (var type in types.OrderBy(t => t.FullName))
                        {
                            typeTable.AddRow(
                                type.Name,
                                type.Namespace ?? "[dim]N/A[/]"
                            );
                        }

                        AnsiConsole.Write(typeTable);
                    }
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[yellow]⚠[/] Could not load types: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error showing NuGet package info: {ex.Message}");
            }
        }

        private void ValidateNugget(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] File not found: {path}");
                    return;
                }

                AnsiConsole.Status()
                    .Start($"Validating {Path.GetFileName(path)}...", ctx =>
                    {
                        var validations = new List<(string check, bool passed, string message)>();

                        // Check file extension
                        var ext = Path.GetExtension(path).ToLower();
                        validations.Add(("File Extension", 
                            ext == ".dll" || ext == ".nupkg", 
                            ext == ".dll" || ext == ".nupkg" ? "Valid assembly file" : "Invalid file type"));

                        // Check file size
                        var fileInfo = new FileInfo(path);
                        validations.Add(("File Size", 
                            fileInfo.Length > 0, 
                            $"{fileInfo.Length / 1024:N0} KB"));

                        // Try to load assembly metadata
                        try
                        {
                            var asmName = System.Reflection.AssemblyName.GetAssemblyName(path);
                            validations.Add(("Assembly Metadata", 
                                true, 
                                $"{asmName.Name} v{asmName.Version}"));
                        }
                        catch (Exception ex)
                        {
                            validations.Add(("Assembly Metadata", 
                                false, 
                                ex.Message));
                        }

                        // Display results
                        AnsiConsole.WriteLine();
                        var table = new Table();
                        table.Border = TableBorder.Rounded;
                        table.AddColumn("Validation Check");
                        table.AddColumn("Result");
                        table.AddColumn("Details");

                        foreach (var (check, passed, message) in validations)
                        {
                            table.AddRow(
                                check,
                                passed ? "[green]✓ Pass[/]" : "[red]✗ Fail[/]",
                                message
                            );
                        }

                        AnsiConsole.Write(table);

                        var allPassed = validations.All(v => v.passed);
                        if (allPassed)
                        {
                            AnsiConsole.MarkupLine("\n[green]✓[/] Package validation passed");
                        }
                        else
                        {
                            AnsiConsole.MarkupLine("\n[red]✗[/] Package validation failed");
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error validating NuGet package: {ex.Message}");
            }
        }

        public bool CanExecute() => _editor != null;

        public string[] GetExamples()
        {
            return new[]
            {
                "nuget list",
                "nuget list --details",
                "nuget list --filter System",
                "nuget install",
                "nuget install-from-file C:\\packages\\MyPackage.dll",
                "nuget install-from-directory C:\\packages --recursive",
                "nuget install-from-directory C:\\packages --auto",
                "nuget update",
                "nuget update --name MyPackage",
                "nuget remove",
                "nuget remove --name MyPackage",
                "nuget info MyPackage",
                "nuget search C:\\packages --recursive",
                "nuget validate C:\\packages\\MyPackage.dll",
                "nuget status"
            };
        }
    }
}
