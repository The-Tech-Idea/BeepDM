namespace TheTechIdea.Beep.Common.Retry
{
    /// <summary>
    /// Per-attempt context passed to every <see cref="RetryPlan{T}"/> callback.
    /// Cheap to construct; the pipeline allocates one per attempt and reuses it for
    /// <see cref="BeforeAttempt"/> → <see cref="Run"/> → classify → <see cref="OnSuccess"/>/<see cref="OnGiveUp"/>.
    /// </summary>
    public sealed class RetryAttemptContext<TResult>
    {
        /// <summary>1-indexed attempt number.</summary>
        public int Attempt { get; init; }

        /// <summary>Result of the previous attempt, or <c>default</c> on attempt 1.</summary>
        public TResult? LastResult { get; init; }

        /// <summary>The last error (or null if the previous attempt did not produce an error object).</summary>
        public IErrorsInfo? LastError { get; init; }

        /// <summary>The last failure message, useful for keyword / rule classifiers.</summary>
        public string? FailureMessage { get; init; }
    }
}
