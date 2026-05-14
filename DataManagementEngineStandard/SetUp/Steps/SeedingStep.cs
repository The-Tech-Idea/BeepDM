using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.SetUp.Seeding;

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
    public class SeedingStep : ISetupStep
    {
        private readonly SeedingStepOptions _opts;

        public SeedingStep(SeedingStepOptions opts)
        {
            _opts = opts ?? throw new ArgumentNullException(nameof(opts));
        }

        // ── ISetupStep ───────────────────────────────────────────────────────

        public string StepId => "seeding";
        public string StepName => "Seed Initial Data";
        public string Description => "Runs all registered seeders in dependency order.";
        public IReadOnlyList<string> DependsOn => new[] { "schema-setup" };

        public bool CanSkip(SetupContext context)
        {
            if (context?.Options?.SkipSeeding == true) return true;
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

            // When seeding is explicitly skipped, there's nothing to validate
            if (context.Options?.SkipSeeding == true)
                return Ok("Seeding will be skipped (SkipSeeding=true).");

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
                    continue;

                Report(progress, (int)(i * 100.0 / total),
                    $"[{i + 1}/{total}] Running seeder: {seeder.SeederName}…");

                // Idempotency guard
                if (seeder.IsAlreadySeeded(ds, editor))
                {
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
                        Report(progress, baseProgress + stepContribution, a.Messege);
                    }));

                if (result.Flag != Errors.Ok)
                {
                    PersistCompletedSeeders(context, completedIds);
                    return Fail($"Seeder '{seeder.SeederId}' failed: {result.Message}", result.Ex);
                }

                completedIds.Add(seeder.SeederId);
                PersistCompletedSeeders(context, completedIds);
            }

            Report(progress, 100, $"Seeding complete. {completedIds.Count} seeders applied.");
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
            if (context.State != null)
                context.State.CompletedSeederIds = new HashSet<string>(completedIds, StringComparer.Ordinal);
        }

        private static void Report(IProgress<PassedArgs> p, int pct, string msg) =>
            p?.Report(new PassedArgs { ParameterInt1 = pct, Messege = msg });

        private static IErrorsInfo Ok(string msg = "Ok") =>
            new ErrorsInfo { Flag = Errors.Ok, Message = msg };

        private static IErrorsInfo Fail(string msg, Exception ex = null) =>
            new ErrorsInfo { Flag = Errors.Failed, Message = msg, Ex = ex };
    }
}
