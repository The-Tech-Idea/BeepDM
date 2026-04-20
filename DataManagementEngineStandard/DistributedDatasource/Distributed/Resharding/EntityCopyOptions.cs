using System;

namespace TheTechIdea.Beep.Distributed.Resharding
{
    /// <summary>
    /// Tuning knobs for <see cref="EntityCopyService"/>. Separated from
    /// <see cref="DistributedDataSourceOptions"/> so per-reshard runs
    /// can override batch size or throttling without affecting the
    /// datasource-wide options.
    /// </summary>
    public sealed class EntityCopyOptions
    {
        /// <summary>Initialises a new options instance with defaults.</summary>
        public EntityCopyOptions()
        {
        }

        /// <summary>Rows read per <c>GetEntity</c> page during copy. Default: <c>1000</c>.</summary>
        public int CopyBatchSize { get; set; } = 1000;

        /// <summary>
        /// Upper bound on rows copied per second. <c>0</c> disables
        /// throttling. Default: <c>0</c> — operators explicitly opt in
        /// so unit tests and dev boxes run at full speed.
        /// </summary>
        public int MaxCopyRowsPerSecond { get; set; } = 0;

        /// <summary>
        /// Minimum delay between checkpoint persistences. The copy
        /// loop debounces checkpoint writes by this interval so a
        /// fast source shard does not flood the checkpoint store.
        /// Default: 1 second.
        /// </summary>
        public TimeSpan CheckpointInterval { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Maximum retry attempts for a single failed page before the
        /// copy loop surfaces the error to the caller. Default: 3.
        /// </summary>
        public int PageRetryCount { get; set; } = 3;
    }
}
