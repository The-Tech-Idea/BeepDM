namespace TheTechIdea.Beep.Services.Telemetry.Retention
{
    /// <summary>
    /// Hard cap on the total bytes a sink directory may consume on disk.
    /// The budget enforcer checks this last (after age / file count) so a
    /// breach implies the operator has not configured aggressive enough
    /// rotation or retention upstream.
    /// </summary>
    public sealed class StorageBudget
    {
        /// <summary>Default total cap (50 MB).</summary>
        public const long DefaultMaxTotalBytes = 50L * 1024 * 1024;

        /// <summary>
        /// Maximum total bytes for the directory. Set to a non-positive
        /// value to disable the cap (not recommended on mobile / browser
        /// hosts).
        /// </summary>
        public long MaxTotalBytes { get; set; } = DefaultMaxTotalBytes;

        /// <summary>
        /// When <c>true</c> (the default for logs) the enforcer compresses
        /// every <c>.ndjson</c> sibling that has just rolled. Disable on
        /// CPU-constrained hosts (MAUI / Blazor) where the gzip cost
        /// outweighs the disk savings.
        /// </summary>
        public bool CompressOnRotate { get; set; } = true;

        /// <summary>
        /// What the enforcer does when the directory exceeds
        /// <see cref="MaxTotalBytes"/>. See <see cref="BudgetBreachAction"/>
        /// for the available behaviors.
        /// </summary>
        public BudgetBreachAction OnBreach { get; set; } = BudgetBreachAction.DeleteOldest;

        /// <summary>Convenience copy used by the registration helpers.</summary>
        public StorageBudget Clone()
            => new StorageBudget
            {
                MaxTotalBytes = MaxTotalBytes,
                CompressOnRotate = CompressOnRotate,
                OnBreach = OnBreach
            };
    }
}
