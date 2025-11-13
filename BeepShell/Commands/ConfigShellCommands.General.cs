using System;
using System.CommandLine;
using System.IO;
using Spectre.Console;
using TheTechIdea.Beep.ConfigUtil;

namespace BeepShell.Commands
{
    /// <summary>
    /// General configuration commands for ConfigShellCommands
    /// Handles show, save, reload, reset, export, and import operations
    /// </summary>
    public partial class ConfigShellCommands
    {
        private void AddGeneralCommands(Command parent)
        {
            // Show config
            var showCommand = new Command("show", "Show configuration summary");
            showCommand.SetHandler(ShowConfig);
            parent.AddCommand(showCommand);

            // Save config
            var saveCommand = new Command("save", "Save all configuration");
            saveCommand.SetHandler(SaveAllConfig);
            parent.AddCommand(saveCommand);

            // Reload config
            var reloadCommand = new Command("reload", "Reload all configuration");
            reloadCommand.SetHandler(ReloadConfig);
            parent.AddCommand(reloadCommand);

            // Reset config
            var resetCommand = new Command("reset", "Reset configuration to defaults");
            var confirmOpt = new Option<bool>("--confirm", "Confirm reset without prompt");
            resetCommand.AddOption(confirmOpt);
            resetCommand.SetHandler(ResetConfig, confirmOpt);
            parent.AddCommand(resetCommand);

            // Export config
            var exportCommand = new Command("export", "Export configuration to file");
            var exportArg = new Argument<string>("path", "Export file path");
            exportCommand.AddArgument(exportArg);
            exportCommand.SetHandler(ExportConfig, exportArg);
            parent.AddCommand(exportCommand);

            // Import config
            var importCommand = new Command("import", "Import configuration from file");
            var importArg = new Argument<string>("path", "Import file path");
            importCommand.AddArgument(importArg);
            importCommand.SetHandler(ImportConfig, importArg);
            parent.AddCommand(importCommand);
        }

        private void ShowConfig()
        {
            try
            {
                var config = _editor.ConfigEditor.Config;
                
                var panel = new Panel(new Markup(
                    $"[cyan]Config Path:[/] {config.ConfigPath}\n" +
                    $"[cyan]Container:[/] {_editor.ConfigEditor.ContainerName}\n" +
                    $"[cyan]Connections:[/] {_editor.ConfigEditor.DataConnections?.Count ?? 0}\n" +
                    $"[cyan]Data Sources:[/] {_editor.DataSources.Count}\n" +
                    $"[cyan]Drivers:[/] {_editor.ConfigEditor.DataDriversClasses?.Count ?? 0}\n" +
                    $"[cyan]Loaded Assemblies:[/] {_editor.ConfigEditor.LoadedAssemblies?.Count ?? 0}"
                ));
                panel.Header = new PanelHeader("[green]BeepDM Configuration[/]");
                panel.Border = BoxBorder.Rounded;

                AnsiConsole.Write(panel);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error showing config: {ex.Message}");
            }
        }

        private void SaveAllConfig()
        {
            try
            {
                AnsiConsole.Status()
                    .Start("Saving configuration...", ctx =>
                    {
                        ctx.Status("Saving config values...");
                        _editor.ConfigEditor.SaveConfigValues();
                        
                        ctx.Status("Saving data connections...");
                        _editor.ConfigEditor.SaveDataconnectionsValues();
                        
                        ctx.Status("Saving connection drivers...");
                        _editor.ConfigEditor.SaveConnectionDriversConfigValues();
                        
                        AnsiConsole.MarkupLine("[green]✓[/] All configuration saved successfully");
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error saving config: {ex.Message}");
            }
        }

        private void ReloadConfig()
        {
            try
            {
                AnsiConsole.Status()
                    .Start("Reloading configuration...", ctx =>
                    {
                        ctx.Status("Loading config values...");
                        _editor.ConfigEditor.LoadConfigValues();
                        
                        ctx.Status("Loading data connections...");
                        _editor.ConfigEditor.LoadDataConnectionsValues();
                        
                        ctx.Status("Loading connection drivers...");
                        _editor.ConfigEditor.LoadConnectionDriversConfigValues();
                        
                        AnsiConsole.MarkupLine("[green]✓[/] All configuration reloaded successfully");
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error reloading config: {ex.Message}");
            }
        }

        private void ResetConfig(bool confirm)
        {
            try
            {
                if (!confirm)
                {
                    confirm = AnsiConsole.Confirm("[red]Are you sure you want to reset configuration to defaults?[/]");
                }

                if (!confirm)
                {
                    AnsiConsole.MarkupLine("[dim]Reset cancelled[/]");
                    return;
                }

                // Create new default config
                var newConfig = new ConfigandSettings
                {
                    ExePath = _editor.ConfigEditor.ExePath,
                    ConfigPath = _editor.ConfigEditor.ConfigPath
                };

                _editor.ConfigEditor.Config = newConfig;
                _editor.ConfigEditor.SaveConfigValues();

                AnsiConsole.MarkupLine("[green]✓[/] Configuration reset to defaults");
                AnsiConsole.MarkupLine("[yellow]Note:[/] Restart BeepShell to apply all changes");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error resetting config: {ex.Message}");
            }
        }

        private void ExportConfig(string path)
        {
            try
            {
                var fullPath = Path.GetFullPath(path);
                var directory = Path.GetDirectoryName(fullPath);

                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                _editor.ConfigEditor.JsonLoader.Serialize(fullPath, _editor.ConfigEditor.Config);
                
                AnsiConsole.MarkupLine($"[green]✓[/] Configuration exported successfully");
                AnsiConsole.MarkupLine($"[dim]Location: {fullPath}[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error exporting config: {ex.Message}");
            }
        }

        private void ImportConfig(string path)
        {
            try
            {
                var fullPath = Path.GetFullPath(path);

                if (!File.Exists(fullPath))
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] File not found: {fullPath}[/]");
                    return;
                }

                var importedConfig = _editor.ConfigEditor.JsonLoader.DeserializeSingleObject<ConfigandSettings>(fullPath);

                if (importedConfig == null)
                {
                    AnsiConsole.MarkupLine("[red]✗[/] Failed to load configuration from file");
                    return;
                }

                // Preserve current paths
                importedConfig.ExePath = _editor.ConfigEditor.ExePath;
                importedConfig.ConfigPath = _editor.ConfigEditor.ConfigPath;

                _editor.ConfigEditor.Config = importedConfig;
                _editor.ConfigEditor.SaveConfigValues();

                AnsiConsole.MarkupLine($"[green]✓[/] Configuration imported successfully");
                AnsiConsole.MarkupLine("[yellow]Note:[/] Restart BeepShell to apply all changes");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error importing config: {ex.Message}");
            }
        }
    }
}
