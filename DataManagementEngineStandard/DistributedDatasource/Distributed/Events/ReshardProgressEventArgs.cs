using System;

namespace TheTechIdea.Beep.Distributed.Events
{
    /// <summary>
    /// Payload for <c>OnReshardProgress</c>. Emitted by Phase 11 as a
    /// reshard copy loop advances so dashboards and runbooks can show
    /// per-entity progress without subscribing to the lower-level
    /// <see cref="Resharding.CopyProgress"/> callback.
    /// </summary>
    public sealed class ReshardProgressEventArgs : EventArgs
    {
        /// <summary>Initialises a new progress payload.</summary>
        /// <param name="reshardId">Governing reshard id. Required.</param>
        /// <param name="entityName">Entity the progress applies to. Required.</param>
        /// <param name="fromShardId">Source shard for this leg.</param>
        /// <param name="toShardId">Target shard for this leg.</param>
        /// <param name="rowsCopied">Cumulative rows copied so far for this leg.</param>
        /// <param name="totalRows">Total rows (when known); <c>null</c> otherwise.</param>
        /// <param name="dualWriteState">Current state of the associated dual-write window.</param>
        public ReshardProgressEventArgs(
            string reshardId,
            string entityName,
            string fromShardId,
            string toShardId,
            long   rowsCopied,
            long?  totalRows,
            Resharding.DualWriteState dualWriteState)
        {
            ReshardId      = reshardId  ?? throw new ArgumentNullException(nameof(reshardId));
            EntityName     = entityName ?? throw new ArgumentNullException(nameof(entityName));
            FromShardId    = fromShardId ?? string.Empty;
            ToShardId      = toShardId   ?? string.Empty;
            RowsCopied     = rowsCopied;
            TotalRows      = totalRows;
            DualWriteState = dualWriteState;
            TimestampUtc   = DateTime.UtcNow;
        }

        /// <summary>Governing reshard id.</summary>
        public string ReshardId { get; }

        /// <summary>Entity the progress applies to.</summary>
        public string EntityName { get; }

        /// <summary>Source shard for this leg.</summary>
        public string FromShardId { get; }

        /// <summary>Target shard for this leg.</summary>
        public string ToShardId { get; }

        /// <summary>Cumulative rows copied so far for this leg.</summary>
        public long RowsCopied { get; }

        /// <summary>Total rows when known; <c>null</c> otherwise.</summary>
        public long? TotalRows { get; }

        /// <summary>Current state of the associated dual-write window.</summary>
        public Resharding.DualWriteState DualWriteState { get; }

        /// <summary>UTC timestamp when the event was raised.</summary>
        public DateTime TimestampUtc { get; }
    }
}
