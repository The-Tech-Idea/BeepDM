using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Distributed.Query
{
    /// <summary>
    /// Partial class carrying the Top-N and SortMerge paths. The
    /// implementation is a simple list-sort + offset/limit; a full
    /// heap-based k-way merge can replace it transparently once shard
    /// results are streamed lazily.
    /// </summary>
    public sealed partial class QueryAwareResultMerger
    {
        // Top-N and SortMerge share the same core algorithm — collect
        // pre-sorted per-shard rows, sort globally, then apply
        // offset/top. Cross-shard ordering ties are broken by shard
        // index to keep the output deterministic when the ORDER BY
        // key values are equal.

        private IEnumerable<object> MergeTopN(
            MergeSpec                           merge,
            IReadOnlyList<IEnumerable<object>>  perShardResults)
        {
            var sorted = CollectAndSort(merge.OrderBy, perShardResults);
            return ApplyOffsetAndLimit(sorted, merge.Offset, merge.TopN);
        }

        private IEnumerable<object> MergeSortMerge(
            MergeSpec                           merge,
            IReadOnlyList<IEnumerable<object>>  perShardResults)
        {
            var sorted = CollectAndSort(merge.OrderBy, perShardResults);
            return ApplyOffsetAndLimit(sorted, merge.Offset, 0);
        }

        private static List<object> CollectAndSort(
            IReadOnlyList<OrderBySpec>          orderBy,
            IReadOnlyList<IEnumerable<object>>  perShardResults)
        {
            // Row + originating shard index so we can break ties deterministically.
            var tagged = new List<(object Row, int ShardIndex)>();

            for (int shardIndex = 0; shardIndex < perShardResults.Count; shardIndex++)
            {
                var shardRows = perShardResults[shardIndex];
                if (shardRows == null) continue;

                foreach (var row in shardRows)
                {
                    if (row == null) continue;
                    tagged.Add((row, shardIndex));
                }
            }

            if (orderBy == null || orderBy.Count == 0)
            {
                var passthrough = new List<object>(tagged.Count);
                foreach (var item in tagged) passthrough.Add(item.Row);
                return passthrough;
            }

            tagged.Sort((left, right) =>
            {
                for (int i = 0; i < orderBy.Count; i++)
                {
                    var spec = orderBy[i];
                    var l    = RowValueExtractor.GetValueOrDefault(left.Row,  spec.Column);
                    var r    = RowValueExtractor.GetValueOrDefault(right.Row, spec.Column);

                    int cmp = CompareSortKeys(l, r);
                    if (spec.IsDescending) cmp = -cmp;
                    if (cmp != 0) return cmp;
                }

                return left.ShardIndex.CompareTo(right.ShardIndex);
            });

            var sorted = new List<object>(tagged.Count);
            foreach (var item in tagged) sorted.Add(item.Row);
            return sorted;
        }

        private static IEnumerable<object> ApplyOffsetAndLimit(
            IList<object> rows,
            int           offset,
            int           topN)
        {
            if (rows == null) yield break;

            int total    = rows.Count;
            int start    = offset > 0 ? Math.Min(offset, total) : 0;
            int endExcl  = topN > 0 ? Math.Min(start + topN, total) : total;

            for (int i = start; i < endExcl; i++)
            {
                yield return rows[i];
            }
        }

        private static int CompareSortKeys(object left, object right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left  == null) return -1;
            if (right == null) return  1;

            if (left is IComparable lc && left.GetType() == right.GetType())
            {
                return lc.CompareTo(right);
            }

            if (IsNumericSortKey(left) && IsNumericSortKey(right))
            {
                try
                {
                    double l = Convert.ToDouble(left);
                    double r = Convert.ToDouble(right);
                    return l.CompareTo(r);
                }
                catch (Exception)
                {
                    // Fall through to string compare.
                }
            }

            return string.Compare(
                Convert.ToString(left),
                Convert.ToString(right),
                StringComparison.Ordinal);
        }

        private static bool IsNumericSortKey(object value)
            => value is byte  || value is sbyte
            || value is short || value is ushort
            || value is int   || value is uint
            || value is long  || value is ulong
            || value is float || value is double
            || value is decimal;
    }
}
