using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Proxy
{
    public partial class ProxyDataSource
    {
        // ─────────────────────────────────────────────────────────────────────
        //  P2-13 — Write Fan-Out / Quorum Write
        //
        //  Activated when ProxyPolicy.WriteMode is FanOut or QuorumWrite.
        //  SinglePrimary (default) bypasses this entirely — no behaviour change.
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Executes <paramref name="operation"/> concurrently on all healthy Primary-role
        /// datasources.  Returns the first successful result once quorum is satisfied.
        /// Throws <see cref="AggregateException"/> if fewer than quorum Primaries succeed.
        /// </summary>
        private async Task<(bool Success, T Result, List<string> FanOutSucceeded)>
            ExecuteFanOutWriteAsync<T>(
                string operationName,
                Func<IDataSource, Task<T>> operation,
                Func<T, bool> successPredicate,
                ProxyExecutionContext ctx,
                CancellationToken cancellationToken)
        {
            var primaries = SelectWriteCandidates();
            if (primaries.Count == 0)
                throw new InvalidOperationException(
                    $"[{ctx.CorrelationId}] FanOut: no Primary sources available for {operationName}.");

            int required = _policy.WriteMode == ProxyWriteMode.QuorumWrite
                ? Math.Max(1, Math.Min(_policy.WriteFanOutQuorum, primaries.Count))
                : primaries.Count;   // FanOut = all must succeed

            // Launch all writes concurrently
            var tasks = primaries
                .Select(dsName => ExecuteSingleFanOutAsync(dsName, operationName, operation, successPredicate, ctx, cancellationToken))
                .ToList();

            var results = await Task.WhenAll(tasks).ConfigureAwait(false);

            var succeeded = results.Where(r => r.Success).ToList();
            var failed    = results.Where(r => !r.Success).ToList();

            if (succeeded.Count >= required)
            {
                var fanOutNames = succeeded.Select(r => r.DsName).ToList();
                return (true, succeeded[0].Result, fanOutNames);
            }

            var errors = failed.Select(r => r.Error).Where(e => e != null).ToList();
            throw new AggregateException(
                $"[{ctx.CorrelationId}] FanOut:{operationName} — only {succeeded.Count}/{primaries.Count} Primaries succeeded (quorum={required}).",
                errors);
        }

        private async Task<(bool Success, string DsName, T Result, Exception Error)>
            ExecuteSingleFanOutAsync<T>(
                string dsName,
                string operationName,
                Func<IDataSource, Task<T>> operation,
                Func<T, bool> successPredicate,
                ProxyExecutionContext ctx,
                CancellationToken cancellationToken)
        {
            var ds = GetPooledConnection(dsName);
            if (ds == null)
                return (false, dsName, default, new InvalidOperationException($"No connection for {dsName}"));

            var sw = Stopwatch.StartNew();
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var result = await operation(ds).ConfigureAwait(false);
                sw.Stop();

                bool ok = successPredicate == null || successPredicate(result);
                if (ok)
                {
                    RecordSuccess(dsName, sw.Elapsed);
                    ReturnConnection(dsName, ds);
                    ctx.Attempts.Add(new ProxyAttemptRecord
                    {
                        DataSourceName = dsName, AttemptNumber = 1,
                        Success = true, Duration = sw.Elapsed
                    });
                    return (true, dsName, result, null);
                }

                // Successful call but predicate rejected the result
                ctx.Attempts.Add(new ProxyAttemptRecord
                {
                    DataSourceName = dsName, AttemptNumber = 1,
                    Success = false, Duration = sw.Elapsed
                });
                return (false, dsName, default, new Exception($"Predicate rejected result from {dsName}"));
            }
            catch (Exception ex)
            {
                sw.Stop();
                var (_, severity) = ProxyErrorClassifier.Classify(ex);
                RecordFailure(dsName, severity);
                ctx.Attempts.Add(new ProxyAttemptRecord
                {
                    DataSourceName = dsName, AttemptNumber = 1,
                    Success = false, Duration = sw.Elapsed,
                    ErrorMessage = ex.Message
                });
                LogSafe($"[FanOut] Write to {dsName} failed for {operationName}.", ex);
                return (false, dsName, default, ex);
            }
        }
    }
}
