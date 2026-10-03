using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Workflow.Mapping;

namespace TheTechIdea.Beep.Editor.Importing.Helpers
{
    internal static class BoundMappingMetadata
    {
        internal static void Validate(EntityDataMap mapping, EntityStructure source, EntityStructure target)
        {
            var sourceFields = Index(source);
            var targetFields = Index(target);
            if (mapping?.MappedEntities == null || mapping.MappedEntities.Count != 1)
                throw Invalid();
            var detail = mapping.MappedEntities[0];
            if (detail == null || detail.FieldMapping == null || detail.FieldMapping.Count == 0 ||
                !Same(mapping.EntityName, source.EntityName) || !Same(detail.EntityName, target.EntityName))
                throw Invalid();

            var assigned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in detail.FieldMapping)
            {
                if (pair == null || string.IsNullOrWhiteSpace(pair.FromFieldName) ||
                    string.IsNullOrWhiteSpace(pair.ToFieldName) ||
                    !sourceFields.ContainsKey(pair.FromFieldName) || !targetFields.ContainsKey(pair.ToFieldName) ||
                    !assigned.Add(pair.ToFieldName) ||
                    !Same(pair.FromEntityName, source.EntityName) || !Same(pair.ToEntityName, target.EntityName))
                    throw Invalid();
            }
            if (targetFields.Values.Any(field => field.IsRequired && !field.IsAutoIncrement && !assigned.Contains(field.FieldName)))
                throw Invalid();
        }

        internal static void ValidateConfiguration(DataImportConfiguration config)
        {
            Validate(config.Mapping, config.SourceEntityStructure, config.DestEntityStructure);
            if (!Same(config.SourceEntityName, config.SourceEntityStructure.EntityName) ||
                !Same(config.DestEntityName, config.DestEntityStructure.EntityName) ||
                !Same(config.Mapping.EntityDataSource, config.SourceDataSourceName) ||
                !Same(config.Mapping.MappedEntities[0].EntityDataSource, config.DestDataSourceName))
                throw Invalid();
            var detail = config.Mapping.MappedEntities[0];
            ValidateShape(detail.EntityFields, config.DestEntityStructure.Fields);
            ValidateShape(detail.SelectedDestFields, config.DestEntityStructure.Fields);
            var source = Index(config.SourceEntityStructure);
            var target = Index(config.DestEntityStructure);
            foreach (var pair in detail.FieldMapping)
            {
                if (!string.Equals(pair.FromFieldType, source[pair.FromFieldName].Fieldtype, StringComparison.Ordinal) ||
                    !string.Equals(pair.ToFieldType, target[pair.ToFieldName].Fieldtype, StringComparison.Ordinal))
                    throw Invalid();
            }
        }

        internal static void ValidateGeneratedTarget(IDMEEditor editor, DataImportConfiguration config)
        {
            // Match MappingManager.GetEntityObject's namespace/type request exactly.
            var type = DMTypeBuilder.GetOrCreateType(editor, config.DestEntityName, null,
                config.DestEntityName, config.Mapping.MappedEntities[0].SelectedDestFields);
            foreach (var field in config.DestEntityStructure.Fields)
            {
                var property = type.GetProperty(field.FieldName, BindingFlags.Public | BindingFlags.Instance);
                if (property == null || !property.CanWrite || property.GetIndexParameters().Length != 0)
                    throw Invalid();
            }
        }

        private static void ValidateShape(List<EntityField> shape, List<EntityField> target)
        {
            if (shape == null || shape.Count != target.Count ||
                shape.Where((field, index) => !ReferenceEquals(field, target[index])).Any())
                throw Invalid();
        }

        internal static Dictionary<string, EntityField> Index(EntityStructure metadata)
        {
            if (metadata == null || string.IsNullOrWhiteSpace(metadata.EntityName) ||
                metadata.Fields == null || metadata.Fields.Count == 0)
                throw Invalid();
            var result = new Dictionary<string, EntityField>(StringComparer.OrdinalIgnoreCase);
            foreach (var field in metadata.Fields)
            {
                if (field == null || string.IsNullOrWhiteSpace(field.FieldName) ||
                    string.IsNullOrWhiteSpace(field.Fieldtype) || !result.TryAdd(field.FieldName, field))
                    throw Invalid();
            }
            return result;
        }

        private static bool Same(string left, string right) =>
            string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

        private static InvalidOperationException Invalid() =>
            new InvalidOperationException("Bound mapping metadata is missing, ambiguous or incomplete.");
    }
}
