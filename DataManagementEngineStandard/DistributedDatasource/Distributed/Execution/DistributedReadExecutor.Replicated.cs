using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Distributed.Routing;
using TheTechIdea.Beep.Proxy;

namespace TheTechIdea.Beep.Distributed.Execution
{
    /// <summary>
    /// <see cref="DistributedReadExecutor"/> partial — replicated read
    /// path. Picks a single shard from
    /// <see cref="RoutingDecision.ShardIds"/> according to
    /// <see cref="DistributedDataSourceOptions.ReplicatedReadPolicy"/>
    /// and fails over to the next live shard when the chosen one
    /// throws.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each failed replica is reported via
    /// <see cref="IShardInvoker.NotifyShardFailure"/> so operators see
    /// why a hot replica is being skipped. The final exception is
    /// rethrown only when every replica failed.
    /// </para>
    /// <para>
    /// Per-shard HA (node-level failover inside a cluster) is already
    /// handled by <see cref="IProxyCluster"/>; this method adds a
    /// <em>cross-cluster</em> failover so a degraded cluster can be
    /// skipped entirely.
    /// </para>
    /// </remarks>
    public sealed partial class DistributedReadExecutor
    {
        private static readonly Random Rng = new Random();

        /// <inheritdoc/>
        public T ExecuteReplicatedRead<T>(
            RoutingDecision             decision,
            Func<IProxyCluster, T>      readOperation,
            DistributedExecutionContext ctx)
        {
            RequireNonEmpty(decision);
            if (readOperation == null) throw new ArgumentNullException(nameof(readOperation));

            var order    = OrderReplicas(decision);
            var op       = ctx?.OperationName ?? string.Empty;
            var key      = FormatPartitionKey(decision);
            var errors   = new List<Exception>(order.Count);

            // Phase 14: distribution-tier gate + hot-shard shed.
            using var distributedPermit = _shards.AcquireDistributedCallPermit(CancellationToken.None);

            for (int i = 0; i < order.Count; i++)
            {
                var shardId = order[i];

                // Phase 14: skip hot shards when there is still
                // another replica left to try.
                if (i < order.Count - 1 && _shards.ShedHotShards(decision, new[] { shardId }).Count == 0)
                {
                    continue;
                }

                _shards.NotifyShardSelected(
                    entityName:   decision.EntityName,
                    shardId:      shardId,
                    operation:    op,
                    partitionKey: key,
                    reason:       i == 0 ? "replicated-primary" : "replicated-failover");

                var cluster = _shards.GetShard(shardId);
                if (cluster == null)
                {
                    errors.Add(new InvalidOperationException(
                        $"Shard '{shardId}' is not registered (replicated read)."));
                    continue;
                }

                IDisposable shardPermit = null;
                var sw = Stopwatch.StartNew();
                try
                {
                    shardPermit = _shards.AcquireShardCallPermit(shardId, CancellationToken.None);
                    var value = readOperation(cluster);
                    sw.Stop();
                    _shards.NotifyShardSuccess(shardId, sw.Elapsed.TotalMilliseconds);
                    return value;
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    errors.Add(ex);
                    _shards.NotifyShardFailure(
                        entityName: decision.EntityName,
                        shardId:    shardId,
                        operation:  op,
                        exception:  ex);
                }
                finally
                {
                    try { shardPermit?.Dispose(); } catch { /* best-effort */ }
                }
            }

            throw new AggregateException(
                $"Replicated read for '{decision.EntityName}' failed on every replica ({order.Count}).",
                errors);
        }

        /// <inheritdoc/>
        public async Task<T> ExecuteReplicatedReadAsync<T>(
            RoutingDecision                                 decision,
            Func<IProxyCluster, CancellationToken, Task<T>> readOperation,
            DistributedExecutionContext                     ctx,
            CancellationToken                               cancellationToken = default)
        {
            RequireNonEmpty(decision);
            if (readOperation == null) throw new ArgumentNullException(nameof(readOperation));

            var order  = OrderReplicas(decision);
            var op     = ctx?.OperationName ?? string.Empty;
            var key    = FormatPartitionKey(decision);
            var errors = new List<Exception>(order.Count);

            using var distributedPermit = _shards.AcquireDistributedCallPermit(cancellationToken);
            using var linked            = CreateLinkedDeadline(cancellationToken);

            for (int i = 0; i < order.Count; i++)
            {
                var shardId = order[i];

                if (i < order.Count - 1 && _shards.ShedHotShards(decision, new[] { shardId }).Count == 0)
                {
                    continue;
                }

                _shards.NotifyShardSelected(
                    entityName:   decision.EntityName,
                    shardId:      shardId,
                    operation:    op,
                    partitionKey: key,
                    reason:       i == 0 ? "replicated-primary" : "replicated-failover");

                var cluster = _shards.GetShard(shardId);
                if (cluster == null)
                {
                    errors.Add(new InvalidOperationException(
                        $"Shard '{shardId}' is not registered (replicated read)."));
                    continue;
                }

                IDisposable shardPermit = null;
                CancellationTokenSource perShardCts = null;
                var sw = Stopwatch.StartNew();
                try
                {
                    shardPermit = _shards.AcquireShardCallPermit(shardId, linked.Token);

                    int adaptiveMs = _shards.ComputeShardDeadlineMs(shardId, _options.DefaultPerShardTimeoutMs);
                    CancellationToken legToken = linked.Token;
                    if (adaptiveMs > 0)
                    {
                        perShardCts = CancellationTokenSource.CreateLinkedTokenSource(linked.Token);
                        perShardCts.CancelAfter(adaptiveMs);
                        legToken = perShardCts.Token;
                    }

                    var value = await readOperation(cluster, legToken).ConfigureAwait(false);
                    sw.Stop();
                    _shards.NotifyShardSuccess(shardId, sw.Elapsed.TotalMilliseconds);
                    return value;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // Caller-driven cancellation — do not fail over.
                    throw;
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    errors.Add(ex);
                    _shards.NotifyShardFailure(
                        entityName: decision.EntityName,
                        shardId:    shardId,
                        operation:  op,
                        exception:  ex);
                }
                finally
                {
                    try { perShardCts?.Dispose(); } catch { /* best-effort */ }
                    try { shardPermit?.Dispose(); } catch { /* best-effort */ }
                }
            }

            throw new AggregateException(
                $"Replicated read for '{decision.EntityName}' failed on every replica ({order.Count}).",
                errors);
        }

        /// <summary>
        /// Returns the shard list in the order the executor will
        /// attempt them, honouring
        /// <see cref="ReplicatedReadPolicy"/>. Phase 10 additionally
        /// promotes shards that pass the resilience tier's health
        /// filter to the front of the order so unhealthy replicas are
        /// tried only as a last-resort fallback.
        /// </summary>
        private IReadOnlyList<string> OrderReplicas(RoutingDecision decision)
        {
            var ids = decision.ShardIds;
            if (ids.Count == 0) return ids;

            IReadOnlyList<string> baseOrder;
            if (ids.Count == 1 || _options.ReplicatedReadPolicy == ReplicatedReadPolicy.First)
            {
                baseOrder = ids;
            }
            else
            {
                int start;
                lock (Rng)
                {
                    start = Rng.Next(ids.Count);
                }
                var rotated = new string[ids.Count];
                for (int i = 0; i < ids.Count; i++)
                {
                    rotated[i] = ids[(start + i) % ids.Count];
                }
                baseOrder = rotated;
            }

            return PromoteHealthyShards(baseOrder);
        }

        /// <summary>
        /// Stable-partitions <paramref name="order"/> into healthy and
        /// unhealthy groups so callers try healthy replicas first.
        /// </summary>
        private IReadOnlyList<string> PromoteHealthyShards(IReadOnlyList<string> order)
        {
            if (order.Count <= 1) return order;

            var healthy   = new List<string>(order.Count);
            var unhealthy = new List<string>();
            for (int i = 0; i < order.Count; i++)
            {
                if (_shards.IsShardHealthy(order[i]))
                {
                    healthy.Add(order[i]);
                }
                else
                {
                    unhealthy.Add(order[i]);
                }
            }

            if (unhealthy.Count == 0) return order;
            if (healthy.Count == 0)   return order; // all unhealthy: preserve original order so failover still runs.

            healthy.AddRange(unhealthy);
            return healthy;
        }
    }
}
