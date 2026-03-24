using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.StreamandEvents
{
    // ── Storage Provider Interface ────────────────────────────────────────────

    /// <summary>
    /// Pluggable persistence backend for the Beep streaming engine.
    /// <para>
    /// Implementations handle durable storage of messages, topic metadata, consumer offsets,
    /// and retention. The engine (<see cref="IBeepStreamNode"/>) delegates all I/O to this
    /// interface; real-time dispatch uses <c>System.Threading.Channels</c> internally.
    /// </para>
    /// <para>
    /// Built-in providers: <c>InMemoryStorageProvider</c> (dev/test),
    /// <c>SQLiteStorageProvider</c> (embedded production).
    /// Custom providers can back the engine with any durable store.
    /// </para>
    /// </summary>
    public interface IStreamStorageProvider : IAsyncDisposable
    {
        /// <summary>Human-readable name (e.g. "InMemory", "SQLite", "LiteDB").</summary>
        string ProviderName { get; }

        /// <summary>True when the provider survives process restarts.</summary>
        bool SupportsPersistence { get; }

        // ── Lifecycle ─────────────────────────────────────────────────────

        /// <summary>Initialize the provider (open files, create schema, etc.).</summary>
        Task InitializeAsync(CancellationToken cancellationToken = default);

        // ── Topic management ──────────────────────────────────────────────

        Task CreateTopicAsync(string topic, int partitionCount, TopicStorageConfig config, CancellationToken cancellationToken = default);
        Task DeleteTopicAsync(string topic, CancellationToken cancellationToken = default);
        Task<bool> TopicExistsAsync(string topic, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<string>> ListTopicsAsync(CancellationToken cancellationToken = default);
        Task<StoredTopicMetadata> GetTopicMetadataAsync(string topic, CancellationToken cancellationToken = default);

        // ── Message storage ───────────────────────────────────────────────

        /// <summary>
        /// Appends a message to the specified topic-partition.
        /// Returns the assigned offset (monotonically increasing per partition).
        /// </summary>
        Task<long> AppendAsync(string topic, int partition, StoredMessage message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Appends a batch of messages atomically to a single partition.
        /// Returns the offset of the last message in the batch.
        /// </summary>
        Task<long> AppendBatchAsync(string topic, int partition, IReadOnlyList<StoredMessage> messages, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads up to <paramref name="maxCount"/> messages starting from <paramref name="fromOffset"/>.
        /// Returns an empty list if the offset is past the end.
        /// </summary>
        Task<IReadOnlyList<StoredMessage>> ReadAsync(string topic, int partition, long fromOffset, int maxCount, CancellationToken cancellationToken = default);

        /// <summary>Returns the highest offset written to the partition, or -1 if empty.</summary>
        Task<long> GetLatestOffsetAsync(string topic, int partition, CancellationToken cancellationToken = default);

        /// <summary>Returns the earliest offset still available (after retention).</summary>
        Task<long> GetEarliestOffsetAsync(string topic, int partition, CancellationToken cancellationToken = default);

        // ── Consumer group offsets ────────────────────────────────────────

        Task CommitOffsetAsync(string consumerGroup, string topic, int partition, long offset, CancellationToken cancellationToken = default);
        Task<long> GetCommittedOffsetAsync(string consumerGroup, string topic, int partition, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ConsumerGroupOffsetInfo>> ListConsumerGroupOffsetsAsync(string consumerGroup, CancellationToken cancellationToken = default);

        // ── Retention ─────────────────────────────────────────────────────

        /// <summary>Enforces the retention policy by deleting messages older than the policy allows.</summary>
        Task ApplyRetentionAsync(string topic, CancellationToken cancellationToken = default);
    }

    // ── Storage DTOs ──────────────────────────────────────────────────────────

    /// <summary>A single message as stored by the provider. Offsets are assigned on append.</summary>
    public sealed class StoredMessage
    {
        /// <summary>Monotonically increasing offset within the partition (assigned by provider).</summary>
        public long Offset { get; set; }

        /// <summary>Partition key — used for key-based routing and compaction.</summary>
        public string Key { get; init; }

        /// <summary>Serialized payload bytes.</summary>
        public byte[] Value { get; init; }

        /// <summary>Header metadata (trace IDs, tenant, event type, etc.).</summary>
        public IReadOnlyDictionary<string, string> Headers { get; init; }

        /// <summary>Producer timestamp (UTC).</summary>
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;

        /// <summary>Fully-qualified event type name (for schema dispatch).</summary>
        public string EventType { get; init; }

        /// <summary>Unique event ID for idempotency.</summary>
        public string EventId { get; init; }
    }

    /// <summary>Topic-level configuration stored alongside the topic.</summary>
    public sealed class TopicStorageConfig
    {
        /// <summary>Maximum age of messages before retention kicks in. Null = infinite.</summary>
        public TimeSpan? RetentionPeriod { get; init; }

        /// <summary>Maximum total size in bytes before retention kicks in. Null = unlimited.</summary>
        public long? MaxSizeBytes { get; init; }

        /// <summary>If true, only the latest message per key is kept (log compaction).</summary>
        public bool IsCompacted { get; init; }

        /// <summary>Replication factor — how many nodes should hold a copy.</summary>
        public int ReplicationFactor { get; init; } = 1;

        public static TopicStorageConfig Default => new()
        {
            RetentionPeriod = TimeSpan.FromDays(7),
            IsCompacted = false,
            ReplicationFactor = 1
        };
    }

    /// <summary>Metadata snapshot for a stored topic.</summary>
    public sealed class StoredTopicMetadata
    {
        public string TopicName { get; init; }
        public int PartitionCount { get; init; }
        public TopicStorageConfig Config { get; init; }
        public DateTime CreatedAt { get; init; }

        /// <summary>Per-partition high watermarks: partition → latest offset.</summary>
        public IReadOnlyDictionary<int, long> PartitionHighWatermarks { get; init; }
    }

    /// <summary>Offset info for a single (consumer-group, topic, partition) triple.</summary>
    public sealed class ConsumerGroupOffsetInfo
    {
        public string ConsumerGroup { get; init; }
        public string Topic { get; init; }
        public int Partition { get; init; }
        public long CommittedOffset { get; init; }
    }
}
