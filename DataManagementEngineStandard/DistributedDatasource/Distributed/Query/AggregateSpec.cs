using System;

namespace TheTechIdea.Beep.Distributed.Query
{
    /// <summary>
    /// Immutable description of a single aggregate projection:
    /// <c>kind(column) AS alias</c>. Built by <see cref="QueryIntent"/>
    /// and consumed by both <see cref="QueryPlanner"/> and
    /// <see cref="QueryAwareResultMerger"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="Alias"/> drives how the merger emits the aggregate
    /// back into the row dictionary — callers that forget to alias
    /// are given a safe default (<c>kind_column</c> / <c>count_*</c>
    /// for <c>COUNT(*)</c>).
    /// </para>
    /// <para>
    /// When <see cref="Kind"/> is <see cref="AggregateKind.Avg"/> the
    /// planner rewrites the spec into two <see cref="PartialAggregate"/>
    /// entries — a <see cref="AggregateKind.Sum"/> and a matching
    /// <see cref="AggregateKind.Count"/> — tagged with the original
    /// alias so the merger can rebuild the average.
    /// </para>
    /// </remarks>
    public sealed class AggregateSpec
    {
        /// <summary>Creates a new aggregate spec.</summary>
        /// <param name="kind">Aggregate function to apply.</param>
        /// <param name="column">Target column; use <c>"*"</c> (or <c>null</c>) for <c>COUNT(*)</c>.</param>
        /// <param name="alias">Optional output alias. Defaulted when omitted.</param>
        public AggregateSpec(AggregateKind kind, string column, string alias = null)
        {
            Kind   = kind;
            Column = string.IsNullOrWhiteSpace(column) ? "*" : column;
            Alias  = string.IsNullOrWhiteSpace(alias) ? BuildDefaultAlias(kind, Column) : alias;
        }

        /// <summary>Aggregate function.</summary>
        public AggregateKind Kind { get; }

        /// <summary>Target column; <c>"*"</c> means "every row" (valid for <see cref="AggregateKind.Count"/>).</summary>
        public string Column { get; }

        /// <summary>Output alias in the merged row dictionary.</summary>
        public string Alias { get; }

        /// <summary>Convenience factory for <c>COUNT(*)</c>.</summary>
        public static AggregateSpec Count(string alias = "count_all")
            => new AggregateSpec(AggregateKind.Count, "*", alias);

        /// <summary>Convenience factory for <c>SUM(column)</c>.</summary>
        public static AggregateSpec Sum(string column, string alias = null)
            => new AggregateSpec(AggregateKind.Sum, column, alias);

        /// <summary>Convenience factory for <c>MIN(column)</c>.</summary>
        public static AggregateSpec Min(string column, string alias = null)
            => new AggregateSpec(AggregateKind.Min, column, alias);

        /// <summary>Convenience factory for <c>MAX(column)</c>.</summary>
        public static AggregateSpec Max(string column, string alias = null)
            => new AggregateSpec(AggregateKind.Max, column, alias);

        /// <summary>Convenience factory for <c>AVG(column)</c>.</summary>
        public static AggregateSpec Avg(string column, string alias = null)
            => new AggregateSpec(AggregateKind.Avg, column, alias);

        private static string BuildDefaultAlias(AggregateKind kind, string column)
        {
            var suffix = column == "*" ? "all" : column;
            return kind.ToString().ToLowerInvariant() + "_" + suffix;
        }

        /// <inheritdoc/>
        public override string ToString()
            => $"{Kind}({Column}) AS {Alias}";
    }
}
