using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Distributed.Routing;
using TheTechIdea.Beep.Proxy;

namespace TheTechIdea.Beep.Distributed.Execution
{
    /// <summary>
    /// Fan-out write path for
    /// <see cref="DistributedWriteExecutor"/>. Covers replicated
    /// writes and broadcast writes (which are replicated writes with
    /// <c>TargetShardIds = full catalog</c>). Quorum is resolved via
    /// <see cref="DistributedWriteExecutor.ResolveQuorum"/> so both
    /// plan-derived and caller-overridden policies are honoured.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Per-shard calls run in parallel, bounded by
    /// <see cref="DistributedDataSourceOptions.MaxFanOutParallelism"/>.
    /// The implementation uses <see cref="Task.Run(Action)"/> so the
    /// per-shard <c>IDataSource</c> calls (which are typically
    /// synchronous) don't block the caller's thread. Failures never
    /// cancel siblings — callers that want fail-fast semantics pick
    /// <see cref="QuorumPolicy.All"/> and treat any non-quorum outcome
    /// as failure.
    /// </para>
    /// <para>
    /// The returned <see cref="WriteOutcome"/> always contains one leg
    /// per target shard, even if the quorum was satisfied early; this
    /// keeps audit / reconciliation telemetry honest.
    /// </para>
    /// </remarks>
    public sealed partial class DistributedWriteExecutor
    {
        /// <inheritdoc/>
        public WriteOutcome ExecuteFanOut(
            RoutingDecision                  decision,
            Func<IProxyCluster, IErrorsInfo> writeOperation,
            DistributedExecutionContext      ctx,
            DistributedWriteOptions          options = null)
        {
            if (writeOperation == null) throw new ArgumentNullException(nameof(writeOperation));

            RequireNonEmpty(decision);

            // Phase 14 — distribution-tier concurrency gate. Fails
            // fast with BackpressureException when the overall cap
            // is exhausted rather than queueing unboundedly behind
            // a slow downstream.
            using var distributedPermit = _shards.AcquireDistributedCallPermit(System.Threading.CancellationToken.None);

            var operation = ctx?.OperationName ?? string.Empty;
            var attempted = decision.ShardIds;

            // Phase 10 — filter unhealthy shards and evaluate the
            // scatter gate before dispatching any leg. The per-mode
            // policy resolution (SkipShard / FailFast / DegradeScatter)
            // is delegated to ShardDownPolicyOptions and expressed
            // through the invoker's gate + filter pair: gate enforces
            // MinimumHealthyShardRatio, filter drops unhealthy shards.
            var healthy = ResolveHealthyFanOutShards(attempted);
            var gate    = _shards.EvaluateScatterGate(attempted, healthy);
            if (gate != null) throw gate;

            NotifyFanOut(decision, healthy, ctx, reason: "fan-out-write");

            var (policy, requiredAck) = ResolveQuorum(decision, options);

            WriteFanOutResult[] perShard;
            IReadOnlyList<string> skipped = CollectSkipped(attempted, healthy);
            if (skipped.Count == 0)
            {
                perShard = RunParallelLegs(
                    entityName:     decision.EntityName,
                    operation:      operation,
                    shardIds:       attempted,
                    writeOperation: writeOperation);
            }
            else
            {
                perShard = RunWithSkips(
                    entityName:     decision.EntityName,
                    operation:      operation,
                    attempted:      attempted,
                    healthy:        healthy,
                    writeOperation: writeOperation);

                _shards.NotifyPartialBroadcast(
                    entityName:        decision.EntityName,
                    operation:         operation,
                    attemptedShardIds: attempted,
                    skippedShardIds:   skipped,
                    reason:            "One or more shards were unhealthy and skipped by Phase 10 policy.");
            }

            return new WriteOutcome(
                entityName:       decision.EntityName,
                operation:        operation,
                perShard:         perShard,
                requiredAckCount: requiredAck,
                quorumPolicy:     policy);
        }

        /// <summary>
        /// Returns the shard subset the resilience tier considers
        /// healthy for dispatch. Falls back to the raw input when no
        /// resilience adapter is wired (the default invoker returns
        /// its argument unchanged).
        /// </summary>
        private IReadOnlyList<string> ResolveHealthyFanOutShards(IReadOnlyList<string> attempted)
        {
            var healthy = _shards.FilterHealthyShards(attempted);
            return healthy ?? attempted;
        }

        /// <summary>
        /// Computes the set of shards present in <paramref name="attempted"/>
        /// but absent from <paramref name="healthy"/>, preserving the
        /// original attempted order so operators see deterministic
        /// <see cref="DistributedDataSource.OnPartialBroadcast"/> payloads.
        /// </summary>
        private static IReadOnlyList<string> CollectSkipped(
            IReadOnlyList<string> attempted,
            IReadOnlyList<string> healthy)
        {
            if (attempted == null || attempted.Count == 0) return Array.Empty<string>();
            if (healthy == null || healthy.Count == 0)     return attempted;
            if (healthy.Count == attempted.Count)          return Array.Empty<string>();

            var healthySet = new HashSet<string>(healthy, StringComparer.OrdinalIgnoreCase);
            var skipped    = new List<string>(attempted.Count - healthy.Count);
            for (int i = 0; i < attempted.Count; i++)
            {
                if (!healthySet.Contains(attempted[i]))
                {
                    skipped.Add(attempted[i]);
                }
            }
            return skipped;
        }

        /// <summary>
        /// Runs the fan-out while synthesising a skipped-shard
        /// <see cref="WriteFanOutResult"/> for every shard filtered out
        /// by the resilience tier. Quorum evaluation sees the full
        /// attempted set so <see cref="WriteOutcome.QuorumAchieved"/>
        /// stays honest when a partial broadcast cannot make quorum.
        /// </summary>
        private WriteFanOutResult[] RunWithSkips(
            string                           entityName,
            string                           operation,
            IReadOnlyList<string>            attempted,
            IReadOnlyList<string>            healthy,
            Func<IProxyCluster, IErrorsInfo> writeOperation)
        {
            var healthySet = new HashSet<string>(healthy, StringComparer.OrdinalIgnoreCase);
            var results    = new WriteFanOutResult[attempted.Count];
            var liveIndex  = new List<int>(healthy.Count);
            var liveIds    = new List<string>(healthy.Count);

            for (int i = 0; i < attempted.Count; i++)
            {
                if (healthySet.Contains(attempted[i]))
                {
                    liveIndex.Add(i);
                    liveIds.Add(attempted[i]);
                }
                else
                {
                    results[i] = WriteFanOutResult.Failure(
                        shardId:  attempted[i],
                        error:    new InvalidOperationException(
                                      $"Shard '{attempted[i]}' skipped: classified unhealthy by the distributed resilience tier."),
                        duration: TimeSpan.Zero);
                }
            }

            if (liveIds.Count == 0) return results;

            var liveResults = RunParallelLegs(
                entityName:     entityName,
                operation:      operation,
                shardIds:       liveIds,
                writeOperation: writeOperation);

            for (int i = 0; i < liveIndex.Count; i++)
            {
                results[liveIndex[i]] = liveResults[i];
            }
            return results;
        }

        private WriteFanOutResult[] RunParallelLegs(
            string                              entityName,
            string                              operation,
            IReadOnlyList<string>               shardIds,
            Func<IProxyCluster, IErrorsInfo>    writeOperation)
        {
            int count     = shardIds.Count;
            var results   = new WriteFanOutResult[count];
            int maxDop    = Math.Max(1, _options.MaxFanOutParallelism);
            if (maxDop > count) maxDop = count;

            if (count <= 1 || maxDop == 1)
            {
                for (int i = 0; i < count; i++)
                {
                    results[i] = ExecuteLeg(shardIds[i], entityName, operation, writeOperation);
                }
                return results;
            }

            // Partition-by-stride: each worker pulls shard indices via
            // Interlocked.Increment so a slow leg on one worker does
            // not block other shards from being scheduled.
            var tasks = new Task[maxDop];
            int next  = -1;

            for (int w = 0; w < maxDop; w++)
            {
                tasks[w] = Task.Run(() =>
                {
                    while (true)
                    {
                        int idx = System.Threading.Interlocked.Increment(ref next);
                        if (idx >= count) return;
                        results[idx] = ExecuteLeg(shardIds[idx], entityName, operation, writeOperation);
                    }
                });
            }

            Task.WaitAll(tasks);
            return results;
        }
    }
}
