using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.SetUp;

namespace TheTechIdea.Beep.Installer.Steps
{
    /// <summary>
    /// Reads the install-manifest.json, reverses all install operations, and removes the application.
    /// </summary>
    public class UninstallStep : ISetupStep
    {
        public string StepId => "installer.uninstall";
        public string StepName => "Uninstall";
        public string Description => "Removes all installed files, shortcuts, and registry entries.";
        public IReadOnlyList<string> DependsOn => Array.Empty<string>();

        public bool CanSkip(SetupContext context) => false;

        public IErrorsInfo Validate(SetupContext context)
        {
            var path = context.TryGetProperty<string>("InstallPath");
            if (string.IsNullOrWhiteSpace(path))
                return StepErrorHelpers.Fail("InstallPath not set in context.");

            var manifestPath = Path.Combine(path, "install-manifest.json");
            if (!File.Exists(manifestPath))
                return StepErrorHelpers.Fail($"Manifest not found at {manifestPath}. Cannot uninstall.");

            return StepErrorHelpers.Ok("Manifest found.");
        }

        public IErrorsInfo Execute(SetupContext context, IProgress<PassedArgs>? progress = null)
        {
            var installPath = context.TryGetProperty<string>("InstallPath")!;
            var manifestPath = Path.Combine(installPath, "install-manifest.json");

            UninstallManifest manifest;
            try
            {
                var json = File.ReadAllText(manifestPath);
                manifest = JsonSerializer.Deserialize<UninstallManifest>(json)
                    ?? new UninstallManifest();
            }
            catch (Exception ex)
            {
                return StepErrorHelpers.Fail($"Failed to read manifest: {ex.Message}");
            }

            int removed = 0;
            var errors = new List<string>();

            // 1. Remove shortcuts
            if (manifest.Shortcuts != null)
            {
                foreach (var sc in manifest.Shortcuts)
                {
                    try
                    {
                        var linkPath = GetShortcutPath(sc);
                        if (File.Exists(linkPath)) { File.Delete(linkPath); removed++; }
                    }
                    catch (Exception ex) { errors.Add($"Shortcut {sc.Name}: {ex.Message}"); }
                }
            }

            // 2. Remove registry entries
            if (manifest.RegistryEntries != null)
            {
                foreach (var entry in manifest.RegistryEntries)
                {
                    try
                    {
                        using var key = Registry.LocalMachine.OpenSubKey(entry.KeyPath, writable: true);
                        if (key?.GetValue(entry.ValueName) != null)
                        {
                            key.DeleteValue(entry.ValueName, throwOnMissingValue: false);
                            removed++;
                        }
                    }
                    catch (Exception ex) { errors.Add($"Registry {entry.KeyPath}: {ex.Message}"); }
                }
                // Remove the main key if empty
                try { Registry.LocalMachine.DeleteSubKey(@"SOFTWARE\TheTechIdea\Beep", throwOnMissingSubKey: false); }
                catch { }
            }

            // 3. Remove installed files (reverse order — manifests last)
            if (manifest.InstalledFiles != null)
            {
                foreach (var file in manifest.InstalledFiles.AsEnumerable().Reverse())
                {
                    try
                    {
                        if (File.Exists(file)) { File.Delete(file); removed++; }
                    }
                    catch (Exception ex) { errors.Add($"File {file}: {ex.Message}"); }
                }
            }

            // 4. Remove empty directories (bottom-up)
            if (manifest.CreatedDirectories != null)
            {
                foreach (var dir in manifest.CreatedDirectories.OrderByDescending(d => d.Length))
                {
                    try
                    {
                        if (Directory.Exists(dir) && !Directory.EnumerateFileSystemEntries(dir).Any())
                        {
                            Directory.Delete(dir, recursive: false);
                            removed++;
                        }
                    }
                    catch { /* not empty or access denied — leave it */ }
                }
            }

            // 5. Remove manifest itself
            try { if (File.Exists(manifestPath)) File.Delete(manifestPath); }
            catch { }

            progress?.Report(new PassedArgs { ParameterInt1 = 100, Messege = $"Removed {removed} items." });

            return errors.Count > 0
                ? StepErrorHelpers.Fail($"Uninstall completed with {errors.Count} errors: {string.Join("; ", errors)}")
                : StepErrorHelpers.Ok($"Uninstall complete. {removed} items removed.");
        }

        public Task<IErrorsInfo> ExecuteAsync(SetupContext context, IProgress<PassedArgs>? progress = null, CancellationToken token = default)
            => Task.FromResult(Execute(context, progress));

        private static string GetShortcutPath(ShortcutDefinition sc) => sc.Location switch
        {
            ShortcutLocation.Desktop => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), sc.Name + ".lnk"),
            ShortcutLocation.StartMenu => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Programs), sc.StartMenuSubfolder ?? "", sc.Name + ".lnk"),
            ShortcutLocation.Startup => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Startup), sc.Name + ".lnk"),
            _ => ""
        };
    }
}
