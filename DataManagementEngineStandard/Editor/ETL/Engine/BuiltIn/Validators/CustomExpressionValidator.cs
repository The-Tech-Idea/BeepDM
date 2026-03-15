using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Pipelines.Attributes;
using TheTechIdea.Beep.Pipelines.Engine.Expressions;
using TheTechIdea.Beep.Pipelines.Interfaces;
using TheTechIdea.Beep.Pipelines.Models;
using ValidationResult = TheTechIdea.Beep.Pipelines.Interfaces.ValidationResult;

namespace TheTechIdea.Beep.Pipelines.Engine.BuiltIn.Validators
{
    /// <summary>
    /// Evaluates one or more expression-based rules against each record.
    /// Rules are evaluated in order; the first failure determines the overall outcome.
    /// Parameters:
    /// <list type="bullet">
    ///   <item><c>Rules</c> — JSON array of
    ///     <c>{ "Expression": "...", "Message": "...", "Outcome": "Reject|Warn" }</c>.
    ///     Expression must evaluate to a boolean; a <c>true</c> result means the record <em>passes</em>
    ///     the rule.</item>
    /// </list>
    /// </summary>
    [PipelinePlugin(
        "beep.validate.expression",
        "Custom Expression",
        PipelinePluginType.Validator,
        Category = "Validate",
        Version  = "1.0",
        Author   = "The Tech Idea")]
    public class CustomExpressionValidator : IPipelineValidator
    {
        public string PluginId    => "beep.validate.expression";
        public string DisplayName => "Custom Expression";
        public string Description => "Validates records against expression-based rules.";

        private readonly List<(SimpleExpressionEvaluator Eval, string Message, ValidationOutcome Outcome)> _rules = new();

        public IReadOnlyList<PipelineParameterDef> GetParameterDefinitions() => new[]
        {
            new PipelineParameterDef
            {
                Name        = "Rules",
                Type        = ParamType.Json,
                IsRequired  = true,
                Description = "JSON array: [ { \"Expression\": \"...\", \"Message\": \"...\", \"Outcome\": \"Reject\" }, ... ]"
            }
        };

        public void Configure(IReadOnlyDictionary<string, object> parameters)
        {
            _rules.Clear();

            if (!parameters.TryGetValue("Rules", out var raw)) return;

            List<JsonElement>? list = raw switch
            {
                string json => JsonSerializer.Deserialize<List<JsonElement>>(json),
                _           => null
            };
            if (list == null) return;

            foreach (var elem in list)
            {
                string expr = elem.TryGetProperty("Expression", out var e) ? e.GetString() ?? "" : "";
                string msg  = elem.TryGetProperty("Message",    out var m) ? m.GetString() ?? "Custom validation failed." : "Custom validation failed.";
                string oc   = elem.TryGetProperty("Outcome",    out var o) ? o.GetString() ?? "Reject" : "Reject";

                if (string.IsNullOrWhiteSpace(expr)) continue;

                Enum.TryParse<ValidationOutcome>(oc, true, out var outcome);
                _rules.Add((new SimpleExpressionEvaluator(expr), msg, outcome == ValidationOutcome.Pass ? ValidationOutcome.Reject : outcome));
            }
        }

        public Task<ValidationResult> ValidateAsync(
            PipelineRecord record,
            PipelineRunContext ctx,
            CancellationToken token)
        {
            foreach (var (eval, msg, outcome) in _rules)
            {
                bool pass;
                try   { pass = eval.EvaluateBool(record); }
                catch { pass = false; }

                if (!pass)
                    return Task.FromResult(new ValidationResult(outcome, PluginId, msg, record));
            }

            return Task.FromResult(new ValidationResult(ValidationOutcome.Pass, PluginId, string.Empty));
        }
    }
}
