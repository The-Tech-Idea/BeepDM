using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Beep.Workflow.Mapping;

namespace TheTechIdea.Beep.Editor.Mapping
{
    // ─────────────────────────────────────────────────────────────────────────
    // Supporting types
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Controls how field names are compared when auto-mapping by convention.
    /// </summary>
    public enum NameMatchMode
    {
        /// <summary>Names must match character-for-character (ordinal).</summary>
        Exact,

        /// <summary>Names match regardless of casing.</summary>
        CaseInsensitive,

        /// <summary>
        /// Names match when one is a prefix of the other (case-insensitive).
        /// Useful for aligning "CustomerID" → "CustID"-style shortened names.
        /// </summary>
        FuzzyPrefix
    }

    /// <summary>
    /// Describes the outcome of <see cref="MappingManager.ValidateMappingAsync"/>.
    /// </summary>
    public sealed class MappingValidationResult
    {
        public bool IsValid => Errors.Count == 0;

        /// <summary>Indicates whether any warnings were produced.</summary>
        public bool HasWarnings => Warnings.Count > 0;

        /// <summary>Human-readable issues found in the mapping.</summary>
        public List<string> Errors { get; } = new();

        /// <summary>Informational warnings that do not invalidate the mapping.</summary>
        public List<string> Warnings { get; } = new();
    }

    /// <summary>
    /// Describes the field-level differences between two versions of a mapping.
    /// </summary>
    public sealed class MappingDiff
    {
        /// <summary>Fields present in <c>current</c> but not in <c>baseline</c>.</summary>
        public List<Mapping_rep_fields> Added { get; } = new();

        /// <summary>Fields present in <c>baseline</c> but removed in <c>current</c>.</summary>
        public List<Mapping_rep_fields> Removed { get; } = new();

        /// <summary>
        /// Fields whose destination-field name or type changed between the two versions.
        /// Each entry is the <em>current</em> field (after change).
        /// </summary>
        public List<Mapping_rep_fields> Changed { get; } = new();

        public bool HasChanges => Added.Count > 0 || Removed.Count > 0 || Changed.Count > 0;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MappingManager partial — convention-based mapping helpers
    // ─────────────────────────────────────────────────────────────────────────

    public static partial class MappingManager
    {
        // ── AutoMapByConvention ────────────────────────────────────────────

        /// <summary>
        /// Builds an <see cref="EntityDataMap"/> by matching source fields to destination
        /// fields using the specified <paramref name="mode"/>.  Unmatched destination fields
        /// receive a <c>null</c> source assignment so they remain visible for manual review.
        /// </summary>
        /// <param name="editor">Active DMEEditor instance.</param>
        /// <param name="srcEntityName">Source entity name.</param>
        /// <param name="srcDataSourceName">Source data-source name.</param>
        /// <param name="destEntityName">Destination entity name.</param>
        /// <param name="destDataSourceName">Destination data-source name.</param>
        /// <param name="mode">Name-matching strategy (default: <see cref="NameMatchMode.CaseInsensitive"/>).</param>
        /// <returns>Auto-populated mapping — save via <see cref="SaveMapping"/> when satisfied.</returns>
        public static EntityDataMap AutoMapByConvention(
            IDMEEditor editor,
            string srcEntityName,
            string srcDataSourceName,
            string destEntityName,
            string destDataSourceName,
            NameMatchMode mode = NameMatchMode.CaseInsensitive)
        {
            if (editor == null) throw new ArgumentNullException(nameof(editor));

            var mapping = LoadOrInitializeMapping(editor, destEntityName, destDataSourceName);
            mapping.MappingName = $"{destEntityName}_{destDataSourceName}_auto";

            try
            {
                var destStructure = GetEntityStructure(editor, destDataSourceName, destEntityName);
                var srcStructure  = GetEntityStructure(editor, srcDataSourceName,  srcEntityName);

                mapping.EntityFields = destStructure?.Fields ?? new System.Collections.Generic.List<EntityField>();

                var srcFields = srcStructure?.Fields ?? new System.Collections.Generic.List<EntityField>();
                var fieldMappings = new List<Mapping_rep_fields>();

                foreach (var destField in mapping.EntityFields)
                {
                    var match = FindMatch(srcFields, destField.FieldName, mode);
                    fieldMappings.Add(new Mapping_rep_fields
                    {
                        ToFieldName   = destField.FieldName,
                        ToFieldType   = destField.Fieldtype,
                        FromFieldName = match?.FieldName,
                        FromFieldType = match?.Fieldtype
                    });
                }

                var detail = new EntityDataMap_DTL
                {
                    EntityName       = srcEntityName,
                    EntityDataSource = srcDataSourceName,
                    SelectedDestFields = srcFields,
                    FieldMapping     = fieldMappings
                };

                mapping.MappedEntities.Clear();
                mapping.MappedEntities.Add(detail);
            }
            catch (Exception ex)
            {
                LogError(editor, $"AutoMapByConvention failed for {destEntityName}", ex);
            }

            return mapping;
        }

        // ── ValidateMappingAsync ───────────────────────────────────────────

        /// <summary>
        /// Validates a mapping asynchronously, checking that:
        /// <list type="bullet">
        ///   <item>Every destination field with a non-null source assignment actually exists
        ///         in the live source entity structure.</item>
        ///   <item>No destination field has an ambiguous (duplicate) source assignment.</item>
        ///   <item>There is at least one mapped field.</item>
        /// </list>
        /// </summary>
        public static Task<MappingValidationResult> ValidateMappingAsync(
            IDMEEditor editor,
            EntityDataMap mapping)
        {
            if (editor == null)  throw new ArgumentNullException(nameof(editor));
            if (mapping == null) throw new ArgumentNullException(nameof(mapping));

            return Task.Run(() => ValidateMappingCore(editor, mapping));
        }

        private static MappingValidationResult ValidateMappingCore(IDMEEditor editor, EntityDataMap mapping)
        {
            var result = new MappingValidationResult();

            var allFields = mapping.MappedEntities?
                .SelectMany(e => e.FieldMapping ?? Enumerable.Empty<Mapping_rep_fields>())
                .ToList() ?? new List<Mapping_rep_fields>();

            if (allFields.Count == 0)
            {
                result.Errors.Add("Mapping contains no field assignments.");
                return result;
            }

            int mappedCount = allFields.Count(f => !string.IsNullOrWhiteSpace(f.FromFieldName));
            if (mappedCount == 0)
            {
                result.Errors.Add("All destination fields have a null source — nothing will be imported.");
                return result;
            }

            // Check for duplicate source → dest bindings within each entity detail
            foreach (var detail in mapping.MappedEntities ?? Enumerable.Empty<EntityDataMap_DTL>())
            {
                EntityField[]? liveFields = null;

                try
                {
                    var srcStructure = GetEntityStructure(editor, detail.EntityDataSource, detail.EntityName);
                    liveFields = srcStructure?.Fields?.ToArray();
                }
                catch
                {
                    result.Warnings.Add($"Could not load live structure for source entity '{detail.EntityName}' " +
                                        $"on '{detail.EntityDataSource}' — skipping live field validation.");
                }

                var duplicates = (detail.FieldMapping ?? Enumerable.Empty<Mapping_rep_fields>())
                    .Where(f => !string.IsNullOrWhiteSpace(f.FromFieldName))
                    .GroupBy(f => f.FromFieldName, StringComparer.OrdinalIgnoreCase)
                    .Where(g => g.Count() > 1);

                foreach (var dup in duplicates)
                {
                    result.Errors.Add($"Source field '{dup.Key}' is mapped to multiple destination fields: " +
                                      string.Join(", ", dup.Select(f => f.ToFieldName)));
                }

                if (liveFields != null)
                {
                    var liveNames = new HashSet<string>(liveFields.Select(f => f.FieldName),
                        StringComparer.OrdinalIgnoreCase);

                    var missing = (detail.FieldMapping ?? Enumerable.Empty<Mapping_rep_fields>())
                        .Where(f => !string.IsNullOrWhiteSpace(f.FromFieldName) &&
                                    !liveNames.Contains(f.FromFieldName!))
                        .ToList();

                    foreach (var m in missing)
                    {
                        result.Errors.Add($"Source field '{m.FromFieldName}' mapped to '{m.ToFieldName}' " +
                                          $"does not exist in live source entity '{detail.EntityName}'.");
                    }
                }
            }

            int unmapped = allFields.Count(f => string.IsNullOrWhiteSpace(f.FromFieldName));
            if (unmapped > 0)
            {
                result.Warnings.Add($"{unmapped} destination field(s) have no source assignment and will " +
                                    "receive default values.");
            }

            return result;
        }

        // ── DiffMapping ────────────────────────────────────────────────────

        /// <summary>
        /// Computes the field-level diff between a <paramref name="baseline"/> mapping and a
        /// <paramref name="current"/> mapping, returning added, removed, and changed fields.
        /// Field identity is determined by <see cref="Mapping_rep_fields.ToFieldName"/> (case-insensitive).
        /// </summary>
        public static MappingDiff DiffMapping(EntityDataMap baseline, EntityDataMap current)
        {
            if (baseline == null) throw new ArgumentNullException(nameof(baseline));
            if (current  == null) throw new ArgumentNullException(nameof(current));

            var diff = new MappingDiff();

            var baseFields = FlattenFields(baseline);
            var currFields = FlattenFields(current);

            var baseIndex = baseFields.ToDictionary(
                f => f.ToFieldName ?? string.Empty, StringComparer.OrdinalIgnoreCase);
            var currIndex = currFields.ToDictionary(
                f => f.ToFieldName ?? string.Empty, StringComparer.OrdinalIgnoreCase);

            // Added — in current but not in baseline
            foreach (var kv in currIndex)
            {
                if (!baseIndex.ContainsKey(kv.Key))
                {
                    diff.Added.Add(kv.Value);
                }
            }

            // Removed — in baseline but not in current
            foreach (var kv in baseIndex)
            {
                if (!currIndex.ContainsKey(kv.Key))
                {
                    diff.Removed.Add(kv.Value);
                }
            }

            // Changed — present in both but source name or type differs
            foreach (var kv in currIndex)
            {
                if (baseIndex.TryGetValue(kv.Key, out var baseField))
                {
                    bool sourceChanged = !string.Equals(
                        kv.Value.FromFieldName, baseField.FromFieldName,
                        StringComparison.OrdinalIgnoreCase);
                    bool typeChanged = !string.Equals(
                        kv.Value.ToFieldType, baseField.ToFieldType,
                        StringComparison.OrdinalIgnoreCase);

                    if (sourceChanged || typeChanged)
                    {
                        diff.Changed.Add(kv.Value);
                    }
                }
            }

            return diff;
        }

        // ── Private helpers ────────────────────────────────────────────────

        private static IEnumerable<Mapping_rep_fields> FlattenFields(EntityDataMap mapping) =>
            mapping.MappedEntities?
                .SelectMany(e => e.FieldMapping ?? Enumerable.Empty<Mapping_rep_fields>())
            ?? Enumerable.Empty<Mapping_rep_fields>();

        private static EntityField? FindMatch(
            IEnumerable<EntityField> candidates,
            string targetName,
            NameMatchMode mode)
        {
            return mode switch
            {
                NameMatchMode.Exact =>
                    candidates.FirstOrDefault(f =>
                        string.Equals(f.FieldName, targetName, StringComparison.Ordinal)),

                NameMatchMode.CaseInsensitive =>
                    candidates.FirstOrDefault(f =>
                        string.Equals(f.FieldName, targetName, StringComparison.OrdinalIgnoreCase)),

                NameMatchMode.FuzzyPrefix =>
                    candidates.FirstOrDefault(f =>
                        f.FieldName.StartsWith(targetName, StringComparison.OrdinalIgnoreCase) ||
                        targetName.StartsWith(f.FieldName,  StringComparison.OrdinalIgnoreCase)),

                _ => null
            };
        }
    }
}
