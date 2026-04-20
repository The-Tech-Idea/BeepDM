using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Distributed.Query
{
    /// <summary>
    /// Partial class carrying the <see cref="MergeOperation.GroupAggregate"/>
    /// path. Responsible for combining per-shard partial aggregate
    /// rows into a single globally-aggregated row per group, then
    /// re-pairing AVG sum/count components and applying any
    /// outstanding <c>ORDER BY</c> / <c>TOP</c> from the merge spec.
    /// </summary>
    public sealed partial class QueryAwareResultMerger
    {
        private IEnumerable<object> MergeGroupAggregate(
            MergeSpec                           merge,
            IReadOnlyList<IEnumerable<object>>  perShardResults)
        {
            var groups = new Dictionary<string, GroupBucket>(StringComparer.Ordinal);
            var order  = new List<string>();

            for (int shardIndex = 0; shardIndex < perShardResults.Count; shardIndex++)
            {
                var shardRows = perShardResults[shardIndex];
                if (shardRows == null) continue;

                foreach (var row in shardRows)
                {
                    if (row == null) continue;

                    var key = BuildGroupKey(row, merge.GroupBy, out var groupValues);
                    if (!groups.TryGetValue(key, out var bucket))
                    {
                        bucket = new GroupBucket(merge.GroupBy, groupValues, merge.Aggregates);
                        groups.Add(key, bucket);
                        order.Add(key);
                    }

                    bucket.FoldPartialRow(row);
                }
            }

            // Emit group rows as case-insensitive string→object dictionaries so downstream
            // consumers (report renderers, tests) can read both group keys and aggregate aliases uniformly.
            var output = new List<object>(groups.Count);
            foreach (var key in order)
            {
                output.Add(groups[key].Emit());
            }

            // Post-aggregate ORDER BY / TOP / OFFSET — same algorithm as the sort-merge path.
            if (merge.OrderBy.Count > 0 || merge.HasTopN || merge.HasOffset)
            {
                var rowsView = new IEnumerable<object>[] { output };
                var sorted   = CollectAndSort(merge.OrderBy, rowsView);
                return ApplyOffsetAndLimit(sorted, merge.Offset, merge.TopN);
            }

            return output;
        }

        private static string BuildGroupKey(
            object                  row,
            IReadOnlyList<string>   groupBy,
            out object[]            groupValues)
        {
            if (groupBy.Count == 0)
            {
                groupValues = Array.Empty<object>();
                return string.Empty;
            }

            groupValues = new object[groupBy.Count];
            var parts   = new string[groupBy.Count];

            for (int i = 0; i < groupBy.Count; i++)
            {
                var raw = RowValueExtractor.GetValueOrDefault(row, groupBy[i]);
                groupValues[i] = raw;
                parts[i]       = raw == null ? "\u0000" : Convert.ToString(raw);
            }

            return string.Join("\u001f", parts);
        }

        // ── Per-group bucket ──────────────────────────────────────────────

        private sealed class GroupBucket
        {
            private readonly IReadOnlyList<string>                 _groupColumns;
            private readonly object[]                              _groupValues;
            private readonly IReadOnlyList<PartialAggregate>       _partials;
            private readonly AggregateAccumulator[]                _accumulators;

            public GroupBucket(
                IReadOnlyList<string>           groupColumns,
                object[]                        groupValues,
                IReadOnlyList<PartialAggregate> partials)
            {
                _groupColumns = groupColumns;
                _groupValues  = groupValues;
                _partials     = partials;
                _accumulators = new AggregateAccumulator[partials.Count];

                for (int i = 0; i < partials.Count; i++)
                {
                    _accumulators[i] = new AggregateAccumulator(partials[i].Kind);
                }
            }

            public void FoldPartialRow(object row)
            {
                for (int i = 0; i < _partials.Count; i++)
                {
                    var partial = _partials[i];
                    var value   = RowValueExtractor.GetValueOrDefault(row, partial.PartialKey);
                    _accumulators[i].Add(value);
                }
            }

            public IDictionary<string, object> Emit()
            {
                var result = new Dictionary<string, object>(
                    _groupColumns.Count + _partials.Count,
                    StringComparer.OrdinalIgnoreCase);

                for (int i = 0; i < _groupColumns.Count; i++)
                {
                    result[_groupColumns[i]] = _groupValues[i];
                }

                // First pass: write non-AVG partials directly; collect AVG Sum/Count pairs.
                Dictionary<string, object> sums     = null;
                Dictionary<string, object> counts   = null;

                for (int i = 0; i < _partials.Count; i++)
                {
                    var partial = _partials[i];
                    var value   = _accumulators[i].GetResult();

                    if (!partial.IsAvgComponent)
                    {
                        result[partial.OutputAlias] = value;
                        continue;
                    }

                    if (partial.Kind == AggregateKind.Sum)
                    {
                        (sums ??= new Dictionary<string, object>(StringComparer.Ordinal))[partial.AvgPairAlias] = value;
                    }
                    else if (partial.Kind == AggregateKind.Count)
                    {
                        (counts ??= new Dictionary<string, object>(StringComparer.Ordinal))[partial.AvgPairAlias] = value;
                    }
                }

                // Second pass: divide sum/count pairs to produce the final AVG value.
                if (sums != null && counts != null)
                {
                    foreach (var pair in sums)
                    {
                        counts.TryGetValue(pair.Key, out var countValue);
                        result[pair.Key] = AggregateAccumulator.DivideAverage(pair.Value, countValue);
                    }
                }

                return result;
            }
        }
    }
}
