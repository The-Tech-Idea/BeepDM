using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.StreamandEvents
{
    // ── Entry ─────────────────────────────────────────────────────────────────

    /// <summary>A single versioned entry in a <see cref="IDistributedStreamStateStore{T}"/>.</summary>
    public sealed class StateStoreEntry<T>
    {
        /// <summary>Entry key (may include the <see cref="StreamStateStoreOptions.KeyPrefix"/>).</summary>
        public string Key { get; init; }

        /// <summary>Stored value.</summary>
        public T Value { get; init; }

        /// <summary>
        /// Monotonically increasing version counter.
        /// Incremented on every successful write. Used for optimistic concurrency in
        /// <see cref="IDistributedStreamStateStore{T}.SetIfVersionAsync"/>.
        /// </summary>
        public long Version { get; init; }

        /// <summary>UTC time the entry was last written.</summary>
        public DateTimeOffset UpdatedAt { get; init; }

        /// <summary>Entry expiry time. <c>null</c> means the entry never expires.</summary>
        public DateTimeOffset? ExpiresAt { get; init; }

        /// <summary>Returns true when the entry has a finite expiry and it has passed.</summary>
        public bool IsExpired => ExpiresAt.HasValue && DateTimeOffset.UtcNow > ExpiresAt.Value;
    }

    // ── Optimistic concurrency exception ────────────────────────────────────

    /// <summary>
    /// Thrown by <see cref="IDistributedStreamStateStore{T}.SetIfVersionAsync"/> when the
    /// supplied <c>expectedVersion</c> does not match the current version of the entry.
    /// </summary>
    public sealed class OptimisticConcurrencyException : Exception
    {
        public string Key { get; }
        public long ExpectedVersion { get; }
        public long ActualVersion { get; }

        public OptimisticConcurrencyException(string key, long expectedVersion, long actualVersion)
            : base($"Optimistic concurrency conflict on key '{key}': expected version {expectedVersion} but found {actualVersion}.")
        {
            Key = key;
            ExpectedVersion = expectedVersion;
            ActualVersion = actualVersion;
        }
    }

    // ── Options ───────────────────────────────────────────────────────────────

    /// <summary>Configuration for a <see cref="IDistributedStreamStateStore{T}"/> instance.</summary>
    public sealed class StreamStateStoreOptions
    {
        /// <summary>
        /// Prefix prepended to all keys (separator: <c>:</c>).
        /// Use this to namespace multiple stores that share a backing store.
        /// </summary>
        public string KeyPrefix { get; init; } = "beep-stream";

        /// <summary>
        /// Default TTL applied when <c>SetAsync</c> is called without an explicit TTL.
        /// <c>null</c> = entries never expire.
        /// </summary>
        public TimeSpan? DefaultTtl { get; init; }

        /// <summary>Serialization format used by external/persistent backing stores.</summary>
        public StateStoreSerializationFormat SerializationFormat { get; init; } = StateStoreSerializationFormat.Json;
    }

    /// <summary>Wire format used when serializing state store values to an external backend.</summary>
    public enum StateStoreSerializationFormat
    {
        Json,
        MessagePack
    }

    // ── Interface ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Generic persistent key-value store for streaming engine state
    /// (idempotency keys, checkpoints, outbox records, circuit breaker state, saga state, etc.).
    /// All implementations must be safe for concurrent use from multiple threads and nodes.
    /// </summary>
    public interface IDistributedStreamStateStore<T>
    {
        /// <summary>
        /// Returns the entry for <paramref name="key"/>, or <c>null</c> when not found or expired.
        /// </summary>
        Task<StateStoreEntry<T>> GetAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Unconditionally writes <paramref name="value"/> for <paramref name="key"/>.
        /// Increments the version counter and resets the TTL.
        /// Returns the updated entry.
        /// </summary>
        Task<StateStoreEntry<T>> SetAsync(
            string key,
            T value,
            TimeSpan? ttl = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Writes <paramref name="value"/> only if the current version equals <paramref name="expectedVersion"/>.
        /// Use <c>expectedVersion = 0</c> for insert-if-absent (key must not yet exist).
        /// Throws <see cref="OptimisticConcurrencyException"/> on version mismatch.
        /// Returns the updated entry on success.
        /// </summary>
        Task<StateStoreEntry<T>> SetIfVersionAsync(
            string key,
            T value,
            long expectedVersion,
            TimeSpan? ttl = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes <paramref name="key"/> unconditionally.
        /// Returns <c>true</c> when the key existed, <c>false</c> when it was already absent.
        /// </summary>
        Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes <paramref name="key"/> only if its current version equals <paramref name="expectedVersion"/>.
        /// Returns <c>true</c> when deleted, <c>false</c> when not found or version mismatched.
        /// </summary>
        Task<bool> DeleteIfVersionAsync(string key, long expectedVersion, CancellationToken cancellationToken = default);

        /// <summary>Returns <c>true</c> when <paramref name="key"/> exists and has not expired.</summary>
        Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Scans all non-expired entries whose keys start with <paramref name="prefix"/> (inclusive).
        /// Results are not guaranteed to be in insertion order.
        /// </summary>
        IAsyncEnumerable<StateStoreEntry<T>> ScanAsync(
            string prefix,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Streams changes to <paramref name="key"/> as they occur.
        /// A <c>null</c> item signals that the key was deleted.
        /// Completes when <paramref name="cancellationToken"/> is cancelled.
        /// Multiple concurrent callers each receive all change notifications.
        /// </summary>
        IAsyncEnumerable<StateStoreEntry<T>> WatchAsync(
            string key,
            CancellationToken cancellationToken = default);
    }
}
