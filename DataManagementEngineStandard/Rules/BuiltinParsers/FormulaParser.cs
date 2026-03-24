using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TheTechIdea.Beep.Rules.BuiltinParsers
{
    /// <summary>
    /// Parses Excel/spreadsheet-style formula expressions into tokens.
    /// Recognizes:
    ///   - Function calls:    <c>SUM(A, B)</c>, <c>IF(cond, trueVal, falseVal)</c>
    ///   - Arithmetic ops:    <c>+  -  *  /  ^</c>
    ///   - Comparison ops:    <c>&lt;  &gt;  &lt;=  &gt;=  =  &lt;&gt;</c>
    ///   - String literals:   <c>"text"</c> or <c>'text'</c>
    ///   - Number literals:   <c>42</c>, <c>3.14</c>
    ///   - Cell references:   <c>A1</c>, <c>$B$2</c>, <c>Sheet1!C3</c>
    ///   - Identifiers:       bare names used as variables/parameters
    /// Resulting tokens are stored in the <see cref="RuleStructure"/> for
    /// downstream evaluation by <see cref="RulesEngine"/>.
    /// </summary>
    [RuleParser(parserKey: "FormulaParser")]
    public sealed class FormulaParser : IRuleParser
    {
        // Language tokens in priority order
        private static readonly Regex _tokenRx = new Regex(
            @"(?<CellRef>\$?[A-Za-z]+\d+(?:!\$?[A-Za-z]+\d+)?)" +  // A1, $B$2, Sheet1!C3
            @"|(?<FuncName>[A-Za-z_][A-Za-z0-9_]*(?=\s*\())" +      // SUM, IF, etc. (followed by '(')
            @"|(?<Str>""[^""]*""|'[^']*')" +                         // "text" or 'text'
            @"|(?<Num>-?\d+(?:\.\d+)?)" +                            // 42, 3.14
            @"|(?<Op>[<>!]=?|[=+\-*\/^&]|<>)" +                     // operators
            @"|(?<Paren>[()])" +                                      // parentheses
            @"|(?<Comma>,)" +                                         // argument separator
            @"|(?<Ident>[A-Za-z_][A-Za-z0-9_]*)",                    // plain identifier/variable
            RegexOptions.Compiled);

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
                    Message  = "Formula expression is null or empty."
                });
                return result;
            }

            var tokens = new List<Token>();
            var diags  = new List<ParseDiagnostic>();
            int parenDepth = 0;

            foreach (Match m in _tokenRx.Matches(expression))
            {
                TokenType tt;
                if      (m.Groups["FuncName"].Success) tt = TokenType.Identifier;
                else if (m.Groups["CellRef"].Success)  tt = TokenType.Identifier;
                else if (m.Groups["Str"].Success)      tt = TokenType.StringLiteral;
                else if (m.Groups["Num"].Success)      tt = TokenType.NumericLiteral;
                else if (m.Groups["Op"].Success)       tt = TokenType.Unknown;
                else if (m.Groups["Paren"].Success)
                {
                    tt = m.Value == "(" ? TokenType.LeftParenthesis : TokenType.RightParenthesis;
                    parenDepth += m.Value == "(" ? 1 : -1;
                }
                else if (m.Groups["Comma"].Success)    tt = TokenType.Comma;
                else                                   tt = TokenType.Identifier;

                tokens.Add(new Token(tt, m.Value, m.Index, m.Length));
            }

            if (parenDepth != 0)
                diags.Add(new ParseDiagnostic
                {
                    Code     = DiagnosticCode.MismatchedParenthesis,
                    Severity = DiagnosticSeverity.Error,
                    Message  = "Unmatched parenthesis in formula."
                });

            var structure = new RuleStructure
            {
                Expression = expression,
                Tokens     = tokens,
                Rulename   = "Formula",
                RuleType   = "FormulaParser"
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
