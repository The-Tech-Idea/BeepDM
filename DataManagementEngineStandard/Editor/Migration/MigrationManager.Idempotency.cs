using System;
using System.Linq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.Migration
{
    /// <summary>
    /// Idempotency gate over the per-datasource migration history. Lets a caller record a named
    /// migration once and skip it on subsequent runs — closing the "history is write-only, never
    /// queried" gap. History is read/written through <see cref="IConfigEditor"/> (the same store the
    /// execution checkpoints and plan artifacts persist to).
    /// </summary>
    public partial class MigrationManager
    {
        /// <summary>
        /// True when a <b>successful</b> migration record with <paramref name="migrationName"/> (matched
        /// on either <c>MigrationId</c> or <c>Name</c>) already exists in the current datasource's history.
        /// </summary>
        public bool IsMigrationApplied(string migrationName)
        {
            if (string.IsNullOrWhiteSpace(migrationName)) return false;
            try
            {
                var configEditor = _editor?.ConfigEditor;
                var dsName = MigrateDataSource?.DatasourceName ?? string.Empty;
                if (configEditor == null || string.IsNullOrWhiteSpace(dsName)) return false;

                var history = configEditor.LoadMigrationHistory(dsName);
                return history?.Migrations?.Any(record =>
                    record != null && record.Success &&
                    (string.Equals(record.MigrationId, migrationName, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(record.Name, migrationName, StringComparison.OrdinalIgnoreCase))) ?? false;
            }
            catch (Exception ex)
            {
                _editor?.AddLogMessage("MigrationManager",
                    $"IsMigrationApplied('{migrationName}') failed: {ex.Message}",
                    DateTime.Now, 0, null, Errors.Warning);
                return false;
            }
        }

        /// <summary>
        /// Appends a named migration record to the current datasource's history so later runs can gate on
        /// <see cref="IsMigrationApplied"/>.
        /// </summary>
        public IErrorsInfo RecordMigration(string migrationName, bool success = true, string notes = null)
        {
            if (string.IsNullOrWhiteSpace(migrationName))
                return CreateErrorsInfo(Errors.Failed, "Migration name is required to record a migration.");

            try
            {
                var configEditor = _editor?.ConfigEditor;
                if (configEditor == null)
                    return CreateErrorsInfo(Errors.Failed, "ConfigEditor is not available; cannot record migration.");

                var record = new MigrationRecord
                {
                    MigrationId = migrationName,
                    Name = migrationName,
                    AppliedOnUtc = DateTime.UtcNow,
                    Success = success,
                    Notes = notes ?? string.Empty
                };
                configEditor.AppendMigrationRecord(
                    MigrateDataSource?.DatasourceName ?? string.Empty,
                    MigrateDataSource?.DatasourceType ?? DataSourceType.Unknown,
                    record);
                return CreateErrorsInfo(Errors.Ok, $"Recorded migration '{migrationName}'.");
            }
            catch (Exception ex)
            {
                return CreateErrorsInfo(Errors.Failed, $"Failed to record migration '{migrationName}': {ex.Message}", ex);
            }
        }
    }
}
