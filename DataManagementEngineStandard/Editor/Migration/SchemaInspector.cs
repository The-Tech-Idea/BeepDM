using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Importing.Schema;
using TheTechIdea.Beep.Editor.Migration;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.Migration
{
    /// <summary>
    /// High-level service that answers the question
    /// "what changed in the schema between the .NET class and the database table?"
    ///
    /// <para>
    /// EF Core's Migrations model captures a <c>ModelSnapshot</c> file per migration and
    /// compares the live model to the latest snapshot to generate a diff. BeepDM now
    /// provides the same capability via the
    /// <see cref="Importing.Schema.SchemaSnapshot"/> +
    /// <see cref="Importing.Schema.SchemaComparator"/> infrastructure already in the engine.
    /// </para>
    ///
    /// <para>Capabilities:</para>
    /// <list type="bullet">
    ///   <item><description><see cref="CaptureFromTypeAsync(Type)"/> — snapshot of a POCO / Entity / EF Core class</description></item>
    ///   <item><description><see cref="CaptureFromDataSourceAsync(IDataSource, string)"/> — snapshot of the live database table</description></item>
    ///   <item><description><see cref="InspectAsync(Type, IDataSource, string)"/> — full diff report (added / removed / altered fields)</description></item>
    ///   <item><description><see cref="SaveBaselineAsync(Type, IDataSource, string)"/> — persist a baseline snapshot for future comparisons</description></item>
    ///   <item><description><see cref="DiffAgainstBaselineAsync(Type, IDataSource, string)"/> — current code vs. the persisted baseline</description></item>
    /// </list>
    /// </summary>
    public class SchemaInspector
    {
        private readonly IDMEEditor _editor;
        private readonly ISchemaSnapshotStore _store;

        public SchemaInspector(IDMEEditor editor, ISchemaSnapshotStore store = null)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
            _store = store ?? new FileSchemaSnapshotStore();
        }

        /// <summary>Compute the canonical context key for a (datasource, entity) pair.</summary>
        public static string BuildKey(string dataSourceName, string entityName) =>
            $"{dataSourceName}/{entityName}";

        // ── 1. Capture from a .NET Type ────────────────────────────────────

        public Task<SchemaSnapshot> CaptureFromTypeAsync(
            Type type,
            string dataSourceName,
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            var snap = new SchemaSnapshot
            {
                ContextKey = BuildKey(dataSourceName, type.Name),
                CapturedAt = DateTime.UtcNow,
                DataSourceName = dataSourceName,
                EntityName = type.Name,
                Fields = ReadStructureFields(GetEntityStructureFromType(type))
            };
            return Task.FromResult(snap);
        }

        // ── 2. Capture from a live database ────────────────────────────────

        public Task<SchemaSnapshot> CaptureFromDataSourceAsync(
            IDataSource ds,
            string entityName,
            bool refresh = true,
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (ds == null) throw new ArgumentNullException(nameof(ds));

            var structure = ds.GetEntityStructure(entityName, refresh);
            var snap = new SchemaSnapshot
            {
                ContextKey = BuildKey(ds.DatasourceName, entityName),
                CapturedAt = DateTime.UtcNow,
                DataSourceName = ds.DatasourceName,
                EntityName = entityName,
                Fields = ReadStructureFields(structure)
            };
            return Task.FromResult(snap);
        }

        // ── 3. Full diff between .NET Type and live database ───────────────

        public async Task<SchemaDriftReport> InspectAsync(
            Type type,
            IDataSource ds,
            string entityName,
            CancellationToken token = default)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (ds == null) throw new ArgumentNullException(nameof(ds));

            var desired = await CaptureFromTypeAsync(type, ds.DatasourceName, token).ConfigureAwait(false);
            var actual = await CaptureFromDataSourceAsync(ds, entityName, refresh: true, token: token).ConfigureAwait(false);

            if (actual.Fields.Count == 0)
            {
                return new SchemaDriftReport
                {
                    Baseline = desired,
                    Current = actual,
                    AddedFields = new List<SnapshotField>(desired.Fields),
                    RemovedFields = new List<SnapshotField>(),
                    AlteredFields = new List<FieldTypeDrift>()
                };
            }

            return SchemaComparator.Compare(baseline: desired, current: actual);
        }

        // ── 4. Save the current Type as a baseline snapshot for future runs ─

        public async Task<SchemaSnapshot> SaveBaselineAsync(
            Type type,
            IDataSource ds,
            string entityName,
            CancellationToken token = default)
        {
            var snap = await CaptureFromTypeAsync(type, ds?.DatasourceName ?? string.Empty, token).ConfigureAwait(false);
            await _store.SaveAsync(snap, token).ConfigureAwait(false);
            _editor.AddLogMessage("SchemaInspector",
                $"Saved schema baseline for '{type.Name}' (key: {snap.ContextKey})",
                DateTime.Now, 0, null, Errors.Ok);
            return snap;
        }

        // ── 5. Diff the current Type against a previously-saved baseline ───

        public async Task<SchemaDriftReport?> DiffAgainstBaselineAsync(
            Type type,
            IDataSource ds,
            string entityName,
            CancellationToken token = default)
        {
            var baseline = await _store.LoadAsync(BuildKey(ds?.DatasourceName ?? string.Empty, type.Name), token).ConfigureAwait(false);
            if (baseline == null) return null;

            var current = await CaptureFromTypeAsync(type, ds?.DatasourceName ?? string.Empty, token).ConfigureAwait(false);
            return SchemaComparator.Compare(baseline, current);
        }

        // ── 6. Save the live database schema as a baseline ─────────────────

        public async Task<SchemaSnapshot> SaveDatabaseBaselineAsync(
            IDataSource ds,
            string entityName,
            CancellationToken token = default)
        {
            var snap = await CaptureFromDataSourceAsync(ds, entityName, refresh: true, token).ConfigureAwait(false);
            await _store.SaveAsync(snap, token).ConfigureAwait(false);
            return snap;
        }

        // ── 7. Multi-entity inspection ──────────────────────────────────────

        public async Task<Dictionary<string, SchemaDriftReport>> InspectManyAsync(
            IEnumerable<Type> types,
            IDataSource ds,
            CancellationToken token = default)
        {
            var result = new Dictionary<string, SchemaDriftReport>(StringComparer.OrdinalIgnoreCase);
            if (types == null || ds == null) return result;

            foreach (var type in types)
            {
                token.ThrowIfCancellationRequested();
                if (type == null) continue;
                var report = await InspectAsync(type, ds, type.Name, token).ConfigureAwait(false);
                result[type.Name] = report;
            }
            return result;
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        private EntityStructure GetEntityStructureFromType(Type type)
        {
            try
            {
                var classCreator = _editor?.classCreator;
                if (classCreator != null)
                {
                    var structure = classCreator.ConvertToEntityStructure(type);
                    if (structure != null) return structure;
                }
            }
            catch
            {
                /* fall through to reflection */
            }

            // Fallback: build a minimal EntityStructure via reflection
            var fallback = new EntityStructure
            {
                EntityName = type.Name,
                Fields = new List<EntityField>()
            };

            foreach (var prop in type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                fallback.Fields.Add(new EntityField
                {
                    FieldName = prop.Name,
                    Fieldtype = prop.PropertyType.Name,
                    AllowDBNull = Nullable.GetUnderlyingType(prop.PropertyType) != null || !prop.PropertyType.IsValueType
                });
            }

            return fallback;
        }

        private static List<SnapshotField> ReadStructureFields(EntityStructure structure)
        {
            var list = new List<SnapshotField>();
            if (structure?.Fields == null) return list;

            foreach (var f in structure.Fields)
            {
                list.Add(new SnapshotField
                {
                    Name = f?.FieldName ?? string.Empty,
                    DataType = f?.Fieldtype ?? string.Empty,
                    IsNullable = f?.AllowDBNull ?? true,
                    MaxLength = f?.MaxLength ?? f?.Size1 ?? 0,
                    Precision = f?.NumericPrecision ?? 0,
                    Scale = f?.NumericScale ?? 0
                });
            }

            return list;
        }
    }
}
