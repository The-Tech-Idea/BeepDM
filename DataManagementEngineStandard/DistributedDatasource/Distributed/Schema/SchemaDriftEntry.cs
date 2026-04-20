using System;

namespace TheTechIdea.Beep.Distributed.Schema
{
    /// <summary>
    /// Single per-shard drift record produced by
    /// <see cref="IDistributedSchemaService.DetectSchemaDriftAsync"/>.
    /// Records the entity, the shard that disagreed with the reference
    /// shard, the drift kind, the expected / observed values, and an
    /// optional underlying exception.
    /// </summary>
    /// <remarks>
    /// Instances are immutable so a drift report can be cached, serialised
    /// to JSON for CI gating, or streamed through the audit pipeline
    /// without defensive copies. Column-level drift populates
    /// <see cref="ColumnName"/>; entity-level drift leaves it <c>null</c>.
    /// </remarks>
    public sealed class SchemaDriftEntry
    {
        /// <summary>Initialises a new drift entry.</summary>
        public SchemaDriftEntry(
            string          entityName,
            string          shardId,
            SchemaDriftKind kind,
            string          columnName        = null,
            string          expectedValue     = null,
            string          observedValue     = null,
            Exception       samplingException = null)
        {
            if (string.IsNullOrWhiteSpace(entityName))
                throw new ArgumentException("Entity name cannot be null or whitespace.", nameof(entityName));
            if (string.IsNullOrWhiteSpace(shardId))
                throw new ArgumentException("Shard id cannot be null or whitespace.", nameof(shardId));

            EntityName        = entityName;
            ShardId           = shardId;
            Kind              = kind;
            ColumnName        = columnName;
            ExpectedValue     = expectedValue;
            ObservedValue     = observedValue;
            SamplingException = samplingException;
        }

        /// <summary>Entity whose schema drifted. Always set.</summary>
        public string EntityName { get; }

        /// <summary>Shard that disagreed with the reference. Always set.</summary>
        public string ShardId { get; }

        /// <summary>Classifies what drifted.</summary>
        public SchemaDriftKind Kind { get; }

        /// <summary>Column that drifted for column-level entries; <c>null</c> for entity-level drift.</summary>
        public string ColumnName { get; }

        /// <summary>Reference value (from the canonical shard). <c>null</c> when not applicable.</summary>
        public string ExpectedValue { get; }

        /// <summary>Observed value on <see cref="ShardId"/>. <c>null</c> when not applicable.</summary>
        public string ObservedValue { get; }

        /// <summary>
        /// Set only when <see cref="Kind"/> is
        /// <see cref="SchemaDriftKind.SamplingFailed"/>; carries the
        /// underlying provider exception for diagnostics.
        /// </summary>
        public Exception SamplingException { get; }

        /// <inheritdoc/>
        public override string ToString()
        {
            var column = string.IsNullOrWhiteSpace(ColumnName) ? string.Empty : "." + ColumnName;
            var values = ExpectedValue == null && ObservedValue == null
                ? string.Empty
                : $" (expected='{ExpectedValue ?? "(null)"}', observed='{ObservedValue ?? "(null)"}')";
            return $"{EntityName}{column}@{ShardId} {Kind}{values}";
        }
    }
}
