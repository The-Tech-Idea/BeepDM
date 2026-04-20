using System;

namespace TheTechIdea.Beep.Distributed.Events
{
    /// <summary>
    /// Raised by <see cref="DistributedDataSource.OnShardSelected"/>
    /// every time the <c>ShardRouter</c> (Phase 05) resolves an entity /
    /// key pair to a concrete shard. Subscribers typically use this for
    /// observability dashboards (heat maps), audit, or debug logging —
    /// it is fired on the hot path so handlers MUST be cheap and never
    /// throw. Phase 01 declares the event but never raises it; the
    /// raise sites are added in Phase 05 / Phase 06 / Phase 07.
    /// </summary>
    public sealed class ShardSelectedEventArgs : EventArgs
    {
        /// <summary>Initialises a new shard-selection event payload.</summary>
        /// <param name="entityName">Logical entity name (table). Never <c>null</c>.</param>
        /// <param name="shardId">Identifier of the resolved shard. Never <c>null</c>.</param>
        /// <param name="operation">Operation kind that triggered routing (e.g. "GetEntity", "InsertEntity").</param>
        /// <param name="partitionKey">Optional partition-key value used for row-level routing; <c>null</c> for entity-level routing.</param>
        /// <param name="reason">Free-form description of why this shard was chosen (placement match, hash slot, broadcast pick, etc.).</param>
        public ShardSelectedEventArgs(
            string entityName,
            string shardId,
            string operation,
            object partitionKey,
            string reason)
        {
            EntityName   = entityName ?? throw new ArgumentNullException(nameof(entityName));
            ShardId      = shardId    ?? throw new ArgumentNullException(nameof(shardId));
            Operation    = operation  ?? string.Empty;
            PartitionKey = partitionKey;
            Reason       = reason     ?? string.Empty;
            TimestampUtc = DateTime.UtcNow;
        }

        /// <summary>Logical entity name (table) being accessed.</summary>
        public string EntityName { get; }

        /// <summary>Identifier of the shard that the router selected.</summary>
        public string ShardId { get; }

        /// <summary>Caller-supplied operation kind for correlation (read / write / scatter).</summary>
        public string Operation { get; }

        /// <summary>Partition-key value used to route a sharded entity, or <c>null</c> for entity-level routing.</summary>
        public object PartitionKey { get; }

        /// <summary>Human-readable reason describing why this shard was picked.</summary>
        public string Reason { get; }

        /// <summary>UTC timestamp captured when the routing decision was made.</summary>
        public DateTime TimestampUtc { get; }
    }
}
