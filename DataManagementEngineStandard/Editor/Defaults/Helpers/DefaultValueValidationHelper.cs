using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Defaults.Interfaces;
using TheTechIdea.Beep.Editor.Defaults.RuleParsing;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.Defaults.Helpers
{
    /// <summary>
    /// Helper for validating default value configurations
    /// </summary>
    public class DefaultValueValidationHelper : IDefaultValueValidationHelper
    {
        private readonly IDMEEditor _editor;

        public DefaultValueValidationHelper(IDMEEditor editor)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        }

        public IErrorsInfo ValidateDefaultValue(DefaultValue defaultValue)
        {
            var errorInfo = new ErrorsInfo { Flag = Errors.Ok };

            try
            {
                if (defaultValue == null)
                {
                    return CreateError("Default value cannot be null");
                }

                // Validate property name
                if (string.IsNullOrWhiteSpace(defaultValue.PropertyName))
                {
                    return CreateError("Property name cannot be empty");
                }

                // Validate that either value or rule is provided
                if (IsEmptyValue(defaultValue.PropertyValue) && 
                    string.IsNullOrWhiteSpace(defaultValue.Rule))
                {
                    return CreateError("Either property value or rule must be specified");
                }

                // Validate property name format
                if (!IsValidPropertyName(defaultValue.PropertyName))
                {
                    return CreateError($"Invalid property name format: '{defaultValue.PropertyName}'");
                }

                // If rule is provided, validate it
                if (!string.IsNullOrWhiteSpace(defaultValue.Rule))
                {
                    var ruleValidation = ValidateRule(defaultValue.Rule);
                    if (ruleValidation.Flag == Errors.Failed)
                    {
                        return ruleValidation;
                    }
                }

                // Validate property type consistency
                var typeValidation = ValidatePropertyTypeConsistency(defaultValue);
                if (typeValidation.Flag == Errors.Failed)
                {
                    return typeValidation;
                }

                errorInfo.Message = "Default value validation passed";
                return errorInfo;
            }
            catch (Exception ex)
            {
                return CreateError($"Validation error: {ex.Message}");
            }
        }

        /// <summary>
        /// Full three-tier rule validation: syntax → semantic → safety.
        /// Each tier must pass before the next is evaluated.
        /// </summary>
        public IErrorsInfo ValidateRule(string rule)
        {
            if (string.IsNullOrWhiteSpace(rule))
            {
                var empty = new ErrorsInfo { Flag = Errors.Ok, Message = "No rule to validate" };
                return empty;
            }

            var syntax   = ValidateRuleSyntax(rule);
            if (syntax.Flag == Errors.Failed)   return syntax;

            var semantic = ValidateRuleSemantic(rule);
            if (semantic.Flag == Errors.Failed) return semantic;

            var safety   = ValidateRuleSafety(rule);
            if (safety.Flag == Errors.Failed)   return safety;

            // Aggregate any warnings from all tiers into the final result
            var warnings = new System.Text.StringBuilder();
            if (syntax.Flag   == Errors.Warning) warnings.Append(syntax.Message).Append("; ");
            if (semantic.Flag == Errors.Warning) warnings.Append(semantic.Message).Append("; ");
            if (safety.Flag   == Errors.Warning) warnings.Append(safety.Message).Append("; ");

            if (warnings.Length > 0)
            {
                _editor.AddLogMessage("DefaultValueValidationHelper",
                    $"Rule '{Redact(rule)}' warnings: {warnings}", DateTime.Now, -1, "", Errors.Warning);
                return new ErrorsInfo { Flag = Errors.Warning, Message = warnings.ToString().TrimEnd(';', ' ') };
            }

            return new ErrorsInfo { Flag = Errors.Ok, Message = "Rule validation passed" };
        }

        // ── Tier 1 – Syntax ──────────────────────────────────────────────────

        /// <summary>
        /// Checks structural correctness: parseable, balanced brackets,
        /// no whitespace-only content, no unterminated strings.
        /// </summary>
        public IErrorsInfo ValidateRuleSyntax(string rule)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(rule))
                    return CreateError("Rule cannot be empty or whitespace");

                // Run through the normalization pipeline — this catches dot-style arity issues.
                var parsed = RuleNormalizer.Normalize(rule);
                if (!parsed.IsValid)
                {
                    var errors = parsed.Diagnostics
                        .Where(d => d.Severity == RuleDiagnosticSeverity.Error)
                        .Select(d => d.ToString());
                    return CreateError($"[SYNTAX] Parse errors: {string.Join("; ", errors)}");
                }

                var normalized = parsed.NormalizedRule ?? rule;

                if (!IsValidRuleSyntax(normalized))
                    return CreateError($"[SYNTAX] Invalid rule syntax (double punctuation or control chars): '{Redact(rule)}'");

                if (!AreParenthesesBalanced(normalized))
                    return CreateError($"[SYNTAX] Unbalanced parentheses in rule: '{Redact(rule)}'");

                // Warn on dot-style warnings from the parser
                var warnings = parsed.Diagnostics
                    .Where(d => d.Severity == RuleDiagnosticSeverity.Warning)
                    .Select(d => d.ToString())
                    .ToList();
                if (warnings.Count > 0)
                    return new ErrorsInfo { Flag = Errors.Warning, Message = $"[SYNTAX] {string.Join("; ", warnings)}" };

                return new ErrorsInfo { Flag = Errors.Ok, Message = "Syntax ok" };
            }
            catch (Exception ex)
            {
                return CreateError($"[SYNTAX] Exception during syntax validation: {ex.Message}");
            }
        }

        // ── Tier 2 – Semantic ────────────────────────────────────────────────

        /// <summary>
        /// Checks that operator arity is satisfied and parameter references look well-formed.
        /// Does not require a live data source connection.
        /// </summary>
        public IErrorsInfo ValidateRuleSemantic(string rule)
        {
            try
            {
                var parsed = RuleNormalizer.Normalize(rule);
                if (!parsed.IsValid || string.IsNullOrWhiteSpace(parsed.Operator))
                    return new ErrorsInfo { Flag = Errors.Ok, Message = "Semantic check skipped (no parsed operator)" };

                // Arity constraints for well-known operators
                var arityMin = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                {
                    ["LOOKUP"]       = 2, ["ENTITYLOOKUP"] = 2,
                    ["ADDDAYS"]      = 2, ["ADDHOURS"] = 2, ["ADDMINUTES"] = 2,
                    ["FORMAT"]       = 2,
                    ["SEQUENCE"]     = 1, ["INCREMENT"] = 1,
                    ["RANDOM"]       = 1, ["RANDOMVALUE"] = 1,
                    ["QUERY"]        = 2,
                    ["GETENTITY"]    = 1,
                    ["COUNT"]        = 1, ["SUM"] = 1, ["AVG"] = 1, ["MIN"] = 1, ["MAX"] = 1,
                    ["ADD"]          = 2, ["SUBTRACT"] = 2, ["SUB"] = 2,
                    ["MULTIPLY"]     = 2, ["MUL"]      = 2,
                    ["DIVIDE"]       = 2, ["DIV"]      = 2,
                    ["ROUND"]        = 1,
                    ["IF"]           = 3,
                    ["EQ"]           = 2, ["NE"]  = 2, ["GT"] = 2, ["GTE"] = 2, ["LT"] = 2, ["LTE"] = 2,
                    ["AND"]          = 2, ["OR"] = 2,
                    ["NOT"]          = 1,
                };

                if (arityMin.TryGetValue(parsed.Operator, out int minArgs))
                {
                    int actualArgs = parsed.Args?.Count ?? 0;
                    if (actualArgs < minArgs)
                        return CreateError($"[SEMANTIC] Operator '{parsed.Operator}' requires at least {minArgs} argument(s) but got {actualArgs}");
                }

                // Warn on @Param references that don't look like valid identifiers
                if (parsed.Args != null)
                {
                    foreach (var arg in parsed.Args)
                    {
                        if (arg != null && arg.StartsWith("@"))
                        {
                            var paramName = arg.Substring(1);
                            if (string.IsNullOrWhiteSpace(paramName) || paramName.Any(c => !char.IsLetterOrDigit(c) && c != '_'))
                                return new ErrorsInfo
                                {
                                    Flag = Errors.Warning,
                                    Message = $"[SEMANTIC] Parameter reference '{arg}' contains invalid characters"
                                };
                        }
                    }
                }

                if (!IsKnownRulePattern(parsed.NormalizedRule ?? rule))
                {
                    return new ErrorsInfo
                    {
                        Flag = Errors.Warning,
                        Message = $"[SEMANTIC] Unknown rule pattern — may not be handled by any registered resolver: '{Redact(rule)}'"
                    };
                }

                return new ErrorsInfo { Flag = Errors.Ok, Message = "Semantic ok" };
            }
            catch (Exception ex)
            {
                return CreateError($"[SEMANTIC] Exception during semantic validation: {ex.Message}");
            }
        }

        // ── Tier 3 – Safety ──────────────────────────────────────────────────

        /// <summary>
        /// Rejects rules that carry dangerous side-effect patterns (DDL, shell calls, etc.).
        /// Runs on the normalized rule to catch encoded attempts.
        /// </summary>
        public IErrorsInfo ValidateRuleSafety(string rule)
        {
            try
            {
                var normalized = RuleNormalizer.GetNormalizedRule(rule, out _);
                var check = normalized ?? rule;

                if (ContainsPotentialSecurityRisk(check))
                    return CreateError($"[SAFETY] Rule contains potentially unsafe content and has been rejected: '{Redact(rule)}'");

                // Reject raw SQL injection patterns: semicolons separating statements
                if (check.Contains(";") && (check.ToUpperInvariant().Contains("SELECT") ||
                                            check.ToUpperInvariant().Contains("INSERT") ||
                                            check.ToUpperInvariant().Contains("UPDATE")))
                    return CreateError($"[SAFETY] Rule appears to contain SQL injection patterns: '{Redact(rule)}'");

                return new ErrorsInfo { Flag = Errors.Ok, Message = "Safety ok" };
            }
            catch (Exception ex)
            {
                return CreateError($"[SAFETY] Exception during safety validation: {ex.Message}");
            }
        }

        /// <summary>Redacts rule value for log output — truncates and masks middle to avoid leaking sensitive literals.</summary>
        private static string Redact(string rule)
        {
            if (rule == null) return "(null)";
            if (rule.Length <= 20) return rule;
            return rule.Substring(0, 10) + "…(redacted)…" + rule.Substring(rule.Length - 5);
        }

        public IErrorsInfo ValidateFieldName(string dataSourceName, string FieldName)
        {
            var errorInfo = new ErrorsInfo { Flag = Errors.Ok };

            try
            {
                if (string.IsNullOrWhiteSpace(dataSourceName))
                {
                    return CreateError("Data source name cannot be empty");
                }

                if (string.IsNullOrWhiteSpace(FieldName))
                {
                    return CreateError("Field name cannot be empty");
                }

                // Check if data source exists
                var dataSource = _editor.GetDataSource(dataSourceName);
                if (dataSource == null)
                {
                    return CreateError($"Data source '{dataSourceName}' not found");
                }

                // Validate field name format
                if (!IsValidPropertyName(FieldName))
                {
                    return CreateError($"Invalid field name format: '{FieldName}'");
                }

                errorInfo.Message = $"Field validation passed for '{FieldName}' in '{dataSourceName}'";
                return errorInfo;
            }
            catch (Exception ex)
            {
                return CreateError($"Field validation error: {ex.Message}");
            }
        }

        /// <summary>
        /// Validates consistency between property type and the actual configuration
        /// </summary>
        /// <param name="defaultValue">The default value to validate</param>
        /// <returns>Validation result</returns>
        private IErrorsInfo ValidatePropertyTypeConsistency(DefaultValue defaultValue)
        {
            var errorInfo = new ErrorsInfo { Flag = Errors.Ok };

            var hasValue = !IsEmptyValue(defaultValue.PropertyValue);
            var hasRule = !string.IsNullOrWhiteSpace(defaultValue.Rule);

            // Check if the property type matches the configuration
            switch (defaultValue.propertyType)
            {
                case TheTechIdea.Beep.Utilities.DefaultValueType.Static:
                case TheTechIdea.Beep.Utilities.DefaultValueType.ReplaceValue:
                    if (!hasValue)
                    {
                        errorInfo.Flag = Errors.Warning;
                        errorInfo.Message = $"Property type '{defaultValue.propertyType}' expects a static value but none is provided";
                    }
                    break;

                case TheTechIdea.Beep.Utilities.DefaultValueType.Rule:
                case TheTechIdea.Beep.Utilities.DefaultValueType.Expression:
                case TheTechIdea.Beep.Utilities.DefaultValueType.Function:
                case TheTechIdea.Beep.Utilities.DefaultValueType.Computed:
                case TheTechIdea.Beep.Utilities.DefaultValueType.Conditional:
                case TheTechIdea.Beep.Utilities.DefaultValueType.CurrentDateTime:
                case TheTechIdea.Beep.Utilities.DefaultValueType.CurrentUser:
                case TheTechIdea.Beep.Utilities.DefaultValueType.GenerateUniqueId:
                    if (!hasRule)
                    {
                        errorInfo.Flag = Errors.Warning;
                        errorInfo.Message = $"Property type '{defaultValue.propertyType}' expects a rule but none is provided";
                    }
                    break;
            }

            // Warn if both rule and value are provided (rule takes precedence)
            if (hasValue && hasRule && errorInfo.Flag == Errors.Ok)
            {
                errorInfo.Flag = Errors.Warning;
                errorInfo.Message = "Both static value and rule are provided. Rule will take precedence.";
            }

            return errorInfo;
        }

        /// <summary>
        /// Checks if a value is considered empty
        /// </summary>
        /// <param name="value">Value to check</param>
        /// <returns>True if the value is considered empty</returns>
        private bool IsEmptyValue(object value)
        {
            if (value == null)
                return true;

            if (value is string stringValue)
                return string.IsNullOrWhiteSpace(stringValue);

            if (value is Array arrayValue)
                return arrayValue.Length == 0;

            // For value types, check if it's the default value
            if (value.GetType().IsValueType)
                return value.Equals(Activator.CreateInstance(value.GetType()));

            return false;
        }

        private IErrorsInfo CreateError(string message)
        {
            return new ErrorsInfo
            {
                Flag = Errors.Failed,
                Message = message
            };
        }

        private bool IsValidPropertyName(string propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                return false;

            // Allow entity.property format
            if (propertyName.Contains('.'))
            {
                var parts = propertyName.Split('.');
                return parts.Length == 2 && 
                       IsValidIdentifier(parts[0]) && 
                       IsValidIdentifier(parts[1]);
            }

            return IsValidIdentifier(propertyName);
        }

        private bool IsValidIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                return false;

            // Check if identifier starts with letter or underscore
            if (!char.IsLetter(identifier[0]) && identifier[0] != '_')
                return false;

            // Check if all characters are valid (letters, digits, underscores)
            foreach (char c in identifier)
            {
                if (!char.IsLetterOrDigit(c) && c != '_')
                    return false;
            }

            return true;
        }

        private bool IsValidRuleSyntax(string rule)
        {
            if (string.IsNullOrWhiteSpace(rule))
                return false;

            // Basic checks for obviously invalid syntax
            if (rule.Contains(";;") || rule.Contains("..") || rule.Contains(",,"))

                return false;

            // Check for invalid characters that shouldn't be in rules
            if (rule.Contains("\n") || rule.Contains("\r") || rule.Contains("\t"))
                return false;

            // Check for unterminated strings
            if (rule.ToCharArray().Count(c => c == '"') % 2 != 0 || rule.ToCharArray().Count(c => c == '\'') % 2 != 0)
                return false;

            return true;
        }

        private bool AreParenthesesBalanced(string rule)
        {
            int roundCount = 0, squareCount = 0, curlyCount = 0;

            foreach (char c in rule)
            {
                switch (c)
                {
                    case '(':
                        roundCount++;
                        break;
                    case ')':
                        roundCount--;
                        if (roundCount < 0) return false;
                        break;
                    case '[':
                        squareCount++;
                        break;
                    case ']':
                        squareCount--;
                        if (squareCount < 0) return false;
                        break;
                    case '{':
                        curlyCount++;
                        break;
                    case '}':
                        curlyCount--;
                        if (curlyCount < 0) return false;
                        break;
                }
            }

            return roundCount == 0 && squareCount == 0 && curlyCount == 0;
        }

        private bool ContainsPotentialSecurityRisk(string rule)
        {
            var upperRule = rule.ToUpperInvariant();
            
            // Check for potentially dangerous patterns
            var dangerousPatterns = new[]
            {
                "EXEC", "EXECUTE", "DROP", "DELETE", "TRUNCATE", "ALTER",
                "SCRIPT", "EVAL", "SYSTEM", "CMD", "SHELL", "POWERSHELL"
            };

            return dangerousPatterns.Any(pattern => upperRule.Contains(pattern));
        }

        private bool IsKnownRulePattern(string rule)
        {
            var upperRule = rule.ToUpperInvariant().Trim();

            // Dot-style DSL rules are considered known patterns when the parser accepts them.
            if (DotStyleRuleParser.IsDotStyleRule(rule))
                return true;

            // Simple value patterns
            var simplePatterns = new[]
            {
                "NOW", "TODAY", "YESTERDAY", "TOMORROW", "CURRENTDATE", "CURRENTTIME", "CURRENTDATETIME",
                "USERNAME", "USERID", "CURRENTUSER", "USEREMAIL",
                "MACHINENAME", "HOSTNAME", "VERSION", "APPVERSION",
                "NEWGUID", "GUID", "UUID", "GENERATEUNIQUEID"
            };

            foreach (var pattern in simplePatterns)
            {
                if (upperRule.Equals(pattern) || upperRule.StartsWith(pattern + ":"))
                    return true;
            }

            // Function patterns (with parameters)
            var functionPatterns = new[]
            {
                "ADDDAYS(", "ADDHOURS(", "ADDMINUTES(", "FORMAT(", 
                "SEQUENCE(", "INCREMENT(", "AUTOINCREMENT(",
                "RANDOM(", "RANDOMVALUE(", "CALCULATE(", "COMPUTE(",
                "LOOKUP(", "ENTITYLOOKUP(", "WEBSERVICE(", "API(",
                "CONFIGURATIONVALUE(", "ENVIRONMENTVARIABLE(", "ENV(",
                "SESSIONVALUE(", "SESSION(", "CACHEDVALUE(", "CACHE(",
                "PARENTVALUE(", "PARENT(", "CHILDVALUE(", "CHILD(",
                "ROLEBASEDVALUE(", "ROLE(", "LOCALIZEDVALUE(", "LOCALIZED(",
                "BUSINESSCALENDAR(", "BUSINESSDAY(", "WORKDAY(",
                "USERPREFERENCE(", "PREFERENCE(", "LOCATIONBASED(", "LOCATION(",
                "MLPREDICTION(", "PREDICT(", "STATISTICAL(", "STATS(",
                "WORKFLOWCONTEXT(", "WORKFLOW(", "AUDITVALUE(", "AUDIT(",
                "INHERITEDVALUE(", "INHERIT(", "TEMPLATE("
            };

            foreach (var pattern in functionPatterns)
            {
                if (upperRule.StartsWith(pattern))
                    return true;
            }

            // Prefixed patterns (key:value format)
            var prefixedPatterns = new[]
            {
                "CONFIGURATIONVALUE:", "CONFIG:", "APPSETTING:",
                "ENVIRONMENTVARIABLE:", "ENV:", "ENVIRONMENT:",
                "SESSIONVALUE:", "SESSION:", "USER:",
                "ENTITYLOOKUP:", "LOOKUP:", "TABLE:",
                "WEBSERVICE:", "API:", "HTTP:",
                "CACHEDVALUE:", "CACHE:",
                "PARENTVALUE:", "PARENT:",
                "CHILDVALUE:", "CHILD:",
                "ROLEBASEDVALUE:", "ROLE:", "PERMISSION:",
                "LOCALIZEDVALUE:", "LOCALIZED:", "CULTURE:",
                "BUSINESSCALENDAR:", "BUSINESSDAY:", "WORKDAY:",
                "USERPREFERENCE:", "PREFERENCE:", "SETTING:",
                "LOCATIONBASED:", "LOCATION:", "GEO:",
                "MLPREDICTION:", "PREDICT:", "AI:",
                "STATISTICAL:", "STATS:", "MATH:",
                "WORKFLOWCONTEXT:", "WORKFLOW:", "PROCESS:",
                "AUDITVALUE:", "AUDIT:", "LOG:",
                "INHERITEDVALUE:", "INHERIT:", "FROM:",
                "TEMPLATE:", "BASED:"
            };

            foreach (var pattern in prefixedPatterns)
            {
                if (upperRule.StartsWith(pattern))
                    return true;
            }

            // Expression patterns
            if (ContainsExpressionPatterns(upperRule))
                return true;

            return false;
        }

        private bool ContainsExpressionPatterns(string upperRule)
        {
            // Mathematical operators
            if (upperRule.Contains("+") || upperRule.Contains("-") || 
                upperRule.Contains("*") || upperRule.Contains("/") ||
                upperRule.Contains("%") || upperRule.Contains("^"))
                return true;

            // Comparison operators
            if (upperRule.Contains("=") || upperRule.Contains("<") || 
                upperRule.Contains(">") || upperRule.Contains("<=") ||
                upperRule.Contains(">=") || upperRule.Contains("!=") ||
                upperRule.Contains("<>"))
                return true;

            // Logical operators
            if (upperRule.Contains("AND") || upperRule.Contains("OR") || 
                upperRule.Contains("NOT") || upperRule.Contains("&&") ||
                upperRule.Contains("||") || upperRule.Contains("!"))
                return true;

            // Common functions
            var functionNames = new[]
            {
                "IF(", "CASE(", "WHEN(", "THEN(", "ELSE(",
                "MIN(", "MAX(", "AVG(", "SUM(", "COUNT(",
                "LEN(", "LENGTH(", "SUBSTRING(", "SUBSTR(",
                "UPPER(", "LOWER(", "TRIM(", "LTRIM(", "RTRIM(",
                "REPLACE(", "CONCAT(", "SPLIT("
            };

            return functionNames.Any(func => upperRule.Contains(func));
        }
    }
}