namespace TheTechIdea.Beep.Services.Telemetry
{
    /// <summary>
    /// Behavior selected by the telemetry pipeline when its bounded queue is full.
    /// Logs default to <see cref="DropOldest"/>; audit defaults to <see cref="Block"/>
    /// because audit events are lossless by policy.
    /// </summary>
    public enum BackpressureMode
    {
        /// <summary>
        /// New writes evict the oldest queued envelope. The dropped count is
        /// surfaced through <c>PipelineMetrics</c>. Suited for lossy logs.
        /// </summary>
        DropOldest = 0,

        /// <summary>
        /// New writes block until the queue has free capacity. Suited for
        /// audit and other lossless flows. The pipeline may escalate to
        /// <see cref="FailFast"/> if the block exceeds shutdown timeout.
        /// </summary>
        Block = 1,

        /// <summary>
        /// New writes throw immediately when the queue is full. The producer
        /// decides how to recover (typically by aborting the user action).
        /// </summary>
        FailFast = 2,
    }
}
