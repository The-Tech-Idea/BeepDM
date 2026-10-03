using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace TheTechIdea.Beep.Rules.BuiltinParsers
{
    // Admission and execution share an owned tree, never the legacy RPN path.
    internal sealed class NfelExpression
    {
        internal const int MaxSourceLength = 65536, MaxTokens = 4096, MaxNodes = 2048,
            MaxDepth = 64, MaxStringLength = 16384;
        private readonly List<Token> _tokens;
        private readonly Node _root;
        private NfelExpression(List<Token> tokens)
        {
            _tokens = tokens;
            _root = new Grammar(tokens).Parse();
        }
        internal static NfelExpression Parse(string source)
        {
            if (source == null || source.Length == 0) throw Syntax(DiagnosticCode.EmptyExpression, 0, 0);
            if (source.Length > MaxSourceLength) throw Syntax(DiagnosticCode.PolicyViolation, 0, 0);
            return new NfelExpression(new Lexer(source).Read());
        }
        internal static NfelExpression FromTokens(IList<Token> source)
        {
            try
            {
                if (source == null) throw Syntax(DiagnosticCode.EmptyExpression, 0, 0);
                int count = source.Count;
                if (count < 1 || count > MaxTokens) throw Syntax(DiagnosticCode.PolicyViolation, 0, 0);
                var owned = new List<Token>(count);
                int characters = 0;
                for (int i = 0; i < count; i++)
                {
                    var token = source[i];
                    if (token == null) throw Syntax(DiagnosticCode.UnexpectedToken, 0, 0);
                    var copy = Copy(token);
                    if (copy.Value == null || copy.Value.Length > MaxStringLength)
                        throw Syntax(DiagnosticCode.UnexpectedToken, 0, 0);
                    characters = checked(characters + copy.Value.Length);
                    if (characters > MaxSourceLength) throw Syntax(DiagnosticCode.PolicyViolation, 0, 0);
                    ValidateToken(copy);
                    owned.Add(copy);
                }
                if (source.Count != count) throw Syntax(DiagnosticCode.UnexpectedToken, 0, 0);
                return new NfelExpression(owned);
            }
            catch (NfelSyntaxException) { throw; }
            catch (Exception) { throw Syntax(DiagnosticCode.UnexpectedToken, 0, 0); }
        }
        internal List<Token> CopyTokens() => _tokens.ConvertAll(Copy);
        internal bool Matches(IList<Token> tokens)
        {
            var candidate = FromTokens(tokens);
            if (candidate._tokens.Count != _tokens.Count) return false;
            for (int i = 0; i < _tokens.Count; i++)
                if (_tokens[i].Type != candidate._tokens[i].Type ||
                    !string.Equals(_tokens[i].Value, candidate._tokens[i].Value, StringComparison.Ordinal)) return false;
            return true;
        }
        internal object Evaluate(Dictionary<string, object> parameters, RuleExecutionPolicy policy, Stopwatch clock)
        {
            var budget = new Budget(policy, clock);
            foreach (var token in _tokens)
                if (!budget.Allows(token.Type)) throw Fault(DiagnosticCode.OperatorNotAllowed);
            var result = Evaluate(_root, parameters, budget);
            budget.Check();
            return result;
        }
        private static object Evaluate(Node node, Dictionary<string, object> parameters, Budget budget)
        {
            budget.Check();
            if (node.Type == TokenType.Identifier)
            {
                object value;
                bool found;
                try
                {
                    value = null;
                    found = parameters != null && parameters.TryGetValue(node.Name, out value);
                }
                catch (Exception) { throw Fault(DiagnosticCode.PolicyViolation); }
                budget.Check();
                if (!found) throw Fault(DiagnosticCode.IdentifierNotFound);
                return ClosedValue(value);
            }
            if (node.Left == null) return node.Value;
            var left = Evaluate(node.Left, parameters, budget);
            if (node.Type == TokenType.Question)
                return Evaluate(Boolean(left) ? node.Right : node.Third, parameters, budget);
            if (node.Type == TokenType.And && !Boolean(left)) return false;
            if (node.Type == TokenType.Or && Boolean(left)) return true;
            if (node.Right == null)
                return node.Type == TokenType.Not ? !Boolean(left) :
                    Finite(node.Type == TokenType.Minus ? -Number(left) : Number(left));
            var right = Evaluate(node.Right, parameters, budget);
            budget.Check();
            if (node.Type == TokenType.And || node.Type == TokenType.Or) return Boolean(right);
            if (node.Type == TokenType.Plus && left is string ls && right is string rs)
            {
                if (ls.Length > MaxStringLength - rs.Length) throw Fault(DiagnosticCode.PolicyViolation);
                return ls + rs;
            }
            if (node.Type is TokenType.Equal or TokenType.NotEqual or TokenType.LessThan or
                TokenType.LessEqual or TokenType.GreaterThan or TokenType.GreaterEqual)
            {
                int comparison;
                if (left == null || right == null)
                {
                    if (node.Type != TokenType.Equal && node.Type != TokenType.NotEqual) throw Fault(DiagnosticCode.TypeCoercionFailed);
                    comparison = left == null && right == null ? 0 : 1;
                }
                else if (left is string a && right is string b) comparison = string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
                else if (left is bool x && right is bool y && node.Type is TokenType.Equal or TokenType.NotEqual) comparison = x.CompareTo(y);
                else comparison = CompareNumbers(left, right);
                return node.Type switch
                {
                    TokenType.Equal => comparison == 0, TokenType.NotEqual => comparison != 0,
                    TokenType.LessThan => comparison < 0, TokenType.LessEqual => comparison <= 0,
                    TokenType.GreaterThan => comparison > 0, _ => comparison >= 0
                };
            }
            double l = Number(left), r = Number(right);
            if ((node.Type == TokenType.Divide || node.Type == TokenType.Modulo) && r == 0) throw Fault(DiagnosticCode.DivisionByZero);
            return Finite(node.Type switch
            {
                TokenType.Plus => l + r, TokenType.Minus => l - r, TokenType.Multiply => l * r,
                TokenType.Divide => l / r, TokenType.Modulo => l % r, TokenType.Power => Math.Pow(l, r),
                _ => throw Fault(DiagnosticCode.UnsupportedOperator)
            });
        }
        private static object ClosedValue(object value)
        {
            if (value == null || value is bool) return value;
            if (value is string text)
            {
                if (text.Length > MaxStringLength) throw Fault(DiagnosticCode.PolicyViolation);
                return text;
            }
            Number(value);
            return value;
        }
        private static bool Boolean(object value) => value is bool b ? b : throw Fault(DiagnosticCode.TypeCoercionFailed);
        private static double Number(object value) => Finite(value switch
        {
            byte n => n, sbyte n => n, short n => n, ushort n => n, int n => n, uint n => n,
            long n => n, ulong n => n, float n => n, double n => n, decimal n => (double)n,
            _ => throw Fault(DiagnosticCode.TypeCoercionFailed)
        });
        private static int CompareNumbers(object left, object right)
        {
            Number(left); Number(right);
            // Integral/Decimal parameter comparisons stay exact; floating values use binary64.
            if (left is not float and not double && right is not float and not double)
                return Convert.ToDecimal(left, CultureInfo.InvariantCulture).CompareTo(Convert.ToDecimal(right, CultureInfo.InvariantCulture));
            return Number(left).CompareTo(Number(right));
        }
        private static double Finite(double value) => double.IsFinite(value) ? value : throw Fault(DiagnosticCode.InvalidNumeric);
        private static RuleEvaluationException Fault(DiagnosticCode code) => new(code, "NFEL evaluation denied: " + code + ".");
        private sealed class Budget
        {
            private readonly int _timeout;
            private readonly HashSet<TokenType> _allowed;
            private readonly Stopwatch _clock;
            internal Budget(RuleExecutionPolicy policy, Stopwatch clock)
            {
                _timeout = policy.MaxExecutionMs;
                _allowed = policy.AllowedTokenTypes;
                _clock = clock;
                Check();
            }
            internal bool Allows(TokenType type) => _allowed == null || _allowed.Contains(type);
            internal void Check()
            {
                if (_timeout > 0 && _clock.Elapsed.TotalMilliseconds >= _timeout) throw Fault(DiagnosticCode.TimeoutExceeded);
            }
        }
        private static void ValidateToken(Token token)
        {
            if (token.Start < 0 || token.Length < 0 || token.Start > MaxSourceLength ||
                token.Length > MaxSourceLength - token.Start ||
                !string.Equals(token.SchemaVersion, RuleStructure.CurrentSchemaVersion, StringComparison.Ordinal))
                throw Syntax(DiagnosticCode.UnexpectedToken, 0, 0);
            bool valid = token.Type switch
            {
                TokenType.StringLiteral => true, TokenType.NumericLiteral => IsNumber(token.Value),
                TokenType.Identifier => IsIdentifier(token.Value),
                TokenType.BooleanLiteral => token.Value.Equals("true", StringComparison.OrdinalIgnoreCase) || token.Value.Equals("false", StringComparison.OrdinalIgnoreCase),
                TokenType.NullLiteral => token.Value.Equals("null", StringComparison.OrdinalIgnoreCase),
                TokenType.And => token.Value == "&&" || token.Value.Equals("AND", StringComparison.OrdinalIgnoreCase),
                TokenType.Or => token.Value == "||" || token.Value.Equals("OR", StringComparison.OrdinalIgnoreCase),
                TokenType.Not => token.Value == "!" || token.Value.Equals("NOT", StringComparison.OrdinalIgnoreCase),
                _ => Operator(token.Value) == token.Type
            };
            if (!valid) throw Syntax(DiagnosticCode.UnexpectedToken, token.Start, token.Length);
        }
        private static bool IsNumber(string value)
        {
            if (value.Length < 1 || value.Length > 128) return false;
            int index = 0;
            ScanNumber(value, ref index);
            return index == value.Length && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) && double.IsFinite(number);
        }
        private static void ScanNumber(string source, ref int index)
        {
            int initial = index;
            while (index < source.Length && Digit(source[index])) index++;
            bool digits = index > initial;
            if (index < source.Length && source[index] == '.')
            {
                index++;
                int fraction = index;
                while (index < source.Length && Digit(source[index])) index++;
                if (index == fraction) { index = initial; return; }
                digits = true;
            }
            if (!digits) { index = initial; return; }
            if (index < source.Length && (source[index] == 'e' || source[index] == 'E'))
            {
                index++;
                if (index < source.Length && (source[index] == '+' || source[index] == '-')) index++;
                int exponent = index;
                while (index < source.Length && Digit(source[index])) index++;
                if (index == exponent) index = initial;
            }
        }
        private static bool IsIdentifier(string text)
        {
            if (text.Length < 1 || text.Length > 256) return false;
            bool first = true;
            foreach (char c in text)
            {
                if (c == '.') { if (first) return false; first = true; }
                else if (first) { if (!Letter(c)) return false; first = false; }
                else if (!Letter(c) && !Digit(c)) return false;
            }
            return !first && !text.Equals("true", StringComparison.OrdinalIgnoreCase) && !text.Equals("false", StringComparison.OrdinalIgnoreCase) &&
                !text.Equals("null", StringComparison.OrdinalIgnoreCase) && !text.Equals("AND", StringComparison.OrdinalIgnoreCase) &&
                !text.Equals("OR", StringComparison.OrdinalIgnoreCase) && !text.Equals("NOT", StringComparison.OrdinalIgnoreCase);
        }
        private static bool Digit(char c) => c >= '0' && c <= '9';
        private static bool Letter(char c) => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or '_';
        private static Token Copy(Token token) => new(token.Type, token.Value, token.Start, token.Length) { SchemaVersion = token.SchemaVersion };
        internal static NfelSyntaxException Syntax(DiagnosticCode code, int start, int length) => new(code, Math.Max(0, start), Math.Max(0, length));
        private static TokenType? Operator(string value) => value switch
        {
            "+" => TokenType.Plus, "-" => TokenType.Minus, "*" => TokenType.Multiply, "/" => TokenType.Divide, "%" => TokenType.Modulo,
            "^" => TokenType.Power, "==" => TokenType.Equal, "!=" => TokenType.NotEqual, "<" => TokenType.LessThan, ">" => TokenType.GreaterThan,
            "<=" => TokenType.LessEqual, ">=" => TokenType.GreaterEqual, "&&" => TokenType.And, "||" => TokenType.Or, "!" => TokenType.Not,
            "(" => TokenType.LeftParenthesis, ")" => TokenType.RightParenthesis, "?" => TokenType.Question, ":" => TokenType.Colon, _ => null
        };
        private sealed class Lexer
        {
            private readonly string _source;
            private int _position;
            internal Lexer(string source) => _source = source;
            internal List<Token> Read()
            {
                var tokens = new List<Token>();
                while (_position < _source.Length)
                {
                    if (char.IsWhiteSpace(_source[_position])) { _position++; continue; }
                    if (tokens.Count >= MaxTokens) throw Syntax(DiagnosticCode.PolicyViolation, _position, 0);
                    int start = _position;
                    char c = _source[_position];
                    TokenType type;
                    string value;
                    if (c == '\'' || c == '"') { type = TokenType.StringLiteral; value = ReadString(); }
                    else if (Digit(c) || c == '.')
                    {
                        ScanNumber(_source, ref _position);
                        value = _source.Substring(start, _position - start);
                        if (!IsNumber(value)) throw Syntax(DiagnosticCode.InvalidNumeric, start, Math.Max(1, _position - start));
                        type = TokenType.NumericLiteral;
                    }
                    else if (Letter(c))
                    {
                        while (_position < _source.Length && (Letter(_source[_position]) || Digit(_source[_position]) || _source[_position] == '.')) _position++;
                        value = _source.Substring(start, _position - start);
                        type = value.ToUpperInvariant() switch
                        {
                            "TRUE" or "FALSE" => TokenType.BooleanLiteral, "NULL" => TokenType.NullLiteral,
                            "AND" => TokenType.And, "OR" => TokenType.Or, "NOT" => TokenType.Not, _ => TokenType.Identifier
                        };
                        if (type == TokenType.Identifier && !IsIdentifier(value)) throw Syntax(DiagnosticCode.UnexpectedToken, start, _position - start);
                    }
                    else
                    {
                        value = _position + 1 < _source.Length ? _source.Substring(_position, 2) : "";
                        var op = Operator(value);
                        if (op == null) { value = c.ToString(); op = Operator(value); }
                        if (op == null) throw Syntax(DiagnosticCode.UnknownToken, start, 1);
                        type = op.Value;
                        _position += value.Length;
                    }
                    tokens.Add(new Token(type, value, start, _position - start));
                }
                if (tokens.Count == 0) throw Syntax(DiagnosticCode.EmptyExpression, 0, 0);
                return tokens;
            }
            private string ReadString()
            {
                int start = _position;
                char quote = _source[_position++];
                var value = new StringBuilder();
                while (_position < _source.Length)
                {
                    char c = _source[_position++];
                    if (c == quote) return value.ToString();
                    if (c == '\\')
                    {
                        if (_position == _source.Length) break;
                        c = _source[_position++];
                        if (c == 'u')
                        {
                            if (_position > _source.Length - 4 || !ushort.TryParse(_source.AsSpan(_position, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var code))
                                throw Syntax(DiagnosticCode.UnexpectedToken, _position - 2, 2);
                            c = (char)code;
                            _position += 4;
                        }
                        else c = c switch
                        {
                            '\\' => '\\', '\'' => '\'', '"' => '"', 'n' => '\n', 'r' => '\r', 't' => '\t', 'b' => '\b', 'f' => '\f',
                            _ => throw Syntax(DiagnosticCode.UnexpectedToken, _position - 2, 2)
                        };
                    }
                    else if (char.IsControl(c)) throw Syntax(DiagnosticCode.UnexpectedToken, _position - 1, 1);
                    if (value.Length >= MaxStringLength) throw Syntax(DiagnosticCode.PolicyViolation, start, _position - start);
                    value.Append(c);
                }
                throw Syntax(DiagnosticCode.UnterminatedString, start, _position - start);
            }
        }
        private sealed class Node
        {
            internal readonly TokenType Type;
            internal readonly object Value;
            internal readonly string Name;
            internal readonly Node Left, Right, Third;
            internal readonly int Height;
            internal Node(Token token, Node left = null, Node right = null, Node third = null)
            {
                Type = token.Type; Left = left; Right = right; Third = third;
                Height = 1 + Math.Max(left?.Height ?? 0, Math.Max(right?.Height ?? 0, third?.Height ?? 0));
                if (Height > MaxDepth) throw Syntax(DiagnosticCode.MaxDepthExceeded, token.Start, token.Length);
                Name = token.Value;
                Value = Type switch
                {
                    TokenType.NumericLiteral => double.Parse(token.Value, NumberStyles.Float, CultureInfo.InvariantCulture),
                    TokenType.BooleanLiteral => token.Value.Equals("true", StringComparison.OrdinalIgnoreCase),
                    TokenType.StringLiteral => token.Value, _ => null
                };
            }
        }
        private sealed class Grammar
        {
            private readonly List<Token> _tokens;
            private int _position, _nodes;
            internal Grammar(List<Token> tokens) => _tokens = tokens;
            internal Node Parse()
            {
                var node = Conditional(1);
                if (_position != _tokens.Count) throw Error(DiagnosticCode.UnexpectedToken);
                return node;
            }
            private Node Conditional(int depth)
            {
                CheckDepth(depth);
                var condition = Binary(1, depth);
                if (!Take(TokenType.Question, out var question)) return condition;
                var yes = Conditional(depth + 1);
                if (!Take(TokenType.Colon, out _)) throw Error(DiagnosticCode.UnexpectedToken);
                return Make(question, condition, yes, Conditional(depth + 1));
            }
            private Node Binary(int minimum, int depth)
            {
                CheckDepth(depth);
                var left = Prefix(depth);
                while (_position < _tokens.Count)
                {
                    var op = _tokens[_position];
                    int precedence = Precedence(op.Type);
                    if (precedence < minimum) break;
                    _position++;
                    var right = Binary(precedence + (op.Type == TokenType.Power ? 0 : 1), depth + 1);
                    left = Make(op, left, right);
                }
                return left;
            }
            private Node Prefix(int depth)
            {
                CheckDepth(depth);
                if (_position >= _tokens.Count) throw Error(DiagnosticCode.UnexpectedToken);
                var token = _tokens[_position++];
                if (token.Type is TokenType.Not or TokenType.Plus or TokenType.Minus) return Make(token, Binary(6, depth + 1));
                if (token.Type == TokenType.LeftParenthesis)
                {
                    var grouped = Conditional(depth + 1);
                    if (!Take(TokenType.RightParenthesis, out _)) throw Error(DiagnosticCode.MismatchedParenthesis);
                    return grouped;
                }
                if (token.Type is not (TokenType.Identifier or TokenType.NumericLiteral or TokenType.StringLiteral or TokenType.BooleanLiteral or TokenType.NullLiteral))
                    throw Syntax(DiagnosticCode.UnexpectedToken, token.Start, token.Length);
                return Make(token);
            }
            private Node Make(Token token, Node left = null, Node right = null, Node third = null)
            {
                if (++_nodes > MaxNodes) throw Syntax(DiagnosticCode.PolicyViolation, token.Start, token.Length);
                return new Node(token, left, right, third);
            }
            private bool Take(TokenType type, out Token token)
            {
                token = _position < _tokens.Count ? _tokens[_position] : null;
                if (token?.Type != type) return false;
                _position++;
                return true;
            }
            private NfelSyntaxException Error(DiagnosticCode code) => _position < _tokens.Count
                ? Syntax(code, _tokens[_position].Start, _tokens[_position].Length)
                : Syntax(code, _tokens.Count == 0 ? 0 : _tokens[_tokens.Count - 1].Start + _tokens[_tokens.Count - 1].Length, 0);
            private static void CheckDepth(int depth)
            {
                if (depth > MaxDepth) throw Syntax(DiagnosticCode.MaxDepthExceeded, 0, 0);
            }
            private static int Precedence(TokenType type) => type switch
            {
                TokenType.Or => 1, TokenType.And => 2,
                TokenType.Equal or TokenType.NotEqual or TokenType.LessThan or TokenType.LessEqual or TokenType.GreaterThan or TokenType.GreaterEqual => 3,
                TokenType.Plus or TokenType.Minus => 4,
                TokenType.Multiply or TokenType.Divide or TokenType.Modulo => 5,
                TokenType.Power => 7, _ => 0
            };
        }
    }
    internal sealed class NfelSyntaxException : Exception
    {
        internal ParseDiagnostic Diagnostic { get; }
        internal NfelSyntaxException(DiagnosticCode code, int start, int length) : base("NFEL expression denied: " + code + ".")
        {
            Diagnostic = new ParseDiagnostic { Code = code, Severity = DiagnosticSeverity.Error, Start = start, Length = length, Message = Message };
        }
    }
}
