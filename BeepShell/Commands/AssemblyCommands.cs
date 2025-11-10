using System.CommandLine;
using BeepShell.Infrastructure;
using Spectre.Console;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Shell.Commands
{
    /// <summary>
    /// Assembly management commands for BeepShell
    /// Provides direct access to AssemblyHandler operations
    /// </summary>
    public static class AssemblyCommands
    {
        /// <summary>
        /// Build the assembly management root command
        /// </summary>
        public static Command Build(IDMEEditor editor)
        {
            var assemblyCmd = new Command("assembly", "Manage assemblies, types, and extensions");
            assemblyCmd.AddAlias("asm");

            // Subcommands
            assemblyCmd.AddCommand(BuildListCommand(editor));
            assemblyCmd.AddCommand(BuildLoadCommand(editor));
            assemblyCmd.AddCommand(BuildUnloadCommand(editor));
            assemblyCmd.AddCommand(BuildScanCommand(editor));
            assemblyCmd.AddCommand(BuildTypesCommand(editor));
            assemblyCmd.AddCommand(BuildDriversCommand(editor));
            assemblyCmd.AddCommand(BuildExtensionsCommand(editor));
            assemblyCmd.AddCommand(BuildCreateInstanceCommand(editor));
            assemblyCmd.AddCommand(BuildNuggetCommand(editor));

            return assemblyCmd;
        }

        private static Command BuildListCommand(IDMEEditor editor)
        {
            var cmd = new Command("list", "List loaded assemblies");
            cmd.AddAlias("ls");

            var verboseOption = new Option<bool>("--verbose", () => false, "Show detailed information");
            verboseOption.AddAlias("-v");
            cmd.AddOption(verboseOption);

            var filterOption = new Option<string>("--filter", "Filter assemblies by name");
            filterOption.AddAlias("-f");
            cmd.AddOption(filterOption);

            cmd.SetHandler((verbose, filter) =>
            {
                var assemblies = editor.AssemblyHandler.LoadedAssemblies;

                if (!string.IsNullOrWhiteSpace(filter))
                {
                    assemblies = assemblies.Where(a => a.FullName.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (!assemblies.Any())
                {
                    AnsiConsole.MarkupLine("[yellow]No assemblies found[/]");
                    return;
                }

                var table = new Table();
                table.Border = TableBorder.Rounded;
                table.Title = new TableTitle($"[bold cyan]Loaded Assemblies ({assemblies.Count})[/]");
                
                table.AddColumn("#");
                table.AddColumn("Name");
                
                if (verbose)
                {
                    table.AddColumn("Version");
                    table.AddColumn("Location");
                    table.AddColumn("GAC");
                }

                int index = 1;
                foreach (var asm in assemblies)
                {
                    var name = asm.GetName();
                    if (verbose)
                    {
                        table.AddRow(
                            index.ToString(),
                            name.Name,
                            name.Version?.ToString() ?? "N/A",
                            asm.Location ?? "[dim]Dynamic[/]",
                            asm.GlobalAssemblyCache ? "[green]Yes[/]" : "[dim]No[/]"
                        );
                    }
                    else
                    {
                        table.AddRow(index.ToString(), name.Name);
                    }
                    index++;
                }

                AnsiConsole.Write(table);
                
                if (!verbose)
                {
                    AnsiConsole.MarkupLine("[dim]Use --verbose for more details[/]");
                }

            }, verboseOption, filterOption);

            return cmd;
        }

        private static Command BuildLoadCommand(IDMEEditor editor)
        {
            var cmd = new Command("load", "Load assembly from path");

            var pathArg = new Argument<string>("path", "Path to assembly file or directory");
            cmd.AddArgument(pathArg);

            var typeOption = new Option<string>("--type", () => "SharedAssembly", "Folder type (SharedAssembly, ProjectClass, Addin, OtherDLL)");
            typeOption.AddAlias("-t");
            cmd.AddOption(typeOption);

            cmd.SetHandler((path, typeStr) =>
            {
                if (!File.Exists(path) && !Directory.Exists(path))
                {
                    AnsiConsole.MarkupLine($"[red]Path not found:[/] {path}");
                    return;
                }

                FolderFileTypes folderType;
                if (!Enum.TryParse<FolderFileTypes>(typeStr, true, out folderType))
                {
                    AnsiConsole.MarkupLine($"[red]Invalid folder type:[/] {typeStr}");
                    AnsiConsole.MarkupLine("[yellow]Valid types:[/] SharedAssembly, ProjectClass, Addin, OtherDLL");
                    return;
                }

                try
                {
                    var result = AnsiConsole.Status()
                        .Spinner(Spinner.Known.Dots)
                        .Start($"Loading assembly from {path}...", ctx =>
                        {
                            if (File.Exists(path))
                            {
                                var asm = editor.AssemblyHandler.LoadAssembly(path);
                                return asm != null ? "Success" : "Failed";
                            }
                            else
                            {
                                var result = editor.AssemblyHandler.LoadAssembly(path, folderType);
                                return result;
                            }
                        });

                    if (result == "Success" || string.IsNullOrEmpty(result))
                    {
                        AnsiConsole.MarkupLine($"[green]✓[/] Assembly loaded successfully");
                        AnsiConsole.MarkupLine($"[dim]Total assemblies: {editor.AssemblyHandler.LoadedAssemblies.Count}[/]");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[yellow]Result:[/] {result}");
                    }
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
                }

            }, pathArg, typeOption);

            return cmd;
        }

        private static Command BuildUnloadCommand(IDMEEditor editor)
        {
            var cmd = new Command("unload", "Unload assembly or nugget");

            var nameArg = new Argument<string>("name", "Assembly or nugget name");
            cmd.AddArgument(nameArg);

            var isNuggetOption = new Option<bool>("--nugget", () => false, "Unload as nugget package");
            isNuggetOption.AddAlias("-n");
            cmd.AddOption(isNuggetOption);

            cmd.SetHandler((name, isNugget) =>
            {
                try
                {
                    bool success;
                    if (isNugget)
                    {
                        success = editor.AssemblyHandler.UnloadNugget(name);
                    }
                    else
                    {
                        success = editor.AssemblyHandler.UnloadAssembly(name);
                    }

                    if (success)
                    {
                        AnsiConsole.MarkupLine($"[green]✓[/] {(isNugget ? "Nugget" : "Assembly")} unloaded: {name}");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[yellow]Failed to unload:[/] {name}");
                    }
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
                }

            }, nameArg, isNuggetOption);

            return cmd;
        }

        private static Command BuildScanCommand(IDMEEditor editor)
        {
            var cmd = new Command("scan", "Scan assemblies for types and extensions");

            var pathOption = new Option<string>("--path", "Scan specific assembly path");
            pathOption.AddAlias("-p");
            cmd.AddOption(pathOption);

            var allOption = new Option<bool>("--all", () => false, "Scan all loaded assemblies");
            allOption.AddAlias("-a");
            cmd.AddOption(allOption);

            cmd.SetHandler((path, all) =>
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        var asm = editor.AssemblyHandler.LoadAssembly(path);
                        if (asm != null)
                        {
                            AnsiConsole.Status()
                                .Start("Scanning assembly...", ctx =>
                                {
                                    editor.AssemblyHandler.ScanAssembly(asm);
                                });
                            AnsiConsole.MarkupLine($"[green]✓[/] Assembly scanned: {asm.GetName().Name}");
                        }
                    }
                    else if (all)
                    {
                        AnsiConsole.Status()
                            .Start("Scanning all assemblies...", ctx =>
                            {
                                foreach (var asm in editor.AssemblyHandler.LoadedAssemblies)
                                {
                                    ctx.Status($"Scanning {asm.GetName().Name}...");
                                    editor.AssemblyHandler.ScanAssembly(asm);
                                }
                            });
                        AnsiConsole.MarkupLine($"[green]✓[/] All assemblies scanned");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[yellow]Specify --path or --all[/]");
                    }
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
                }

            }, pathOption, allOption);

            return cmd;
        }

        private static Command BuildTypesCommand(IDMEEditor editor)
        {
            var cmd = new Command("types", "List types from assemblies");

            var assemblyOption = new Option<string>("--assembly", "Filter by assembly name");
            assemblyOption.AddAlias("-a");
            cmd.AddOption(assemblyOption);

            var interfaceOption = new Option<string>("--interface", "Filter by implemented interface");
            interfaceOption.AddAlias("-i");
            cmd.AddOption(interfaceOption);

            var limitOption = new Option<int>("--limit", () => 50, "Maximum number of types to display");
            limitOption.AddAlias("-l");
            cmd.AddOption(limitOption);

            cmd.SetHandler((assemblyName, interfaceName, limit) =>
            {
                var types = new List<Type>();

                var assemblies = editor.AssemblyHandler.LoadedAssemblies;
                if (!string.IsNullOrWhiteSpace(assemblyName))
                {
                    assemblies = assemblies.Where(a => a.GetName().Name.Contains(assemblyName, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                foreach (var asm in assemblies)
                {
                    try
                    {
                        var asmTypes = asm.GetTypes();
                        
                        if (!string.IsNullOrWhiteSpace(interfaceName))
                        {
                            asmTypes = asmTypes.Where(t => 
                                t.GetInterfaces().Any(i => i.Name.Contains(interfaceName, StringComparison.OrdinalIgnoreCase))
                            ).ToArray();
                        }

                        types.AddRange(asmTypes);
                    }
                    catch { }
                }

                if (!types.Any())
                {
                    AnsiConsole.MarkupLine("[yellow]No types found[/]");
                    return;
                }

                var displayTypes = types.Take(limit).ToList();

                var table = new Table();
                table.Border = TableBorder.Rounded;
                table.Title = new TableTitle($"[bold cyan]Types ({types.Count} total, showing {displayTypes.Count})[/]");
                table.AddColumn("Type Name");
                table.AddColumn("Assembly");
                table.AddColumn("Namespace");

                foreach (var type in displayTypes)
                {
                    table.AddRow(
                        type.Name,
                        type.Assembly.GetName().Name,
                        type.Namespace ?? "[dim]N/A[/]"
                    );
                }

                AnsiConsole.Write(table);

                if (types.Count > limit)
                {
                    AnsiConsole.MarkupLine($"[dim]Showing {limit} of {types.Count} types. Use --limit to see more.[/]");
                }

            }, assemblyOption, interfaceOption, limitOption);

            return cmd;
        }

        private static Command BuildDriversCommand(IDMEEditor editor)
        {
            var cmd = new Command("drivers", "List data drivers from assemblies");

            var categoryOption = new Option<string>("--category", "Filter by category");
            categoryOption.AddAlias("-c");
            cmd.AddOption(categoryOption);

            cmd.SetHandler((category) =>
            {
                var drivers = editor.ConfigEditor.DataDriversClasses;

                if (!string.IsNullOrWhiteSpace(category))
                {
                    drivers = drivers.Where(d => d.category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (!drivers.Any())
                {
                    AnsiConsole.MarkupLine("[yellow]No drivers found[/]");
                    return;
                }

                var table = new Table();
                table.Border = TableBorder.Rounded;
                table.Title = new TableTitle($"[bold cyan]Data Drivers ({drivers.Count})[/]");
                table.AddColumn("Name");
                table.AddColumn("Category");
                table.AddColumn("Package");
                table.AddColumn("Version");

                foreach (var driver in drivers)
                {
                    table.AddRow(
                        driver.className,
                        driver.category.ToString(),
                        driver.PackageName ?? "[dim]N/A[/]",
                        driver.version ?? "[dim]N/A[/]"
                    );
                }

                AnsiConsole.Write(table);

            }, categoryOption);

            return cmd;
        }

        private static Command BuildExtensionsCommand(IDMEEditor editor)
        {
            var cmd = new Command("extensions", "List loader extensions");

            cmd.SetHandler(() =>
            {
                var extensions = editor.AssemblyHandler.LoaderExtensionClasses;

                if (!extensions.Any())
                {
                    AnsiConsole.MarkupLine("[yellow]No loader extensions found[/]");
                    return;
                }

                var table = new Table();
                table.Border = TableBorder.Rounded;
                table.Title = new TableTitle($"[bold cyan]Loader Extensions ({extensions.Count})[/]");
                table.AddColumn("Class Name");
                table.AddColumn("Assembly");
                table.AddColumn("Namespace");

                foreach (var ext in extensions)
                {
                    table.AddRow(
                        ext.className,
                        ext.assemblyname,
                        ext.nameSpace ?? "[dim]N/A[/]"
                    );
                }

                AnsiConsole.Write(table);
            });

            return cmd;
        }

        private static Command BuildCreateInstanceCommand(IDMEEditor editor)
        {
            var cmd = new Command("create", "Create instance from type name");

            var typeArg = new Argument<string>("typename", "Fully qualified type name");
            cmd.AddArgument(typeArg);

            var assemblyOption = new Option<string>("--assembly", "Assembly name if type is in specific assembly");
            assemblyOption.AddAlias("-a");
            cmd.AddOption(assemblyOption);

            cmd.SetHandler((typeName, assemblyName) =>
            {
                try
                {
                    object instance;
                    
                    if (!string.IsNullOrWhiteSpace(assemblyName))
                    {
                        instance = editor.AssemblyHandler.CreateInstanceFromString(assemblyName, typeName);
                    }
                    else
                    {
                        instance = editor.AssemblyHandler.CreateInstanceFromString(typeName);
                    }

                    if (instance != null)
                    {
                        AnsiConsole.MarkupLine($"[green]✓[/] Instance created: {instance.GetType().FullName}");
                        AnsiConsole.MarkupLine($"[dim]Type: {instance.GetType().Name}[/]");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[yellow]Failed to create instance[/]");
                    }
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
                }

            }, typeArg, assemblyOption);

            return cmd;
        }

        private static Command BuildNuggetCommand(IDMEEditor editor)
        {
            var nuggetCmd = new Command("nugget", "Manage NuGet packages (nuggets)");

            // Load nugget
            var loadCmd = new Command("load", "Load a nugget package");
            var pathArg = new Argument<string>("path", "Path to nugget directory or DLL");
            loadCmd.AddArgument(pathArg);

            loadCmd.SetHandler((path) =>
            {
                try
                {
                    var success = editor.AssemblyHandler.LoadNugget(path);
                    if (success)
                    {
                        AnsiConsole.MarkupLine($"[green]✓[/] Nugget loaded: {path}");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[yellow]Failed to load nugget[/]");
                    }
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
                }
            }, pathArg);

            nuggetCmd.AddCommand(loadCmd);

            return nuggetCmd;
        }
    }
}
