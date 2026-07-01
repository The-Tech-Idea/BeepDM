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

            var allFiles = new List<FileCopyOperation>();
            foreach (var component in config.Components.Where(c => c.Selected || c.Required))
                allFiles.AddRange(component.Files);

            if (allFiles.Count == 0)
                return StepErrorHelpers.Ok("No files to copy.");

            var copied = new List<string>();
            int total = allFiles.Count;
            long totalBytes = 0;

            for (int i = 0; i < total; i++)
            {
                var op = allFiles[i];
                var destPath = Path.Combine(installPath, op.DestinationPath);
                var destDir = Path.GetDirectoryName(destPath);

                if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                    Directory.CreateDirectory(destDir);

                if (File.Exists(destPath) && !op.Overwrite)
                {
                    if (op.SkipIfNewer && File.GetLastWriteTimeUtc(op.SourcePath) <= File.GetLastWriteTimeUtc(destPath))
                        continue;
                }

                progress?.Report(new PassedArgs
                {
                    Messege = op.Description ?? Path.GetFileName(op.SourcePath),
                    ParameterInt1 = (int)((i + 1) * 100.0 / total),
                    ParameterInt2 = i + 1,
                    ParameterString1 = op.SourcePath
                });

                File.Copy(op.SourcePath, destPath, overwrite: true);
                copied.Add(destPath);

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
