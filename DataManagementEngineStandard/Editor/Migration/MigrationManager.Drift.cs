using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Editor.Schema;

namespace TheTechIdea.Beep.Editor.Migration
{
    /// <summary>
    /// Schema-drift DETECTION for the migration datasource. This is the single home for
    /// "how does the desired schema (from a .NET type) differ from the live datasource?" —
    /// previously duplicated in the removed <c>SchemaManager</c>. It reads structure through
    /// <see cref="IDataSource"/> interface calls (no DDL) and compares via the shared
    /// <see cref="SchemaComparator"/>, producing a FULL <see cref="SchemaDriftReport"/>
    /// (added / removed / altered fields — unlike the additive-only plan diff).
    ///
    /// Detection only: this does not change plan/execute behavior, which stays additive and
    /// routed through the per-datasource <c>ISchemaMigrationProvider</c>.
    /// </summary>
    public partial class MigrationManager
    {
        private readonly ISchemaComparator _driftComparator = new SchemaComparator();

        /// <summary>
        /// Full field-level drift between a .NET type (desired) and the live entity on
        /// <see cref="MigrateDataSource"/> (actual). Semantics preserved from the former
        /// <c>SchemaManager.InspectAsync</c>: comparison is <c>Compare(baseline: desired, current: actual)</c>.
        /// </summary>
        public SchemaDriftReport InspectDrift(Type entityType)
        {
            if (entityType == null) throw new ArgumentNullException(nameof(entityType));

            var dsName = MigrateDataSource?.DatasourceName ?? string.Empty;

            // Phase 7 (W3): resolve the live entity via the SAME name planning uses (honours [Table]),
            // so a [Table("TBL_X")] class drifts against TBL_X — not a non-existent table named after
            // the CLR class (which made everything read as "added").
            var desiredStructure = TryGetEntityStructure(entityType);
            var resolvedName = GetEntityName(entityType, desiredStructure);

            var desired = new SchemaSnapshot
            {
                ContextKey     = $"{dsName}/{resolvedName}",
                CapturedAt     = DateTime.UtcNow,
                DataSourceName = dsName,
                EntityName     = resolvedName,
                Fields         = ReadSnapshotFields(desiredStructure)
            };

            EntityStructure current = null;
            try { current = MigrateDataSource?.GetEntityStructure(resolvedName, true); }
            catch (Exception ex)
            {
                _editor?.AddLogMessage("MigrationManager",
                    $"InspectDrift: could not read live structure for '{resolvedName}': {ex.Message}",
                    DateTime.Now, 0, null, Errors.Warning);
            }

            var actual = new SchemaSnapshot
            {
                ContextKey     = $"{dsName}/{resolvedName}",
                CapturedAt     = DateTime.UtcNow,
                DataSourceName = dsName,
                EntityName     = resolvedName,
                Fields         = ReadSnapshotFields(current)
            };

            // Entity absent on the target → every desired field reads as "added".
            if (actual.Fields.Count == 0)
                return new SchemaDriftReport
                {
                    Baseline = desired,
                    Current  = actual,
                    AddedFields   = new List<SnapshotField>(desired.Fields),
                    RemovedFields = new List<SnapshotField>(),
                    AlteredFields = new List<FieldTypeDrift>()
                };

            return _driftComparator.Compare(baseline: desired, current: actual);
        }

        /// <summary>Batch <see cref="InspectDrift(Type)"/> keyed by type name.</summary>
        public Dictionary<string, SchemaDriftReport> InspectDrift(IEnumerable<Type> entityTypes)
        {
            var result = new Dictionary<string, SchemaDriftReport>(StringComparer.OrdinalIgnoreCase);
            if (entityTypes == null) return result;
            foreach (var type in entityTypes.Where(t => t != null))
                result[type.Name] = InspectDrift(type);
            return result;
        }

        // EntityStructure → snapshot fields (ported verbatim from the removed SchemaManager).
        private static List<SnapshotField> ReadSnapshotFields(EntityStructure structure)
        {
            var list = new List<SnapshotField>();
            if (structure?.Fields == null) return list;
            foreach (var f in structure.Fields)
            {
                list.Add(new SnapshotField
                {
                    Name       = f?.FieldName ?? string.Empty,
                    DataType   = f?.Fieldtype ?? string.Empty,
                    IsNullable = f?.AllowDBNull ?? true,
                    MaxLength  = f?.MaxLength ?? f?.Size1 ?? 0,
                    Precision  = f?.NumericPrecision ?? 0,
                    Scale      = f?.NumericScale ?? 0
                });
            }
            return list;
        }
    }
}
