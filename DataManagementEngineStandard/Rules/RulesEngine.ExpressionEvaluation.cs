using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace TheTechIdea.Beep.Rules
{
    public partial class RuleEngine
    {
        // ── Shunting-yard: infix → RPN ────────────────────────────────────────────

        private static List<Token> ConvertToRpn(IList<Token> tokens)
        {
            var output = new List<Token>();
            var opStack = new Stack<Token>();

            foreach (var token in tokens ?? (IList<Token>)Array.Empty<Token>())
            {
                if (IsOperand(token))  { output.Add(token); continue; }

                if (IsOperator(token))
                {
                    while (opStack.Count > 0 && IsOperator(opStack.Peek()) &&
                           ((IsLeftAssociative(token) && Precedence(token) <= Precedence(opStack.Peek())) ||
                            (!IsLeftAssociative(token) && Precedence(token)  < Precedence(opStack.Peek()))))
                        output.Add(opStack.Pop());
                    opStack.Push(token);
                    continue;
                }

                if (token.Type == TokenType.LeftParenthesis)  { opStack.Push(token); continue; }

                if (token.Type == TokenType.RightParenthesis)
                {
                    while (opStack.Count > 0 && opStack.Peek().Type != TokenType.LeftParenthesis)
                        output.Add(opStack.Pop());
                    if (opStack.Count > 0) opStack.Pop(); // discard '('
                    continue;
                }
            }

            while (opStack.Count > 0) output.Add(opStack.Pop());
            return output;
        }

        // ── RPN evaluator with coercion matrix ───────────────────────────────────

        private object EvaluateRpn(List<Token> rpn, Dictionary<string, object> parameters,
            RuleExecutionPolicy policy, HashSet<string> callChain, int depth)
        {
            var stack = new Stack<object>();

            foreach (var token in rpn ?? new List<Token>())
            {
                if (IsOperand(token))
                {
                    stack.Push(ResolveOperand(token, parameters, policy, callChain, depth));
                    continue;
                }

                if (!IsOperator(token)) continue;

                if (token.Type == TokenType.Not)
                {
                    if (stack.Count < 1) Fault(token, "NOT requires one operand.");
                    stack.Push(ApplyNot(stack.Pop()));
                    continue;
                }

                if (stack.Count < 2) Fault(token, $"Operator '{token.Value}' requires two operands.");
                var right = stack.Pop();
                var left  = stack.Pop();
                stack.Push(Apply(token, left, right));
            }

            if (stack.Count != 1)
                throw new RuleEvaluationException(DiagnosticCode.UnsupportedOperator,
                    "Expression evaluation ended with an invalid stack state.");

            return stack.Pop();
        }

        // ── Operand resolution ────────────────────────────────────────────────────

        private object ResolveOperand(Token token, Dictionary<string, object> parameters,
            RuleExecutionPolicy policy, HashSet<string> callChain, int depth)
        {
            switch (token.Type)
            {
                case TokenType.NumericLiteral:
                    if (double.TryParse(token.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var num))
                        return num;
                    throw new RuleEvaluationException(DiagnosticCode.TypeCoercionFailed,
                        $"Cannot parse numeric literal '{token.Value}'.");

                case TokenType.StringLiteral:
                    return token.Value;

                case TokenType.BooleanLiteral:
                    return token.Value.Equals("true", StringComparison.OrdinalIgnoreCase);

                case TokenType.NullLiteral:
                    return null;

                case TokenType.EntityField:
                    if (parameters != null && parameters.TryGetValue(token.Value, out var fieldVal))
                        return fieldVal;
                    throw new RuleEvaluationException(DiagnosticCode.EntityFieldNotFound,
                        $"Entity field '{token.Value}' not found in parameters.");

                case TokenType.Identifier:
                    if (parameters != null && parameters.TryGetValue(token.Value, out var idVal))
                        return idVal;
                    throw new RuleEvaluationException(DiagnosticCode.IdentifierNotFound,
                        $"Identifier '{token.Value}' not found in parameters.");

                case TokenType.RuleReference:
                    var (_, refResult) = SolveRuleInternal(token.Value, parameters, policy, callChain, depth);
                    return refResult;

                default:
                    return token.Value;
            }
        }

        // ── Operator dispatch ─────────────────────────────────────────────────────

        private static object Apply(Token op, object left, object right)
        {
            // Null propagation: any arithmetic/comparison with null returns null
            if (left == null || right == null)
            {
                return op.Type switch
                {
                    TokenType.Equal    => left == null && right == null,
                    TokenType.NotEqual => !(left == null && right == null),
                    _                  => null
                };
            }

            // String operations
            if (op.Type == TokenType.Plus && (left is string || right is string))
                return Convert.ToString(left) + Convert.ToString(right);

            if (op.Type is TokenType.Equal or TokenType.NotEqual
                       or TokenType.GreaterThan or TokenType.LessThan
                       or TokenType.GreaterEqual or TokenType.LessEqual)
                return ApplyComparison(op, left, right);

            if (op.Type is TokenType.And or TokenType.Or)
                return ApplyLogical(op, left, right);

            // Arithmetic — coerce to double
            double l = ToDouble(left,  op);
            double r = ToDouble(right, op);

            return op.Type switch
            {
                TokenType.Plus     => l + r,
                TokenType.Minus    => l - r,
                TokenType.Multiply => l * r,
                TokenType.Divide   => r == 0
                    ? throw new RuleEvaluationException(DiagnosticCode.DivisionByZero, "Division by zero.")
                    : l / r,
                TokenType.Modulo   => r == 0
                    ? throw new RuleEvaluationException(DiagnosticCode.DivisionByZero, "Modulo by zero.")
                    : l % r,
                _ => throw new RuleEvaluationException(DiagnosticCode.UnsupportedOperator,
                         $"Unsupported operator '{op.Value}'.")
            };
        }

        private static object ApplyNot(object operand)
        {
            if (operand == null) return null;
            return !ToBool(operand);
        }

        private static object ApplyComparison(Token op, object left, object right)
        {
            // Try numeric comparison first
            bool lNum = TryDouble(left,  out double ld);
            bool rNum = TryDouble(right, out double rd);
            if (lNum && rNum)
            {
                return op.Type switch
                {
                    TokenType.Equal        => ld == rd,
                    TokenType.NotEqual     => ld != rd,
                    TokenType.GreaterThan  => ld >  rd,
                    TokenType.LessThan     => ld <  rd,
                    TokenType.GreaterEqual => ld >= rd,
                    TokenType.LessEqual    => ld <= rd,
                    _ => false
                };
            }

            // String comparison
            string ls = Convert.ToString(left,  CultureInfo.InvariantCulture);
            string rs = Convert.ToString(right, CultureInfo.InvariantCulture);
            int cmp   = string.Compare(ls, rs, StringComparison.OrdinalIgnoreCase);
            return op.Type switch
            {
                TokenType.Equal        => cmp == 0,
                TokenType.NotEqual     => cmp != 0,
                TokenType.GreaterThan  => cmp >  0,
                TokenType.LessThan     => cmp <  0,
                TokenType.GreaterEqual => cmp >= 0,
                TokenType.LessEqual    => cmp <= 0,
                _ => false
            };
        }

        private static object ApplyLogical(Token op, object left, object right)
        {
            bool l = ToBool(left);
            bool r = ToBool(right);
            return op.Type == TokenType.And ? l && r : l || r;
        }

        // ── Coercion helpers ──────────────────────────────────────────────────────

        private static double ToDouble(object v, Token op)
        {
            if (TryDouble(v, out double d)) return d;
            throw new RuleEvaluationException(DiagnosticCode.TypeCoercionFailed,
                $"Cannot coerce '{v}' to a number for operator '{op.Value}'.");
        }

        private static bool TryDouble(object v, out double d)
        {
            d = 0;
            if (v == null) return false;
            if (v is double dv)   { d = dv;  return true; }
            if (v is int    iv)   { d = iv;  return true; }
            if (v is long   lv)   { d = lv;  return true; }
            if (v is decimal dcv) { d = (double)dcv; return true; }
            if (v is bool   bv)   { d = bv ? 1 : 0; return true; }
            return double.TryParse(Convert.ToString(v), NumberStyles.Any,
                       CultureInfo.InvariantCulture, out d);
        }

        private static bool ToBool(object v)
        {
            if (v == null)   return false;
            if (v is bool b) return b;
            if (TryDouble(v, out double d)) return d != 0;
            return !string.IsNullOrEmpty(Convert.ToString(v));
        }

        // ── Token classification ──────────────────────────────────────────────────

        private static bool IsOperand(Token t) => t != null && t.Type is
            TokenType.EntityField or TokenType.RuleReference or
            TokenType.StringLiteral or TokenType.NumericLiteral or
            TokenType.BooleanLiteral or TokenType.NullLiteral or
            TokenType.Identifier;

        private static bool IsOperator(Token t) => t != null && t.Type is
            TokenType.Plus or TokenType.Minus or TokenType.Multiply or TokenType.Divide or
            TokenType.Modulo or TokenType.Equal or TokenType.NotEqual or
            TokenType.GreaterThan or TokenType.LessThan or
            TokenType.GreaterEqual or TokenType.LessEqual or
            TokenType.And or TokenType.Or or TokenType.Not;

        private static bool IsLeftAssociative(Token t) =>
            t.Type != TokenType.Not; // NOT is right-associative (unary prefix)

        private static int Precedence(Token t) => t.Type switch
        {
            TokenType.Or           => 1,
            TokenType.And          => 2,
            TokenType.Equal or TokenType.NotEqual
                or TokenType.GreaterThan or TokenType.LessThan
                or TokenType.GreaterEqual or TokenType.LessEqual => 3,
            TokenType.Plus or TokenType.Minus  => 4,
            TokenType.Multiply or TokenType.Divide or TokenType.Modulo => 5,
            TokenType.Not          => 6,
            _                      => 0
        };

        private static void Fault(Token t, string msg) =>
            throw new RuleEvaluationException(DiagnosticCode.UnsupportedOperator,
                $"Evaluation error at token '{t.Value}': {msg}");
    }
}
