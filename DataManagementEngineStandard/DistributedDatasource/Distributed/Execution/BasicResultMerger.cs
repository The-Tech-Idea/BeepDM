using System;
using System.Collections.Generic;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Distributed.Routing;

namespace TheTechIdea.Beep.Distributed.Execution
{
    /// <summary>
    /// Default Phase 06 <see cref="IResultMerger"/>: concatenates rows
    /// and sums scalar / count values. Zero-knowledge of SQL semantics —
    /// suitable for <c>GetEntity(filters)</c> fan-outs and
    /// <c>COUNT(*)</c>-style scalars. Phase 08 replaces it with the
    /// query-aware merger for <c>ORDER BY</c> / <c>LIMIT</c> /
    /// <c>AVG</c> / <c>MIN</c> / <c>MAX</c>.
    /// </summary>
    /// <remarks>
    /// Stateless and thread-safe; register once and share across reads.
    /// <see cref="Instance"/> is the recommended singleton.
    /// </remarks>
    public sealed class BasicResultMerger : IResultMerger
    {
        /// <summary>Process-wide singleton used by the default executor.</summary>
        public static readonly BasicResultMerger Instance = new BasicResultMerger();

        /// <inheritdoc/>
        public IEnumerable<object> MergeRows(
            RoutingDecision                    decision,
            IReadOnlyList<IEnumerable<object>> perShardResults)
        {
            if (perShardResults == null || perShardResults.Count == 0)
            {
                return Array.Empty<object>();
            }
            return ConcatEnumerables(perShardResults);
        }

        /// <inheritdoc/>
        public PagedResult MergePaged(
            RoutingDecision            decision,
            IReadOnlyList<PagedResult> perShardResults,
            int                        pageNumber,
            int                        pageSize)
        {
            // v1 policy: sum total record counts, concatenate row data.
            //  - Correct for "total rows across shards" diagnostics.
            //  - Per-page slicing across shards is intentionally NOT
            //    attempted; the Phase 08 query-aware merger owns that.
            var rows   = new List<object>();
            int total  = 0;

            if (perShardResults != null)
            {
                for (int i = 0; i < perShardResults.Count; i++)
                {
                    var p = perShardResults[i];
                    if (p == null) continue;
                    total += p.TotalRecords;
                    AppendRows(rows, p.Data);
                }
            }

            return new PagedResult(
                data:         rows,
                pageNumber:   pageNumber,
                pageSize:     pageSize,
                totalRecords: total);
        }

        /// <inheritdoc/>
        public double MergeScalar(RoutingDecision decision, IReadOnlyList<double> perShardResults)
        {
            if (perShardResults == null || perShardResults.Count == 0)
            {
                return 0d;
            }
            double sum = 0d;
            for (int i = 0; i < perShardResults.Count; i++)
            {
                sum += perShardResults[i];
            }
            return sum;
        }

        private static IEnumerable<object> ConcatEnumerables(
            IReadOnlyList<IEnumerable<object>> perShardResults)
        {
            for (int i = 0; i < perShardResults.Count; i++)
            {
                var shardRows = perShardResults[i];
                if (shardRows == null) continue;
                foreach (var row in shardRows)
                {
                    yield return row;
                }
            }
        }

        private static void AppendRows(List<object> rows, object data)
        {
            if (data == null) return;
            if (data is System.Collections.IEnumerable enumerable && data is not string)
            {
                foreach (var row in enumerable)
                {
                    rows.Add(row);
                }
                return;
            }
            rows.Add(data);
        }
    }
}
