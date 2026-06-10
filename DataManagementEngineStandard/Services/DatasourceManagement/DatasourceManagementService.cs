using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Migration;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Services.DatasourceManagement
{
    public class DatasourceManagementService
    {
        private readonly IDMEEditor _editor;

        public DatasourceManagementService(IDMEEditor editor)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        }

        public List<ConnectionProperties> GetAllDatasources()
        {
            return _editor.ConfigEditor?.DataConnections ?? new List<ConnectionProperties>();
        }

        public ConnectionProperties GetDatasource(string name)
        {
            return _editor.ConfigEditor?.DataConnections
                ?.FirstOrDefault(c => string.Equals(c.ConnectionName, name, StringComparison.OrdinalIgnoreCase));
        }

        public bool DatasourceExists(string name)
        {
            return _editor.ConfigEditor?.DataConnectionExist(name) == true
                || _editor.CheckDataSourceExist(name);
        }

        public IErrorsInfo AddDatasource(ConnectionProperties connection)
        {
            if (connection == null)
                return CreateError("Connection properties cannot be null.");

            if (string.IsNullOrWhiteSpace(connection.ConnectionName))
                return CreateError("Connection name is required.");

            if (_editor.ConfigEditor?.DataConnectionExist(connection.ConnectionName) == true)
                return CreateError($"Connection '{connection.ConnectionName}' already exists.");

            var result = _editor.ConfigEditor?.AddDataConnection(connection) == true;
            if (result)
            {
                _editor.ConfigEditor?.SaveDataconnectionsValues();
                _editor.AddLogMessage("DatasourceMgr",
                    $"Added datasource '{connection.ConnectionName}'",
                    DateTime.Now, 0, null, Errors.Ok);
                return new ErrorsInfo { Flag = Errors.Ok };
            }

            return CreateError($"Failed to add datasource '{connection.ConnectionName}'.");
        }

        public IErrorsInfo RemoveDatasource(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return CreateError("Datasource name is required.");

            var conn = GetDatasource(name);
            if (conn == null)
                return CreateError($"Datasource '{name}' not found.");

            try
            {
                _editor.CloseDataSource(name);
            }
            catch { }

            var result = _editor.RemoveDataDource(name);
            if (result)
            {
                _editor.ConfigEditor?.RemoveConnByName(name);
                _editor.ConfigEditor?.SaveDataconnectionsValues();
                _editor.AddLogMessage("DatasourceMgr",
                    $"Removed datasource '{name}'",
                    DateTime.Now, 0, null, Errors.Ok);
                return new ErrorsInfo { Flag = Errors.Ok };
            }

            return CreateError($"Failed to remove datasource '{name}'.");
        }

        public ConnectionState TestConnection(ConnectionProperties connection)
        {
            if (connection == null) return ConnectionState.Broken;

            try
            {
                var ds = _editor.CreateNewDataSourceConnection(connection, connection.ConnectionName);
                if (ds == null) return ConnectionState.Broken;

                var state = ds.Openconnection();
                ds.Closeconnection();
                return state;
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage("DatasourceMgr",
                    $"Connection test failed for '{connection.ConnectionName}': {ex.Message}",
                    DateTime.Now, 0, null, Errors.Failed);
                return ConnectionState.Broken;
            }
        }

        public DatasourceStatus GetDatasourceStatus(string name)
        {
            var status = new DatasourceStatus
            {
                Name = name,
                IsConnected = false,
                State = ConnectionState.Closed
            };

            try
            {
                var ds = _editor.GetDataSource(name);
                if (ds == null)
                {
                    status.State = ConnectionState.Broken;
                    return status;
                }

                status.State = ds.ConnectionStatus;
                status.IsConnected = ds.ConnectionStatus == ConnectionState.Open;

                var conn = GetDatasource(name);
                if (conn != null)
                {
                    status.Category = conn.Category.ToString();
                    status.DataSourceType = conn.DatabaseType.ToString();
                }
            }
            catch (Exception ex)
            {
                status.State = ConnectionState.Broken;
                status.ErrorMessage = ex.Message;
            }

            return status;
        }

        public async Task<IErrorsInfo> ApplySchemaToDatasource(
            string datasourceName,
            IEnumerable<Type> entityTypes,
            bool detectRelationships = true,
            bool applyForeignKeys = false,
            bool applyIndexes = false,
            IProgress<PassedArgs> progress = null)
        {
            var result = await ApplySchemaToDatasourceWithInspection(
                datasourceName, entityTypes, detectRelationships, applyForeignKeys, applyIndexes, progress);
            return result.MigrationResult;
        }

        /// <summary>
        /// Same as <see cref="ApplySchemaToDatasource"/> but also returns a per-entity
        /// <see cref="TheTechIdea.Beep.Editor.Importing.Schema.SchemaDriftReport"/>
        /// describing the structural differences between each .NET entity type and the
        /// live database table BEFORE the migration was applied.
        /// </summary>
        public async Task<SchemaApplyResult> ApplySchemaToDatasourceWithInspection(
            string datasourceName,
            IEnumerable<Type> entityTypes,
            bool detectRelationships = true,
            bool applyForeignKeys = false,
            bool applyIndexes = false,
            IProgress<PassedArgs> progress = null)
        {
            var result = new SchemaApplyResult { DatasourceName = datasourceName };

            if (string.IsNullOrWhiteSpace(datasourceName))
            {
                result.MigrationResult = CreateError("Datasource name is required.");
                return result;
            }

            var typesList = entityTypes?.ToList();
            if (typesList == null || typesList.Count == 0)
            {
                result.MigrationResult = CreateError("At least one entity type is required.");
                return result;
            }

            try
            {
                var ds = _editor.GetDataSource(datasourceName);
                if (ds == null)
                {
                    result.MigrationResult = CreateError($"Datasource '{datasourceName}' not found.");
                    return result;
                }

                var connState = await DataSourceLifecycleHelper.OpenWithRetryAsync(ds);
                if (connState != ConnectionState.Open)
                {
                    result.MigrationResult = CreateError($"Could not open connection to '{datasourceName}'. State: {connState}");
                    return result;
                }

                // ── NEW: schema diff BEFORE migration (compares .NET class to live DB table)
                var inspector = new SchemaInspector(_editor);
                result.SchemaDrift = await inspector.InspectManyAsync(typesList, ds);
                _editor.AddLogMessage("DatasourceMgr",
                    $"Schema drift report: {result.SchemaDrift.Count} entities inspected before migration",
                    DateTime.Now, 0, null, Errors.Ok);

                var migration = new MigrationManager(_editor, ds)
                {
                    MigrateDataSource = ds
                };

                var plan = migration.BuildMigrationPlanForTypes(
                    typesList,
                    detectRelationships: detectRelationships,
                    applyForeignKeys: applyForeignKeys,
                    applyIndexes: applyIndexes);
                if (plan == null || plan.Operations == null || plan.Operations.Count == 0)
                {
                    result.MigrationResult = new ErrorsInfo
                    {
                        Flag = Errors.Ok,
                        Message = "Schema is already up to date. No changes needed."
                    };
                    return result;
                }

                var migrationResult = migration.ExecuteMigrationPlan(plan, null, null, progress);
                if (migrationResult == null || !migrationResult.Success)
                {
                    result.MigrationResult = CreateError(migrationResult?.Message ?? "Schema migration failed.");
                    return result;
                }

                AppendSchemaToHistory(datasourceName, ds, migrationResult, plan);

                _editor.AddLogMessage("DatasourceMgr",
                    $"Applied schema migration to '{datasourceName}': {migrationResult.AppliedCount} operations",
                    DateTime.Now, 0, null, Errors.Ok);

                result.MigrationResult = new ErrorsInfo { Flag = Errors.Ok, Message = migrationResult.Message };
                return result;
            }
            catch (Exception ex)
            {
                result.MigrationResult = CreateError($"Schema migration failed: {ex.Message}", ex);
                return result;
            }
        }

        /// <summary>
        /// Inspects the schema without applying any changes. Returns a drift report
        /// describing the differences between the supplied .NET entity types and the
        /// live database tables.
        /// </summary>
        public async Task<Dictionary<string, TheTechIdea.Beep.Editor.Importing.Schema.SchemaDriftReport>> InspectSchemaAsync(
            string datasourceName,
            IEnumerable<Type> entityTypes,
            CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(datasourceName))
                return new Dictionary<string, TheTechIdea.Beep.Editor.Importing.Schema.SchemaDriftReport>();
            if (entityTypes == null) return new Dictionary<string, TheTechIdea.Beep.Editor.Importing.Schema.SchemaDriftReport>();

            var ds = _editor.GetDataSource(datasourceName);
            if (ds == null) return new Dictionary<string, TheTechIdea.Beep.Editor.Importing.Schema.SchemaDriftReport>();

            var connState = await DataSourceLifecycleHelper.OpenWithRetryAsync(ds);
            if (connState != ConnectionState.Open)
                return new Dictionary<string, TheTechIdea.Beep.Editor.Importing.Schema.SchemaDriftReport>();

            var inspector = new SchemaInspector(_editor);
            return await inspector.InspectManyAsync(entityTypes, ds, token).ConfigureAwait(false);
        }

        private void AppendSchemaToHistory(string datasourceName, IDataSource ds, MigrationExecutionResult result, MigrationPlanArtifact plan)
        {
            try
            {
                var configEditor = _editor.ConfigEditor as ConfigEditor;
                if (configEditor == null) return;

                var record = new MigrationRecord
                {
                    MigrationId = Guid.NewGuid().ToString("N")[..12],
                    Name = $"Schema_{datasourceName}_{DateTime.UtcNow:yyyyMMddHHmmss}",
                    AppliedOnUtc = DateTime.UtcNow,
                    Success = result?.Success ?? false,
                    Notes = result?.Message ?? string.Empty,
                    Steps = new List<MigrationStep>()
                };

                if (plan?.Operations != null)
                {
                    var completedBySequence = result?.Checkpoint?.Steps?
                        .GroupBy(step => step.Sequence)
                        .ToDictionary(group => group.Key, group => group.First())
                        ?? new Dictionary<int, MigrationExecutionStep>();

                    for (var i = 0; i < plan.Operations.Count; i++)
                    {
                        var op = plan.Operations[i];
                        if (op == null) continue;

                        completedBySequence.TryGetValue(i + 1, out var execStep);
                        record.Steps.Add(new MigrationStep
                        {
                            Operation = op.Kind.ToString(),
                            EntityName = op.EntityName ?? op.TargetName ?? string.Empty,
                            Success = execStep != null
                                ? execStep.Status == MigrationExecutionStepStatus.Completed
                                : result?.Success == true
                        });
                    }
                }

                configEditor.AppendMigrationRecord(datasourceName, ds?.DatasourceType ?? DataSourceType.Unknown, record);
            }
            catch { }
        }

        public void SaveConfiguration()
        {
            _editor.ConfigEditor?.SaveDataconnectionsValues();
        }

        private static IErrorsInfo CreateError(string message, Exception ex = null)
        {
            return new ErrorsInfo
            {
                Flag = Errors.Failed,
                Message = message,
                Ex = ex
            };
        }
    }

    public class DatasourceStatus
    {
        public string Name { get; set; } = string.Empty;
        public bool IsConnected { get; set; }
        public ConnectionState State { get; set; } = ConnectionState.Closed;
        public string Category { get; set; } = string.Empty;
        public string DataSourceType { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result of <see cref="DatasourceManagementService.ApplySchemaToDatasourceWithInspection"/>.
    /// Carries both the legacy <see cref="IErrorsInfo"/> (for backward compatibility) and the
    /// per-entity <see cref="TheTechIdea.Beep.Editor.Importing.Schema.SchemaDriftReport"/>s
    /// captured before the migration was applied.
    /// </summary>
    public class SchemaApplyResult
    {
        public string DatasourceName { get; set; } = string.Empty;
        public IErrorsInfo MigrationResult { get; set; } = new ErrorsInfo { Flag = Errors.Ok };
        public Dictionary<string, TheTechIdea.Beep.Editor.Importing.Schema.SchemaDriftReport> SchemaDrift { get; set; }
            = new Dictionary<string, TheTechIdea.Beep.Editor.Importing.Schema.SchemaDriftReport>(StringComparer.OrdinalIgnoreCase);
    }
}
