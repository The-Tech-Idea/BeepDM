using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Common.Retry
{
    /// <summary>
    /// Common loop for retrying a unit of work N times with a backoff delay between attempts.
    /// The "unit" is whatever the caller wants — a single migration step, a whole sync run,
    /// an HTTP call, anything. The loop is agnostic.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Typical use:
    /// </para>
    /// <code>
    /// var result = await pipeline.ExecuteAsync(new RetryPlan&lt;MyResult&gt; {
    ///     MaxAttempts = 3,
    ///     Backoff     = attempt =&gt; TimeSpan.FromMilliseconds(100 * (1 &lt;&lt; (attempt - 1))),
    ///     Classify    = ctx =&gt; ctx.LastError?.Flag == Errors.Ok
    ///                          ? RetryDecision.Succeed
    ///                          : RetryDecision.Retry,
    ///     Run         = async (ctx, token) =&gt; await DoWorkAsync(ctx.Attempt, token),
    ///     OnSuccess   = (ctx, value, token) =&gt; FinalizeAsync(value, token),
    /// }, token);
    /// </code>
    /// </remarks>
    public interface IRetryPipeline
    {
        Task<RetryResult<TResult>> ExecuteAsync<TResult>(
            RetryPlan<TResult> plan,
            CancellationToken token = default);
    }
}
