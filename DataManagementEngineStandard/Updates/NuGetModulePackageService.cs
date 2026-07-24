using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Versioning;
using TheTechIdea.Beep.NuGetManagement.Services;

namespace TheTechIdea.Beep.Updates
{
    /// <summary>
    /// Adapts BeepDM's existing <see cref="UpdateService"/> to the narrow
    /// <see cref="IModulePackageService"/> the module channel governs — no new NuGet logic, just
    /// the seam. The installed inventory is read from the standard
    /// <c>&lt;installDir&gt;/&lt;packageId&gt;/&lt;version&gt;/</c> layout the installer/loader use.
    /// </summary>
    public sealed class NuGetModulePackageService : IModulePackageService
    {
        private readonly UpdateService _update;

        public NuGetModulePackageService(UpdateService update)
            => _update = update ?? throw new ArgumentNullException(nameof(update));

        public Task<IReadOnlyList<InstalledModule>> GetInstalledModulesAsync(string? installDirectory = null, CancellationToken ct = default)
        {
            var list = new List<InstalledModule>();
            if (!string.IsNullOrWhiteSpace(installDirectory) && Directory.Exists(installDirectory))
            {
                foreach (var pkgDir in Directory.GetDirectories(installDirectory))
                {
                    var id = Path.GetFileName(pkgDir);
                    var version = Directory.GetDirectories(pkgDir)
                        .Select(Path.GetFileName)
                        .Where(v => NuGetVersion.TryParse(v, out _))
                        .Select(NuGetVersion.Parse!)
                        .OrderByDescending(v => v)
                        .FirstOrDefault()?.ToNormalizedString();
                    if (!string.IsNullOrEmpty(version))
                        list.Add(new InstalledModule { Id = id, Version = version! });
                }
            }
            return Task.FromResult<IReadOnlyList<InstalledModule>>(list);
        }

        public async Task<ModuleUpdateOutcome> UpdateModuleAsync(string packageId, string version, string? installDirectory = null, CancellationToken ct = default)
        {
            var r = await _update.UpdateAsync(packageId, version, installDirectory).ConfigureAwait(false);
            return new ModuleUpdateOutcome
            {
                Success = r.Success,
                Error = r.Error,
                NewVersion = r.NewVersion
                // UpdateService does not surface the fetched .nupkg path; feed version-pinning +
                // source TLS are the v1 integrity guarantee (D11). PackagePath stays null.
            };
        }
    }
}
