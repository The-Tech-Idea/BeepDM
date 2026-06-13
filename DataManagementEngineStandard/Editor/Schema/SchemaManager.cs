using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Editor.Schema
{
    /// <summary>
    /// Default <see cref="ISchemaManager"/> implementation. Owns ALL schema concerns
    /// in BeepDM: preflight, draft, drift, snapshot, structure resolution, entity existence,
    /// and entity creation. Other services (MigrationManager, BeepSyncManager, DataImportManager,
    /// ETLEditor) call into this for any schema work.
    /// </summary>
    public class SchemaManager : ISchemaManager
    {
        private readonly IDMEEditor _editor;
        private readonly ISchemaSnapshotStore _store;
        private readonly ISchemaComparator _comparator;
        private readonly ISchemaFingerprinter _fingerprinter;

        public SchemaManager(
            IDMEEditor editor,
            ISchemaSnapshotStore? store = null,
            ISchemaComparator? comparator = null,
            ISchemaFingerprinter? fingerprinter = null)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
            _store  = store ?? new FileSchemaSnapshotStore();
            _comparator = comparator ?? new SchemaComparator();
            _fingerprinter = fingerprinter ?? new SchemaFingerprinter();
        }

        // ──────────────────────────────────────────────────────────────────
        // Resolution
        // ──────────────────────────────────────────────────────────────────

        public async Task<SchemaResolutionResult> ResolveDataSourceAsync(
            string dataSourceName, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(dataSourceName))
                return new SchemaResolutionResult
                {
                    Status = CreateErrors(Errors.Failed, "Data source name is required.")
                };

            return await Task.Run(() =>
            {
                try
                {
                    var ds = _editor.GetDataSource(dataSourceName);
                    if (ds == null)
                        return new SchemaResolutionResult
                        {
                            Status = CreateErrors(Errors.Failed, $"Data source '{dataSourceName}' not found.")
                        };

                    if (ds.ConnectionStatus != ConnectionState.Open)
                        _editor.OpenDataSource(dataSourceName);

                    return new SchemaResolutionResult
                    {
                        DataSource = ds,
                        IsOpen     = ds.ConnectionStatus == ConnectionState.Open,
                        Status     = CreateErrors(Errors.Ok, "Resolved.")
                    };
                }
                catch (Exception ex)
                {
                    return new SchemaResolutionResult
                    {
                        Status = CreateErrors(Errors.Failed, $"Resolve failed: {ex.Message}")
                    };
                }
            }, token).ConfigureAwait(false);
        }

        public async Task<EntityStructure?> LoadEntityStructureAsync(
            string dataSourceName, string entityName, bool refresh = false,
            CancellationToken token = default)
        {
            var resolution = await ResolveDataSourceAsync(dataSourceName, token).ConfigureAwait(false);
            if (resolution.DataSource == null) return null;

            return await Task.Run(() =>
            {
                try { return resolution.DataSource!.GetEntityStructure(entityName, refresh); }
                catch (Exception ex)
                {
                    _editor?.Logger?.WriteLog($"SchemaManager.LoadEntityStructureAsync: {ex.Message}");
                    return null;
                }
            }, token).ConfigureAwait(false);
        }

        public async Task<bool> EntityExistsAsync(
            string dataSourceName, string entityName, CancellationToken token = default)
        {
            var resolution = await ResolveDataSourceAsync(dataSourceName, token).ConfigureAwait(false);
            if (resolution.DataSource == null) return false;
            return await Task.Run(() =>
            {
                try { return resolution.DataSource!.CheckEntityExist(entityName); }
                catch { return false; }
            }, token).ConfigureAwait(false);
        }

        public Task<EntityStructure?> TryGetEntityStructureAsync(
            Type type, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult(GetEntityStructureFromType(type));
        }

        // ──────────────────────────────────────────────────────────────────
        // Preflight
        // ──────────────────────────────────────────────────────────────────

        public async Task<SchemaPreflightResult> RunPreflightAsync(
            SchemaRequest request, Action<string>? log = null, CancellationToken token = default)
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
                        source = _editor.GetDataSource(request.SourceDataSourceName);
                        if (source?.ConnectionStatus != ConnectionState.Open)
                            _editor.OpenDataSource(request.SourceDataSourceName);
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
                        dest = _editor.GetDataSource(request.DestinationDataSourceName);
                        if (dest?.ConnectionStatus != ConnectionState.Open)
                            _editor.OpenDataSource(request.DestinationDataSourceName);
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

                    EntityStructure? destStruct = null;
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
                    SchemaSnapshot? destSnapshot = null;
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
                    _editor?.Logger?.WriteLog($"SchemaManager.RunPreflightAsync: {ex.Message}");
                    return new SchemaPreflightResult
                    {
                        Status = CreateErrors(Errors.Failed, $"Preflight exception: {ex.Message}")
                    };
                }
            }, token).ConfigureAwait(false);
        }

        // ──────────────────────────────────────────────────────────────────
        // Sync draft
        // ──────────────────────────────────────────────────────────────────

        public async Task<SchemaDraftResult> BuildSyncDraftAsync(
            SchemaRequest request, CancellationToken token = default)
        {
            if (request == null)
                return new SchemaDraftResult { Status = CreateErrors(Errors.Failed, "BuildSyncDraft: request is null.") };

            return await Task.Run(() =>
                {
                    try
                    {
                        var source = request.SourceData;
                        if (source == null && !string.IsNullOrWhiteSpace(request.SourceDataSourceName))
                            source = _editor.GetDataSource(request.SourceDataSourceName);

                        var dest = request.DestinationData;
                        if (dest == null && !string.IsNullOrWhiteSpace(request.DestinationDataSourceName))
                            dest = _editor.GetDataSource(request.DestinationDataSourceName);

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

                            _editor?.Logger?.WriteLog($"BuildSyncDraft: auto-seeded {draft.MappedFields.Count} mapped fields from source structure.");
                        }

                        _editor?.Logger?.WriteLog($"BuildSyncDraft: draft '{draft.EntityName}' created.");

                        return new SchemaDraftResult { Draft = draft, Status = CreateErrors(Errors.Ok, "Draft built.") };
                    }
                    catch (Exception ex)
                    {
                        _editor?.Logger?.WriteLog($"SchemaManager.BuildSyncDraftAsync: {ex.Message}");
                        return new SchemaDraftResult { Status = CreateErrors(Errors.Failed, $"BuildSyncDraft exception: {ex.Message}") };
                    }
                }, token).ConfigureAwait(false);
        }

        // ──────────────────────────────────────────────────────────────────
        // Entity creation (wrapper over IDataSource.CreateEntityAs)
        // ──────────────────────────────────────────────────────────────────

        public async Task<SchemaEntityResult> CreateEntityAsync(
            string dataSourceName, EntityStructure entity, CancellationToken token = default)
        {
            if (entity == null || string.IsNullOrWhiteSpace(entity.EntityName))
                return new SchemaEntityResult { Status = CreateErrors(Errors.Failed, "Entity structure / name is required.") };

            var resolution = await ResolveDataSourceAsync(dataSourceName, token).ConfigureAwait(false);
            if (resolution.DataSource == null)
                return new SchemaEntityResult { Status = resolution.Status };

            return await Task.Run(() =>
            {
                try
                {
                    if (resolution.DataSource!.CheckEntityExist(entity.EntityName))
                        return new SchemaEntityResult
                        {
                            Status = CreateErrors(Errors.Ok, $"Entity '{entity.EntityName}' already exists."),
                            Created = false
                        };

                    bool ok = resolution.DataSource!.CreateEntityAs(entity);
                    return new SchemaEntityResult
                    {
                        Status  = ok ? CreateErrors(Errors.Ok, $"Entity '{entity.EntityName}' created.") : CreateErrors(Errors.Failed, $"CreateEntityAs returned false for '{entity.EntityName}'."),
                        Created = ok
                    };
                }
                catch (Exception ex)
                {
                    return new SchemaEntityResult { Status = CreateErrors(Errors.Failed, $"Create entity exception: {ex.Message}") };
                }
            }, token).ConfigureAwait(false);
        }

        // ──────────────────────────────────────────────────────────────────
        // Snapshots & drift (was SchemaInspector)
        // ──────────────────────────────────────────────────────────────────

        public Task<SchemaSnapshot> CaptureFromTypeAsync(
            Type type, string dataSourceName, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            var snap = new SchemaSnapshot
            {
                ContextKey     = BuildKey(dataSourceName, type.Name),
                CapturedAt     = DateTime.UtcNow,
                DataSourceName = dataSourceName,
                EntityName     = type.Name,
                Fields         = ReadStructureFields(GetEntityStructureFromType(type))
            };
            return Task.FromResult(snap);
        }

        public async Task<SchemaSnapshot> CaptureFromDataSourceAsync(
            string dataSourceName, string entityName, bool refresh = true, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            var structure = await LoadEntityStructureAsync(dataSourceName, entityName, refresh, token).ConfigureAwait(false);
            return new SchemaSnapshot
            {
                ContextKey     = BuildKey(dataSourceName, entityName),
                CapturedAt     = DateTime.UtcNow,
                DataSourceName = dataSourceName,
                EntityName     = entityName,
                Fields         = ReadStructureFields(structure)
            };
        }

        public async Task<SchemaDriftReport> InspectAsync(
            Type type, string dataSourceName, string entityName, CancellationToken token = default)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            var desired = await CaptureFromTypeAsync(type, dataSourceName, token).ConfigureAwait(false);
            var actual  = await CaptureFromDataSourceAsync(dataSourceName, entityName, refresh: true, token).ConfigureAwait(false);

            if (actual.Fields.Count == 0)
                return new SchemaDriftReport
                {
                    Baseline = desired,
                    Current  = actual,
                    AddedFields   = new List<SnapshotField>(desired.Fields),
                    RemovedFields = new List<SnapshotField>(),
                    AlteredFields = new List<FieldTypeDrift>()
                };

            return _comparator.Compare(baseline: desired, current: actual);
        }

        public async Task<SchemaSnapshot> SaveBaselineAsync(
            Type type, string dataSourceName, string entityName, CancellationToken token = default)
        {
            var snap = await CaptureFromTypeAsync(type, dataSourceName, token).ConfigureAwait(false);
            await _store.SaveAsync(snap, token).ConfigureAwait(false);
            _editor.AddLogMessage("SchemaManager", $"Saved schema baseline for '{type.Name}' (key: {snap.ContextKey})",
                DateTime.Now, 0, null, Errors.Ok);
            return snap;
        }

        public async Task<SchemaDriftReport?> DiffAgainstBaselineAsync(
            Type type, string dataSourceName, string entityName, CancellationToken token = default)
        {
            var baseline = await _store.LoadAsync(BuildKey(dataSourceName, type.Name), token).ConfigureAwait(false);
            if (baseline == null) return null;
            var current = await CaptureFromTypeAsync(type, dataSourceName, token).ConfigureAwait(false);
            return _comparator.Compare(baseline, current);
        }

        public async Task<SchemaSnapshot> SaveDatabaseBaselineAsync(
            string dataSourceName, string entityName, CancellationToken token = default)
        {
            var snap = await CaptureFromDataSourceAsync(dataSourceName, entityName, refresh: true, token).ConfigureAwait(false);
            await _store.SaveAsync(snap, token).ConfigureAwait(false);
            return snap;
        }

        public async Task<Dictionary<string, SchemaDriftReport>> InspectManyAsync(
            IEnumerable<Type> types, string dataSourceName, CancellationToken token = default)
        {
            var result = new ConcurrentDictionary<string, SchemaDriftReport>(StringComparer.OrdinalIgnoreCase);
            if (types == null) return new Dictionary<string, SchemaDriftReport>(result, StringComparer.OrdinalIgnoreCase);

            var typeList = types.Where(t => t != null).ToList();
            var sema = new SemaphoreSlim(Math.Max(1, Environment.ProcessorCount));

            var tasks = typeList.Select(async type =>
            {
                await sema.WaitAsync(token).ConfigureAwait(false);
                try
                {
                    token.ThrowIfCancellationRequested();
                    var report = await InspectAsync(type, dataSourceName, type.Name, token).ConfigureAwait(false);
                    result[type.Name] = report;
                }
                finally { sema.Release(); }
            });

            await Task.WhenAll(tasks).ConfigureAwait(false);
            return new Dictionary<string, SchemaDriftReport>(result, StringComparer.OrdinalIgnoreCase);
        }

        public async Task<Dictionary<string, SchemaDriftReport>> InspectManyAsync(
            IEnumerable<Type> types, IDataSource dataSource, CancellationToken token = default)
        {
            var result = new ConcurrentDictionary<string, SchemaDriftReport>(StringComparer.OrdinalIgnoreCase);
            if (types == null || dataSource == null)
                return new Dictionary<string, SchemaDriftReport>(result, StringComparer.OrdinalIgnoreCase);

            var typeList = types.Where(t => t != null).ToList();
            var sema = new SemaphoreSlim(Math.Max(1, Environment.ProcessorCount));

            var tasks = typeList.Select(async type =>
            {
                await sema.WaitAsync(token).ConfigureAwait(false);
                try
                {
                    token.ThrowIfCancellationRequested();
                    var desired = await CaptureFromTypeAsync(type, dataSource.DatasourceName, token).ConfigureAwait(false);
                    var actual = await CaptureFromDataSourceAsync(dataSource.DatasourceName, type.Name, refresh: true, token).ConfigureAwait(false);
                    result[type.Name] = actual.Fields.Count == 0
                        ? new SchemaDriftReport
                          {
                              Baseline = desired, Current = actual,
                              AddedFields   = new List<SnapshotField>(desired.Fields),
                              RemovedFields = new List<SnapshotField>(),
                              AlteredFields = new List<FieldTypeDrift>()
                          }
                         : _comparator.Compare(desired, actual);
                }
                finally { sema.Release(); }
            });

            await Task.WhenAll(tasks).ConfigureAwait(false);
            return new Dictionary<string, SchemaDriftReport>(result, StringComparer.OrdinalIgnoreCase);
        }

        // ──────────────────────────────────────────────────────────────────
        // Change plan generation
        // ──────────────────────────────────────────────────────────────────

        public Task<SchemaChangePlan> GenerateChangePlanAsync(
            SchemaDriftReport driftReport,
            CancellationToken token = default)
        {
            if (driftReport == null)
                return Task.FromResult(new SchemaChangePlan());

            return Task.Run(() =>
            {
                var changes = new List<SchemaChange>();

                foreach (var field in driftReport.RemovedFields)
                    changes.Add(new SchemaChange
                    {
                        ChangeType   = "DROP_COLUMN",
                        FieldName    = field.Name,
                        DataType     = field.DataType,
                        IsNullable   = field.IsNullable,
                        MaxLength    = field.MaxLength,
                        Description  = $"Drop column '{field.Name}'"
                    });

                foreach (var field in driftReport.AddedFields)
                    changes.Add(new SchemaChange
                    {
                        ChangeType   = "ADD_COLUMN",
                        FieldName    = field.Name,
                        DataType     = field.DataType,
                        IsNullable   = field.IsNullable,
                        MaxLength    = field.MaxLength,
                        Description  = $"Add column '{field.Name}' ({field.DataType})"
                    });

                foreach (var drift in driftReport.AlteredFields)
                    changes.Add(new SchemaChange
                    {
                        ChangeType   = "ALTER_COLUMN",
                        FieldName    = drift.FieldName,
                        DataType     = drift.CurrentType,
                        Description  = drift.Description
                    });

                return new SchemaChangePlan
                {
                    Baseline = driftReport.Baseline,
                    Current  = driftReport.Current,
                    Changes  = changes
                };
            }, token);
        }

        // ──────────────────────────────────────────────────────────────────
        // Internals
        // ──────────────────────────────────────────────────────────────────

        public static string BuildKey(string dataSourceName, string entityName) =>
            $"{dataSourceName}/{entityName}";

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
            catch { /* fall through to reflection */ }

            var fallback = new EntityStructure
            {
                EntityName = type.Name,
                Fields = new List<EntityField>()
            };

            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                fallback.Fields.Add(new EntityField
                {
                    FieldName    = prop.Name,
                    Fieldtype    = prop.PropertyType.Name,
                    AllowDBNull  = Nullable.GetUnderlyingType(prop.PropertyType) != null || !prop.PropertyType.IsValueType
                });
            }

            return fallback;
        }

        private static List<SnapshotField> ReadStructureFields(EntityStructure? structure)
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

        private static IErrorsInfo CreateErrors(Errors flag, string message) =>
            new ErrorsInfo { Flag = flag, Message = message };
    }
}
