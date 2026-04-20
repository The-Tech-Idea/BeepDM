using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Distributed.Query
{
    /// <summary>
    /// Declarative description of the cross-shard merge step the
    /// <see cref="QueryAwareResultMerger"/> runs after every shard
    /// has returned its partial result. Attached to
    /// <see cref="QueryPlan.Merge"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A <see cref="MergeSpec"/> is produced by the Phase 08 planner
    /// alongside the per-shard sub-intents. Keeping the merge
    /// description separate from the planner lets the runtime swap
    /// out the planner while reusing the same merge engine (useful
    /// when a future phase layers a smarter planner on top of the
    /// basic one).
    /// </para>
    /// <para>
    /// <see cref="Operation"/> drives the behaviour; all other
    /// properties are optional and consumed only by the operations
    /// that need them (<see cref="TopN"/> only for
    /// <see cref="MergeOperation.TopN"/>,
    /// <see cref="GroupBy"/>/<see cref="Aggregates"/> only for
    /// <see cref="MergeOperation.GroupAggregate"/>, etc.).
    /// </para>
    /// </remarks>
    public sealed class MergeSpec
    {
        private static readonly IReadOnlyList<string>           NoGroupBy    = Array.Empty<string>();
        private static readonly IReadOnlyList<PartialAggregate> NoAggregates = Array.Empty<PartialAggregate>();
        private static readonly IReadOnlyList<OrderBySpec>      NoOrderBy    = Array.Empty<OrderBySpec>();

        private MergeSpec(
            MergeOperation                       operation,
            IReadOnlyList<OrderBySpec>           orderBy,
            int                                  topN,
            int                                  offset,
            IReadOnlyList<string>                groupBy,
            IReadOnlyList<PartialAggregate>      aggregates)
        {
            Operation  = operation;
            OrderBy    = orderBy    ?? NoOrderBy;
            TopN       = topN < 0 ? 0 : topN;
            Offset     = offset < 0 ? 0 : offset;
            GroupBy    = groupBy    ?? NoGroupBy;
            Aggregates = aggregates ?? NoAggregates;
        }

        /// <summary>Which merge algorithm to run.</summary>
        public MergeOperation Operation { get; }

        /// <summary>Order-by columns for <see cref="MergeOperation.TopN"/> / <see cref="MergeOperation.SortMerge"/>.</summary>
        public IReadOnlyList<OrderBySpec> OrderBy { get; }

        /// <summary>Top-N limit for <see cref="MergeOperation.TopN"/>; <c>0</c> means no limit.</summary>
        public int TopN { get; }

        /// <summary>Offset applied after the merged result is produced. <c>0</c> means "no offset".</summary>
        public int Offset { get; }

        /// <summary>Group-by columns for <see cref="MergeOperation.GroupAggregate"/>.</summary>
        public IReadOnlyList<string> GroupBy { get; }

        /// <summary>Partial aggregates the per-shard sub-intents emit; the merger folds them by group.</summary>
        public IReadOnlyList<PartialAggregate> Aggregates { get; }

        /// <summary><c>true</c> when <see cref="TopN"/> is &gt; 0.</summary>
        public bool HasTopN => TopN > 0;

        /// <summary><c>true</c> when <see cref="Offset"/> is &gt; 0.</summary>
        public bool HasOffset => Offset > 0;

        /// <summary>Produces a plain union merge (the Phase 06 default).</summary>
        public static MergeSpec Union()
            => new MergeSpec(MergeOperation.Union, null, 0, 0, null, null);

        /// <summary>Produces a top-N merge with an order-by.</summary>
        public static MergeSpec ForTopN(IReadOnlyList<OrderBySpec> orderBy, int topN, int offset = 0)
            => new MergeSpec(MergeOperation.TopN, orderBy, topN, offset, null, null);

        /// <summary>Produces a sort-merge without a row cap.</summary>
        public static MergeSpec SortMerge(IReadOnlyList<OrderBySpec> orderBy, int offset = 0)
            => new MergeSpec(MergeOperation.SortMerge, orderBy, 0, offset, null, null);

        /// <summary>
        /// Produces a group-aggregate merge. The merger preserves
        /// <paramref name="orderBy"/> / <paramref name="topN"/> so
        /// <c>GROUP BY ... ORDER BY ... TOP N</c> flows through the
        /// same merge pass.
        /// </summary>
        public static MergeSpec GroupAggregate(
            IReadOnlyList<string>           groupBy,
            IReadOnlyList<PartialAggregate> aggregates,
            IReadOnlyList<OrderBySpec>      orderBy = null,
            int                             topN    = 0,
            int                             offset  = 0)
            => new MergeSpec(MergeOperation.GroupAggregate, orderBy, topN, offset, groupBy, aggregates);

        /// <inheritdoc/>
        public override string ToString()
            => $"MergeSpec({Operation}" +
               $"; groupBy={GroupBy.Count}; aggs={Aggregates.Count}" +
               $"; orderBy={OrderBy.Count}; top={TopN}; offset={Offset})";
    }

    /// <summary>
    /// Describes one partial aggregate the merger expects on every
    /// shard's result row. Lives next to <see cref="MergeSpec"/>
    /// because the planner produces these as a byproduct of
    /// <c>AVG</c> rewriting.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For plain aggregates (<see cref="AggregateKind.Count"/>,
    /// <see cref="AggregateKind.Sum"/>, <see cref="AggregateKind.Min"/>,
    /// <see cref="AggregateKind.Max"/>) the merger folds values under
    /// <see cref="PartialKey"/> and emits the result under
    /// <see cref="OutputAlias"/>.
    /// </para>
    /// <para>
    /// For <see cref="AggregateKind.Avg"/> the planner emits two
    /// <see cref="PartialAggregate"/> entries with
    /// <see cref="AvgPairAlias"/> = original alias: one for the SUM
    /// side (<see cref="Kind"/> == <see cref="AggregateKind.Sum"/>)
    /// and one for the COUNT side
    /// (<see cref="Kind"/> == <see cref="AggregateKind.Count"/>).
    /// The merger detects the pair via <see cref="AvgPairAlias"/> and
    /// divides after folding to produce the final average.
    /// </para>
    /// </remarks>
    public sealed class PartialAggregate
    {
        /// <summary>Creates a new partial aggregate descriptor.</summary>
        /// <param name="kind">Partial aggregate kind (Count / Sum / Min / Max).</param>
        /// <param name="column">Source column; <c>"*"</c> for COUNT(*).</param>
        /// <param name="partialKey">Row-dictionary key produced by each shard.</param>
        /// <param name="outputAlias">Alias on the merged output row.</param>
        /// <param name="avgPairAlias">
        /// When non-<c>null</c>, this partial is one half of an AVG
        /// pair; the merger matches Sum/Count pairs by this value.
        /// </param>
        public PartialAggregate(
            AggregateKind kind,
            string        column,
            string        partialKey,
            string        outputAlias,
            string        avgPairAlias = null)
        {
            if (string.IsNullOrWhiteSpace(partialKey))
                throw new ArgumentException("Partial key cannot be null or whitespace.", nameof(partialKey));
            if (string.IsNullOrWhiteSpace(outputAlias))
                throw new ArgumentException("Output alias cannot be null or whitespace.", nameof(outputAlias));

            Kind         = kind;
            Column       = string.IsNullOrWhiteSpace(column) ? "*" : column;
            PartialKey   = partialKey;
            OutputAlias  = outputAlias;
            AvgPairAlias = avgPairAlias;
        }

        /// <summary>Aggregate kind (never <see cref="AggregateKind.Avg"/> — avg is split by the planner).</summary>
        public AggregateKind Kind { get; }

        /// <summary>Source column; <c>"*"</c> means every row.</summary>
        public string Column { get; }

        /// <summary>Row-dictionary key produced by each shard (input to the merger).</summary>
        public string PartialKey { get; }

        /// <summary>Alias on the merged output row.</summary>
        public string OutputAlias { get; }

        /// <summary>Non-<c>null</c> when this partial is half of an AVG split; the merger uses it to pair Sum + Count.</summary>
        public string AvgPairAlias { get; }

        /// <summary><c>true</c> when <see cref="AvgPairAlias"/> is set.</summary>
        public bool IsAvgComponent => !string.IsNullOrEmpty(AvgPairAlias);

        /// <inheritdoc/>
        public override string ToString()
            => $"{Kind}({Column}) via '{PartialKey}' -> '{OutputAlias}'" +
               (IsAvgComponent ? $" [avgPair={AvgPairAlias}]" : string.Empty);
    }
}
