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

            var written = new List<RegistryOperation>();
            // Honor install scope: per-user → HKCU, 32-bit → WOW6432 view (A3.1).
            using var baseKey = InstallScope.OpenBaseKey(context, config);
            foreach (var entry in entries)
            {
                using var key = baseKey.CreateSubKey(entry.KeyPath);
                if (key != null)
                {
                    key.SetValue(entry.ValueName, entry.Value, entry.ValueKind);
                    written.Add(entry);
                }
            }

            context.Properties["RegistryEntriesWritten"] = written;
            return StepErrorHelpers.Ok($"{written.Count} registry entries written.");
        }

        public Task<IErrorsInfo> ExecuteAsync(SetupContext context, IProgress<PassedArgs>? progress = null, CancellationToken token = default)
            => Task.FromResult(Execute(context, progress));
    }
}
