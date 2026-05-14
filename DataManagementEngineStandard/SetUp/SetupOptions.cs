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
        public bool DryRun { get; set; } = false;

        /// <summary>Skip all seeding steps (Phase 4).</summary>
        public bool SkipSeeding { get; set; } = false;

        /// <summary>Skip schema-creation steps (Phase 3) — assume schema already exists.</summary>
        public bool SkipSchema { get; set; } = false;

        /// <summary>
        /// Target environment label used by policy gates (e.g. "Development", "Staging", "Production").
        /// </summary>
        public string Environment { get; set; } = "Development";

        /// <summary>
        /// When true, any policy warning is treated as a blocking error.
        /// Recommended for production deployments.
        /// </summary>
        public bool StrictPolicyMode { get; set; } = false;

        /// <summary>
        /// File path where <see cref="SetupState"/> JSON is persisted so runs can resume
        /// after interruption.  Null = in-memory only (no checkpoint persistence).
        /// </summary>
        public string StateFilePath { get; set; }

        /// <summary>
        /// Directory path where <see cref="SetupReport"/> JSON and Markdown artifacts are
        /// written after a run.  Null = no file output.
        /// </summary>
        public string ReportOutputPath { get; set; }
    }
}
