using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Distributed.Execution;
using TheTechIdea.Beep.Distributed.Resilience;
using TheTechIdea.Beep.Proxy;

namespace TheTechIdea.Beep.Distributed
{
    /// <summary>
    /// <see cref="DistributedDataSource"/> partial — adapter that
    /// exposes the private shard map and event raisers to the Phase
    /// 06 read executor via the <see cref="IShardInvoker"/> contract.
    /// Defined as a nested type so the executor can live in its own
    /// test assembly without the datasource leaking private state.
    /// </summary>
    public partial class DistributedDataSource
    {
        private sealed class ShardInvokerAdapter : IShardInvoker
        {
            private readonly DistributedDataSource _parent;

            internal ShardInvokerAdapter(DistributedDataSource parent)
            {
                _parent = parent ?? throw new ArgumentNullException(nameof(parent));
            }

            public IProxyCluster GetShard(string shardId)
            {
                if (string.IsNullOrWhiteSpace(shardId)) return null;
                return _parent._shards.TryGetValue(shardId, out var cluster) ? cluster : null;
            }

            public void NotifyShardSelected(
                string entityName,
                string shardId,
                string operation,
                object partitionKey,
                string reason)
                => _parent.RaiseShardSelected(entityName, shardId, operation, partitionKey, reason);

            public void NotifyShardFailure(
                string    entityName,
                string    shardId,
                string    operation,
                Exception exception)
            {
                // Phase 06 surfaces per-shard failures via PassEvent so
                // the scatter best-effort drops and replicated failovers
                // remain visible to operators. Phase 10 additionally
                // feeds the health monitor / circuit breaker so
                // subsequent calls can reroute away from the bad shard.
                var message =
                    "DistributedDataSource shard '" + shardId + "' " +
                    "operation '" + (operation ?? "?") + "' on entity '" + (entityName ?? "?") + "' failed: " +
                    (exception?.Message ?? "(null)");
                _parent.RaisePassEventSafe(message);
                _parent.NotifyShardCallFailed(shardId, exception, reason: operation);
            }

            // ── Phase 10 resilience overrides ─────────────────────────────

            public bool IsShardHealthy(string shardId)
                => _parent.IsShardHealthyForDispatch(shardId);

            public IReadOnlyList<string> FilterHealthyShards(IReadOnlyList<string> shardIds)
                => _parent.FilterHealthyShards(shardIds);

            public DegradedShardSetException EvaluateScatterGate(
                IReadOnlyList<string> attemptedShardIds,
                IReadOnlyList<string> healthyShardIds)
                => _parent.EvaluateScatterGate(attemptedShardIds, healthyShardIds);

            public void NotifyShardSuccess(string shardId, double latencyMs)
                => _parent.NotifyShardCallSucceeded(shardId, latencyMs);

            public void NotifyPartialBroadcast(
                string                entityName,
                string                operation,
                IReadOnlyList<string> attemptedShardIds,
                IReadOnlyList<string> skippedShardIds,
                string                reason)
                => _parent.ReportPartialBroadcast(
                       entityName,
                       operation,
                       attemptedShardIds,
                       skippedShardIds,
                       reason);

            // ── Phase 14 capacity overrides ───────────────────────────────

            public IDisposable AcquireDistributedCallPermit(System.Threading.CancellationToken cancellationToken = default)
                => _parent.AcquireDistributedCallPermit(cancellationToken);

            public IDisposable AcquireShardCallPermit(string shardId, System.Threading.CancellationToken cancellationToken = default)
                => _parent.AcquireShardCallPermit(shardId, cancellationToken);

            public IReadOnlyList<string> ShedHotShards(
                Routing.RoutingDecision decision,
                IReadOnlyList<string>   candidates)
            {
                if (candidates == null || candidates.Count == 0) return candidates;
                if (decision == null) return candidates;
                if (!_parent.PerformanceOptions.EnableHotShardReadShedding) return candidates;

                bool canShed = decision.Mode == DistributionMode.Replicated
                            || decision.Mode == DistributionMode.Broadcast;
                if (!canShed) return candidates;

                // Never shed the last replica — if every replica is
                // flagged hot we still have to pick one.
                List<string> keep = null;
                for (int i = 0; i < candidates.Count; i++)
                {
                    if (_parent.ShouldShedReadForDecision(decision, candidates[i]))
                    {
                        if (keep == null)
                        {
                            keep = new List<string>(candidates.Count);
                            for (int j = 0; j < i; j++) keep.Add(candidates[j]);
                        }
                    }
                    else
                    {
                        keep?.Add(candidates[i]);
                    }
                }
                if (keep == null)      return candidates;
                if (keep.Count == 0)   return candidates; // fallback — all hot
                return keep;
            }

            public int ComputeShardDeadlineMs(string shardId, int fallbackMs)
                => _parent.ComputeShardDeadlineMs(shardId, fallbackMs);
        }
    }
}
