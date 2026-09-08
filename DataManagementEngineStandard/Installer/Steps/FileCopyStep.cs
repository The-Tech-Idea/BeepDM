using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.SetUp;

namespace TheTechIdea.Beep.Installer.Steps
{
    /// <summary>
    /// Copies installation files with per-file progress reporting.
    /// Reads <see cref="InstallConfig"/> from context properties.
    /// </summary>
    public class FileCopyStep : ISetupStep
    {
        public string StepId => "installer.files.copy";
        public string StepName => "Copy files";
        public string Description => "Copies application files to the installation directory.";
        public IReadOnlyList<string> DependsOn { get; }

        public FileCopyStep(string? dependsOn = null)
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

            // Resolve the payload root: explicit context value (Url/extracted) wins,
            // otherwise fall back to the config directory, then the exe directory.
            var payloadRoot = context.TryGetProperty<string>("PayloadRoot")
                              ?? ConfigManager.ResolvePayloadRoot(config);

            var allFiles = new List<FileCopyOperation>();
            foreach (var component in config.Components.Where(c => c.Selected || c.Required))
                allFiles.AddRange(component.Files);

            if (allFiles.Count == 0)
                return StepErrorHelpers.Ok("No files to copy.");

            if (context.Options?.DryRun == true)
            {
                progress?.Report(new PassedArgs
                {
                    Messege = $"Dry run: would copy {allFiles.Count} file(s) to {installPath}",
                    ParameterInt1 = 100
                });
                return StepErrorHelpers.Ok($"Dry run: {allFiles.Count} file(s) would be copied to '{installPath}'. Nothing was written.");
            }

            var copied = new List<string>();
            var createdByThisStep = new List<string>();
            int total = allFiles.Count;
            long totalBytes = 0;

            for (int i = 0; i < total; i++)
            {
                var op = allFiles[i];
                var srcPath = ConfigManager.ResolveSourcePath(op.SourcePath, payloadRoot);
                var destPath = Path.Combine(installPath, op.DestinationPath);
                var destDir = Path.GetDirectoryName(destPath);

                if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                    Directory.CreateDirectory(destDir);

                if (!File.Exists(srcPath))
                {
                    var msg = $"Source file not found: {op.SourcePath} (resolved: {srcPath})";
                    if (op.IsRequired)
                        return StepErrorHelpers.Fail(msg);
                    progress?.Report(new PassedArgs { Messege = $"Skipped (missing): {op.SourcePath}", ParameterInt1 = (int)((i + 1) * 100.0 / total) });
                    continue;
                }

                var existedBefore = File.Exists(destPath);

                if (File.Exists(destPath) && !op.Overwrite)
                {
                    if (op.SkipIfNewer && File.GetLastWriteTimeUtc(srcPath) <= File.GetLastWriteTimeUtc(destPath))
                        continue;
                }

                progress?.Report(new PassedArgs
                {
                    Messege = op.Description ?? Path.GetFileName(srcPath),
                    ParameterInt1 = (int)((i + 1) * 100.0 / total),
                    ParameterInt2 = i + 1,
                    ParameterString1 = srcPath
                });

                try
                {
                    File.Copy(srcPath, destPath, overwrite: true);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    // The destination is locked (in use). Elevated, we can schedule the swap for
                    // the next reboot; unelevated, PendingFileRenameOperations cannot be written and
                    // we must fail with an actionable message rather than throw mid-copy.
                    if (TrySchedulePendingReplace(srcPath, destPath))
                    {
                        context.Properties["RebootRequired"] = true;
                        progress?.Report(new PassedArgs
                        {
                            Messege = $"In use — scheduled for replacement at next reboot: {op.DestinationPath}",
                            ParameterInt1 = (int)((i + 1) * 100.0 / total)
                        });
                        continue;
                    }

                    return StepErrorHelpers.Fail(
                        $"The file '{destPath}' is in use and could not be replaced. " +
                        "Close the application using it and retry, or reboot and run the installer again.", ex);
                }


                copied.Add(destPath);
                // Only files this step brought into existence may be deleted on rollback. A copy
                // that replaced an existing file must be restored from the upgrade backup, not
                // deleted -- deleting it would remove something the machine had before we ran.
                if (!existedBefore) createdByThisStep.Add(destPath);

                // Register rollback so a later step failure undoes this copy.
                (context.TryGetProperty<RollbackManager>("RollbackManager"))?.RegisterFileCreated(destPath);

                if (File.Exists(destPath))
                    totalBytes += new FileInfo(destPath).Length;
            }

            context.Properties["InstalledFiles"] = copied;
            context.Properties["FilesCreatedByCopy"] = createdByThisStep;
            context.Properties["TotalBytesInstalled"] = totalBytes;
            return StepErrorHelpers.Ok($"{copied.Count} files copied ({FormatBytes(totalBytes)}).");
        }

        public bool SupportsRollback => true;

        /// <summary>
        /// Deletes only the files this step created, newest first so emptied directories can go
        /// with them. A file that already existed and was overwritten is deliberately left alone:
        /// the upgrade backup owns restoring those, and deleting one would take away something the
        /// machine had before the install ran.
        /// </summary>
        public Task<IErrorsInfo> RollbackAsync(SetupContext context, IProgress<PassedArgs>? progress = null, CancellationToken token = default)
        {
            var created = context.TryGetProperty<List<string>>("FilesCreatedByCopy");
            if (created == null || created.Count == 0)
                return Task.FromResult(StepErrorHelpers.Ok("No copied files to undo."));

            var removed = 0;
            foreach (var path in Enumerable.Reverse(created))
            {
                try
                {
                    if (File.Exists(path)) { File.Delete(path); removed++; }
                }
                catch (Exception ex)
                {
                    // Best-effort: a file we cannot delete must not abort the rest of the rollback.
                    progress?.Report(new PassedArgs { Messege = $"Could not remove {path}: {ex.Message}" });
                }
            }

            context.Properties["FilesCreatedByCopy"] = new List<string>();
            return Task.FromResult(StepErrorHelpers.Ok($"{removed} copied file(s) removed."));
        }

        public Task<IErrorsInfo> ExecuteAsync(SetupContext context, IProgress<PassedArgs>? progress = null, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult(Execute(context, progress));
        }

        /// <summary>
        /// Stages the new file alongside the locked destination as <c>&lt;dest&gt;.pending</c> and
        /// registers a reboot-time swap through the shared <see cref="InstallHelpers.ScheduleFileForRestart"/>
        /// (delete the locked target, then move the staged file into its place). Returns false (and
        /// cleans up the staged file) when scheduling is not possible — chiefly when unelevated,
        /// since the swap is recorded in HKLM's PendingFileRenameOperations.
        /// </summary>
        private static bool TrySchedulePendingReplace(string srcPath, string destPath)
        {
            var pending = destPath + ".pending";
            try
            {
                File.Copy(srcPath, pending, overwrite: true);

                // Two ordered PendingFileRenameOperations: remove the locked target, then rename
                // the staged file onto it. Both write to HKLM, so both need elevation.
                var scheduledDelete = InstallHelpers.ScheduleFileForRestart(destPath, null);
                var scheduledMove = InstallHelpers.ScheduleFileForRestart(pending, destPath);
                if (scheduledDelete && scheduledMove)
                    return true; // staged file stays until the reboot completes the move

                TryDelete(pending);
                return false;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                TryDelete(pending);
                return false;
            }
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { /* best effort */ }
        }

        private static string FormatBytes(long bytes) => bytes switch
        {
            >= 1_073_741_824 => $"{bytes / 1_073_741_824.0:F1} GB",
            >= 1_048_576 => $"{bytes / 1_048_576.0:F1} MB",
            >= 1024 => $"{bytes / 1024.0:F1} KB",
            _ => $"{bytes} B"
        };
    }
}