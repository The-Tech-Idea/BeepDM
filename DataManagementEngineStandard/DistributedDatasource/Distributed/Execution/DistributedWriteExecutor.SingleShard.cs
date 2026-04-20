using System;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Distributed.Routing;
using TheTechIdea.Beep.Proxy;

namespace TheTechIdea.Beep.Distributed.Execution
{
    /// <summary>
    /// Single-shard write path for
    /// <see cref="DistributedWriteExecutor"/>. Used for sharded writes
    /// that routed to exactly one shard via the partition function.
    /// Quorum is implicitly "the one shard must ack."
    /// </summary>
    public sealed partial class DistributedWriteExecutor
    {
        /// <inheritdoc/>
        public WriteOutcome ExecuteSingleShard(
            RoutingDecision                  decision,
            Func<IProxyCluster, IErrorsInfo> writeOperation,
            DistributedExecutionContext      ctx)
        {
            if (writeOperation == null) throw new ArgumentNullException(nameof(writeOperation));

            RequireSingleShard(decision);

            var shardId   = decision.ShardIds[0];
            var operation = ctx?.OperationName ?? string.Empty;

            _shards.NotifyShardSelected(
                entityName:   decision.EntityName,
                shardId:      shardId,
                operation:    operation,
                partitionKey: FormatPartitionKey(decision),
                reason:       "single-shard-write");

            // Phase 14 — acquire the distribution-tier permit; the
            // per-shard permit is acquired inside ExecuteLeg so all
            // write paths (single + fan-out) share the same
            // back-pressure semantics.
            using var distributedPermit = _shards.AcquireDistributedCallPermit(System.Threading.CancellationToken.None);

            var leg = ExecuteLeg(
                shardId:        shardId,
                entityName:     decision.EntityName,
                operation:      operation,
                writeOperation: writeOperation);

            return new WriteOutcome(
                entityName:       decision.EntityName,
                operation:        operation,
                perShard:         new[] { leg },
                requiredAckCount: 1,
                quorumPolicy:     QuorumPolicy.All);
        }
    }
}
