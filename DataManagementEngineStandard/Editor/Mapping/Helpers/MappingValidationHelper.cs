using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.Mapping.Helpers
{
    /// <summary>
    /// Helper class for validating mapping operations and configurations
    /// </summary>
    public static class MappingValidationHelper
    {
        /// <summary>
        /// Validates that source and destination types are suitable for mapping
        /// </summary>
        public static MappingValidationResult ValidateTypes<TSource, TDest>()
        {
            return ValidateTypes(typeof(TSource), typeof(TDest));
        }

        /// <summary>
        /// Validates that source and destination types are suitable for mapping
        /// </summary>
        public static MappingValidationResult ValidateTypes(Type sourceType, Type destType)
        {
            var result = new MappingValidationResult();

            if (sourceType == null)
            {
                result.AddError("Source type cannot be null");
                return result;
            }

            if (destType == null)
            {
                result.AddError("Destination type cannot be null");
                return result;
            }

            // Check for primitive types
            if (sourceType.IsPrimitive && destType.IsPrimitive)
            {
                if (sourceType != destType)
                {
                    result.AddWarning($"Mapping between different primitive types: {sourceType.Name} -> {destType.Name}");
                }
            }

            // Check for abstract or interface types
            if (destType.IsAbstract && !destType.IsInterface)
            {
                result.AddError($"Destination type {destType.Name} is abstract and cannot be instantiated");
            }

            if (destType.IsInterface)
            {
                result.AddError($"Destination type {destType.Name} is an interface and cannot be instantiated");
            }

            // Check for generic type definitions
            if (sourceType.IsGenericTypeDefinition)
            {
                result.AddError($"Source type {sourceType.Name} is a generic type definition");
            }

            if (destType.IsGenericTypeDefinition)
            {
                result.AddError($"Destination type {destType.Name} is a generic type definition");
            }

            // Check for circular references
            if (sourceType == destType)
            {
                result.AddWarning($"Source and destination types are the same: {sourceType.Name}");
            }

            return result;
        }

        /// <summary>
        /// Validates mapping configuration
        /// </summary>
        public static MappingValidationResult ValidateConfiguration<TSource, TDest>(
            Interfaces.ITypeMapBase typeMap, 
            AutoObjMapperOptions options)
        {
            var result = new MappingValidationResult();

            // Validate type compatibility first
            var typeValidation = ValidateTypes<TSource, TDest>();
            result.Merge(typeValidation);

            if (typeMap != null)
            {
                // Additional validation for custom resolvers could be added here
                // For example, checking if resolver delegates are not null
            }

            if (options == null)
            {
                result.AddError("Mapper options cannot be null");
            }

            return result;
        }

        /// <summary>
        /// Validates that an object instance is suitable for mapping
        /// </summary>
        public static MappingValidationResult ValidateInstance(object instance, Type expectedType)
        {
            var result = new MappingValidationResult();

            if (instance == null)
            {
                result.AddWarning("Instance is null");
                return result;
            }

            var actualType = instance.GetType();
            if (!expectedType.IsAssignableFrom(actualType))
            {
                result.AddError($"Instance type {actualType.Name} is not assignable to expected type {expectedType.Name}");
            }

            return result;
        }
    }

    /// <summary>
    /// Result of a mapping validation operation
    /// </summary>
    public class MappingValidationResult
    {
        private readonly List<string> _errors;
        private readonly List<string> _warnings;

        public MappingValidationResult()
        {
            _errors = new List<string>();
            _warnings = new List<string>();
        }

        /// <summary>
        /// Gets all validation errors
        /// </summary>
        public IReadOnlyList<string> Errors => _errors.AsReadOnly();

        /// <summary>
        /// Gets all validation warnings
        /// </summary>
        public IReadOnlyList<string> Warnings => _warnings.AsReadOnly();

        /// <summary>
        /// Indicates if validation passed (no errors)
        /// </summary>
        public bool IsValid => _errors.Count == 0;

        /// <summary>
        /// Indicates if there are any warnings
        /// </summary>
        public bool HasWarnings => _warnings.Count > 0;

        /// <summary>
        /// Adds an error to the validation result
        /// </summary>
        public void AddError(string error)
        {
            if (!string.IsNullOrWhiteSpace(error))
                _errors.Add(error);
        }

        /// <summary>
        /// Adds a warning to the validation result
        /// </summary>
        public void AddWarning(string warning)
        {
            if (!string.IsNullOrWhiteSpace(warning))
                _warnings.Add(warning);
        }

        /// <summary>
        /// Merges another validation result into this one
        /// </summary>
        public void Merge(MappingValidationResult other)
        {
            if (other == null) return;

            _errors.AddRange(other._errors);
            _warnings.AddRange(other._warnings);
        }

        /// <summary>
        /// Gets a summary of the validation result
        /// </summary>
        public override string ToString()
        {
            var summary = $"Validation: {(IsValid ? "PASSED" : "FAILED")}";
            
            if (_errors.Count > 0)
                summary += $", Errors: {_errors.Count}";
            
            if (_warnings.Count > 0)
                summary += $", Warnings: {_warnings.Count}";

            return summary;
        }
    }
}