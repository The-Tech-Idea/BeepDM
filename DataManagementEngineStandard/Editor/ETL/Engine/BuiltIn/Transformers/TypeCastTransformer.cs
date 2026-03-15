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
    /// Casts field values to specified CLR types using <see cref="Convert.ChangeType"/>.
    /// Parameter: <c>Casts</c> — <see cref="Dictionary{TKey,TValue}"/> where
    /// key = field name, value = CLR type name (e.g. "System.Int32", "System.DateTime").
    /// Fields not listed in Casts pass through unchanged.
    /// On conversion failure the original value is preserved and a warning is logged.
    /// </summary>
    [PipelinePlugin(
        "beep.transform.typecast",
        "Type Cast",
        PipelinePluginType.Transformer,
        Category = "Transform",
        Version  = "1.0",
        Author   = "The Tech Idea")]
    public class TypeCastTransformer : IPipelineTransformer
    {
        // ── IPipelinePlugin ───────────────────────────────────────────────

        public string PluginId    => "beep.transform.typecast";
        public string DisplayName => "Type Cast";
        public string Description => "Converts field values to specified CLR types.";

        // fieldName → resolved Type
        private readonly Dictionary<string, Type> _casts = new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<PipelineParameterDef> GetParameterDefinitions() => new[]
        {
            new PipelineParameterDef
            {
                Name        = "Casts",
                Type        = ParamType.Json,
                IsRequired  = true,
                Description = "JSON object: { \"fieldName\": \"System.Int32\", ... }"
            }
        };

        public void Configure(IReadOnlyDictionary<string, object> parameters)
        {
            _casts.Clear();

            if (!parameters.TryGetValue("Casts", out var raw)) return;

            Dictionary<string, string>? source = raw switch
            {
                Dictionary<string, string> typed => typed,
                Dictionary<string, object> obj   => obj.ToDictionary(kv => kv.Key, kv => kv.Value?.ToString() ?? string.Empty),
                string json                       => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json),
                _                                 => null
            };

            if (source == null) return;

            foreach (var (field, typeName) in source)
            {
                var t = Type.GetType(typeName, throwOnError: false, ignoreCase: true);
                if (t != null) _casts[field] = t;
            }
        }

        // ── IPipelineTransformer ──────────────────────────────────────────

        public PipelineSchema GetOutputSchema(PipelineSchema inputSchema)
        {
            var fields = inputSchema.Fields.Select(f =>
            {
                if (_casts.TryGetValue(f.Name, out var targetType))
                    return new PipelineField(f.Name, targetType, f.IsKey, f.IsNullable, f.MaxLength);
                return f;
            });
            return new PipelineSchema(inputSchema.Name, fields);
        }

        public async IAsyncEnumerable<PipelineRecord> TransformAsync(
            IAsyncEnumerable<PipelineRecord> input,
            PipelineRunContext ctx,
            [EnumeratorCancellation] CancellationToken token)
        {
            PipelineSchema? outSchema = null;

            await foreach (var rec in input.WithCancellation(token))
            {
                outSchema ??= GetOutputSchema(rec.Schema);
                var outRec = rec.Clone();

                // Re-attach to the output schema
                var typed = new PipelineRecord(outSchema);
                Array.Copy(outRec.Values, typed.Values, outRec.Values.Length);
                foreach (var kv in rec.Meta) typed.Meta[kv.Key] = kv.Value;

                foreach (var (fieldName, targetType) in _casts)
                {
                    var val = typed[fieldName];
                    if (val == null || val == DBNull.Value) continue;

                    try
                    {
                        object? converted;
                        if (targetType == typeof(DateTime))
                            converted = Convert.ToDateTime(val);
                        else if (targetType == typeof(Guid))
                            converted = Guid.Parse(val.ToString()!);
                        else
                            converted = Convert.ChangeType(val, targetType);

                        typed[fieldName] = converted;
                    }
                    catch
                    {
                        // Keep original value; record a warning in meta
                        typed.Meta[$"__cast_warn_{fieldName}"] =
                            $"Cannot cast '{val}' to {targetType.Name}";
                        ctx.TotalRecordsWarned++;
                    }
                }

                yield return typed;
            }
        }
    }
}
