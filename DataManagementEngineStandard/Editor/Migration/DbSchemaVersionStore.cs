using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.Migration
{
    /// <summary>
    /// Reads and writes the authoritative schema-version marker <em>inside the target database</em>
    /// (table <see cref="MarkerEntityName"/>), EF-Core-<c>__EFMigrationsHistory</c> style, so any
    /// machine that opens the datasource can learn its version without machine-local state. A single
    /// current-version row is kept; full history is the JSON audit mirror on
    /// <c>IVersionManagementService</c>.
    /// </summary>
    /// <remarks>
    /// Degrades gracefully: on a datasource that cannot create the marker table (many NoSQL / file /
    /// REST providers), <see cref="Read"/> returns null and <see cref="Write"/> reports a warning —
    /// callers then treat the JSON mirror as authoritative. Nothing here throws to the caller.
    /// </remarks>
    public sealed class DbSchemaVersionStore
    {
        /// <summary>Name of the in-database marker table. Double-underscore, EF-style; providers quote it.</summary>
        public const string MarkerEntityName = "__BeepSchemaVersion";

        private readonly IDMEEditor _editor;

        public DbSchemaVersionStore(IDMEEditor editor)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        }

        /// <summary>
        /// Returns the version recorded in the datasource, or null when the marker is absent, the
        /// datasource can't be opened, or the read fails. Null means "unknown / never versioned".
        /// </summary>
        public DatabaseVersion Read(string datasourceName)
        {
            if (string.IsNullOrWhiteSpace(datasourceName)) return null;

            try
            {
                var ds = OpenDataSource(datasourceName);
                if (ds == null) return null;
                if (!ds.CheckEntityExist(MarkerEntityName)) return null;

                var rows = ds.GetEntity(MarkerEntityName, new List<AppFilter>())?.Cast<object>().ToList();
                if (rows == null || rows.Count == 0) return null;

                // Newest row wins (single-row design keeps one, but tolerate more).
                var row = rows
                    .OrderByDescending(r => AsDateTime(GetField(r, nameof(BeepSchemaVersionRecord.AppliedAtUtc))))
                    .First();

                var payload = AsString(GetField(row, nameof(BeepSchemaVersionRecord.PayloadJson)));
                if (!string.IsNullOrWhiteSpace(payload))
                {
                    try
                    {
                        var fromJson = JsonSerializer.Deserialize<DatabaseVersion>(payload);
                        if (fromJson != null) return fromJson;
                    }
                    catch (Exception ex)
                    {
                        _editor?.AddLogMessage("DbSchemaVersionStore",
                            $"Marker PayloadJson unreadable for '{datasourceName}', rebuilding from columns: {ex.Message}",
                            DateTime.Now, 0, null, Errors.Warning);
                    }
                }

                // Fall back to the scalar columns.
                return new DatabaseVersion
                {
                    DatasourceName = datasourceName,
                    Version = AsString(GetField(row, nameof(BeepSchemaVersionRecord.Version))) ?? "0.0.0",
                    Major = AsInt(GetField(row, nameof(BeepSchemaVersionRecord.Major))),
                    Minor = AsInt(GetField(row, nameof(BeepSchemaVersionRecord.Minor))),
                    Patch = AsInt(GetField(row, nameof(BeepSchemaVersionRecord.Patch))),
                    SchemaHash = AsString(GetField(row, nameof(BeepSchemaVersionRecord.SchemaHash))),
                    MigrationPlanHash = AsString(GetField(row, nameof(BeepSchemaVersionRecord.MigrationPlanHash))),
                    AppliedAt = AsDateTime(GetField(row, nameof(BeepSchemaVersionRecord.AppliedAtUtc))) ?? DateTime.UtcNow,
                    AppliedBy = AsString(GetField(row, nameof(BeepSchemaVersionRecord.AppliedBy)))
                };
            }
            catch (Exception ex)
            {
                _editor?.AddLogMessage("DbSchemaVersionStore",
                    $"Could not read version marker for '{datasourceName}': {ex.Message}",
                    DateTime.Now, 0, null, Errors.Warning);
                return null;
            }
        }

        /// <summary>
        /// Upserts the current-version row into the datasource, creating the marker table first if
        /// needed. Returns a warning (never throws) when the datasource can't host the marker.
        /// </summary>
        public IErrorsInfo Write(string datasourceName, DatabaseVersion version)
        {
            var result = new ErrorsInfo { Flag = Errors.Ok, Message = "Version marker written." };
            if (string.IsNullOrWhiteSpace(datasourceName) || version == null)
                return Warn(result, "Datasource name and version are required.");

            try
            {
                var ds = OpenDataSource(datasourceName);
                if (ds == null)
                    return Warn(result, $"Datasource '{datasourceName}' could not be opened; version marker not written.");

                if (!EnsureMarker(ds, datasourceName))
                    return Warn(result,
                        $"Datasource '{datasourceName}' cannot host the '{MarkerEntityName}' table; " +
                        "version recorded in the JSON mirror only.");

                var record = ToRecord(version);
                var existing = ds.GetEntity(MarkerEntityName,
                    new List<AppFilter> { new AppFilter { FieldName = nameof(BeepSchemaVersionRecord.Id), Operator = "=", FilterValue = record.Id.ToString() } })
                    ?.Cast<object>().ToList();

                var op = (existing != null && existing.Count > 0)
                    ? ds.UpdateEntity(MarkerEntityName, record)
                    : ds.InsertEntity(MarkerEntityName, record);

                if (op == null || op.Flag == Errors.Failed)
                    return Warn(result, $"Marker upsert failed for '{datasourceName}': {op?.Message}");

                _editor?.AddLogMessage("DbSchemaVersionStore",
                    $"Version marker set to v{version.VersionString} on '{datasourceName}'.",
                    DateTime.Now, 0, null, Errors.Ok);
                return result;
            }
            catch (Exception ex)
            {
                return Warn(result, $"Could not write version marker for '{datasourceName}': {ex.Message}");
            }
        }

        /// <summary>
        /// Ensures the marker table exists by routing the <see cref="BeepSchemaVersionRecord"/> POCO
        /// through <see cref="MigrationManager"/> — the same plan → execute path (and per-datasource
        /// <c>ISchemaMigrationProvider</c> / <c>IDataSource.CreateEntityAs</c>) every other entity uses.
        /// No hand-rolled DDL or EntityStructure. Returns false when the datasource cannot host the
        /// marker (many NoSQL / file / REST providers) — the caller then falls back to the JSON mirror.
        /// </summary>
        public bool EnsureMarker(IDataSource ds, string datasourceName)
        {
            if (ds == null) return false;
            try
            {
                if (ds.CheckEntityExist(MarkerEntityName)) return true;

                var migration = new MigrationManager(_editor, ds) { MigrateDataSource = ds };
                var plan = migration.BuildMigrationPlanForTypes(
                    new[] { typeof(BeepSchemaVersionRecord) }, detectRelationships: false);
                if (plan == null) return false;

                var result = migration.ExecuteMigrationPlan(plan, new MigrationExecutionPolicy());
                return (result?.Success ?? false) && ds.CheckEntityExist(MarkerEntityName);
            }
            catch (Exception ex)
            {
                _editor?.AddLogMessage("DbSchemaVersionStore",
                    $"Could not create '{MarkerEntityName}' on '{datasourceName}': {ex.Message}",
                    DateTime.Now, 0, null, Errors.Warning);
                return false;
            }
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private IDataSource OpenDataSource(string datasourceName)
        {
            var ds = _editor.GetDataSource(datasourceName);
            if (ds == null) return null;
            if (ds.ConnectionStatus != ConnectionState.Open && ds.Openconnection() != ConnectionState.Open)
                return null;
            return ds;
        }

        private static BeepSchemaVersionRecord ToRecord(DatabaseVersion v)
        {
            var record = new BeepSchemaVersionRecord
            {
                Id = 1,
                Version = string.IsNullOrWhiteSpace(v.Version) ? v.VersionString : v.Version,
                Major = v.Major,
                Minor = v.Minor,
                Patch = v.Patch,
                SchemaHash = v.SchemaHash ?? string.Empty,
                MigrationPlanHash = v.MigrationPlanHash ?? string.Empty,
                AppliedAtUtc = v.AppliedAt == default ? DateTime.UtcNow : v.AppliedAt,
                AppliedBy = v.AppliedBy ?? string.Empty
            };
            record.PayloadJson = JsonSerializer.Serialize(v);
            return record;
        }

        private static IErrorsInfo Warn(ErrorsInfo result, string message)
        {
            result.Flag = Errors.Warning;
            result.Message = message;
            return result;
        }

        /// <summary>Reads a field from a row of unknown shape: dictionary, DataRow, or POCO.</summary>
        private static object GetField(object row, string name)
        {
            if (row == null || string.IsNullOrEmpty(name)) return null;

            if (row is IDictionary dict)
            {
                foreach (DictionaryEntry e in dict)
                    if (e.Key is string k && string.Equals(k, name, StringComparison.OrdinalIgnoreCase))
                        return e.Value;
                return null;
            }

            if (row is DataRow dr)
                return dr.Table.Columns.Contains(name) ? dr[name] : null;

            var prop = row.GetType().GetProperty(name,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            return prop?.GetValue(row);
        }

        private static string AsString(object v) => v == null || v is DBNull ? null : Convert.ToString(v);

        private static int AsInt(object v)
        {
            if (v == null || v is DBNull) return 0;
            try { return Convert.ToInt32(v); } catch { return 0; }
        }

        private static DateTime? AsDateTime(object v)
        {
            if (v == null || v is DBNull) return null;
            if (v is DateTime dt) return dt;
            return DateTime.TryParse(Convert.ToString(v), out var parsed) ? parsed : (DateTime?)null;
        }
    }
}
