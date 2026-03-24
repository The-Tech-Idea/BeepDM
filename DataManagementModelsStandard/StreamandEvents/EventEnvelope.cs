using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.StreamandEvents
{
    /// <summary>Broker-agnostic event envelope. All streaming events flow through this contract.</summary>
    public sealed class EventEnvelope<TPayload>
    {
        /// <summary>Unique event ID — deterministic from aggregate + sequence when possible.</summary>
        public string EventId { get; init; } = Guid.NewGuid().ToString();

        /// <summary>Idempotency key — same key on duplicate publish must produce the same effect.</summary>
        public string IdempotencyKey { get; init; }

        /// <summary>Correlation ID for distributed tracing.</summary>
        public string CorrelationId { get; init; }

        /// <summary>Causation ID (ID of the event that caused this one).</summary>
        public string CausationId { get; init; }

        /// <summary>Fully-qualified event type name — used for routing and schema dispatch.</summary>
        public string EventType { get; init; }

        /// <summary>Schema ID + version from the schema registry.</summary>
        public string SchemaId { get; init; }

        /// <summary>Domain-qualified topic name: <c>domain.aggregate.event.vMajor</c>.</summary>
        public string Topic { get; init; }

        /// <summary>UTC time when the event was produced at source.</summary>
        public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

        /// <summary>UTC time when the envelope was created (may differ from OccurredAt on replay).</summary>
        public DateTime EnvelopedAt { get; init; } = DateTime.UtcNow;

        /// <summary>Source service or data source that raised the event.</summary>
        public string Source { get; init; }

        /// <summary>Business payload.</summary>
        public TPayload Payload { get; init; }

        /// <summary>Propagated metadata headers (tracing, auth, compression hints, etc.).</summary>
        public EventHeaders Headers { get; init; } = new();

        /// <summary>Partition/ordering key; null = broker decides.</summary>
        public string PartitionKey { get; init; }

        /// <summary>Monotonically increasing sequence within the aggregate, if known.</summary>
        public long? SequenceNumber { get; init; }

        /// <summary>Marks a tombstone / compaction delete event.</summary>
        public bool IsTombstone { get; init; }
    }

    /// <summary>Carrier-level metadata propagated with every event.</summary>
    public sealed class EventHeaders
    {
        private readonly Dictionary<string, string> _values = new(StringComparer.OrdinalIgnoreCase);

        public string this[string key]
        {
            get => _values.TryGetValue(key, out var v) ? v : null;
            set => _values[key] = value;
        }

        public bool Contains(string key) => _values.ContainsKey(key);
        public IReadOnlyDictionary<string, string> All => _values;

        public void Set(string key, string value) => _values[key] = value;
        public bool TryGet(string key, out string value) => _values.TryGetValue(key, out value);
    }

    /// <summary>Non-generic metadata bag for infrastructure layers that do not know the payload type.</summary>
    public sealed class EventMetadata
    {
        public string EventId { get; init; }
        public string EventType { get; init; }
        public string Topic { get; init; }
        public string SchemaId { get; init; }
        public string CorrelationId { get; init; }
        public string PartitionKey { get; init; }
        public DateTime OccurredAt { get; init; }
        public EventHeaders Headers { get; init; } = new();
    }
}
