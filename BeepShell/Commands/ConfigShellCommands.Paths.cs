using System;
using System.CommandLine;
using System.IO;
using Spectre.Console;

namespace BeepShell.Commands
{
    /// <summary>
    /// Path management commands for ConfigShellCommands
    /// Handles display and creation of all configuration folder paths
    /// Uses ConfigEditor to ensure cross-platform path resolution
    /// </summary>
    public partial class ConfigShellCommands
    {
        private void AddPathCommands(Command parent)
        {
            var pathCommand = new Command("path", "Manage configuration paths");

            // Show paths
            var showCommand = new Command("show", "Show all configuration paths");
            showCommand.SetHandler(ShowPaths);
            pathCommand.AddCommand(showCommand);

            // Create missing directories
            var createCommand = new Command("create", "Create missing configuration directories");
            createCommand.SetHandler(CreateConfigDirectories);
            pathCommand.AddCommand(createCommand);

            parent.AddCommand(pathCommand);
        }

        private void ShowPaths()
        {
            try
            {
                var config = _editor.ConfigEditor.Config;
                
                var table = new Table();
                table.Border = TableBorder.Rounded;
                table.AddColumn("[cyan]Folder Type[/]");
                table.AddColumn("[cyan]Path[/]");
                table.AddColumn("[cyan]Exists[/]");

                AddPathRow(table, "Root/Container", _editor.ConfigEditor.ContainerName);
                AddPathRow(table, "Configuration", config.ConfigPath);
                AddPathRow(table, "Connection Drivers", config.ConnectionDriversPath);
                AddPathRow(table, "Data Sources", config.DataSourcesPath);
                AddPathRow(table, "Loader Extensions", config.LoaderExtensionsPath);
                AddPathRow(table, "Addins", config.AddinPath);
                AddPathRow(table, "Data Files", config.DataFilePath);
                AddPathRow(table, "Data Views", config.DataViewPath);
                AddPathRow(table, "Project Data", config.ProjectDataPath);
                AddPathRow(table, "Project Classes", config.ClassPath);
                AddPathRow(table, "Entities", config.EntitiesPath);
                AddPathRow(table, "Mapping", config.MappingPath);
                AddPathRow(table, "WorkFlows", config.WorkFlowPath);
                AddPathRow(table, "Scripts", config.ScriptsPath);
                AddPathRow(table, "Scripts Logs", config.ScriptsLogsPath);
                AddPathRow(table, "GFX", config.GFXPath);
                AddPathRow(table, "Other DLLs", config.OtherDLLPath);

                AnsiConsole.Write(table);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error showing paths: {ex.Message}");
            }
        }

        private void AddPathRow(Table table, string folderType, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                table.AddRow(folderType, "[dim]Not configured[/]", "[yellow]N/A[/]");
                return;
            }

            var exists = Directory.Exists(path);
            var status = exists ? "[green]✓[/]" : "[red]✗[/]";
            table.AddRow(folderType, path, status);
        }

        private void CreateConfigDirectories()
        {
            try
            {
                AnsiConsole.Status()
                    .Start("Creating configuration directories...", ctx =>
                    {
                        var config = _editor.ConfigEditor.Config;
                        int created = 0;
                        int existing = 0;

                        var directories = new[]
                        {
                            ("Config", config.ConfigPath),
                            ("ConnectionDrivers", config.ConnectionDriversPath),
                            ("DataSources", config.DataSourcesPath),
                            ("LoaderExtensions", config.LoaderExtensionsPath),
                            ("Addins", config.AddinPath),
                            ("DataFiles", config.DataFilePath),
                            ("DataViews", config.DataViewPath),
                            ("ProjectData", config.ProjectDataPath),
                            ("ProjectClasses", config.ClassPath),
                            ("Entities", config.EntitiesPath),
                            ("Mapping", config.MappingPath),
                            ("WorkFlows", config.WorkFlowPath),
                            ("Scripts", config.ScriptsPath),
                            ("ScriptsLogs", config.ScriptsLogsPath),
                            ("GFX", config.GFXPath),
                            ("OtherDLLs", config.OtherDLLPath)
                        };

                        foreach (var (name, path) in directories)
                        {
                            if (string.IsNullOrEmpty(path))
                                continue;

                            ctx.Status($"Processing {name}...");

                            if (!Directory.Exists(path))
                            {
                                Directory.CreateDirectory(path);
                                created++;
                                ctx.Status($"[green]Created {name}[/]");
                            }
                            else
                            {
                                existing++;
                            }
                        }

                        AnsiConsole.MarkupLine($"[green]✓[/] Created {created} directories, {existing} already existed");
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error creating directories: {ex.Message}");
            }
        }
    }
}
