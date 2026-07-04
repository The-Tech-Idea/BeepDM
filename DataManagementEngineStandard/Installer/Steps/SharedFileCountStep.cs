using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.SetUp;

namespace TheTechIdea.Beep.Installer.Steps
{
    /// <summary>
    /// MSI-style shared-file reference counting (Track A3.2). For every installed file flagged
    /// <see cref="FileCopyOperation.SharedCount"/>, increments the Windows <c>SharedDLLs</c>
    /// refcount. Uninstall (<see cref="UninstallStep"/>) decrements and only deletes the file
    /// when the count reaches zero, so a DLL shared between products survives the first uninstall.
    /// Runs after <see cref="FileCopyStep"/>. Hive/view honor the active install scope (A3.1).
    /// </summary>
    public class SharedFileCountStep : ISetupStep
    {
        public string StepId => "installer.sharedfiles.count";
        public string StepName => "Count shared files";
        public string Description => "Increments SharedDLLs reference counts for shared files.";
        public IReadOnlyList<string> DependsOn { get; }

        private readonly string _sharedDllKeyPath;

        /// <summary>Default Windows SharedDLLs registry location.</summary>
        public const string DefaultSharedDllKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\SharedDLLs";

        public SharedFileCountStep(string? dependsOn = null, string? sharedDllKeyPath = null)
        {
            DependsOn = dependsOn != null ? new List<string> { dependsOn } : Array.Empty<string>();
            _sharedDllKeyPath = sharedDllKeyPath ?? DefaultSharedDllKeyPath;
        }

        public bool CanSkip(SetupContext context)
        {
            var config = context.TryGetProperty<InstallConfig>("InstallConfig");
            return config?.Components
                .Where(c => c.Selected || c.Required)
                .SelectMany(c => c.Files ?? Enumerable.Empty<FileCopyOperation>())
                .All(f => !f.SharedCount) ?? true;
        }

        public IErrorsInfo Validate(SetupContext context) => StepErrorHelpers.Ok("Validated.");

        public IErrorsInfo Execute(SetupContext context, IProgress<PassedArgs>? progress = null)
        {
            var config = context.TryGetProperty<InstallConfig>("InstallConfig");
            if (config == null) return StepErrorHelpers.Ok("No configuration — nothing to count.");

            var installed = context.TryGetProperty<List<string>>("InstalledFiles") ?? new List<string>();
            var installPath = context.TryGetProperty<string>("InstallPath") ?? "";
            var shared = new List<string>();

            // Map destination relative path → installed absolute path for quick lookup.
            var installedSet = new HashSet<string>(installed, StringComparer.OrdinalIgnoreCase);

            using var baseKey = InstallScope.OpenBaseKey(context, config);
            foreach (var comp in config.Components.Where(c => c.Selected || c.Required))
            {
                foreach (var file in comp.Files ?? Enumerable.Empty<FileCopyOperation>())
                {
                    if (!file.SharedCount) continue;
                    var abs = Path.Combine(installPath, file.DestinationPath);
                    if (!installedSet.Contains(abs) && !File.Exists(abs)) continue;

                    SharedDllRefCount.Increment(baseKey, abs, _sharedDllKeyPath);
                    shared.Add(abs);
                }
            }

            context.Properties["SharedFiles"] = shared;
            return shared.Count == 0
                ? StepErrorHelpers.Ok("No shared files.")
                : StepErrorHelpers.Ok($"RefCounted {shared.Count} shared file(s).");
        }

        public Task<IErrorsInfo> ExecuteAsync(SetupContext context, IProgress<PassedArgs>? progress = null, CancellationToken token = default)
            => Task.FromResult(Execute(context, progress));
    }

    /// <summary>Pure SharedDLLs refcount math (testable against any hive/key path).</summary>
    public static class SharedDllRefCount
    {
        /// <summary>Increments the refcount for <paramref name="filePath"/>; returns the new count.</summary>
        public static int Increment(RegistryKey hive, string filePath, string keyPath = SharedFileCountStep.DefaultSharedDllKeyPath)
        {
            using var key = hive.CreateSubKey(keyPath, writable: true);
            var current = (int?)key?.GetValue(filePath, 0) ?? 0;
            var next = current + 1;
            key?.SetValue(filePath, next, RegistryValueKind.DWord);
            return next;
        }

        /// <summary>
        /// Decrements the refcount for <paramref name="filePath"/>.
        /// Returns the resulting count and whether the caller should now remove the file
        /// (true only when the count reached zero, at which point the registry value is deleted).
        /// </summary>
        public static (int count, bool remove) Decrement(RegistryKey hive, string filePath, string keyPath = SharedFileCountStep.DefaultSharedDllKeyPath)
        {
            using var key = hive.OpenSubKey(keyPath, writable: true);
            if (key == null)
                return (0, true); // no record → treat as last reference

            var current = (int?)key.GetValue(filePath, 0) ?? 0;
            var next = current - 1;
            if (next <= 0)
            {
                key.DeleteValue(filePath, throwOnMissingValue: false);
                return (0, true);
            }
            key.SetValue(filePath, next, RegistryValueKind.DWord);
            return (next, false);
        }

        /// <summary>Reads the current refcount (0 when absent).</summary>
        public static int Get(RegistryKey hive, string filePath, string keyPath = SharedFileCountStep.DefaultSharedDllKeyPath)
        {
            using var key = hive.OpenSubKey(keyPath, writable: false);
            return (int?)key?.GetValue(filePath, 0) ?? 0;
        }
    }
}
