using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using Spectre.Console;
using TheTechIdea.Beep.CLI.Infrastructure;

namespace TheTechIdea.Beep.CLI.Commands
{
    /// <summary>
    /// Enhanced profile management commands for managing configuration profiles
    /// </summary>
    public static class ProfileCommands
    {
        public static Command Build()
        {
            var profileCommand = new Command("profile", "Manage configuration profiles for different environments");

            // profile list
            var listCommand = new Command("list", "List all available profiles");
            var verboseOption = new Option<bool>("--verbose", () => false, "Show detailed information");
            listCommand.AddOption(verboseOption);
            listCommand.SetHandler((bool verbose) =>
            {
                var profiles = ProfileManager.ListProfiles();
                
                if (!profiles.Any())
                {
                    AnsiConsole.MarkupLine("[yellow]No profiles found[/]");
                    AnsiConsole.MarkupLine($"[dim]Create a profile with:[/] [cyan]profile create <name>[/]");
                    return;
                }

                var table = new Table();
                table.Border = TableBorder.Rounded;
                table.Title = new TableTitle("[bold cyan]Available Profiles[/]");
                table.AddColumn(new TableColumn("[bold]Profile Name[/]").Centered());
                table.AddColumn(new TableColumn("[bold]Status[/]").Centered());
                
                if (verbose)
                {
                    table.AddColumn(new TableColumn("[bold]Path[/]").LeftAligned());
                    table.AddColumn(new TableColumn("[bold]Files[/]").Centered());
                }
                
                foreach (var profile in profiles.OrderBy(p => p))
                {
                    var profilePath = ProfileManager.GetProfilePath(profile);
                    var isDefault = profile == ProfileManager.DEFAULT_PROFILE;
                    var exists = Directory.Exists(profilePath);
                    
                    var statusMarkup = exists ? "[green]✓ Active[/]" : "[red]✗ Missing[/]";
                    var nameMarkup = isDefault ? $"[bold yellow]{profile}[/] [dim](default)[/]" : $"[cyan]{profile}[/]";
                    
                    if (verbose)
                    {
                        var fileCount = exists ? Directory.GetFiles(profilePath, "*.*", SearchOption.AllDirectories).Length : 0;
                        table.AddRow(
                            nameMarkup,
                            statusMarkup,
                            $"[dim]{profilePath}[/]",
                            fileCount.ToString()
                        );
                    }
                    else
                    {
                        table.AddRow(nameMarkup, statusMarkup);
                    }
                }
                
                AnsiConsole.Write(table);
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"[dim]Total profiles:[/] [cyan]{profiles.Length}[/]");
            }, verboseOption);

            // profile create
            var createCommand = new Command("create", "Create a new profile");
            var nameArg = new Argument<string>("name", "Name of the new profile");
            var sourceOption = new Option<string?>("--from", "Source profile to copy from (copies all configuration)");
            var descriptionOption = new Option<string?>("--description", "Description of the profile");
            createCommand.AddArgument(nameArg);
            createCommand.AddOption(sourceOption);
            createCommand.AddOption(descriptionOption);
            createCommand.SetHandler((string name, string? from, string? description) =>
            {
                // Validate profile name
                if (string.IsNullOrWhiteSpace(name) || name.Contains(Path.GetInvalidFileNameChars().First()))
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Invalid profile name. Use alphanumeric characters and hyphens only.");
                    return;
                }

                if (ProfileManager.ProfileExists(name))
                {
                    AnsiConsole.MarkupLine($"[yellow]![/] Profile '{name}' already exists");
                    
                    if (!AnsiConsole.Confirm($"Do you want to recreate it?", false))
                        return;
                    
                    ProfileManager.DeleteProfile(name);
                }

                AnsiConsole.Status()
                    .Start($"Creating profile '{name}'...", ctx =>
                    {
                        if (!string.IsNullOrEmpty(from))
                        {
                            if (!ProfileManager.ProfileExists(from))
                            {
                                AnsiConsole.MarkupLine($"[red]✗[/] Source profile '{from}' does not exist");
                                return;
                            }
                            ctx.Status($"Copying from '{from}'...");
                        }

                        if (ProfileManager.CreateProfile(name, from ?? string.Empty))
                        {
                            AnsiConsole.MarkupLine($"[green]✓[/] Profile '{name}' created successfully");
                            
                            var path = ProfileManager.GetProfilePath(name);
                            AnsiConsole.MarkupLine($"[dim]Location:[/] {path}");
                            
                            if (!string.IsNullOrEmpty(description))
                            {
                                // Save description to a file
                                var descFile = Path.Combine(path, "profile.txt");
                                try
                                {
                                    File.WriteAllText(descFile, description);
                                    AnsiConsole.MarkupLine($"[dim]Description saved[/]");
                                }
                                catch { }
                            }

                            if (!string.IsNullOrEmpty(from))
                            {
                                AnsiConsole.MarkupLine($"[dim]Copied from:[/] [cyan]{from}[/]");
                            }
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[red]✗[/] Failed to create profile '{name}'");
                        }
                    });
            }, nameArg, sourceOption, descriptionOption);

            // profile delete
            var deleteCommand = new Command("delete", "Delete a profile");
            var deleteNameArg = new Argument<string>("name", "Name of the profile to delete");
            var forceOption = new Option<bool>("--force", () => false, "Skip confirmation prompt");
            deleteCommand.AddArgument(deleteNameArg);
            deleteCommand.AddOption(forceOption);
            deleteCommand.SetHandler((string name, bool force) =>
            {
                if (name == ProfileManager.DEFAULT_PROFILE)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Cannot delete the default profile");
                    return;
                }

                if (!ProfileManager.ProfileExists(name))
                {
                    AnsiConsole.MarkupLine($"[yellow]![/] Profile '{name}' does not exist");
                    return;
                }
                
                if (!force)
                {
                    var path = ProfileManager.GetProfilePath(name);
                    AnsiConsole.MarkupLine($"[yellow]Warning:[/] This will permanently delete the profile and all its configurations.");
                    AnsiConsole.MarkupLine($"[dim]Path:[/] {path}");
                    
                    if (!AnsiConsole.Confirm($"Are you sure you want to delete profile '{name}'?", false))
                    {
                        AnsiConsole.MarkupLine("[dim]Operation cancelled[/]");
                        return;
                    }
                }
                
                if (ProfileManager.DeleteProfile(name))
                {
                    AnsiConsole.MarkupLine($"[green]✓[/] Profile '{name}' deleted successfully");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to delete profile '{name}'");
                }
            }, deleteNameArg, forceOption);

            // profile show
            var showCommand = new Command("show", "Show detailed profile information");
            var showNameArg = new Argument<string>("name", () => ProfileManager.DEFAULT_PROFILE, "Name of the profile");
            showCommand.AddArgument(showNameArg);
            showCommand.SetHandler((string name) =>
            {
                if (!ProfileManager.ProfileExists(name))
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Profile '{name}' does not exist");
                    AnsiConsole.MarkupLine($"[dim]Available profiles:[/]");
                    foreach (var p in ProfileManager.ListProfiles())
                    {
                        AnsiConsole.MarkupLine($"  • [cyan]{p}[/]");
                    }
                    return;
                }
                
                var path = ProfileManager.GetProfilePath(name);
                var isDefault = name == ProfileManager.DEFAULT_PROFILE;
                
                // Profile header
                var panel = new Panel(new Markup($"[bold cyan]{name}[/]" + (isDefault ? " [dim](default)[/]" : "")))
                {
                    Border = BoxBorder.Rounded,
                    Padding = new Padding(1, 0)
                };
                AnsiConsole.Write(panel);
                AnsiConsole.WriteLine();

                // Profile details
                var table = new Table();
                table.Border = TableBorder.None;
                table.HideHeaders();
                table.AddColumn(new TableColumn("").Width(15));
                table.AddColumn(new TableColumn(""));

                table.AddRow("[bold]Status:[/]", "[green]Active[/]");
                table.AddRow("[bold]Location:[/]", $"[dim]{path}[/]");
                
                // Check for description
                var descFile = Path.Combine(path, "profile.txt");
                if (File.Exists(descFile))
                {
                    try
                    {
                        var desc = File.ReadAllText(descFile).Trim();
                        if (!string.IsNullOrEmpty(desc))
                        {
                            table.AddRow("[bold]Description:[/]", desc);
                        }
                    }
                    catch { }
                }

                // Count configuration files
                if (Directory.Exists(path))
                {
                    var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
                    var configFiles = files.Where(f => 
                        f.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".config", StringComparison.OrdinalIgnoreCase)).ToArray();
                    
                    table.AddRow("[bold]Total Files:[/]", files.Length.ToString());
                    table.AddRow("[bold]Config Files:[/]", configFiles.Length.ToString());
                    
                    var size = files.Sum(f => new FileInfo(f).Length);
                    table.AddRow("[bold]Total Size:[/]", FormatBytes(size));
                    
                    var lastModified = files.Any() 
                        ? files.Max(f => new FileInfo(f).LastWriteTime)
                        : Directory.GetLastWriteTime(path);
                    table.AddRow("[bold]Last Modified:[/]", lastModified.ToString("yyyy-MM-dd HH:mm:ss"));
                }

                AnsiConsole.Write(table);

                // List configuration files
                if (Directory.Exists(path))
                {
                    var configFiles = Directory.GetFiles(path, "*.json", SearchOption.TopDirectoryOnly);
                    if (configFiles.Any())
                    {
                        AnsiConsole.WriteLine();
                        AnsiConsole.MarkupLine("[bold]Configuration Files:[/]");
                        
                        var tree = new Tree("📁 [cyan]" + name + "[/]");
                        foreach (var file in configFiles.OrderBy(f => f))
                        {
                            var fileName = Path.GetFileName(file);
                            var fileSize = new FileInfo(file).Length;
                            tree.AddNode($"📄 {fileName} [dim]({FormatBytes(fileSize)})[/]");
                        }
                        
                        AnsiConsole.Write(tree);
                    }
                }
            }, showNameArg);

            // profile rename
            var renameCommand = new Command("rename", "Rename an existing profile");
            var oldNameArg = new Argument<string>("current-name", "Current profile name");
            var newNameArg = new Argument<string>("new-name", "New profile name");
            renameCommand.AddArgument(oldNameArg);
            renameCommand.AddArgument(newNameArg);
            renameCommand.SetHandler((string oldName, string newName) =>
            {
                if (oldName == ProfileManager.DEFAULT_PROFILE)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Cannot rename the default profile");
                    return;
                }

                if (!ProfileManager.ProfileExists(oldName))
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Profile '{oldName}' does not exist");
                    return;
                }

                if (ProfileManager.ProfileExists(newName))
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Profile '{newName}' already exists");
                    return;
                }

                try
                {
                    var oldPath = ProfileManager.GetProfilePath(oldName);
                    var newPath = ProfileManager.GetProfilePath(newName);
                    
                    Directory.Move(oldPath, newPath);
                    AnsiConsole.MarkupLine($"[green]✓[/] Profile renamed from '{oldName}' to '{newName}'");
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to rename profile: {ex.Message}");
                }
            }, oldNameArg, newNameArg);

            // profile export
            var exportCommand = new Command("export", "Export profile to a zip file");
            var exportNameArg = new Argument<string>("name", "Profile name to export");
            var exportPathArg = new Argument<string>("output-path", "Output file path");
            exportCommand.AddArgument(exportNameArg);
            exportCommand.AddArgument(exportPathArg);
            exportCommand.SetHandler((string name, string outputPath) =>
            {
                if (!ProfileManager.ProfileExists(name))
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Profile '{name}' does not exist");
                    return;
                }

                try
                {
                    var profilePath = ProfileManager.GetProfilePath(name);
                    
                    // Ensure output has .zip extension
                    if (!outputPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        outputPath += ".zip";
                    }

                    AnsiConsole.Status()
                        .Start($"Exporting profile '{name}'...", ctx =>
                        {
                            System.IO.Compression.ZipFile.CreateFromDirectory(profilePath, outputPath);
                        });

                    AnsiConsole.MarkupLine($"[green]✓[/] Profile exported successfully");
                    AnsiConsole.MarkupLine($"[dim]Location:[/] {outputPath}");
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Export failed: {ex.Message}");
                }
            }, exportNameArg, exportPathArg);

            // profile import
            var importCommand = new Command("import", "Import profile from a zip file");
            var importPathArg = new Argument<string>("zip-path", "Path to zip file");
            var importNameArg = new Argument<string>("profile-name", "Name for the imported profile");
            importCommand.AddArgument(importPathArg);
            importCommand.AddArgument(importNameArg);
            importCommand.SetHandler((string zipPath, string profileName) =>
            {
                if (!File.Exists(zipPath))
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] File not found: {zipPath}");
                    return;
                }

                if (ProfileManager.ProfileExists(profileName))
                {
                    AnsiConsole.MarkupLine($"[yellow]![/] Profile '{profileName}' already exists");
                    
                    if (!AnsiConsole.Confirm($"Do you want to overwrite it?", false))
                        return;
                    
                    ProfileManager.DeleteProfile(profileName);
                }

                try
                {
                    var profilePath = ProfileManager.GetProfilePath(profileName);
                    
                    AnsiConsole.Status()
                        .Start($"Importing profile '{profileName}'...", ctx =>
                        {
                            System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, profilePath);
                        });

                    AnsiConsole.MarkupLine($"[green]✓[/] Profile imported successfully as '{profileName}'");
                    AnsiConsole.MarkupLine($"[dim]Location:[/] {profilePath}");
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Import failed: {ex.Message}");
                }
            }, importPathArg, importNameArg);

            // profile clean
            var cleanCommand = new Command("clean", "Remove empty or invalid profiles");
            var dryRunOption = new Option<bool>("--dry-run", () => false, "Show what would be deleted without actually deleting");
            cleanCommand.AddOption(dryRunOption);
            cleanCommand.SetHandler((bool dryRun) =>
            {
                var profiles = ProfileManager.ListProfiles();
                var emptyProfiles = profiles.Where(p => 
                {
                    if (p == ProfileManager.DEFAULT_PROFILE) return false;
                    
                    var path = ProfileManager.GetProfilePath(p);
                    if (!Directory.Exists(path)) return true;
                    
                    var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
                    return files.Length == 0;
                }).ToArray();

                if (!emptyProfiles.Any())
                {
                    AnsiConsole.MarkupLine("[green]✓[/] No empty profiles found");
                    return;
                }

                AnsiConsole.MarkupLine($"[yellow]Found {emptyProfiles.Length} empty profile(s):[/]");
                foreach (var profile in emptyProfiles)
                {
                    AnsiConsole.MarkupLine($"  • [cyan]{profile}[/]");
                }

                if (dryRun)
                {
                    AnsiConsole.MarkupLine($"[dim](Dry run - no profiles deleted)[/]");
                    return;
                }

                if (!AnsiConsole.Confirm("Delete these profiles?", false))
                {
                    AnsiConsole.MarkupLine("[dim]Operation cancelled[/]");
                    return;
                }

                int deleted = 0;
                foreach (var profile in emptyProfiles)
                {
                    if (ProfileManager.DeleteProfile(profile))
                        deleted++;
                }

                AnsiConsole.MarkupLine($"[green]✓[/] Deleted {deleted} profile(s)");
            }, dryRunOption);

            profileCommand.AddCommand(listCommand);
            profileCommand.AddCommand(createCommand);
            profileCommand.AddCommand(deleteCommand);
            profileCommand.AddCommand(showCommand);
            profileCommand.AddCommand(renameCommand);
            profileCommand.AddCommand(exportCommand);
            profileCommand.AddCommand(importCommand);
            profileCommand.AddCommand(cleanCommand);

            return profileCommand;
        }

        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }
    }
}
