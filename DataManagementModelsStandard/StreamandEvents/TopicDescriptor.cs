using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.StreamandEvents
{
    /// <summary>
    /// Describes a topic and its runtime/governance properties.
    /// Topic naming pattern: <c>domain.aggregate.event.vMajor</c>
    /// </summary>
    public sealed class TopicDescriptor
    {
        /// <summary>Full topic name — must follow naming convention.</summary>
        public string TopicName { get; init; }

        /// <summary>Domain segment (e.g. "inventory", "orders").</summary>
        public string Domain { get; init; }

        /// <summary>Aggregate root that owns this event stream.</summary>
        public string Aggregate { get; init; }

        /// <summary>Event name segment (e.g. "OrderPlaced").</summary>
        public string EventName { get; init; }

        /// <summary>Major schema version. Increment on breaking change.</summary>
        public int SchemaMajorVersion { get; init; } = 1;

        public int PartitionCount { get; init; } = 1;
        public int ReplicationFactor { get; init; } = 1;

        /// <summary>Data retention: e.g. "7d", "30d", "compact".</summary>
        public string RetentionPolicy { get; init; } = "7d";

        /// <summary>Owning team/service responsible for governance.</summary>
        public string OwnerService { get; init; }

        public bool IsCompacted { get; init; }
        public bool IsTombstoneAllowed { get; init; }

        /// <summary>Required consumer schema compatibility mode for subscribers.</summary>
        public SchemaCompatibilityMode RequiredCompatibilityMode { get; init; } = SchemaCompatibilityMode.Backward;

        public Dictionary<string, string> AdditionalOptions { get; init; } = new();

        /// <summary>Derives the canonical topic name from components when TopicName is null.</summary>
        public string ResolvedTopicName =>
            TopicName ?? $"{Domain}.{Aggregate}.{EventName}.v{SchemaMajorVersion}".ToLowerInvariant();
    }

    /// <summary>Describes how a service subscribes to a topic.</summary>
    public sealed class SubscriptionDescriptor
    {
        public string TopicName { get; init; }
        public ConsumerGroupPolicy GroupPolicy { get; init; }

        /// <summary>Schema versions this consumer supports (for compatibility gating).</summary>
        public IReadOnlyList<int> SupportedSchemaVersions { get; init; } = Array.Empty<int>();

        public DeliverySemantics DeliverySemantics { get; init; } = DeliverySemantics.AtLeastOnce;
        public AckStrategy AckStrategy { get; init; } = AckStrategy.AfterHandler;

        /// <summary>
        /// Duplicate-event handling policy. When null, defaults are derived from <see cref="DeliverySemantics"/>:
        /// enabled for AtLeastOnce/ExactlyOnce, disabled for AtMostOnce.
        /// </summary>
        public DuplicateEventPolicy DuplicatePolicy { get; init; }

        /// <summary>
        /// Subscription type (ConsumerGroup, Exclusive, Shared, Failover, KeyShared).
        /// Defaults to ConsumerGroup for backward compatibility.
        /// </summary>
        public SubscriptionType SubscriptionType { get; init; } = SubscriptionType.ConsumerGroup;

        /// <summary>Logical name for the subscription (used with non-ConsumerGroup types).</summary>
        public string SubscriptionName { get; init; }

        /// <summary>Number of hash buckets for KeyShared subscriptions (default 256).</summary>
        public int KeySharedBuckets { get; init; } = 256;
    }

    /// <summary>Consumer group configuration.</summary>
    public sealed class ConsumerGroupPolicy
    {
        public string GroupId { get; init; }
        public int MaxConcurrency { get; init; } = 1;

        /// <summary>
        /// When true, ordering is preserved per partition key by serialising handlers.
        /// When false, handlers may execute concurrently for independent keys.
        /// </summary>
        public bool PreserveOrdering { get; init; } = true;

        /// <summary>Drain timeout on shutdown before forcibly closing.</summary>
        public TimeSpan ShutdownDrainTimeout { get; init; } = TimeSpan.FromSeconds(30);

        /// <summary>Auto-commit (at-most-once) vs manual ack (at-least-once / exactly-once).</summary>
        public bool AutoCommit { get; init; } = false;
    }
}
