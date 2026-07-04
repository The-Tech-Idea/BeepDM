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

                // Remember where the config was loaded from so relative (rebased)
                // payload paths can be resolved at runtime.
                try { config.ConfigDirectory = Path.GetDirectoryName(Path.GetFullPath(path)); }
                catch { /* path may be invalid in edge cases — leave null */ }

                var schemaWarn = CheckSchemaVersion(config);

                var errors = Validate(config);
                if (errors.Count > 0)
                    return (config, $"Validation warnings: {string.Join("; ", errors)}{schemaWarn}");

                return (config, string.IsNullOrEmpty(schemaWarn) ? null : schemaWarn.Trim());
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

            // Payload root for resolving rebased (relative) source paths.
            var payloadRoot = ResolvePayloadRoot(config);

            foreach (var comp in config.Components ?? Enumerable.Empty<InstallComponent>())
            {
                if (string.IsNullOrWhiteSpace(comp.Id))
                    warnings.Add($"Component '{comp.Name}' has no id.");
                if (comp.Files != null)
                {
                    foreach (var file in comp.Files)
                    {
                        var resolved = ResolveSourcePath(file.SourcePath, payloadRoot);
                        if (!File.Exists(resolved))
                            warnings.Add($"File not found: {file.SourcePath} (component: {comp.Name})");
                    }
                }
            }

            return warnings;
        }

        /// <summary>
        /// Returns a non-null warning string if the config's schema version is missing or
        /// does not match the current runtime contract (P0-3). Empty string when OK.
        /// </summary>
        public static string CheckSchemaVersion(InstallConfig config)
        {
            if (string.IsNullOrWhiteSpace(config.SchemaVersion))
                return $" Config has no schemaVersion — expected v{InstallConfig.CurrentSchemaVersion}.";
            if (!string.Equals(config.SchemaVersion, InstallConfig.CurrentSchemaVersion, StringComparison.OrdinalIgnoreCase))
                return $" Config schemaVersion '{config.SchemaVersion}' != runtime v{InstallConfig.CurrentSchemaVersion} — behavior may differ.";
            return "";
        }

        /// <summary>
        /// Resolves the payload root directory. Rebased source paths are relative to this.
        /// Order: config.ConfigDirectory/payloadFolder, then AppContext.BaseDirectory/payloadFolder,
        /// then AppContext.BaseDirectory.
        /// </summary>
        public static string ResolvePayloadRoot(InstallConfig config)
        {
            var folder = "payload";
            var bases = new List<string>();
            if (!string.IsNullOrWhiteSpace(config.ConfigDirectory))
                bases.Add(config.ConfigDirectory);
            bases.Add(AppContext.BaseDirectory);

            foreach (var b in bases)
            {
                var candidate = Path.Combine(b, folder);
                if (Directory.Exists(candidate)) return candidate;
            }
            return bases[0];
        }

        /// <summary>Resolves a source path: absolute paths are used as-is; relative paths resolve against the payload root.</summary>
        public static string ResolveSourcePath(string sourcePath, string payloadRoot)
        {
            var expanded = ExpandVariables(sourcePath ?? "").Replace('/', '\\').Trim();
            if (string.IsNullOrEmpty(expanded)) return expanded;
            if (Path.IsPathRooted(expanded)) return expanded;
            return Path.Combine(payloadRoot ?? AppContext.BaseDirectory, expanded);
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