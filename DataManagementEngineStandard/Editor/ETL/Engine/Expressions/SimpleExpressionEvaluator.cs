using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TheTechIdea.Beep.Pipelines.Models;

namespace TheTechIdea.Beep.Pipelines.Engine.Expressions
{
    // ── Token ─────────────────────────────────────────────────────────────────

    internal enum TokKind
    {
        EOF, Number, String, Bool, Null, Ident,
        Plus, Minus, Star, Slash,
        Eq, NotEq, Lt, Gt, LtEq, GtEq,
        And, Or, Not,
        LParen, RParen, Comma
    }

    internal sealed class Token
    {
        internal TokKind Kind  { get; }
        internal string  Text  { get; }
        internal Token(TokKind k, string t) { Kind = k; Text = t; }
        public override string ToString() => $"[{Kind} '{Text}']";
    }

    // ── Lexer ─────────────────────────────────────────────────────────────────

    internal sealed class Lexer
    {
        private readonly string _src;
        private int _pos;

        internal Lexer(string src) { _src = src; _pos = 0; }

        private char Peek() => _pos < _src.Length ? _src[_pos] : '\0';
        private char Read()  => _pos < _src.Length ? _src[_pos++] : '\0';

        internal IReadOnlyList<Token> Tokenize()
        {
            var tokens = new List<Token>();
            while (true)
            {
                SkipWhitespace();
                if (_pos >= _src.Length) { tokens.Add(new Token(TokKind.EOF, "")); break; }

                char c = Peek();

                if (c == '\'' || c == '"')  { tokens.Add(ReadString()); continue; }
                if (char.IsDigit(c) || (c == '-' && _pos + 1 < _src.Length && char.IsDigit(_src[_pos + 1]) && (tokens.Count == 0 || IsOperatorContext(tokens[tokens.Count - 1]))))
                                            { tokens.Add(ReadNumber()); continue; }
                if (char.IsLetter(c) || c == '_') { tokens.Add(ReadIdent()); continue; }

                tokens.Add(ReadPunct());
            }
            return tokens;
        }

        private bool IsOperatorContext(Token t) =>
            t.Kind is TokKind.LParen or TokKind.Comma or TokKind.Plus or TokKind.Minus
                   or TokKind.Star  or TokKind.Slash or TokKind.Eq   or TokKind.NotEq
                   or TokKind.Lt    or TokKind.Gt    or TokKind.LtEq or TokKind.GtEq
                   or TokKind.And   or TokKind.Or    or TokKind.Not;

        private void SkipWhitespace()
        {
            while (_pos < _src.Length && char.IsWhiteSpace(_src[_pos])) _pos++;
        }

        private Token ReadString()
        {
            char q = Read();
            var sb = new StringBuilder();
            while (_pos < _src.Length)
            {
                char c = Read();
                if (c == q) break;
                if (c == '\\' && _pos < _src.Length) c = Read(); // simple escape
                sb.Append(c);
            }
            return new Token(TokKind.String, sb.ToString());
        }

        private Token ReadNumber()
        {
            var sb = new StringBuilder();
            if (Peek() == '-') sb.Append(Read());
            while (_pos < _src.Length && (char.IsDigit(_src[_pos]) || _src[_pos] == '.'))
                sb.Append(Read());
            return new Token(TokKind.Number, sb.ToString());
        }

        private Token ReadIdent()
        {
            var sb = new StringBuilder();
            while (_pos < _src.Length && (char.IsLetterOrDigit(_src[_pos]) || _src[_pos] == '_' || _src[_pos] == '.'))
                sb.Append(Read());

            string text = sb.ToString();
            return text.ToUpperInvariant() switch
            {
                "AND"   => new Token(TokKind.And,  text),
                "OR"    => new Token(TokKind.Or,   text),
                "NOT"   => new Token(TokKind.Not,  text),
                "TRUE"  => new Token(TokKind.Bool, "true"),
                "FALSE" => new Token(TokKind.Bool, "false"),
                "NULL"  => new Token(TokKind.Null, text),
                _       => new Token(TokKind.Ident, text)
            };
        }

        private Token ReadPunct()
        {
            char c = Read();
            char next = Peek();

            return c switch
            {
                '+'                   => new Token(TokKind.Plus,   "+"),
                '*'                   => new Token(TokKind.Star,   "*"),
                '/'                   => new Token(TokKind.Slash,  "/"),
                '('                   => new Token(TokKind.LParen, "("),
                ')'                   => new Token(TokKind.RParen, ")"),
                ','                   => new Token(TokKind.Comma,  ","),
                '-' when next != '\0' => new Token(TokKind.Minus,  "-"),
                '-'                   => new Token(TokKind.Minus,  "-"),
                '='                   => new Token(TokKind.Eq,     "="),
                '<' when next == '='  => new Token(TokKind.LtEq,  "<=" )  .AndAdvance(this),
                '<'                   => new Token(TokKind.Lt,     "<"),
                '>' when next == '='  => new Token(TokKind.GtEq,  ">=" )  .AndAdvance(this),
                '>'                   => new Token(TokKind.Gt,     ">"),
                '!' when next == '='  => new Token(TokKind.NotEq, "!=")   .AndAdvance(this),
                _                     => throw new InvalidOperationException($"Unexpected character '{c}' in expression")
            };
        }

        internal void Advance() => _pos++;
    }

    internal static class TokenExt
    {
        internal static Token AndAdvance(this Token t, Lexer l) { l.Advance(); return t; }
    }

    // ── Parser ────────────────────────────────────────────────────────────────

    internal delegate object? Resolver(string fieldName);

    internal sealed class ExprParser
    {
        private readonly IReadOnlyList<Token> _tokens;
        private int _pos;
        private readonly Resolver _resolver;

        internal ExprParser(IReadOnlyList<Token> tokens, Resolver resolver)
        {
            _tokens   = tokens;
            _resolver = resolver;
        }

        private Token Curr  => _tokens[_pos];
        private Token Peek  => _pos + 1 < _tokens.Count ? _tokens[_pos + 1] : _tokens[_tokens.Count - 1];
        private Token Eat() => _tokens[_pos++];

        internal object? Parse() => ParseOr();

        // OR has lowest precedence
        private object? ParseOr()
        {
            var left = ParseAnd();
            while (Curr.Kind == TokKind.Or)
            {
                Eat();
                var right = ParseAnd();
                left = ToBoolean(left) || ToBoolean(right) ? (object?)true : false;
            }
            return left;
        }

        private object? ParseAnd()
        {
            var left = ParseNot();
            while (Curr.Kind == TokKind.And)
            {
                Eat();
                var right = ParseNot();
                left = ToBoolean(left) && ToBoolean(right) ? (object?)true : false;
            }
            return left;
        }

        private object? ParseNot()
        {
            if (Curr.Kind == TokKind.Not)
            {
                Eat();
                return !ToBoolean(ParseComparison());
            }
            return ParseComparison();
        }

        private object? ParseComparison()
        {
            var left = ParseAddSub();
            while (Curr.Kind is TokKind.Eq or TokKind.NotEq or TokKind.Lt
                              or TokKind.Gt or TokKind.LtEq or TokKind.GtEq)
            {
                var op  = Eat();
                var right = ParseAddSub();
                left = Compare(left, op.Kind, right);
            }
            return left;
        }

        private object? ParseAddSub()
        {
            var left = ParseMulDiv();
            while (Curr.Kind is TokKind.Plus or TokKind.Minus)
            {
                var op    = Eat();
                var right = ParseMulDiv();

                if (op.Kind == TokKind.Plus && (left is string || right is string))
                {
                    left = $"{left}{right}";
                }
                else
                {
                    double l = ToDouble(left);
                    double r = ToDouble(right);
                    left = op.Kind == TokKind.Plus ? l + r : l - r;
                }
            }
            return left;
        }

        private object? ParseMulDiv()
        {
            var left = ParseUnary();
            while (Curr.Kind is TokKind.Star or TokKind.Slash)
            {
                var op    = Eat();
                var right = ParseUnary();
                double l  = ToDouble(left);
                double r  = ToDouble(right);
                left = op.Kind == TokKind.Star
                    ? l * r
                    : r == 0 ? throw new DivideByZeroException("Division by zero in expression") : l / r;
            }
            return left;
        }

        private object? ParseUnary()
        {
            if (Curr.Kind == TokKind.Minus)
            {
                Eat();
                return -ToDouble(ParsePrimary());
            }
            return ParsePrimary();
        }

        private object? ParsePrimary()
        {
            var t = Curr;

            switch (t.Kind)
            {
                case TokKind.Number:
                    Eat();
                    return double.Parse(t.Text, CultureInfo.InvariantCulture);

                case TokKind.String:
                    Eat();
                    return t.Text;

                case TokKind.Bool:
                    Eat();
                    return bool.Parse(t.Text);

                case TokKind.Null:
                    Eat();
                    return null;

                case TokKind.LParen:
                    Eat();
                    var inner = Parse();
                    if (Curr.Kind == TokKind.RParen) Eat();
                    return inner;

                case TokKind.Ident:
                    // Is this a function call?
                    if (Peek.Kind == TokKind.LParen)
                        return ParseFunction(t.Text.ToUpperInvariant());

                    // Otherwise it's a field reference
                    Eat();
                    return _resolver(t.Text);

                case TokKind.EOF:
                    return null;

                default:
                    throw new InvalidOperationException($"Unexpected token {t} in expression");
            }
        }

        private object? ParseFunction(string name)
        {
            Eat(); // function name
            Eat(); // '('

            var args = new List<object?>();
            while (Curr.Kind != TokKind.RParen && Curr.Kind != TokKind.EOF)
            {
                args.Add(Parse());
                if (Curr.Kind == TokKind.Comma) Eat();
            }
            if (Curr.Kind == TokKind.RParen) Eat();

            return EvalFunction(name, args);
        }

        // ── Built-in functions ─────────────────────────────────────────────

        private static object? EvalFunction(string name, List<object?> args)
        {
            string Str(int i) => args.Count > i ? args[i]?.ToString() ?? "" : "";
            double Num(int i) => args.Count > i ? ToDouble(args[i]) : 0;

            return name switch
            {
                "UPPER"  => Str(0).ToUpperInvariant(),
                "LOWER"  => Str(0).ToLowerInvariant(),
                "TRIM"   => Str(0).Trim(),
                "LEN"    => (double)Str(0).Length,
                "ROUND"  => Math.Round(Num(0), args.Count > 1 ? (int)Num(1) : 0),
                "YEAR"   => (double)ToDate(args.Count > 0 ? args[0] : null).Year,
                "MONTH"  => (double)ToDate(args.Count > 0 ? args[0] : null).Month,
                "DAY"    => (double)ToDate(args.Count > 0 ? args[0] : null).Day,
                "TODAY"  => (object?)DateTime.Today,
                "IF"     => args.Count >= 3 && ToBoolean(args[0]) ? args[1] : (args.Count >= 3 ? args[2] : null),
                "CONCAT" => string.Concat(args.ConvertAll(a => a?.ToString() ?? "")),
                "SUBSTR" => SubStr(Str(0), (int)Num(1), args.Count > 2 ? (int?)((int)Num(2)) : null),
                "ISNULL" => (object?)(args.Count > 0 && args[0] == null),
                "COALESCE" => Coalesce(args),
                _ => throw new NotSupportedException($"Unknown function '{name}'")
            };
        }

        private static string SubStr(string s, int start, int? length)
        {
            if (start < 0) start = Math.Max(0, s.Length + start);
            if (start >= s.Length) return "";
            return length.HasValue
                ? s.Substring(start, Math.Min(length.Value, s.Length - start))
                : s.Substring(start);
        }

        private static object? Coalesce(List<object?> args)
        {
            foreach (var a in args)
                if (a != null) return a;
            return null;
        }

        // ── Helpers ────────────────────────────────────────────────────────

        internal static bool ToBoolean(object? v)
        {
            if (v is null)   return false;
            if (v is bool b) return b;
            if (v is double d) return d != 0;
            string s = v.ToString()!;
            if (bool.TryParse(s, out var br)) return br;
            return !string.IsNullOrEmpty(s);
        }

        internal static double ToDouble(object? v)
        {
            if (v is double d) return d;
            if (v is null)     return 0;
            if (double.TryParse(v.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var r))
                return r;
            return 0;
        }

        private static DateTime ToDate(object? v)
        {
            if (v is DateTime dt) return dt;
            if (v is null) return DateTime.MinValue;
            if (DateTime.TryParse(v.ToString(), out var r)) return r;
            return DateTime.MinValue;
        }

        private static object? Compare(object? left, TokKind op, object? right)
        {
            // null comparisons
            if (left is null && right is null) return op is TokKind.Eq or TokKind.LtEq or TokKind.GtEq;
            if (left is null || right is null) return op == TokKind.NotEq;

            // numeric comparison
            if (left is double || right is double ||
                (double.TryParse(left.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out _) &&
                 double.TryParse(right.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out _)))
            {
                double l = ToDouble(left);
                double r = ToDouble(right);
                return op switch
                {
                    TokKind.Eq    => l == r,
                    TokKind.NotEq => l != r,
                    TokKind.Lt    => l < r,
                    TokKind.Gt    => l > r,
                    TokKind.LtEq  => l <= r,
                    TokKind.GtEq  => l >= r,
                    _             => false
                };
            }

            // date comparison
            if (left is DateTime || right is DateTime ||
                (DateTime.TryParse(left.ToString(), out _) && DateTime.TryParse(right.ToString(), out _)))
            {
                DateTime l = ToDate(left);
                DateTime r = ToDate(right);
                return op switch
                {
                    TokKind.Eq    => l == r,
                    TokKind.NotEq => l != r,
                    TokKind.Lt    => l < r,
                    TokKind.Gt    => l > r,
                    TokKind.LtEq  => l <= r,
                    TokKind.GtEq  => l >= r,
                    _             => false
                };
            }

            // string comparison (ordinal)
            int cmp = string.Compare(left.ToString(), right.ToString(), StringComparison.OrdinalIgnoreCase);
            return op switch
            {
                TokKind.Eq    => cmp == 0,
                TokKind.NotEq => cmp != 0,
                TokKind.Lt    => cmp < 0,
                TokKind.Gt    => cmp > 0,
                TokKind.LtEq  => cmp <= 0,
                TokKind.GtEq  => cmp >= 0,
                _             => false
            };
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Lightweight expression evaluator used by the pipeline's filter and computed-field transformers.
    /// Supports arithmetic and logical operators, string concatenation, and a set of built-in functions —
    /// all without any code-compilation dependency (no Roslyn required).
    /// </summary>
    /// <remarks>
    /// Supported operators: <c>+  -  *  /  ==  !=  &lt;  &gt;  &lt;=  &gt;=  AND  OR  NOT</c>.
    /// Supported functions: <c>UPPER  LOWER  TRIM  LEN  ROUND  YEAR  MONTH  DAY  TODAY  IF
    ///                        CONCAT  SUBSTR  ISNULL  COALESCE</c>.
    /// Field names that exist in the supplied <see cref="PipelineRecord"/> are resolved automatically.
    /// </remarks>
    public sealed class SimpleExpressionEvaluator
    {
        private readonly string _expression;
        private readonly IReadOnlyList<Token> _tokens;

        /// <param name="expression">The raw expression string, e.g. <c>"UPPER(FirstName) + ' ' + UPPER(LastName)"</c>.</param>
        /// <exception cref="InvalidOperationException">Thrown when the expression contains an unrecognised character.</exception>
        public SimpleExpressionEvaluator(string expression)
        {
            _expression = expression ?? throw new ArgumentNullException(nameof(expression));
            _tokens     = new Lexer(expression).Tokenize();
        }

        /// <summary>
        /// Evaluates the expression against the supplied record, returning the raw CLR value.
        /// Returns <see langword="null"/> on empty expression.
        /// </summary>
        public object? Evaluate(PipelineRecord record)
        {
            if (string.IsNullOrWhiteSpace(_expression)) return null;
            var parser = new ExprParser(_tokens, fieldName => record[fieldName]);
            return parser.Parse();
        }

        /// <summary>
        /// Evaluates the expression against a simple string-keyed dictionary (for tests or templates).
        /// </summary>
        public object? Evaluate(IReadOnlyDictionary<string, object?> vars)
        {
            if (string.IsNullOrWhiteSpace(_expression)) return null;
            var parser = new ExprParser(_tokens, field =>
                vars.TryGetValue(field, out var v) ? v : null);
            return parser.Parse();
        }

        /// <summary>
        /// Evaluates the expression and coerces the result to <see cref="bool"/>.
        /// Used by <c>FilterTransformer</c> and validator rules.
        /// </summary>
        public bool EvaluateBool(PipelineRecord record) =>
            ExprParser.ToBoolean(Evaluate(record));

        public override string ToString() => _expression;
    }
}
