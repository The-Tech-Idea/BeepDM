using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Distributed.Audit
{
    /// <summary>
    /// Immutable audit record emitted by the distribution tier at
    /// every decision point. Instances are marshalled to
    /// JSON-lines by <see cref="FileDistributedAuditSink"/>; keep
    /// the shape additive so persisted logs remain readable across
    /// versions.
    /// </summary>
    public sealed class DistributedAuditEvent
    {
        /// <summary>Creates a new audit event.</summary>
        public DistributedAuditEvent(
            DistributedAuditEventKind    kind,
            DateTime                     timestampUtc,
            string                       correlationId,
            string                       entityName,
            string                       mode,
            string                       operation,
            IReadOnlyList<string>        shardIds,
            string                       partitionKey,
            string                       principal,
            string                       message,
            Exception                    error        = null,
            IReadOnlyDictionary<string, string> tags = null)
        {
            Kind          = kind;
            TimestampUtc  = timestampUtc;
            CorrelationId = correlationId ?? string.Empty;
            EntityName    = entityName    ?? string.Empty;
            Mode          = mode          ?? string.Empty;
            Operation     = operation     ?? string.Empty;
            ShardIds      = shardIds      ?? Array.Empty<string>();
            PartitionKey  = partitionKey  ?? string.Empty;
            Principal     = principal     ?? string.Empty;
            Message       = message       ?? string.Empty;
            Error         = error;
            Tags          = tags ?? EmptyTags;
        }

        private static readonly IReadOnlyDictionary<string, string> EmptyTags
            = new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>Kind of decision point captured.</summary>
        public DistributedAuditEventKind Kind { get; }

        /// <summary>Event timestamp (UTC).</summary>
        public DateTime TimestampUtc { get; }

        /// <summary>Correlation id linking related events for one logical operation.</summary>
        public string CorrelationId { get; }

        /// <summary>Entity name, when applicable.</summary>
        public string EntityName { get; }

        /// <summary>Distribution mode name ("Sharded", "Replicated", "Broadcast" …).</summary>
        public string Mode { get; }

        /// <summary>High-level operation name (GetEntity, UpdateEntity …).</summary>
        public string Operation { get; }

        /// <summary>Shards targeted by the operation.</summary>
        public IReadOnlyList<string> ShardIds { get; }

        /// <summary>Partition key (redacted for sensitive values).</summary>
        public string PartitionKey { get; }

        /// <summary>Calling principal (user/service account) if known.</summary>
        public string Principal { get; }

        /// <summary>Free-form human-readable message.</summary>
        public string Message { get; }

        /// <summary>Error captured at the decision point, or <c>null</c>.</summary>
        public Exception Error { get; }

        /// <summary>Extension tags for sink-specific extras.</summary>
        public IReadOnlyDictionary<string, string> Tags { get; }

        /// <summary>Factory that stamps the event at <see cref="DateTime.UtcNow"/>.</summary>
        public static DistributedAuditEvent Now(
            DistributedAuditEventKind    kind,
            string                       correlationId = null,
            string                       entityName    = null,
            string                       mode          = null,
            string                       operation     = null,
            IReadOnlyList<string>        shardIds      = null,
            string                       partitionKey  = null,
            string                       principal     = null,
            string                       message       = null,
            Exception                    error         = null,
            IReadOnlyDictionary<string, string> tags   = null)
            => new DistributedAuditEvent(
                kind, DateTime.UtcNow,
                correlationId, entityName, mode, operation,
                shardIds, partitionKey, principal, message, error, tags);
    }
}
