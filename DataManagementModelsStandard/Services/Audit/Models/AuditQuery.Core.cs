using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Services.Audit.Models
{
    /// <summary>
    /// Phase 10 partial extending <see cref="AuditQuery"/> with the fluent
    /// builder API. Filter values from Phase 01 are still respected; the
    /// builder methods simply assign the same backing properties so calls
    /// from older clients keep compiling unchanged.
    /// </summary>
    /// <remarks>
    /// All builder methods return <c>this</c> so callers can chain
    /// (<c>new AuditQuery().User("u123").Between(from, to).Take(100)</c>).
    /// Engine-specific fields (<see cref="OrderByField"/>,
    /// <see cref="OrderDescending"/>, <see cref="PropertyFilters"/>) live
    /// on the partial so the Phase 01 contract surface stays minimal for
    /// hosts that prefer the property-bag style.
    /// </remarks>
    public partial class AuditQuery
    {
        /// <summary>
        /// Optional <c>category</c> filter. <c>null</c> = any category.
        /// </summary>
        public AuditCategory? CategoryFilter { get; set; }

        /// <summary>
        /// Optional <c>operation</c> filter (case-insensitive equality).
        /// </summary>
        public string OperationFilter { get; set; }

        /// <summary>
        /// Optional <c>outcome</c> filter. <c>null</c> = any outcome.
        /// </summary>
        public AuditOutcome? OutcomeFilter { get; set; }

        /// <summary>
        /// Optional <c>chainId</c> filter; useful when audit volumes are
        /// segmented by tenant or category.
        /// </summary>
        public string ChainIdFilter { get; set; }

        /// <summary>
        /// Optional property-bag predicates. Engines apply equality
        /// matching using the canonical scalar form.
        /// </summary>
        public IDictionary<string, object> PropertyFilters { get; } =
            new Dictionary<string, object>(StringComparer.Ordinal);

        /// <summary>
        /// Field name used for ordering. Supported values: <c>"ts"</c>
        /// (default), <c>"sequence"</c>, <c>"user"</c>, <c>"entity"</c>.
        /// </summary>
        public string OrderByField { get; set; } = "ts";

        /// <summary>
        /// When <c>true</c> (the default) results are ordered descending
        /// so the most recent records appear first.
        /// </summary>
        public bool OrderDescending { get; set; } = true;

        /// <summary>Sets the <c>category</c> filter.</summary>
        public AuditQuery Category(AuditCategory category)
        {
            CategoryFilter = category;
            return this;
        }

        /// <summary>Sets the <c>operation</c> filter.</summary>
        public AuditQuery Operation(string operation)
        {
            OperationFilter = operation;
            return this;
        }

        /// <summary>Sets the <c>outcome</c> filter.</summary>
        public AuditQuery OutcomeFilterValue(AuditOutcome outcome)
        {
            OutcomeFilter = outcome;
            return this;
        }

        /// <summary>Sets the <c>userId</c> filter.</summary>
        public AuditQuery User(string userId)
        {
            UserId = userId;
            return this;
        }

        /// <summary>Sets the <c>tenant</c> filter.</summary>
        public AuditQuery TenantId(string tenant)
        {
            Tenant = tenant;
            return this;
        }

        /// <summary>Sets the entity-name (and optional record-key) filter.</summary>
        public AuditQuery Entity(string entityName, string recordKey = null)
        {
            EntityName = entityName;
            RecordKey = recordKey;
            return this;
        }

        /// <summary>Sets the <c>source</c> filter.</summary>
        public AuditQuery SourceFilter(string source)
        {
            Source = source;
            return this;
        }

        /// <summary>Sets the inclusive <c>[fromUtc..toUtc]</c> window.</summary>
        public AuditQuery Between(DateTime fromUtc, DateTime toUtc)
        {
            FromUtc = fromUtc;
            ToUtc = toUtc;
            return this;
        }

        /// <summary>Sets the <c>chainId</c> filter.</summary>
        public AuditQuery ChainId(string chainId)
        {
            ChainIdFilter = chainId;
            return this;
        }

        /// <summary>Adds a property predicate (equality).</summary>
        public AuditQuery WithProperty(string key, object value)
        {
            if (string.IsNullOrEmpty(key))
            {
                return this;
            }
            PropertyFilters[key] = value;
            return this;
        }

        /// <summary>Sets the order-by field and direction.</summary>
        public AuditQuery OrderBy(string field, bool descending = true)
        {
            if (!string.IsNullOrEmpty(field))
            {
                OrderByField = field;
            }
            OrderDescending = descending;
            return this;
        }

        /// <summary>Sets the maximum number of records to return.</summary>
        public AuditQuery TakeMax(int max)
        {
            Take = max < 0 ? 0 : max;
            return this;
        }
    }
}
