using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Distributed.Query
{
    /// <summary>
    /// Structured description of a read request the Phase 08 planner
    /// can reason about: entity name, pushdown filters, column list,
    /// group/aggregate, order-by, and top/offset. Kept intentionally
    /// small — full SQL parsing remains out of scope for v1.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Callers typically build a <see cref="QueryIntent"/> either by
    /// hand (for programmatic read APIs) or via the lightweight
    /// <see cref="QuerySqlInspector"/> (Phase 08) that scans an
    /// incoming <c>RunQuery</c> string for <c>ORDER BY</c> / <c>TOP</c>
    /// keywords. The intent is immutable once constructed — modifying
    /// it after planning would defeat the plan cache.
    /// </para>
    /// <para>
    /// An intent with <see cref="GroupBy"/> = empty and
    /// <see cref="Aggregates"/> = empty and no
    /// <see cref="OrderBy"/>/<see cref="Top"/> is treated by the
    /// planner as a plain filter read (matches
    /// <see cref="MergeOperation.Union"/>).
    /// </para>
    /// </remarks>
    public sealed class QueryIntent
    {
        private static readonly IReadOnlyList<AppFilter>     NoFilters    = Array.Empty<AppFilter>();
        private static readonly IReadOnlyList<string>        NoColumns    = Array.Empty<string>();
        private static readonly IReadOnlyList<string>        NoGroupBy    = Array.Empty<string>();
        private static readonly IReadOnlyList<AggregateSpec> NoAggregates = Array.Empty<AggregateSpec>();
        private static readonly IReadOnlyList<OrderBySpec>   NoOrderBy    = Array.Empty<OrderBySpec>();

        /// <summary>Creates a new intent. Lists are copied defensively.</summary>
        /// <param name="entityName">Target entity; required.</param>
        /// <param name="filters">Optional pushdown filters.</param>
        /// <param name="columns">Optional projection (empty = "all").</param>
        /// <param name="groupBy">Optional group-by columns.</param>
        /// <param name="aggregates">Optional aggregate projections.</param>
        /// <param name="orderBy">Optional ordering.</param>
        /// <param name="top">Optional global <c>TOP</c> / <c>LIMIT</c>; &lt;= 0 means "no cap".</param>
        /// <param name="offset">Optional offset; &lt; 0 is clamped to 0.</param>
        /// <param name="rawSql">Optional original SQL text for observability.</param>
        public QueryIntent(
            string                            entityName,
            IReadOnlyList<AppFilter>          filters    = null,
            IReadOnlyList<string>             columns    = null,
            IReadOnlyList<string>             groupBy    = null,
            IReadOnlyList<AggregateSpec>      aggregates = null,
            IReadOnlyList<OrderBySpec>        orderBy    = null,
            int                               top        = 0,
            int                               offset     = 0,
            string                            rawSql     = null)
        {
            if (string.IsNullOrWhiteSpace(entityName))
                throw new ArgumentException("Entity name cannot be null or whitespace.", nameof(entityName));

            EntityName = entityName;
            Filters    = CopyOrEmpty(filters,    NoFilters);
            Columns    = CopyOrEmpty(columns,    NoColumns);
            GroupBy    = CopyOrEmpty(groupBy,    NoGroupBy);
            Aggregates = CopyOrEmpty(aggregates, NoAggregates);
            OrderBy    = CopyOrEmpty(orderBy,    NoOrderBy);
            Top        = top < 0 ? 0 : top;
            Offset     = offset < 0 ? 0 : offset;
            RawSql     = rawSql ?? string.Empty;
        }

        /// <summary>Target entity.</summary>
        public string                       EntityName { get; }

        /// <summary>Pushdown filters; empty means "no WHERE clause".</summary>
        public IReadOnlyList<AppFilter>     Filters    { get; }

        /// <summary>Projected columns; empty means "select all" (the underlying shard may still return everything).</summary>
        public IReadOnlyList<string>        Columns    { get; }

        /// <summary>GROUP BY columns; empty means "no group".</summary>
        public IReadOnlyList<string>        GroupBy    { get; }

        /// <summary>Aggregate projections; paired with <see cref="GroupBy"/> to form a group-aggregate.</summary>
        public IReadOnlyList<AggregateSpec> Aggregates { get; }

        /// <summary>ORDER BY columns; empty means "unspecified".</summary>
        public IReadOnlyList<OrderBySpec>   OrderBy    { get; }

        /// <summary><c>&gt; 0</c> caps the global merged result to that many rows.</summary>
        public int                          Top        { get; }

        /// <summary>Offset applied after the global merge.</summary>
        public int                          Offset     { get; }

        /// <summary>Original SQL text, when the intent came from <c>RunQuery</c>. Pure diagnostics.</summary>
        public string                       RawSql     { get; }

        /// <summary><c>true</c> when the intent carries any aggregate / group-by projection.</summary>
        public bool HasAggregates => Aggregates.Count > 0 || GroupBy.Count > 0;

        /// <summary><c>true</c> when the intent carries an ordering spec.</summary>
        public bool HasOrderBy    => OrderBy.Count > 0;

        /// <summary><c>true</c> when the intent asks for a top-N / limit.</summary>
        public bool HasTop        => Top > 0;

        /// <summary><c>true</c> when the intent is a plain filter/projection read.</summary>
        public bool IsSimple      => !HasAggregates && !HasOrderBy && !HasTop && Offset == 0;

        private static IReadOnlyList<T> CopyOrEmpty<T>(IReadOnlyList<T> source, IReadOnlyList<T> fallback)
        {
            if (source == null || source.Count == 0) return fallback;
            var copy = new List<T>(source.Count);
            for (int i = 0; i < source.Count; i++) copy.Add(source[i]);
            return copy;
        }

        /// <inheritdoc/>
        public override string ToString()
            => $"QueryIntent({EntityName}" +
               $"; filters={Filters.Count}" +
               $"; cols={Columns.Count}" +
               $"; groupBy={GroupBy.Count}" +
               $"; aggs={Aggregates.Count}" +
               $"; orderBy={OrderBy.Count}" +
               $"; top={Top}; offset={Offset})";
    }
}
