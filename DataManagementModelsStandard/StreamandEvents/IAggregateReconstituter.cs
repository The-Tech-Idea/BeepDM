using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.StreamandEvents
{
    // ── AggregateRoot ─────────────────────────────────────────────────────────

    /// <summary>
    /// Base class for event-sourced aggregates.
    /// Subclasses call <see cref="RaiseEvent{TPayload}"/> to record domain events,
    /// and override <see cref="Apply"/> to evolve their state in response to events.
    /// </summary>
    /// <typeparam name="TState">Mutable state bag owned by the aggregate.</typeparam>
    public abstract class AggregateRoot<TState>
        where TState : new()
    {
        private readonly List<EventStoreRecord> _uncommittedEvents = new();

        // ── Public surface ────────────────────────────────────────────────────

        /// <summary>Current projected state.</summary>
        public TState State { get; private set; } = new TState();

        /// <summary>Latest committed event version (0 = no commits yet).</summary>
        public long Version { get; private set; }

        /// <summary>Events raised since the last <see cref="MarkCommitted"/> call.</summary>
        public IReadOnlyList<EventStoreRecord> UncommittedEvents => _uncommittedEvents.AsReadOnly();

        // ── Reconstitution ────────────────────────────────────────────────────

        /// <summary>
        /// Replays a sequence of historical events to rebuild state from scratch.
        /// Resets the current state and version before replaying.
        /// </summary>
        public void Reconstitute(IEnumerable<EventStoreRecord> history)
        {
            State   = new TState();
            Version = 0;
            _uncommittedEvents.Clear();

            foreach (var record in history)
            {
                Apply(record);
                Version = record.Version;
            }
        }

        // ── Event raising ─────────────────────────────────────────────────────

        /// <summary>
        /// Records a new domain event: applies it immediately to state and adds it to
        /// <see cref="UncommittedEvents"/> for persisting later.
        /// </summary>
        protected void RaiseEvent<TPayload>(
            TPayload   payload,
            string     eventType       = null,
            string     correlationId   = null,
            string     causationId     = null,
            string     contentType     = "application/json",
            byte[]     serialisedBytes = null)
        {
            var record = new EventStoreRecord
            {
                StreamId      = GetStreamId(),
                Version       = Version + _uncommittedEvents.Count + 1,
                EventType     = eventType ?? typeof(TPayload).FullName,
                PayloadBytes  = serialisedBytes ?? Array.Empty<byte>(),
                ContentType   = contentType,
                OccurredAt    = DateTimeOffset.UtcNow,
                CorrelationId = correlationId,
                CausationId   = causationId
            };

            Apply(record);
            _uncommittedEvents.Add(record);
        }

        /// <summary>Marks all uncommitted events as committed and advances <see cref="Version"/>.</summary>
        public void MarkCommitted(long newVersion)
        {
            Version = newVersion;
            _uncommittedEvents.Clear();
        }

        // ── Abstract hooks ────────────────────────────────────────────────────

        /// <summary>
        /// Applies a single event record to <see cref="State"/>.
        /// Dispatching by <see cref="EventStoreRecord.EventType"/> is the recommended pattern.
        /// </summary>
        protected abstract void Apply(EventStoreRecord record);

        /// <summary>Returns the stream ID for this aggregate instance (e.g. "Order-42").</summary>
        protected abstract string GetStreamId();
    }

    // ── IAggregateReconstituter ───────────────────────────────────────────────

    /// <summary>
    /// Reconstructs an aggregate from its event store history, optionally aided by snapshots.
    /// </summary>
    /// <typeparam name="TAggregate">
    ///   The <see cref="AggregateRoot{TState}"/> subtype to reconstitute.
    /// </typeparam>
    public interface IAggregateReconstituter<TAggregate>
    {
        /// <summary>
        /// Loads the full event history for <paramref name="streamId"/> and reconstitutes
        /// the aggregate to its current state.
        /// </summary>
        Task<TAggregate> ReconstitutAsync(
            string            streamId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Loads events up to and including <paramref name="toVersion"/> and reconstitutes
        /// the aggregate to that point-in-time state.
        /// </summary>
        Task<TAggregate> ReconstitutToVersionAsync(
            string            streamId,
            long              toVersion,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Starts from the provided <paramref name="snapshot"/> and replays only the events
        /// that occurred after the snapshot version — dramatically faster for long streams.
        /// </summary>
        /// <typeparam name="TState">State type that the snapshot carries.</typeparam>
        Task<TAggregate> ReconstitutFromSnapshotAsync<TState>(
            SnapshotRecord<TState> snapshot,
            CancellationToken      cancellationToken = default);
    }
}
