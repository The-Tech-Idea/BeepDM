using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Defaults.Attributes;

namespace TheTechIdea.Beep.Editor.Defaults.Resolvers
{
    /// <summary>
    /// Resolver for expression evaluation and complex logic operations
    /// </summary>
    [DefaultResolver("Expression", "Expression Resolver",
        Description = "Evaluates conditional expressions, logical operators, and ternary operations.",
        SupportedTokens = "EXPRESSION,EVAL,IF,CASE,WHEN,THEN,ELSE,CONDITIONAL,TERNARY,ISNULL,COALESCE,AND,OR,NOT,EQ,NE,GT,GTE,LT,LTE")]
    public class ExpressionResolver : BaseDefaultValueResolver
    {
        public ExpressionResolver(IDMEEditor editor) : base(editor) { }

        public override string ResolverName => "Expression";

        public override IEnumerable<string> SupportedRuleTypes => new[]
        {
            // Legacy function-style (v1 legacy) — conditional
            "EXPRESSION", "EVAL", "IF", "CASE", "WHEN", "THEN", "ELSE",
            "CONDITIONAL", "TERNARY", "ISNULL", "COALESCE",
            // Logical operators
            "AND", "OR", "NOT",
            // Comparison operators
            "EQ", "NE", "GT", "GTE", "LT", "LTE",
            // Dot-style operator tokens (v1 dot) - normalized to function style before resolution
            "IF.", "CASE.", "COALESCE.", "ISNULL.",
            "AND.", "OR.", "NOT.",
            "EQ.", "NE.", "GT.", "GTE.", "LT.", "LTE."
        };

        public override object ResolveValue(string rule, IPassedArgs parameters)
        {
            var upperRule = rule.ToUpperInvariant().Trim();
            
            try
            {
                return upperRule switch
                {
                    _ when upperRule.StartsWith("EXPRESSION(") || upperRule.StartsWith("EVAL(") => HandleExpression(rule, parameters),
                    _ when upperRule.StartsWith("IF(") => HandleIfStatement(rule, parameters),
                    _ when upperRule.StartsWith("CASE(") => HandleCaseStatement(rule, parameters),
                    _ when upperRule.StartsWith("CONDITIONAL(") => HandleConditional(rule, parameters),
                    _ when upperRule.StartsWith("TERNARY(") => HandleTernary(rule, parameters),
                    _ when upperRule.StartsWith("ISNULL(") => HandleIsNull(rule, parameters),
                    _ when upperRule.StartsWith("COALESCE(") => HandleCoalesce(rule, parameters),
                    _ when upperRule.StartsWith("AND(") => HandleLogical(rule, parameters, "AND"),
                    _ when upperRule.StartsWith("OR(") => HandleLogical(rule, parameters, "OR"),
                    _ when upperRule.StartsWith("NOT(") => HandleNot(rule, parameters),
                    _ when upperRule.StartsWith("EQ(") => HandleComparison(rule, parameters, "="),
                    _ when upperRule.StartsWith("NE(") => HandleComparison(rule, parameters, "!="),
                    _ when upperRule.StartsWith("GT(") => HandleComparison(rule, parameters, ">"),
                    _ when upperRule.StartsWith("GTE(") => HandleComparison(rule, parameters, ">="),
                    _ when upperRule.StartsWith("LT(") => HandleComparison(rule, parameters, "<"),
                    _ when upperRule.StartsWith("LTE(") => HandleComparison(rule, parameters, "<="),
                    _ => null
                };
            }
            catch (Exception ex)
            {
                LogError($"Error resolving expression rule '{rule}'", ex);
                return null;
            }
        }

        public override bool CanHandle(string rule)
        {
            if (string.IsNullOrWhiteSpace(rule))
                return false;

            var upperRule = rule.ToUpperInvariant().Trim();

            // Legacy function-style
            if (upperRule.StartsWith("EXPRESSION(") ||
                upperRule.StartsWith("EVAL(") ||
                upperRule.StartsWith("IF(") ||
                upperRule.StartsWith("CASE(") ||
                upperRule.StartsWith("CONDITIONAL(") ||
                upperRule.StartsWith("TERNARY(") ||
                upperRule.StartsWith("ISNULL(") ||
                upperRule.StartsWith("COALESCE(") ||
                upperRule.StartsWith("AND(") ||
                upperRule.StartsWith("OR(") ||
                upperRule.StartsWith("NOT(") ||
                upperRule.StartsWith("EQ(") ||
                upperRule.StartsWith("NE(") ||
                upperRule.StartsWith("GT(") ||
                upperRule.StartsWith("GTE(") ||
                upperRule.StartsWith("LT(") ||
                upperRule.StartsWith("LTE("))
                return true;

            // Dot-style pre-normalization fallback
            return upperRule.StartsWith("IF.") ||
                   upperRule.StartsWith("CASE.") ||
                   upperRule.StartsWith("COALESCE.") ||
                   upperRule.StartsWith("ISNULL.") ||
                   upperRule.StartsWith("AND.") ||
                   upperRule.StartsWith("OR.") ||
                   upperRule.StartsWith("NOT.") ||
                   upperRule.StartsWith("EQ.") ||
                   upperRule.StartsWith("NE.") ||
                   upperRule.StartsWith("GT.") ||
                   upperRule.StartsWith("GTE.") ||
                   upperRule.StartsWith("LT.") ||
                   upperRule.StartsWith("LTE.");
        }

        public override IEnumerable<string> GetExamples()
        {
            return new[]
            {
                "EXPRESSION(2 + 3 * 4) - Evaluate mathematical expression",
                "EVAL(Length > 10) - Evaluate boolean expression",
                "IF(Age >= 18, 'Adult', 'Minor') - Conditional logic",
                "CASE(Status, 'Active', 'Yes', 'No') - Case/switch logic",
                "CONDITIONAL(IsActive = true, 'Enabled', 'Disabled') - Conditional value",
                "TERNARY(Score >= 70, 'Pass', 'Fail') - Ternary operator",
                "ISNULL(MiddleName, '') - Return value if not null, else default",
                "COALESCE(NickName, FirstName, 'Unknown') - First non-null value",
                "AND(IsActive, IsVerified) - true when all conditions are true",
                "OR(IsAdmin, IsSuperUser) - true when any condition is true",
                "NOT(IsDeleted) - logical negation",
                "EQ(Status, Active) - equality comparison, returns bool",
                "NE(Status, Deleted) - inequality comparison, returns bool",
                "GT(Score, 70) - greater-than comparison",
                "GTE(Age, 18) - greater-than-or-equal comparison",
                "LT(Price, 100) - less-than comparison",
                "LTE(Rank, 10) - less-than-or-equal comparison"
            };
        }

        #region Handler Methods

        private object HandleExpression(string rule, IPassedArgs parameters)
        {
            try
            {
                var expression = ExtractParenthesesContent(rule);
                if (string.IsNullOrWhiteSpace(expression))
                {
                    LogError("EXPRESSION requires an expression parameter");
                    return null;
                }

                return EvaluateExpression(expression, parameters);
            }
            catch (Exception ex)
            {
                LogError($"Error in HandleExpression", ex);
                return null;
            }
        }

        private object HandleIfStatement(string rule, IPassedArgs parameters)
        {
            try
            {
                var content = ExtractParenthesesContent(rule);
                var parts = SplitParameters(content);

                if (parts.Length < 2)
                {
                    LogError("IF requires at least condition and true value parameters");
                    return null;
                }

                var condition = parts[0].Trim();
                var trueValue = RemoveQuotes(parts[1].Trim());
                var falseValue = parts.Length > 2 ? RemoveQuotes(parts[2].Trim()) : null;

                var conditionResult = EvaluateCondition(condition, parameters);
                return conditionResult ? trueValue : falseValue;
            }
            catch (Exception ex)
            {
                LogError($"Error in HandleIfStatement", ex);
                return null;
            }
        }

        private object HandleCaseStatement(string rule, IPassedArgs parameters)
        {
            try
            {
                var content = ExtractParenthesesContent(rule);
                var parts = SplitParameters(content);

                if (parts.Length < 3)
                {
                    LogError("CASE requires at least value, condition, and result parameters");
                    return null;
                }

                var testValue = RemoveQuotes(parts[0].Trim());
                
                // Process condition/result pairs
                for (int i = 1; i < parts.Length - 1; i += 2)
                {
                    var condition = RemoveQuotes(parts[i].Trim());
                    var result = RemoveQuotes(parts[i + 1].Trim());

                    if (string.Equals(testValue, condition, StringComparison.OrdinalIgnoreCase))
                    {
                        return result;
                    }
                }

                // Return default value if provided (last odd-indexed parameter)
                if (parts.Length % 2 == 0) // Even number means we have a default value
                {
                    return RemoveQuotes(parts[parts.Length - 1].Trim());
                }

                return null;
            }
            catch (Exception ex)
            {
                LogError($"Error in HandleCaseStatement", ex);
                return null;
            }
        }

        private object HandleConditional(string rule, IPassedArgs parameters)
        {
            try
            {
                var content = ExtractParenthesesContent(rule);
                var parts = SplitParameters(content);

                if (parts.Length < 2)
                {
                    LogError("CONDITIONAL requires condition and true value parameters");
                    return null;
                }

                var condition = parts[0].Trim();
                var trueValue = RemoveQuotes(parts[1].Trim());
                var falseValue = parts.Length > 2 ? RemoveQuotes(parts[2].Trim()) : null;

                var conditionResult = EvaluateCondition(condition, parameters);
                return conditionResult ? trueValue : falseValue;
            }
            catch (Exception ex)
            {
                LogError($"Error in HandleConditional", ex);
                return null;
            }
        }

        private object HandleTernary(string rule, IPassedArgs parameters)
        {
            // Same as IF statement
            return HandleIfStatement(rule, parameters);
        }

        private object HandleIsNull(string rule, IPassedArgs parameters)
        {
            try
            {
                var content = ExtractParenthesesContent(rule);
                var parts = SplitParameters(content);

                if (parts.Length < 2)
                {
                    LogError("ISNULL requires value and default parameters");
                    return null;
                }

                var value = GetValueFromExpression(parts[0].Trim(), parameters);
                var defaultValue = RemoveQuotes(parts[1].Trim());

                return value ?? defaultValue;
            }
            catch (Exception ex)
            {
                LogError($"Error in HandleIsNull", ex);
                return null;
            }
        }

        private object HandleCoalesce(string rule, IPassedArgs parameters)
        {
            try
            {
                var content = ExtractParenthesesContent(rule);
                var parts = SplitParameters(content);

                if (parts.Length < 2)
                {
                    LogError("COALESCE requires at least 2 parameters");
                    return null;
                }

                // Return first non-null value
                foreach (var part in parts)
                {
                    var value = GetValueFromExpression(part.Trim(), parameters);
                    if (value != null && !string.IsNullOrEmpty(value.ToString()))
                    {
                        return value;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                LogError($"Error in HandleCoalesce", ex);
                return null;
            }
        }

        /// <summary>
        /// AND(cond1, cond2, ...) / OR(cond1, cond2, ...)
        /// Returns bool — true when ALL (AND) or ANY (OR) conditions evaluate to true.
        /// Each argument may be a literal bool, a field reference, or a nested condition string.
        /// </summary>
        private object HandleLogical(string rule, IPassedArgs parameters, string op)
        {
            try
            {
                var parts = SplitParameters(ExtractParenthesesContent(rule));
                if (parts.Length < 2) { LogError($"{op} requires at least 2 parameters"); return null; }

                var results = parts.Select(p => EvaluateCondition(p.Trim(), parameters)).ToList();
                return op == "AND" ? results.All(r => r) : results.Any(r => r);
            }
            catch (Exception ex) { LogError($"Error in Handle{op}", ex); return null; }
        }

        /// <summary>
        /// NOT(condition) — logical negation of a single condition.
        /// </summary>
        private object HandleNot(string rule, IPassedArgs parameters)
        {
            try
            {
                var parts = SplitParameters(ExtractParenthesesContent(rule));
                if (parts.Length < 1) { LogError("NOT requires a condition parameter"); return null; }
                return !EvaluateCondition(parts[0].Trim(), parameters);
            }
            catch (Exception ex) { LogError("Error in HandleNot", ex); return null; }
        }

        /// <summary>
        /// EQ/NE/GT/GTE/LT/LTE(left, right) — explicit comparison operators.
        /// Returns bool. Arguments may be field references or literals.
        /// Phase-3 canonical form: avoids inline operator parsing ambiguity.
        /// </summary>
        private object HandleComparison(string rule, IPassedArgs parameters, string op)
        {
            try
            {
                var parts = SplitParameters(ExtractParenthesesContent(rule));
                if (parts.Length < 2) { LogError($"Comparison requires 2 parameters"); return null; }

                var left  = GetValueFromExpression(parts[0].Trim(), parameters);
                var right = GetValueFromExpression(parts[1].Trim(), parameters);
                return CompareValues(left, right, op);
            }
            catch (Exception ex) { LogError($"Error in HandleComparison({op})", ex); return null; }
        }

        #endregion

        #region Helper Methods

        private object EvaluateExpression(string expression, IPassedArgs parameters)
        {
            // Simple expression evaluator - in production, use a proper expression engine
            try
            {
                expression = expression.Trim();

                // Handle boolean expressions first
                if (expression.Contains("=") || expression.Contains("<") || expression.Contains(">") ||
                    expression.Contains("AND") || expression.Contains("OR") || expression.Contains("NOT"))
                {
                    return EvaluateCondition(expression, parameters);
                }

                // Handle arithmetic expressions
                if (expression.Contains("+") || expression.Contains("-") || 
                    expression.Contains("*") || expression.Contains("/"))
                {
                    return EvaluateArithmetic(expression, parameters);
                }

                // Try to get as property/field value
                var value = GetValueFromExpression(expression, parameters);
                if (value != null)
                    return value;

                // Try to parse as literal
                if (TryConvert<double>(expression, out double numResult))
                    return numResult;

                if (bool.TryParse(expression, out bool boolResult))
                    return boolResult;

                return RemoveQuotes(expression);
            }
            catch (Exception ex)
            {
                LogWarning($"Could not evaluate expression '{expression}': {ex.Message}");
                return null;
            }
        }

        private bool EvaluateCondition(string condition, IPassedArgs parameters)
        {
            try
            {
                condition = condition.Trim();

                // Handle comparison operators
                var operators = new[] { ">=", "<=", "!=", "<>", "=", ">", "<" };
                
                foreach (var op in operators)
                {
                    var index = condition.IndexOf(op);
                    if (index > 0)
                    {
                        var left = condition.Substring(0, index).Trim();
                        var right = condition.Substring(index + op.Length).Trim();

                        var leftValue = GetValueFromExpression(left, parameters);
                        var rightValue = GetValueFromExpression(right, parameters);

                        return CompareValues(leftValue, rightValue, op);
                    }
                }

                // Handle boolean value directly
                var boolValue = GetValueFromExpression(condition, parameters);
                if (boolValue is bool b)
                    return b;

                if (bool.TryParse(condition, out bool result))
                    return result;

                // Non-null, non-empty values are truthy
                return boolValue != null && !string.IsNullOrEmpty(boolValue.ToString());
            }
            catch (Exception ex)
            {
                LogWarning($"Could not evaluate condition '{condition}': {ex.Message}");
                return false;
            }
        }

        private double EvaluateArithmetic(string expression, IPassedArgs parameters)
        {
            // Very basic arithmetic evaluator
            try
            {
                if (expression.Contains("+"))
                {
                    var parts = expression.Split('+');
                    if (parts.Length == 2)
                    {
                        var left = GetNumericValue(parts[0].Trim(), parameters);
                        var right = GetNumericValue(parts[1].Trim(), parameters);
                        return left + right;
                    }
                }
                else if (expression.Contains("-"))
                {
                    var parts = expression.Split('-');
                    if (parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[0]))
                    {
                        var left = GetNumericValue(parts[0].Trim(), parameters);
                        var right = GetNumericValue(parts[1].Trim(), parameters);
                        return left - right;
                    }
                }
                else if (expression.Contains("*"))
                {
                    var parts = expression.Split('*');
                    if (parts.Length == 2)
                    {
                        var left = GetNumericValue(parts[0].Trim(), parameters);
                        var right = GetNumericValue(parts[1].Trim(), parameters);
                        return left * right;
                    }
                }
                else if (expression.Contains("/"))
                {
                    var parts = expression.Split('/');
                    if (parts.Length == 2)
                    {
                        var left = GetNumericValue(parts[0].Trim(), parameters);
                        var right = GetNumericValue(parts[1].Trim(), parameters);
                        return right != 0 ? left / right : 0;
                    }
                }

                return GetNumericValue(expression, parameters);
            }
            catch (Exception ex)
            {
                LogWarning($"Could not evaluate arithmetic expression '{expression}': {ex.Message}");
                return 0;
            }
        }

        private object GetValueFromExpression(string expression, IPassedArgs parameters)
        {
            expression = RemoveQuotes(expression);

            // Try to get from object properties first
            var targetObject = GetParameterValue<object>(parameters, "Object") ??
                              GetParameterValue<object>(parameters, "Record");

            if (targetObject != null)
            {
                try
                {
                    var property = targetObject.GetType().GetProperty(expression);
                    if (property != null && property.CanRead)
                    {
                        return property.GetValue(targetObject);
                    }
                }
                catch
                {
                    // Ignore property access errors
                }
            }

            return null;
        }

        private double GetNumericValue(string expression, IPassedArgs parameters)
        {
            var value = GetValueFromExpression(expression, parameters);
            
            if (value != null && TryConvert<double>(value.ToString(), out double result))
                return result;

            if (TryConvert<double>(expression, out double direct))
                return direct;

            return 0;
        }

        private bool CompareValues(object left, object right, string op)
        {
            try
            {
                // Handle null comparisons
                if (left == null && right == null)
                    return op == "=" || op == ">=";
                if (left == null || right == null)
                    return op == "!=" || op == "<>";

                // Try numeric comparison first
                if (TryConvert<double>(left.ToString(), out double leftNum) && 
                    TryConvert<double>(right.ToString(), out double rightNum))
                {
                    return op switch
                    {
                        "=" => Math.Abs(leftNum - rightNum) < 0.0001,
                        "!=" or "<>" => Math.Abs(leftNum - rightNum) >= 0.0001,
                        ">" => leftNum > rightNum,
                        "<" => leftNum < rightNum,
                        ">=" => leftNum >= rightNum,
                        "<=" => leftNum <= rightNum,
                        _ => false
                    };
                }

                // String comparison
                var leftStr = left.ToString();
                var rightStr = right.ToString();
                var comparison = string.Compare(leftStr, rightStr, StringComparison.OrdinalIgnoreCase);

                return op switch
                {
                    "=" => comparison == 0,
                    "!=" or "<>" => comparison != 0,
                    ">" => comparison > 0,
                    "<" => comparison < 0,
                    ">=" => comparison >= 0,
                    "<=" => comparison <= 0,
                    _ => false
                };
            }
            catch (Exception ex)
            {
                LogWarning($"Error comparing values: {ex.Message}");
                return false;
            }
        }

        #endregion
    }
}