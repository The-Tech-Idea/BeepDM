namespace TheTechIdea.Beep.StreamandEvents
{
    /// <summary>
    /// Defines the subscription model that controls how messages are delivered to consumers.
    /// </summary>
    public enum SubscriptionType
    {
        /// <summary>
        /// Kafka-style consumer group: partitions assigned to consumers, at-most-one consumer per partition.
        /// Default behavior — backward compatible with existing code.
        /// </summary>
        ConsumerGroup = 0,

        /// <summary>
        /// Exactly one consumer per subscription. A second consumer attempt fails immediately.
        /// Use case: ordered processing, singleton workers.
        /// </summary>
        Exclusive,

        /// <summary>
        /// Round-robin dispatch across all consumers — no partition affinity.
        /// Use case: stateless work distribution, high fan-out.
        /// </summary>
        Shared,

        /// <summary>
        /// Active-standby: one active consumer, ordered standby list.
        /// On active failure, the next standby is promoted.
        /// Use case: HA with minimal rebalance.
        /// </summary>
        Failover,

        /// <summary>
        /// Hash-based sticky routing: messages with the same key always go to the same consumer.
        /// More flexible than partitions — buckets can be redistributed independently.
        /// Use case: per-key processing, session affinity.
        /// </summary>
        KeyShared
    }
}
