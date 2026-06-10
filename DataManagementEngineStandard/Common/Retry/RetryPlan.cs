using System;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Common.Retry
{
    /// <summary>
    /// All the inputs the <see cref="IRetryPipeline"/> needs, pre-resolved by the caller.
    /// Construct one per call to <c>ExecuteAsync</c>.
    /// </summary>
    /// <remarks>
    /// Required properties use C# 11 <c>required</c> — the compiler enforces that callers
    /// set <see cref="Run"/>, <see cref="Classify"/>, and <see cref="Backoff"/> before the
    /// pipeline accepts the plan. <see cref="BeforeAttempt"/>, <see cref="OnSuccess"/>, and
    /// <see cref="OnGiveUp"/> are optional hooks.
    /// </remarks>
    public sealed class RetryPlan<TResult>
    {
        /// <summary>Total attempts including the first one. Must be ≥ 1. Default: 1 (no retry).</summary>
        public int MaxAttempts { get; init; } = 1;

        /// <summary>The work to retry. Runs once per attempt.</summary>
        public required Func<RetryAttemptContext<TResult>, CancellationToken, Task<TResult>> Run { get; init; }

        /// <summary>
        /// Classify the outcome of an attempt. Return <see cref="RetryDecision.Succeed"/>
        /// to stop on success, <see cref="RetryDecision.Retry"/> to try again, or
        /// <see cref="RetryDecision.GiveUp"/> to stop with the current failure.
        /// </summary>
        public required Func<RetryAttemptContext<TResult>, RetryDecision> Classify { get; init; }

        /// <summary>
        /// Per-attempt delay. Return <see cref="TimeSpan.Zero"/> to skip the sleep.
        /// Pipeline calls this with the current attempt number (1-indexed).
        /// </summary>
        public required Func<int, TimeSpan> Backoff { get; init; }

        /// <summary>
        /// Optional: called BEFORE every attempt (including the first). Use this to save
        /// in-progress checkpoints or record "starting attempt N" diagnostics.
        /// </summary>
        public Func<RetryAttemptContext<TResult>, CancellationToken, Task>? BeforeAttempt { get; init; }

        /// <summary>
        /// Optional: called after a successful attempt. Use this to finalize
        /// (advance watermark, mark "Completed", build a success report).
        /// </summary>
        public Func<RetryAttemptContext<TResult>, TResult, CancellationToken, Task>? OnSuccess { get; init; }

        /// <summary>
        /// Optional: called when the loop gives up (non-retryable classification, classifier
        /// said <see cref="RetryDecision.GiveUp"/>, attempts exhausted, or cancellation).
        /// Use this to mark "Failed" + record diagnostics.
        /// </summary>
        public Func<RetryAttemptContext<TResult>, TResult, RetryDecision, CancellationToken, Task>? OnGiveUp { get; init; }

        /// <summary>Logger source tag for human-readable retry messages. Default: "RetryPipeline".</summary>
        public string LoggerTag { get; init; } = "RetryPipeline";
    }
}
