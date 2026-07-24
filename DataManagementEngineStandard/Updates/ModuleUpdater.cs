using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Versioning;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Installer;

namespace TheTechIdea.Beep.Updates
{
    /// <summary>
    /// The module channel: a thin <em>governed</em> layer over the app's existing NuGet update
    /// machinery. "Governed" means the feed pins exact versions and (optionally) hashes — the
    /// feed's version always wins over "latest on the source", and a required module that is stale
    /// blocks app start. It updates individual packages without touching the app's own files, so a
    /// module bump never reinstalls the application.
    /// </summary>
    public sealed class ModuleUpdater
    {
        private readonly IModulePackageService _packages;

        public ModuleUpdater(IModulePackageService packages)
            => _packages = packages ?? throw new ArgumentNullException(nameof(packages));

        /// <summary>
        /// Which feed modules are out of step with what is installed. Pure. The feed pins the
        /// version, so an installed version that <em>differs</em> — newer or older — is stale (a
        /// pin can deliberately hold a module back). An optional module that is not installed is
        /// left alone; a required one that is missing must be installed.
        /// </summary>
        public static IReadOnlyList<ModuleRef> ComputeStaleModules(
            IEnumerable<ModuleRef> feedModules, IEnumerable<InstalledModule> installed)
        {
            var byId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var m in installed ?? Enumerable.Empty<InstalledModule>())
                byId[m.Id] = m.Version;

            var stale = new List<ModuleRef>();
            foreach (var mod in feedModules ?? Enumerable.Empty<ModuleRef>())
            {
                if (string.IsNullOrWhiteSpace(mod.Id) || string.IsNullOrWhiteSpace(mod.Version)) continue;

                if (!byId.TryGetValue(mod.Id, out var installedVersion))
                {
                    if (mod.Required) stale.Add(mod); // required must be present; optional-and-absent is left alone
                    continue;
                }
                if (!SameVersion(installedVersion, mod.Version)) stale.Add(mod);
            }
            return stale;
        }

        /// <summary>True when any stale module is <c>required</c> — the policy layer blocks app start until it is updated.</summary>
        public static bool HasBlockingRequiredModule(IEnumerable<ModuleRef> staleModules)
            => (staleModules ?? Enumerable.Empty<ModuleRef>()).Any(m => m.Required);

        /// <summary>
        /// Updates each stale module to its feed-pinned version, verifying the fetched package's
        /// SHA-256 when the feed names one. Never touches the application's own files — only the
        /// module packages change.
        /// </summary>
        public async Task<ModuleUpdateReport> ApplyAsync(
            IEnumerable<ModuleRef> staleModules, string? installDirectory = null,
            IProgress<PassedArgs>? progress = null, CancellationToken ct = default)
        {
            var report = new ModuleUpdateReport();
            foreach (var mod in staleModules ?? Enumerable.Empty<ModuleRef>())
            {
                ct.ThrowIfCancellationRequested();
                progress?.Report(new PassedArgs { Messege = $"Updating module {mod.Id} → {mod.Version}" });

                var outcome = await _packages.UpdateModuleAsync(mod.Id, mod.Version, installDirectory, ct).ConfigureAwait(false);
                if (!outcome.Success)
                {
                    report.Failed.Add((mod.Id, outcome.Error ?? "update failed"));
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(mod.Sha256)
                    && !string.IsNullOrWhiteSpace(outcome.PackagePath) && File.Exists(outcome.PackagePath)
                    && !InstallHelpers.VerifyFileHash(outcome.PackagePath!, mod.Sha256!))
                {
                    report.Failed.Add((mod.Id, $"package failed SHA-256 verification (expected {mod.Sha256})"));
                    continue;
                }

                report.Updated.Add((mod.Id, outcome.NewVersion ?? mod.Version));
            }
            return report;
        }

        /// <summary>Lists installed modules via the underlying package service (for a check pass).</summary>
        public Task<IReadOnlyList<InstalledModule>> GetInstalledAsync(string? installDirectory = null, CancellationToken ct = default)
            => _packages.GetInstalledModulesAsync(installDirectory, ct);

        private static bool SameVersion(string a, string b)
        {
            if (NuGetVersion.TryParse(a, out var va) && NuGetVersion.TryParse(b, out var vb)) return va == vb;
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }
    }
}
