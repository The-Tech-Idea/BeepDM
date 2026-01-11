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
    /// Helper class for sync validation operations
    /// Based on validation patterns from DataSyncManager.ValidateSchema
    /// </summary>
    public class SyncValidationHelper : ISyncValidationHelper
    {
        private readonly IDMEEditor _editor;

        /// <summary>
        /// Initializes a new instance of the SyncValidationHelper class
        /// </summary>
        /// <param name="editor">The DME editor instance</param>
        public SyncValidationHelper(IDMEEditor editor)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        }

        /// <summary>
        /// Validate complete sync schema for all required fields and configurations
        /// Based on DataSyncManager.ValidateSchema method
        /// </summary>
        /// <param name="schema">The sync schema to validate</param>
        /// <returns>Validation result with detailed error information</returns>
        public IErrorsInfo ValidateSchema(DataSyncSchema schema)
        {
            var result = new ErrorsInfo { Flag = Errors.Ok };

            if (schema == null)
            {
                result.Flag = Errors.Failed;
                result.Message = "Schema cannot be null";
                return result;
            }

            var errors = new List<ErrorsInfo>();

            // Validate required string fields
            ValidateRequiredField(schema.SourceDataSourceName, "Source Data Source Name", errors);
            ValidateRequiredField(schema.DestinationDataSourceName, "Destination Data Source Name", errors);
            ValidateRequiredField(schema.SourceEntityName, "Source Entity Name", errors);
            ValidateRequiredField(schema.DestinationEntityName, "Destination Entity Name", errors);
            ValidateRequiredField(schema.SourceSyncDataField, "Source Sync Data Field", errors);
            ValidateRequiredField(schema.DestinationSyncDataField, "Destination Sync Data Field", errors);
            ValidateRequiredField(schema.SyncType, "Sync Type", errors);
            ValidateRequiredField(schema.SyncDirection, "Sync Direction", errors);

            // Validate data sources exist and are accessible
            var sourceValidation = ValidateDataSource(schema.SourceDataSourceName);
            if (sourceValidation.Flag == Errors.Failed)
                errors.Add(new ErrorsInfo { Message = $"Source data source validation failed: {sourceValidation.Message}" });

            var destValidation = ValidateDataSource(schema.DestinationDataSourceName);
            if (destValidation.Flag == Errors.Failed)
                errors.Add(new ErrorsInfo { Message = $"Destination data source validation failed: {destValidation.Message}" });

            // Validate entities exist in their respective data sources
            var sourceEntityValidation = ValidateEntity(schema.SourceDataSourceName, schema.SourceEntityName);
            if (sourceEntityValidation.Flag == Errors.Failed)
                errors.Add(new ErrorsInfo { Message = $"Source entity validation failed: {sourceEntityValidation.Message}" });

            var destEntityValidation = ValidateEntity(schema.DestinationDataSourceName, schema.DestinationEntityName);
            if (destEntityValidation.Flag == Errors.Failed)
                errors.Add(new ErrorsInfo { Message = $"Destination entity validation failed: {destEntityValidation.Message}" });

            // Set result based on errors found
            if (errors.Count > 0)
            {
                result.Flag = Errors.Failed;
                result.Errors = errors.Cast<IErrorsInfo>().ToList();
                result.Message = $"Schema validation failed with {errors.Count} error(s)";
            }
            else
            {
                result.Message = "Schema validation successful";
            }

            return result;
        }

        /// <summary>
        /// Validate that a data source exists and is accessible
        /// </summary>
        /// <param name="dataSourceName">Name of the data source</param>
        /// <returns>Validation result</returns>
        public IErrorsInfo ValidateDataSource(string dataSourceName)
        {
            var result = new ErrorsInfo { Flag = Errors.Ok };

            if (string.IsNullOrWhiteSpace(dataSourceName))
            {
                result.Flag = Errors.Failed;
                result.Message = "Data source name cannot be null or empty";
                return result;
            }

            try
            {
                if (!_editor.CheckDataSourceExist(dataSourceName))
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"Data source '{dataSourceName}' does not exist";
                }
                else
                {
                    result.Message = $"Data source '{dataSourceName}' validation successful";
                }
            }
            catch (Exception ex)
            {
                result.Flag = Errors.Failed;
                result.Message = $"Error validating data source '{dataSourceName}': {ex.Message}";
                _editor.AddLogMessage("BeepSync", result.Message, DateTime.Now, -1, "", Errors.Failed);
            }

            return result;
        }

        /// <summary>
        /// Validate that an entity exists in the specified data source
        /// </summary>
        /// <param name="dataSourceName">Name of the data source</param>
        /// <param name="entityName">Name of the entity</param>
        /// <returns>Validation result</returns>
        public IErrorsInfo ValidateEntity(string dataSourceName, string entityName)
        {
            var result = new ErrorsInfo { Flag = Errors.Ok };

            if (string.IsNullOrWhiteSpace(dataSourceName))
            {
                result.Flag = Errors.Failed;
                result.Message = "Data source name cannot be null or empty";
                return result;
            }

            if (string.IsNullOrWhiteSpace(entityName))
            {
                result.Flag = Errors.Failed;
                result.Message = "Entity name cannot be null or empty";
                return result;
            }

            try
            {
                var dataSource = _editor.GetDataSource(dataSourceName);
                if (dataSource == null)
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"Data source '{dataSourceName}' not found";
                    return result;
                }

                // Try to get entity structure to validate entity exists
                var entityStructure = dataSource.GetEntityStructure(entityName, false);
                if (entityStructure == null)
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"Entity '{entityName}' not found in data source '{dataSourceName}'";
                }
                else
                {
                    result.Message = $"Entity '{entityName}' validation successful in data source '{dataSourceName}'";
                }
            }
            catch (Exception ex)
            {
                result.Flag = Errors.Failed;
                result.Message = $"Error validating entity '{entityName}' in data source '{dataSourceName}': {ex.Message}";
                _editor.AddLogMessage("BeepSync", result.Message, DateTime.Now, -1, "", Errors.Failed);
            }

            return result;
        }

        /// <summary>
        /// Validate sync operation before execution - comprehensive pre-sync validation
        /// </summary>
        /// <param name="schema">The sync schema to validate for operation</param>
        /// <returns>Validation result</returns>
        public IErrorsInfo ValidateSyncOperation(DataSyncSchema schema)
        {
            var result = new ErrorsInfo { Flag = Errors.Ok };

            // First validate the schema itself
            var schemaValidation = ValidateSchema(schema);
            if (schemaValidation.Flag == Errors.Failed)
                return schemaValidation;

            var errors = new List<ErrorsInfo>();

            try
            {
                // Validate sync field exists in source entity
                var sourceDs = _editor.GetDataSource(schema.SourceDataSourceName);
                if (sourceDs != null)
                {
                    var sourceStructure = sourceDs.GetEntityStructure(schema.SourceEntityName, false);
                    if (sourceStructure?.Fields != null)
                    {
                        var sourceSyncField = sourceStructure.Fields.FirstOrDefault(f => 
                            string.Equals(f.fieldname, schema.SourceSyncDataField, StringComparison.OrdinalIgnoreCase));
                        
                        if (sourceSyncField == null)
                            errors.Add(new ErrorsInfo { Message = $"Source sync field '{schema.SourceSyncDataField}' not found in entity '{schema.SourceEntityName}'" });
                    }
                }

                // Validate sync field exists in destination entity
                var destDs = _editor.GetDataSource(schema.DestinationDataSourceName);
                if (destDs != null)
                {
                    var destStructure = destDs.GetEntityStructure(schema.DestinationEntityName, false);
                    if (destStructure?.Fields != null)
                    {
                        var destSyncField = destStructure.Fields.FirstOrDefault(f => 
                            string.Equals(f.fieldname, schema.DestinationSyncDataField, StringComparison.OrdinalIgnoreCase));
                        
                        if (destSyncField == null)
                            errors.Add(new ErrorsInfo { Message = $"Destination sync field '{schema.DestinationSyncDataField}' not found in entity '{schema.DestinationEntityName}'" });
                    }
                }

                // Validate field mappings if they exist
                if (schema.MappedFields != null && schema.MappedFields.Count > 0)
                {
                    var mappingHelper = new FieldMappingHelper(_editor);
                    var mappingValidation = mappingHelper.ValidateFieldMappings(schema.MappedFields);
                    if (mappingValidation.Flag == Errors.Failed)
                        errors.Add(new ErrorsInfo { Message = $"Field mapping validation failed: {mappingValidation.Message}" });
                }

                // Check if sync type is supported
                var supportedSyncTypes = new[] { "Full", "Incremental", "Delta", "Manual" };
                if (!supportedSyncTypes.Contains(schema.SyncType, StringComparer.OrdinalIgnoreCase))
                    errors.Add(new ErrorsInfo { Message = $"Unsupported sync type: {schema.SyncType}" });

                // Check if sync direction is supported
                var supportedDirections = new[] { "SourceToDestination", "DestinationToSource", "Bidirectional" };
                if (!supportedDirections.Contains(schema.SyncDirection, StringComparer.OrdinalIgnoreCase))
                    errors.Add(new ErrorsInfo { Message = $"Unsupported sync direction: {schema.SyncDirection}" });

            }
            catch (Exception ex)
            {
                errors.Add(new ErrorsInfo { Message = $"Error during sync operation validation: {ex.Message}" });
                _editor.AddLogMessage("BeepSync", $"Error during sync operation validation: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
            }

            // Set result based on errors found
            if (errors.Count > 0)
            {
                result.Flag = Errors.Failed;
                result.Errors = errors.Cast<IErrorsInfo>().ToList();
                result.Message = $"Sync operation validation failed with {errors.Count} error(s)";
            }
            else
            {
                result.Message = "Sync operation validation successful";
            }

            return result;
        }

        /// <summary>
        /// Helper method to validate required fields
        /// </summary>
        private void ValidateRequiredField(string fieldValue, string fieldName, List<ErrorsInfo> errors)
        {
            if (string.IsNullOrWhiteSpace(fieldValue))
                errors.Add(new ErrorsInfo { Message = $"{fieldName} is empty or null" });
        }
    }
}
