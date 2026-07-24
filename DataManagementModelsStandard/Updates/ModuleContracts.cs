using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Updates
{
    /// <summary>A NuGet-packaged module installed with the app: its id and installed version.</summary>
    public sealed class InstalledModule
    {
        public string Id { get; set; } = "";
        public string Version { get; set; } = "";
    }

    /// <summary>The result of updating one module package.</summary>
    public sealed class ModuleUpdateOutcome
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string? NewVersion { get; set; }

        /// <summary>Path to the fetched .nupkg, when the provider surfaces it, so its hash can be verified.</summary>
        public string? PackagePath { get; set; }
    }

    /// <summary>Per-module outcome of an <c>ApplyAsync</c> run.</summary>
    public sealed class ModuleUpdateReport
    {
        public List<(string Id, string Version)> Updated { get; } = new();
        public List<(string Id, string Error)> Failed { get; } = new();
        public bool AllSucceeded => Failed.Count == 0;
    }

    /// <summary>
    /// Narrow seam over the NuGet package machinery the module channel governs. Kept minimal (list
    /// installed modules, update one to a pinned version) so <c>ModuleUpdater</c> is unit-testable
    /// without constructing the full NuGet service graph.
    /// </summary>
    public interface IModulePackageService
    {
        Task<IReadOnlyList<InstalledModule>> GetInstalledModulesAsync(string? installDirectory = null, CancellationToken ct = default);

        /// <summary>Updates (or installs) a package to an exact version. The feed pins the version — never "latest".</summary>
        Task<ModuleUpdateOutcome> UpdateModuleAsync(string packageId, string version, string? installDirectory = null, CancellationToken ct = default);
    }
}
