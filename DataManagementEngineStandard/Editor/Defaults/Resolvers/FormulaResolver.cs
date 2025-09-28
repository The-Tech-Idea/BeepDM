using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Editor.Defaults.Resolvers
{
    /// <summary>
    /// Resolver for mathematical formulas and expressions
    /// </summary>
    public class FormulaResolver : BaseDefaultValueResolver
    {
        private readonly Random _random = new Random();

        public FormulaResolver(IDMEEditor editor) : base(editor) { }

        public override string ResolverName => "Formula";

        public override IEnumerable<string> SupportedRuleTypes => new[]
        {
            "SEQUENCE", "INCREMENT", "AUTOINCREMENT", "RANDOM", "RANDOMVALUE", 
            "CALCULATE", "COMPUTE", "MATH", "ADD", "SUBTRACT", "MULTIPLY", "DIVIDE"
        };

        public override object ResolveValue(string rule, IPassedArgs parameters)
        {
            var upperRule = rule.ToUpperInvariant().Trim();
            
            try
            {
                return upperRule switch
                {
                    _ when upperRule.StartsWith("SEQUENCE(") => ParseSequence(rule, parameters),
                    _ when upperRule.StartsWith("INCREMENT(") || upperRule.StartsWith("AUTOINCREMENT(") => ParseIncrement(rule, parameters),
                    _ when upperRule.StartsWith("RANDOM(") || upperRule.StartsWith("RANDOMVALUE(") => ParseRandom(rule),
                    _ when upperRule.StartsWith("CALCULATE(") || upperRule.StartsWith("COMPUTE(") => ParseCalculation(rule, parameters),
                    _ when upperRule.StartsWith("MATH(") => ParseMathFunction(rule),
                    _ when upperRule.StartsWith("ADD(") => ParseBinaryOperation(rule, (a, b) => a + b),
                    _ when upperRule.StartsWith("SUBTRACT(") => ParseBinaryOperation(rule, (a, b) => a - b),
                    _ when upperRule.StartsWith("MULTIPLY(") => ParseBinaryOperation(rule, (a, b) => a * b),
                    _ when upperRule.StartsWith("DIVIDE(") => ParseBinaryOperation(rule, (a, b) => b != 0 ? a / b : 0),
                    _ => 0
                };
            }
            catch (Exception ex)
            {
                LogError($"Error resolving formula rule '{rule}'", ex);
                return 0;
            }
        }

        public override bool CanHandle(string rule)
        {
            if (string.IsNullOrWhiteSpace(rule))
                return false;

            var upperRule = rule.ToUpperInvariant().Trim();
            return upperRule.StartsWith("SEQUENCE(") ||
                   upperRule.StartsWith("INCREMENT(") ||
                   upperRule.StartsWith("AUTOINCREMENT(") ||
                   upperRule.StartsWith("RANDOM(") ||
                   upperRule.StartsWith("RANDOMVALUE(") ||
                   upperRule.StartsWith("CALCULATE(") ||
                   upperRule.StartsWith("COMPUTE(") ||
                   upperRule.StartsWith("MATH(") ||
                   upperRule.StartsWith("ADD(") ||
                   upperRule.StartsWith("SUBTRACT(") ||
                   upperRule.StartsWith("MULTIPLY(") ||
                   upperRule.StartsWith("DIVIDE(");
        }

        public override IEnumerable<string> GetExamples()
        {
            return new[]
            {
                "SEQUENCE(1000) - Start sequence from 1000",
                "SEQUENCE(OrderNumber, 1) - Named sequence starting at 1",
                "INCREMENT(fieldname) - Increment based on existing field",
                "AUTOINCREMENT(1) - Auto increment starting from 1", 
                "RANDOM(1, 100) - Random number between 1 and 100",
                "RANDOMVALUE(10) - Random number between 0 and 10",
                "CALCULATE(field1 + field2) - Simple calculation",
                "COMPUTE(price * quantity) - Product calculation",
                "MATH(PI) - Mathematical constant",
                "MATH(SQRT, 16) - Square root of 16",
                "ADD(10, 5) - Add two numbers",
                "SUBTRACT(20, 8) - Subtract numbers",
                "MULTIPLY(3, 7) - Multiply numbers",
                "DIVIDE(100, 4) - Divide numbers"
            };
        }

        #region Private Parser Methods

        private object ParseSequence(string rule, IPassedArgs parameters)
        {
            try
            {
                var content = ExtractParenthesesContent(rule);
                var parts = SplitParameters(content);

                if (parts.Length == 0)
                {
                    LogError("SEQUENCE requires parameters");
                    return 1;
                }

                // Simple sequence implementation
                if (parts.Length == 1)
                {
                    // Single parameter - starting number
                    if (TryConvert<int>(parts[0].Trim(), out int start))
                    {
                        // In a real implementation, you'd store and increment this value
                        // For now, return start + current millisecond for uniqueness
                        return start + (DateTime.Now.Millisecond % 1000);
                    }
                }
                else if (parts.Length == 2)
                {
                    // Named sequence with starting number
                    var sequenceName = RemoveQuotes(parts[0].Trim());
                    if (TryConvert<int>(parts[1].Trim(), out int start))
                    {
                        // This would typically be stored in a sequence table
                        // For demo, return a computed value
                        var hash = sequenceName.GetHashCode();
                        return start + Math.Abs(hash % 1000) + (DateTime.Now.Millisecond % 100);
                    }
                }

                return 1;
            }
            catch (Exception ex)
            {
                LogError($"Error parsing SEQUENCE rule '{rule}'", ex);
                return 1;
            }
        }

        private object ParseIncrement(string rule, IPassedArgs parameters)
        {
            try
            {
                var content = ExtractParenthesesContent(rule);
                
                if (string.IsNullOrWhiteSpace(content))
                {
                    // Auto increment from 1
                    return DateTime.Now.Ticks % 1000000;
                }

                var parts = SplitParameters(content);
                
                if (parts.Length == 1)
                {
                    if (TryConvert<int>(parts[0].Trim(), out int start))
                    {
                        // Start from specific number
                        return start + (DateTime.Now.Millisecond % 1000);
                    }
                    else
                    {
                        // Field name - would typically look up max value and increment
                        var fieldName = RemoveQuotes(parts[0].Trim());
                        LogInfo($"INCREMENT for field '{fieldName}' - would typically query max value");
                        return DateTime.Now.Ticks % 1000000;
                    }
                }

                return 1;
            }
            catch (Exception ex)
            {
                LogError($"Error parsing INCREMENT rule '{rule}'", ex);
                return 1;
            }
        }

        private object ParseRandom(string rule)
        {
            try
            {
                var content = ExtractParenthesesContent(rule);
                var parts = SplitParameters(content);
                
                if (parts.Length == 0)
                {
                    return _random.Next(1, 100); // Default range
                }
                else if (parts.Length == 1)
                {
                    // Single parameter - max value (min is 0)
                    if (TryConvert<int>(parts[0].Trim(), out int max))
                    {
                        return _random.Next(0, max + 1);
                    }
                }
                else if (parts.Length == 2)
                {
                    // Two parameters - min and max
                    if (TryConvert<int>(parts[0].Trim(), out int min) && 
                        TryConvert<int>(parts[1].Trim(), out int max))
                    {
                        return _random.Next(min, max + 1);
                    }
                }

                return _random.Next(1, 100);
            }
            catch (Exception ex)
            {
                LogError($"Error parsing RANDOM rule '{rule}'", ex);
                return _random.Next(1, 100);
            }
        }

        private object ParseCalculation(string rule, IPassedArgs parameters)
        {
            try
            {
                var expression = ExtractParenthesesContent(rule);
                
                if (string.IsNullOrWhiteSpace(expression))
                {
                    LogError("CALCULATE requires an expression");
                    return 0;
                }

                // Simple expression parser - this could be enhanced with a proper expression engine
                return EvaluateSimpleExpression(expression, parameters);
            }
            catch (Exception ex)
            {
                LogError($"Error parsing CALCULATE rule '{rule}'", ex);
                return 0;
            }
        }

        private object ParseMathFunction(string rule)
        {
            try
            {
                var content = ExtractParenthesesContent(rule);
                var parts = SplitParameters(content);

                if (parts.Length == 0)
                {
                    LogError("MATH requires function name");
                    return 0;
                }

                var function = parts[0].Trim().ToUpperInvariant();

                return function switch
                {
                    "PI" => Math.PI,
                    "E" => Math.E,
                    "SQRT" when parts.Length > 1 && TryConvert<double>(parts[1], out double sqrtVal) => Math.Sqrt(sqrtVal),
                    "ABS" when parts.Length > 1 && TryConvert<double>(parts[1], out double absVal) => Math.Abs(absVal),
                    "ROUND" when parts.Length > 1 && TryConvert<double>(parts[1], out double roundVal) => Math.Round(roundVal),
                    "FLOOR" when parts.Length > 1 && TryConvert<double>(parts[1], out double floorVal) => Math.Floor(floorVal),
                    "CEILING" when parts.Length > 1 && TryConvert<double>(parts[1], out double ceilVal) => Math.Ceiling(ceilVal),
                    "SIN" when parts.Length > 1 && TryConvert<double>(parts[1], out double sinVal) => Math.Sin(sinVal),
                    "COS" when parts.Length > 1 && TryConvert<double>(parts[1], out double cosVal) => Math.Cos(cosVal),
                    "TAN" when parts.Length > 1 && TryConvert<double>(parts[1], out double tanVal) => Math.Tan(tanVal),
                    _ => 0
                };
            }
            catch (Exception ex)
            {
                LogError($"Error parsing MATH rule '{rule}'", ex);
                return 0;
            }
        }

        private object ParseBinaryOperation(string rule, Func<double, double, double> operation)
        {
            try
            {
                var content = ExtractParenthesesContent(rule);
                var parts = SplitParameters(content);

                if (parts.Length != 2)
                {
                    LogError($"Binary operation requires exactly 2 parameters");
                    return 0;
                }

                if (TryConvert<double>(parts[0].Trim(), out double a) && 
                    TryConvert<double>(parts[1].Trim(), out double b))
                {
                    return operation(a, b);
                }

                return 0;
            }
            catch (Exception ex)
            {
                LogError($"Error parsing binary operation rule '{rule}'", ex);
                return 0;
            }
        }

        private double EvaluateSimpleExpression(string expression, IPassedArgs parameters)
        {
            // This is a very basic expression evaluator
            // In a production system, you'd want to use a proper expression parsing library
            
            expression = expression.Trim();

            // Handle simple binary operations
            if (expression.Contains("+"))
            {
                var parts = expression.Split('+');
                if (parts.Length == 2 && 
                    TryConvert<double>(parts[0].Trim(), out double a) && 
                    TryConvert<double>(parts[1].Trim(), out double b))
                {
                    return a + b;
                }
            }
            else if (expression.Contains("-"))
            {
                var parts = expression.Split('-');
                if (parts.Length == 2 && 
                    TryConvert<double>(parts[0].Trim(), out double a) && 
                    TryConvert<double>(parts[1].Trim(), out double b))
                {
                    return a - b;
                }
            }
            else if (expression.Contains("*"))
            {
                var parts = expression.Split('*');
                if (parts.Length == 2 && 
                    TryConvert<double>(parts[0].Trim(), out double a) && 
                    TryConvert<double>(parts[1].Trim(), out double b))
                {
                    return a * b;
                }
            }
            else if (expression.Contains("/"))
            {
                var parts = expression.Split('/');
                if (parts.Length == 2 && 
                    TryConvert<double>(parts[0].Trim(), out double a) && 
                    TryConvert<double>(parts[1].Trim(), out double b) && b != 0)
                {
                    return a / b;
                }
            }

            // Try to parse as single number
            if (TryConvert<double>(expression, out double result))
            {
                return result;
            }

            LogWarning($"Could not evaluate expression '{expression}'");
            return 0;
        }

        #endregion
    }
}