using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace TheTechIdea.Beep.Installer
{
    /// <summary>
    /// Persists and loads installer configurations as human-readable JSON.
    /// Supports variable substitution (%InstallPath%, %ProgramFiles%, etc.).
    /// </summary>
    public static class ConfigManager
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        /// <summary>Saves an InstallConfig to a JSON file.</summary>
        public static void Save(InstallConfig config, string path)
        {
            var json = JsonSerializer.Serialize(config, _jsonOptions);
            File.WriteAllText(path, json);
        }

        /// <summary>Loads an InstallConfig from a JSON file. Returns null with error message on failure.</summary>
        public static (InstallConfig? config, string? error) Load(string path)
        {
            if (!File.Exists(path))
                return (null, $"File not found: {path}");

            try
            {
                var json = File.ReadAllText(path);
                var config = JsonSerializer.Deserialize<InstallConfig>(json, _jsonOptions);
                if (config == null)
                    return (null, "Failed to parse config — file may be empty or invalid JSON.");

                var errors = Validate(config);
                if (errors.Count > 0)
                    return (config, $"Validation warnings: {string.Join("; ", errors)}");

                return (config, null);
            }
            catch (JsonException ex)
            {
                return (null, $"JSON error: {ex.Message} (line {ex.LineNumber})");
            }
            catch (Exception ex)
            {
                return (null, $"Error loading config: {ex.Message}");
            }
        }

        /// <summary>Validates an InstallConfig and returns warnings.</summary>
        public static List<string> Validate(InstallConfig config)
        {
            var warnings = new List<string>();

            if (string.IsNullOrWhiteSpace(config.ProductName))
                warnings.Add("productName is empty.");
            if (string.IsNullOrWhiteSpace(config.ProductVersion))
                warnings.Add("productVersion is empty.");
            if (config.Components == null || config.Components.Count == 0)
                warnings.Add("No components defined — nothing will be installed.");

            foreach (var comp in config.Components ?? Enumerable.Empty<InstallComponent>())
            {
                if (string.IsNullOrWhiteSpace(comp.Id))
                    warnings.Add($"Component '{comp.Name}' has no id.");
                if (comp.Files != null)
                {
                    foreach (var file in comp.Files)
                    {
                        if (!File.Exists(ExpandVariables(file.SourcePath)))
                            warnings.Add($"File not found: {file.SourcePath} (component: {comp.Name})");
                    }
                }
            }

            return warnings;
        }

        /// <summary>Expands %Variable% patterns in a path.</summary>
        public static string ExpandVariables(string path)
        {
            return path
                .Replace("%ProgramFiles%", Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles))
                .Replace("%AppData%", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData))
                .Replace("%LocalAppData%", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData))
                .Replace("%Desktop%", Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory))
                .Replace("%Temp%", Path.GetTempPath())
                .Replace("%UserProfile%", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        }

        /// <summary>Generates a fresh config with defaults for a given directory.</summary>
        public static InstallConfig GenerateFromDirectory(string sourceDir, string productName, string version, string publisher)
        {
            var config = new InstallConfig
            {
                ProductName = productName,
                ProductVersion = version,
                Publisher = publisher,
                DefaultInstallPath = Path.Combine("%ProgramFiles%".Replace("%ProgramFiles%",
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)), productName),
                StartMenuFolder = productName,
                Components = new List<InstallComponent>()
            };

            if (!Directory.Exists(sourceDir)) return config;

            var allFiles = Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories);
            var core = new InstallComponent
            {
                Id = "core", Name = "Core Application", Required = true, Selected = true,
                Description = "Main application files", IncludedIn = InstallationType.Typical,
                Files = new List<FileCopyOperation>()
            };

            long totalSize = 0;
            foreach (var file in allFiles)
            {
                var relPath = Path.GetRelativePath(sourceDir, file);
                var size = new FileInfo(file).Length;
                totalSize += size;
                core.Files.Add(new FileCopyOperation
                {
                    SourcePath = file, DestinationPath = relPath,
                    Description = Path.GetFileName(file), Overwrite = true
                });
            }
            core.SizeBytes = totalSize;
            config.Components.Add(core);

            var exeFile = allFiles.FirstOrDefault(f => f.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));
            if (exeFile != null)
            {
                var exeName = Path.GetFileName(exeFile);
                config.Shortcuts = new List<ShortcutDefinition>
                {
                    new() { Name = productName, TargetPath = exeName, Location = ShortcutLocation.StartMenu, StartMenuSubfolder = productName },
                    new() { Name = productName, TargetPath = exeName, Location = ShortcutLocation.Desktop }
                };
            }

            return config;
        }
    }
}
