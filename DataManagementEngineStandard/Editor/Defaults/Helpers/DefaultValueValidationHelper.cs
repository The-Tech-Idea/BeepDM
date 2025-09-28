using System;
using System.Linq;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Defaults.Interfaces;
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

        public IErrorsInfo ValidateRule(string rule)
        {
            var errorInfo = new ErrorsInfo { Flag = Errors.Ok };

            try
            {
                if (string.IsNullOrWhiteSpace(rule))
                {
                    errorInfo.Message = "No rule to validate";
                    return errorInfo;
                }

                // Basic rule syntax validation
                if (!IsValidRuleSyntax(rule))
                {
                    return CreateError($"Invalid rule syntax: '{rule}'");
                }

                // Check for balanced parentheses
                if (!AreParenthesesBalanced(rule))
                {
                    return CreateError($"Unbalanced parentheses in rule: '{rule}'");
                }

                // Check for security issues (basic)
                if (ContainsPotentialSecurityRisk(rule))
                {
                    return CreateError($"Rule contains potentially unsafe content: '{rule}'");
                }

                // Validate rule format patterns
                if (!IsKnownRulePattern(rule))
                {
                    _editor.AddLogMessage("DefaultValueValidationHelper", 
                        $"Warning: Unknown rule pattern '{rule}' - may not be resolvable", 
                        DateTime.Now, -1, "", Errors.Warning);
                    
                    errorInfo.Flag = Errors.Warning;
                    errorInfo.Message = $"Unknown rule pattern: '{rule}' - may not be resolvable";
                    return errorInfo;
                }

                errorInfo.Message = "Rule validation passed";
                return errorInfo;
            }
            catch (Exception ex)
            {
                return CreateError($"Rule validation error: {ex.Message}");
            }
        }

        public IErrorsInfo ValidateFieldName(string dataSourceName, string fieldName)
        {
            var errorInfo = new ErrorsInfo { Flag = Errors.Ok };

            try
            {
                if (string.IsNullOrWhiteSpace(dataSourceName))
                {
                    return CreateError("Data source name cannot be empty");
                }

                if (string.IsNullOrWhiteSpace(fieldName))
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
                if (!IsValidPropertyName(fieldName))
                {
                    return CreateError($"Invalid field name format: '{fieldName}'");
                }

                errorInfo.Message = $"Field validation passed for '{fieldName}' in '{dataSourceName}'";
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