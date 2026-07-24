using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.SetUp;

namespace TheTechIdea.Beep.Installer.Steps
{
    /// <summary>
    /// Repairs an installation by restoring files that are missing or have been modified since
    /// install, using the staged payload as the source of truth. It converges the install toward
    /// the manifest and nothing more: intact files are left untouched (not rewritten), and files
    /// that were never staged in the payload are neither invented nor deleted.
    /// </summary>
    public class RepairFilesStep : ISetupStep
    {
        public string StepId => "installer.files.repair";
        public string StepName => "Repair files";
        public string Description => "Restores missing or modified files from the payload.";
        public IReadOnlyList<string> DependsOn { get; }

        public RepairFilesStep(string? dependsOn = null)
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

        /// <summary>A single file the repair pass will restore, and why.</summary>
        public sealed class RepairAction
        {
            /// <summary>Absolute path to the payload source.</summary>
            public string SourcePath { get; init; } = "";

            /// <summary>Install-relative destination (as authored in <see cref="FileCopyOperation.DestinationPath"/>).</summary>
            public string DestinationPath { get; init; } = "";

            /// <summary><c>"missing"</c> when the file is absent, <c>"modified"</c> when its content differs.</summary>
            public string Reason { get; init; } = "";
        }

        /// <summary>
        /// Computes which files need restoring without changing anything. A file is planned when it
        /// is absent (<c>missing</c>) or its content differs from the payload (<c>modified</c>).
        /// Files with no payload source are skipped — repair never touches what it cannot verify.
        /// </summary>
        public static IReadOnlyList<RepairAction> ComputePlan(
            IEnumerable<FileCopyOperation> operations, string payloadRoot, string installPath)
        {
            var plan = new List<RepairAction>();
            if (operations == null) return plan;

            foreach (var op in operations)
            {
                var src = ConfigManager.ResolveSourcePath(op.SourcePath, payloadRoot);
                if (string.IsNullOrWhiteSpace(src) || !File.Exists(src))
                    continue; // never staged / optional — leave the target alone

                var dest = Path.Combine(installPath, op.DestinationPath);

                string reason;
                if (!File.Exists(dest)) reason = "missing";
                else if (!ContentsMatch(src, dest)) reason = "modified";
                else continue; // intact — do not churn

                plan.Add(new RepairAction { SourcePath = src, DestinationPath = op.DestinationPath, Reason = reason });
            }

            return plan;
        }

        public IErrorsInfo Execute(SetupContext context, IProgress<PassedArgs>? progress = null)
        {
            var config = context.TryGetProperty<InstallConfig>("InstallConfig");
            var installPath = context.TryGetProperty<string>("InstallPath");
            if (config == null || string.IsNullOrWhiteSpace(installPath))
                return StepErrorHelpers.Fail("Configuration missing.");

            var payloadRoot = context.TryGetProperty<string>("PayloadRoot")
                              ?? ConfigManager.ResolvePayloadRoot(config);

            var ops = config.Components
                .Where(c => c.Selected || c.Required)
                .SelectMany(c => c.Files)
                .ToList();

            var plan = ComputePlan(ops, payloadRoot, installPath);

            if (context.Options?.DryRun == true)
            {
                progress?.Report(new PassedArgs { Messege = $"Dry run: {plan.Count} file(s) would be repaired.", ParameterInt1 = 100 });
                return StepErrorHelpers.Ok($"Dry run: {plan.Count} file(s) would be repaired. Nothing was written.");
            }

            var repaired = new List<string>();
            for (int i = 0; i < plan.Count; i++)
            {
                var action = plan[i];
                var dest = Path.Combine(installPath, action.DestinationPath);
                var destDir = Path.GetDirectoryName(dest);
                if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                    Directory.CreateDirectory(destDir);

                progress?.Report(new PassedArgs
                {
                    Messege = $"Repairing ({action.Reason}): {action.DestinationPath}",
                    ParameterInt1 = (int)((i + 1) * 100.0 / Math.Max(1, plan.Count))
                });

                File.Copy(action.SourcePath, dest, overwrite: true);
                repaired.Add(action.DestinationPath);
            }

            context.Properties["RepairedFiles"] = repaired;
            return StepErrorHelpers.Ok(repaired.Count == 0
                ? "Nothing to repair — all files are intact."
                : $"{repaired.Count} file(s) repaired.");
        }

        /// <summary>Streaming byte comparison; differing length short-circuits before reading content.</summary>
        private static bool ContentsMatch(string a, string b)
        {
            try
            {
                var fa = new FileInfo(a);
                var fb = new FileInfo(b);
                if (fa.Length != fb.Length) return false;

                using var sa = File.OpenRead(a);
                using var sb = File.OpenRead(b);
                var bufA = new byte[8192];
                var bufB = new byte[8192];
                int read;
                while ((read = sa.Read(bufA, 0, bufA.Length)) > 0)
                {
                    var readB = ReadExact(sb, bufB, read);
                    if (readB != read) return false;
                    for (int i = 0; i < read; i++)
                        if (bufA[i] != bufB[i]) return false;
                }
                return true;
            }
            catch
            {
                // If either file can't be read, treat as mismatched so repair restores it.
                return false;
            }
        }

        private static int ReadExact(Stream stream, byte[] buffer, int count)
        {
            int total = 0;
            while (total < count)
            {
                int n = stream.Read(buffer, total, count - total);
                if (n == 0) break;
                total += n;
            }
            return total;
        }

        public Task<IErrorsInfo> ExecuteAsync(SetupContext context, IProgress<PassedArgs>? progress = null, CancellationToken token = default)
            => Task.FromResult(Execute(context, progress));
    }
}
