using System;
using System.Collections.Generic;
using System.Text.Json;

namespace TheTechIdea.Beep.Rules.BuiltinParsers
{
    /// <summary>
    /// Parses a JSON-encoded rule tree into a <see cref="RuleStructure"/>.
    /// Expected JSON format (n8n-style nodes):
    /// <code>
    /// {
    ///   "rule": "Field.IsInRange",
    ///   "params": { "Min": "0", "Max": "100" },
    ///   "children": [ { "rule": "...", "params": {}, "children": [] } ]
    /// }
    /// </code>
    /// The flat token stream represents a DFS traversal of the node tree.
    /// </summary>
    [RuleParser(parserKey: "JsonRuleTreeParser")]
    public sealed class JsonRuleTreeParser : IRuleParser
    {
        private readonly List<IRuleStructure> _structures = new();

        List<IRuleStructure> IRuleParser.RuleStructures => _structures;

        public ParseResult ParseRule(string expression)
        {
            var result = new ParseResult();
            if (string.IsNullOrWhiteSpace(expression))
            {
                result.Success = false;
                result.Diagnostics.Add(new ParseDiagnostic
                {
                    Code     = DiagnosticCode.EmptyExpression,
                    Severity = DiagnosticSeverity.Error,
                    Start = 0, Length = 0,
                    Message  = "JSON rule tree expression is null or empty."
                });
                return result;
            }

            var diags = new List<ParseDiagnostic>();
            var tokens = new List<Token>();

            try
            {
                using var doc = JsonDocument.Parse(expression);
                TraverseNode(doc.RootElement, tokens, diags, 0);
            }
            catch (JsonException ex)
            {
                diags.Add(new ParseDiagnostic
                {
                    Code     = DiagnosticCode.UnexpectedToken,
                    Severity = DiagnosticSeverity.Error,
                    Start    = ex.BytePositionInLine.HasValue ? (int)ex.BytePositionInLine.Value : 0,
                    Length   = 1,
                    Message  = $"Invalid JSON: {ex.Message}",
                    Suggestion = "Ensure the rule tree is valid JSON."
                });
                result.Success     = false;
                result.Diagnostics = diags;
                return result;
            }

            var structure = new RuleStructure
            {
                Expression = expression,
                Tokens     = tokens,
                Rulename   = "JsonRuleTree",
                RuleType   = "JsonParser"
            };
            structure.Touch();
            _structures.Add(structure);

            result.Success     = !diags.Exists(d => d.Severity == DiagnosticSeverity.Error);
            result.Structure   = structure;
            result.Diagnostics = diags;
            return result;
        }

        private static void TraverseNode(JsonElement node, List<Token> tokens,
            List<ParseDiagnostic> diags, int depth)
        {
            if (node.ValueKind != JsonValueKind.Object)
            {
                diags.Add(new ParseDiagnostic
                {
                    Code = DiagnosticCode.UnexpectedToken, Severity = DiagnosticSeverity.Error,
                    Message = "Each rule tree node must be a JSON object."
                });
                return;
            }

            // Extract "rule"
            if (!node.TryGetProperty("rule", out var ruleEl) || ruleEl.ValueKind != JsonValueKind.String)
            {
                diags.Add(new ParseDiagnostic
                {
                    Code     = DiagnosticCode.UnexpectedToken,
                    Severity = DiagnosticSeverity.Error,
                    Message  = $"Node at depth {depth} is missing required 'rule' string property."
                });
                return;
            }

            string ruleKey = ruleEl.GetString() ?? string.Empty;
            tokens.Add(new Token(TokenType.Identifier, ruleKey, depth, ruleKey.Length));

            // Extract "params"
            if (node.TryGetProperty("params", out var paramsEl) && paramsEl.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in paramsEl.EnumerateObject())
                {
                    tokens.Add(new Token(
                        TokenType.Identifier,
                        $"{prop.Name}={prop.Value}",
                        depth,
                        prop.Name.Length
                    ));
                }
            }

            // Recurse "children"
            if (node.TryGetProperty("children", out var childrenEl) && childrenEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var child in childrenEl.EnumerateArray())
                    TraverseNode(child, tokens, diags, depth + 1);
            }
        }

        public ParseResult ParseRule(IRule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            return ParseRule(rule.RuleText);
        }

        public void Clear() => _structures.Clear();
    }
}
