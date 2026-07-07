// Example 05 — CI Validation and Artifact Export
//
// Run CI checks and publish an evidence bundle to disk.

using System;
using System.IO;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Migration;

namespace TheTechIdea.Beep.Editor.Migration.Examples
{
    /// <summary>
    /// Phase 9 — collect the migration artifacts (plan JSON, dry-run, impact,
    /// telemetry snapshot) into a directory that CI can publish as a build
    /// artifact.
    /// </summary>
    public static class Example05_CiAndArtifacts
    {
        public static string Run(
            IDMEEditor editor,
            IDataSource dataSource,
            MigrationPlanArtifact plan,
            string outputDirectory)
        {
            if (editor == null) throw new ArgumentNullException(nameof(editor));
            if (dataSource == null) throw new ArgumentNullException(nameof(dataSource));
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (string.IsNullOrWhiteSpace(outputDirectory))
                throw new ArgumentException("outputDirectory required", nameof(outputDirectory));

            Directory.CreateDirectory(outputDirectory);

            var migrationManager = new MigrationManager(editor, dataSource);

            // ── 1. Policy gate (CI lint) ─────────────────────────────────────────
            var policy = migrationManager.EvaluateMigrationPlanPolicy(plan, new MigrationPolicyOptions
            {
                EnvironmentTier = MigrationEnvironmentTier.Test
            });
            if (policy.Decision == MigrationPolicyDecision.Block)
            {
                throw new InvalidOperationException("CI migration gates failed.");
            }

            // ── 2. Dry-run + impact + telemetry snapshot ───────────────────────
            var dryRun = migrationManager.GenerateDryRunReport(plan);
            var impact = migrationManager.BuildImpactReport(plan);
            var telemetry = migrationManager.GetMigrationTelemetrySnapshot();

            // ── 3. Write the artifact bundle ───────────────────────────────────
            string planPath     = Path.Combine(outputDirectory, "migration-plan.json");
            string dryRunPath   = Path.Combine(outputDirectory, "migration-dryrun.json");
            string impactPath   = Path.Combine(outputDirectory, "migration-impact.json");
            string telemetryPath = Path.Combine(outputDirectory, "migration-telemetry.json");

            File.WriteAllText(planPath,     ToJson(plan));
            File.WriteAllText(dryRunPath,   ToJson(dryRun));
            File.WriteAllText(impactPath,   ToJson(impact));
            File.WriteAllText(telemetryPath, ToJson(telemetry));

            Console.WriteLine($"Wrote migration artifacts to '{outputDirectory}'.");
            return outputDirectory;
        }

        private static string ToJson(object payload) => System.Text.Json.JsonSerializer.Serialize(
            payload, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }
}
