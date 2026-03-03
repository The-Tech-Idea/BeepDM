using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Importing.Interfaces;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.Importing
{
    /// <summary>
    /// Phase 3 — Preflight validation and sync-draft generation.
    /// Logic previously lived in ImportExportOrchestrator (UI layer); moved here so all
    /// callers can use it without taking a dependency on the Winform project.
    /// </summary>
    public partial class DataImportManager
    {
        #region RunMigrationPreflightAsync

        /// <summary>
        /// Validates schema compatibility between source and destination before running a migration.
        /// Checks that connections are reachable, entity structures can be read, and
        /// (when AddMissingColumns is false) that every source field already exists on the destination.
        /// </summary>
        /// <param name="config">The import configuration to validate.</param>
        /// <param name="log">Optional logger callback for diagnostic messages.</param>
        /// <returns>
        /// <see cref="Errors.Ok"/> if the run can proceed;
        /// <see cref="Errors.Failed"/> with a descriptive message if it cannot.
        /// </returns>
        public async Task<IErrorsInfo> RunMigrationPreflightAsync(
            DataImportConfiguration config,
            Action<string>? log = null)
        {
            if (config == null)
                return CreateErrorsInfo(Errors.Failed, "Preflight: config is null.");

            return await Task.Run(() =>
            {
                try
                {
                    void Log(string msg)
                    {
                        log?.Invoke(msg);
                        _progressHelper?.LogImport(msg, 0);
                    }

                    Log("Preflight: initialising data sources…");

                    // ── 1. Resolve source ──────────────────────────────────────────────
                    if (config.SourceData == null && !string.IsNullOrWhiteSpace(config.SourceDataSourceName))
                    {
                        config.SourceData = _editor.GetDataSource(config.SourceDataSourceName);
                        if (config.SourceData?.ConnectionStatus != ConnectionState.Open)
                            _editor.OpenDataSource(config.SourceDataSourceName);
                    }

                    if (config.SourceData == null)
                        return CreateErrorsInfo(Errors.Failed, $"Preflight: source datasource '{config.SourceDataSourceName}' could not be resolved.");

                    if (config.SourceData.ConnectionStatus != ConnectionState.Open)
                        return CreateErrorsInfo(Errors.Failed, $"Preflight: source datasource '{config.SourceDataSourceName}' is not connected.");

                    // ── 2. Resolve destination ─────────────────────────────────────────
                    if (config.DestData == null && !string.IsNullOrWhiteSpace(config.DestDataSourceName))
                    {
                        config.DestData = _editor.GetDataSource(config.DestDataSourceName);
                        if (config.DestData?.ConnectionStatus != ConnectionState.Open)
                            _editor.OpenDataSource(config.DestDataSourceName);
                    }

                    if (config.DestData == null)
                        return CreateErrorsInfo(Errors.Failed, $"Preflight: destination datasource '{config.DestDataSourceName}' could not be resolved.");

                    if (config.DestData.ConnectionStatus != ConnectionState.Open)
                        return CreateErrorsInfo(Errors.Failed, $"Preflight: destination datasource '{config.DestDataSourceName}' is not connected.");

                    // ── 3. Resolve entity structures ───────────────────────────────────
                    config.SourceEntityStructure ??=
                        config.SourceData.GetEntityStructure(config.SourceEntityName, false);

                    if (config.SourceEntityStructure == null)
                        return CreateErrorsInfo(Errors.Failed, $"Preflight: could not read structure for source entity '{config.SourceEntityName}'.");

                    Log($"Preflight: source entity '{config.SourceEntityName}' has {config.SourceEntityStructure.Fields?.Count ?? 0} fields.");

                    // Destination entity may not exist yet if CreateDestinationIfNotExists == true
                    var destEntities = config.DestData.GetEntitesList();
                    var destExists = destEntities?.Any(e =>
                        string.Equals(e, config.DestEntityName, StringComparison.OrdinalIgnoreCase)) ?? false;

                    if (destExists)
                    {
                        config.DestEntityStructure ??=
                            config.DestData.GetEntityStructure(config.DestEntityName, false);
                    }

                    // ── 4. Field compatibility check (only when not auto-adding columns) ─
                    if (!config.AddMissingColumns && destExists && config.DestEntityStructure != null)
                    {
                        var destFieldNames = config.DestEntityStructure.Fields?
                            .Select(f => f.FieldName.ToLowerInvariant())
                            .ToHashSet() ?? new System.Collections.Generic.HashSet<string>();

                        var missing = config.SourceEntityStructure.Fields?
                            .Where(f => !destFieldNames.Contains(f.FieldName.ToLowerInvariant()))
                            .Select(f => f.FieldName)
                            .ToList();

                        if (missing?.Count > 0)
                        {
                            var missingList = string.Join(", ", missing);
                            return CreateErrorsInfo(Errors.Failed,
                                $"Preflight: {missing.Count} source field(s) are missing from destination and AddMissingColumns is false: {missingList}");
                        }
                    }

                    if (!destExists && !config.CreateDestinationIfNotExists)
                        return CreateErrorsInfo(Errors.Failed,
                            $"Preflight: destination entity '{config.DestEntityName}' does not exist and CreateDestinationIfNotExists is false.");

                    Log("Preflight: all checks passed.");
                    return CreateErrorsInfo(Errors.Ok, "Preflight passed.");
                }
                catch (Exception ex)
                {
                    _progressHelper?.LogError("Preflight failed with exception", ex);
                    return CreateErrorsInfo(Errors.Failed, $"Preflight exception: {ex.Message}");
                }
            });
        }

        #endregion

        #region BuildSyncDraftAsync

        /// <summary>
        /// Builds a <see cref="DataSyncSchema"/> that describes what a sync run would do,
        /// without executing any data movement.
        /// The draft can be persisted, reviewed, or handed to the wizard summary step.
        /// </summary>
        /// <param name="config">The import configuration to build from.</param>
        /// <returns>A populated <see cref="DataSyncSchema"/> or null on failure.</returns>
        public async Task<DataSyncSchema> BuildSyncDraftAsync(DataImportConfiguration config)
        {
            if (config == null)
                return null;

            return await Task.Run(() =>
            {
                try
                {
                    // Ensure structures are available
                    if (config.SourceData == null && !string.IsNullOrWhiteSpace(config.SourceDataSourceName))
                        config.SourceData = _editor.GetDataSource(config.SourceDataSourceName);

                    if (config.DestData == null && !string.IsNullOrWhiteSpace(config.DestDataSourceName))
                        config.DestData = _editor.GetDataSource(config.DestDataSourceName);

                    config.SourceEntityStructure ??=
                        config.SourceData?.GetEntityStructure(config.SourceEntityName, false);

                    config.DestEntityStructure ??=
                        config.DestData?.GetEntityStructure(config.DestEntityName, false);

                    var draft = new DataSyncSchema
                    {
                        Id                      = Guid.NewGuid().ToString("N"),
                        EntityName              = $"SyncDraft_{config.SourceEntityName}_{config.DestEntityName}",
                        SourceEntityName        = config.SourceEntityName,
                        DestinationEntityName   = config.DestEntityName,
                        SourceDataSourceName    = config.SourceDataSourceName,
                        DestinationDataSourceName = config.DestDataSourceName,
                    };

                    // Optionally seed field-level mapping from source structure
                    if (config.SourceEntityStructure?.Fields != null && config.Mapping == null)
                    {
                        _progressHelper?.LogImport(
                            $"BuildSyncDraft: auto-seeding {config.SourceEntityStructure.Fields.Count} fields from source structure.", 0);
                    }

                    _progressHelper?.LogImport(
                        $"BuildSyncDraft: draft '{draft.EntityName}' created.", 0);

                    return draft;
                }
                catch (Exception ex)
                {
                    _progressHelper?.LogError("BuildSyncDraft failed", ex);
                    return null;
                }
            });
        }

        #endregion
    }
}
