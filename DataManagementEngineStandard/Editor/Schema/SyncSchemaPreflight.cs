using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Editor.Schema
{
    /// <summary>
    /// Stateless helper for cross-datasource SYNC preflight and sync-draft production —
    /// "will the destination accept the source, and here is the field mapping." This is a
    /// data-movement concern (source → destination), NOT single-datasource schema evolution;
    /// schema changes go through <c>MigrationManager</c>. Extracted verbatim from the removed
    /// <c>SchemaManager</c> so both <c>DataImportManager</c> and <c>BeepSyncManager</c> can share
    /// one implementation without either owning it.
    /// </summary>
    public static class SyncSchemaPreflight
    {
        /// <summary>Cross-datasource preflight: resolves source+destination and checks compatibility.</summary>
        public static async Task<SchemaPreflightResult> RunPreflightAsync(
            IDMEEditor editor, SchemaRequest request, Action<string> log = null, CancellationToken token = default)
        {
            if (request == null)
                return new SchemaPreflightResult { Status = CreateErrors(Errors.Failed, "Preflight: request is null.") };

            return await Task.Run(() =>
            {
                try
                {
                    void Log(string msg) => log?.Invoke(msg);

                    Log("Preflight: initialising data sources…");

                    // Source
                    var source = request.SourceData;
                    if (source == null && !string.IsNullOrWhiteSpace(request.SourceDataSourceName))
                    {
                        source = editor.GetDataSource(request.SourceDataSourceName);
                        if (source?.ConnectionStatus != ConnectionState.Open)
                            editor.OpenDataSource(request.SourceDataSourceName);
                    }

                    if (source == null)
                        return new SchemaPreflightResult
                        {
                            Status = CreateErrors(Errors.Failed, $"Preflight: source datasource '{request.SourceDataSourceName}' could not be resolved.")
                        };

                    bool sourceConnected = source.ConnectionStatus == ConnectionState.Open;
                    if (!sourceConnected)
                        return new SchemaPreflightResult
                        {
                            SourceResolved  = true,
                            SourceConnected = false,
                            Status          = CreateErrors(Errors.Failed, $"Preflight: source datasource '{request.SourceDataSourceName}' is not connected.")
                        };

                    // Destination
                    var dest = request.DestinationData;
                    if (dest == null && !string.IsNullOrWhiteSpace(request.DestinationDataSourceName))
                    {
                        dest = editor.GetDataSource(request.DestinationDataSourceName);
                        if (dest?.ConnectionStatus != ConnectionState.Open)
                            editor.OpenDataSource(request.DestinationDataSourceName);
                    }

                    if (dest == null)
                        return new SchemaPreflightResult
                        {
                            SourceResolved  = true,
                            SourceConnected = sourceConnected,
                            Status          = CreateErrors(Errors.Failed, $"Preflight: destination datasource '{request.DestinationDataSourceName}' could not be resolved.")
                        };

                    bool destConnected = dest.ConnectionStatus == ConnectionState.Open;
                    if (!destConnected)
                        return new SchemaPreflightResult
                        {
                            SourceResolved       = true, SourceConnected = sourceConnected,
                            DestinationResolved  = true,
                            Status               = CreateErrors(Errors.Failed, $"Preflight: destination datasource '{request.DestinationDataSourceName}' is not connected.")
                        };

                    // Structures
                    var sourceStruct = request.SourceEntityStructure
                                       ?? source.GetEntityStructure(request.SourceEntityName, false);
                    if (sourceStruct == null)
                        return new SchemaPreflightResult
                        {
                            SourceResolved = true, SourceConnected = sourceConnected,
                            DestinationResolved = true, DestinationConnected = destConnected,
                            Status = CreateErrors(Errors.Failed, $"Preflight: could not read structure for source entity '{request.SourceEntityName}'.")
                        };

                    Log($"Preflight: source entity '{request.SourceEntityName}' has {sourceStruct.Fields?.Count ?? 0} fields.");

                    var destEntities = dest.GetEntitesList();
                    bool destExists = destEntities?.Any(e =>
                        string.Equals(e, request.DestinationEntityName, StringComparison.OrdinalIgnoreCase)) ?? false;

                    EntityStructure destStruct = null;
                    if (destExists)
                        destStruct = request.DestinationEntityStructure
                                     ?? dest.GetEntityStructure(request.DestinationEntityName, false);

                    // Field-compat (only when not auto-adding)
                    var missing = Array.Empty<string>();
                    if (!request.AddMissingColumns && destExists && destStruct != null)
                    {
                        var destFieldNames = destStruct.Fields?
                            .Select(f => f.FieldName.ToLowerInvariant())
                            .ToHashSet() ?? new HashSet<string>();

                        missing = sourceStruct.Fields?
                            .Where(f => !destFieldNames.Contains(f.FieldName.ToLowerInvariant()))
                            .Select(f => f.FieldName)
                            .ToArray() ?? Array.Empty<string>();

                        if (missing.Length > 0)
                            return new SchemaPreflightResult
                            {
                                SourceResolved = true, SourceConnected = sourceConnected,
                                DestinationResolved = true, DestinationConnected = destConnected,
                                SourceStructureLoaded = true, DestinationStructureLoaded = true,
                                DestinationExisted = destExists,
                                MissingDestinationFields = missing,
                                Status = CreateErrors(Errors.Failed,
                                    $"Preflight: {missing.Length} source field(s) are missing from destination and AddMissingColumns is false: {string.Join(", ", missing)}")
                            };
                    }

                    if (!destExists && !request.CreateDestinationIfNotExists)
                        return new SchemaPreflightResult
                        {
                            SourceResolved = true, SourceConnected = sourceConnected,
                            DestinationResolved = true, DestinationConnected = destConnected,
                            SourceStructureLoaded = true, DestinationStructureLoaded = false,
                            DestinationExisted = destExists,
                            Status = CreateErrors(Errors.Failed,
                                $"Preflight: destination entity '{request.DestinationEntityName}' does not exist and CreateDestinationIfNotExists is false.")
                        };

                    Log("Preflight: all checks passed.");

                    // Capture baseline snapshot
                    SchemaSnapshot destSnapshot = null;
                    if (destExists && destStruct != null)
                    {
                        destSnapshot = new SchemaSnapshot
                        {
                            ContextKey     = $"{request.DestinationDataSourceName}/{request.DestinationEntityName}",
                            DataSourceName = request.DestinationDataSourceName,
                            EntityName     = request.DestinationEntityName,
                            Fields         = destStruct.Fields?
                                .Select(f => new SnapshotField
                                {
                                    Name       = f.FieldName,
                                    DataType   = f.ColumnTypeName ?? string.Empty,
                                    IsNullable = f.AllowDBNull,
                                    MaxLength  = f.Size
                                }).ToList() ?? new()
                        };
                    }

                    return new SchemaPreflightResult
                    {
                        SourceResolved = true, SourceConnected = sourceConnected,
                        DestinationResolved = true, DestinationConnected = destConnected,
                        SourceStructureLoaded = true,
                        DestinationStructureLoaded = destExists,
                        DestinationExisted = destExists,
                        MissingDestinationFields = missing,
                        DestinationSnapshot = destSnapshot,
                        Status = CreateErrors(Errors.Ok, "Preflight passed.")
                    };
                }
                catch (Exception ex)
                {
                    editor?.Logger?.WriteLog($"SyncSchemaPreflight.RunPreflightAsync: {ex.Message}");
                    return new SchemaPreflightResult
                    {
                        Status = CreateErrors(Errors.Failed, $"Preflight exception: {ex.Message}")
                    };
                }
            }, token).ConfigureAwait(false);
        }

        /// <summary>Builds a <see cref="DataSyncSchema"/> draft (field mapping) without moving data.</summary>
        public static async Task<SchemaDraftResult> BuildSyncDraftAsync(
            IDMEEditor editor, SchemaRequest request, CancellationToken token = default)
        {
            if (request == null)
                return new SchemaDraftResult { Status = CreateErrors(Errors.Failed, "BuildSyncDraft: request is null.") };

            return await Task.Run(() =>
                {
                    try
                    {
                        var source = request.SourceData;
                        if (source == null && !string.IsNullOrWhiteSpace(request.SourceDataSourceName))
                            source = editor.GetDataSource(request.SourceDataSourceName);

                        var dest = request.DestinationData;
                        if (dest == null && !string.IsNullOrWhiteSpace(request.DestinationDataSourceName))
                            dest = editor.GetDataSource(request.DestinationDataSourceName);

                        var sourceStruct = request.SourceEntityStructure
                                           ?? source?.GetEntityStructure(request.SourceEntityName, false);
                        var destStruct = request.DestinationEntityStructure
                                          ?? dest?.GetEntityStructure(request.DestinationEntityName, false);

                        var draft = new DataSyncSchema
                        {
                            Id                        = Guid.NewGuid().ToString("N"),
                            EntityName                = $"SyncDraft_{request.SourceEntityName}_{request.DestinationEntityName}",
                            SourceEntityName          = request.SourceEntityName,
                            DestinationEntityName     = request.DestinationEntityName,
                            SourceDataSourceName      = request.SourceDataSourceName,
                            DestinationDataSourceName = request.DestinationDataSourceName,
                        };

                        if (sourceStruct?.Fields != null)
                        {
                            var destFieldNames = destStruct?.Fields?
                                .Select(f => f.FieldName)
                                .ToHashSet(StringComparer.OrdinalIgnoreCase)
                                ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                            foreach (var sourceField in sourceStruct.Fields)
                            {
                                var matchedDest = string.IsNullOrWhiteSpace(sourceField.FieldName)
                                    ? null
                                    : destStruct?.Fields?.FirstOrDefault(df =>
                                        string.Equals(df.FieldName, sourceField.FieldName, StringComparison.OrdinalIgnoreCase));

                                draft.MappedFields.Add(new FieldSyncData
                                {
                                    SourceField          = sourceField.FieldName ?? string.Empty,
                                    DestinationField     = matchedDest?.FieldName ?? sourceField.FieldName ?? string.Empty,
                                    SourceFieldType      = sourceField.Fieldtype ?? string.Empty,
                                    DestinationFieldType = matchedDest?.Fieldtype ?? sourceField.Fieldtype ?? string.Empty,
                                });
                            }

                            editor?.Logger?.WriteLog($"BuildSyncDraft: auto-seeded {draft.MappedFields.Count} mapped fields from source structure.");
                        }

                        editor?.Logger?.WriteLog($"BuildSyncDraft: draft '{draft.EntityName}' created.");

                        return new SchemaDraftResult { Draft = draft, Status = CreateErrors(Errors.Ok, "Draft built.") };
                    }
                    catch (Exception ex)
                    {
                        editor?.Logger?.WriteLog($"SyncSchemaPreflight.BuildSyncDraftAsync: {ex.Message}");
                        return new SchemaDraftResult { Status = CreateErrors(Errors.Failed, $"BuildSyncDraft exception: {ex.Message}") };
                    }
                }, token).ConfigureAwait(false);
        }

        private static IErrorsInfo CreateErrors(Errors flag, string message) =>
            new ErrorsInfo { Flag = flag, Message = message };
    }
}
