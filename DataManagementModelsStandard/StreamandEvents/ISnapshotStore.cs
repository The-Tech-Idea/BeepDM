using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.StreamandEvents
{
    // ── SnapshotRecord ────────────────────────────────────────────────────────

    /// <summary>
    /// A serialised aggregate snapshot saved at a specific event-log version.
    /// </summary>
    /// <typeparam name="TState">The aggregated state type.</typeparam>
    public sealed class SnapshotRecord<TState>
    {
        /// <summary>Stream (aggregate) identifier this snapshot belongs to.</summary>
        public string StreamId { get; init; }

        /// <summary>Event-log version at which the snapshot was taken.</summary>
        public long AtVersion { get; init; }

        /// <summary>Deserialised state captured at <see cref="AtVersion"/>.</summary>
        public TState State { get; init; }

        /// <summary>Unique snapshot identifier (auto-generated on save).</summary>
        public string SnapshotId { get; init; } = Guid.NewGuid().ToString();

        /// <summary>UTC wall clock when the snapshot was persisted.</summary>
        public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    }

    // ── ISnapshotStore ────────────────────────────────────────────────────────

    /// <summary>
    /// Read/write store for aggregate snapshots.
    /// <para>
    /// Snapshots reduce reconstitution time for long-lived aggregates by providing
    /// a starting state close to current, so only a fraction of the event log needs replaying.
    /// </para>
    /// </summary>
    /// <typeparam name="TState">The aggregated state type.</typeparam>
    public interface ISnapshotStore<TState>
    {
        /// <summary>Persists a new snapshot — older snapshots for the same stream are NOT deleted.</summary>
        Task SaveSnapshotAsync(
            SnapshotRecord<TState> snapshot,
            CancellationToken      cancellationToken = default);

        /// <summary>
        /// Returns the most recently saved snapshot for <paramref name="streamId"/>,
        /// or <c>null</c> if none exists.
        /// </summary>
        Task<SnapshotRecord<TState>?> TryLoadLatestAsync(
            string            streamId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes all snapshots for <paramref name="streamId"/> whose version is strictly below
        /// <paramref name="olderThanVersion"/>. Useful for pruning after a successful snapshot save.
        /// </summary>
        Task DeleteSnapshotsOlderThanAsync(
            string            streamId,
            long              olderThanVersion,
            CancellationToken cancellationToken = default);

        /// <summary>Lists all snapshot metadata for a stream in descending version order.</summary>
        Task<IReadOnlyList<SnapshotRecord<TState>>> ListSnapshotsAsync(
            string            streamId,
            CancellationToken cancellationToken = default);
    }
}
