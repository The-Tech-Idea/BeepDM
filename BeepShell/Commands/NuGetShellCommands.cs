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
        private IDMEEditor _editor;

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
            listCmd.SetHandler(() => ListNuggets());
            cmd.AddCommand(listCmd);

            // nuget load <path> - Load a NuGet package
            var loadCmd = new Command("load", "Load a NuGet package from path");
            var pathArg = new Argument<string>("path", "Path to the NuGet package or assembly");
            loadCmd.AddArgument(pathArg);
            loadCmd.SetHandler((string path) => LoadNugget(path), pathArg);
            cmd.AddCommand(loadCmd);

            // nuget unload <name> - Unload a NuGet package
            var unloadCmd = new Command("unload", "Unload a NuGet package");
            var nameArg = new Argument<string>("name", "Name of the NuGet package to unload");
            unloadCmd.AddArgument(nameArg);
            unloadCmd.SetHandler((string name) => UnloadNugget(name), nameArg);
            cmd.AddCommand(unloadCmd);

            // nuget search <directory> - Search for NuGet packages in directory
            var searchCmd = new Command("search", "Search for NuGet packages in directory");
            var dirArg = new Argument<string>("directory", () => Environment.CurrentDirectory, "Directory to search");
            var recursiveOpt = new Option<bool>(new[] { "--recursive", "-r" }, "Search recursively");
            searchCmd.AddArgument(dirArg);
            searchCmd.AddOption(recursiveOpt);
            searchCmd.SetHandler((string directory, bool recursive) => SearchNuggets(directory, recursive), dirArg, recursiveOpt);
            cmd.AddCommand(searchCmd);

            // nuget info <name> - Get information about a loaded nugget
            var infoCmd = new Command("info", "Get information about a loaded NuGet package");
            var infoNameArg = new Argument<string>("name", "Name of the NuGet package");
            infoCmd.AddArgument(infoNameArg);
            infoCmd.SetHandler((string name) => ShowNuggetInfo(name), infoNameArg);
            cmd.AddCommand(infoCmd);

            // nuget reload <name> - Reload a NuGet package
            var reloadCmd = new Command("reload", "Reload a NuGet package");
            var reloadNameArg = new Argument<string>("name", "Name of the NuGet package to reload");
            reloadCmd.AddArgument(reloadNameArg);
            reloadCmd.SetHandler((string name) => ReloadNugget(name), reloadNameArg);
            cmd.AddCommand(reloadCmd);

            // nuget validate <path> - Validate a NuGet package before loading
            var validateCmd = new Command("validate", "Validate a NuGet package");
            var validatePathArg = new Argument<string>("path", "Path to the NuGet package");
            validateCmd.AddArgument(validatePathArg);
            validateCmd.SetHandler((string path) => ValidateNugget(path), validatePathArg);
            cmd.AddCommand(validateCmd);

            return cmd;
        }

        private void ListNuggets()
        {
            try
            {
                var assemblies = _editor.assemblyHandler.LoadedAssemblies;

                if (assemblies == null || assemblies.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]No NuGet packages loaded[/]");
                    return;
                }

                var table = new Table();
                table.Border = TableBorder.Rounded;
                table.Title = new TableTitle($"[bold cyan]Loaded NuGet Packages ({assemblies.Count})[/]");
                table.AddColumn("[cyan]Name[/]");
                table.AddColumn("[cyan]Version[/]");
                table.AddColumn("[cyan]Location[/]");
                table.AddColumn("[cyan]Types[/]");

                foreach (var asm in assemblies)
                {
                    var name = asm.GetName();
                    var location = string.IsNullOrEmpty(asm.Location) ? "[dim]In Memory[/]" : Path.GetFileName(asm.Location);
                    var typeCount = 0;
                    
                    try
                    {
                        typeCount = asm.GetTypes().Length;
                    }
                    catch { }

                    table.AddRow(
                        name.Name ?? "[dim]Unknown[/]",
                        name.Version?.ToString() ?? "[dim]N/A[/]",
                        location,
                        typeCount.ToString()
                    );
                }

                AnsiConsole.Write(table);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error listing NuGet packages: {ex.Message}");
            }
        }

        private void LoadNugget(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    AnsiConsole.MarkupLine("[red]✗[/] Path is required");
                    return;
                }

                if (!File.Exists(path))
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] File not found: {path}");
                    return;
                }

                AnsiConsole.Status()
                    .Start($"Loading NuGet package from {Path.GetFileName(path)}...", ctx =>
                    {
                        var success = _editor.assemblyHandler.LoadNugget(path);

                        if (success)
                        {
                            AnsiConsole.MarkupLine($"[green]✓[/] NuGet package loaded: {Path.GetFileName(path)}");
                            
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
                            AnsiConsole.MarkupLine($"[red]✗[/] Failed to load NuGet package");
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error loading NuGet package: {ex.Message}");
            }
        }

        private void UnloadNugget(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    AnsiConsole.MarkupLine("[red]✗[/] Package name is required");
                    return;
                }

                var confirm = AnsiConsole.Confirm($"Are you sure you want to unload '{name}'?");
                if (!confirm)
                {
                    AnsiConsole.MarkupLine("[yellow]Cancelled[/]");
                    return;
                }

                AnsiConsole.Status()
                    .Start($"Unloading NuGet package '{name}'...", ctx =>
                    {
                        var success = _editor.assemblyHandler.UnloadNugget(name);

                        if (success)
                        {
                            AnsiConsole.MarkupLine($"[green]✓[/] NuGet package unloaded: {name}");
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[yellow]⚠[/] NuGet package not found or cannot be unloaded: {name}");
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error unloading NuGet package: {ex.Message}");
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

        private void ReloadNugget(string name)
        {
            try
            {
                // Find the assembly
                var assembly = _editor.assemblyHandler.LoadedAssemblies
                    .FirstOrDefault(a => a.GetName().Name?.Equals(name, StringComparison.OrdinalIgnoreCase) == true);

                if (assembly == null)
                {
                    AnsiConsole.MarkupLine($"[yellow]NuGet package not found: {name}[/]");
                    return;
                }

                var location = assembly.Location;
                if (string.IsNullOrEmpty(location))
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Cannot reload in-memory assembly");
                    return;
                }

                AnsiConsole.Status()
                    .Start($"Reloading NuGet package '{name}'...", ctx =>
                    {
                        ctx.Status("Unloading...");
                        var unloaded = _editor.assemblyHandler.UnloadNugget(name);

                        if (unloaded)
                        {
                            ctx.Status("Loading...");
                            System.Threading.Thread.Sleep(500); // Brief pause
                            var loaded = _editor.assemblyHandler.LoadNugget(location);

                            if (loaded)
                            {
                                AnsiConsole.MarkupLine($"[green]✓[/] NuGet package reloaded: {name}");
                            }
                            else
                            {
                                AnsiConsole.MarkupLine($"[red]✗[/] Failed to reload NuGet package");
                            }
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[red]✗[/] Failed to unload NuGet package");
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error reloading NuGet package: {ex.Message}");
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
                "nuget load C:\\packages\\MyPackage.dll",
                "nuget unload MyPackage",
                "nuget search C:\\packages --recursive",
                "nuget info MyPackage",
                "nuget reload MyPackage",
                "nuget validate C:\\packages\\MyPackage.dll"
            };
        }
    }
}
