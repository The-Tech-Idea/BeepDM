using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using BeepShell.Infrastructure;
using Spectre.Console;
using TheTechIdea.Beep.Editor;

namespace BeepShell.Commands
{
    /// <summary>
    /// Profile management commands for BeepShell
    /// Uses persistent DMEEditor for profile operations
    /// </summary>
    public class ProfileShellCommands : IShellCommand
    {
        private IDMEEditor _editor;

        public string CommandName => "profile";
        public string Description => "Manage BeepDM profiles";
        public string Category => "Configuration";
        public string Version => "1.0.0";
        public string Author => "BeepDM Team";
        public string[] Aliases => new[] { "prof", "profiles" };

        public void Initialize(IDMEEditor editor)
        {
            _editor = editor;
        }

        public Command BuildCommand()
        {
            var profileCommand = new Command("profile", Description);

            // profile list
            var listCommand = new Command("list", "List all available profiles");
            listCommand.SetHandler(() => ListProfiles());
            profileCommand.AddCommand(listCommand);

            // profile current
            var currentCommand = new Command("current", "Show current profile");
            currentCommand.SetHandler(() => ShowCurrentProfile());
            profileCommand.AddCommand(currentCommand);

            // profile create
            var createCommand = new Command("create", "Create a new profile");
            var nameArg = new Argument<string>("name", "Profile name");
            var templateOption = new Option<string>("--template", "Template profile to copy from");

            createCommand.AddArgument(nameArg);
            createCommand.AddOption(templateOption);

            createCommand.SetHandler((name, template) =>
            {
                CreateProfile(name, template);
            }, nameArg, templateOption);

            profileCommand.AddCommand(createCommand);

            // profile delete
            var deleteCommand = new Command("delete", "Delete a profile");
            var deleteNameArg = new Argument<string>("name", "Profile name");
            deleteCommand.AddArgument(deleteNameArg);
            deleteCommand.SetHandler((name) => DeleteProfile(name), deleteNameArg);
            profileCommand.AddCommand(deleteCommand);

            // profile info
            var infoCommand = new Command("info", "Show profile information");
            var infoNameArg = new Argument<string>("name", "Profile name (current if not specified)");
            infoNameArg.SetDefaultValue(null);
            infoCommand.AddArgument(infoNameArg);
            infoCommand.SetHandler((name) => ShowProfileInfo(name), infoNameArg);
            profileCommand.AddCommand(infoCommand);

            return profileCommand;
        }

        private void ListProfiles()
        {
            try
            {
                var configPath = _editor.ConfigEditor.Config.ConfigPath;
                var configDir = Path.GetDirectoryName(configPath);

                if (!Directory.Exists(configDir))
                {
                    AnsiConsole.MarkupLine("[yellow]Config directory not found[/]");
                    return;
                }

                var configFiles = Directory.GetFiles(configDir, "*.json")
                    .Where(f => Path.GetFileName(f).StartsWith("Config_", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (configFiles.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]No profiles found[/]");
                    return;
                }

                var currentProfile = Path.GetFileNameWithoutExtension(_editor.ConfigEditor.Config.ConfigPath)
                    .Replace("Config_", "");

                var table = new Table();
                table.Border = TableBorder.Rounded;
                table.AddColumn("[cyan]Profile[/]");
                table.AddColumn("[cyan]Path[/]");
                table.AddColumn("[cyan]Current[/]");

                foreach (var file in configFiles)
                {
                    var profileName = Path.GetFileNameWithoutExtension(file).Replace("Config_", "");
                    var isCurrent = profileName.Equals(currentProfile, StringComparison.OrdinalIgnoreCase);
                    var marker = isCurrent ? "[green]✓[/]" : "";

                    table.AddRow(
                        profileName,
                        file,
                        marker
                    );
                }

                AnsiConsole.Write(table);
                AnsiConsole.MarkupLine($"\n[dim]Total: {configFiles.Count} profiles[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error listing profiles: {ex.Message}");
            }
        }

        private void ShowCurrentProfile()
        {
            try
            {
                var currentProfile = Path.GetFileNameWithoutExtension(_editor.ConfigEditor.Config.ConfigPath)
                    .Replace("Config_", "");

                var panel = new Panel(new Markup(
                    $"[cyan]Name:[/] {currentProfile}\n" +
                    $"[cyan]Path:[/] {_editor.ConfigEditor.Config.ConfigPath}\n" +
                    $"[cyan]Connections:[/] {_editor.ConfigEditor.DataConnections?.Count ?? 0}\n" +
                    $"[cyan]Data Sources:[/] {_editor.DataSources.Count}\n" +
                    $"[cyan]Open Connections:[/] {_editor.DataSources.Count(ds => ds.ConnectionStatus == System.Data.ConnectionState.Open)}"
                ));
                panel.Header = new PanelHeader("[green]Current Profile[/]");
                panel.Border = BoxBorder.Rounded;

                AnsiConsole.Write(panel);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
            }
        }

        private void CreateProfile(string name, string template)
        {
            try
            {
                var configPath = _editor.ConfigEditor.Config.ConfigPath;
                var configDir = Path.GetDirectoryName(configPath);
                var newProfilePath = Path.Combine(configDir, $"Config_{name}.json");

                if (File.Exists(newProfilePath))
                {
                    AnsiConsole.MarkupLine($"[yellow]Profile '{name}' already exists[/]");
                    return;
                }

                if (!string.IsNullOrEmpty(template))
                {
                    var templatePath = Path.Combine(configDir, $"Config_{template}.json");
                    if (File.Exists(templatePath))
                    {
                        File.Copy(templatePath, newProfilePath);
                        AnsiConsole.MarkupLine($"[green]✓[/] Profile '{name}' created from template '{template}'");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[yellow]Template '{template}' not found, creating empty profile[/]");
                        CreateEmptyProfile(newProfilePath);
                    }
                }
                else
                {
                    CreateEmptyProfile(newProfilePath);
                }

                AnsiConsole.MarkupLine($"[green]✓[/] Profile '{name}' created at: {newProfilePath}");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
            }
        }

        private void CreateEmptyProfile(string path)
        {
            var emptyConfig = @"{
  ""ConfigPath"": """",
  ""DataConnections"": [],
  ""DataDriversClasses"": [],
  ""DataMappings"": []
}";
            File.WriteAllText(path, emptyConfig);
        }

        private void DeleteProfile(string name)
        {
            try
            {
                var configPath = _editor.ConfigEditor.Config.ConfigPath;
                var configDir = Path.GetDirectoryName(configPath);
                var profilePath = Path.Combine(configDir, $"Config_{name}.json");

                var currentProfile = Path.GetFileNameWithoutExtension(configPath).Replace("Config_", "");
                if (currentProfile.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    AnsiConsole.MarkupLine("[red]✗[/] Cannot delete the current profile");
                    return;
                }

                if (!File.Exists(profilePath))
                {
                    AnsiConsole.MarkupLine($"[yellow]Profile '{name}' not found[/]");
                    return;
                }

                File.Delete(profilePath);
                AnsiConsole.MarkupLine($"[green]✓[/] Profile '{name}' deleted");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
            }
        }

        private void ShowProfileInfo(string name)
        {
            try
            {
                string profilePath;
                
                if (string.IsNullOrEmpty(name))
                {
                    // Show current profile
                    ShowCurrentProfile();
                    return;
                }

                var configPath = _editor.ConfigEditor.Config.ConfigPath;
                var configDir = Path.GetDirectoryName(configPath);
                profilePath = Path.Combine(configDir, $"Config_{name}.json");

                if (!File.Exists(profilePath))
                {
                    AnsiConsole.MarkupLine($"[yellow]Profile '{name}' not found[/]");
                    return;
                }

                var fileInfo = new FileInfo(profilePath);
                var panel = new Panel(new Markup(
                    $"[cyan]Name:[/] {name}\n" +
                    $"[cyan]Path:[/] {profilePath}\n" +
                    $"[cyan]Size:[/] {fileInfo.Length} bytes\n" +
                    $"[cyan]Modified:[/] {fileInfo.LastWriteTime}"
                ));
                panel.Header = new PanelHeader($"[green]Profile: {name}[/]");
                panel.Border = BoxBorder.Rounded;

                AnsiConsole.Write(panel);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
            }
        }

        public bool CanExecute() => _editor != null;

        public string[] GetExamples()
        {
            return new[]
            {
                "profile list",
                "profile current",
                "profile create dev",
                "profile create staging --template production",
                "profile info dev",
                "profile delete old_profile"
            };
        }
    }
}
