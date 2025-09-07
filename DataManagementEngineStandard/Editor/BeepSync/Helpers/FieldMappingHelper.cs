using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor.BeepSync.Interfaces;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Editor.BeepSync.Helpers
{
    /// <summary>
    /// Helper class for field mapping operations
    /// Based on mapping patterns from DataSyncManager
    /// </summary>
    public class FieldMappingHelper : IFieldMappingHelper
    {
        private readonly IDMEEditor _editor;
        private readonly IDataSourceHelper _dataSourceHelper;

        /// <summary>
        /// Initializes a new instance of the FieldMappingHelper class
        /// </summary>
        /// <param name="editor">The DME editor instance</param>
        /// <param name="dataSourceHelper">The data source helper instance</param>
        public FieldMappingHelper(IDMEEditor editor, IDataSourceHelper dataSourceHelper)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
            _dataSourceHelper = dataSourceHelper ?? throw new ArgumentNullException(nameof(dataSourceHelper));
        }

        /// <summary>
        /// Map fields from source to destination object using field mappings
        /// Based on the MapFields method from DataSyncManager
        /// </summary>
        /// <param name="source">Source object</param>
        /// <param name="destination">Destination object</param>
        /// <param name="mappedFields">Field mapping definitions</param>
        public void MapFields(object source, object destination, IEnumerable<FieldSyncData> mappedFields)
        {
            if (source == null || destination == null || mappedFields == null)
            {
                _editor.AddLogMessage("BeepSync", "Cannot map fields: source, destination, or mappedFields is null", DateTime.Now, -1, "", Errors.Failed);
                return;
            }

            var sourceType = source.GetType();
            var destinationType = destination.GetType();

            foreach (var field in mappedFields)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(field.SourceField) || string.IsNullOrWhiteSpace(field.DestinationField))
                        continue;

                    // Get source property
                    var sourceProperty = sourceType.GetProperty(field.SourceField);
                    if (sourceProperty == null)
                    {
                        _editor.AddLogMessage("BeepSync", $"Source property '{field.SourceField}' not found", DateTime.Now, -1, "", Errors.Failed);
                        continue;
                    }

                    // Get destination property
                    var destinationProperty = destinationType.GetProperty(field.DestinationField);
                    if (destinationProperty == null || !destinationProperty.CanWrite)
                    {
                        _editor.AddLogMessage("BeepSync", $"Destination property '{field.DestinationField}' not found or not writable", DateTime.Now, -1, "", Errors.Failed);
                        continue;
                    }

                    // Get source value
                    var sourceValue = sourceProperty.GetValue(source);

                    // Convert value if necessary
                    var convertedValue = ConvertValue(sourceValue, destinationProperty.PropertyType, field);

                    // Set destination value
                    destinationProperty.SetValue(destination, convertedValue);
                }
                catch (Exception ex)
                {
                    _editor.AddLogMessage("BeepSync", $"Error mapping field '{field.SourceField}' to '{field.DestinationField}': {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                }
            }
        }

        /// <summary>
        /// Create destination entity instance
        /// Based on CreateDestinationEntity pattern from DataSyncManager
        /// </summary>
        /// <param name="dataSourceName">Name of the destination data source</param>
        /// <param name="entityName">Name of the entity</param>
        /// <returns>New entity instance</returns>
        public object CreateDestinationEntity(string dataSourceName, string entityName)
        {
            try
            {
                var dataSource = _dataSourceHelper.GetDataSource(dataSourceName);
                if (dataSource == null)
                    return null;

                var entityType = dataSource.GetEntityType(entityName);
                if (entityType == null)
                {
                    _editor.AddLogMessage("BeepSync", $"Entity type '{entityName}' not found in data source '{dataSourceName}'", DateTime.Now, -1, "", Errors.Failed);
                    return null;
                }

                return Activator.CreateInstance(entityType);
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage("BeepSync", $"Error creating destination entity '{entityName}': {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                return null;
            }
        }

        /// <summary>
        /// Auto-map fields based on name matching between source and destination
        /// </summary>
        /// <param name="sourceDataSource">Source data source name</param>
        /// <param name="sourceEntity">Source entity name</param>
        /// <param name="destDataSource">Destination data source name</param>
        /// <param name="destEntity">Destination entity name</param>
        /// <returns>List of auto-generated field mappings</returns>
        public List<FieldSyncData> AutoMapFields(string sourceDataSource, string sourceEntity, string destDataSource, string destEntity)
        {
            var mappings = new List<FieldSyncData>();

            try
            {
                var sourceDs = _dataSourceHelper.GetDataSource(sourceDataSource);
                var destDs = _dataSourceHelper.GetDataSource(destDataSource);

                if (sourceDs == null || destDs == null)
                    return mappings;

                // Get entity structures
                var sourceStructure = sourceDs.GetEntityStructure(sourceEntity, false);
                var destStructure = destDs.GetEntityStructure(destEntity, false);

                if (sourceStructure?.Fields == null || destStructure?.Fields == null)
                    return mappings;

                // Match fields by name (case-insensitive)
                foreach (var sourceField in sourceStructure.Fields)
                {
                    var matchingDestField = destStructure.Fields.FirstOrDefault(df => 
                        string.Equals(df.fieldname, sourceField.fieldname, StringComparison.OrdinalIgnoreCase));

                    if (matchingDestField != null)
                    {
                        mappings.Add(new FieldSyncData
                        {
                            ID = Guid.NewGuid().ToString(),
                            SourceField = sourceField.fieldname,
                            DestinationField = matchingDestField.fieldname,
                            SourceFieldType = sourceField.fieldtype,
                            DestinationFieldType = matchingDestField.fieldtype
                        });
                    }
                }

                _editor.AddLogMessage("BeepSync", $"Auto-mapped {mappings.Count} fields between {sourceEntity} and {destEntity}", DateTime.Now, -1, "", Errors.Ok);
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage("BeepSync", $"Error auto-mapping fields: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
            }

            return mappings;
        }

        /// <summary>
        /// Validate field mappings for correctness
        /// </summary>
        /// <param name="mappedFields">Field mappings to validate</param>
        /// <returns>Validation result</returns>
        public IErrorsInfo ValidateFieldMappings(IEnumerable<FieldSyncData> mappedFields)
        {
            var result = new ErrorsInfo { Flag = Errors.Ok };

            if (mappedFields == null)
            {
                result.Flag = Errors.Failed;
                result.Message = "Field mappings cannot be null";
                return result;
            }

            var errors = new List<string>();

            foreach (var mapping in mappedFields)
            {
                if (string.IsNullOrWhiteSpace(mapping.SourceField))
                    errors.Add($"Source field is empty for mapping ID: {mapping.ID}");

                if (string.IsNullOrWhiteSpace(mapping.DestinationField))
                    errors.Add($"Destination field is empty for mapping ID: {mapping.ID}");

                // Check for duplicate destination fields
                var duplicateDestFields = mappedFields
                    .Where(m => !string.IsNullOrWhiteSpace(m.DestinationField))
                    .GroupBy(m => m.DestinationField)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key);

                foreach (var duplicateField in duplicateDestFields)
                {
                    errors.Add($"Duplicate destination field: {duplicateField}");
                }
            }

            if (errors.Any())
            {
                result.Flag = Errors.Failed;
                result.Message = string.Join("; ", errors);
                result.Errors = errors.Select(e => (IErrorsInfo)new ErrorsInfo { Message = e }).ToList();
            }

            return result;
        }

        /// <summary>
        /// Convert value from source type to destination type with error handling
        /// </summary>
        /// <param name="value">Source value</param>
        /// <param name="targetType">Target type</param>
        /// <param name="field">Field mapping info for error reporting</param>
        /// <returns>Converted value</returns>
        private object ConvertValue(object value, Type targetType, FieldSyncData field)
        {
            if (value == null)
                return null;

            var sourceType = value.GetType();

            // If types match, return as-is
            if (sourceType == targetType)
                return value;

            // Handle nullable types
            var underlyingType = Nullable.GetUnderlyingType(targetType);
            if (underlyingType != null)
            {
                if (value == null)
                    return null;
                targetType = underlyingType;
            }

            try
            {
                // Common type conversions
                if (targetType == typeof(string))
                    return value.ToString();

                if (targetType == typeof(int))
                    return Convert.ToInt32(value);

                if (targetType == typeof(long))
                    return Convert.ToInt64(value);

                if (targetType == typeof(decimal))
                    return Convert.ToDecimal(value);

                if (targetType == typeof(double))
                    return Convert.ToDouble(value);

                if (targetType == typeof(float))
                    return Convert.ToSingle(value);

                if (targetType == typeof(bool))
                    return Convert.ToBoolean(value);

                if (targetType == typeof(DateTime))
                    return Convert.ToDateTime(value);

                if (targetType == typeof(Guid))
                    return Guid.Parse(value.ToString());

                // Default conversion attempt
                return Convert.ChangeType(value, targetType);
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage("BeepSync", $"Error converting value '{value}' from {sourceType.Name} to {targetType.Name} for field mapping '{field.SourceField}' -> '{field.DestinationField}': {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                return value; // Return original value if conversion fails
            }
        }
    }
}
