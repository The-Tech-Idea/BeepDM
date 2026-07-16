using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.Importing.Interfaces;
using TheTechIdea.Beep.Editor.Schema;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.Importing
{
    /// <summary>
    /// Schema-related entry points exposed on <see cref="IDataImportManager"/>.
    /// These are thin shims over <see cref="SyncSchemaPreflight"/> (in <c>Editor/Schema/</c>),
    /// the shared cross-datasource sync preflight/draft helper.
    /// </summary>
    public partial class DataImportManager
    {
        #region RunMigrationPreflightAsync (back-compat shim)

        /// <summary>
        /// Validates schema compatibility between source and destination before running a migration.
        /// Delegates to <see cref="SyncSchemaPreflight.RunPreflightAsync"/>; kept on
        /// <see cref="IDataImportManager"/> for back-compat with existing callers.
        /// </summary>
        public async Task<IErrorsInfo> RunMigrationPreflightAsync(
            DataImportConfiguration config,
            Action<string>? log = null)
        {
            var preflight = await SyncSchemaPreflight.RunPreflightAsync(
                _editor, ToRequest(config), log, CancellationToken.None).ConfigureAwait(false);

            LogPreflight(preflight, log);
            return preflight.Status;
        }

        #endregion

        #region BuildSyncDraftAsync (back-compat shim)

        /// <summary>
        /// Builds a <see cref="DataSyncSchema"/> that describes what a sync run would do,
        /// without executing any data movement. Delegates to <see cref="SyncSchemaPreflight.BuildSyncDraftAsync"/>.
        /// </summary>
        public async Task<DataSyncSchema> BuildSyncDraftAsync(DataImportConfiguration config)
        {
            var result = await SyncSchemaPreflight.BuildSyncDraftAsync(
                _editor, ToRequest(config), CancellationToken.None).ConfigureAwait(false);

            if (result?.Draft != null)
            {
                _progressHelper?.LogImport($"BuildSyncDraft: draft '{result.Draft.EntityName}' created.", 0);
            }
            return result?.Draft;
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────────────────────────

        private static SchemaRequest ToRequest(DataImportConfiguration config) => new()
        {
            SourceDataSourceName          = config?.SourceDataSourceName ?? string.Empty,
            SourceEntityName              = config?.SourceEntityName ?? string.Empty,
            DestinationDataSourceName     = config?.DestDataSourceName ?? string.Empty,
            DestinationEntityName         = config?.DestEntityName ?? string.Empty,
            AddMissingColumns             = config?.AddMissingColumns ?? false,
            CreateDestinationIfNotExists  = config?.CreateDestinationIfNotExists ?? false,
            Mapping                       = config?.Mapping,
            SourceData                    = config?.SourceData,
            DestinationData               = config?.DestData,
            SourceEntityStructure         = config?.SourceEntityStructure,
            DestinationEntityStructure    = config?.DestEntityStructure
        };

        private void LogPreflight(SchemaPreflightResult preflight, Action<string>? log)
        {
            void Log(string msg)
            {
                log?.Invoke(msg);
                _progressHelper?.LogImport(msg, 0);
            }
            if (preflight == null) return;
            if (preflight.SourceStructureLoaded)
                Log($"Preflight: source entity has {preflight.MissingDestinationFields.Count} field(s) flagged as missing.");
            if (preflight.DestinationSnapshot != null)
                Log($"Preflight: captured baseline snapshot '{preflight.DestinationSnapshot.ContextKey}'.");
        }
    }
}
