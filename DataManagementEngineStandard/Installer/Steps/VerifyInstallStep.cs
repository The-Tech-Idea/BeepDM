using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.SetUp;

namespace TheTechIdea.Beep.Installer.Steps
{
    /// <summary>Verifies the installation: checks files exist, validates manifest, reports results.</summary>
    public class VerifyInstallStep : ISetupStep
    {
        public string StepId => "installer.verify";
        public string StepName => "Verify installation";
        public string Description => "Verifies all files were copied correctly.";
        public IReadOnlyList<string> DependsOn { get; }

        public VerifyInstallStep(string? dependsOn = null)
        {
            DependsOn = dependsOn != null ? new List<string> { dependsOn } : Array.Empty<string>();
        }

        public bool CanSkip(SetupContext context) => false;

        public IErrorsInfo Validate(SetupContext context)
        {
            if (context.TryGetProperty<InstallConfig>("InstallConfig") == null)
                return StepErrorHelpers.Fail("InstallConfig not found.");
            return StepErrorHelpers.Ok("Validated.");
        }

        public IErrorsInfo Execute(SetupContext context, IProgress<PassedArgs>? progress = null)
        {
            var config = context.TryGetProperty<InstallConfig>("InstallConfig")!;
            var installPath = context.TryGetProperty<string>("InstallPath");
            if (string.IsNullOrWhiteSpace(installPath))
                return StepErrorHelpers.Fail("InstallPath not set.");

            var missing = new List<string>();
            var verified = 0;
            int total = config.Components.Sum(c => c.Files.Count);

            foreach (var comp in config.Components.Where(c => c.Selected || c.Required))
            {
                foreach (var file in comp.Files)
                {
                    var destPath = Path.Combine(installPath, file.DestinationPath);
                    if (!File.Exists(destPath))
                        missing.Add(destPath);
                    else
                        verified++;
                }
            }

            // Save uninstall manifest
            var manifest = new UninstallManifest
            {
                ProductName = config.ProductName,
                ProductVersion = config.ProductVersion,
                InstalledAt = DateTime.Now,
                InstallPath = installPath,
                InstalledFiles = context.TryGetProperty<List<string>>("InstalledFiles") ?? new List<string>(),
                CreatedDirectories = context.TryGetProperty<List<string>>("CreatedDirectories") ?? new List<string>(),
                RegistryEntries = context.TryGetProperty<List<RegistryOperation>>("RegistryEntriesWritten") ?? new List<RegistryOperation>(),
                Shortcuts = context.TryGetProperty<List<ShortcutDefinition>>("ShortcutsCreated") ?? new List<ShortcutDefinition>(),
                EnvironmentVariables = context.TryGetProperty<List<EnvironmentVariableOp>>("EnvVarsSet") ?? new List<EnvironmentVariableOp>(),
                SharedFiles = context.TryGetProperty<List<string>>("SharedFiles") ?? new List<string>(),
                ComRegistrations = context.TryGetProperty<List<ComRegistration>>("ComRegistrationsWritten") ?? new List<ComRegistration>(),
                GacAssemblies = context.TryGetProperty<List<GacAssembly>>("GacAssembliesInstalled") ?? new List<GacAssembly>()
            };

            var manifestPath = Path.Combine(installPath, "install-manifest.json");
            File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
            context.Properties["ManifestPath"] = manifestPath;

            progress?.Report(new PassedArgs { ParameterInt1 = 100, Messege = $"Verified {verified}/{total} files." });

            if (missing.Count > 0)
                return StepErrorHelpers.Fail($"Missing files: {string.Join("; ", missing)}");

            return StepErrorHelpers.Ok($"All {verified} files verified. Manifest saved.");
        }

        public Task<IErrorsInfo> ExecuteAsync(SetupContext context, IProgress<PassedArgs>? progress = null, CancellationToken token = default)
            => Task.FromResult(Execute(context, progress));
    }
}