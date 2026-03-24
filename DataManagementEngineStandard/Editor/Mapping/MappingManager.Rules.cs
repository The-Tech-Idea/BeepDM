using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using TheTechIdea.Beep.Rules;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Beep.Workflow.Mapping;

namespace TheTechIdea.Beep.Editor.Mapping
{
    public enum MappingRulePriority
    {
        ExplicitUserRule = 1,
        ConfidenceAutoMap = 2,
        FallbackDefault = 3
    }

    public enum MappingRuleDecision
    {
        Map,
        Skip,
        UseDefault
    }

    public sealed class MappingRuleExecutionTrace
    {
        public MappingRulePriority Priority { get; set; } = MappingRulePriority.ConfidenceAutoMap;
        public MappingRuleDecision Decision { get; set; } = MappingRuleDecision.Map;
        public string Message { get; set; } = string.Empty;
        public string SourcePath { get; set; } = string.Empty;
        public string TargetPath { get; set; } = string.Empty;
        public bool ConditionMatched { get; set; } = true;
    }

    public sealed class MappingRuleValidationResult
    {
        public bool IsValid => Errors.Count == 0;
        public List<string> Errors { get; } = new List<string>();
        public List<string> Warnings { get; } = new List<string>();
    }

    public static partial class MappingManager
    {
        public static MappingRuleValidationResult ValidateRuleSet(EntityDataMap mapping)
        {
            var result = new MappingRuleValidationResult();
            if (mapping == null)
            {
                result.Errors.Add("Mapping is null.");
                return result;
            }

            var conflicts = mapping.MappedEntities?
                .SelectMany(entity => entity.FieldMapping ?? new List<Mapping_rep_fields>())
                .Where(field => field != null)
                .GroupBy(field => field.ToFieldName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .ToList() ?? new List<IGrouping<string, Mapping_rep_fields>>();

            foreach (var conflict in conflicts)
            {
                result.Warnings.Add($"Multiple mapping rules target '{conflict.Key}'. Deterministic precedence still applies but review is recommended.");
            }

            foreach (var field in mapping.MappedEntities?.SelectMany(entity => entity.FieldMapping ?? new List<Mapping_rep_fields>()) ?? Enumerable.Empty<Mapping_rep_fields>())
            {
                if (field == null || string.IsNullOrWhiteSpace(field.Rules))
                    continue;

                try
                {
                    var tokenizeResult = new Tokenizer(field.Rules).Tokenize();
                    // Check for unknown tokens using foreach on the Tokens collection
                    bool hasUnknownTokens = false;
                    foreach (var token in tokenizeResult.Tokens)
                    {
                        if (token.Type == TokenType.Unknown)
                        {
                            hasUnknownTokens = true;
                            break;
                        }
                    }

                    if (hasUnknownTokens)
                    {
                        result.Warnings.Add($"Rule for '{field.ToFieldName}' contains unknown token(s) and should be reviewed.");
                    }
                }
                catch (Exception ex)
                {
                    result.Warnings.Add($"Rule tokenization failed for '{field.ToFieldName}': {ex.Message}");
                }

                if (field.Rules.Contains("when", StringComparison.OrdinalIgnoreCase) &&
                    !field.Rules.Contains("then", StringComparison.OrdinalIgnoreCase) &&
                    !field.Rules.Contains("->", StringComparison.OrdinalIgnoreCase) &&
                    !field.Rules.Contains("map", StringComparison.OrdinalIgnoreCase))
                {
                    result.Warnings.Add($"Rule for '{field.ToFieldName}' has condition text without explicit action; default action is map.");
                }
            }

            return result;
        }

        private static MappingRuleExecutionTrace ResolveRuleExecutionTrace(object source, Mapping_rep_fields mapping, out RuleResolution resolution)
        {
            resolution = new RuleResolution
            {
                SourcePath = mapping?.FromFieldName ?? string.Empty,
                Decision = string.IsNullOrWhiteSpace(mapping?.FromFieldName) ? MappingRuleDecision.UseDefault : MappingRuleDecision.Map,
                Priority = string.IsNullOrWhiteSpace(mapping?.FromFieldName) ? MappingRulePriority.FallbackDefault : MappingRulePriority.ConfidenceAutoMap
            };

            var trace = new MappingRuleExecutionTrace
            {
                Priority = resolution.Priority,
                Decision = resolution.Decision,
                SourcePath = resolution.SourcePath,
                TargetPath = mapping?.ToFieldName ?? string.Empty,
                Message = resolution.Decision == MappingRuleDecision.UseDefault
                    ? "No source path; fallback default path selected."
                    : "Confidence auto-map selected."
            };

            if (mapping == null || string.IsNullOrWhiteSpace(mapping.Rules))
                return trace;

            resolution.Priority = MappingRulePriority.ExplicitUserRule;
            trace.Priority = MappingRulePriority.ExplicitUserRule;

            var ruleText = mapping.Rules.Trim();
            var sourceOverride = ParseSourceOverride(ruleText);
            if (!string.IsNullOrWhiteSpace(sourceOverride))
                resolution.SourcePath = sourceOverride;

            var conditionExpression = ParseConditionExpression(ruleText);
            if (!string.IsNullOrWhiteSpace(conditionExpression))
            {
                var conditionMatched = EvaluateConditionExpression(source, conditionExpression, resolution.SourcePath);
                trace.ConditionMatched = conditionMatched;
                if (!conditionMatched)
                {
                    resolution.Decision = MappingRuleDecision.Skip;
                    trace.Decision = MappingRuleDecision.Skip;
                    trace.Message = $"Condition not met: {conditionExpression}";
                    trace.SourcePath = resolution.SourcePath;

                    var falseTransforms = ParseTransformPipeline(ruleText, preferFalseBranch: true);
                    if (falseTransforms.Count > 0)
                    {
                        resolution.Decision = MappingRuleDecision.Map;
                        resolution.ExplicitTransforms = falseTransforms;
                        trace.Decision = MappingRuleDecision.Map;
                        trace.Message = $"Condition not met; false-branch transform pipeline applied ({falseTransforms.Count} step(s)).";
                    }

                    return trace;
                }
            }

            var nullDefaultValue = ParseNullDefaultValue(ruleText);
            if (nullDefaultValue.parsed)
            {
                resolution.NullDefaultSpecified = true;
                resolution.NullDefaultValue = nullDefaultValue.value;
            }

            var trueTransforms = ParseTransformPipeline(ruleText, preferFalseBranch: false);
            if (trueTransforms.Count > 0)
                resolution.ExplicitTransforms = trueTransforms;

            resolution.Decision = MappingRuleDecision.Map;
            trace.Decision = MappingRuleDecision.Map;
            trace.SourcePath = resolution.SourcePath;
            trace.Message = "Explicit user rule selected.";
            return trace;
        }

        private static object ResolveSourceValueFromPath(object source, string sourcePath)
        {
            if (source == null || string.IsNullOrWhiteSpace(sourcePath))
                return null;

            var value = GetMemberValueByPath(source, sourcePath);
            if (value != null)
                return value;

            var sourceType = source.GetType();
            var property = sourceType.GetProperty(sourcePath, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.IgnoreCase);
            if (property != null)
                return property.GetValue(source);
            return null;
        }

        private static bool EvaluateConditionExpression(object source, string expression, string defaultSourcePath)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return true;

            expression = expression.Trim();
            if (expression.StartsWith("AND(", StringComparison.OrdinalIgnoreCase))
            {
                var args = ParseFunctionArguments(expression, "AND");
                return args.All(arg => EvaluateConditionExpression(source, arg, defaultSourcePath));
            }

            if (expression.StartsWith("OR(", StringComparison.OrdinalIgnoreCase))
            {
                var args = ParseFunctionArguments(expression, "OR");
                return args.Any(arg => EvaluateConditionExpression(source, arg, defaultSourcePath));
            }

            if (expression.StartsWith("NOT(", StringComparison.OrdinalIgnoreCase))
            {
                var args = ParseFunctionArguments(expression, "NOT");
                return args.Count == 1 && !EvaluateConditionExpression(source, args[0], defaultSourcePath);
            }

            if (expression.StartsWith("ISNULL(", StringComparison.OrdinalIgnoreCase))
            {
                var args = ParseFunctionArguments(expression, "ISNULL");
                var value = ResolveOperandValue(source, args.FirstOrDefault(), defaultSourcePath);
                return value == null || value == DBNull.Value;
            }

            if (expression.StartsWith("NOTNULL(", StringComparison.OrdinalIgnoreCase))
            {
                var args = ParseFunctionArguments(expression, "NOTNULL");
                var value = ResolveOperandValue(source, args.FirstOrDefault(), defaultSourcePath);
                return value != null && value != DBNull.Value;
            }

            return EvaluateBinaryCondition(source, expression, defaultSourcePath);
        }

        private static bool EvaluateBinaryCondition(object source, string expression, string defaultSourcePath)
        {
            foreach (var functionName in new[] { "EQ", "NE", "GT", "GTE", "LT", "LTE" })
            {
                if (!expression.StartsWith(functionName + "(", StringComparison.OrdinalIgnoreCase))
                    continue;

                var args = ParseFunctionArguments(expression, functionName);
                if (args.Count < 2)
                    return false;

                var left = ResolveOperandValue(source, args[0], defaultSourcePath);
                var right = ResolveOperandValue(source, args[1], defaultSourcePath);
                var comparison = Compare(left, right);

                return functionName.ToUpperInvariant() switch
                {
                    "EQ" => comparison == 0,
                    "NE" => comparison != 0,
                    "GT" => comparison > 0,
                    "GTE" => comparison >= 0,
                    "LT" => comparison < 0,
                    "LTE" => comparison <= 0,
                    _ => false
                };
            }

            return false;
        }

        private static int Compare(object left, object right)
        {
            if (left == null && right == null)
                return 0;
            if (left == null)
                return -1;
            if (right == null)
                return 1;

            if (TryConvertToDecimal(left, out var leftNumber) && TryConvertToDecimal(right, out var rightNumber))
                return leftNumber.CompareTo(rightNumber);

            if (DateTime.TryParse(left.ToString(), out var leftDate) && DateTime.TryParse(right.ToString(), out var rightDate))
                return leftDate.CompareTo(rightDate);

            return string.Compare(left.ToString(), right.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryConvertToDecimal(object value, out decimal parsed)
        {
            if (value is decimal directDecimal)
            {
                parsed = directDecimal;
                return true;
            }

            return decimal.TryParse(value?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out parsed);
        }

        private static object ResolveOperandValue(object source, string token, string defaultSourcePath)
        {
            token = token?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(token))
                return ResolveSourceValueFromPath(source, defaultSourcePath);

            if ((token.StartsWith("'") && token.EndsWith("'")) || (token.StartsWith("\"") && token.EndsWith("\"")))
                return token.Substring(1, token.Length - 2);

            if (token.StartsWith("Source.", StringComparison.OrdinalIgnoreCase))
                return ResolveSourceValueFromPath(source, token.Substring("Source.".Length));

            if (bool.TryParse(token, out var boolValue))
                return boolValue;
            if (decimal.TryParse(token, NumberStyles.Any, CultureInfo.InvariantCulture, out var numericValue))
                return numericValue;

            return token;
        }

        private static string ParseConditionExpression(string rules)
        {
            if (string.IsNullOrWhiteSpace(rules))
                return string.Empty;

            var keyValue = Regex.Match(rules, @"(?i)when\s*[:=]\s*(?<expr>[^;|]+)");
            if (keyValue.Success)
                return keyValue.Groups["expr"].Value.Trim();

            var inline = Regex.Match(rules, @"(?i)\bwhen\s+(?<expr>.+?)(?:\bthen\b|\bmap\b|$)");
            return inline.Success ? inline.Groups["expr"].Value.Trim() : string.Empty;
        }

        private static string ParseSourceOverride(string rules)
        {
            if (string.IsNullOrWhiteSpace(rules))
                return string.Empty;

            var keyValue = Regex.Match(rules, @"(?i)(source|mapsource)\s*[:=]\s*(?<path>[A-Za-z0-9_.]+)");
            if (keyValue.Success)
                return keyValue.Groups["path"].Value.Trim();

            var dsl = Regex.Match(rules, @"(?i)\bmap\s+(?<src>[A-Za-z0-9_.]+)\s*->");
            if (dsl.Success)
                return dsl.Groups["src"].Value.Replace("Source.", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();

            return string.Empty;
        }

        private static (bool parsed, object value) ParseNullDefaultValue(string rules)
        {
            if (string.IsNullOrWhiteSpace(rules))
                return (false, null);

            var explicitValue = Regex.Match(rules, @"(?i)(nulldefault|default)\s*[:=]\s*(?<value>[^;|]+)");
            if (explicitValue.Success)
                return (true, ParseLiteral(explicitValue.Groups["value"].Value.Trim()));

            var dsl = Regex.Match(rules, @"(?i)if\s+source\s+is\s+null\s+then\s+default\((?<value>.*?)\)");
            if (dsl.Success)
                return (true, ParseLiteral(dsl.Groups["value"].Value.Trim()));

            return (false, null);
        }

        private static object ParseLiteral(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            if ((value.StartsWith("'") && value.EndsWith("'")) || (value.StartsWith("\"") && value.EndsWith("\"")))
                return value.Substring(1, value.Length - 2);

            if (bool.TryParse(value, out var boolValue))
                return boolValue;
            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var numericValue))
                return numericValue;
            return value;
        }

        private static List<FieldTransformStep> ParseTransformPipeline(string rules, bool preferFalseBranch)
        {
            var steps = new List<FieldTransformStep>();
            if (string.IsNullOrWhiteSpace(rules))
                return steps;

            var key = preferFalseBranch ? "transform_false" : "transform";
            var keyValue = Regex.Match(rules, $@"(?i){Regex.Escape(key)}\s*[:=]\s*(?<pipeline>[^;]+)");
            if (keyValue.Success)
            {
                steps.AddRange(ParseStepPipeline(keyValue.Groups["pipeline"].Value));
                return steps;
            }

            if (!preferFalseBranch)
            {
                var dsl = Regex.Match(rules, @"(?i)\btransform\s+(?<pipeline>.+?)(?:\bonNull\b|\bonError\b|->|$)");
                if (dsl.Success)
                    steps.AddRange(ParseStepPipeline(dsl.Groups["pipeline"].Value));
            }

            return steps;
        }

        private static IEnumerable<FieldTransformStep> ParseStepPipeline(string pipeline)
        {
            var segments = (pipeline ?? string.Empty).Split(new[] { '|', ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var segment in segments)
            {
                var trimmed = segment.Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                    continue;

                var function = Regex.Match(trimmed, @"^(?<name>[A-Za-z0-9_]+)\((?<arg>.*)\)$");
                if (function.Success)
                {
                    yield return new FieldTransformStep
                    {
                        Name = function.Groups["name"].Value.Trim(),
                        Argument = function.Groups["arg"].Value.Trim()
                    };
                }
                else
                {
                    yield return new FieldTransformStep { Name = trimmed, Argument = string.Empty };
                }
            }
        }

        private static List<string> ParseFunctionArguments(string expression, string functionName)
        {
            var inner = expression.Trim();
            var prefix = functionName + "(";
            if (!inner.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || !inner.EndsWith(")", StringComparison.Ordinal))
                return new List<string>();

            inner = inner.Substring(prefix.Length, inner.Length - prefix.Length - 1);
            var values = new List<string>();
            var current = string.Empty;
            var depth = 0;
            var inSingleQuote = false;
            var inDoubleQuote = false;

            foreach (var ch in inner)
            {
                if (ch == '\'' && !inDoubleQuote)
                    inSingleQuote = !inSingleQuote;
                else if (ch == '"' && !inSingleQuote)
                    inDoubleQuote = !inDoubleQuote;

                if (!inSingleQuote && !inDoubleQuote)
                {
                    if (ch == '(') depth++;
                    if (ch == ')') depth--;
                }

                if (ch == ',' && depth == 0 && !inSingleQuote && !inDoubleQuote)
                {
                    values.Add(current.Trim());
                    current = string.Empty;
                    continue;
                }

                current += ch;
            }

            if (!string.IsNullOrWhiteSpace(current))
                values.Add(current.Trim());

            return values;
        }

        private sealed class RuleResolution
        {
            public MappingRulePriority Priority { get; set; }
            public MappingRuleDecision Decision { get; set; }
            public string SourcePath { get; set; } = string.Empty;
            public bool NullDefaultSpecified { get; set; }
            public object NullDefaultValue { get; set; }
            public List<FieldTransformStep> ExplicitTransforms { get; set; } = new List<FieldTransformStep>();
        }
    }
}
