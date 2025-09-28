using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Defaults;
using TheTechIdea.Beep.Editor.Importing.Interfaces;
using TheTechIdea.Beep.Editor.Mapping;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Workflow.Mapping;
using TheTechIdea.Beep.Editor.ETL;
using TheTechIdea.Beep.Addin;

namespace TheTechIdea.Beep.Editor.Importing.Helpers
{
    /// <summary>
    /// Helper class for data import transformation operations
    /// </summary>
    public class DataImportTransformationHelper : IDataImportTransformationHelper
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
                _editor.Logger?.WriteLog($"Error applying field filtering: {ex.Message}");
                return record; // Return original record on error
            }
        }

        /// <summary>
        /// Applies entity mapping transformations
        /// </summary>
        public object ApplyEntityMapping(object record, EntityDataMap mapping, string targetEntityName)
        {
            if (record == null || mapping == null || string.IsNullOrEmpty(targetEntityName))
                return record;

            try
            {
                var mappedEntity = mapping.MappedEntities?.FirstOrDefault(
                    p => p.EntityName.Equals(targetEntityName, StringComparison.InvariantCultureIgnoreCase));

                if (mappedEntity == null)
                {
                    _editor.Logger?.WriteLog($"No mapping found for entity '{targetEntityName}'");
                    return record;
                }

                // Use MappingManager for the actual transformation
                return MappingManager.MapObjectToAnother(_editor, targetEntityName, mappedEntity, record);
            }
            catch (Exception ex)
            {
                _editor.Logger?.WriteLog($"Error applying entity mapping: {ex.Message}");
                return record; // Return original record on error
            }
        }

        /// <summary>
        /// Applies default values to a record
        /// </summary>
        public object ApplyDefaultValues(object record, List<DefaultValue> defaultValues, 
            EntityStructure entityStructure, string dataSourceName)
        {
            if (record == null || defaultValues == null || !defaultValues.Any() || entityStructure == null)
                return record;

            try
            {
                foreach (var defaultValue in defaultValues)
                {
                    // Check if the field exists in the entity structure
                    var field = entityStructure.Fields?.FirstOrDefault(
                        f => f.fieldname.Equals(defaultValue.PropertyName, StringComparison.InvariantCultureIgnoreCase));

                    if (field == null)
                        continue;

                    // Skip if field already has a value and we're not forcing defaults
                    var currentValue = _editor.Utilfunction.GetFieldValueFromObject(defaultValue.PropertyName, record);
                    if (currentValue != null && !ShouldOverrideExistingValue(defaultValue, currentValue))
                        continue;

                    // Resolve the default value using DefaultsManager
                    var resolvedValue = DefaultsManager.ResolveDefaultValue(
                        _editor, 
                        dataSourceName, 
                        defaultValue.PropertyName, 
                        new PassedArgs 
                        { 
                            SentData = defaultValue, 
                            ObjectName = "DefaultValue",
                            ReturnData = record
                        });

                    // Set the resolved value
                    if (resolvedValue != null)
                    {
                        _editor.Utilfunction.SetFieldValueFromObject(defaultValue.PropertyName, record, resolvedValue);
                    }
                }

                return record;
            }
            catch (Exception ex)
            {
                _editor.Logger?.WriteLog($"Error applying default values: {ex.Message}");
                return record; // Return original record on error
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
                return transformationFunction(record);
            }
            catch (Exception ex)
            {
                _editor.Logger?.WriteLog($"Error applying custom transformation: {ex.Message}");
                return record; // Return original record on error
            }
        }

        /// <summary>
        /// Applies complete transformation pipeline
        /// </summary>
        public object ApplyTransformationPipeline(object record, DataImportConfiguration config)
        {
            if (record == null || config == null)
                return record;

            try
            {
                var transformedRecord = record;

                // Step 1: Apply field filtering if configured
                if (config.SelectedFields != null && config.SelectedFields.Any())
                {
                    transformedRecord = ApplyFieldFiltering(transformedRecord, config.SelectedFields);
                }

                // Step 2: Apply entity mapping if configured
                if (config.Mapping != null && !string.IsNullOrEmpty(config.DestEntityName))
                {
                    transformedRecord = ApplyEntityMapping(transformedRecord, config.Mapping, config.DestEntityName);
                }

                // Step 3: Apply default values if configured
                if (config.ApplyDefaults && config.DefaultValues != null && config.DefaultValues.Any() && 
                    config.DestEntityStructure != null)
                {
                    transformedRecord = ApplyDefaultValues(
                        transformedRecord, 
                        config.DefaultValues, 
                        config.DestEntityStructure, 
                        config.DestDataSourceName);
                }

                // Step 4: Apply custom transformation if provided
                if (config.CustomTransformation != null)
                {
                    transformedRecord = ApplyCustomTransformation(transformedRecord, config.CustomTransformation);
                }

                return transformedRecord;
            }
            catch (Exception ex)
            {
                _editor.Logger?.WriteLog($"Error in transformation pipeline: {ex.Message}");
                return record; // Return original record on error
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
                    var value = _editor.Utilfunction.GetFieldValueFromObject(requiredField.fieldname, transformedRecord);
                    if (value == null || (value is string str && string.IsNullOrWhiteSpace(str)))
                    {
                        _editor.Logger?.WriteLog($"Required field '{requiredField.fieldname}' is missing or empty after transformation");
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