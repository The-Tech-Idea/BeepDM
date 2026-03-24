using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.StreamandEvents
{
    // ── Event args ────────────────────────────────────────────────────────────

    /// <summary>Notification raised when a materialized view entry is created, updated, or deleted.</summary>
    public sealed record MaterializedViewUpdatedArgs<TKey, TValue>(
        TKey           Key,
        TValue?        OldValue,
        TValue?        NewValue,
        bool           IsDelete,
        DateTimeOffset OccurredAt);

    // ── IMaterializedView ─────────────────────────────────────────────────────

    /// <summary>
    /// A queryable in-memory projection of a compacted log stream.
    /// Provides random-access reads over a key-value snapshot built from streaming events
    /// (analogous to a KTable in Kafka Streams).
    /// </summary>
    public interface IMaterializedView<TKey, TValue>
    {
        /// <summary>Returns the value for <paramref name="key"/>, or <c>default</c> if absent.</summary>
        Task<TValue?> GetAsync(TKey key, CancellationToken cancellationToken = default);

        /// <summary>Streams all current key-value pairs.</summary>
        IAsyncEnumerable<KeyValuePair<TKey, TValue>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>Returns <c>true</c> if the key exists in the view.</summary>
        Task<bool> ContainsAsync(TKey key, CancellationToken cancellationToken = default);

        /// <summary>Returns the current number of entries.</summary>
        Task<long> CountAsync(CancellationToken cancellationToken = default);

        /// <summary>Upserts a key-value pair into the view.</summary>
        Task PutAsync(TKey key, TValue value, CancellationToken cancellationToken = default);

        /// <summary>Removes a key from the view.</summary>
        Task DeleteAsync(TKey key, CancellationToken cancellationToken = default);

        /// <summary>Raised after every put or delete operation.</summary>
        event EventHandler<MaterializedViewUpdatedArgs<TKey, TValue>> OnUpdated;
    }

}
