using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor.Importing;
using TheTechIdea.Beep.Helpers;

namespace TheTechIdea.Beep.Editor.Defaults.Resolvers
{
    // Parse the whole shipped expression before reading fields or invoking plugins.
    internal static class RequiredExpressionEvaluator
    {
        private sealed record NumberLiteral(object Value);
        private abstract record Node(int Depth);
        private sealed record Literal(object Value) : Node(1);
        private sealed record Field(string Name) : Node(1);
        private sealed record Unary(string Op, Node Operand) : Node(Operand.Depth + 1);
        private sealed record Binary(string Op, Node Left, Node Right) : Node(Math.Max(Left.Depth, Right.Depth) + 1);
        private sealed record Call(string Name, string Source, List<Node> Args) : Node(1 + (Args.Count == 0 ? 0 : Args.Max(arg => arg.Depth)));
        private sealed record Token(string Text, int Start, int End, object Value = null, bool IsLiteral = false);

        internal static object Resolve(string rule, IDMEEditor editor, IPassedArgs parameters)
        {
            var tree = new Parser(rule).Parse();
            var value = new Evaluator(editor, parameters).Evaluate(tree, root: true);
            return value is NumberLiteral number ? ToDouble(number.Value) : value;
        }

        private static void Check()
        {
            RequiredDefaultResolution.Current?.Token.ThrowIfCancellationRequested();
            if (RequiredDefaultResolution.Current?.Failed == true) Deny();
        }

        private static void Deny() => throw new ImportTransformationException(ImportTransformationStage.Defaults);
        private static object Unwrap(object value) => value is NumberLiteral number ? number.Value : value;
        internal static bool IsNumber(object value) => Unwrap(value) is byte or sbyte or short or ushort or int or uint or long or ulong or decimal or float or double;
        private static bool IsFloat(object value) => Unwrap(value) is float or double;

        private static object Scalar(object value)
        {
            Check();
            value = value == DBNull.Value ? null : value;
            if (value == null || value is string or bool or char or Guid or DateTime or DateTimeOffset or DateOnly or TimeOnly or TimeSpan || IsNumber(value))
            {
                if (Unwrap(value) is double d && !double.IsFinite(d) || Unwrap(value) is float f && !float.IsFinite(f)) Deny();
                return value;
            }
            Deny();
            return null;
        }

        internal static double ToDouble(object value)
        {
            if (!IsNumber(value)) Deny();
            var result = Convert.ToDouble(Unwrap(value), CultureInfo.InvariantCulture);
            if (!double.IsFinite(result)) Deny();
            return result;
        }

        private static decimal ToDecimal(object value)
        {
            value = Unwrap(value);
            if (!IsNumber(value)) Deny();
            if (!IsFloat(value)) return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
            var text = value is float f ? f.ToString("R", CultureInfo.InvariantCulture) : ((double)value).ToString("R", CultureInfo.InvariantCulture);
            if (!decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ||
                NumericKey(text) != NumericKey(result.ToString("G29", CultureInfo.InvariantCulture))) Deny();
            return result;
        }

        private static string NumericKey(string text)
        {
            text = text.ToUpperInvariant();
            var negative = text.StartsWith("-", StringComparison.Ordinal);
            text = text.TrimStart('+', '-');
            var exponentAt = text.IndexOf('E');
            var exponent = 0;
            if (exponentAt >= 0)
            {
                if (!int.TryParse(text.Substring(exponentAt + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out exponent)) Deny();
                text = text.Substring(0, exponentAt);
            }
            var dot = text.IndexOf('.');
            if (dot >= 0) exponent = checked(exponent - (text.Length - dot - 1));
            var digits = text.Replace(".", "").TrimStart('0');
            if (digits.Length == 0) return "0";
            var trimmed = digits.TrimEnd('0');
            exponent = checked(exponent + digits.Length - trimmed.Length);
            return (negative ? "-" : "") + trimmed + "E" + exponent.ToString(CultureInfo.InvariantCulture);
        }

        private static NumberLiteral ParseNumber(string text)
        {
            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) || !double.IsFinite(d)) Deny();
            var key = NumericKey(text);
            if (decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var m) &&
                key == NumericKey(m.ToString("G29", CultureInfo.InvariantCulture))) return new NumberLiteral(m);
            if (key != NumericKey(d.ToString("R", CultureInfo.InvariantCulture))) Deny();
            return new NumberLiteral(d);
        }

        private sealed class Parser
        {
            private readonly string _source;
            private readonly List<Token> _tokens = new();
            private int _index;
            private int _nodes;
            private int _nesting;
            private Token Current => _tokens[_index];
            private bool At(string text) => !Current.IsLiteral && string.Equals(Current.Text, text, StringComparison.OrdinalIgnoreCase);
            private bool NextOpen => _index + 1 < _tokens.Count && _tokens[_index + 1].Text == "(";

            internal Parser(string source)
            {
                _source = source;
                for (var i = 0; i < source.Length;)
                {
                    Check();
                    if (_tokens.Count >= 4096) Deny();
                    if (char.IsWhiteSpace(source[i])) { i++; continue; }
                    var start = i;
                    var ch = source[i++];
                    if (ch is '\'' or '"')
                    {
                        while (i < source.Length && source[i] != ch) i++;
                        if (i == source.Length) Deny();
                        _tokens.Add(new Token(source.Substring(start, ++i - start), start, i, source.Substring(start + 1, i - start - 2), true));
                    }
                    else if (char.IsAsciiDigit(ch) || ch == '.' && i < source.Length && char.IsAsciiDigit(source[i]))
                    {
                        while (i < source.Length && char.IsAsciiDigit(source[i])) i++;
                        if (ch != '.' && i < source.Length && source[i] == '.')
                        {
                            i++;
                            while (i < source.Length && char.IsAsciiDigit(source[i])) i++;
                        }
                        if (i < source.Length && source[i] is 'e' or 'E')
                        {
                            i++;
                            if (i < source.Length && source[i] is '+' or '-') i++;
                            var exponent = i;
                            while (i < source.Length && char.IsAsciiDigit(source[i])) i++;
                            if (i == exponent) Deny();
                        }
                        var text = source.Substring(start, i - start);
                        _tokens.Add(new Token(text, start, i, ParseNumber(text), true));
                    }
                    else if (char.IsLetter(ch) || ch == '_')
                    {
                        while (i < source.Length && (char.IsLetterOrDigit(source[i]) || source[i] is '_' or '.')) i++;
                        var text = source.Substring(start, i - start);
                        if (text.EndsWith(".", StringComparison.Ordinal) || text.Contains("..")) Deny();
                        _tokens.Add(new Token(text, start, i));
                    }
                    else
                    {
                        var text = ch.ToString();
                        if (i < source.Length && (text + source[i]) is "==" or "!=" or "<>" or "<=" or ">=" or "&&" or "||") text += source[i++];
                        if (!new[] { "(", ")", ",", "+", "-", "*", "/", "%", "=", "==", "!=", "<>", "<", ">", "<=", ">=", "!", "&&", "||" }.Contains(text)) Deny();
                        _tokens.Add(new Token(text, start, i));
                    }
                }
                _tokens.Add(new Token("", source.Length, source.Length));
            }

            private T Make<T>(T node) where T : Node
            {
                Check();
                if (++_nodes > 1024 || node.Depth > 32) Deny();
                return node;
            }

            internal Node Parse()
            {
                var node = Or();
                if (!At("")) Deny();
                return node;
            }

            private Node Or() => Chain(And, "OR", "||");
            private Node And() => Chain(Not, "AND", "&&");
            private Node Add() => Chain(Multiply, "+", "-");
            private Node Multiply() => Chain(Sign, "*", "/", "%");
            private Node Chain(Func<Node> operand, params string[] operators)
            {
                var left = operand();
                while (operators.Any(At))
                {
                    var op = Current.Text.ToUpperInvariant(); _index++;
                    left = Make(new Binary(op, left, operand()));
                }
                return left;
            }

            private Node Not()
            {
                if ((At("NOT") && !NextOpen) || At("!")) return Prefix(Not);
                return Comparison();
            }

            private Node Comparison()
            {
                var left = Add();
                if (!new[] { "=", "==", "!=", "<>", "<", ">", "<=", ">=" }.Any(At)) return left;
                var op = Current.Text; _index++;
                return Make(new Binary(op, left, Add()));
            }

            private Node Sign() => At("+") || At("-") ? Prefix(Sign) : Primary();
            private Node Prefix(Func<Node> operand)
            {
                if (++_nesting > 32) Deny();
                var op = Current.Text.ToUpperInvariant(); _index++;
                var node = Make(new Unary(op, operand()));
                _nesting--;
                return node;
            }

            private Node Primary()
            {
                Check();
                var token = Current;
                if (token.IsLiteral) { _index++; return Make(new Literal(token.Value)); }
                if (At("("))
                {
                    if (++_nesting > 32) Deny();
                    _index++; var expression = Or(); Require(")"); _nesting--;
                    return expression;
                }
                if (token.Text.Length == 0 || !(char.IsLetter(token.Text[0]) || token.Text[0] == '_')) Deny();
                _index++;
                if (At("("))
                {
                    if (++_nesting > 32) Deny();
                    _index++;
                    var args = new List<Node>();
                    if (!At(")"))
                    {
                        args.Add(Or());
                        while (At(",")) { _index++; args.Add(Or()); }
                    }
                    var end = Current.End; Require(")"); _nesting--;
                    return Make(new Call(token.Text.ToUpperInvariant(), _source.Substring(token.Start, end - token.Start), args));
                }
                if (string.Equals(token.Text, "null", StringComparison.OrdinalIgnoreCase)) return Make(new Literal(null));
                if (bool.TryParse(token.Text, out var boolean)) return Make(new Literal(boolean));
                return Make(new Field(token.Text));
            }

            private void Require(string token) { if (!At(token)) Deny(); _index++; }
        }

        private sealed class Evaluator
        {
            private readonly IDMEEditor _editor;
            private readonly IPassedArgs _parameters;
            internal Evaluator(IDMEEditor editor, IPassedArgs parameters) { _editor = editor; _parameters = parameters; }

            internal object Evaluate(Node node, bool root = false)
            {
                Check();
                return node switch
                {
                    Literal literal => literal.Value,
                    Field field => Scalar(RecordFieldAccess.ReadRequired(_parameters?.ReturnData, field.Name)),
                    Unary unary => UnaryValue(unary),
                    Binary binary => BinaryValue(binary),
                    Call call => CallValue(call, root),
                    _ => throw new ImportTransformationException(ImportTransformationStage.Defaults)
                };
            }

            private static bool Boolean(object value)
            {
                if (value is bool result) return result;
                Deny(); return false;
            }

            private object UnaryValue(Unary node)
            {
                var value = Evaluate(node.Operand);
                if (node.Op is "NOT" or "!") return !Boolean(value);
                if (!IsNumber(value)) Deny();
                return IsFloat(value) ? new NumberLiteral(node.Op == "-" ? -ToDouble(value) : ToDouble(value)) :
                    new NumberLiteral(node.Op == "-" ? checked(-ToDecimal(value)) : ToDecimal(value));
            }

            private object BinaryValue(Binary node)
            {
                var left = Evaluate(node.Left);
                if (node.Op is "AND" or "&&") return Boolean(left) && Boolean(Evaluate(node.Right));
                if (node.Op is "OR" or "||") return Boolean(left) || Boolean(Evaluate(node.Right));
                var right = Evaluate(node.Right);
                if (node.Op is "+" or "-" or "*" or "/" or "%") return Arithmetic(node.Op, left, right);
                return Compare(node.Op, left, right);
            }

            private object CallValue(Call call, bool root)
            {
                if (!root)
                {
                    var registry = DefaultsManager.GetRequiredResolverRegistry(_editor);
                    var resolver = registry.Select(call.Source);
                    if (resolver == null) Deny();
                    if (resolver.GetType() != typeof(ExpressionResolver) && resolver.GetType() != typeof(FormulaResolver))
                        return Scalar(registry.Resolve(":" + call.Source, _parameters, RequiredDefaultResolution.Current.Token));
                }
                var args = call.Args;
                void Count(int min, int max) { if (args.Count < min || args.Count > max) Deny(); }
                switch (call.Name)
                {
                    case "EXPRESSION": case "EVAL": Count(1, 1); return Evaluate(args[0]);
                    case "CALCULATE": case "COMPUTE": Count(1, 1); return new NumberLiteral(Unwrap(Numeric(args[0])));
                    case "IF": case "CONDITIONAL": case "TERNARY":
                        Count(call.Name == "TERNARY" ? 3 : 2, 3);
                        return Boolean(Evaluate(args[0])) ? Evaluate(args[1]) : args.Count == 3 ? Evaluate(args[2]) : null;
                    case "ISNULL": Count(2, 2); return Evaluate(args[0]) ?? Evaluate(args[1]);
                    case "COALESCE":
                        Count(2, int.MaxValue);
                        foreach (var arg in args) { var value = Evaluate(arg); if (value != null && value is not string { Length: 0 }) return value; }
                        return null;
                    case "CASE":
                        Count(3, int.MaxValue); var test = Evaluate(args[0]);
                        var limit = args.Count % 2 == 0 ? args.Count - 1 : args.Count;
                        for (var i = 1; i + 1 < limit; i += 2) if (Compare("=", test, Evaluate(args[i]))) return Evaluate(args[i + 1]);
                        return args.Count % 2 == 0 ? Evaluate(args[args.Count - 1]) : null;
                    case "AND": case "OR":
                        Count(2, int.MaxValue);
                        foreach (var arg in args) { var value = Boolean(Evaluate(arg)); if (value == (call.Name == "OR")) return value; }
                        return call.Name == "AND";
                    case "NOT": Count(1, 1); return !Boolean(Evaluate(args[0]));
                    case "EQ": case "NE": case "GT": case "GTE": case "LT": case "LTE":
                        Count(2, 2); return Compare(call.Name, Evaluate(args[0]), Evaluate(args[1]));
                    case "ADD": case "SUBTRACT": case "SUB": case "MULTIPLY": case "MUL": case "DIVIDE": case "DIV":
                        Count(2, 2); return Arithmetic(call.Name, Numeric(args[0]), Numeric(args[1]));
                    case "ROUND":
                        Count(1, 2); var decimals = args.Count == 1 ? 0 : Integer(args[1]);
                        if (decimals < 0 || decimals > 15) Deny();
                        var rounded = Numeric(args[0]);
                        return IsFloat(rounded) ? Math.Round(ToDouble(rounded), decimals, MidpointRounding.AwayFromZero) :
                            new NumberLiteral(decimal.Round(ToDecimal(rounded), decimals, MidpointRounding.AwayFromZero));
                    case "RANDOM": case "RANDOMVALUE":
                        Count(0, 2); var min = args.Count == 2 ? Integer(args[0]) : args.Count == 0 ? 1 : 0;
                        var max = args.Count == 0 ? 99 : Integer(args[args.Count - 1]);
                        if (min > max) Deny();
                        return (int)Random.Shared.NextInt64(min, (long)max + 1);
                    case "MATH":
                        Count(1, 2);
                        var name = args[0] is Field field ? field.Name : args[0] is Literal { Value: string text } ? text : "";
                        name = name.ToUpperInvariant();
                        if (name is "PI" or "E") { Count(1, 1); return name == "PI" ? Math.PI : Math.E; }
                        Count(2, 2); var operand = Numeric(args[1]);
                        if (name == "ROUND" && !IsFloat(operand)) return new NumberLiteral(decimal.Round(ToDecimal(operand)));
                        var number = ToDouble(operand);
                        return Scalar(name switch
                        {
                            "SQRT" => Math.Sqrt(number), "ABS" => Math.Abs(number), "ROUND" => Math.Round(number),
                            "FLOOR" => Math.Floor(number), "CEILING" => Math.Ceiling(number),
                            "SIN" => Math.Sin(number), "COS" => Math.Cos(number), "TAN" => Math.Tan(number),
                            _ => throw new ImportTransformationException(ImportTransformationStage.Defaults)
                        });
                    default: Deny(); return null;
                }
            }

            private object Numeric(Node node)
            {
                var value = Evaluate(node);
                // Quoted numeric operands are a shipped math-call convenience, never implicit expression truthiness/coercion.
                if (node is Literal && value is string text) value = ParseNumber(text);
                if (!IsNumber(value)) Deny();
                return value;
            }

            private int Integer(Node node)
            {
                var value = ToDecimal(Numeric(node));
                if (value != decimal.Truncate(value) || value < int.MinValue || value > int.MaxValue) Deny();
                return (int)value;
            }
        }

        internal static object Arithmetic(string op, object left, object right)
        {
            if (!IsNumber(left) || !IsNumber(right)) Deny();
            op = op switch { "ADD" => "+", "SUBTRACT" or "SUB" => "-", "MULTIPLY" or "MUL" => "*", "DIVIDE" or "DIV" => "/", _ => op };
            if (IsFloat(left) || IsFloat(right))
            {
                var a = ToDouble(left); var b = ToDouble(right);
                if (b == 0 && op is "/" or "%") Deny();
                return Scalar(op switch { "+" => a + b, "-" => a - b, "*" => a * b, "/" => a / b, "%" => a % b, _ => throw new InvalidOperationException() });
            }
            var x = ToDecimal(left); var y = ToDecimal(right);
            if (y == 0 && op is "/" or "%") Deny();
            return new NumberLiteral(op switch { "+" => checked(x + y), "-" => checked(x - y), "*" => checked(x * y), "/" => x / y, "%" => x % y, _ => throw new InvalidOperationException() });
        }

        internal static bool Compare(string op, object left, object right)
        {
            left = Unwrap(Scalar(left)); right = Unwrap(Scalar(right));
            op = op switch { "EQ" or "==" => "=", "NE" or "<>" => "!=", "GT" => ">", "GTE" => ">=", "LT" => "<", "LTE" => "<=", _ => op };
            if (left == null || right == null)
            {
                if (op is not ("=" or "!=")) Deny();
                return (left == null && right == null) == (op == "=");
            }
            int comparison;
            if (IsNumber(left) && IsNumber(right))
                comparison = IsFloat(left) && IsFloat(right) ? ToDouble(left).CompareTo(ToDouble(right)) : ToDecimal(left).CompareTo(ToDecimal(right));
            else
            {
                if (left.GetType() != right.GetType()) Deny();
                if (left is bool or Guid && op is not ("=" or "!=")) Deny();
                comparison = left switch
                {
                    string text => StringComparer.OrdinalIgnoreCase.Compare(text, (string)right),
                    bool boolean => boolean.CompareTo((bool)right), char character => character.CompareTo((char)right),
                    Guid guid => guid == (Guid)right ? 0 : 1, DateTime date => date.CompareTo((DateTime)right),
                    DateTimeOffset date => date.CompareTo((DateTimeOffset)right), DateOnly date => date.CompareTo((DateOnly)right),
                    TimeOnly time => time.CompareTo((TimeOnly)right), TimeSpan time => time.CompareTo((TimeSpan)right),
                    _ => throw new ImportTransformationException(ImportTransformationStage.Defaults)
                };
            }
            return op switch { "=" => comparison == 0, "!=" => comparison != 0, ">" => comparison > 0, "<" => comparison < 0, ">=" => comparison >= 0, "<=" => comparison <= 0, _ => throw new InvalidOperationException() };
        }
    }
}
