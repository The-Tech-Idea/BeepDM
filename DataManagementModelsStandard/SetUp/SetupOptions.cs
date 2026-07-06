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
    }
}
