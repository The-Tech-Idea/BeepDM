using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Pipelines.Attributes;
using TheTechIdea.Beep.Pipelines.Interfaces;
using TheTechIdea.Beep.Pipelines.Models;
using ValidationResult = TheTechIdea.Beep.Pipelines.Interfaces.ValidationResult;

namespace TheTechIdea.Beep.Pipelines.Engine.BuiltIn.Validators
{
    /// <summary>
    /// Validates that numeric or date field values fall within a specified range.
    /// Parameters:
    /// <list type="bullet">
    ///   <item><c>Rules</c> — JSON array of <c>{ "Field": "...", "Min": "...", "Max": "..." }</c>.
    ///     Omit <c>Min</c> or <c>Max</c> to apply a one-sided range check.</item>
    ///   <item><c>Outcome</c> — <c>"Reject"</c> (default) or <c>"Warn"</c> on violation.</item>
    /// </list>
    /// </summary>
    [PipelinePlugin(
        "beep.validate.range",
        "Range",
        PipelinePluginType.Validator,
        Category = "Validate",
        Version  = "1.0",
        Author   = "The Tech Idea")]
    public class RangeValidator : IPipelineValidator
    {
        public string PluginId    => "beep.validate.range";
        public string DisplayName => "Range";
        public string Description => "Validates that field values fall within a specified numeric or date range.";

        private readonly List<RangeRule> _rules = new();
        private ValidationOutcome _failOutcome = ValidationOutcome.Reject;

        public IReadOnlyList<PipelineParameterDef> GetParameterDefinitions() => new[]
        {
            new PipelineParameterDef { Name = "Rules",   Type = ParamType.Json,   IsRequired = true  },
            new PipelineParameterDef { Name = "Outcome", Type = ParamType.String, IsRequired = false,
                DefaultValue = "Reject", Description = "Reject | Warn" }
        };

        public void Configure(IReadOnlyDictionary<string, object> parameters)
        {
            _rules.Clear();

            if (parameters.TryGetValue("Outcome", out var oc) &&
                Enum.TryParse<ValidationOutcome>(oc?.ToString(), true, out var parsed))
                _failOutcome = parsed;

            if (!parameters.TryGetValue("Rules", out var raw)) return;

            List<JsonElement>? list = raw switch
            {
                string json => JsonSerializer.Deserialize<List<JsonElement>>(json),
                _           => null
            };
            if (list == null) return;

            foreach (var elem in list)
            {
                string field = elem.TryGetProperty("Field", out var f) ? f.GetString() ?? "" : "";
                string? min  = elem.TryGetProperty("Min",   out var mn) ? mn.GetString() : null;
                string? max  = elem.TryGetProperty("Max",   out var mx) ? mx.GetString() : null;
                string  msg  = elem.TryGetProperty("Message", out var m) ? m.GetString() ?? "" : "";

                if (!string.IsNullOrWhiteSpace(field))
                    _rules.Add(new RangeRule(field, min, max, msg));
            }
        }

        public Task<ValidationResult> ValidateAsync(
            PipelineRecord record,
            PipelineRunContext ctx,
            CancellationToken token)
        {
            foreach (var rule in _rules)
            {
                var val = record[rule.Field];
                if (val == null) continue;

                bool inRange = CheckRange(val, rule);
                if (!inRange)
                {
                    string msg = string.IsNullOrEmpty(rule.Message)
                        ? $"Field '{rule.Field}' value '{val}' is out of range [{rule.Min ?? "∞"}, {rule.Max ?? "∞"}]."
                        : rule.Message;
                    return Task.FromResult(new ValidationResult(_failOutcome, PluginId, msg, record));
                }
            }

            return Task.FromResult(new ValidationResult(ValidationOutcome.Pass, PluginId, string.Empty));
        }

        private static bool CheckRange(object val, RangeRule rule)
        {
            // Try numeric first
            if (double.TryParse(val.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double num))
            {
                if (rule.MinDouble.HasValue && num < rule.MinDouble.Value) return false;
                if (rule.MaxDouble.HasValue && num > rule.MaxDouble.Value) return false;
                return true;
            }

            // Try date
            if (val is DateTime dt ||
                (val is string ds && DateTime.TryParse(ds, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt)))
            {
                if (rule.MinDate.HasValue && dt < rule.MinDate.Value) return false;
                if (rule.MaxDate.HasValue && dt > rule.MaxDate.Value) return false;
                return true;
            }

            return true; // non-numeric/date fields: skip range check
        }

        // ── Rule ──────────────────────────────────────────────────────────────

        private sealed class RangeRule
        {
            internal string  Field   { get; }
            internal string? Min     { get; }
            internal string? Max     { get; }
            internal string  Message { get; }

            internal double?   MinDouble { get; }
            internal double?   MaxDouble { get; }
            internal DateTime? MinDate   { get; }
            internal DateTime? MaxDate   { get; }

            internal RangeRule(string field, string? min, string? max, string message)
            {
                Field   = field;
                Min     = min;
                Max     = max;
                Message = message;

                if (min != null)
                {
                    if (double.TryParse(min, NumberStyles.Any, CultureInfo.InvariantCulture, out double md))
                        MinDouble = md;
                    else if (DateTime.TryParse(min, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime mdt))
                        MinDate = mdt;
                }

                if (max != null)
                {
                    if (double.TryParse(max, NumberStyles.Any, CultureInfo.InvariantCulture, out double md))
                        MaxDouble = md;
                    else if (DateTime.TryParse(max, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime mdt))
                        MaxDate = mdt;
                }
            }
        }
    }
}
