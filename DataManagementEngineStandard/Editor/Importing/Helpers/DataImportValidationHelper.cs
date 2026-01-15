using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Importing.Interfaces;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Workflow.Mapping;

namespace TheTechIdea.Beep.Editor.Importing.Helpers
{
    /// <summary>
    /// Helper class for data import validation operations
    /// </summary>
    public class DataImportValidationHelper : IDataImportValidationHelper
    {
        private readonly IDMEEditor _editor;

        public DataImportValidationHelper(IDMEEditor editor)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        }

        /// <summary>
        /// Validates import configuration before execution
        /// </summary>
        public IErrorsInfo ValidateImportConfiguration(DataImportConfiguration config)
        {
            if (config == null)
                return CreateErrorsInfo(Errors.Failed, "Import configuration cannot be null");

            // Validate basic configuration
            if (string.IsNullOrEmpty(config.SourceEntityName))
                return CreateErrorsInfo(Errors.Failed, "Source entity name is required");

            if (string.IsNullOrEmpty(config.DestEntityName))
                return CreateErrorsInfo(Errors.Failed, "Destination entity name is required");

            if (string.IsNullOrEmpty(config.SourceDataSourceName))
                return CreateErrorsInfo(Errors.Failed, "Source data source name is required");

            if (string.IsNullOrEmpty(config.DestDataSourceName))
                return CreateErrorsInfo(Errors.Failed, "Destination data source name is required");

            // Validate data sources
            if (config.SourceData == null && config.DestData == null)
            {
                var dataSourceValidation = ValidateDataSourceNames(config.SourceDataSourceName, config.DestDataSourceName);
                if (dataSourceValidation.Flag == Errors.Failed)
                    return dataSourceValidation;
            }
            else if (config.SourceData != null && config.DestData != null)
            {
                var dataSourceValidation = ValidateDataSources(config.SourceData, config.DestData);
                if (dataSourceValidation.Flag == Errors.Failed)
                    return dataSourceValidation;
            }

            // Validate entity structures if provided
            if (config.SourceEntityStructure != null && config.DestEntityStructure != null)
            {
                var entityValidation = ValidateEntityCompatibility(config.SourceEntityStructure, config.DestEntityStructure);
                if (entityValidation.Flag == Errors.Failed)
                    return entityValidation;
            }

            // Validate batch size
            if (config.BatchSize <= 0)
                return CreateErrorsInfo(Errors.Failed, "Batch size must be greater than 0");

            // Validate mapping if provided
            if (config.Mapping != null)
            {
                var mappingValidation = ValidateEntityMapping(config.Mapping);
                if (mappingValidation.Flag == Errors.Failed)
                    return mappingValidation;
            }

            return CreateErrorsInfo(Errors.Ok, "Import configuration is valid");
        }

        /// <summary>
        /// Validates entity mapping configuration
        /// </summary>
        public IErrorsInfo ValidateEntityMapping(EntityDataMap mapping)
        {
            if (mapping == null)
                return CreateErrorsInfo(Errors.Failed, "Entity mapping cannot be null");

            if (mapping.MappedEntities == null || !mapping.MappedEntities.Any())
                return CreateErrorsInfo(Errors.Failed, "Entity mapping must contain at least one mapped entity");

            foreach (var mappedEntity in mapping.MappedEntities)
            {
                if (string.IsNullOrEmpty(mappedEntity.EntityName))
                    return CreateErrorsInfo(Errors.Failed, "Mapped entity name cannot be empty");

                if (mappedEntity.EntityFields == null || !mappedEntity.EntityFields.Any())
                    return CreateErrorsInfo(Errors.Failed, $"Mapped entity '{mappedEntity.EntityName}' must have field mappings");

                // Validate field mappings
                foreach (var fieldMap in mappedEntity.EntityFields    )
                {
                    if (string.IsNullOrEmpty(fieldMap.FieldName))
                        return CreateErrorsInfo(Errors.Failed, $"Field name cannot be empty in entity '{mappedEntity.EntityName}'");
                }
            }

            return CreateErrorsInfo(Errors.Ok, "Entity mapping is valid");
        }

        /// <summary>
        /// Validates source and destination entity compatibility
        /// </summary>
        public IErrorsInfo ValidateEntityCompatibility(EntityStructure sourceEntity, EntityStructure destEntity)
        {
            if (sourceEntity == null)
                return CreateErrorsInfo(Errors.Failed, "Source entity structure cannot be null");

            if (destEntity == null)
                return CreateErrorsInfo(Errors.Failed, "Destination entity structure cannot be null");

            if (sourceEntity.Fields == null || !sourceEntity.Fields.Any())
                return CreateErrorsInfo(Errors.Failed, "Source entity must have at least one field");

            if (destEntity.Fields == null || !destEntity.Fields.Any())
                return CreateErrorsInfo(Errors.Failed, "Destination entity must have at least one field");

            // Check for compatible fields (at least some overlap)
            var sourceFieldNames = sourceEntity.Fields.Select(f => f.FieldName.ToLowerInvariant()).ToHashSet();
            var destFieldNames = destEntity.Fields.Select(f => f.FieldName.ToLowerInvariant()).ToHashSet();

            var commonFields = sourceFieldNames.Intersect(destFieldNames);
            if (!commonFields.Any())
            {
                return CreateErrorsInfo(Errors.Failed, "No compatible fields found between source and destination entities");
            }

            // Validate required destination fields can be populated
            var requiredDestFields = destEntity.Fields.Where(f => f.IsRequired && !f.IsAutoIncrement).ToList();
            foreach (var requiredField in requiredDestFields)
            {
                var FieldName = requiredField.FieldName.ToLowerInvariant();
                if (!sourceFieldNames.Contains(FieldName))
                {
                    // Check if there's a default value configured for this field
                    // This would require integration with DefaultsManager
                    _editor.Logger?.WriteLog($"Warning: Required field '{requiredField.FieldName}' not found in source. " +
                                          "Ensure default value is configured or mapping is provided.");
                }
            }

            return CreateErrorsInfo(Errors.Ok, $"Entities are compatible. Found {commonFields.Count()} matching fields.");
        }

        /// <summary>
        /// Validates data source connections
        /// </summary>
        public IErrorsInfo ValidateDataSources(IDataSource sourceDataSource, IDataSource destDataSource)
        {
            if (sourceDataSource == null)
                return CreateErrorsInfo(Errors.Failed, "Source data source cannot be null");

            if (destDataSource == null)
                return CreateErrorsInfo(Errors.Failed, "Destination data source cannot be null");

            // Check connection status
            if (sourceDataSource.ConnectionStatus != System.Data.ConnectionState.Open)
            {
                return CreateErrorsInfo(Errors.Failed, 
                    $"Source data source connection is not open. Status: {sourceDataSource.ConnectionStatus}");
            }

            if (destDataSource.ConnectionStatus != System.Data.ConnectionState.Open)
            {
                return CreateErrorsInfo(Errors.Failed, 
                    $"Destination data source connection is not open. Status: {destDataSource.ConnectionStatus}");
            }

            return CreateErrorsInfo(Errors.Ok, "Data sources are valid and connected");
        }

        /// <summary>
        /// Validates data source names and their existence
        /// </summary>
        private IErrorsInfo ValidateDataSourceNames(string sourceDataSourceName, string destDataSourceName)
        {
            if (!_editor.CheckDataSourceExist(sourceDataSourceName))
                return CreateErrorsInfo(Errors.Failed, $"Source data source '{sourceDataSourceName}' does not exist");

            if (!_editor.CheckDataSourceExist(destDataSourceName))
                return CreateErrorsInfo(Errors.Failed, $"Destination data source '{destDataSourceName}' does not exist");

            return CreateErrorsInfo(Errors.Ok, "Data source names are valid");
        }

        /// <summary>
        /// Creates an IErrorsInfo object with the specified flag and message
        /// </summary>
        private IErrorsInfo CreateErrorsInfo(Errors flag, string message)
        {
            return new ErrorsInfo
            {
                Flag = flag,
                Message = message
            };
        }
    }
}