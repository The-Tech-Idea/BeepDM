using System;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Distributed.Routing;
using TheTechIdea.Beep.Proxy;

namespace TheTechIdea.Beep.Distributed.Execution
{
    /// <summary>
    /// <see cref="DistributedReadExecutor"/> partial — single-shard path.
    /// No fan-out, no merging; the executor delegates straight to the
    /// resolved <see cref="IProxyCluster"/>. Per-shard HA (node
    /// failover, retries, load balancing) is the cluster's job.
    /// </summary>
    public sealed partial class DistributedReadExecutor
    {
        /// <inheritdoc/>
        public T ExecuteSingleShard<T>(
            RoutingDecision             decision,
            Func<IProxyCluster, T>      readOperation,
            DistributedExecutionContext ctx)
        {
            RequireSingleShard(decision);
            if (readOperation == null) throw new ArgumentNullException(nameof(readOperation));

            var shardId = decision.ShardIds[0];
            var operation = ctx?.OperationName ?? string.Empty;

            _shards.NotifyShardSelected(
                entityName:   decision.EntityName,
                shardId:      shardId,
                operation:    operation,
                partitionKey: FormatPartitionKey(decision),
                reason:       "single-shard");

            var cluster = ResolveShardOrThrow(shardId, operation, decision.EntityName);

            // Phase 14 — capacity gates (distributed + per-shard).
            using var distributedPermit = _shards.AcquireDistributedCallPermit(CancellationToken.None);
            using var shardPermit       = _shards.AcquireShardCallPermit(shardId, CancellationToken.None);
            return readOperation(cluster);
        }

        /// <inheritdoc/>
        public async Task<T> ExecuteSingleShardAsync<T>(
            RoutingDecision                                 decision,
            Func<IProxyCluster, CancellationToken, Task<T>> readOperation,
            DistributedExecutionContext                     ctx,
            CancellationToken                               cancellationToken = default)
        {
            RequireSingleShard(decision);
            if (readOperation == null) throw new ArgumentNullException(nameof(readOperation));

            var shardId   = decision.ShardIds[0];
            var operation = ctx?.OperationName ?? string.Empty;

            _shards.NotifyShardSelected(
                entityName:   decision.EntityName,
                shardId:      shardId,
                operation:    operation,
                partitionKey: FormatPartitionKey(decision),
                reason:       "single-shard");

            var cluster = ResolveShardOrThrow(shardId, operation, decision.EntityName);

            using var distributedPermit = _shards.AcquireDistributedCallPermit(cancellationToken);
            using var shardPermit       = _shards.AcquireShardCallPermit(shardId, cancellationToken);

            using var linked = CreateLinkedDeadline(cancellationToken);

            // Phase 14 — adaptive per-shard deadline layered on top
            // of the caller's deadline; never shortens below the
            // configured fallback.
            int adaptiveMs = _shards.ComputeShardDeadlineMs(shardId, _options.DefaultPerShardTimeoutMs);
            CancellationTokenSource perShardCts = null;
            CancellationToken token = linked.Token;
            try
            {
                if (adaptiveMs > 0)
                {
                    perShardCts = CancellationTokenSource.CreateLinkedTokenSource(linked.Token);
                    perShardCts.CancelAfter(adaptiveMs);
                    token = perShardCts.Token;
                }
                return await readOperation(cluster, token).ConfigureAwait(false);
            }
            finally
            {
                try { perShardCts?.Dispose(); } catch { /* best-effort */ }
            }
        }

        /// <summary>
        /// Combines the caller's cancellation token with the configured
        /// <see cref="DistributedDataSourceOptions.DefaultReadDeadlineMs"/>
        /// so a slow shard cannot pin the whole request forever. When
        /// the option is <c>0</c> we honor only the caller's token.
        /// </summary>
        private CancellationTokenSource CreateLinkedDeadline(CancellationToken callerToken)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(callerToken);
            if (_options.DefaultReadDeadlineMs > 0)
            {
                cts.CancelAfter(_options.DefaultReadDeadlineMs);
            }
            return cts;
        }
    }
}
