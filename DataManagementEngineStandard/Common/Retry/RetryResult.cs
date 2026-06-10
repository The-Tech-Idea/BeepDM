using System;

namespace TheTechIdea.Beep.Common.Retry
{
    /// <summary>
    /// Result of a <see cref="IRetryPipeline"/> run.
    /// </summary>
    public sealed class RetryResult<TResult>
    {
        /// <summary>
        /// The successful unit-of-work result, or <c>default</c> if the loop gave up.
        /// Check <see cref="FinalDecision"/> before reading this value.
        /// </summary>
        public TResult? Value { get; init; }

        /// <summary>Number of attempts the loop made (1..<c>MaxAttempts</c>).</summary>
        public int AttemptsUsed { get; init; }

        /// <summary>The final decision the loop reached.</summary>
        public RetryDecision FinalDecision { get; init; }

        /// <summary>Failure message, populated when <see cref="FinalDecision"/> is <see cref="RetryDecision.GiveUp"/>.</summary>
        public string? FailureMessage { get; init; }

        /// <summary>Total wall-clock time the loop spent, including sleeps and classifier overhead.</summary>
        public TimeSpan TotalElapsed { get; init; }
    }
}
