using System.Collections.Generic;

namespace TheTechIdea.Beep.SetUp
{
    /// <summary>
    /// Configuration flags that control overall wizard behaviour.
    /// Pass a <see cref="SetupOptions"/> instance to <see cref="SetupWizardBuilder.WithOptions"/>.
    /// </summary>
    public class SetupOptions
    {
        /// <summary>
        /// When true, build plans and dry-run reports but do NOT apply any changes to the
        /// datasource.  Steps should read from context and report what they would do.
        /// </summary>
        public bool DryRun { get; init; }
        public bool SkipSeeding { get; init; }
        public bool SkipSchema { get; init; }
        public string Environment { get; init; } = "Development";
        public bool StrictPolicyMode { get; init; }
        public string StateFilePath { get; init; }
        public string ReportOutputPath { get; init; }

        /// <summary>
        /// When true, a failed run automatically rolls back its completed steps in reverse.
        /// </summary>
        /// <remarks>
        /// Opt-in, not default: silently undoing a partial setup can destroy the very state a human
        /// needs to diagnose the failure. The rollback outcome is always recorded on
        /// <see cref="SetupReport.RollbackReportJson"/> regardless of this flag.
        /// </remarks>
        public bool AutoRollbackOnFailure { get; init; }

        // ── Versioned migrate-on-startup (Phase 9) ────────────────────────────

        /// <summary>
        /// When true (the default), the bootstrap upgrade pass compares the declared schema version
        /// and the entity model against the version recorded in the target database on every startup,
        /// and applies pending migrations. Set false for a locked-down deployment that requires an
        /// explicit admin action to migrate.
        /// </summary>
        public bool MigrateOnStartup { get; init; } = true;

        /// <summary>
        /// Explicit declared schema version, e.g. "2.3.0". Wins over the <see cref="AppSchemaVersionAttribute"/>
        /// on the entity assembly. When null and no attribute is present, the version gate falls back to
        /// entity-diff only (a needed migration is never blocked for lack of a declared version).
        /// </summary>
        public string DeclaredSchemaVersion { get; init; }

        /// <summary>
        /// Assembly-qualified or simple names of assemblies whose entity types feed the schema/version
        /// steps. Used by the upgrade pass to resolve the model when the app didn't pass explicit types.
        /// </summary>
        public IReadOnlyList<string> EntityAssemblies { get; init; }

        /// <summary>
        /// Explicit entity type names (full names) to migrate, resolved via the assembly handler. When
        /// set, these take priority over <see cref="EntityAssemblies"/> discovery.
        /// </summary>
        public IReadOnlyList<string> EntityTypeNames { get; init; }
    }
}
