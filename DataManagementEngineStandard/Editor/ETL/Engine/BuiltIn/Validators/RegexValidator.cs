using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Pipelines.Attributes;
using TheTechIdea.Beep.Pipelines.Interfaces;
using TheTechIdea.Beep.Pipelines.Models;
using ValidationResult = TheTechIdea.Beep.Pipelines.Interfaces.ValidationResult;

namespace TheTechIdea.Beep.Pipelines.Engine.BuiltIn.Validators
{
    /// <summary>
    /// Validates field values against regular-expression patterns.
    /// Parameters:
    /// <list type="bullet">
    ///   <item><c>Rules</c> — JSON array of <c>{ "Field": "...", "Pattern": "...", "Message": "..." }</c>.</item>
    ///   <item><c>Outcome</c> — <c>"Reject"</c> (default) or <c>"Warn"</c> when a pattern does not match.</item>
    /// </list>
    /// Null field values are skipped (combine with <see cref="NotNullValidator"/> if needed).
    /// </summary>
    [PipelinePlugin(
        "beep.validate.regex",
        "Regex",
        PipelinePluginType.Validator,
        Category = "Validate",
        Version  = "1.0",
        Author   = "The Tech Idea")]
    public class RegexValidator : IPipelineValidator
    {
        public string PluginId    => "beep.validate.regex";
        public string DisplayName => "Regex";
        public string Description => "Validates field values against regular-expression patterns.";

        private readonly List<(string Field, Regex Pattern, string Message)> _rules = new();
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
                string field   = elem.TryGetProperty("Field",   out var f) ? f.GetString() ?? "" : "";
                string pattern = elem.TryGetProperty("Pattern", out var p) ? p.GetString() ?? "" : "";
                string msg     = elem.TryGetProperty("Message", out var m) ? m.GetString() ?? $"Field '{field}' failed regex validation." : $"Field '{field}' failed regex validation.";

                if (!string.IsNullOrWhiteSpace(field) && !string.IsNullOrWhiteSpace(pattern))
                    _rules.Add((field, new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant), msg));
            }
        }

        public Task<ValidationResult> ValidateAsync(
            PipelineRecord record,
            PipelineRunContext ctx,
            CancellationToken token)
        {
            foreach (var (field, regex, msg) in _rules)
            {
                var val = record[field]?.ToString();
                if (val == null) continue; // null = skip (use NotNullValidator for that)

                if (!regex.IsMatch(val))
                    return Task.FromResult(new ValidationResult(_failOutcome, PluginId, msg, record));
            }

            return Task.FromResult(new ValidationResult(ValidationOutcome.Pass, PluginId, string.Empty));
        }
    }
}
