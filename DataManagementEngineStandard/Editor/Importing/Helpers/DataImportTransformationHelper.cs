using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Defaults;
using TheTechIdea.Beep.Editor.Defaults.Helpers;
using TheTechIdea.Beep.Editor.Defaults.Resolvers;
using TheTechIdea.Beep.Editor.Importing.Interfaces;
using TheTechIdea.Beep.Editor.Mapping;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Workflow.Mapping;
using TheTechIdea.Beep.Editor.ETL;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Helpers;

namespace TheTechIdea.Beep.Editor.Importing.Helpers
{
    /// <summary>
    /// Helper class for data import transformation operations
    /// </summary>
    public class DataImportTransformationHelper : IDataImportTransformationHelper, IDataImportTransformationOutcome
    {
        private readonly IDMEEditor _editor;

        public DataImportTransformationHelper(IDMEEditor editor)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        }

        /// <summary>
        /// Applies field filtering to a record
        /// </summary>
        public object ApplyFieldFiltering(object record, List<string> selectedFields)
        {
            if (record == null || selectedFields == null || !selectedFields.Any())
                return record;

            try
            {
                var filteredRecord = new Dictionary<string, object>();

                // Handle different record types
                if (record is IDictionary<string, object> dict)
                {
                    foreach (var field in selectedFields)
                    {
                        if (dict.ContainsKey(field))
                        {
                            filteredRecord[field] = dict[field];
                        }
                    }
                    return filteredRecord;
                }
                else
                {
                    // Handle object types using reflection
                    var recordType = record.GetType();
                    foreach (var field in selectedFields)
                    {
                        var property = recordType.GetProperty(field);
                        if (property != null && property.CanRead)
                        {
                            filteredRecord[field] = property.GetValue(record);
                        }
                    }
                    return filteredRecord;
                }
            }
            catch (Exception ex)
            {
                throw new ImportTransformationException(ImportTransformationStage.Projection, ex.GetType().Name);
            }
        }

        /// <summary>
        /// Applies entity mapping transformations
        /// </summary>
        public object ApplyEntityMapping(object record, EntityDataMap mapping, string targetEntityName)
        {
            if (record == null || mapping == null)
                return record;

            try
            {
                var mappedEntity = mapping.MappedEntities?.FirstOrDefault(
                    p => p != null && string.Equals(p.EntityName, targetEntityName, StringComparison.InvariantCultureIgnoreCase));

                if (mappedEntity == null)
                {
                    throw new ImportTransformationException(ImportTransformationStage.Mapping);
                }

                // Use MappingManager for the actual transformation
                return MappingManager.MapObjectToAnotherStrict(_editor, targetEntityName, mappedEntity, record);
            }
            catch (ImportTransformationException) { throw; }
            catch (Exception ex)
            {
                throw new ImportTransformationException(ImportTransformationStage.Mapping, ex.GetType().Name);
            }
        }

        /// <summary>
        /// Applies default values to a record
        /// </summary>
        public object ApplyDefaultValues(object record, List<DefaultValue> defaultValues, 
            EntityStructure entityStructure, string dataSourceName)
            => ApplyDefaultValuesCore(record, defaultValues, entityStructure, dataSourceName, CancellationToken.None);

        private object ApplyDefaultValuesCore(object record, List<DefaultValue> defaultValues,
            EntityStructure entityStructure, string dataSourceName, CancellationToken token)
        {
            if (record == null || defaultValues == null || !defaultValues.Any(d => d == null || d.IsEnabled))
                return record;

            try
            {
                var definitions = DefaultValueHelper.CaptureRequired(defaultValues);
                using var resolverScope = definitions.Any(value => !string.IsNullOrWhiteSpace(value.Rule))
                    ? new RequiredResolverContext(
                        DefaultsManager.GetRequiredResolverRegistry(_editor), dataSourceName, definitions) : null;
                if (entityStructure?.Fields == null)
                    throw new ImportTransformationException(ImportTransformationStage.Defaults);
                foreach (var defaultValue in definitions)
                {
                    token.ThrowIfCancellationRequested();
                    if (defaultValue == null)
                        throw new ImportTransformationException(ImportTransformationStage.Defaults);
                    if (!defaultValue.IsEnabled) continue;
                    var propertyName = defaultValue.PropertyName;
                    var rule = defaultValue.Rule;
                    // Check if the field exists in the entity structure
                    var field = entityStructure.Fields?.FirstOrDefault(
                        f => f.FieldName.Equals(propertyName, StringComparison.InvariantCultureIgnoreCase));

                    if (field == null)
                        throw new ImportTransformationException(ImportTransformationStage.Defaults);

                    // Skip if field already has a value and we're not forcing defaults
                    var currentValue = RecordFieldAccess.Read(record, propertyName);
                    if (currentValue != null && !ShouldOverrideExistingValue(defaultValue, currentValue))
                        continue;

                    // Resolve the default value using DefaultsManager
                    var resolvedValue = string.IsNullOrWhiteSpace(rule)
                        ? DefaultValueHelper.CopyLiteralRequired(defaultValue.PropertyValue)
                        : DefaultsManager.ResolveRequired(_editor, rule, new PassedArgs
                        {
                            SentData = defaultValue,
                            ObjectName = "DefaultValue",
                            ReturnData = record
                        }, token);
                    if (resolvedValue == null && !string.IsNullOrWhiteSpace(rule))
                        throw new ImportTransformationException(ImportTransformationStage.Defaults);

                    // Set the resolved value
                    if (resolvedValue != null)
                    {
                        RecordFieldAccess.Write(record, propertyName, resolvedValue);
                    }
                }

                return record;
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                throw new ImportTransformationException(ImportTransformationStage.Defaults, ex.GetType().Name);
            }
        }

        /// <summary>
        /// Applies custom transformation function
        /// </summary>
        public object ApplyCustomTransformation(object record, Func<object, object> transformationFunction)
        {
            if (record == null || transformationFunction == null)
                return record;

            try
            {
                return transformationFunction(record) ?? throw new ImportTransformationException(ImportTransformationStage.Custom);
            }
            catch (Exception ex)
            {
                throw new ImportTransformationException(ImportTransformationStage.Custom, ex.GetType().Name);
            }
        }

        /// <summary>
        /// Applies complete transformation pipeline
        /// </summary>
        public object ApplyTransformationPipeline(object record, DataImportConfiguration config)
        {
            var result = TransformRecord(record, config, CancellationToken.None);
            if (!result.Succeeded)
                throw new ImportTransformationException(result.Stage, result.ExceptionType);
            return result.Record;
        }

        public ImportTransformationResult TransformRecord(object record, DataImportConfiguration config, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (record == null || config == null)
                return ImportTransformationResult.Failure(ImportTransformationStage.Input);

            var stage = ImportTransformationStage.Input;
            try
            {
                var transformedRecord = record;

                // Step 1: Apply field filtering if configured
                if (config.SelectedFields != null && config.SelectedFields.Any())
                {
                    stage = ImportTransformationStage.Projection;
                    transformedRecord = ApplyFieldFiltering(transformedRecord, config.SelectedFields);
                }
                token.ThrowIfCancellationRequested();

                // Step 2: Apply entity mapping if configured
                if (config.Mapping != null)
                {
                    stage = ImportTransformationStage.Mapping;
                    transformedRecord = ApplyEntityMapping(transformedRecord, config.Mapping, config.DestEntityName);
                }
                token.ThrowIfCancellationRequested();

                // Step 3: Apply default values if configured
                if (config.ApplyDefaults && config.DefaultValues != null && config.DefaultValues.Any())
                {
                    stage = ImportTransformationStage.Defaults;
                    transformedRecord = ApplyDefaultValuesCore(
                        transformedRecord, 
                        config.DefaultValues, 
                        config.DestEntityStructure, 
                        config.DestDataSourceName, token);
                }
                token.ThrowIfCancellationRequested();

                // Step 4: Apply custom transformation if provided
                if (config.CustomTransformation != null)
                {
                    stage = ImportTransformationStage.Custom;
                    transformedRecord = ApplyCustomTransformation(transformedRecord, config.CustomTransformation);
                }

                token.ThrowIfCancellationRequested();
                return transformedRecord == null
                    ? ImportTransformationResult.Failure(stage)
                    : ImportTransformationResult.Success(transformedRecord);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                token.ThrowIfCancellationRequested();
                return ImportTransformationResult.Failure(ex is ImportTransformationException stageFailure ? stageFailure.Stage : stage,
                    ex is ImportTransformationException failure ? failure.ExceptionType : ex.GetType().Name);
            }
        }

        /// <summary>
        /// Determines if an existing value should be overridden with a default value
        /// </summary>
        private bool ShouldOverrideExistingValue(DefaultValue defaultValue, object currentValue)
        {
            // Don't override non-null values unless specifically configured to do so
            if (currentValue != null)
            {
                // Check for empty strings, which we might want to override
                if (currentValue is string str && string.IsNullOrWhiteSpace(str))
                    return true;

                // Could add logic here for other "empty" values based on type
                // For now, preserve existing non-null values
                return false;
            }

            return true; // Override null values
        }

        /// <summary>
        /// Validates a transformation result
        /// </summary>
        public bool ValidateTransformationResult(object originalRecord, object transformedRecord, EntityStructure targetStructure)
        {
            if (transformedRecord == null)
                return false;

            if (targetStructure?.Fields == null)
                return true; // Can't validate without structure info

            try
            {
                // Check required fields are present
                var requiredFields = targetStructure.Fields.Where(f => f.IsRequired && !f.IsAutoIncrement).ToList();
                
                foreach (var requiredField in requiredFields)
                {
                    var value = _editor.Utilfunction.GetFieldValueFromObject(requiredField.FieldName, transformedRecord);
                    if (value == null || (value is string str && string.IsNullOrWhiteSpace(str)))
                    {
                        _editor.Logger?.WriteLog($"Required field '{requiredField.FieldName}' is missing or empty after transformation");
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _editor.Logger?.WriteLog($"Error validating transformation result: {ex.Message}");
                return false;
            }
        }
    }
}
