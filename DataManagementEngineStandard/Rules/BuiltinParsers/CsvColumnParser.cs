using System;
using System.Collections.Generic;
using System.Linq;

namespace TheTechIdea.Beep.Rules.BuiltinParsers
{
    /// <summary>
    /// Parses CSV-style column-reference expressions of the form:
    ///   <c>column[N]</c> or <c>row.column[N]</c>
    /// where N is a 0-based column index or column name.
    /// Examples: <c>col[2]</c>, <c>row.price[0]</c>, <c>Amount[3]</c>.
    /// </summary>
    [RuleParser(parserKey: "CsvColumnParser")]
    public sealed class CsvColumnParser : IRuleParser
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
                    Code       = DiagnosticCode.EmptyExpression,
                    Severity   = DiagnosticSeverity.Error,
                    Start      = 0, Length = 0,
                    Message    = "Expression is null or empty.",
                    Suggestion = "Provide a CSV column reference, e.g. Amount[2]"
                });
                return result;
            }

            var tokens = new List<Token>();
            var diags  = new List<ParseDiagnostic>();

            // Expected pattern: [rowAlias.]columnName[index]  e.g. "row.Amount[3]" or "Amount[3]"
            int i = 0;
            string? rowAlias    = null;
            string? columnName  = null;
            int?    columnIndex = null;

            // Optional rowAlias. prefix
            int dotPos = expression.IndexOf('.', StringComparison.Ordinal);
            int bracketOpen  = expression.IndexOf('[', StringComparison.Ordinal);
            int bracketClose = expression.IndexOf(']', StringComparison.Ordinal);

            if (dotPos >= 0 && (bracketOpen < 0 || dotPos < bracketOpen))
            {
                rowAlias   = expression[..dotPos].Trim();
                expression = expression[(dotPos + 1)..];
                bracketOpen  = expression.IndexOf('[', StringComparison.Ordinal);
                bracketClose = expression.IndexOf(']', StringComparison.Ordinal);
            }

            if (bracketOpen < 0 || bracketClose < 0 || bracketClose < bracketOpen)
            {
                diags.Add(new ParseDiagnostic
                {
                    Code     = DiagnosticCode.UnexpectedToken,
                    Severity = DiagnosticSeverity.Error,
                    Start    = 0, Length = expression.Length,
                    Message  = "Expected format: ColumnName[Index]",
                    Suggestion = "Wrap the column index in square brackets, e.g. Amount[2]"
                });
                result.Success     = false;
                result.Diagnostics = diags;
                return result;
            }

            columnName = expression[..bracketOpen].Trim();
            string indexStr = expression[(bracketOpen + 1)..bracketClose].Trim();

            if (int.TryParse(indexStr, out int colIdx))
                columnIndex = colIdx;

            tokens.Add(new Token(TokenType.Identifier,    columnName ?? string.Empty, 0,               bracketOpen));
            tokens.Add(new Token(TokenType.NumericLiteral, indexStr,                   bracketOpen + 1, indexStr.Length));

            var structure = new RuleStructure
            {
                Expression = expression,
                Tokens     = tokens,
                Rulename   = "CsvColumnRef",
                RuleType   = "CsvParser"
            };
            structure.Touch();
            _structures.Add(structure);

            result.Success     = true;
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
