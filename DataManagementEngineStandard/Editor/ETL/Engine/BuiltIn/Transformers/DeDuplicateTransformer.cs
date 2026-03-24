using System;
using System.Collections.Generic;
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
    /// Removes duplicate records within a sliding window keyed on one or more fields.
    /// Parameters:
    /// <list type="bullet">
    ///   <item><c>KeyFields</c>    — comma-separated list of field names that form the key.</item>
    ///   <item><c>Strategy</c>     — <c>KeepFirst</c> (default), <c>KeepLast</c>,
    ///                              <c>KeepMax(fieldName)</c>, <c>KeepMin(fieldName)</c>.</item>
    ///   <item><c>WindowSize</c>   — max distinct keys to track (default 100 000). When exceeded,
    ///                              the oldest 10 % of entries are evicted.</item>
    /// </list>
    /// </summary>
    [PipelinePlugin(
        "beep.transform.deduplicate",
        "De-Duplicate",
        PipelinePluginType.Transformer,
        Category = "Transform",
        Version  = "1.0",
        Author   = "The Tech Idea")]
    public class DeDuplicateTransformer : IPipelineTransformer
    {
        public string PluginId    => "beep.transform.deduplicate";
        public string DisplayName => "De-Duplicate";
        public string Description => "Removes duplicate records within a sliding key window.";

        private string[]  _keyFields  = Array.Empty<string>();
        private string    _strategy   = "KeepFirst";
        private int       _windowSize = 100_000;

        public IReadOnlyList<PipelineParameterDef> GetParameterDefinitions() => new[]
        {
            new PipelineParameterDef { Name = "KeyFields",   Type = ParamType.String, IsRequired = true,  Description = "Comma-separated field names for the deduplication key." },
            new PipelineParameterDef { Name = "Strategy",    Type = ParamType.String, IsRequired = false, DefaultValue = "KeepFirst", Description = "KeepFirst | KeepLast | KeepMax(field) | KeepMin(field)" },
            new PipelineParameterDef { Name = "WindowSize",  Type = ParamType.Integer,    IsRequired = false, DefaultValue = "100000",    Description = "Max distinct key entries to hold in memory." }
        };

        public void Configure(IReadOnlyDictionary<string, object> parameters)
        {
            if (parameters.TryGetValue("KeyFields", out var kf))
                _keyFields = kf.ToString()!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parameters.TryGetValue("Strategy", out var st))
                _strategy = st?.ToString() ?? "KeepFirst";

            if (parameters.TryGetValue("WindowSize", out var ws) &&
                int.TryParse(ws?.ToString(), out int w) && w > 0)
                _windowSize = w;
        }

        public PipelineSchema GetOutputSchema(PipelineSchema inputSchema) => inputSchema;

        public async IAsyncEnumerable<PipelineRecord> TransformAsync(
            IAsyncEnumerable<PipelineRecord> input,
            PipelineRunContext ctx,
            [EnumeratorCancellation] CancellationToken token)
        {
            // seenKey → last/max/min record candidate (null for KeepFirst: just track presence)
            var seen    = new Dictionary<string, PipelineRecord?>(StringComparer.Ordinal);
            var order   = new Queue<string>();       // insertion order for eviction

            bool isLast = _strategy.Equals("KeepLast", StringComparison.OrdinalIgnoreCase);
            bool isMax  = _strategy.StartsWith("KeepMax", StringComparison.OrdinalIgnoreCase);
            bool isMin  = _strategy.StartsWith("KeepMin", StringComparison.OrdinalIgnoreCase);

            string? aggField = null;
            if (isMax || isMin)
            {
                int lparen = _strategy.IndexOf('(');
                int rparen = _strategy.IndexOf(')');
                if (lparen >= 0 && rparen > lparen)
                    aggField = _strategy.Substring(lparen + 1, rparen - lparen - 1).Trim();
            }

            long evictions = 0;

            // KeepLast / KeepMax / KeepMin: windowed buffer with LRU eviction, emit survivors at end
            if (isLast || isMax || isMin)
            {
                await foreach (var record in input.WithCancellation(token))
                {
                    string key = BuildKey(record);
                    if (!seen.ContainsKey(key))
                    {
                        // Evict oldest 10% when window is full
                        if (seen.Count >= _windowSize)
                        {
                            int evict = Math.Max(1, _windowSize / 10);
                            for (int i = 0; i < evict && order.Count > 0; i++)
                            {
                                seen.Remove(order.Dequeue());
                                evictions++;
                            }
                        }
                        seen[key] = record;
                        order.Enqueue(key);
                    }
                    else
                    {
                        seen[key] = PickWinner(seen[key]!, record, aggField, isMax);
                    }
                }
                foreach (var winner in seen.Values)
                    if (winner != null) yield return winner;
            }
            else // KeepFirst: streaming (no buffering)
            {
                await foreach (var record in input.WithCancellation(token))
                {
                    string key = BuildKey(record);
                    if (!seen.ContainsKey(key))
                    {
                        // Evict oldest 10 % when window is full
                        if (seen.Count >= _windowSize)
                        {
                            int evict = Math.Max(1, _windowSize / 10);
                            for (int i = 0; i < evict && order.Count > 0; i++)
                            {
                                seen.Remove(order.Dequeue());
                                evictions++;
                            }
                        }
                        seen[key] = null;
                        order.Enqueue(key);
                        yield return record;
                    }
                    else
                    {
                        ctx.TotalRecordsRejected++;
                    }
                }
            }

            ctx.RuntimeState["dedup_distinct_keys"]    = (long)seen.Count;
            ctx.RuntimeState["dedup_eviction_count"]   = evictions;
        }

        private string BuildKey(PipelineRecord rec)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var f in _keyFields)
            {
                sb.Append(rec[f]?.ToString() ?? "\0");
                sb.Append('|');
            }
            return sb.ToString();
        }

        private static PipelineRecord PickWinner(PipelineRecord current, PipelineRecord candidate, string? aggField, bool isMax)
        {
            if (aggField == null) return candidate; // KeepLast

            double curVal  = ToDouble(current[aggField]);
            double candVal = ToDouble(candidate[aggField]);

            return isMax ? (candVal > curVal ? candidate : current)
                         : (candVal < curVal ? candidate : current);
        }

        private static double ToDouble(object? v)
        {
            if (v is double d) return d;
            if (v is null) return 0;
            return double.TryParse(v.ToString(), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var r) ? r : 0;
        }
    }
}
