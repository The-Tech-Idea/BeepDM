using System.Collections.Generic;

namespace TheTechIdea.Beep.StreamandEvents
{
    /// <summary>
    /// Translates logical topic names and consumer group IDs into tenant-scoped physical names.
    /// Implementations are injected into <c>TenantAwareBrokerAdapter</c> to provide the
    /// naming strategy (prefix-based, hierarchical, etc.).
    /// </summary>
    public interface ITenantTopicResolver
    {
        /// <summary>
        /// Returns the physical broker topic name for <paramref name="logicalTopic"/>
        /// scoped to <paramref name="tenantId"/>.
        /// Example: <c>Resolve("acme", "orders")</c> → <c>"tenant.acme.orders"</c>.
        /// </summary>
        string Resolve(string tenantId, string logicalTopic);

        /// <summary>
        /// Returns the physical consumer group ID for <paramref name="consumerGroup"/>
        /// scoped to <paramref name="tenantId"/>.
        /// Example: <c>ResolveConsumerGroup("acme", "order-processor")</c> → <c>"tenant.acme.order-processor"</c>.
        /// </summary>
        string ResolveConsumerGroup(string tenantId, string consumerGroup);

        /// <summary>
        /// Extracts the tenant ID from a physical topic name, or <c>null</c> if the topic
        /// does not follow the tenant naming convention.
        /// </summary>
        string? ParseTenantId(string physicalTopic);

        /// <summary>
        /// Returns all physical topic names that are currently registered for <paramref name="tenantId"/>.
        /// May return an empty list if the resolver does not track topic ownership.
        /// </summary>
        IReadOnlyList<string> GetTopicsForTenant(string tenantId);
    }
}
