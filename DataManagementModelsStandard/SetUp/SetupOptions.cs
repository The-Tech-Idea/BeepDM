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
    }
}
