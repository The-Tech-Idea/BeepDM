using System;
using System.Collections.Generic;
using System.Text;

namespace TheTechIdea.Beep.Rules
{
    /// <summary>
    /// Stateless, deterministic lexer. Converts a rule expression string into
    /// a <see cref="TokenizeResult"/> carrying the token stream and structured diagnostics.
    /// </summary>
    public sealed class Tokenizer
    {
        private readonly string _input;
        private int _pos;

        public Tokenizer(string input)
        {
            _input = input ?? string.Empty;
            _pos = 0;
        }


        public TokenizeResult Tokenize()
        {
            var tokens = new List<Token>();
            var diags = new List<ParseDiagnostic>();

            while (!AtEnd)
            {
                SkipWhitespace();
                if (AtEnd) break;

                int start = _pos;
                char c = Peek();

                if (c == ':')              { tokens.Add(ReadPrefixedToken(TokenType.EntityField, start));   continue; }
                if (c == '@')              { tokens.Add(ReadPrefixedToken(TokenType.RuleReference, start)); continue; }
                if (c == '"' || c == '\'') { tokens.Add(ReadStringLiteral(start, diags));                  continue; }
                if (c == '#')              { tokens.Add(ReadHashLiteral(start));                            continue; }
                if (IsDigitStart(c))       { tokens.Add(ReadNumericLiteral(start, diags));                  continue; }
                if (IsOperatorStart(c))    { tokens.Add(ReadOperator(start, diags));                        continue; }
                if (c == '(') { Advance(); tokens.Add(new Token(TokenType.LeftParenthesis,  "(", start, 1)); continue; }
                if (c == ')') { Advance(); tokens.Add(new Token(TokenType.RightParenthesis, ")", start, 1)); continue; }
                if (c == ',') { Advance(); tokens.Add(new Token(TokenType.Comma, ",", start, 1));            continue; }
                if (c == '%') { Advance(); tokens.Add(new Token(TokenType.Modulo, "%", start, 1));           continue; }
                if (IsIdentifierStart(c))  { tokens.Add(ReadIdentifierOrKeyword(start));                    continue; }

                diags.Add(MakeDiag(DiagnosticCode.UnknownToken, DiagnosticSeverity.Error, start, 1,
                    $"Unexpected character '{c}'.", $"Remove or replace the character at position {start}."));
                Advance();
            }

            return new TokenizeResult(tokens, diags);
        }

        // ── Readers ──────────────────────────────────────────────────────────────────

        private Token ReadPrefixedToken(TokenType type, int start)
        {
            Advance(); // consume ':' or '@'
            var sb = new StringBuilder();
            while (!AtEnd && !IsDelimiter(Peek()))
                sb.Append(Advance());
            return new Token(type, sb.ToString(), start, _pos - start);
        }

        private Token ReadStringLiteral(int start, List<ParseDiagnostic> diags)
        {
            char quote = Advance();
            var sb = new StringBuilder();
            while (!AtEnd && Peek() != quote)
            {
                if (Peek() == '\\') { Advance(); if (!AtEnd) sb.Append(Advance()); }
                else sb.Append(Advance());
            }
            if (AtEnd)
                diags.Add(MakeDiag(DiagnosticCode.UnterminatedString, DiagnosticSeverity.Error, start, _pos - start,
                    "String literal is not closed.", $"Add a closing {quote} to terminate the string."));
            else
                Advance(); // closing quote
            return new Token(TokenType.StringLiteral, sb.ToString(), start, _pos - start);
        }

        private Token ReadHashLiteral(int start)
        {
            Advance(); // '#'
            var sb = new StringBuilder();
            while (!AtEnd && !IsDelimiter(Peek()) && !char.IsWhiteSpace(Peek()))
                sb.Append(Advance());
            return new Token(TokenType.StringLiteral, sb.ToString(), start, _pos - start);
        }

        private Token ReadNumericLiteral(int start, List<ParseDiagnostic> diags)
        {
            var sb = new StringBuilder();
            bool hasDot = false;
            while (!AtEnd && (char.IsDigit(Peek()) || (Peek() == '.' && !hasDot)))
            {
                if (Peek() == '.') hasDot = true;
                sb.Append(Advance());
            }
            string raw = sb.ToString();
            if (!double.TryParse(raw, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out _))
                diags.Add(MakeDiag(DiagnosticCode.InvalidNumeric, DiagnosticSeverity.Error, start, _pos - start,
                    $"'{raw}' is not a valid numeric literal.", "Check for extra decimal points or non-digit characters."));
            return new Token(TokenType.NumericLiteral, raw, start, _pos - start);
        }

        private Token ReadOperator(int start, List<ParseDiagnostic> diags)
        {
            var sb = new StringBuilder();
            sb.Append(Advance());
            while (!AtEnd && IsOperatorContinuation(Peek()))
                sb.Append(Advance());

            string candidate = sb.ToString();
            while (candidate.Length > 0 && LookupOperator(candidate) == null)
            {
                _pos--;
                candidate = candidate.Substring(0, candidate.Length - 1);
            }

            if (candidate.Length == 0)
            {
                diags.Add(MakeDiag(DiagnosticCode.InvalidOperator, DiagnosticSeverity.Error, start, 1,
                    $"Unrecognized operator starting with '{_input[start]}'.", null));
                return new Token(TokenType.Unknown, _input[start].ToString(), start, 1);
            }

            return new Token(LookupOperator(candidate)!.Value, candidate, start, candidate.Length);
        }

        private Token ReadIdentifierOrKeyword(int start)
        {
            var sb = new StringBuilder();
            while (!AtEnd && (char.IsLetterOrDigit(Peek()) || Peek() == '_'))
                sb.Append(Advance());
            string word = sb.ToString();

            if (word.Equals("true",  StringComparison.OrdinalIgnoreCase)) return new Token(TokenType.BooleanLiteral, "true",  start, _pos - start);
            if (word.Equals("false", StringComparison.OrdinalIgnoreCase)) return new Token(TokenType.BooleanLiteral, "false", start, _pos - start);
            if (word.Equals("null",  StringComparison.OrdinalIgnoreCase)) return new Token(TokenType.NullLiteral,    "null",  start, _pos - start);
            if (word.Equals("AND",   StringComparison.OrdinalIgnoreCase)) return new Token(TokenType.And,            word,    start, _pos - start);
            if (word.Equals("OR",    StringComparison.OrdinalIgnoreCase)) return new Token(TokenType.Or,             word,    start, _pos - start);
            if (word.Equals("NOT",   StringComparison.OrdinalIgnoreCase)) return new Token(TokenType.Not,            word,    start, _pos - start);

            return new Token(TokenType.Identifier, word, start, _pos - start);
        }

        // ── Helpers ──────────────────────────────────────────────────────────────────

        private bool AtEnd => _pos >= _input.Length;
        private char Peek() => _input[_pos];
        private char Advance() => _input[_pos++];
        private void SkipWhitespace() { while (!AtEnd && char.IsWhiteSpace(Peek())) Advance(); }
        private bool IsDigitStart(char c) => char.IsDigit(c) || (c == '.' && _pos + 1 < _input.Length && char.IsDigit(_input[_pos + 1]));
        private static bool IsOperatorStart(char c)        => "+-*/=!<>&|".IndexOf(c) >= 0;
        private static bool IsOperatorContinuation(char c) => "=!<>&|".IndexOf(c) >= 0;
        private static bool IsIdentifierStart(char c) => char.IsLetter(c) || c == '_';
        private static bool IsDelimiter(char c) =>
            c == ':' || c == '@' || c == '#' || c == '(' || c == ')' || c == ',' || IsOperatorStart(c);

        private static TokenType? LookupOperator(string op) => op switch
        {
            "+"  => TokenType.Plus,
            "-"  => TokenType.Minus,
            "*"  => TokenType.Multiply,
            "/"  => TokenType.Divide,
            "==" => TokenType.Equal,
            "!=" => TokenType.NotEqual,
            "<>" => TokenType.NotEqual,
            ">"  => TokenType.GreaterThan,
            "<"  => TokenType.LessThan,
            ">=" => TokenType.GreaterEqual,
            "<=" => TokenType.LessEqual,
            "&&" => TokenType.And,
            "||" => TokenType.Or,
            "!"  => TokenType.Not,
            _    => (TokenType?)null
        };

        private static ParseDiagnostic MakeDiag(DiagnosticCode code, DiagnosticSeverity sev,
            int start, int len, string message, string suggestion) =>
            new ParseDiagnostic { Code = code, Severity = sev, Start = start, Length = len,
                                  Message = message, Suggestion = suggestion };
    }

    /// <summary>Result of a tokenize pass — always recovered; no exceptions thrown.</summary>
    public sealed class TokenizeResult
    {
        public IReadOnlyList<Token> Tokens { get; }
        public IReadOnlyList<ParseDiagnostic> Diagnostics { get; }
        public bool HasErrors
        {
            get { foreach (var d in Diagnostics) if (d.Severity == DiagnosticSeverity.Error) return true; return false; }
        }
        public TokenizeResult(List<Token> tokens, List<ParseDiagnostic> diagnostics)
        {
            Tokens = tokens;
            Diagnostics = diagnostics;
        }
    }
}

