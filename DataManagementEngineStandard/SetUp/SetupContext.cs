using System;
using System.Collections.Generic;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Migration;
using TheTechIdea.Beep.Addin;

namespace TheTechIdea.Beep.SetUp
{
    /// <summary>
    /// Mutable shared state that flows through all wizard steps.
    /// Steps read from and write to this context.
    /// </summary>
    public class SetupContext
    {
        /// <summary>The central BeepDM editor — provides ConfigEditor, assemblyHandler, data sources, etc.</summary>
        public IDMEEditor Editor { get; set; }

        /// <summary>The open datasource being configured/migrated/seeded. Populated by Phase 2.</summary>
        public IDataSource DataSource { get; set; }

        /// <summary>Wizard-level options (dry-run, skip flags, environment).</summary>
        public SetupOptions Options { get; set; } = new SetupOptions();

        /// <summary>Persisted wizard state (completed steps, schema hash, completed seeders, etc.).</summary>
        public SetupState State { get; set; } = new SetupState();

        /// <summary>Platform-specific progress reporter. Optional.</summary>
        public ISetupProgressReporter ProgressReporter { get; set; }

        // ── Phase 2 outputs ──────────────────────────────────────────────────

        /// <summary>Validated and persisted connection properties. Populated by ConnectionConfigStep.</summary>
        public ConnectionProperties ConnectionProperties { get; set; }

        // ── Phase 3 outputs ──────────────────────────────────────────────────

        /// <summary>Migration plan produced by SchemaSetupStep.</summary>
        public MigrationPlanArtifact MigrationPlan { get; set; }

        /// <summary>Migration execution result produced by SchemaSetupStep.</summary>
        public MigrationExecutionResult MigrationResult { get; set; }

        // ── Phase 4 outputs ──────────────────────────────────────────────────

        /// <summary>
        /// Seeder IDs that completed in this run.
        /// Sourced from <see cref="State"/>.CompletedSeederIds; provided here for
        /// convenient access by callers who inspect the context after the wizard completes.
        /// </summary>
        public IReadOnlyCollection<string> CompletedSeederIds =>
            State?.CompletedSeederIds ?? (IReadOnlyCollection<string>)Array.Empty<string>();

        // ── Extension bag ────────────────────────────────────────────────────

        /// <summary>Arbitrary key-value bag for steps to pass custom data forward.</summary>
        public Dictionary<string, object> Properties { get; } = new Dictionary<string, object>();
    }
}
