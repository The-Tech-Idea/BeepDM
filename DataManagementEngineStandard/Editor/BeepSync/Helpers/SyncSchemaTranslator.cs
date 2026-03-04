using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Importing.Interfaces;
using TheTechIdea.Beep.Editor.Importing.ErrorStore;
using TheTechIdea.Beep.Editor.Importing.History;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Beep.Workflow.Mapping;

namespace TheTechIdea.Beep.Editor.BeepSync.Helpers
{
    /// <summary>
    /// Translates DataSyncSchema into DataImportConfiguration for execution by DataImportManager.
    /// </summary>
    public static class SyncSchemaTranslator
    {
        /// <summary>
        /// Converts a DataSyncSchema into a DataImportConfiguration that DataImportManager can execute.
        /// </summary>
        /// <param name="schema">The sync schema to translate</param>
        /// <param name="errorStore">Optional error store for quarantined records</param>
        /// <param name="historyStore">Optional run history store</param>
        /// <returns>A DataImportConfiguration ready for RunImportAsync</returns>
        public static DataImportConfiguration ToImportConfiguration(
            DataSyncSchema schema,
            IImportErrorStore errorStore = null,
            IImportRunHistoryStore historyStore = null)
        {
            if (schema == null)
                throw new ArgumentNullException(nameof(schema));

            var config = new DataImportConfiguration
            {
                SourceEntityName = schema.SourceEntityName ?? string.Empty,
                SourceDataSourceName = schema.SourceDataSourceName ?? string.Empty,
                DestEntityName = schema.DestinationEntityName ?? string.Empty,
                DestDataSourceName = schema.DestinationDataSourceName ?? string.Empty,
                SourceFilters = schema.Filters?.ToList() ?? new List<AppFilter>(),
                BatchSize = GetBatchSize(schema),
                SyncMode = MapSyncType(schema.SyncType),
                WatermarkColumn = schema.SourceSyncDataField ?? string.Empty,
                LastWatermarkValue = GetLastWatermarkValue(schema),
                UpsertKeyColumns = new List<string> { schema.DestinationSyncDataField ?? schema.SourceSyncDataField ?? string.Empty },
                ErrorStore = errorStore,
                RunHistoryStore = historyStore,
                CreateDestinationIfNotExists = true,
                ApplyDefaults = true
            };

            // Map FieldSyncData → EntityDataMap for transformation
            if (schema.MappedFields != null && schema.MappedFields.Count > 0)
            {
                config.Mapping = BuildEntityDataMap(schema);
            }

            return config;
        }

        /// <summary>
        /// Creates a reverse DataImportConfiguration for bidirectional sync (destination → source).
        /// </summary>
        public static DataImportConfiguration ToReverseImportConfiguration(
            DataSyncSchema schema,
            IImportErrorStore errorStore = null,
            IImportRunHistoryStore historyStore = null)
        {
            if (schema == null)
                throw new ArgumentNullException(nameof(schema));

            var config = new DataImportConfiguration
            {
                SourceEntityName = schema.DestinationEntityName ?? string.Empty,
                SourceDataSourceName = schema.DestinationDataSourceName ?? string.Empty,
                DestEntityName = schema.SourceEntityName ?? string.Empty,
                DestDataSourceName = schema.SourceDataSourceName ?? string.Empty,
                SourceFilters = schema.Filters?.ToList() ?? new List<AppFilter>(),
                BatchSize = GetBatchSize(schema),
                SyncMode = SyncMode.FullRefresh,
                UpsertKeyColumns = new List<string> { schema.SourceSyncDataField ?? string.Empty },
                ErrorStore = errorStore,
                RunHistoryStore = historyStore,
                CreateDestinationIfNotExists = false,
                ApplyDefaults = true
            };

            // Reverse field mappings: destination → source
            if (schema.MappedFields != null && schema.MappedFields.Count > 0)
            {
                config.Mapping = BuildReverseEntityDataMap(schema);
            }

            return config;
        }

        private static int GetBatchSize(DataSyncSchema schema)
        {
            // DataSyncSchema may have BatchSize property in future; use default 50 for now
            return 50;
        }

        private static object GetLastWatermarkValue(DataSyncSchema schema)
        {
            if (schema?.LastSyncDate == null || schema.LastSyncDate <= DateTime.MinValue)
                return null;
            return schema.LastSyncDate;
        }

        private static SyncMode MapSyncType(string syncType)
        {
            if (string.IsNullOrWhiteSpace(syncType))
                return SyncMode.FullRefresh;

            return syncType.Trim().ToUpperInvariant() switch
            {
                "INCREMENTAL" => SyncMode.Incremental,
                "UPSERT" => SyncMode.Upsert,
                "DELTA" => SyncMode.Incremental,
                _ => SyncMode.FullRefresh
            };
        }

        private static EntityDataMap BuildEntityDataMap(DataSyncSchema schema)
        {
            var map = new EntityDataMap
            {
                MappingName = $"BeepSync_{schema.SourceEntityName}_to_{schema.DestinationEntityName}",
                EntityName = schema.SourceEntityName,
                EntityDataSource = schema.SourceDataSourceName,
                MappedEntities = new List<EntityDataMap_DTL>()
            };

            var dtl = new EntityDataMap_DTL
            {
                EntityName = schema.DestinationEntityName,
                EntityDataSource = schema.DestinationDataSourceName,
                FieldMapping = new List<Mapping_rep_fields>()
            };

            foreach (var field in schema.MappedFields ?? Enumerable.Empty<FieldSyncData>())
            {
                if (string.IsNullOrWhiteSpace(field.SourceField) || string.IsNullOrWhiteSpace(field.DestinationField))
                    continue;

                dtl.FieldMapping.Add(new Mapping_rep_fields
                {
                    FromEntityName = schema.SourceEntityName,
                    FromFieldName = field.SourceField,
                    FromFieldType = field.SourceFieldType,
                    ToEntityName = schema.DestinationEntityName,
                    ToFieldName = field.DestinationField,
                    ToFieldType = field.DestinationFieldType
                });
            }

            // Include sync key field if not already in MappedFields
            var keyInMapped = schema.MappedFields?.Any(f =>
                string.Equals(f.SourceField, schema.SourceSyncDataField, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(f.DestinationField, schema.DestinationSyncDataField, StringComparison.OrdinalIgnoreCase)) ?? false;

            if (!keyInMapped && !string.IsNullOrWhiteSpace(schema.SourceSyncDataField) && !string.IsNullOrWhiteSpace(schema.DestinationSyncDataField))
            {
                dtl.FieldMapping.Add(new Mapping_rep_fields
                {
                    FromEntityName = schema.SourceEntityName,
                    FromFieldName = schema.SourceSyncDataField,
                    ToEntityName = schema.DestinationEntityName,
                    ToFieldName = schema.DestinationSyncDataField
                });
            }

            map.MappedEntities.Add(dtl);
            return map;
        }

        private static EntityDataMap BuildReverseEntityDataMap(DataSyncSchema schema)
        {
            var map = new EntityDataMap
            {
                MappingName = $"BeepSync_Reverse_{schema.DestinationEntityName}_to_{schema.SourceEntityName}",
                EntityName = schema.DestinationEntityName,
                EntityDataSource = schema.DestinationDataSourceName,
                MappedEntities = new List<EntityDataMap_DTL>()
            };

            var dtl = new EntityDataMap_DTL
            {
                EntityName = schema.SourceEntityName,
                EntityDataSource = schema.SourceDataSourceName,
                FieldMapping = new List<Mapping_rep_fields>()
            };

            foreach (var field in schema.MappedFields ?? Enumerable.Empty<FieldSyncData>())
            {
                if (string.IsNullOrWhiteSpace(field.SourceField) || string.IsNullOrWhiteSpace(field.DestinationField))
                    continue;

                dtl.FieldMapping.Add(new Mapping_rep_fields
                {
                    FromEntityName = schema.DestinationEntityName,
                    FromFieldName = field.DestinationField,
                    FromFieldType = field.DestinationFieldType,
                    ToEntityName = schema.SourceEntityName,
                    ToFieldName = field.SourceField,
                    ToFieldType = field.SourceFieldType
                });
            }

            if (!string.IsNullOrWhiteSpace(schema.DestinationSyncDataField) && !string.IsNullOrWhiteSpace(schema.SourceSyncDataField))
            {
                dtl.FieldMapping.Add(new Mapping_rep_fields
                {
                    FromEntityName = schema.DestinationEntityName,
                    FromFieldName = schema.DestinationSyncDataField,
                    ToEntityName = schema.SourceEntityName,
                    ToFieldName = schema.SourceSyncDataField
                });
            }

            map.MappedEntities.Add(dtl);
            return map;
        }
    }
}
