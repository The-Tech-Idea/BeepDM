using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Pipelines.Attributes;
using TheTechIdea.Beep.Pipelines.Interfaces;
using TheTechIdea.Beep.Pipelines.Models;

namespace TheTechIdea.Beep.Pipelines.Engine.BuiltIn.Transformers
{
    /// <summary>
    /// Groups records by one or more fields and emits aggregated rows.
    /// All input is buffered in-memory before the first output row is emitted.
    /// Parameters:
    /// <list type="bullet">
    ///   <item><c>GroupBy</c>    — comma-separated field names to group by.</item>
    ///   <item><c>Aggregates</c> — JSON object, e.g.
    ///     <c>{ "TotalAmt": "SUM(Amount)", "RowCount": "COUNT(*)", "MaxSal": "MAX(Salary)" }</c>.
    ///     Supported functions: <c>SUM COUNT AVG MAX MIN FIRST LAST</c>.
    ///   </item>
    ///   <item><c>MaxGroupCount</c> — (int) Maximum groups to hold in memory. Default 1,000,000. 0 = unlimited.</item>
    ///   <item><c>OverflowStrategy</c> — "Fail" (default) throws when limit is hit; "DropOldest" evicts the least-recently-seen group.</item>
    /// </list>
    /// </summary>
    [PipelinePlugin(
        "beep.transform.aggregate",
        "Aggregate",
        PipelinePluginType.Transformer,
        Category = "Transform",
        Version  = "1.0",
        Author   = "The Tech Idea")]
    public class AggregateTransformer : IPipelineTransformer
    {
        public string PluginId    => "beep.transform.aggregate";
        public string DisplayName => "Aggregate";
        public string Description => "Groups and aggregates records in-memory.";

        private string[]  _groupBy          = Array.Empty<string>();
        private int        _maxGroupCount    = 1_000_000;
        private string     _overflowStrategy = "Fail";

        // destField → (function, sourceField)
        private readonly List<(string Dest, string Func, string SrcField)> _aggDefs = new();

        public IReadOnlyList<PipelineParameterDef> GetParameterDefinitions() => new[]
        {
            new PipelineParameterDef { Name = "GroupBy",          Type = ParamType.String,  IsRequired = true  },
            new PipelineParameterDef { Name = "Aggregates",       Type = ParamType.Json,    IsRequired = true,
                Description = "JSON: { \"destField\": \"FUNC(srcField)\" }" },
            new PipelineParameterDef { Name = "MaxGroupCount",    Type = ParamType.Integer, IsRequired = false,
                Description = "Max groups in memory. Default 1,000,000; 0 = unlimited." },
            new PipelineParameterDef { Name = "OverflowStrategy", Type = ParamType.String,  IsRequired = false,
                Description = "Fail (default) or DropOldest." }
        };

        public void Configure(IReadOnlyDictionary<string, object> parameters)
        {
            _groupBy = parameters.TryGetValue("GroupBy", out var gb)
                ? gb.ToString()!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                : Array.Empty<string>();

            _aggDefs.Clear();

            if (parameters.TryGetValue("MaxGroupCount", out var mgc))
                _maxGroupCount = Convert.ToInt32(mgc);
            if (parameters.TryGetValue("OverflowStrategy", out var ofs))
                _overflowStrategy = ofs?.ToString() ?? "Fail";

            if (!parameters.TryGetValue("Aggregates", out var ag)) return;
            Dictionary<string, string>? map = ag switch
            {
                Dictionary<string, string> typed => typed,
                Dictionary<string, object> obj   => obj.ToDictionary(kv => kv.Key, kv => kv.Value?.ToString() ?? ""),
                string json                       => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json),
                _                                 => null
            };
            if (map == null) return;

            foreach (var (dest, expr) in map)
            {
                var (fn, src) = ParseAggExpr(expr);
                _aggDefs.Add((dest, fn, src));
            }
        }

        public PipelineSchema GetOutputSchema(PipelineSchema inputSchema)
        {
            var fields = _groupBy.Select(f =>
            {
                int idx = inputSchema.GetFieldIndex(f);
                return idx >= 0 ? inputSchema.Fields[idx] : new PipelineField(f, typeof(object));
            }).ToList();

            foreach (var (dest, fn, _) in _aggDefs)
            {
                Type t = fn is "COUNT" or "SUM" or "AVG" or "MAX" or "MIN" ? typeof(double) : typeof(object);
                fields.Add(new PipelineField(dest, t));
            }

            return new PipelineSchema(inputSchema.Name, fields);
        }

        public async IAsyncEnumerable<PipelineRecord> TransformAsync(
            IAsyncEnumerable<PipelineRecord> input,
            PipelineRunContext ctx,
            [EnumeratorCancellation] CancellationToken token)
        {
            // Buffer phase: group rows with memory bound
            var groups = new Dictionary<string, GroupAccumulator>(StringComparer.Ordinal);
            // Insertion order tracking for DropOldest eviction
            var insertionOrder = _overflowStrategy.Equals("DropOldest", StringComparison.OrdinalIgnoreCase)
                ? new Queue<string>() : null;
            PipelineSchema? inSchema = null;
            long rowsBuffered = 0;
            long evictions = 0;

            await foreach (var record in input.WithCancellation(token))
            {
                inSchema ??= record.Schema;
                string key = BuildGroupKey(record);
                if (!groups.TryGetValue(key, out var acc))
                {
                    // Enforce group limit
                    if (_maxGroupCount > 0 && groups.Count >= _maxGroupCount)
                    {
                        if (_overflowStrategy.Equals("DropOldest", StringComparison.OrdinalIgnoreCase)
                            && insertionOrder != null && insertionOrder.Count > 0)
                        {
                            // Evict oldest 10% (minimum 1)
                            int evictCount = Math.Max(1, groups.Count / 10);
                            for (int e = 0; e < evictCount && insertionOrder.Count > 0; e++)
                            {
                                var oldKey = insertionOrder.Dequeue();
                                groups.Remove(oldKey);
                                evictions++;
                            }
                        }
                        else
                        {
                            throw new InvalidOperationException(
                                $"AggregateTransformer: MaxGroupCount ({_maxGroupCount}) exceeded. " +
                                $"Set OverflowStrategy to DropOldest or increase MaxGroupCount.");
                        }
                    }

                    acc = new GroupAccumulator(_groupBy.Length, _aggDefs.Count);
                    for (int i = 0; i < _groupBy.Length; i++)
                        acc.GroupValues[i] = record[_groupBy[i]];
                    groups[key] = acc;
                    insertionOrder?.Enqueue(key);
                }
                Accumulate(acc, record);
                rowsBuffered++;

                // Progress every 50 000 rows
                if (rowsBuffered % 50_000 == 0)
                    ctx.RuntimeState["aggregate_buffered_rows"] = rowsBuffered;
            }

            ctx.RuntimeState["aggregate_buffered_rows"]  = rowsBuffered;
            ctx.RuntimeState["aggregate_group_count"]    = (long)groups.Count;
            ctx.RuntimeState["aggregate_eviction_count"] = evictions;

            if (inSchema == null) yield break;

            var outSchema = GetOutputSchema(inSchema);

            // Emit phase
            foreach (var acc in groups.Values)
            {
                var values = new object?[outSchema.Fields.Count];
                int gi = 0;

                // group-by columns first
                foreach (var gf in _groupBy)
                {
                    int oi = outSchema.GetFieldIndex(gf);
                    if (oi >= 0) values[oi] = acc.GroupValues[gi];
                    gi++;
                }

                // aggregate columns
                for (int i = 0; i < _aggDefs.Count; i++)
                {
                    var (dest, fn, _) = _aggDefs[i];
                    int oi = outSchema.GetFieldIndex(dest);
                    if (oi >= 0) values[oi] = Finalize(acc.AggState[i], fn, acc.Count);
                }

                var outRec = new PipelineRecord(outSchema);
                Array.Copy(values, outRec.Values, values.Length);
                yield return outRec;
            }
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private string BuildGroupKey(PipelineRecord rec)
        {
            return string.Join("|", _groupBy.Select(f => rec[f]?.ToString() ?? "\0"));
        }

        private void Accumulate(GroupAccumulator acc, PipelineRecord rec)
        {
            acc.Count++;
            for (int i = 0; i < _aggDefs.Count; i++)
            {
                var (_, fn, src) = _aggDefs[i];
                object? val = src == "*" ? 1.0 : rec[src];
                acc.AggState[i] = Update(acc.AggState[i], fn, val, acc.Count);
            }
        }

        private static object? Update(object? state, string fn, object? val, long count)
        {
            return fn switch
            {
                "COUNT" => (double)(count),
                "SUM"   => ToD(state) + ToD(val),
                "AVG"   => ToD(state) + ToD(val),   // finalize divides by count
                "MAX"   => state == null ? ToD(val) : Math.Max(ToD(state), ToD(val)),
                "MIN"   => state == null ? ToD(val) : Math.Min(ToD(state), ToD(val)),
                "FIRST" => count == 1 ? val : state,
                "LAST"  => val,
                _       => state
            };
        }

        private static object? Finalize(object? state, string fn, long count)
        {
            if (fn == "AVG" && count > 0) return ToD(state) / count;
            return state;
        }

        private static double ToD(object? v)
        {
            if (v is double d) return d;
            if (v is null) return 0;
            return double.TryParse(v.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var r) ? r : 0;
        }

        private static (string Func, string SrcField) ParseAggExpr(string expr)
        {
            expr = expr.Trim();
            int lp = expr.IndexOf('(');
            int rp = expr.LastIndexOf(')');
            if (lp < 0 || rp < 0)
                return ("FIRST", expr);

            string fn  = expr.Substring(0, lp).Trim().ToUpperInvariant();
            string src = expr.Substring(lp + 1, rp - lp - 1).Trim();
            return (fn, src);
        }

        // ── Inner accumulator ─────────────────────────────────────────────────

        private sealed class GroupAccumulator
        {
            internal object?[] GroupValues { get; }
            internal object?[] AggState    { get; }
            internal long Count { get; set; }

            internal GroupAccumulator(int groupCount, int aggCount)
            {
                GroupValues = new object?[groupCount];
                AggState    = new object?[aggCount];
            }
        }
    }
}
