using System;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Importing;
using TheTechIdea.Beep.Editor.Mapping;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Beep.Workflow.Mapping;

namespace TheTechIdea.Beep.Editor.Mapping.Adapters
{
    /// <summary>
    /// Adapter that bridges the engine's <see cref="MappingManager"/> to a
    /// <see cref="DataImportConfiguration"/> so a user-authored mapping can be
    /// reused by the import pipeline. Produces a ready-to-run
    /// <see cref="DataImportConfiguration"/> with the same source / destination
    /// entity + datasource pair as the mapping.
    /// </summary>
    /// <remarks>
    /// Phase 9 — Mapping ↔ DataImport integration. The adapter is one-way: it
    /// reads a mapping's <see cref="EntityDataMap_DTL.EntityName"/> and the
    /// optional <c>DestEntityName</c> hint to populate a fresh import
    /// configuration. It does not overwrite an existing import configuration's
    /// field mappings because <see cref="DataImportConfiguration"/> does not carry
    /// per-field mappings — it relies on column-name parity between source /
    /// destination entities at run time.
    /// </remarks>
    public static class DataImportMappingAdapter
    {
        /// <summary>
        /// Build a fresh <see cref="DataImportConfiguration"/> from the supplied
        /// mapping. The mapping's <see cref="EntityDataMap_DTL.EntityName"/> becomes
        /// the source entity; the destination entity / datasource come from the
        /// explicit <paramref name="destEntityName"/> / <paramref name="destDataSource"/>
        /// arguments (or fall back to the source values when null / empty).
        /// </summary>
        public static DataImportConfiguration ToImportConfig(
            EntityDataMap_DTL map,
            string destEntityName = null,
            string destDataSource = null,
            string watermarkColumn = "")
        {
            if (map == null) throw new ArgumentNullException(nameof(map));

            string sourceEntity = map.EntityName;
            string sourceDataSource = map.EntityDataSource;

            // Heuristic fall-back: if no explicit destination provided, the import
            // will read from source and write to itself (the operator must pre-
            // configure the destination in the editor before the import runs).
            string destinationEntity =
                !string.IsNullOrWhiteSpace(destEntityName) ? destEntityName : sourceEntity;
            string destinationDataSource =
                !string.IsNullOrWhiteSpace(destDataSource) ? destDataSource : sourceDataSource;

            return new DataImportConfiguration
            {
                SourceEntityName = sourceEntity,
                SourceDataSourceName = sourceDataSource,
                DestEntityName = destinationEntity,
                DestDataSourceName = destinationDataSource,
                WatermarkColumn = watermarkColumn ?? string.Empty,
                ApplyDefaults = true
            };
        }

        /// <summary>
        /// Round-trip: read the <paramref name="config"/>'s source / destination
        /// pair and emit a stub <see cref="EntityDataMap_DTL"/>. The resulting map
        /// has no field-level entries — column-name parity is the import
        /// pipeline's contract — but the entity / datasource metadata is preserved
        /// for downstream tooling.
        /// </summary>
        public static EntityDataMap_DTL FromImportConfig(DataImportConfiguration config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));

            return new EntityDataMap_DTL
            {
                EntityName = config.SourceEntityName,
                EntityDataSource = config.SourceDataSourceName
            };
        }
    }
}
