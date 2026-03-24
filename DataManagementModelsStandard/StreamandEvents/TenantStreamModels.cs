using System;

namespace TheTechIdea.Beep.StreamandEvents
{
    // ── Tenant context ────────────────────────────────────────────────────────

    /// <summary>Runtime context for a registered tenant in the multi-tenant stream isolation layer.</summary>
    public sealed class TenantStreamContext
    {
        /// <summary>Unique tenant identifier — used as the routing key.</summary>
        public string TenantId { get; init; }

        /// <summary>Human-readable tenant name (optional).</summary>
        public string? TenantName { get; init; }

        /// <summary>Whether the tenant is allowed to publish and consume. Modified by Suspend/Activate.</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>When the tenant was first registered.</summary>
        public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

        /// <summary>When the tenant was last suspended (null if never suspended or currently active).</summary>
        public DateTimeOffset? SuspendedAt { get; set; }

        /// <summary>Human-readable reason provided at suspend time.</summary>
        public string? SuspensionReason { get; set; }

        /// <summary>Per-tenant throughput and capacity limits.</summary>
        public TenantQuotaPolicy Quota { get; init; } = TenantQuotaPolicy.Default;
    }

    // ── Quota policy ──────────────────────────────────────────────────────────

    /// <summary>Throughput and capacity limits applied per tenant.</summary>
    public sealed class TenantQuotaPolicy
    {
        /// <summary>Max publish events per second. 0 = unlimited.</summary>
        public int MaxPublishRatePerSecond { get; init; }

        /// <summary>Max consume events per second. 0 = unlimited.</summary>
        public int MaxConsumeRatePerSecond { get; init; }

        /// <summary>Max in-flight messages per topic channel. 0 = unlimited.</summary>
        public int MaxChannelCapacityPerTopic { get; init; }

        /// <summary>Max number of distinct topics the tenant may create. 0 = unlimited.</summary>
        public int MaxTopicsAllowed { get; init; }

        /// <summary>Max concurrent consumer instances for this tenant. 0 = unlimited.</summary>
        public int MaxConcurrentConsumers { get; init; }

        /// <summary>Max allowed serialised message size in bytes. 0 = unlimited.</summary>
        public int MessageSizeLimitBytes { get; init; }

        /// <summary>All limits set to zero (unlimited).</summary>
        public static TenantQuotaPolicy Unlimited { get; } = new TenantQuotaPolicy();

        /// <summary>Sensible production defaults: 1 000 msg/s, 5 MB max message, 100 topics.</summary>
        public static TenantQuotaPolicy Default { get; } = new TenantQuotaPolicy
        {
            MaxPublishRatePerSecond    = 1_000,
            MaxConsumeRatePerSecond    = 1_000,
            MaxChannelCapacityPerTopic = 10_000,
            MaxTopicsAllowed           = 100,
            MaxConcurrentConsumers     = 50,
            MessageSizeLimitBytes      = 5 * 1024 * 1024   // 5 MB
        };
    }

    // ── Topic naming ──────────────────────────────────────────────────────────

    /// <summary>
    /// Helpers for converting between logical topic names and physical (tenant-scoped) topic names.
    /// Default format: <c>tenant.{tenantId}.{logicalTopic}</c>.
    /// </summary>
    public static class TenantTopicName
    {
        /// <summary>Separator character between name segments.</summary>
        public const string Separator = ".";

        private const string DefaultPrefix = "tenant";

        /// <summary>Formats a physical topic name from <paramref name="tenantId"/> and <paramref name="logicalTopic"/>.</summary>
        public static string Format(string tenantId, string logicalTopic)
            => $"{DefaultPrefix}{Separator}{tenantId}{Separator}{logicalTopic}";

        /// <summary>
        /// Parses a physical topic name back into its tenant and logical topic components.
        /// Returns <c>null</c> if the topic does not match the expected pattern.
        /// </summary>
        public static (string TenantId, string LogicalTopic)? Parse(string physicalTopic)
        {
            if (string.IsNullOrEmpty(physicalTopic)) return null;
            var parts = physicalTopic.Split('.');
            if (parts.Length < 3 || parts[0] != DefaultPrefix) return null;
            return (parts[1], string.Join(Separator, parts, 2, parts.Length - 2));
        }
    }

    /// <summary>Configuration knobs for tenant topic naming conventions.</summary>
    public sealed class TenantTopicNameOptions
    {
        /// <summary>Segment separator. Default: <c>"."</c>.</summary>
        public string Separator { get; set; } = ".";

        /// <summary>Left-most prefix added before the tenant ID. Null = no prefix.</summary>
        public string? Prefix { get; set; } = "tenant";

        /// <summary>When true, the tenant ID segment is uppercased in the physical topic name.</summary>
        public bool UseUpperCase { get; set; }
    }

    // ── Lifecycle events ──────────────────────────────────────────────────────

    /// <summary>Immutable event emitted by the tenant registry when tenant lifecycle state changes.</summary>
    public sealed class TenantStreamEvent
    {
        /// <summary>The affected tenant.</summary>
        public string TenantId { get; init; }

        /// <summary>One of the <see cref="Types"/> constants.</summary>
        public string EventType { get; init; }

        /// <summary>When the event occurred.</summary>
        public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;

        /// <summary>Optional human-readable detail (e.g. suspension reason).</summary>
        public string? Detail { get; init; }

        /// <summary>Well-known <see cref="TenantStreamEvent.EventType"/> constants.</summary>
        public static class Types
        {
            public const string Registered = "Registered";
            public const string Suspended  = "Suspended";
            public const string Activated  = "Activated";
            public const string Retired    = "Retired";
        }
    }
}
