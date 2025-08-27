using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TheTechIdea.Beep.Workflow.Models.Base;
using TheTechIdea.Beep.Workflow.Models;

namespace TheTechIdea.Beep.Workflow.Models.Base
{
    /// <summary>
    /// Implementation partial class for ScenarioVariable - contains business logic methods
    /// </summary>
    public partial class ScenarioVariable
    {
        /// <summary>
        /// Creates a new scenario variable
        /// </summary>
        public static ScenarioVariable CreateVariable(
            int scenarioId,
            string name,
            object value = null,
            string dataType = "String",
            string description = null,
            bool isRequired = false,
            VariableScope scope = VariableScope.Scenario)
        {
            return new ScenarioVariable
            {
                ScenarioId = scenarioId,
                Name = name,
                Value = value,
                DefaultValue = value,
                DataType = dataType,
                Description = description,
                IsRequired = isRequired,
                Scope = scope,
                DisplayName = name,
                CreatedDate = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Sets the variable value with type conversion
        /// </summary>
        public void SetValue(object value)
        {
            Value = ConvertValue(value, DataType);
            ModifiedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets the variable value with type conversion
        /// </summary>
        public T GetValue<T>()
        {
            if (Value == null) return default;

            try
            {
                return (T)ConvertValue(Value, typeof(T).Name);
            }
            catch
            {
                return default;
            }
        }

        /// <summary>
        /// Resets the variable to its default value
        /// </summary>
        public void ResetToDefault()
        {
            Value = DefaultValue;
            ModifiedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Validates the variable value
        /// </summary>
        public ValidationResult Validate()
        {
            var result = new ValidationResult();

            if (ScenarioId <= 0)
            {
                result.Errors.Add("Scenario ID must be greater than 0");
            }

            if (string.IsNullOrWhiteSpace(Name))
            {
                result.Errors.Add("Variable name is required");
            }

            if (Name?.Length > 200)
            {
                result.Errors.Add("Variable name cannot exceed 200 characters");
            }

            if (!string.IsNullOrWhiteSpace(ValidationPattern))
            {
                if (!IsValidPattern(Value?.ToString(), ValidationPattern))
                {
                    result.Errors.Add($"Variable '{Name}' does not match the required pattern");
                }
            }

            if (IsRequired && (Value == null || string.IsNullOrWhiteSpace(Value.ToString())))
            {
                result.Errors.Add($"Variable '{Name}' is required");
            }

            if (!IsValidDataType(DataType))
            {
                result.Errors.Add($"Variable '{Name}' has an invalid data type: {DataType}");
            }

            if (!IsValidRange(Value))
            {
                result.Errors.Add($"Variable '{Name}' value is outside the allowed range");
            }

            if (!IsValidAllowedValues(Value))
            {
                result.Errors.Add($"Variable '{Name}' value is not in the list of allowed values");
            }

            return result;
        }

        /// <summary>
        /// Validates the variable name format
        /// </summary>
        public ValidationResult ValidateName()
        {
            var result = new ValidationResult();

            if (string.IsNullOrWhiteSpace(Name))
            {
                result.Errors.Add("Variable name is required");
                return result;
            }

            if (!Regex.IsMatch(Name, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
            {
                result.Errors.Add("Variable name must start with a letter or underscore and contain only letters, numbers, and underscores");
            }

            if (Name.Length > 200)
            {
                result.Errors.Add("Variable name cannot exceed 200 characters");
            }

            return result;
        }

        /// <summary>
        /// Gets the variable value as a string
        /// </summary>
        public string GetValueAsString()
        {
            return Value?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// Gets the default value as a string
        /// </summary>
        public string GetDefaultValueAsString()
        {
            return DefaultValue?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// Checks if the variable has a value set
        /// </summary>
        public bool HasValue()
        {
            return Value != null && !string.IsNullOrWhiteSpace(Value.ToString());
        }

        /// <summary>
        /// Checks if the variable value has changed from default
        /// </summary>
        public bool HasChangedFromDefault()
        {
            if (Value == null && DefaultValue == null) return false;
            if (Value == null || DefaultValue == null) return true;
            return !Value.Equals(DefaultValue);
        }

        /// <summary>
        /// Gets the list of allowed values
        /// </summary>
        public List<string> GetAllowedValues()
        {
            if (string.IsNullOrWhiteSpace(AllowedValues))
            {
                return new List<string>();
            }

            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<List<string>>(AllowedValues);
            }
            catch
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// Sets the list of allowed values
        /// </summary>
        public void SetAllowedValues(List<string> values)
        {
            if (values != null && values.Any())
            {
                AllowedValues = System.Text.Json.JsonSerializer.Serialize(values);
            }
            else
            {
                AllowedValues = null;
            }
        }

        /// <summary>
        /// Converts a value to the specified data type
        /// </summary>
        private object ConvertValue(object value, string dataType)
        {
            if (value == null) return null;

            try
            {
                switch (dataType?.ToLower())
                {
                    case "string":
                        return value.ToString();
                    case "int":
                    case "integer":
                        return Convert.ToInt32(value);
                    case "long":
                        return Convert.ToInt64(value);
                    case "double":
                    case "decimal":
                        return Convert.ToDecimal(value);
                    case "float":
                        return Convert.ToSingle(value);
                    case "bool":
                    case "boolean":
                        return Convert.ToBoolean(value);
                    case "datetime":
                        return Convert.ToDateTime(value);
                    case "guid":
                        return Guid.Parse(value.ToString());
                    default:
                        return value.ToString();
                }
            }
            catch
            {
                return value.ToString();
            }
        }

        /// <summary>
        /// Validates if the data type is supported
        /// </summary>
        private bool IsValidDataType(string dataType)
        {
            if (string.IsNullOrWhiteSpace(dataType)) return false;

            var validTypes = new[] { "string", "int", "integer", "long", "double", "decimal", "float", "bool", "boolean", "datetime", "guid" };
            return validTypes.Contains(dataType.ToLower());
        }

        /// <summary>
        /// Validates if the value is within the allowed range
        /// </summary>
        private bool IsValidRange(object value)
        {
            if (value == null || (MinValue == null && MaxValue == null))
            {
                return true;
            }

            try
            {
                if (value is IComparable comparable)
                {
                    if (MinValue is IComparable min && comparable.CompareTo(min) < 0)
                    {
                        return false;
                    }

                    if (MaxValue is IComparable max && comparable.CompareTo(max) > 0)
                    {
                        return false;
                    }
                }
            }
            catch
            {
                // If comparison fails, consider it valid
            }

            return true;
        }

        /// <summary>
        /// Validates if the value is in the allowed values list
        /// </summary>
        private bool IsValidAllowedValues(object value)
        {
            if (value == null || string.IsNullOrWhiteSpace(AllowedValues))
            {
                return true;
            }

            var allowedValues = GetAllowedValues();
            if (!allowedValues.Any())
            {
                return true;
            }

            return allowedValues.Contains(value.ToString());
        }

        /// <summary>
        /// Validates if the value matches the validation pattern
        /// </summary>
        private bool IsValidPattern(string value, string pattern)
        {
            if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(pattern))
            {
                return true;
            }

            try
            {
                return Regex.IsMatch(value, pattern);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Creates a copy of the variable
        /// </summary>
        public ScenarioVariable Clone()
        {
            return new ScenarioVariable
            {
                ScenarioId = ScenarioId,
                Name = Name,
                Description = Description,
                DataType = DataType,
                Value = Value,
                DefaultValue = DefaultValue,
                IsRequired = IsRequired,
                IsGlobal = IsGlobal,
                Scope = Scope,
                ValidationPattern = ValidationPattern,
                MinValue = MinValue,
                MaxValue = MaxValue,
                AllowedValues = AllowedValues,
                DisplayName = DisplayName,
                Category = Category,
                DisplayOrder = DisplayOrder,
                CreatedDate = CreatedDate,
                CreatedBy = CreatedBy
            };
        }

        /// <summary>
        /// Gets a display-friendly representation of the variable
        /// </summary>
        public string GetDisplayValue()
        {
            if (Value == null) return string.Empty;

            if (DataType?.ToLower() == "password" || Name?.ToLower().Contains("password") == true)
            {
                return "***";
            }

            return GetValueAsString();
        }

        /// <summary>
        /// Updates the variable's metadata
        /// </summary>
        public void UpdateMetadata(string modifiedBy = null)
        {
            ModifiedDate = DateTime.UtcNow;
            ModifiedBy = modifiedBy;
        }
    }
}
