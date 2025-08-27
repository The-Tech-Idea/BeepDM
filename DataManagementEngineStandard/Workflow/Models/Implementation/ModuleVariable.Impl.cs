using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using TheTechIdea.Beep.Workflow.Models.Base;

namespace TheTechIdea.Beep.Workflow.Models.Base
{
    /// <summary>
    /// Implementation partial class for ModuleVariable - contains business logic methods
    /// </summary>
    public partial class ModuleVariable
    {
        /// <summary>
        /// Creates a new module variable
        /// </summary>
        public static ModuleVariable CreateVariable(
            int moduleId,
            string name,
            object value = null,
            string dataType = "String",
            string description = null,
            bool isRequired = false,
            VariableScope scope = VariableScope.Module)
        {
            return new ModuleVariable
            {
                ModuleId = moduleId,
                Name = name,
                Value = value,
                DefaultValue = value,
                DataType = dataType,
                Description = description,
                IsRequired = isRequired,
                Scope = scope,
                CreatedDate = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Sets the variable value with type conversion
        /// </summary>
        public void SetValue(object value)
        {
            Value = value;
            ModifiedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets the variable value with type conversion
        /// </summary>
        public T GetValue<T>()
        {
            if (Value == null) return default(T);

            try
            {
                if (typeof(T) == typeof(string))
                    return (T)(object)Value.ToString();

                if (typeof(T) == typeof(int) && Value is string str)
                    return (T)(object)int.Parse(str);

                if (typeof(T) == typeof(double) && Value is string str2)
                    return (T)(object)double.Parse(str2);

                if (typeof(T) == typeof(bool) && Value is string str3)
                    return (T)(object)bool.Parse(str3);

                return (T)Convert.ChangeType(Value, typeof(T));
            }
            catch
            {
                return default(T);
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

            // Check required fields
            if (string.IsNullOrWhiteSpace(Name))
                result.AddError("Variable name is required");

            if (IsRequired && (Value == null || string.IsNullOrWhiteSpace(Value.ToString())))
                result.AddError($"Variable '{Name}' is required but has no value");

            // Validate data type
            if (!string.IsNullOrWhiteSpace(DataType))
            {
                try
                {
                    if (Value != null)
                    {
                        switch (DataType.ToLower())
                        {
                            case "int":
                            case "integer":
                                int.Parse(Value.ToString());
                                break;
                            case "double":
                            case "decimal":
                                double.Parse(Value.ToString());
                                break;
                            case "bool":
                            case "boolean":
                                bool.Parse(Value.ToString());
                                break;
                            case "datetime":
                                DateTime.Parse(Value.ToString());
                                break;
                        }
                    }
                }
                catch
                {
                    result.AddError($"Variable '{Name}' value '{Value}' is not valid for data type '{DataType}'");
                }
            }

            // Validate pattern
            if (!string.IsNullOrWhiteSpace(ValidationPattern) && Value != null)
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(Value.ToString(), ValidationPattern))
                    result.AddError($"Variable '{Name}' value does not match required pattern");
            }

            // Validate range
            if (Value != null && !string.IsNullOrWhiteSpace(DataType))
            {
                try
                {
                    switch (DataType.ToLower())
                    {
                        case "int":
                        case "integer":
                            var intVal = int.Parse(Value.ToString());
                            if (MinValue != null && intVal < (int)MinValue)
                                result.AddError($"Variable '{Name}' value is below minimum value");
                            if (MaxValue != null && intVal > (int)MaxValue)
                                result.AddError($"Variable '{Name}' value is above maximum value");
                            break;
                        case "double":
                        case "decimal":
                            var doubleVal = double.Parse(Value.ToString());
                            if (MinValue != null && doubleVal < (double)MinValue)
                                result.AddError($"Variable '{Name}' value is below minimum value");
                            if (MaxValue != null && doubleVal > (double)MaxValue)
                                result.AddError($"Variable '{Name}' value is above maximum value");
                            break;
                    }
                }
                catch { }
            }

            // Validate allowed values
            if (!string.IsNullOrWhiteSpace(AllowedValues) && Value != null)
            {
                try
                {
                    var allowed = JsonSerializer.Deserialize<List<string>>(AllowedValues);
                    if (allowed != null && !allowed.Contains(Value.ToString()))
                        result.AddError($"Variable '{Name}' value is not in the list of allowed values");
                }
                catch { }
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
                result.AddError("Variable name cannot be empty");
                return result;
            }

            if (Name.Length > 200)
                result.AddError("Variable name cannot exceed 200 characters");

            if (!System.Text.RegularExpressions.Regex.IsMatch(Name, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
                result.AddError("Variable name must start with a letter or underscore and contain only alphanumeric characters and underscores");

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
            if (string.IsNullOrWhiteSpace(AllowedValues)) return new List<string>();

            try
            {
                return JsonSerializer.Deserialize<List<string>>(AllowedValues) ?? new List<string>();
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
            AllowedValues = values != null ? JsonSerializer.Serialize(values) : null;
            ModifiedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Creates a copy of the variable
        /// </summary>
        public ModuleVariable Clone()
        {
            return new ModuleVariable
            {
                ModuleId = ModuleId,
                Name = Name,
                Description = Description,
                DataType = DataType,
                Value = Value,
                DefaultValue = DefaultValue,
                IsRequired = IsRequired,
                IsOutput = IsOutput,
                IsInput = IsInput,
                ValidationPattern = ValidationPattern,
                MinValue = MinValue,
                MaxValue = MaxValue,
                AllowedValues = AllowedValues,
                DisplayName = DisplayName,
                Category = Category,
                DisplayOrder = DisplayOrder,
                Scope = Scope,
                CreatedDate = CreatedDate,
                CreatedBy = CreatedBy
            };
        }

        /// <summary>
        /// Gets a display-friendly representation of the variable
        /// </summary>
        public string GetDisplayValue()
        {
            if (!string.IsNullOrWhiteSpace(DisplayName)) return DisplayName;
            return Name;
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
