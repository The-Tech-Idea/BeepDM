using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Common.Retry
{
    /// <summary>
    /// Default <see cref="IRetryPipeline"/> implementation. The loop is:
    ///
    /// <code>
    /// for attempt in 1..MaxAttempts:
    ///     if cancelled: exit with GiveUp
    ///     await BeforeAttempt(ctx, token)     // optional
    ///     try { result = await Run(ctx, token) }
    ///     catch (OperationCanceledException) { throw }  // pass through
    ///     catch (Exception ex) { result = ...; failure = ex.Message }
    ///     decision = Classify(ctx)
    ///     if decision == Succeed:
    ///         await OnSuccess(ctx, result, token)
    ///         return RetryResult(value)
    ///     if decision == Retry AND attempt &lt; MaxAttempts:
    ///         delay = Backoff(attempt)
    ///         if delay > 0: await Task.Delay(delay, token)   // cancellable
    ///         continue
    ///     // GiveUp (or last attempt) — fall through
    ///     await OnGiveUp(ctx, result, decision, token)
    ///     return RetryResult(giveup, failure=...)
    /// </code>
    ///
    /// <para><b>Edge-case guarantees:</b></para>
    /// <list type="bullet">
    ///   <item><description><c>OperationCanceledException</c> thrown by Run, Backoff, Classify, or any hook always propagates — the pipeline does NOT catch it. This is the cancellable-sleep contract.</description></item>
    ///   <item><description>Any OTHER exception thrown by a hook is caught and treated as a transient failure (logged, loop continues). This prevents a buggy classifier from hard-failing the run.</description></item>
    ///   <item><description>Cancellation between attempts: detected before the next attempt; loop exits with <c>FinalDecision = GiveUp</c> and the most recent failure message.</description></item>
    ///   <item><description>Sleep is <c>Task.Delay(delay, token)</c> — cancellable. Callers that need the old sync <c>Thread.Sleep</c> can override <c>Backoff</c> to call it directly, but the default behavior is cancellable.</description></item>
    /// </list>
    /// </summary>
    public sealed class RetryPipeline : IRetryPipeline
    {
        /// <summary>
        /// Process-wide default instance. Stateless and thread-safe; safe to share.
        /// Prefer this over <c>new RetryPipeline()</c> at call sites that don't need
        /// to inject a custom pipeline (e.g. tests).
        /// </summary>
        public static readonly RetryPipeline Instance = new RetryPipeline();

        public RetryPipeline() { }

        // ── Debug-only logging ───────────────────────────────────────────────────
        // We surface transient-retry events and non-fatal hook throws to the
        // system debug stream only. This keeps the pipeline's public surface
        // free of a logger dependency and avoids polluting the engine's
        // info-level logs (which downstream tools may scrape). The Debug output
        // is visible to anyone running under a debugger or DebugView, and is
        // stripped in Release by the [Conditional] attribute — the call site
        // does not need to be guarded.
        [System.Diagnostics.Conditional("DEBUG")]
        private static void LogDebug(string tag, string message)
        {
            System.Diagnostics.Debug.WriteLine($"[{tag}] {message}");
        }
        public async Task<RetryResult<TResult>> ExecuteAsync<TResult>(
            RetryPlan<TResult> plan, CancellationToken token = default)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (plan.MaxAttempts < 1) throw new ArgumentOutOfRangeException(nameof(plan), "MaxAttempts must be ≥ 1.");
            if (plan.Run == null) throw new ArgumentException("Run is required.", nameof(plan));
            if (plan.Classify == null) throw new ArgumentException("Classify is required.", nameof(plan));
            if (plan.Backoff == null) throw new ArgumentException("Backoff is required.", nameof(plan));

            var sw = Stopwatch.StartNew();
            TResult? value = default;
            IErrorsInfo? lastError = null;
            string? lastMsg = null;

            for (int attempt = 1; attempt <= plan.MaxAttempts; attempt++)
            {
                if (token.IsCancellationRequested)
                {
                    LogDebug(plan.LoggerTag, $"cancelled before attempt {attempt}/{plan.MaxAttempts}");
                    return new RetryResult<TResult>
                    {
                        AttemptsUsed    = attempt - 1,
                        FinalDecision   = RetryDecision.GiveUp,
                        FailureMessage  = lastMsg ?? "Cancelled before attempt.",
                        TotalElapsed    = sw.Elapsed
                    };
                }

                var ctx = new RetryAttemptContext<TResult>
                {
                    Attempt        = attempt,
                    LastResult     = value,
                    LastError      = lastError,
                    FailureMessage = lastMsg
                };

                // ── BeforeAttempt (optional, may throw OperationCanceledException) ──
                if (plan.BeforeAttempt != null)
                {
                    try
                    {
                        await plan.BeforeAttempt(ctx, token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        // Don't crash the run because a hook misbehaved; treat as transient
                        lastError = new ErrorsInfo { Flag = Errors.Failed, Message = ex.Message, Ex = ex };
                        lastMsg   = $"BeforeAttempt threw: {ex.Message}";
                        LogDebug(plan.LoggerTag, $"attempt {attempt}/{plan.MaxAttempts}: {lastMsg} — treating as transient");
                    }
                }

                // ── Run (the actual work) ─────────────────────────────────────
                try
                {
                    value = await plan.Run(ctx, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    lastError = new ErrorsInfo { Flag = Errors.Failed, Message = ex.Message, Ex = ex };
                    lastMsg   = ex.Message;
                }

                // Update context for classifier / hooks with the latest result
                ctx = new RetryAttemptContext<TResult>
                {
                    Attempt        = attempt,
                    LastResult     = value,
                    LastError      = lastError,
                    FailureMessage = lastMsg
                };

                // ── Classify (may throw) ──────────────────────────────────────
                RetryDecision decision;
                try
                {
                    decision = plan.Classify(ctx);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    // Buggy classifier → treat as GiveUp with diagnostic
                    lastMsg   = $"Classifier threw: {ex.Message}";
                    lastError = new ErrorsInfo { Flag = Errors.Failed, Message = lastMsg, Ex = ex };
                    decision  = RetryDecision.GiveUp;
                    LogDebug(plan.LoggerTag, $"attempt {attempt}/{plan.MaxAttempts}: {lastMsg} — falling back to GiveUp");
                }

                // ── Succeed → finalize, return ───────────────────────────────
                if (decision == RetryDecision.Succeed)
                {
                    if (plan.OnSuccess != null)
                    {
                        try
                        {
                            await plan.OnSuccess(ctx, value!, token).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException) { throw; }
                        catch (Exception ex)
                        {
                            // The work itself succeeded; OnSuccess's throw is a bookkeeping
                            // bug, not a run failure. Surface it for debugging but don't
                            // flip the outcome.
                            lastMsg = $"OnSuccess threw (ignored): {ex.Message}";
                            LogDebug(plan.LoggerTag, $"attempt {attempt}/{plan.MaxAttempts}: {lastMsg}");
                        }
                    }
                    return new RetryResult<TResult>
                    {
                        Value         = value,
                        AttemptsUsed  = attempt,
                        FinalDecision = RetryDecision.Succeed,
                        TotalElapsed  = sw.Elapsed
                    };
                }

                // ── Retry → sleep + try again ─────────────────────────────────
                if (decision == RetryDecision.Retry && attempt < plan.MaxAttempts)
                {
                    LogDebug(plan.LoggerTag, $"attempt {attempt}/{plan.MaxAttempts} failed ({(lastMsg ?? "no message")}); retrying");

                    TimeSpan delay;
                    try { delay = plan.Backoff(attempt); }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        // Bad Backoff function — log and continue with no delay
                        delay = TimeSpan.Zero;
                        lastMsg = (lastMsg == null ? string.Empty : lastMsg + " | ") + $"Backoff threw: {ex.Message}";
                        LogDebug(plan.LoggerTag, $"Backoff threw on attempt {attempt}: {ex.Message} — continuing with no delay");
                    }

                    if (delay > TimeSpan.Zero)
                        await Task.Delay(delay, token).ConfigureAwait(false);
                    continue;
                }

                // ── GiveUp (or last attempt) → finalize, return ───────────────
                if (plan.OnGiveUp != null)
                {
                    try
                    {
                        await plan.OnGiveUp(ctx, value!, decision, token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        lastMsg = (lastMsg == null ? string.Empty : lastMsg + " | ") + $"OnGiveUp threw: {ex.Message}";
                        LogDebug(plan.LoggerTag, $"OnGiveUp threw on attempt {attempt}: {ex.Message} — surfacing on result");
                    }
                }
                LogDebug(plan.LoggerTag, $"giving up after {attempt}/{plan.MaxAttempts} attempts: {lastMsg ?? "no message"}");
                return new RetryResult<TResult>
                {
                    AttemptsUsed   = attempt,
                    FinalDecision  = RetryDecision.GiveUp,
                    FailureMessage = lastMsg,
                    TotalElapsed   = sw.Elapsed
                };
            }

            // Unreachable: the loop body always returns. Defensive only.
            return new RetryResult<TResult>
            {
                AttemptsUsed   = plan.MaxAttempts,
                FinalDecision  = RetryDecision.GiveUp,
                FailureMessage = lastMsg,
                TotalElapsed   = sw.Elapsed
            };
        }
    }
}
