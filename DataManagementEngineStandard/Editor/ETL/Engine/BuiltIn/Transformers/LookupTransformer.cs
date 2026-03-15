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
    /// Enriches records by looking up values from a reference data source.
    /// Maintains an LRU in-memory cache to minimize data-source round trips.
    /// Parameters:
    /// <list type="bullet">
    ///   <item><c>LookupSource</c>  — connection name for the lookup data source.</item>
    ///   <item><c>LookupEntity</c>  — entity/table name in that source.</item>
    ///   <item><c>LookupKey</c>     — field name in the lookup entity (the column to match).</item>
    ///   <item><c>InputKey</c>      — field name in the incoming record (the value to look up).</item>
    ///   <item><c>OutputFields</c>  — JSON object <c>{ "lookupField": "destField" }</c>.</item>
    ///   <item><c>OnMiss</c>        — <c>"Null"</c> (default), <c>"Reject"</c>, or <c>"Default"</c>.</item>
    ///   <item><c>CacheSize</c>     — max entries in the LRU cache (default 1000).</item>
    /// </list>
    /// </summary>
    [PipelinePlugin(
        "beep.transform.lookup",
        "Lookup",
        PipelinePluginType.Transformer,
        Category = "Transform",
        Version  = "1.0",
        Author   = "The Tech Idea")]
    public class LookupTransformer : IPipelineTransformer
    {
        public string PluginId    => "beep.transform.lookup";
        public string DisplayName => "Lookup";
        public string Description => "Enriches records by looking up values from a reference data source.";

        private string  _lookupSource = string.Empty;
        private string  _lookupEntity = string.Empty;
        private string  _lookupKey    = string.Empty;
        private string  _inputKey     = string.Empty;
        private string  _onMiss       = "Null";
        private int     _cacheSize    = 1000;

        // lookupField → destField
        private readonly Dictionary<string, string> _outputFields = new(StringComparer.OrdinalIgnoreCase);

        // LRU cache: inputKeyValue → row dict
        private readonly Dictionary<string, Dictionary<string, object?>> _cache = new(StringComparer.Ordinal);
        private readonly LinkedList<string> _cacheOrder = new();

        public IReadOnlyList<PipelineParameterDef> GetParameterDefinitions() => new[]
        {
            new PipelineParameterDef { Name = "LookupSource", Type = ParamType.String, IsRequired = true  },
            new PipelineParameterDef { Name = "LookupEntity", Type = ParamType.String, IsRequired = true  },
            new PipelineParameterDef { Name = "LookupKey",    Type = ParamType.String, IsRequired = true  },
            new PipelineParameterDef { Name = "InputKey",     Type = ParamType.String, IsRequired = true  },
            new PipelineParameterDef { Name = "OutputFields", Type = ParamType.Json,   IsRequired = true,  Description = "{ \"lookupField\": \"destField\" }" },
            new PipelineParameterDef { Name = "OnMiss",       Type = ParamType.String, IsRequired = false, DefaultValue = "Null",  Description = "Null | Reject | Default" },
            new PipelineParameterDef { Name = "CacheSize",    Type = ParamType.Integer,    IsRequired = false, DefaultValue = "1000"  }
        };

        public void Configure(IReadOnlyDictionary<string, object> parameters)
        {
            _lookupSource = parameters.TryGetValue("LookupSource", out var s)  ? s?.ToString() ?? "" : "";
            _lookupEntity = parameters.TryGetValue("LookupEntity", out var e)  ? e?.ToString() ?? "" : "";
            _lookupKey    = parameters.TryGetValue("LookupKey",    out var lk) ? lk?.ToString() ?? "" : "";
            _inputKey     = parameters.TryGetValue("InputKey",     out var ik) ? ik?.ToString() ?? "" : "";
            _onMiss       = parameters.TryGetValue("OnMiss",       out var om) ? om?.ToString() ?? "Null" : "Null";

            if (parameters.TryGetValue("CacheSize", out var cs) &&
                int.TryParse(cs?.ToString(), out int csi) && csi > 0)
                _cacheSize = csi;

            _outputFields.Clear();
            if (!parameters.TryGetValue("OutputFields", out var of)) return;
            Dictionary<string, string>? map = of switch
            {
                Dictionary<string, string> typed => typed,
                Dictionary<string, object> obj   => obj.ToDictionary(kv => kv.Key, kv => kv.Value?.ToString() ?? ""),
                string json                       => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json),
                _                                 => null
            };
            if (map != null)
                foreach (var kv in map)
                    _outputFields[kv.Key] = kv.Value;
        }

        public PipelineSchema GetOutputSchema(PipelineSchema inputSchema)
        {
            var fields = new List<PipelineField>(inputSchema.Fields);
            foreach (var destName in _outputFields.Values)
            {
                if (inputSchema.GetFieldIndex(destName) < 0)
                    fields.Add(new PipelineField(destName, typeof(object)));
            }
            return new PipelineSchema(inputSchema.Name, fields);
        }

        public async IAsyncEnumerable<PipelineRecord> TransformAsync(
            IAsyncEnumerable<PipelineRecord> input,
            PipelineRunContext ctx,
            [EnumeratorCancellation] CancellationToken token)
        {
            var outSchema = (PipelineSchema?)null;

            await foreach (var record in input.WithCancellation(token))
            {
                outSchema ??= GetOutputSchema(record.Schema);
                var lookupRow = await FetchLookupRowAsync(record, ctx, token);

                if (lookupRow == null && _onMiss.Equals("Reject", StringComparison.OrdinalIgnoreCase))
                {
                    ctx.TotalRecordsRejected++;
                    continue;
                }

                // Build output record
                var values = new object?[outSchema.Fields.Count];
                for (int i = 0; i < record.Schema.Fields.Count; i++)
                {
                    int oi = outSchema.GetFieldIndex(record.Schema.Fields[i].Name);
                    if (oi >= 0) values[oi] = record.Values[i];
                }

                foreach (var (lookupField, destField) in _outputFields)
                {
                    int oi = outSchema.GetFieldIndex(destField);
                    if (oi < 0) continue;
                    values[oi] = lookupRow != null && lookupRow.TryGetValue(lookupField, out var lv) ? lv : null;
                }

                var outRec = new PipelineRecord(outSchema!);
                Array.Copy(values, outRec.Values, values.Length);
                foreach (var kv in record.Meta) outRec.Meta[kv.Key] = kv.Value;
                yield return outRec;
            }
        }

        private async Task<Dictionary<string, object?>?> FetchLookupRowAsync(
            PipelineRecord record, PipelineRunContext ctx, CancellationToken token)
        {
            var keyVal = record[_inputKey]?.ToString() ?? "";

            // Cache hit
            if (_cache.TryGetValue(keyVal, out var cached))
            {
                _cacheOrder.Remove(keyVal);
                _cacheOrder.AddLast(keyVal);
                return cached;
            }

            // Cache miss — query data source
            var ds = ctx.DMEEditor?.GetDataSource(_lookupSource);
            if (ds == null) return null;

            ds.Openconnection();

            try
            {
                var filter = new List<TheTechIdea.Beep.Report.AppFilter>
                {
                    new TheTechIdea.Beep.Report.AppFilter
                    {
                        FieldName  = _lookupKey,
                        Operator   = "=",
                        FilterValue = keyVal
                    }
                };

                var dt = ds.GetEntity(_lookupEntity, filter) as System.Data.DataTable;
                if (dt == null || dt.Rows.Count == 0)
                {
                    AddToCache(keyVal, null);
                    return null;
                }

                var row = dt.Rows[0];
                var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (System.Data.DataColumn col in dt.Columns)
                    dict[col.ColumnName] = row[col] == DBNull.Value ? null : row[col];

                AddToCache(keyVal, dict);
                return dict;
            }
            finally
            {
                ds.Closeconnection();
            }
        }

        private void AddToCache(string key, Dictionary<string, object?>? value)
        {
            if (_cache.Count >= _cacheSize && _cacheOrder.First != null)
            {
                _cache.Remove(_cacheOrder.First.Value);
                _cacheOrder.RemoveFirst();
            }
            _cache[key] = value!;
            _cacheOrder.AddLast(key);
        }
    }
}
