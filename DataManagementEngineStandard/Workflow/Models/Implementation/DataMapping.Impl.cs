using System;
using System.Text.RegularExpressions;
using TheTechIdea.Beep.Workflow.Models;

namespace TheTechIdea.Beep.Workflow.Models.Base
{
    /// <summary>
    /// Implementation partial class for DataMapping entity - contains business logic methods
    /// </summary>
    public partial class DataMapping
    {
        /// <summary>
        /// Creates a deep clone of the data mapping
        /// </summary>
        public DataMapping Clone()
        {
            return new DataMapping
            {
                SourceModuleId = this.SourceModuleId,
                TargetModuleId = this.TargetModuleId,
                SourceField = this.SourceField,
                TargetField = this.TargetField,
                SourceFieldType = this.SourceFieldType,
                TargetFieldType = this.TargetFieldType,
                MappingType = this.MappingType,
                TransformationExpression = this.TransformationExpression,
                IsRequired = this.IsRequired,
                DefaultValue = this.DefaultValue,
                ValidationPattern = this.ValidationPattern,
                EnableValidation = this.EnableValidation,
                MappingOrder = this.MappingOrder,
                IsActive = this.IsActive,
                CreatedDate = this.CreatedDate,
                ModifiedDate = this.ModifiedDate,
                CreatedBy = this.CreatedBy,
                ModifiedBy = this.ModifiedBy
            };
        }

        /// <summary>
        /// Validates the data mapping configuration
        /// </summary>
        public ValidationResult Validate()
        {
            var result = new ValidationResult();

            // Validate required fields
            if (string.IsNullOrWhiteSpace(SourceField))
            {
                result.AddError("Source field is required");
            }

            if (string.IsNullOrWhiteSpace(TargetField))
            {
                result.AddError("Target field is required");
            }

            if (SourceModuleId <= 0)
            {
                result.AddError("Valid source module ID is required");
            }

            if (TargetModuleId <= 0)
            {
                result.AddError("Valid target module ID is required");
            }

            // Validate transformation expression if present
            if (!string.IsNullOrWhiteSpace(TransformationExpression))
            {
                if (MappingType == MappingType.Direct)
                {
                    result.AddWarning("Transformation expression specified but mapping type is Direct");
                }
            }

            // Validate validation pattern if present
            if (!string.IsNullOrWhiteSpace(ValidationPattern))
            {
                try
                {
                    Regex.Match("", ValidationPattern);
                }
                catch (ArgumentException)
                {
                    result.AddError("Invalid validation pattern regex");
                }
            }

            return result;
        }

        /// <summary>
        /// Applies transformation to the source value based on mapping configuration
        /// </summary>
        public object ApplyTransformation(object sourceValue)
        {
            if (sourceValue == null)
            {
                return DefaultValue;
            }

            switch (MappingType)
            {
                case MappingType.Direct:
                    return sourceValue;

                case MappingType.Transform:
                    return ApplyTransformationExpression(sourceValue);

                case MappingType.Lookup:
                    // Placeholder for lookup transformation
                    return sourceValue;

                case MappingType.Conditional:
                    // Placeholder for conditional transformation
                    return sourceValue;

                case MappingType.Custom:
                    // Placeholder for custom transformation
                    return sourceValue;

                default:
                    return sourceValue;
            }
        }

        /// <summary>
        /// Validates a mapped value against the mapping constraints
        /// </summary>
        public ValidationResult ValidateMappedValue(object value)
        {
            var result = new ValidationResult();

            if (!EnableValidation)
            {
                return result;
            }

            // Check required validation
            if (IsRequired && (value == null || (value is string str && string.IsNullOrWhiteSpace(str))))
            {
                result.AddError($"Required field '{TargetField}' is missing or empty");
                return result;
            }

            if (value == null)
            {
                return result;
            }

            // Validate against pattern if specified
            if (!string.IsNullOrWhiteSpace(ValidationPattern))
            {
                string stringValue = value.ToString();
                if (!Regex.IsMatch(stringValue, ValidationPattern))
                {
                    result.AddError($"Value '{stringValue}' does not match required pattern for field '{TargetField}'");
                }
            }

            // Type validation
            if (!string.IsNullOrWhiteSpace(TargetFieldType))
            {
                if (!ValidateType(value, TargetFieldType))
                {
                    result.AddError($"Value type does not match expected type '{TargetFieldType}' for field '{TargetField}'");
                }
            }

            return result;
        }

        /// <summary>
        /// Applies transformation expression to source value
        /// </summary>
        private object ApplyTransformationExpression(object sourceValue)
        {
            if (string.IsNullOrWhiteSpace(TransformationExpression))
            {
                return sourceValue;
            }

            string expression = TransformationExpression;
            string stringValue = sourceValue?.ToString() ?? "";

            // Simple placeholder transformations
            expression = expression.Replace("{source}", stringValue);
            expression = expression.Replace("{target}", ""); // Placeholder for target context

            // Basic transformations
            if (expression.Contains("ToUpper"))
            {
                return stringValue.ToUpper();
            }
            else if (expression.Contains("ToLower"))
            {
                return stringValue.ToLower();
            }
            else if (expression.Contains("Trim"))
            {
                return stringValue.Trim();
            }

            return expression;
        }

        /// <summary>
        /// Validates if value matches the expected type
        /// </summary>
        private bool ValidateType(object value, string expectedType)
        {
            if (value == null) return true;

            switch (expectedType.ToLower())
            {
                case "string":
                    return value is string;
                case "int":
                case "int32":
                    return int.TryParse(value.ToString(), out _);
                case "long":
                case "int64":
                    return long.TryParse(value.ToString(), out _);
                case "decimal":
                    return decimal.TryParse(value.ToString(), out _);
                case "double":
                    return double.TryParse(value.ToString(), out _);
                case "bool":
                case "boolean":
                    return bool.TryParse(value.ToString(), out _);
                case "datetime":
                    return DateTime.TryParse(value.ToString(), out _);
                default:
                    return true; // Unknown type, allow
            }
        }
    }
}
