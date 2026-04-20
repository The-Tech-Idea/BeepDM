using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Distributed
{
    /// <summary>
    /// Per-call context that follows a distributed operation through
    /// resolution, execution, and observability hooks. Carries a stable
    /// correlation id so a single request can be stitched together
    /// across multiple <see cref="Distributed.Events.ShardSelectedEventArgs"/>
    /// emissions, executor logs, and (Phase 13) audit records.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The context is intentionally tiny and value-style: copy it
    /// across executor boundaries instead of mutating it in place. Use
    /// <see cref="WithTag"/> to derive a new context with an extra tag.
    /// </para>
    /// <para>
    /// Phase 03 only attaches contexts to the routing partial; later
    /// phases (read/write executors, transactions) carry the same
    /// instance forward so all events share the same correlation id.
    /// </para>
    /// </remarks>
    public sealed class DistributedExecutionContext
    {
        private readonly Dictionary<string, string> _tags;

        private DistributedExecutionContext(
            string                     correlationId,
            string                     operationName,
            string                     entityName,
            bool                       isWrite,
            DateTime                   startedUtc,
            Dictionary<string, string> tags)
        {
            CorrelationId = correlationId;
            OperationName = operationName;
            EntityName    = entityName;
            IsWrite       = isWrite;
            StartedUtc    = startedUtc;
            _tags         = tags ?? new Dictionary<string, string>(0, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>Stable id used to correlate every event emitted for this operation.</summary>
        public string CorrelationId { get; }

        /// <summary>Logical operation name (e.g. <c>"GetEntity"</c>, <c>"InsertEntity"</c>).</summary>
        public string OperationName { get; }

        /// <summary>Entity targeted by the operation; may be empty for plan-level calls.</summary>
        public string EntityName { get; }

        /// <summary>True for write-side operations; <c>false</c> for reads.</summary>
        public bool IsWrite { get; }

        /// <summary>UTC timestamp captured at context creation.</summary>
        public DateTime StartedUtc { get; }

        /// <summary>Read-only tag bag; use <see cref="WithTag"/> to derive an enriched context.</summary>
        public IReadOnlyDictionary<string, string> Tags => _tags;

        /// <summary>Creates a new context with a freshly generated correlation id.</summary>
        public static DistributedExecutionContext New(
            string operationName,
            string entityName = null,
            bool   isWrite    = false)
        {
            return new DistributedExecutionContext(
                correlationId: Guid.NewGuid().ToString("N"),
                operationName: operationName ?? string.Empty,
                entityName:    entityName    ?? string.Empty,
                isWrite:       isWrite,
                startedUtc:    DateTime.UtcNow,
                tags:          null);
        }

        /// <summary>Creates a context with a caller-supplied correlation id (e.g. from an upstream trace).</summary>
        public static DistributedExecutionContext FromCorrelation(
            string correlationId,
            string operationName,
            string entityName = null,
            bool   isWrite    = false)
        {
            return new DistributedExecutionContext(
                correlationId: string.IsNullOrWhiteSpace(correlationId) ? Guid.NewGuid().ToString("N") : correlationId,
                operationName: operationName ?? string.Empty,
                entityName:    entityName    ?? string.Empty,
                isWrite:       isWrite,
                startedUtc:    DateTime.UtcNow,
                tags:          null);
        }

        /// <summary>
        /// Returns a new context with an additional tag. The current
        /// instance is left unchanged so the context can be safely
        /// shared across executor boundaries.
        /// </summary>
        public DistributedExecutionContext WithTag(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key)) return this;

            var copy = new Dictionary<string, string>(_tags, StringComparer.OrdinalIgnoreCase)
            {
                [key] = value ?? string.Empty
            };
            return new DistributedExecutionContext(
                CorrelationId, OperationName, EntityName, IsWrite, StartedUtc, copy);
        }

        /// <inheritdoc/>
        public override string ToString()
            => $"DistCtx({OperationName}/{EntityName}, write={IsWrite}, corr={CorrelationId})";
    }
}
