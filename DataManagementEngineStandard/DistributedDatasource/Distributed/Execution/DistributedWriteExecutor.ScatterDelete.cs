using System;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Distributed.Routing;
using TheTechIdea.Beep.Proxy;

namespace TheTechIdea.Beep.Distributed.Execution
{
    /// <summary>
    /// Scatter write path for
    /// <see cref="DistributedWriteExecutor"/>. Used for sharded
    /// "delete-by-filter" (and similar) operations when the partition
    /// key is missing but the caller explicitly wants the write
    /// broadcast to every shard.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Opt-in only: the caller MUST set
    /// <see cref="DistributedWriteOptions.AllowScatterWrite"/> to
    /// <c>true</c> (or
    /// <see cref="DistributedDataSourceOptions.AllowScatterWrite"/>
    /// must be <c>true</c>). This prevents accidental mass writes
    /// when a key was just forgotten; the
    /// <see cref="ShardRoutingException"/> message identifies the
    /// missing opt-in so the caller knows what to flip.
    /// </para>
    /// <para>
    /// Scatter writes always require <see cref="QuorumPolicy.All"/> —
    /// a partial scatter-delete is almost never what the caller
    /// wanted. Use replicated/broadcast writes when you want quorum
    /// semantics.
    /// </para>
    /// </remarks>
    public sealed partial class DistributedWriteExecutor
    {
        /// <inheritdoc/>
        public WriteOutcome ExecuteScatter(
            RoutingDecision                  decision,
            Func<IProxyCluster, IErrorsInfo> writeOperation,
            DistributedExecutionContext      ctx,
            DistributedWriteOptions          options)
        {
            if (writeOperation == null) throw new ArgumentNullException(nameof(writeOperation));

            RequireNonEmpty(decision);

            bool allowed = options?.AllowScatterWrite ?? _options.AllowScatterWrite;
            if (!allowed)
            {
                throw new ShardRoutingException(
                    entityName: decision.EntityName,
                    reason:     "ScatterWriteRejected",
                    message:    $"Refusing to fan a write for '{decision.EntityName}' across " +
                                $"{decision.ShardIds.Count} shards without a partition key. " +
                                "Pass DistributedWriteOptions.AllowScatterWrite = true (or set " +
                                "DistributedDataSourceOptions.AllowScatterWrite) to opt in to " +
                                "scatter writes (typically used for delete-by-filter).");
            }

            var operation = ctx?.OperationName ?? string.Empty;

            // Phase 14 — distribution-tier capacity gate.
            using var distributedPermit = _shards.AcquireDistributedCallPermit(System.Threading.CancellationToken.None);

            // Phase 10 — evaluate the scatter gate even though scatter
            // writes demand QuorumPolicy.All; a shard set that is
            // already below MinimumHealthyShardRatio cannot possibly
            // satisfy an all-ack quorum, so failing fast here avoids
            // launching doomed legs against unhealthy shards.
            var healthy = _shards.FilterHealthyShards(decision.ShardIds) ?? decision.ShardIds;
            var gate    = _shards.EvaluateScatterGate(decision.ShardIds, healthy);
            if (gate != null) throw gate;

            NotifyFanOut(decision, ctx, reason: "scatter-write");

            var perShard = RunParallelLegs(
                entityName:     decision.EntityName,
                operation:      operation,
                shardIds:       decision.ShardIds,
                writeOperation: writeOperation);

            return new WriteOutcome(
                entityName:       decision.EntityName,
                operation:        operation,
                perShard:         perShard,
                requiredAckCount: perShard.Length,
                quorumPolicy:     QuorumPolicy.All);
        }
    }
}
