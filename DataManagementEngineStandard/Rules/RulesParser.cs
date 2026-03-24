using System;
using System.Collections.Generic;
using System.Linq;

namespace TheTechIdea.Beep.Rules
{
    /// <summary>
    /// Default implementation of <see cref="IRuleParser"/>.
    /// Tokenizes and validates rule expressions, returning a <see cref="ParseResult"/>
    /// that always carries structured diagnostics — no exceptions for malformed input.
    /// </summary>
    [RuleParser(parserKey: "RulesParser")]
    public sealed class RuleParser : IRuleParser, IDisposable
    {
        private readonly List<IRuleStructure> _structures = new List<IRuleStructure>();

        List<IRuleStructure> IRuleParser.RuleStructures => _structures;

        public ParseResult ParseRule(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                var empty = new ParseResult { Success = false };
                empty.Diagnostics.Add(new ParseDiagnostic
                {
                    Code = DiagnosticCode.EmptyExpression, Severity = DiagnosticSeverity.Error,
                    Start = 0, Length = 0,
                    Message = "Expression is null or empty.",
                    Suggestion = "Provide a non-empty rule expression."
                });
                return empty;
            }

            var tokenizeResult = new Tokenizer(expression).Tokenize();
            var diags = new List<ParseDiagnostic>(tokenizeResult.Diagnostics);
            ValidatePrecedenceAndParentheses(tokenizeResult.Tokens, diags);

            var structure = new RuleStructure
            {
                Expression = expression,
                Tokens     = tokenizeResult.Tokens.ToList(),
                Rulename   = "Rule",
                RuleType   = "Advanced"
            };
            structure.Touch();
            _structures.Add(structure);

            return new ParseResult
            {
                Success     = !diags.Any(d => d.Severity == DiagnosticSeverity.Error),
                Structure   = structure,
                Diagnostics = diags
            };
        }

        public ParseResult ParseRule(IRule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            var result = ParseRule(rule.RuleText);

            var attr = (RuleAttribute)rule.GetType()
                           .GetCustomAttributes(typeof(RuleAttribute), false).FirstOrDefault();
            if (attr != null && result.Structure is RuleStructure s)
            {
                s.Rulename = attr.RuleName;
                s.RuleType = attr.RuleType;
                s.Author   = attr.RuleAuthor;
                s.Touch();
            }
            return result;
        }

        public void Clear() => _structures.Clear();
        public void Dispose() => Clear();

        private static void ValidatePrecedenceAndParentheses(
            IReadOnlyList<Token> tokens, List<ParseDiagnostic> diags)
        {
            int depth = 0, openAt = -1;
            foreach (var t in tokens)
            {
                if (t.Type == TokenType.LeftParenthesis)  { depth++; if (depth == 1) openAt = t.Start; }
                if (t.Type == TokenType.RightParenthesis) { depth--; }
                if (depth < 0)
                {
                    diags.Add(new ParseDiagnostic
                    {
                        Code = DiagnosticCode.MismatchedParenthesis, Severity = DiagnosticSeverity.Error,
                        Start = t.Start, Length = t.Length,
                        Message = "Unexpected closing parenthesis with no matching open.",
                        Suggestion = "Remove the extra ')' or add a matching '('."
                    });
                    depth = 0;
                }
            }
            if (depth > 0)
                diags.Add(new ParseDiagnostic
                {
                    Code = DiagnosticCode.MismatchedParenthesis, Severity = DiagnosticSeverity.Error,
                    Start = openAt, Length = 1,
                    Message = $"{depth} opening parenthesis/parentheses not closed.",
                    Suggestion = $"Add {depth} closing ')' to balance the expression."
                });
        }
    }
}
