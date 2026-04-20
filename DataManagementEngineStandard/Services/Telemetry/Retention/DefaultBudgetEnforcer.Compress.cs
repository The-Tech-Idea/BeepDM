using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Services.Telemetry.Sinks;

namespace TheTechIdea.Beep.Services.Telemetry.Retention
{
    /// <summary>
    /// Compress half of <see cref="DefaultBudgetEnforcer"/>. Subscribes
    /// to <see cref="FileRollingSink.Rolled"/> and gzip-compresses every
    /// freshly rotated file when the scope's
    /// <see cref="StorageBudget.CompressOnRotate"/> flag is on, then
    /// runs an immediate enforcement pass for the directory so a sudden
    /// rotation cannot push the directory past the budget unnoticed.
    /// </summary>
    public sealed partial class DefaultBudgetEnforcer
    {
        private const string GzExtension = ".gz";

        private async void OnSinkRolled(RolledFile rolled)
        {
            if (rolled is null || string.IsNullOrEmpty(rolled.Path))
            {
                return;
            }

            try
            {
                await CompressIfNeededAsync(rolled, CancellationToken.None).ConfigureAwait(false);
                EnforcerScope scope = FindScope(Path.GetDirectoryName(rolled.Path));
                if (scope is not null)
                {
                    EnforceScope(scope, CancellationToken.None);
                }
            }
            catch
            {
                // Sink rotation handlers are async-void; never propagate.
            }
        }

        /// <inheritdoc />
        public async Task<bool> CompressIfNeededAsync(RolledFile rolled, CancellationToken cancellationToken = default)
        {
            if (rolled is null || string.IsNullOrEmpty(rolled.Path) || !File.Exists(rolled.Path))
            {
                return false;
            }
            if (rolled.Path.EndsWith(GzExtension, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            EnforcerScope scope = FindScope(Path.GetDirectoryName(rolled.Path));
            if (scope is null || scope.Budget is null || !scope.Budget.CompressOnRotate)
            {
                return false;
            }

            string targetPath = string.Concat(rolled.Path, GzExtension);
            if (File.Exists(targetPath))
            {
                TryDeleteSafe(rolled.Path);
                return false;
            }

            try
            {
                using (FileStream source = new FileStream(
                    rolled.Path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 8192, useAsync: true))
                using (FileStream destination = new FileStream(
                    targetPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, bufferSize: 8192, useAsync: true))
                using (GZipStream gz = new GZipStream(destination, CompressionLevel.Optimal, leaveOpen: false))
                {
                    await source.CopyToAsync(gz, 8192, cancellationToken).ConfigureAwait(false);
                }
            }
            catch
            {
                TryDeleteSafe(targetPath);
                return false;
            }

            TryDeleteSafe(rolled.Path);
            return true;
        }

        private static void TryDeleteSafe(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // best-effort cleanup
            }
        }
    }
}
