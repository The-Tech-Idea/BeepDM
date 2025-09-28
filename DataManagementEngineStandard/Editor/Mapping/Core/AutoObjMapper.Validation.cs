using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Editor.Mapping.Helpers;

namespace TheTechIdea.Beep.Editor.Mapping
{
    /// <summary>
    /// AutoObjMapper - Validation and Error Handling functionality
    /// </summary>
    public sealed partial class AutoObjMapper
    {
        /// <summary>
        /// Validates types before mapping
        /// </summary>
        public MappingValidationResult ValidateMapping<TSource, TDest>()
        {
            var typeMap = _config.GetTypeMap(typeof(TSource), typeof(TDest));
            return MappingValidationHelper.ValidateConfiguration<TSource, TDest>(typeMap, _options);
        }

        /// <summary>
        /// Maps with validation - throws exception if validation fails
        /// </summary>
        public TDest MapWithValidation<TSource, TDest>(TSource source, TDest destination)
        {
            var validation = ValidateMapping<TSource, TDest>();
            if (!validation.IsValid)
            {
                throw new InvalidOperationException($"Mapping validation failed: {string.Join(", ", validation.Errors)}");
            }

            return Map(source, destination);
        }

        /// <summary>
        /// Maps with validation - throws exception if validation fails
        /// </summary>
        public TDest MapWithValidation<TSource, TDest>(TSource source) where TDest : new()
        {
            var validation = ValidateMapping<TSource, TDest>();
            if (!validation.IsValid)
            {
                throw new InvalidOperationException($"Mapping validation failed: {string.Join(", ", validation.Errors)}");
            }

            return Map<TSource, TDest>(source);
        }

        /// <summary>
        /// Safe mapping that returns result with success/error information
        /// </summary>
        public MappingResult<TDest> TryMap<TSource, TDest>(TSource source, TDest destination)
        {
            try
            {
                var validation = ValidateMapping<TSource, TDest>();
                if (!validation.IsValid)
                {
                    return MappingResult<TDest>.Failure(string.Join(", ", validation.Errors), validation.Warnings);
                }

                var result = Map(source, destination);
                return MappingResult<TDest>.Success(result, validation.Warnings);
            }
            catch (Exception ex)
            {
                return MappingResult<TDest>.Failure(ex.Message);
            }
        }

        /// <summary>
        /// Safe mapping that returns result with success/error information
        /// </summary>
        public MappingResult<TDest> TryMap<TSource, TDest>(TSource source) where TDest : new()
        {
            try
            {
                var validation = ValidateMapping<TSource, TDest>();
                if (!validation.IsValid)
                {
                    return MappingResult<TDest>.Failure(string.Join(", ", validation.Errors), validation.Warnings);
                }

                var result = Map<TSource, TDest>(source);
                return MappingResult<TDest>.Success(result, validation.Warnings);
            }
            catch (Exception ex)
            {
                return MappingResult<TDest>.Failure(ex.Message);
            }
        }
    }

    /// <summary>
    /// Result of a mapping operation
    /// </summary>
    public class MappingResult<T>
    {
        private MappingResult(bool success, T value, string error, IReadOnlyList<string> warnings)
        {
            IsSuccess = success;
            Value = value;
            Error = error;
            Warnings = warnings ?? new List<string>();
        }

        /// <summary>
        /// Indicates if the mapping was successful
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// The mapped value (valid only if IsSuccess is true)
        /// </summary>
        public T Value { get; }

        /// <summary>
        /// Error message (valid only if IsSuccess is false)
        /// </summary>
        public string Error { get; }

        /// <summary>
        /// Warnings from the mapping operation
        /// </summary>
        public IReadOnlyList<string> Warnings { get; }

        /// <summary>
        /// Creates a successful mapping result
        /// </summary>
        public static MappingResult<T> Success(T value, IReadOnlyList<string> warnings = null)
        {
            return new MappingResult<T>(true, value, null, warnings);
        }

        /// <summary>
        /// Creates a failed mapping result
        /// </summary>
        public static MappingResult<T> Failure(string error, IReadOnlyList<string> warnings = null)
        {
            return new MappingResult<T>(false, default, error, warnings);
        }
    }
}