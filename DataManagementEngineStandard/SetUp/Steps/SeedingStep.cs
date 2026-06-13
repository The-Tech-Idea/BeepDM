using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Extensions.Logging;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.SetUp.Seeding;
using static TheTechIdea.Beep.SetUp.StepErrorHelpers;

namespace TheTechIdea.Beep.SetUp.Steps
{
    /// <summary>
    /// Wizard step that drives all registered <see cref="ISeeder"/>s in dependency order.
    ///
    /// Behaviour
    /// ─────────
    ///  - Skips the entire step when <see cref="SetupOptions.SkipSeeding"/> is true.
    ///  - Each seeder is individually guarded by <see cref="ISeeder.IsAlreadySeeded"/>
    ///    (idempotency) and by the completed-seeder list in <see cref="SetupState"/>
    ///    (resume after partial failure).
    ///  - Partial progress is persisted to <see cref="SetupState.CompletedSeederIds"/>
    ///    after each seeder so a failed run can resume.
    /// </summary>
    public class SeedingStep : ISeedingStep
    {
        private readonly SeedingStepOptions _opts;
        private readonly ILogger<SeedingStep>? _logger;

        public SeedingStep(SeedingStepOptions opts, ILogger<SeedingStep>? logger = null)
        {
            _opts = opts ?? throw new ArgumentNullException(nameof(opts));
            _logger = logger;
        }

        // ── ISetupStep ───────────────────────────────────────────────────────

        public string StepId => "seeding";
        public string StepName => "Seed Initial Data";
        public string Description => "Runs all registered seeders in dependency order.";
        public IReadOnlyList<string> DependsOn => new[] { "schema-setup" };

        public bool CanSkip(SetupContext context)
        {
            if (context?.Options?.DryRun == true) return true;
            if (context?.Options?.SkipSeeding == true) return true;
            if (context?.Options?.SkipSchema == true) return true;
            if (_opts.Registry == null) return false;          // can't evaluate without a registry
            var ds = context?.DataSource;
            if (ds == null) return false;

            var ordered = GetFilteredSeeders();
            return ordered.Count > 0 &&
                   ordered.All(s => s.IsAlreadySeeded(ds, context.Editor));
        }

        public IErrorsInfo Validate(SetupContext context)
        {
            if (context?.Editor == null)
                return Fail("SetupContext.Editor is required.");

            // When seeding or schema is explicitly skipped, there's nothing to validate
            if (context.Options?.SkipSeeding == true)
                return Ok("Seeding will be skipped (SkipSeeding=true).");
            if (context.Options?.SkipSchema == true)
                return Ok("Seeding will be skipped (SkipSchema=true).");

            if (_opts.Registry == null)
                return Fail("SeedingStepOptions.Registry must be set.");

            if (context.DataSource == null || context.DataSource.ConnectionStatus != ConnectionState.Open)
                return Fail("DataSource must be open before SeedingStep. " +
                            "Ensure SchemaSetupStep ran successfully.");

            return Ok();
        }

        public IErrorsInfo Execute(SetupContext context, IProgress<PassedArgs> progress = null)
        {
            if (context.Options?.SkipSeeding == true)
                return Ok("Seeding skipped (SkipSeeding=true).");

            var ds = context.DataSource;
            var editor = context.Editor;
            var ordered = GetFilteredSeeders();
            int total = ordered.Count;

            if (total == 0)
                return Ok("No seeders registered.");

            var completedIds = context.State?.CompletedSeederIds != null
                ? new HashSet<string>(context.State.CompletedSeederIds, StringComparer.Ordinal)
                : new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < total; i++)
            {
                var seeder = ordered[i];

                // Already completed in a previous (partial) run — skip
                if (completedIds.Contains(seeder.SeederId))
                {
                    if (seeder.IsAlreadySeeded(ds, editor) == false)
                        _logger?.LogWarning("Seeder '{SeederId}' was marked completed but IsAlreadySeeded returns false; re-running",
                            seeder.SeederId);
                    else
                        continue;
                }

                StepErrorHelpers.Report(progress, (int)(i * 100.0 / total),
                    $"[{i + 1}/{total}] Running seeder: {seeder.SeederName}…");

                // Idempotency guard
                if (seeder.IsAlreadySeeded(ds, editor))
                {
                    _logger?.LogInformation("Seeder '{SeederId}' already applied — skipping", seeder.SeederId);
                    editor.Logger?.WriteLog(
                        $"[SeedingStep] Seeder '{seeder.SeederId}' already applied — skipping.");
                    completedIds.Add(seeder.SeederId);
                    PersistCompletedSeeders(context, completedIds);
                    continue;
                }

                var result = seeder.Seed(ds, editor,
                    new Progress<PassedArgs>(a =>
                    {
                        int baseProgress = (int)(i * 100.0 / total);
                        int stepContribution = (int)(a.ParameterInt1 / (double)total);
                        StepErrorHelpers.Report(progress, baseProgress + stepContribution, a.Messege);
                    }));

                if (result.Flag != Errors.Ok)
                {
                    PersistCompletedSeeders(context, completedIds);
                    return Fail($"Seeder '{seeder.SeederId}' failed: {result.Message}", result.Ex);
                }

                completedIds.Add(seeder.SeederId);
                PersistCompletedSeeders(context, completedIds);
            }

            StepErrorHelpers.Report(progress, 100, $"Seeding complete. {completedIds.Count} seeders applied.");
            return Ok($"Seeding complete. {completedIds.Count} seeders applied.");
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private IReadOnlyList<ISeeder> GetFilteredSeeders()
        {
            var all = _opts.Registry.GetOrderedSeeders();
            if (_opts.SeederFilter == null || _opts.SeederFilter.Count == 0)
                return all;

            var filter = new HashSet<string>(_opts.SeederFilter, StringComparer.Ordinal);
            return all.Where(s => filter.Contains(s.SeederId)).ToList().AsReadOnly();
        }

        private static void PersistCompletedSeeders(
            SetupContext context, HashSet<string> completedIds)
        {
            if (context?.State != null)
                context.State.CompletedSeederIds = new HashSet<string>(completedIds, StringComparer.Ordinal);
        }
    }
}
