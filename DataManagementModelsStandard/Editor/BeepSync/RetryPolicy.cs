using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.BeepSync
{
    /// <summary>
    /// Retry and error-classification policy attached to a <see cref="DataSyncSchema"/>.
    /// Drives Rule-Engine-based error triage and exponential backoff across batch retries.
    /// </summary>
    public class RetryPolicy
    {
        /// <summary>Maximum number of retry attempts for a transient error before aborting. Default 3.</summary>
        public int MaxAttempts { get; set; } = 3;

        /// <summary>
        /// Base delay in milliseconds between retry attempts.
        /// Combined with <see cref="BackoffMode"/> to compute per-attempt delay.
        /// </summary>
        public int BaseDelayMs { get; set; } = 1000;

        /// <summary>
        /// Backoff strategy applied to <see cref="BaseDelayMs"/> between retries.
        /// Values: <c>"Linear"</c>, <c>"Exponential"</c>, <c>"Fixed"</c>.
        /// </summary>
        public string BackoffMode { get; set; } = "Exponential";

        /// <summary>
        /// Rule Engine key that classifies an import error into a category/action pair.
        /// Expected outputs:
        /// <c>"category"</c> = <c>"Transient"</c> | <c>"Validation"</c> | <c>"Conflict"</c> | <c>"Fatal"</c>
        /// <c>"action"</c>   = <c>"Retry"</c> | <c>"Abort"</c> | <c>"Quarantine"</c> | <c>"Escalate"</c>
        /// </summary>
        public string ErrorCategoryRuleKey { get; set; }

        /// <summary>
        /// Error categories that must never be retried, regardless of <see cref="MaxAttempts"/>.
        /// Defaults to <c>["Fatal"]</c>.
        /// </summary>
        public List<string> NonRetryableCategories { get; set; } = new List<string> { "Fatal" };

        /// <summary>
        /// Maximum age (in hours) of a saved checkpoint before it is considered stale and unsafe
        /// to resume.  Used by the <c>sync.checkpoint.resume-safe</c> rule.
        /// </summary>
        public int MaxResumeWindowHours { get; set; } = 24;

        /// <summary>
        /// When <c>true</c>, a checkpoint is saved after each successfully committed batch,
        /// enabling mid-run resume on transient failures.
        /// </summary>
        public bool CheckpointEnabled { get; set; } = true;
    }
}
