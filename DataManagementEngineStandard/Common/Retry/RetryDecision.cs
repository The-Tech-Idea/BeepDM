namespace TheTechIdea.Beep.Common.Retry
{
    /// <summary>
    /// The loop's verdict on a single attempt.
    /// </summary>
    public enum RetryDecision
    {
        /// <summary>The attempt succeeded — stop the loop, return <see cref="RetryResult{T}.Value"/>.</summary>
        Succeed,

        /// <summary>Transient failure — sleep + try again (subject to <see cref="RetryPlan{T}.MaxAttempts"/>).</summary>
        Retry,

        /// <summary>Permanent failure or attempts exhausted — stop the loop, return a GiveUp result.</summary>
        GiveUp
    }
}
