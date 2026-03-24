using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TheTechIdea.Beep.Rules.BuiltinParsers
{
    /// <summary>
    /// Parses simplified SQL WHERE clause expressions into a <see cref="RuleStructure"/>.
    /// Recognizes:
    ///   - Simple comparisons: <c>field = 'value'</c>, <c>field &gt; 42</c>
    ///   - IN list:            <c>field IN ('a','b','c')</c>
    ///   - NULL checks:        <c>field IS NULL</c>, <c>field IS NOT NULL</c>
    ///   - BETWEEN:            <c>field BETWEEN 10 AND 20</c>
    ///   - AND / OR joins
    /// Returns a flat list of tokens representing the condition tree.
    /// </summary>
    [RuleParser(parserKey: "SqlWhereParser")]
    public sealed class SqlWhereParser : IRuleParser
    {
        private static readonly Regex _tokenRx = new Regex(
            @"(?<IsNull>IS\s+NOT\s+NULL|IS\s+NULL)" +
            @"|(?<Between>BETWEEN)" +
            @"|(?<In>\bIN\b)" +
            @"|(?<AndOr>\bAND\b|\bOR\b)" +
            @"|(?<Op>[<>!]=?|=)" +
            @"|(?<Str>'[^']*')" +
            @"|(?<Num>-?\d+(?:\.\d+)?)" +
            @"|(?<Paren>[()])" +
            @"|(?<Comma>,)" +
            @"|(?<Ident>[A-Za-z_][A-Za-z0-9_.]*)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

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
                    Message  = "WHERE clause expression is null or empty."
                });
                return result;
            }

            var tokens = new List<Token>();
            var diags  = new List<ParseDiagnostic>();

            foreach (Match m in _tokenRx.Matches(expression))
            {
                TokenType tt;
                if      (m.Groups["IsNull"].Success)  tt = TokenType.Identifier;   // reuse Identifier for IS NULL / IS NOT NULL
                else if (m.Groups["Between"].Success) tt = TokenType.Identifier;
                else if (m.Groups["In"].Success)      tt = TokenType.Identifier;
                else if (m.Groups["AndOr"].Success)   tt = m.Value.ToUpperInvariant() == "AND" ? TokenType.And : TokenType.Or;
                else if (m.Groups["Op"].Success)      tt = TokenType.Unknown;
                else if (m.Groups["Str"].Success)     tt = TokenType.StringLiteral;
                else if (m.Groups["Num"].Success)     tt = TokenType.NumericLiteral;
                else if (m.Groups["Paren"].Success)   tt = m.Value == "(" ? TokenType.LeftParenthesis : TokenType.RightParenthesis;
                else if (m.Groups["Comma"].Success)   tt = TokenType.Comma;
                else                                  tt = TokenType.Identifier;

                tokens.Add(new Token(tt, m.Value.Trim(), m.Index, m.Length));
            }

            int parenDepth = 0;
            foreach (var t in tokens)
            {
                if (t.Type == TokenType.OpenParen)  parenDepth++;
                if (t.Type == TokenType.CloseParen) parenDepth--;
            }
            if (parenDepth != 0)
                diags.Add(new ParseDiagnostic
                {
                    Code     = DiagnosticCode.MismatchedParenthesis,
                    Severity = DiagnosticSeverity.Error,
                    Message  = "Unmatched parenthesis in WHERE clause."
                });

            var structure = new RuleStructure
            {
                Expression = expression,
                Tokens     = tokens,
                Rulename   = "SqlWhere",
                RuleType   = "SqlParser"
            };
            structure.Touch();
            _structures.Add(structure);

            result.Success     = !diags.Exists(d => d.Severity == DiagnosticSeverity.Error);
            result.Structure   = structure;
            result.Diagnostics = diags;
            return result;
        }

        public ParseResult ParseRule(IRule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            return ParseRule(rule.RuleText);
        }

        public void Clear() => _structures.Clear();
    }
}
