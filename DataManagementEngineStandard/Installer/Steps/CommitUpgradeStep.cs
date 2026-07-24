using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.SetUp;

namespace TheTechIdea.Beep.Installer.Steps
{
    /// <summary>
    /// Finalises an upgrade. Runs last, only after verification has proven the new install
    /// complete: it migrates user-editable config the new payload did not carry back from the
    /// backup, then discards the backup taken by <see cref="UpgradeStep"/>.
    ///
    /// Because the backup is only removed here, any failure earlier in the run leaves it intact
    /// for the host to restore from. A fresh install (no backup) skips entirely.
    /// </summary>
    public class CommitUpgradeStep : ISetupStep
    {
        public string StepId => "installer.upgrade.commit";
        public string StepName => "Commit upgrade";
        public string Description => "Migrates user config from the backup and removes it once the upgrade is verified.";
        public IReadOnlyList<string> DependsOn { get; }

        public CommitUpgradeStep(string? dependsOn = null)
        {
            DependsOn = dependsOn != null ? new List<string> { dependsOn } : Array.Empty<string>();
        }

        /// <summary>Nothing to commit when no upgrade backup was taken (fresh or same-version install).</summary>
        public bool CanSkip(SetupContext context)
            => string.IsNullOrWhiteSpace(context.TryGetProperty<string>(UpgradeStep.BackupPathKey));

        public IErrorsInfo Validate(SetupContext context) => StepErrorHelpers.Ok("Validated.");

        public IErrorsInfo Execute(SetupContext context, IProgress<PassedArgs>? progress = null)
        {
            var backup = context.TryGetProperty<string>(UpgradeStep.BackupPathKey);
            if (string.IsNullOrWhiteSpace(backup))
                return StepErrorHelpers.Ok("No upgrade to commit.");

            var installPath = context.TryGetProperty<string>("InstallPath");
            var engine = new UpgradeEngine();

            // Carry user config (*.config/*.json/*.xml/*.ini) the new payload does not already
            // provide back into the install dir before the backup is gone.
            if (!string.IsNullOrWhiteSpace(installPath) && Directory.Exists(backup) && Directory.Exists(installPath))
            {
                progress?.Report(new PassedArgs { Messege = "Migrating user configuration…" });
                engine.MigrateUserConfig(backup!, installPath!);
            }

            try
            {
                if (Directory.Exists(backup))
                    Directory.Delete(backup!, recursive: true);
            }
            catch (Exception ex)
            {
                // The upgrade itself succeeded; a stranded backup is not worth failing the run over.
                return StepErrorHelpers.Ok($"Upgrade committed; backup could not be removed ({ex.Message}). It can be deleted manually: {backup}");
            }

            progress?.Report(new PassedArgs { Messege = "Upgrade committed.", ParameterInt1 = 100 });
            return StepErrorHelpers.Ok("Upgrade committed; previous version backup removed.");
        }

        public Task<IErrorsInfo> ExecuteAsync(SetupContext context, IProgress<PassedArgs>? progress = null, CancellationToken token = default)
            => Task.FromResult(Execute(context, progress));
    }
}
