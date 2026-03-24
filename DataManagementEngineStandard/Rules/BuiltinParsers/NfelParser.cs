using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TheTechIdea.Beep.Rules.BuiltinParsers
{
    /// <summary>
    /// NFEL — Natural Formula Expression Language parser.
    /// A lightweight, human-readable expression language inspired by MVEL/SpEL:
    ///   - Arithmetic: <c>price * qty</c>, <c>(a + b) / 2</c>
    ///   - Comparison: <c>age &gt;= 18</c>, <c>name == 'Alice'</c>
    ///   - Logical:    <c>isActive &amp;&amp; age &gt; 0</c>, <c>!flag</c>
    ///   - Ternary:    <c>score &gt; 50 ? 'pass' : 'fail'</c>
    ///   - Field refs: <c>order.total</c>
    ///   - Null check: <c>value != null</c>
    ///
    /// Tokens are built with correct <see cref="TokenType"/> values for downstream
    /// use by <see cref="RulesEngine"/> expression evaluation.
    /// </summary>
    [RuleParser(parserKey: "NfelParser")]
    public sealed class NfelParser : IRuleParser
    {
        private static readonly Regex _tokenRx = new Regex(
            @"(?<Ternary>[?:])" +                                         // ternary ? :
            @"|(?<LogicNot>!(?!=))" +                                     // logical !
            @"|(?<LogicAnd>&&|\bAND\b)" +                                 // && or AND
            @"|(?<LogicOr>\|\||\bOR\b)" +                                 // || or OR
            @"|(?<Cmp>==|!=|<=|>=|[<>])" +                               // comparison
            @"|(?<Str>""[^""]*""|'[^']*')" +                             // string literals
            @"|(?<Bool>\b(true|false)\b)" +                               // bool literals
            @"|(?<Null>\bnull\b)" +                                       // null literal
            @"|(?<Num>-?\d+(?:\.\d+)?)" +                                // number
            @"|(?<ArithOp>[+\-*\/%^])" +                                 // arithmetic
            @"|(?<Paren>[()])" +                                          // parens
            @"|(?<Comma>,)" +                                             // comma
            @"|(?<Ident>[A-Za-z_][A-Za-z0-9_.]*)",                       // identifiers + field refs
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

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
                    Message  = "NFEL expression is null or empty."
                });
                return result;
            }

            var tokens     = new List<Token>();
            var diags      = new List<ParseDiagnostic>();
            int parenDepth = 0;

            foreach (Match m in _tokenRx.Matches(expression))
            {
                TokenType tt;
                if      (m.Groups["Str"].Success)      tt = TokenType.StringLiteral;
                else if (m.Groups["Bool"].Success)     tt = TokenType.BooleanLiteral;
                else if (m.Groups["Null"].Success)     tt = TokenType.NullLiteral;
                else if (m.Groups["Num"].Success)      tt = TokenType.NumericLiteral;
                else if (m.Groups["LogicAnd"].Success) tt = TokenType.And;
                else if (m.Groups["LogicOr"].Success)  tt = TokenType.Or;
                else if (m.Groups["LogicNot"].Success) tt = TokenType.Not;
                else if (m.Groups["Cmp"].Success)
                {
                    tt = m.Value switch
                    {
                        "==" => TokenType.Equal,
                        "!=" => TokenType.NotEqual,
                        ">"  => TokenType.GreaterThan,
                        "<"  => TokenType.LessThan,
                        ">=" => TokenType.GreaterEqual,
                        "<=" => TokenType.LessEqual,
                        _    => TokenType.Unknown
                    };
                }
                else if (m.Groups["ArithOp"].Success)
                {
                    tt = m.Value switch
                    {
                        "+" => TokenType.Plus,
                        "-" => TokenType.Minus,
                        "*" => TokenType.Multiply,
                        "/" => TokenType.Divide,
                        "%" => TokenType.Modulo,
                        _   => TokenType.Unknown
                    };
                }
                else if (m.Groups["Paren"].Success)
                {
                    if (m.Value == "(") { tt = TokenType.LeftParenthesis;  parenDepth++; }
                    else                { tt = TokenType.RightParenthesis; parenDepth--; }
                }
                else if (m.Groups["Comma"].Success)   tt = TokenType.Comma;
                else if (m.Groups["Ternary"].Success) tt = TokenType.Unknown;
                else                                  tt = TokenType.Identifier;

                tokens.Add(new Token(tt, m.Value, m.Index, m.Length));
            }

            if (parenDepth != 0)
                diags.Add(new ParseDiagnostic
                {
                    Code     = DiagnosticCode.MismatchedParenthesis,
                    Severity = DiagnosticSeverity.Error,
                    Message  = "Unmatched parenthesis in NFEL expression."
                });

            var structure = new RuleStructure
            {
                Expression = expression,
                Tokens     = tokens,
                Rulename   = "Nfel",
                RuleType   = "NfelParser"
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
