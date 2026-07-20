using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.SetUp;

namespace TheTechIdea.Beep.Installer.Steps
{
    /// <summary>Writes registry entries for the installation.</summary>
    public class RegistryWriteStep : ISetupStep
    {
        public string StepId => "installer.registry.write";
        public string StepName => "Write registry entries";
        public string Description => "Writes application registry keys.";
        public IReadOnlyList<string> DependsOn { get; }

        public RegistryWriteStep(string? dependsOn = null)
        {
            DependsOn = dependsOn != null ? new List<string> { dependsOn } : Array.Empty<string>();
        }

        public bool CanSkip(SetupContext context)
        {
            return context.TryGetProperty<InstallConfig>("InstallConfig")?.RegistryEntries?.Count == 0
                && !context.TryGetProperty<InstallConfig>("InstallConfig")?.Components.Any(c => (c.Selected || c.Required) && c.Registry?.Count > 0) == true;
        }

        public IErrorsInfo Validate(SetupContext context) => StepErrorHelpers.Ok("Validated.");

        public IErrorsInfo Execute(SetupContext context, IProgress<PassedArgs>? progress = null)
        {
            var config = context.TryGetProperty<InstallConfig>("InstallConfig");
            if (config == null) return StepErrorHelpers.Ok("No configuration — nothing to write.");

            var entries = new List<RegistryOperation>(config.RegistryEntries ?? Enumerable.Empty<RegistryOperation>());
            foreach (var comp in config.Components.Where(c => c.Selected || c.Required))
                entries.AddRange(comp.Registry ?? Enumerable.Empty<RegistryOperation>());

            if (entries.Count == 0)
                return StepErrorHelpers.Ok("No registry entries to write.");

            if (context.Options?.DryRun == true)
                return StepErrorHelpers.Ok($"Dry run: {entries.Count} registry value(s) would be written. Nothing was written.");

            var written = new List<RegistryOperation>();
            var perUser = InstallScope.IsPerUser(context);

            // Honor install scope: per-user → HKCU, 32-bit → WOW6432 view (A3.1).
            RegistryKey baseKey;
            try
            {
                baseKey = InstallScope.OpenBaseKey(context, config);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException)
            {
                // Writing HKLM without elevation used to surface as "step threw an unhandled
                // exception", which tells the user nothing about how to fix it.
                return StepErrorHelpers.Fail(
                    "Administrator privileges are required to write per-machine registry entries. " +
                    "Run the installer elevated, or build it for a per-user install.");
            }
            using var _baseKey = baseKey;

            // Registry writes were not registered for rollback at all, so a failure after this
            // step left the keys behind. Register against the hive actually written to.
            var rollback = context.TryGetProperty<RollbackManager>("RollbackManager");

            foreach (var entry in entries)
            {
                using var key = baseKey.CreateSubKey(entry.KeyPath);
                if (key != null)
                {
                    key.SetValue(entry.ValueName, entry.Value, entry.ValueKind);
                    written.Add(entry);
                    rollback?.RegisterRegistryWrite(entry.KeyPath, entry.ValueName, baseKey);
                }
            }

            context.Properties["RegistryEntriesWritten"] = written;
            return StepErrorHelpers.Ok($"{written.Count} registry entries written.");
        }

        public Task<IErrorsInfo> ExecuteAsync(SetupContext context, IProgress<PassedArgs>? progress = null, CancellationToken token = default)
            => Task.FromResult(Execute(context, progress));
    }
}
