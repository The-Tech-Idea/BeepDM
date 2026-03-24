using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.StreamandEvents
{
    /// <summary>
    /// Tracks the last successfully processed offset per topic/partition/consumer-group.
    /// Implementations may be in-memory, database-backed, or broker-native (e.g. Kafka auto-commit).
    /// Commit only after handler success to guarantee at-least-once outcomes.
    /// </summary>
    public interface ICheckpointStore
    {
        /// <summary>Persists the checkpoint after a handler has completed successfully.</summary>
        Task CommitAsync(CheckpointRecord checkpoint, CancellationToken cancellationToken = default);

        /// <summary>Returns the last committed checkpoint for a topic/partition/group triple, or null if none.</summary>
        Task<CheckpointRecord?> GetAsync(string topic, string partitionKey, string consumerGroup, CancellationToken cancellationToken = default);

        /// <summary>Removes checkpoint entries older than <paramref name="olderThan"/>.</summary>
        Task PruneAsync(DateTime olderThan, CancellationToken cancellationToken = default);

        /// <summary>Lists all active checkpoints for the given consumer group.</summary>
        Task<IReadOnlyList<CheckpointRecord>> ListAsync(string consumerGroup, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// A single committed consumer-offset checkpoint.
    /// <see cref="OffsetToken"/> is broker-specific (e.g. Kafka offset as string, AMQP delivery tag).
    /// </summary>
    public sealed record CheckpointRecord
    {
        public required string Topic         { get; init; }
        public required string PartitionKey  { get; init; }
        public required string ConsumerGroup { get; init; }
        /// <summary>Broker-specific offset token, stored as a string for portability.</summary>
        public required string OffsetToken   { get; init; }
        public required long   SequenceNumber { get; init; }
        public DateTimeOffset  CommittedAt   { get; init; } = DateTimeOffset.UtcNow;
        /// <summary>The event that was last successfully processed at this checkpoint.</summary>
        public string? EventId { get; init; }
    }

    /// <summary>
    /// Tracks which partitions are currently owned by this consumer instance.
    /// Used during rebalance callbacks to drain in-flight work before revoking ownership.
    /// Thread-safe.
    /// </summary>
    public sealed class PartitionAssignmentState
    {
        private readonly HashSet<string> _assigned = new(StringComparer.Ordinal);
        private readonly object _lock = new();

        /// <summary>Marks <paramref name="partitionKey"/> as owned by this instance.</summary>
        public void Assign(string partitionKey)
        {
            lock (_lock) _assigned.Add(partitionKey);
        }

        /// <summary>Relinquishes ownership. Returns true if the partition was tracked.</summary>
        public bool Revoke(string partitionKey)
        {
            lock (_lock) return _assigned.Remove(partitionKey);
        }

        /// <summary>Returns true if this instance currently owns the partition.</summary>
        public bool IsAssigned(string partitionKey)
        {
            lock (_lock) return _assigned.Contains(partitionKey);
        }

        /// <summary>Snapshot of all currently assigned partitions.</summary>
        public IReadOnlyCollection<string> AssignedPartitions
        {
            get { lock (_lock) return new HashSet<string>(_assigned, StringComparer.Ordinal); }
        }

        /// <summary>Clears all assignments (call after full group revoke).</summary>
        public void RevokeAll()
        {
            lock (_lock) _assigned.Clear();
        }
    }

}
