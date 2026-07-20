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

                File.Copy(srcPath, destPath, overwrite: true);
                copied.Add(destPath);

                // Register rollback so a later step failure undoes this copy.
                (context.TryGetProperty<RollbackManager>("RollbackManager"))?.RegisterFileCreated(destPath);

                if (File.Exists(destPath))
                    totalBytes += new FileInfo(destPath).Length;
            }

            context.Properties["InstalledFiles"] = copied;
            context.Properties["TotalBytesInstalled"] = totalBytes;
            return StepErrorHelpers.Ok($"{copied.Count} files copied ({FormatBytes(totalBytes)}).");
        }

        public Task<IErrorsInfo> ExecuteAsync(SetupContext context, IProgress<PassedArgs>? progress = null, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult(Execute(context, progress));
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