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

        private readonly string _sharedDllKeyPath = SharedFileCountStep.DefaultSharedDllKeyPath;

        public UninstallStep() { }

        /// <param name="sharedDllKeyPath">Override the SharedDLLs registry path (for testing).</param>
        public UninstallStep(string? sharedDllKeyPath)
        {
            if (!string.IsNullOrWhiteSpace(sharedDllKeyPath))
                _sharedDllKeyPath = sharedDllKeyPath;
        }

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
            var config = context.TryGetProperty<InstallConfig>("InstallConfig");

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

            // 2. Remove registry entries (honor the same scope/bitness used at install time — A3.1)
            if (manifest.RegistryEntries != null)
            {
                using var baseKey = InstallScope.OpenBaseKey(context, config);
                foreach (var entry in manifest.RegistryEntries)
                {
                    try
                    {
                        using var key = baseKey.OpenSubKey(entry.KeyPath, writable: true);
                        if (key?.GetValue(entry.ValueName) != null)
                        {
                            key.DeleteValue(entry.ValueName, throwOnMissingValue: false);
                            removed++;
                        }
                    }
                    catch (Exception ex) { errors.Add($"Registry {entry.KeyPath}: {ex.Message}"); }
                }
            }

            // 3. Remove installed files (reverse order — manifests last). Shared files are
            //    refcount-decremented and only deleted when the count hits zero (Track A3.2).
            var sharedSet = new HashSet<string>(manifest.SharedFiles ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
            using var sharedBase = InstallScope.OpenBaseKey(context, config);
            if (manifest.InstalledFiles != null)
            {
                foreach (var file in manifest.InstalledFiles.AsEnumerable().Reverse())
                {
                    try
                    {
                        if (sharedSet.Contains(file))
                        {
                            var (_, remove) = SharedDllRefCount.Decrement(sharedBase, file, _sharedDllKeyPath);
                            if (!remove) continue; // still referenced by another product
                        }
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

            // 4b. Unregister COM servers (reverse of ComRegistrationStep — A3.3)
            if (manifest.ComRegistrations != null && manifest.ComRegistrations.Count > 0)
            {
                using var classes = InstallScope.OpenBaseKey(context, config)
                    .OpenSubKey(@"Software\Classes", writable: true);
                if (classes != null)
                {
                    foreach (var com in manifest.ComRegistrations)
                    {
                        try
                        {
                            if (!string.IsNullOrWhiteSpace(com.Clsid))
                                classes.DeleteSubKeyTree($@"CLSID\{com.Clsid}", throwOnMissingSubKey: false);
                            if (!string.IsNullOrWhiteSpace(com.ProgId))
                                classes.DeleteSubKeyTree(com.ProgId, throwOnMissingSubKey: false);
                            removed++;
                        }
                        catch (Exception ex) { errors.Add($"COM {com.Clsid}: {ex.Message}"); }
                    }
                }
            }

            // 4c. Remove assemblies from the GAC (best-effort — A3.3)
            if (manifest.GacAssemblies != null)
            {
                var gacutil = GacInstallStep.FindGacUtil();
                foreach (var asm in manifest.GacAssemblies)
                {
                    try
                    {
                        if (gacutil == null) { errors.Add("GAC remove skipped — gacutil.exe not found"); continue; }
                        var name = !string.IsNullOrWhiteSpace(asm.StrongName) ? asm.StrongName : System.IO.Path.GetFileNameWithoutExtension(asm.Path);
                        if (!string.IsNullOrWhiteSpace(name))
                        {
                            GacInstallStep.RunGacUtil(gacutil, $"/u \"{name}\"", out _);
                            removed++;
                        }
                    }
                    catch (Exception ex) { errors.Add($"GAC {asm.Path}: {ex.Message}"); }
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
