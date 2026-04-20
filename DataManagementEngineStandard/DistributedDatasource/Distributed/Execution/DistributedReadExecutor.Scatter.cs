using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Distributed.Routing;
using TheTechIdea.Beep.Proxy;

namespace TheTechIdea.Beep.Distributed.Execution
{
    /// <summary>
    /// <see cref="DistributedReadExecutor"/> partial — scatter / fan-out
    /// read path. Issues the same operation against every live shard
    /// in <see cref="RoutingDecision.ShardIds"/>, applies the
    /// configured <see cref="ScatterFailurePolicy"/>, then defers
    /// shape-specific merging to the <see cref="IResultMerger"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Parallelism is capped by
    /// <see cref="DistributedDataSourceOptions.MaxScatterParallelism"/>
    /// via a <see cref="SemaphoreSlim"/>; synchronous scatters run
    /// each shard on its own <see cref="Task"/> so the cap applies
    /// uniformly to sync and async paths.
    /// </para>
    /// <para>
    /// The whole scatter is subject to
    /// <see cref="DistributedDataSourceOptions.DefaultReadDeadlineMs"/>
    /// when the caller did not supply a stricter token.
    /// </para>
    /// </remarks>
    public sealed partial class DistributedReadExecutor
    {
        /// <inheritdoc/>
        public IEnumerable<object> ExecuteScatterRows(
            RoutingDecision                          decision,
            Func<IProxyCluster, IEnumerable<object>> readOperation,
            DistributedExecutionContext              ctx)
        {
            RequireNonEmpty(decision);
            if (readOperation == null) throw new ArgumentNullException(nameof(readOperation));

            var healthy = ResolveHealthyShardsOrThrow(decision);
            NotifyFanOut(decision, healthy, ctx, "scatter-rows");

            // Capture each shard's enumerable eagerly into a list so
            // downstream best-effort drops can exclude failed shards
            // cleanly; lazy enumeration through the merger would leak
            // exceptions past the policy gate.
            var (results, _) = ScatterSync<IEnumerable<object>>(
                decision,
                healthy,
                readOperation,
                ctx);

            return _merger.MergeRows(decision, results);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<object>> ExecuteScatterRowsAsync(
            RoutingDecision                                                    decision,
            Func<IProxyCluster, CancellationToken, Task<IEnumerable<object>>>  readOperation,
            DistributedExecutionContext                                        ctx,
            CancellationToken                                                  cancellationToken = default)
        {
            RequireNonEmpty(decision);
            if (readOperation == null) throw new ArgumentNullException(nameof(readOperation));

            var healthy = ResolveHealthyShardsOrThrow(decision);
            NotifyFanOut(decision, healthy, ctx, "scatter-rows-async");

            var (results, _) = await ScatterAsync<IEnumerable<object>>(
                decision,
                healthy,
                readOperation,
                ctx,
                cancellationToken).ConfigureAwait(false);

            return _merger.MergeRows(decision, results);
        }

        /// <inheritdoc/>
        public double ExecuteScatterScalar(
            RoutingDecision             decision,
            Func<IProxyCluster, double> readOperation,
            DistributedExecutionContext ctx)
        {
            RequireNonEmpty(decision);
            if (readOperation == null) throw new ArgumentNullException(nameof(readOperation));

            var healthy = ResolveHealthyShardsOrThrow(decision);
            NotifyFanOut(decision, healthy, ctx, "scatter-scalar");

            var (results, _) = ScatterSync<double>(decision, healthy, readOperation, ctx);
            return _merger.MergeScalar(decision, results);
        }

        /// <inheritdoc/>
        public async Task<double> ExecuteScatterScalarAsync(
            RoutingDecision                                      decision,
            Func<IProxyCluster, CancellationToken, Task<double>> readOperation,
            DistributedExecutionContext                          ctx,
            CancellationToken                                    cancellationToken = default)
        {
            RequireNonEmpty(decision);
            if (readOperation == null) throw new ArgumentNullException(nameof(readOperation));

            var healthy = ResolveHealthyShardsOrThrow(decision);
            NotifyFanOut(decision, healthy, ctx, "scatter-scalar-async");

            var (results, _) = await ScatterAsync<double>(
                decision,
                healthy,
                readOperation,
                ctx,
                cancellationToken).ConfigureAwait(false);

            return _merger.MergeScalar(decision, results);
        }

        /// <inheritdoc/>
        public PagedResult ExecuteScatterPaged(
            RoutingDecision                  decision,
            Func<IProxyCluster, PagedResult> readOperation,
            int                              pageNumber,
            int                              pageSize,
            DistributedExecutionContext      ctx)
        {
            RequireNonEmpty(decision);
            if (readOperation == null) throw new ArgumentNullException(nameof(readOperation));

            var healthy = ResolveHealthyShardsOrThrow(decision);
            NotifyFanOut(decision, healthy, ctx, "scatter-paged");

            var (results, _) = ScatterSync<PagedResult>(decision, healthy, readOperation, ctx);
            return _merger.MergePaged(decision, results, pageNumber, pageSize);
        }

        // ── Shared scatter machinery ──────────────────────────────────────

        /// <summary>
        /// Phase 10 entry gate for scatter reads. Filters
        /// <paramref name="decision"/>'s shard list to the healthy
        /// subset via <see cref="IShardInvoker.FilterHealthyShards"/>
        /// and asks the resilience tier whether the ratio still meets
        /// <see cref="DistributedDataSourceOptions.MinimumHealthyShardRatio"/>.
        /// Throws <see cref="Resilience.DegradedShardSetException"/>
        /// when the gate rejects the call; returns the filtered shard
        /// list (never <c>null</c>) otherwise. Falls back to the raw
        /// decision when no resilience adapter is wired.
        /// </summary>
        private IReadOnlyList<string> ResolveHealthyShardsOrThrow(RoutingDecision decision)
        {
            var attempted = decision.ShardIds;
            var healthy   = _shards.FilterHealthyShards(attempted);
            if (healthy == null || healthy.Count == 0)
            {
                healthy = attempted;
            }
            var gate = _shards.EvaluateScatterGate(attempted, healthy);
            if (gate != null) throw gate;
            return healthy;
        }

        /// <summary>
        /// Issues <paramref name="readOperation"/> against every shard
        /// in <paramref name="targetShardIds"/> on background tasks,
        /// respecting
        /// <see cref="DistributedDataSourceOptions.MaxScatterParallelism"/>
        /// and <see cref="ScatterFailurePolicy"/>.
        /// </summary>
        private (IReadOnlyList<T> results, int failureCount) ScatterSync<T>(
            RoutingDecision             decision,
            IReadOnlyList<string>       targetShardIds,
            Func<IProxyCluster, T>      readOperation,
            DistributedExecutionContext ctx)
        {
            return ScatterAsync<T>(
                decision,
                targetShardIds,
                (cluster, _) => Task.Run(() => readOperation(cluster)),
                ctx,
                cancellationToken: CancellationToken.None).GetAwaiter().GetResult();
        }

        private async Task<(IReadOnlyList<T> results, int failureCount)> ScatterAsync<T>(
            RoutingDecision                                      decision,
            IReadOnlyList<string>                                targetShardIds,
            Func<IProxyCluster, CancellationToken, Task<T>>      readOperation,
            DistributedExecutionContext                          ctx,
            CancellationToken                                    cancellationToken)
        {
            // Phase 14: distribution-tier capacity gate is acquired
            // once per scatter so a burst of scatters does not
            // exceed MaxConcurrentDistributedCalls. The gate is
            // released via the using-scope at the end.
            using var distributedPermit = _shards.AcquireDistributedCallPermit(cancellationToken);

            int n = targetShardIds.Count;
            var results = new T[n];
            var captured = new Exception[n];

            using var linked = CreateLinkedDeadline(cancellationToken);
            using var gate   = new SemaphoreSlim(Math.Max(1, _options.MaxScatterParallelism));

            var policy = _options.ScatterFailurePolicy;
            var tasks  = new Task[n];
            var failFastCts = policy == ScatterFailurePolicy.FailFast
                              ? CancellationTokenSource.CreateLinkedTokenSource(linked.Token)
                              : null;

            try
            {
                for (int i = 0; i < n; i++)
                {
                    int index       = i;
                    string shardId  = targetShardIds[i];
                    string op       = ctx?.OperationName ?? string.Empty;

                    tasks[i] = Task.Run(async () =>
                    {
                        await gate.WaitAsync(failFastCts?.Token ?? linked.Token).ConfigureAwait(false);
                        var sw = Stopwatch.StartNew();
                        IDisposable shardPermit = null;
                        CancellationTokenSource perShardCts = null;
                        try
                        {
                            var cluster = _shards.GetShard(shardId);
                            if (cluster == null)
                            {
                                throw new InvalidOperationException(
                                    $"Shard '{shardId}' is not registered.");
                            }

                            // Phase 14: per-shard capacity + rate limit.
                            // Throws BackpressureException when exhausted;
                            // captured below so the scatter policy can
                            // decide how to react.
                            var outerToken = failFastCts?.Token ?? linked.Token;
                            shardPermit = _shards.AcquireShardCallPermit(shardId, outerToken);

                            // Phase 14: adaptive timeout. Blends caller's
                            // per-shard budget with observed p95; never
                            // shortens below the caller's fallback.
                            int adaptiveMs = _shards.ComputeShardDeadlineMs(shardId, _options.DefaultPerShardTimeoutMs);
                            CancellationToken legToken = outerToken;
                            if (adaptiveMs > 0)
                            {
                                perShardCts = CancellationTokenSource.CreateLinkedTokenSource(outerToken);
                                perShardCts.CancelAfter(adaptiveMs);
                                legToken = perShardCts.Token;
                            }

                            results[index] = await readOperation(cluster, legToken).ConfigureAwait(false);
                            sw.Stop();
                            _shards.NotifyShardSuccess(shardId, sw.Elapsed.TotalMilliseconds);
                        }
                        catch (Exception ex)
                        {
                            sw.Stop();
                            captured[index] = ex;
                            _shards.NotifyShardFailure(
                                entityName: decision.EntityName,
                                shardId:    shardId,
                                operation:  op,
                                exception:  ex);

                            if (policy == ScatterFailurePolicy.FailFast)
                            {
                                try { failFastCts?.Cancel(); } catch { /* best-effort */ }
                            }
                        }
                        finally
                        {
                            try { perShardCts?.Dispose(); } catch { /* best-effort */ }
                            try { shardPermit?.Dispose(); } catch { /* best-effort */ }
                            gate.Release();
                        }
                    }, failFastCts?.Token ?? linked.Token);
                }

                try
                {
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
                catch
                {
                    // Per-task failures are captured above; swallow
                    // to evaluate the policy once all tasks settle.
                }
            }
            finally
            {
                failFastCts?.Dispose();
            }

            return ApplyPolicy(decision, results, captured, policy);
        }

        private static (IReadOnlyList<T> results, int failureCount) ApplyPolicy<T>(
            RoutingDecision         decision,
            T[]                     results,
            Exception[]             captured,
            ScatterFailurePolicy    policy)
        {
            int failureCount = 0;
            for (int i = 0; i < captured.Length; i++)
            {
                if (captured[i] != null) failureCount++;
            }

            if (failureCount == 0)
            {
                return (results, 0);
            }

            switch (policy)
            {
                case ScatterFailurePolicy.FailFast:
                {
                    for (int i = 0; i < captured.Length; i++)
                    {
                        if (captured[i] != null) throw captured[i];
                    }
                    return (results, failureCount); // unreachable
                }

                case ScatterFailurePolicy.RequireAll:
                {
                    var errors = new List<Exception>(failureCount);
                    for (int i = 0; i < captured.Length; i++)
                    {
                        if (captured[i] != null) errors.Add(captured[i]);
                    }
                    throw new AggregateException(
                        $"Scatter read for '{decision.EntityName}' failed on {failureCount}/{captured.Length} shards.",
                        errors);
                }

                case ScatterFailurePolicy.BestEffort:
                default:
                {
                    // Defaults(T) already sit in the failed slots; the
                    // merger skips them via null checks where the
                    // type is a reference type, and treats 0 as the
                    // additive identity for scalars.
                    return (results, failureCount);
                }
            }
        }

        private void NotifyFanOut(
            RoutingDecision             decision,
            IReadOnlyList<string>       targetShardIds,
            DistributedExecutionContext ctx,
            string                      reason)
        {
            var op  = ctx?.OperationName ?? string.Empty;
            var key = FormatPartitionKey(decision);
            for (int i = 0; i < targetShardIds.Count; i++)
            {
                _shards.NotifyShardSelected(
                    entityName:   decision.EntityName,
                    shardId:      targetShardIds[i],
                    operation:    op,
                    partitionKey: key,
                    reason:       reason);
            }
        }
    }
}
