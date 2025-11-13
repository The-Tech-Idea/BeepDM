using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using Spectre.Console;
using TheTechIdea.Beep.ConfigUtil;

namespace BeepShell.Commands
{
    /// <summary>
    /// Profile management commands for ConfigShellCommands
    /// Handles create, list, switch, and delete operations for configuration profiles
    /// Profiles are stored as Profile_{name}.json in the Config folder
    /// </summary>
    public partial class ConfigShellCommands
    {
        private void AddProfileCommands(Command parent)
        {
            var profileCommand = new Command("profile", "Manage configuration profiles");

            // List profiles
            var listCommand = new Command("list", "List all available profiles");
            listCommand.SetHandler(ListProfiles);
            profileCommand.AddCommand(listCommand);

            // Create profile
            var createCommand = new Command("create", "Create a new profile");
            var nameArg = new Argument<string>("name", "Profile name");
            var templateOpt = new Option<string>("--template", "Template profile to copy from");
            createCommand.AddArgument(nameArg);
            createCommand.AddOption(templateOpt);
            createCommand.SetHandler(CreateProfile, nameArg, templateOpt);
            profileCommand.AddCommand(createCommand);

            // Switch profile
            var switchCommand = new Command("switch", "Switch to a different profile");
            var profileArg = new Argument<string>("name", "Profile name to switch to");
            switchCommand.AddArgument(profileArg);
            switchCommand.SetHandler(SwitchProfile, profileArg);
            profileCommand.AddCommand(switchCommand);

            // Delete profile
            var deleteCommand = new Command("delete", "Delete a profile");
            var deleteArg = new Argument<string>("name", "Profile name to delete");
            deleteCommand.AddArgument(deleteArg);
            deleteCommand.SetHandler(DeleteProfile, deleteArg);
            profileCommand.AddCommand(deleteCommand);

            parent.AddCommand(profileCommand);
        }

        private void ListProfiles()
        {
            try
            {
                var configPath = _editor.ConfigEditor.ConfigPath;
                
                if (!Directory.Exists(configPath))
                {
                    AnsiConsole.MarkupLine("[yellow]Config directory not found[/]");
                    return;
                }

                var profileFiles = Directory.GetFiles(configPath, "*.json")
                    .Where(f => Path.GetFileName(f).StartsWith("Profile_", StringComparison.OrdinalIgnoreCase) ||
                               Path.GetFileName(f).Equals("Config.json", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (profileFiles.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]No profiles found[/]");
                    return;
                }

                var table = new Table();
                table.Border = TableBorder.Rounded;
                table.AddColumn("[cyan]Profile Name[/]");
                table.AddColumn("[cyan]File[/]");
                table.AddColumn("[cyan]Last Modified[/]");
                table.AddColumn("[cyan]Status[/]");

                var currentConfig = Path.Combine(configPath, "Config.json");

                foreach (var file in profileFiles.OrderBy(f => f))
                {
                    var fileName = Path.GetFileName(file);
                    var profileName = fileName.Replace("Profile_", "").Replace(".json", "");
                    if (fileName.Equals("Config.json", StringComparison.OrdinalIgnoreCase))
                        profileName = "default";

                    var lastModified = File.GetLastWriteTime(file).ToString("yyyy-MM-dd HH:mm");
                    var isCurrent = file.Equals(currentConfig, StringComparison.OrdinalIgnoreCase) ? "[green]●[/] Active" : "";

                    table.AddRow(profileName, fileName, lastModified, isCurrent);
                }

                AnsiConsole.Write(table);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error listing profiles: {ex.Message}");
            }
        }

        private void CreateProfile(string profileName, string templateName)
        {
            try
            {
                var configPath = _editor.ConfigEditor.ConfigPath;
                var newProfileFile = Path.Combine(configPath, $"Profile_{profileName}.json");

                if (File.Exists(newProfileFile))
                {
                    AnsiConsole.MarkupLine($"[yellow]Profile '{profileName}' already exists[/]");
                    return;
                }

                // Load template or create new config
                ConfigandSettings newConfig;

                if (!string.IsNullOrEmpty(templateName))
                {
                    var templateFile = Path.Combine(configPath, $"Profile_{templateName}.json");
                    if (!File.Exists(templateFile))
                    {
                        templateFile = Path.Combine(configPath, "Config.json");
                    }

                    if (File.Exists(templateFile))
                    {
                        newConfig = _editor.ConfigEditor.JsonLoader.DeserializeSingleObject<ConfigandSettings>(templateFile);
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[yellow]Template '{templateName}' not found, creating new profile[/]");
                        newConfig = new ConfigandSettings();
                    }
                }
                else
                {
                    newConfig = new ConfigandSettings();
                }

                // Update paths to match new profile
                newConfig.ExePath = _editor.ConfigEditor.ExePath;
                newConfig.ConfigPath = configPath;

                // Save new profile
                _editor.ConfigEditor.JsonLoader.Serialize(newProfileFile, newConfig);
                
                AnsiConsole.MarkupLine($"[green]✓[/] Profile '{profileName}' created successfully");
                AnsiConsole.MarkupLine($"[dim]Location: {newProfileFile}[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error creating profile: {ex.Message}");
            }
        }

        private void SwitchProfile(string profileName)
        {
            try
            {
                var configPath = _editor.ConfigEditor.ConfigPath;
                var profileFile = profileName.Equals("default", StringComparison.OrdinalIgnoreCase)
                    ? Path.Combine(configPath, "Config.json")
                    : Path.Combine(configPath, $"Profile_{profileName}.json");

                if (!File.Exists(profileFile))
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Profile '{profileName}' not found[/]");
                    return;
                }

                // Load the profile
                var newConfig = _editor.ConfigEditor.JsonLoader.DeserializeSingleObject<ConfigandSettings>(profileFile);
                
                if (newConfig != null)
                {
                    _editor.ConfigEditor.Config = newConfig;
                    AnsiConsole.MarkupLine($"[green]✓[/] Switched to profile '{profileName}'[/]");
                    AnsiConsole.MarkupLine($"[yellow]Note:[/] Restart BeepShell to apply all changes");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to load profile '{profileName}'[/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error switching profile: {ex.Message}");
            }
        }

        private void DeleteProfile(string profileName)
        {
            try
            {
                if (profileName.Equals("default", StringComparison.OrdinalIgnoreCase))
                {
                    AnsiConsole.MarkupLine("[red]✗[/] Cannot delete default profile");
                    return;
                }

                var configPath = _editor.ConfigEditor.ConfigPath;
                var profileFile = Path.Combine(configPath, $"Profile_{profileName}.json");

                if (!File.Exists(profileFile))
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Profile '{profileName}' not found[/]");
                    return;
                }

                var confirm = AnsiConsole.Confirm($"Are you sure you want to delete profile '{profileName}'?");
                
                if (confirm)
                {
                    File.Delete(profileFile);
                    AnsiConsole.MarkupLine($"[green]✓[/] Profile '{profileName}' deleted successfully[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine("[dim]Delete cancelled[/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error deleting profile: {ex.Message}");
            }
        }
    }
}
