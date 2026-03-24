namespace TheTechIdea.Beep.Editor.BeepSync
{
    /// <summary>
    /// Performance and scale knobs for a <see cref="DataSyncSchema"/> sync run.
    /// Controls batch sizes, parallelism, rule policy mode, and caching TTLs.
    /// </summary>
    public class SyncPerformanceProfile
    {
        /// <summary>
        /// Target number of records per import batch.  Default: <c>1000</c>.
        /// </summary>
        public int BatchSize { get; set; } = 1000;

        /// <summary>
        /// Maximum degree of parallelism when <see cref="BeepSyncManager.SyncAllDataParallelAsync"/>
        /// is used, or when parallel batch execution is enabled.  Default: <c>4</c>.
        /// </summary>
        public int MaxParallelism { get; set; } = 4;

        /// <summary>
        /// Rule policy mode used for all rule evaluations during this run.
        /// <list type="bullet">
        ///   <item><c>"Safe"</c> — full depth + lifecycle enforcement (default)</item>
        ///   <item><c>"FastPath"</c> — reduced depth, no lifecycle enforcement</item>
        /// </list>
        /// </summary>
        public string RulePolicyMode { get; set; } = "Safe";

        /// <summary>
        /// How long (seconds) a cached <c>EntityDefaultsProfile</c> is considered fresh.
        /// Default: <c>300</c> (5 minutes).
        /// </summary>
        public int DefaultsCacheTtlSeconds { get; set; } = 300;

        /// <summary>
        /// When <c>true</c>, BeepSyncManager pre-loads and caches the
        /// <c>EntityDefaultsProfile</c> for the destination entity before the retry loop starts.
        /// Default: <c>true</c>.
        /// </summary>
        public bool WarmUpDefaultsProfileOnRun { get; set; } = true;

        /// <summary>
        /// When <c>true</c>, DQ rule evaluation is skipped for a record batch that had zero
        /// failures in the preceding batch.  Use with caution on non-critical paths only.
        /// Default: <c>false</c>.
        /// </summary>
        public bool SkipRulesOnCleanBatch { get; set; } = false;

        /// <summary>
        /// When <c>true</c>, <see cref="BeepSyncManager.SyncAllDataParallelAsync"/> runs
        /// eligible schemas concurrently up to <see cref="MaxParallelism"/> tasks.
        /// Default: <c>true</c>.
        /// </summary>
        public bool UseParallelBatches { get; set; } = true;

        /// <summary>
        /// Depth of the parallel batch queue before back-pressure is applied.
        /// Default: <c>8</c>.
        /// </summary>
        public int ParallelBatchQueueDepth { get; set; } = 8;
    }
}
