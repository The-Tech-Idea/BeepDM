using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

namespace TheTechIdea.Beep.Rules
{
    public class RuleEngine : IRuleEngine
    {
        // Registry of rules keyed by a unique identifier—here we use RuleText.
        private readonly Dictionary<string, IRule> _rules;
        private readonly IRuleParser _ruleParser;

        public RuleEngine(IRuleParser ruleParser)
        {
            _rules = new Dictionary<string, IRule>();
            _ruleParser = ruleParser;
        }

        /// <summary>
        /// Registers a rule with the engine.
        /// </summary>
        public void RegisterRule(IRule rule)
        {
            if (rule == null)
                throw new ArgumentNullException(nameof(rule));

            // Use RuleText as the key (or another unique identifier from your metadata).
            if (_rules.ContainsKey(rule.RuleText))
                throw new ArgumentException($"A rule with key '{rule.RuleText}' is already registered.");

            _rules[rule.RuleText] = rule;
        }

        /// <summary>
        /// Evaluates a rule by its key, ensuring its structure is tokenized.
        /// </summary>
        public (Dictionary<string, object> outputs, object result) SolveRule(string ruleKey, Dictionary<string, object> parameters)
        {
            if (!_rules.ContainsKey(ruleKey))
                throw new ArgumentException($"Rule '{ruleKey}' not found.");

            IRule rule = _rules[ruleKey];

            // If the rule's structure has not been tokenized, parse it now.
            if (rule.Structure == null || rule.Structure.Tokens == null || !rule.Structure.Tokens.Any())
            {
                var structure = _ruleParser.ParseRule(rule.RuleText) as RuleStructure;
                rule.Structure = structure;
            }

            // Delegate the evaluation to the rule.
            return rule.SolveRule(parameters);
        }

        #region Expression Evaluation for Dynamic Rules (if needed)

        // Example methods using shunting yard algorithm and RPN evaluation.
        // You can call these from within a rule's SolveRule implementation if necessary.

        public object EvaluateExpression(IList<Token> tokens, Dictionary<string, object> parameters)
        {
            List<Token> rpn = ConvertToRPN(tokens);
            return EvaluateRPN(rpn, parameters);
        }

        private List<Token> ConvertToRPN(IList<Token> tokens)
        {
            List<Token> outputQueue = new List<Token>();
            Stack<Token> operatorStack = new Stack<Token>();

            foreach (var token in tokens)
            {
                if (IsOperand(token))
                {
                    outputQueue.Add(token);
                }
                else if (IsOperator(token))
                {
                    while (operatorStack.Any() && IsOperator(operatorStack.Peek()) &&
                           ((IsLeftAssociative(token) && GetPrecedence(token) <= GetPrecedence(operatorStack.Peek())) ||
                            (!IsLeftAssociative(token) && GetPrecedence(token) < GetPrecedence(operatorStack.Peek()))))
                    {
                        outputQueue.Add(operatorStack.Pop());
                    }
                    operatorStack.Push(token);
                }
                else if (token.Type == TokenType.LeftParenthesis)
                {
                    operatorStack.Push(token);
                }
                else if (token.Type == TokenType.RightParenthesis)
                {
                    while (operatorStack.Any() && operatorStack.Peek().Type != TokenType.LeftParenthesis)
                        outputQueue.Add(operatorStack.Pop());
                    if (operatorStack.Any() && operatorStack.Peek().Type == TokenType.LeftParenthesis)
                        operatorStack.Pop();
                }
            }
            while (operatorStack.Any())
                outputQueue.Add(operatorStack.Pop());
            return outputQueue;
        }

        private object EvaluateRPN(List<Token> rpn, Dictionary<string, object> parameters)
        {
            Stack<object> valueStack = new Stack<object>();

            foreach (var token in rpn)
            {
                if (IsOperand(token))
                {
                    valueStack.Push(GetTokenValue(token, parameters));
                }
                else if (IsOperator(token))
                {
                    if (token.Type == TokenType.Not) // example unary operator
                    {
                        if (valueStack.Count < 1)
                            throw new Exception("Insufficient values for operator " + token.Value);
                        var operand = valueStack.Pop();
                        valueStack.Push(ApplyOperator(token, operand, null));
                    }
                    else
                    {
                        if (valueStack.Count < 2)
                            throw new Exception("Insufficient values for operator " + token.Value);
                        var right = valueStack.Pop();
                        var left = valueStack.Pop();
                        valueStack.Push(ApplyOperator(token, left, right));
                    }
                }
            }

            if (valueStack.Count != 1)
                throw new Exception("Error in expression evaluation.");

            return valueStack.Pop();
        }

        private bool IsOperand(Token token)
        {
            return token.Type == TokenType.EntityField ||
                   token.Type == TokenType.RuleReference ||
                   token.Type == TokenType.StringLiteral ||
                   token.Type == TokenType.NumericLiteral ||
                   token.Type == TokenType.Identifier;
        }

        private bool IsOperator(Token token)
        {
            switch (token.Type)
            {
                case TokenType.Plus:
                case TokenType.Minus:
                case TokenType.Multiply:
                case TokenType.Divide:
                case TokenType.Equal:
                case TokenType.NotEqual:
                case TokenType.GreaterThan:
                case TokenType.LessThan:
                case TokenType.GreaterEqual:
                case TokenType.LessEqual:
                case TokenType.And:
                case TokenType.Or:
                case TokenType.Not:
                    return true;
                default:
                    return false;
            }
        }

        private int GetPrecedence(Token token)
        {
            switch (token.Type)
            {
                case TokenType.Multiply:
                case TokenType.Divide:
                    return 3;
                case TokenType.Plus:
                case TokenType.Minus:
                    return 2;
                default:
                    return 1;
            }
        }

        private bool IsLeftAssociative(Token token) => true;

        private object ApplyOperator(Token opToken, object left, object right)
        {
            double leftNum = Convert.ToDouble(left, CultureInfo.InvariantCulture);
            double rightNum = right != null ? Convert.ToDouble(right, CultureInfo.InvariantCulture) : 0;
            switch (opToken.Type)
            {
                case TokenType.Plus: return leftNum + rightNum;
                case TokenType.Minus: return leftNum - rightNum;
                case TokenType.Multiply: return leftNum * rightNum;
                case TokenType.Divide:
                    if (rightNum == 0)
                        throw new DivideByZeroException();
                    return leftNum / rightNum;
                default:
                    throw new NotSupportedException($"Operator {opToken.Value} is not supported.");
            }
        }

        private object GetTokenValue(Token token, Dictionary<string, object> parameters)
        {
            switch (token.Type)
            {
                case TokenType.NumericLiteral:
                    if (double.TryParse(token.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double num))
                        return num;
                    throw new Exception($"Invalid numeric literal: {token.Value}");
                case TokenType.StringLiteral:
                    return token.Value;
                case TokenType.EntityField:
                    if (parameters != null && parameters.TryGetValue(token.Value, out object entityValue))
                        return entityValue;
                    throw new Exception($"Entity field '{token.Value}' not found.");
                case TokenType.RuleReference:
                    // Recursively evaluate a rule reference.
                    var res = SolveRule(token.Value, parameters).result;
                    return res;
                case TokenType.Identifier:
                    if (parameters != null && parameters.TryGetValue(token.Value, out object idValue))
                        return idValue;
                    throw new Exception($"Identifier '{token.Value}' not found.");
                default:
                    return token.Value;
            }
        }

        #endregion
    }
}
