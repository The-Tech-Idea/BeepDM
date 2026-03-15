using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Pipelines.Attributes;
using TheTechIdea.Beep.Pipelines.Interfaces;
using TheTechIdea.Beep.Pipelines.Models;
using TheTechIdea.Beep.Report;
using ValidationResult = TheTechIdea.Beep.Pipelines.Interfaces.ValidationResult;

namespace TheTechIdea.Beep.Pipelines.Engine.BuiltIn.Validators
{
    /// <summary>
    /// Verifies that field values exist as records in a reference data source.
    /// One data-source query is made per distinct key value (LRU cache reduces round trips).
    /// Parameters:
    /// <list type="bullet">
    ///   <item><c>Rules</c> — JSON array of
    ///     <c>{ "Field": "...", "LookupSource": "...", "LookupEntity": "...", "LookupField": "..." }</c>.</item>
    ///   <item><c>CacheSize</c> — per-rule LRU cache size (default 500).</item>
    ///   <item><c>Outcome</c>   — <c>"Reject"</c> (default) or <c>"Warn"</c> on violation.</item>
    /// </list>
    /// </summary>
    [PipelinePlugin(
        "beep.validate.referential",
        "Referential Integrity",
        PipelinePluginType.Validator,
        Category = "Validate",
        Version  = "1.0",
        Author   = "The Tech Idea")]
    public class ReferentialIntegrityValidator : IPipelineValidator
    {
        public string PluginId    => "beep.validate.referential";
        public string DisplayName => "Referential Integrity";
        public string Description => "Verifies that field values exist in a reference data source.";

        private readonly List<RefRule>    _rules       = new();
        private ValidationOutcome         _failOutcome = ValidationOutcome.Reject;
        private int                       _cacheSize   = 500;

        public IReadOnlyList<PipelineParameterDef> GetParameterDefinitions() => new[]
        {
            new PipelineParameterDef { Name = "Rules",     Type = ParamType.Json,   IsRequired = true  },
            new PipelineParameterDef { Name = "CacheSize", Type = ParamType.Integer,    IsRequired = false, DefaultValue = "500" },
            new PipelineParameterDef { Name = "Outcome",   Type = ParamType.String, IsRequired = false, DefaultValue = "Reject" }
        };

        public void Configure(IReadOnlyDictionary<string, object> parameters)
        {
            _rules.Clear();

            if (parameters.TryGetValue("Outcome", out var oc) &&
                Enum.TryParse<ValidationOutcome>(oc?.ToString(), true, out var parsed))
                _failOutcome = parsed;

            if (parameters.TryGetValue("CacheSize", out var cs) &&
                int.TryParse(cs?.ToString(), out int csi) && csi > 0)
                _cacheSize = csi;

            if (!parameters.TryGetValue("Rules", out var raw)) return;

            List<JsonElement>? list = raw switch
            {
                string json => JsonSerializer.Deserialize<List<JsonElement>>(json),
                _           => null
            };
            if (list == null) return;

            foreach (var elem in list)
            {
                string field  = elem.TryGetProperty("Field",         out var f)  ? f.GetString()  ?? "" : "";
                string src    = elem.TryGetProperty("LookupSource",  out var s)  ? s.GetString()  ?? "" : "";
                string entity = elem.TryGetProperty("LookupEntity",  out var e)  ? e.GetString()  ?? "" : "";
                string lf     = elem.TryGetProperty("LookupField",   out var lk) ? lk.GetString() ?? "" : "";

                if (!string.IsNullOrWhiteSpace(field))
                    _rules.Add(new RefRule(field, src, entity, lf, _cacheSize));
            }
        }

        public async Task<ValidationResult> ValidateAsync(
            PipelineRecord record,
            PipelineRunContext ctx,
            CancellationToken token)
        {
            foreach (var rule in _rules)
            {
                var keyVal = record[rule.Field]?.ToString();
                if (keyVal == null) continue;   // null values: use NotNullValidator

                bool exists = await rule.ExistsAsync(keyVal, ctx, token);
                if (!exists)
                {
                    string msg = $"Referential integrity violation: field '{rule.Field}' value '{keyVal}' " +
                                 $"not found in {rule.LookupSource}.{rule.LookupEntity}.{rule.LookupField}.";
                    return new ValidationResult(_failOutcome, PluginId, msg, record);
                }
            }

            return new ValidationResult(ValidationOutcome.Pass, PluginId, string.Empty);
        }

        // ── Rule ──────────────────────────────────────────────────────────────

        private sealed class RefRule
        {
            internal string Field        { get; }
            internal string LookupSource { get; }
            internal string LookupEntity { get; }
            internal string LookupField  { get; }

            // Simple LRU cache: key → exists
            private readonly Dictionary<string, bool> _cache  = new(StringComparer.Ordinal);
            private readonly LinkedList<string>        _order  = new();
            private readonly int                       _maxSize;

            internal RefRule(string field, string src, string entity, string lf, int cacheSize)
            {
                Field        = field;
                LookupSource = src;
                LookupEntity = entity;
                LookupField  = lf;
                _maxSize     = cacheSize;
            }

            internal async Task<bool> ExistsAsync(string keyVal, PipelineRunContext ctx, CancellationToken token)
            {
                if (_cache.TryGetValue(keyVal, out bool cached))
                {
                    _order.Remove(keyVal);
                    _order.AddLast(keyVal);
                    return cached;
                }

                var ds = ctx.DMEEditor?.GetDataSource(LookupSource);
                if (ds == null) return true; // can't verify — let it pass

                ds.Openconnection();
                bool found;
                try
                {
                    var filter = new List<AppFilter>
                    {
                        new AppFilter { FieldName = LookupField, Operator = "=", FilterValue = keyVal }
                    };

                    var result = ds.GetEntity(LookupEntity, filter) as System.Data.DataTable;
                    found = result != null && result.Rows.Count > 0;
                }
                finally
                {
                    ds.Closeconnection();
                }

                // Evict oldest when full
                if (_cache.Count >= _maxSize && _order.First != null)
                {
                    _cache.Remove(_order.First.Value);
                    _order.RemoveFirst();
                }
                _cache[keyVal] = found;
                _order.AddLast(keyVal);

                return found;
            }
        }
    }
}
