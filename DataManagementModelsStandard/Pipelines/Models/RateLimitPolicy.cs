namespace TheTechIdea.Beep.Pipelines.Models
{
    /// <summary>
    /// Controls how many times and how fast a scheduled pipeline may run.
    /// All limits default to disabled (0 = no limit).
    /// </summary>
    public class RateLimitPolicy
    {
        /// <summary>Maximum runs allowed within <see cref="WindowSeconds"/>. 0 = unlimited.</summary>
        public int MaxRuns { get; set; } = 0;

        /// <summary>Sliding window size in seconds for <see cref="MaxRuns"/> tracking (default 1 hour).</summary>
        public int WindowSeconds { get; set; } = 3_600;

        /// <summary>Minimum gap in seconds between consecutive runs. 0 = no gap enforced.</summary>
        public int MinGapSeconds { get; set; } = 0;
    }
}
