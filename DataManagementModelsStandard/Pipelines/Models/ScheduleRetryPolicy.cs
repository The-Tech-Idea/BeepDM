namespace TheTechIdea.Beep.Pipelines.Models
{
    /// <summary>
    /// Retry policy applied to a scheduled run when it fails.
    /// Distinct from <c>PipelineRetryPolicy</c> (which is an exponential-backoff engine helper).
    /// </summary>
    public class ScheduleRetryPolicy
    {
        /// <summary>Number of additional attempts after the first failure (0 = no retries).</summary>
        public int MaxRetries { get; set; } = 0;

        /// <summary>Base delay in milliseconds before the first retry.</summary>
        public int BaseDelayMs { get; set; } = 60_000;

        /// <summary>Multiplier applied to the delay on each subsequent retry.</summary>
        public double BackoffFactor { get; set; } = 2.0;
    }
}
