using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.SetUp;

namespace TheTechIdea.Beep.Installer.Steps
{
    /// <summary>
    /// Upgrade detection. Runs before anything touches disk: it decides whether this is a fresh
    /// install, a reinstall of the same version, an upgrade, or a downgrade — and refuses a
    /// downgrade unless <see cref="ForceInstallKey"/> is set.
    ///
    /// When an existing install is being replaced (upgrade, or a forced downgrade) it backs the
    /// current install up first via <see cref="UpgradeEngine.Backup"/> and records the backup
    /// path under <see cref="BackupPathKey"/>. The backup is only discarded by
    /// <see cref="CommitUpgradeStep"/> after verification succeeds; on failure the host restores
    /// from it. A same-version reinstall is not backed up — there is nothing to preserve.
    /// </summary>
    public class UpgradeStep : ISetupStep
    {
        /// <summary>Context key: absolute path of the pre-upgrade backup, or absent when none was taken.</summary>
        public const string BackupPathKey = "UpgradeBackupPath";

        /// <summary>Context key: version string of the install being replaced.</summary>
        public const string PreviousVersionKey = "UpgradePreviousVersion";

        /// <summary>Context key (boxed bool): set by the host on <c>/FORCE</c> to allow a downgrade.</summary>
        public const string ForceInstallKey = "ForceInstall";

        public string StepId => "installer.upgrade.detect";
        public string StepName => "Detect existing installation";
        public string Description => "Detects a previous version, refuses downgrades and backs up before upgrading.";
        public IReadOnlyList<string> DependsOn { get; }

        public UpgradeStep(string? dependsOn = null)
        {
            DependsOn = dependsOn != null ? new List<string> { dependsOn } : Array.Empty<string>();
        }

        public bool CanSkip(SetupContext context) => false;

        public IErrorsInfo Validate(SetupContext context)
        {
            if (context.TryGetProperty<InstallConfig>("InstallConfig") == null)
                return StepErrorHelpers.Fail("InstallConfig not found in context.");
            if (string.IsNullOrWhiteSpace(context.TryGetProperty<string>("InstallPath")))
                return StepErrorHelpers.Fail("InstallPath not set.");
            return StepErrorHelpers.Ok("Validated.");
        }

        public IErrorsInfo Execute(SetupContext context, IProgress<PassedArgs>? progress = null)
        {
            var config = context.TryGetProperty<InstallConfig>("InstallConfig");
            var installPath = context.TryGetProperty<string>("InstallPath");
            if (config == null || string.IsNullOrWhiteSpace(installPath))
                return StepErrorHelpers.Fail("Configuration missing.");

            var engine = new UpgradeEngine();

            // Detect through the SAME hive/view the install will register under, or a per-user
            // install written to HKCU would be invisible to a per-machine (HKLM) lookup.
            ExistingInstall? existing;
            using (var baseKey = InstallScope.OpenBaseKey(context, config))
                existing = engine.DetectExisting(config.ProductName, baseKey);

            if (existing == null)
            {
                progress?.Report(new PassedArgs { Messege = "No previous version detected — fresh install.", ParameterInt1 = 100 });
                return StepErrorHelpers.Ok("Fresh install — no previous version to upgrade.");
            }

            var newVersion = config.ProductVersion;
            var existingVersion = existing.InstalledVersion;

            var isUpgrade = engine.IsNewer(existingVersion, newVersion);   // new  > existing
            var isDowngrade = engine.IsNewer(newVersion, existingVersion); // existing > new

            if (isDowngrade)
            {
                var forced = context.Properties.TryGetValue(ForceInstallKey, out var f) && f is bool b && b;
                if (!forced)
                    return StepErrorHelpers.Fail(
                        $"A newer version ({existingVersion}) of {existing.ProductName} is already installed; " +
                        $"refusing to downgrade to {newVersion}. Re-run with /FORCE to override.");

                progress?.Report(new PassedArgs { Messege = $"Forced downgrade over {existingVersion}." });
                return BackUp(engine, context, existing, progress);
            }

            if (!isUpgrade)
            {
                // Same version: a reinstall/repair-over. Nothing to preserve, so no backup.
                progress?.Report(new PassedArgs { Messege = $"Reinstalling version {newVersion}.", ParameterInt1 = 100 });
                return StepErrorHelpers.Ok($"Same version ({newVersion}) already installed — reinstalling in place.");
            }

            progress?.Report(new PassedArgs { Messege = $"Upgrading {existingVersion} → {newVersion}." });
            return BackUp(engine, context, existing, progress);
        }

        /// <summary>Backs up the existing install and records the backup path + previous version.</summary>
        private static IErrorsInfo BackUp(UpgradeEngine engine, SetupContext context, ExistingInstall existing,
            IProgress<PassedArgs>? progress)
        {
            progress?.Report(new PassedArgs { Messege = "Backing up the current installation…" });
            var backup = engine.Backup(existing.InstallPath, CancellationToken.None);
            if (string.IsNullOrWhiteSpace(backup))
                return StepErrorHelpers.Fail(
                    $"Could not back up the existing installation at {existing.InstallPath}; aborting before any changes are made.");

            context.Properties[BackupPathKey] = backup!;
            context.Properties[PreviousVersionKey] = existing.InstalledVersion;
            progress?.Report(new PassedArgs { Messege = "Backup complete.", ParameterInt1 = 100 });
            return StepErrorHelpers.Ok($"Backed up {existing.InstalledVersion} before upgrading.");
        }

        public Task<IErrorsInfo> ExecuteAsync(SetupContext context, IProgress<PassedArgs>? progress = null, CancellationToken token = default)
            => Task.FromResult(Execute(context, progress));
    }
}
