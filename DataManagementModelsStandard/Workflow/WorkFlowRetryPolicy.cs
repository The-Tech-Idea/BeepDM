using System;

namespace TheTechIdea.Beep.Workflow
{
    /// <summary>
    /// Retry policy for a workflow step (distinct from the pipeline-level
    /// <c>PipelineRetryPolicy</c>). Supports fixed-interval and exponential
    /// back-off strategies.
    /// </summary>
    public class WorkFlowRetryPolicy
    {
        public static WorkFlowRetryPolicy None   { get; } = new() { MaxRetries = 0 };
        public static WorkFlowRetryPolicy Once   { get; } = new() { MaxRetries = 1, DelaySeconds = 5 };

        /// <summary>Maximum number of retry attempts (0 = no retries).</summary>
        public int    MaxRetries       { get; set; } = 0;
        /// <summary>Delay in seconds before first retry.</summary>
        public double DelaySeconds     { get; set; } = 5;
        /// <summary>When true each retry doubles the delay up to <see cref="MaxDelaySeconds"/>.</summary>
        public bool   ExponentialBackoff { get; set; } = false;
        public double MaxDelaySeconds  { get; set; } = 300;

        public TimeSpan GetDelay(int attempt)
        {
            if (!ExponentialBackoff)
                return TimeSpan.FromSeconds(DelaySeconds);

            var delay = DelaySeconds * Math.Pow(2, attempt);
            return TimeSpan.FromSeconds(Math.Min(delay, MaxDelaySeconds));
        }
    }
}
