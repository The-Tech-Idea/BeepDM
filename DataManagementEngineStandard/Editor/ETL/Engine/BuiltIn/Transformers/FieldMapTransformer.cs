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
    /// Renames and/or reorders fields.
    /// Parameter: <c>Mappings</c> — <see cref="Dictionary{TKey,TValue}"/> where
    /// key = destination field name, value = source field name.
    /// Fields not listed in Mappings are dropped from the output.
    /// </summary>
    [PipelinePlugin(
        "beep.transform.fieldmap",
        "Field Mapping",
        PipelinePluginType.Transformer,
        Category = "Transform",
        Version  = "1.0",
        Author   = "The Tech Idea")]
    public class FieldMapTransformer : IPipelineTransformer
    {
        // ── IPipelinePlugin ───────────────────────────────────────────────

        public string PluginId     => "beep.transform.fieldmap";
        public string DisplayName  => "Field Mapping";
        public string Description  => "Renames and/or reorders fields. Unmapped fields are dropped.";

        private Dictionary<string, string> _mappings = new(); // destName → srcName

        public IReadOnlyList<PipelineParameterDef> GetParameterDefinitions() => new[]
        {
            new PipelineParameterDef
            {
                Name        = "Mappings",
                Type        = ParamType.Json,
                IsRequired  = true,
                Description = "JSON object: { \"destField\": \"sourceField\", ... }"
            }
        };

        public void Configure(IReadOnlyDictionary<string, object> parameters)
        {
            if (!parameters.TryGetValue("Mappings", out var raw)) return;

            _mappings = raw switch
            {
                Dictionary<string, string> typed => typed,
                Dictionary<string, object> obj   => obj.ToDictionary(kv => kv.Key, kv => kv.Value?.ToString() ?? string.Empty),
                string json                       => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new(),
                _                                 => new()
            };
        }

        // ── IPipelineTransformer ──────────────────────────────────────────

        public PipelineSchema GetOutputSchema(PipelineSchema inputSchema)
        {
            var fields = _mappings
                .Where(kv => inputSchema.GetFieldIndex(kv.Value) >= 0)
                .Select(kv =>
                {
                    int idx = inputSchema.GetFieldIndex(kv.Value);
                    var src = inputSchema.Fields[idx];
                    return new PipelineField(kv.Key, src.DataType, src.IsKey, src.IsNullable, src.MaxLength);
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
                var outRec = new PipelineRecord(outSchema);

                foreach (var (destField, srcField) in _mappings)
                    outRec[destField] = rec[srcField];

                // Carry metadata forward
                foreach (var kv in rec.Meta)
                    outRec.Meta[kv.Key] = kv.Value;

                yield return outRec;
            }
        }
    }
}
