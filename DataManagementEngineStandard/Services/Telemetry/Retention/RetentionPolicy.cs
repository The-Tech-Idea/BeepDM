namespace TheTechIdea.Beep.Services.Telemetry.Retention
{
    /// <summary>
    /// Per-directory retention rules consumed by the budget enforcer.
    /// Either or both knobs may be active; the enforcer applies them in
    /// the order documented in
    /// <see cref="DefaultBudgetEnforcer"/>.
    /// </summary>
    public sealed class RetentionPolicy
    {
        /// <summary>Default file count cap (30 files).</summary>
        public const int DefaultMaxFiles = 30;

        /// <summary>Default age cap in days (30 days).</summary>
        public const int DefaultMaxAgeDays = 30;

        /// <summary>
        /// Maximum number of files to keep in the directory. Set to
        /// <c>0</c> or a negative value to disable the cap.
        /// </summary>
        public int MaxFiles { get; set; } = DefaultMaxFiles;

        /// <summary>
        /// Maximum age of any file in the directory, in days. Set to
        /// <c>0</c> or a negative value to disable the cap.
        /// </summary>
        public int MaxAgeDays { get; set; } = DefaultMaxAgeDays;

        /// <summary>Convenience copy used by the registration helpers.</summary>
        public RetentionPolicy Clone()
            => new RetentionPolicy
            {
                MaxFiles = MaxFiles,
                MaxAgeDays = MaxAgeDays
            };
    }
}
