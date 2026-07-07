// Example 09 — DevEx / CI automation
// Demonstrates running the schema lint gate, producing a plan diff, and exporting an
// artifact bundle for CI consumption.
//
// Phases covered: Phase 9 (DevEx & CI/CD Automation)

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.BeepSync;

namespace TheTechIdea.Beep.Editor.BeepSync.Examples
{
    /// <summary>
    /// Phase 9 — CI-style validation: lint gate + plan diff + artifact bundle.
    /// </summary>
    public static class Example09_DevExCicd
    {
        public static async Task<SyncCiValidationReport> RunAsync(
            IDMEEditor editor,
            DataSyncSchema schema,
            string artifactDirectory,
            CancellationToken cancellationToken = default)
        {
            if (editor == null) throw new ArgumentNullException(nameof(editor));
            if (schema == null) throw new ArgumentNullException(nameof(schema));
            if (string.IsNullOrWhiteSpace(artifactDirectory))
                throw new ArgumentException("artifactDirectory required", nameof(artifactDirectory));

            Directory.CreateDirectory(artifactDirectory);

            // ── 1. Create the manager ──────────────────────────────────────────────
            var syncManager = new BeepSyncManager(editor);

            // ── 2. Run the CI validation: lint + diff + bundle ────────────────────
            SyncCiValidationReport report = await syncManager
                .ValidatePlanForCiAsync(schema, artifactDirectory, cancellationToken)
                .ConfigureAwait(false);

            // ── 3. Branch on the result ──────────────────────────────────────────
            if (!report.Lint.Passed)
            {
                foreach (var diag in report.Lint.Diagnostics)
                {
                    Console.WriteLine($"[{diag.Severity}] {diag.Code}: {diag.Message}");
                }
                throw new InvalidOperationException(
                    $"Schema '{report.PlanId}' failed CI lint; see diagnostics.");
            }

            Console.WriteLine(
                $"Schema '{report.PlanId}' passed CI gate. " +
                $"Artifacts at '{report.ArtifactBundlePath}' ({report.ArtifactFiles.Count} files).");

            return report;
        }
    }
}
