using System;

namespace TheTechIdea.Beep.Distributed.Resharding
{
    /// <summary>
    /// Persistable copy checkpoint emitted by
    /// <see cref="IEntityCopyService"/>. Captures the last
    /// successfully-copied page so a restart can resume mid-reshard
    /// without re-copying every row.
    /// </summary>
    /// <remarks>
    /// Checkpoints are keyed by <c>{ReshardId, EntityName, FromShard, ToShard}</c>
    /// in the backing <see cref="IEntityCopyCheckpointStore"/>. The
    /// <see cref="LastCopiedKey"/> payload is intentionally
    /// provider-neutral (string representation of the last key that
    /// landed on the target shard) so every <see cref="Partitioning.IPartitionFunction"/>
    /// shape can produce one.
    /// </remarks>
    public sealed class CopyCheckpoint
    {
        /// <summary>Initialises a new checkpoint.</summary>
        /// <param name="reshardId">Governing reshard id. Required.</param>
        /// <param name="entityName">Logical entity being copied. Required.</param>
        /// <param name="fromShardId">Source shard id. Required.</param>
        /// <param name="toShardId">Target shard id. Required.</param>
        /// <param name="lastCopiedKey">Last successfully-copied primary-key value, stringified. May be <c>null</c> on the first checkpoint.</param>
        /// <param name="rowsCopied">Cumulative row count copied so far.</param>
        /// <param name="isComplete"><c>true</c> when the source has been fully drained.</param>
        public CopyCheckpoint(
            string   reshardId,
            string   entityName,
            string   fromShardId,
            string   toShardId,
            string   lastCopiedKey,
            long     rowsCopied,
            bool     isComplete)
        {
            if (string.IsNullOrWhiteSpace(reshardId))   throw new ArgumentException("Reshard id required.",   nameof(reshardId));
            if (string.IsNullOrWhiteSpace(entityName))  throw new ArgumentException("Entity name required.",  nameof(entityName));
            if (string.IsNullOrWhiteSpace(fromShardId)) throw new ArgumentException("Source shard required.", nameof(fromShardId));
            if (string.IsNullOrWhiteSpace(toShardId))   throw new ArgumentException("Target shard required.", nameof(toShardId));
            if (rowsCopied < 0)                         throw new ArgumentOutOfRangeException(nameof(rowsCopied));

            ReshardId     = reshardId;
            EntityName    = entityName;
            FromShardId   = fromShardId;
            ToShardId     = toShardId;
            LastCopiedKey = lastCopiedKey;
            RowsCopied    = rowsCopied;
            IsComplete    = isComplete;
            UpdatedUtc    = DateTime.UtcNow;
        }

        /// <summary>Governing reshard operation id.</summary>
        public string ReshardId { get; }

        /// <summary>Logical entity being copied.</summary>
        public string EntityName { get; }

        /// <summary>Source shard id.</summary>
        public string FromShardId { get; }

        /// <summary>Target shard id.</summary>
        public string ToShardId { get; }

        /// <summary>Stringified last-copied primary-key value. <c>null</c> on the first checkpoint.</summary>
        public string LastCopiedKey { get; }

        /// <summary>Cumulative row count copied so far.</summary>
        public long RowsCopied { get; }

        /// <summary><c>true</c> when the source has been fully drained.</summary>
        public bool IsComplete { get; }

        /// <summary>UTC timestamp the checkpoint was persisted.</summary>
        public DateTime UpdatedUtc { get; }

        /// <inheritdoc/>
        public override string ToString()
            => $"CopyCheckpoint(reshard={ReshardId}, entity={EntityName}, " +
               $"{FromShardId} -> {ToShardId}, rows={RowsCopied}, complete={IsComplete})";

        /// <summary>
        /// Returns the composite key used by
        /// <see cref="IEntityCopyCheckpointStore"/> entries.
        /// </summary>
        public string CompositeKey()
            => BuildKey(ReshardId, EntityName, FromShardId, ToShardId);

        /// <summary>Builds the composite checkpoint key from discrete parts.</summary>
        public static string BuildKey(string reshardId, string entityName, string fromShardId, string toShardId)
            => $"{reshardId}|{entityName}|{fromShardId}|{toShardId}";
    }
}
