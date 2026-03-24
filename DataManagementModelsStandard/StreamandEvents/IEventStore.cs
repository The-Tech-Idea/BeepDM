using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.StreamandEvents
{
    // ── EventStoreRecord ──────────────────────────────────────────────────────

    /// <summary>
    /// An immutable record of one domain event as it is stored in the event log.
    /// </summary>
    public sealed class EventStoreRecord
    {
        /// <summary>Logical stream identifier, typically an aggregate ID (e.g. "Order-42").</summary>
        public string StreamId { get; init; }

        /// <summary>
        /// Monotonically increasing per-stream version (1-based).
        /// Version 0 is reserved for the "no-stream" sentinel.
        /// </summary>
        public long Version { get; init; }

        /// <summary>Fully-qualified event type name (e.g. "Order.OrderPlaced").</summary>
        public string EventType { get; init; }

        /// <summary>Serialised event payload bytes.</summary>
        public ReadOnlyMemory<byte> PayloadBytes { get; init; }

        /// <summary>MIME content type of <see cref="PayloadBytes"/> (e.g. "application/json").</summary>
        public string ContentType { get; init; } = "application/json";

        /// <summary>Wall-clock UTC time when the domain event occurred.</summary>
        public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;

        /// <summary>Correlation ID for distributed tracing.</summary>
        public string CorrelationId { get; init; }

        /// <summary>Causation ID — ID of the command or event that caused this one.</summary>
        public string CausationId { get; init; }

        /// <summary>Arbitrary key-value metadata (tags, routing hints, etc.).</summary>
        public IReadOnlyDictionary<string, string> Metadata { get; init; }
            = new Dictionary<string, string>(StringComparer.Ordinal);
    }

    // ── EventStoreSlice ───────────────────────────────────────────────────────

    /// <summary>A page of events returned from <see cref="IEventStore.LoadAsync"/>.</summary>
    public sealed class EventStoreSlice
    {
        public IReadOnlyList<EventStoreRecord> Events      { get; init; } = Array.Empty<EventStoreRecord>();
        public long                            LastVersion { get; init; }
        public bool                            IsEndOfStream { get; init; }
    }

    // ── ExpectedVersion ───────────────────────────────────────────────────────

    /// <summary>
    /// Sentinel values for the <c>expectedVersion</c> parameter of <see cref="IEventStore.AppendAsync"/>.
    /// Using a sentinel instead of optional avoids silent misuse.
    /// </summary>
    public static class ExpectedVersion
    {
        /// <summary>Skip optimistic concurrency check — accept any current version.</summary>
        public const long Any = -2L;

        /// <summary>Expect the stream to not yet exist.</summary>
        public const long NoStream = -1L;

        /// <summary>Expect the stream to exist but contain zero events.</summary>
        public const long EmptyStream = 0L;
    }

    // ── Exceptions ────────────────────────────────────────────────────────────

    /// <summary>
    /// Thrown when the current stream version does not match the expected version supplied to
    /// <see cref="IEventStore.AppendAsync"/>, indicating a concurrent write conflict.
    /// </summary>
    [Serializable]
    public sealed class WrongExpectedVersionException : Exception
    {
        public string StreamId        { get; }
        public long   ExpectedVersion { get; }
        public long   ActualVersion   { get; }

        public WrongExpectedVersionException(string streamId, long expectedVersion, long actualVersion)
            : base($"Stream '{streamId}': expected version {expectedVersion} but actual version is {actualVersion}.")
        {
            StreamId        = streamId;
            ExpectedVersion = expectedVersion;
            ActualVersion   = actualVersion;
        }

#pragma warning disable SYSLIB0051
        private WrongExpectedVersionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            StreamId        = info.GetString(nameof(StreamId));
            ExpectedVersion = info.GetInt64(nameof(ExpectedVersion));
            ActualVersion   = info.GetInt64(nameof(ActualVersion));
        }
#pragma warning restore SYSLIB0051
    }

    /// <summary>Thrown when reading or appending to a stream that does not exist.</summary>
    [Serializable]
    public sealed class StreamNotFoundException : Exception
    {
        public string StreamId { get; }

        public StreamNotFoundException(string streamId)
            : base($"Stream '{streamId}' was not found.")
        {
            StreamId = streamId;
        }

#pragma warning disable SYSLIB0051
        private StreamNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            StreamId = info.GetString(nameof(StreamId));
        }
#pragma warning restore SYSLIB0051
    }

    // ── IEventStore ───────────────────────────────────────────────────────────

    /// <summary>
    /// Append-only event log with optimistic concurrency control.
    /// Suitable for use as the write-side store in an Event Sourcing / CQRS stack.
    /// </summary>
    public interface IEventStore
    {
        /// <summary>
        /// Atomically appends <paramref name="events"/> to the stream identified by <paramref name="streamId"/>.
        /// </summary>
        /// <param name="streamId">Logical stream identifier (e.g. "Order-42").</param>
        /// <param name="events">One or more events to append in order.</param>
        /// <param name="expectedVersion">
        ///   Optimistic concurrency version.
        ///   Use <see cref="ExpectedVersion"/> sentinels for well-known expectations.
        /// </param>
        /// <returns>The new latest version after appending.</returns>
        /// <exception cref="WrongExpectedVersionException">
        ///   Current stream version does not match <paramref name="expectedVersion"/>.
        /// </exception>
        Task<long> AppendAsync(
            string                        streamId,
            IEnumerable<EventStoreRecord> events,
            long                          expectedVersion   = ExpectedVersion.Any,
            CancellationToken             cancellationToken = default);

        /// <summary>
        /// Reads a forward slice of events from <paramref name="streamId"/>.
        /// </summary>
        /// <param name="fromVersion">Inclusive start version (1-based). Default returns all events.</param>
        /// <param name="maxCount">Maximum number of events to return per slice.</param>
        /// <exception cref="StreamNotFoundException">Stream does not exist.</exception>
        Task<EventStoreSlice> LoadAsync(
            string            streamId,
            long              fromVersion       = 1,
            int               maxCount          = int.MaxValue,
            CancellationToken cancellationToken = default);

        /// <summary>Reads events in reverse order (newest first) — useful for snapshot-search.</summary>
        Task<EventStoreSlice> LoadBackwardsAsync(
            string            streamId,
            long              fromVersion       = long.MaxValue,
            int               maxCount          = int.MaxValue,
            CancellationToken cancellationToken = default);

        /// <summary>Permanently removes the stream and all its events.</summary>
        Task DeleteStreamAsync(string streamId, CancellationToken cancellationToken = default);

        /// <summary>Returns <c>true</c> if the stream exists (has at least one event).</summary>
        Task<bool> StreamExistsAsync(string streamId, CancellationToken cancellationToken = default);
    }
}
