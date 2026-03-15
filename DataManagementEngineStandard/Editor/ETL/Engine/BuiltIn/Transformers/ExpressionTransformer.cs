using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using TheTechIdea.Beep.Pipelines.Attributes;
using TheTechIdea.Beep.Pipelines.Engine.Expressions;
using TheTechIdea.Beep.Pipelines.Interfaces;
using TheTechIdea.Beep.Pipelines.Models;

namespace TheTechIdea.Beep.Pipelines.Engine.BuiltIn.Transformers
{
    /// <summary>
    /// Adds or replaces fields using computed expressions.
    /// Configuration parameter: <c>Expressions</c> — <see cref="Dictionary{TKey,TValue}"/> of
    /// <c>{ destinationFieldName: "expression string" }</c>.
    /// </summary>
    [PipelinePlugin(
        "beep.transform.expression",
        "Expression",
        PipelinePluginType.Transformer,
        Category = "Transform",
        Version  = "1.0",
        Author   = "The Tech Idea")]
    public class ExpressionTransformer : IPipelineTransformer
    {
        public string PluginId    => "beep.transform.expression";
        public string DisplayName => "Expression";
        public string Description => "Adds or replaces fields using computed expressions.";

        // ── Config ────────────────────────────────────────────────────────

        // fieldName → compiled evaluator
        private readonly Dictionary<string, SimpleExpressionEvaluator> _exprs
            = new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<PipelineParameterDef> GetParameterDefinitions() => new[]
        {
            new PipelineParameterDef
            {
                Name        = "Expressions",
                Type        = ParamType.Json,
                IsRequired  = true,
                Description = "JSON object: { \"destField\": \"expression\", ... }"
            }
        };

        public void Configure(IReadOnlyDictionary<string, object> parameters)
        {
            _exprs.Clear();

            if (!parameters.TryGetValue("Expressions", out var raw)) return;

            Dictionary<string, string>? map = raw switch
            {
                Dictionary<string, string> typed => typed,
                Dictionary<string, object> obj   => obj.ToDictionary(kv => kv.Key, kv => kv.Value?.ToString() ?? ""),
                string json                       => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json),
                _                                 => null
            };

            if (map == null) return;

            foreach (var kv in map)
                _exprs[kv.Key] = new SimpleExpressionEvaluator(kv.Value);
        }

        // ── Schema ────────────────────────────────────────────────────────

        public PipelineSchema GetOutputSchema(PipelineSchema inputSchema)
        {
            var fields = new List<PipelineField>(inputSchema.Fields);

            foreach (var destName in _exprs.Keys)
            {
                // Replace existing field or append new one
                int idx = inputSchema.GetFieldIndex(destName);
                var newField = idx >= 0
                    ? inputSchema.Fields[idx]
                    : new PipelineField(destName, typeof(object));

                if (idx < 0)
                    fields.Add(newField);
                else
                    fields[idx] = newField;
            }

            return new PipelineSchema(inputSchema.Name, fields);
        }

        // ── Transform ─────────────────────────────────────────────────────

        public async IAsyncEnumerable<PipelineRecord> TransformAsync(
            IAsyncEnumerable<PipelineRecord> input,
            PipelineRunContext ctx,
            [EnumeratorCancellation] CancellationToken token)
        {
            PipelineSchema? outSchema = null;
            var iter = input.GetAsyncEnumerator(token);
            try
            {
                while (await iter.MoveNextAsync().ConfigureAwait(false))
                {
                    var record = iter.Current;
                    outSchema ??= GetOutputSchema(record.Schema);

                    var values = new object?[outSchema.Fields.Count];

                    for (int i = 0; i < record.Schema.Fields.Count; i++)
                    {
                        int outIdx = outSchema.GetFieldIndex(record.Schema.Fields[i].Name);
                        if (outIdx >= 0) values[outIdx] = record.Values[i];
                    }

                    foreach (var (destName, eval) in _exprs)
                    {
                        int outIdx = outSchema.GetFieldIndex(destName);
                        if (outIdx >= 0)
                            values[outIdx] = EvalSafe(eval, record, destName, ctx);
                    }

                    var outRec = new PipelineRecord(outSchema!);
                    Array.Copy(values, outRec.Values, values.Length);
                    foreach (var kv in record.Meta) outRec.Meta[kv.Key] = kv.Value;
                    yield return outRec;
                }
            }
            finally
            {
                await iter.DisposeAsync().ConfigureAwait(false);
            }
        }

        private static object? EvalSafe(SimpleExpressionEvaluator eval, PipelineRecord record,
            string destName, PipelineRunContext ctx)
        {
            try { return eval.Evaluate(record); }
            catch (Exception ex)
            {
                ctx.DMEEditor?.Logger?.WriteLog($"ExpressionTransformer '{destName}': {ex.Message}");
                return null;
            }
        }
    }
}
