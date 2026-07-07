using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.BeepSync;
using TheTechIdea.Beep.Editor.Mapping;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Beep.Workflow.Mapping;

namespace TheTechIdea.Beep.Editor.Mapping.Adapters
{
    /// <summary>
    /// Adapter that bridges a <see cref="DataSyncSchema"/>'s mapped fields to the
    /// engine's <see cref="MappingManager"/> so the sync field mappings can be
    /// expressed as an <see cref="EntityDataMap_DTL"/> reused by CRUD / migration
    /// / import.
    /// </summary>
    /// <remarks>
    /// Phase 9 — Mapping ↔ BeepSync integration. The adapter is one-way (read-only)
    /// in this revision: it consumes the sync schema's mapped fields and produces
    /// an <see cref="EntityDataMap_DTL"/>; it does not write back to the schema.
    /// That keeps the schema the single source of truth for sync routing.
    /// </remarks>
    public static class BeepSyncMappingAdapter
    {
        /// <summary>
        /// Read the <paramref name="schema"/>'s <c>MappedFields</c> collection and
        /// project each <see cref="FieldSyncData"/> into a <see cref="Mapping_rep_fields"/>
        /// entry on a fresh <see cref="EntityDataMap_DTL"/>. The destination entity
        /// name becomes the map's <c>EntityName</c>.
        /// </summary>
        public static EntityDataMap_DTL FromSchema(DataSyncSchema schema)
        {
            if (schema == null) throw new ArgumentNullException(nameof(schema));

            var map = new EntityDataMap_DTL
            {
                EntityName = schema.DestinationEntityName
            };

            if (schema.MappedFields == null) return map;

            foreach (var mapped in schema.MappedFields)
            {
                if (mapped == null) continue;
                map.FieldMapping.Add(new Mapping_rep_fields
                {
                    ToFieldName   = mapped.DestinationField,
                    FromFieldName = mapped.SourceField
                });
            }
            return map;
        }

        /// <summary>
        /// Build a fresh <see cref="DataSyncSchema"/> whose <c>MappedFields</c> are
        /// derived from <paramref name="map"/>. Useful when the user authors a
        /// mapping in the Mapping UI and wants to push it into a sync schema.
        /// </summary>
        public static DataSyncSchema ToSchema(
            string schemaId,
            string sourceDataSource,
            string sourceEntity,
            string destinationDataSource,
            string destinationEntity,
            EntityDataMap_DTL map)
        {
            if (string.IsNullOrWhiteSpace(schemaId))
                throw new ArgumentException("schemaId required", nameof(schemaId));
            if (map == null) throw new ArgumentNullException(nameof(map));

            var schema = new DataSyncSchema
            {
                Id = schemaId,
                SourceDataSourceName = sourceDataSource,
                SourceEntityName = sourceEntity,
                DestinationDataSourceName = destinationDataSource,
                DestinationEntityName = destinationEntity
            };

            if (map.FieldMapping == null) return schema;

            foreach (var field in map.FieldMapping)
            {
                if (field == null) continue;
                schema.MappedFields.Add(new FieldSyncData
                {
                    SourceField      = field.FromFieldName,
                    DestinationField = field.ToFieldName
                });
            }
            return schema;
        }
    }
}
