using System.Diagnostics;

namespace TheTechIdea.Beep.Distributed.Observability
{
    /// <summary>
    /// Shared <see cref="ActivitySource"/> for the distribution
    /// tier. Callers that want OpenTelemetry traces register a
    /// listener for <see cref="SourceName"/>; the distribution
    /// datasource starts a root activity around every
    /// router-scoped operation with the tags listed below.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Standard tags written on each activity:
    /// <c>beep.shard.ids</c>, <c>beep.entity</c>,
    /// <c>beep.mode</c>, <c>beep.match.kind</c>,
    /// <c>beep.partition.key</c>, <c>beep.correlation.id</c>,
    /// <c>beep.operation</c>.
    /// </para>
    /// <para>
    /// No listener ⇒ zero overhead: <see cref="ActivitySource"/>
    /// skips the allocation entirely when nothing is subscribed.
    /// </para>
    /// </remarks>
    public static class DistributedActivitySource
    {
        /// <summary>Activity source name consumers subscribe to.</summary>
        public const string SourceName = "Beep.Distributed.DataSource";

        /// <summary>Activity source version reported on start.</summary>
        public const string SourceVersion = "1.0.0";

        /// <summary>Shared source instance used by the distributed tier.</summary>
        public static readonly ActivitySource Source = new ActivitySource(SourceName, SourceVersion);

        // ── Tag names (kept as constants so string literals never drift) ──

        /// <summary>Tag: comma-separated shard ids targeted by the operation.</summary>
        public const string TagShardIds       = "beep.shard.ids";
        /// <summary>Tag: entity name.</summary>
        public const string TagEntity         = "beep.entity";
        /// <summary>Tag: distribution mode (Sharded, Replicated, Broadcast …).</summary>
        public const string TagMode           = "beep.mode";
        /// <summary>Tag: placement match kind (Exact, Prefix, Default, Broadcast).</summary>
        public const string TagMatchKind      = "beep.match.kind";
        /// <summary>Tag: partition key (stringified, redacted for sensitive values).</summary>
        public const string TagPartitionKey   = "beep.partition.key";
        /// <summary>Tag: correlation id.</summary>
        public const string TagCorrelationId  = "beep.correlation.id";
        /// <summary>Tag: high-level operation name (GetEntity, UpdateEntity …).</summary>
        public const string TagOperation      = "beep.operation";

        /// <summary>
        /// Starts an activity scoped to a distribution-tier
        /// operation. Returns <c>null</c> when no listener is
        /// attached, so callers should use the
        /// <c>using var activity = …</c> pattern.
        /// </summary>
        public static Activity StartActivity(
            string operation,
            ActivityKind kind = ActivityKind.Client)
        {
            return Source.StartActivity(operation, kind);
        }

        /// <summary>
        /// Convenience helper that sets the standard distribution
        /// tags on an activity (all parameters optional).
        /// </summary>
        public static Activity SetDistributedTags(
            this Activity activity,
            string entity        = null,
            string mode          = null,
            string matchKind     = null,
            string partitionKey  = null,
            string shardIds      = null,
            string correlationId = null)
        {
            if (activity == null) return null;
            if (!string.IsNullOrEmpty(entity))        activity.SetTag(TagEntity,        entity);
            if (!string.IsNullOrEmpty(mode))          activity.SetTag(TagMode,          mode);
            if (!string.IsNullOrEmpty(matchKind))     activity.SetTag(TagMatchKind,     matchKind);
            if (!string.IsNullOrEmpty(partitionKey))  activity.SetTag(TagPartitionKey,  partitionKey);
            if (!string.IsNullOrEmpty(shardIds))      activity.SetTag(TagShardIds,      shardIds);
            if (!string.IsNullOrEmpty(correlationId)) activity.SetTag(TagCorrelationId, correlationId);
            return activity;
        }
    }
}
