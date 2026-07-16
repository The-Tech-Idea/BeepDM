using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Migration;

namespace TheTechIdea.Beep.Editor.Migration
{
    public class MigrationTrackingService
    {
        private readonly IDMEEditor _editor;

        public MigrationTrackingService(IDMEEditor editor)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        }

        public MigrationHistory GetMigrationHistory(string datasourceName)
        {
            if (string.IsNullOrWhiteSpace(datasourceName))
                return new MigrationHistory();

            var configEditor = _editor.ConfigEditor as ConfigEditor;
            return configEditor?.LoadMigrationHistory(datasourceName) ?? new MigrationHistory();
        }

        public IErrorsInfo ExecuteTrackedMigration(
            string datasourceName,
            IEnumerable<Type> entityTypes,
            bool detectRelationships = true,
            IProgress<PassedArgs> progress = null,
            bool applyForeignKeys = false,
            bool applyIndexes = false)
        {
            if (string.IsNullOrWhiteSpace(datasourceName))
                return CreateError("Datasource name is required.");

            var typesList = entityTypes?.ToList();
            if (typesList == null || typesList.Count == 0)
                return CreateError("At least one entity type is required.");

            try
            {
                var ds = _editor.GetDataSource(datasourceName);
                if (ds == null)
                    return CreateError($"Datasource '{datasourceName}' not found.");

                if (ds.ConnectionStatus != ConnectionState.Open)
                {
                    var state = ds.Openconnection();
                    if (state != ConnectionState.Open)
                        return CreateError($"Could not open connection to '{datasourceName}'.");
                }

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
                    return new ErrorsInfo
                    {
                        Flag = Errors.Ok,
                        Message = "Schema is already up to date. No migration needed."
                    };
                }

                var result = migration.ExecuteMigrationPlan(plan, null, null, progress);

                var dataSourceType = DataSourceType.Unknown;
                var conn = _editor.ConfigEditor?.DataConnections
                    ?.FirstOrDefault(c => string.Equals(c.ConnectionName, datasourceName, StringComparison.OrdinalIgnoreCase));
                if (conn != null)
                    dataSourceType = conn.DatabaseType;

                MigrationRecordWriter.WritePlanExecution(_editor, datasourceName, dataSourceType, plan, result);

                _editor.AddLogMessage("MigrationTracker",
                    result?.Success == true
                        ? $"Migration applied to '{datasourceName}': {result.AppliedCount} operations"
                        : $"Migration failed for '{datasourceName}': {result?.Message}",
                    DateTime.Now, 0, null, result?.Success == true ? Errors.Ok : Errors.Failed);

                return result?.Success == true
                    ? new ErrorsInfo { Flag = Errors.Ok, Message = result.Message }
                    : CreateError(result?.Message ?? "Migration failed.");
            }
            catch (Exception ex)
            {
                return CreateError($"Migration failed: {ex.Message}", ex);
            }
        }

        public bool CanUndo(string datasourceName)
        {
            if (string.IsNullOrWhiteSpace(datasourceName)) return false;

            var history = GetMigrationHistory(datasourceName);
            if (history.Migrations == null || history.Migrations.Count == 0) return false;

            var lastMigration = history.Migrations.Last();
            return lastMigration.Success && !LastMigrationIsUndone(history);
        }

        public UndoPreview BuildUndoPreview(string datasourceName)
        {
            var preview = new UndoPreview
            {
                DatasourceName = datasourceName,
                CanUndo = false
            };

            if (string.IsNullOrWhiteSpace(datasourceName)) return preview;

            var history = GetMigrationHistory(datasourceName);
            if (history.Migrations == null || history.Migrations.Count == 0)
            {
                preview.Message = "No migration history available.";
                return preview;
            }

            if (LastMigrationIsUndone(history))
            {
                preview.Message = "Last migration has already been undone.";
                return preview;
            }

            var lastMigration = history.Migrations.Last();
            if (!lastMigration.Success)
            {
                preview.Message = "Last migration did not complete successfully.";
                return preview;
            }

            preview.CanUndo = true;
            preview.LastMigrationId = lastMigration.MigrationId;
            preview.LastMigrationName = lastMigration.Name;
            preview.LastMigrationDate = lastMigration.AppliedOnUtc;
            preview.PendingSteps = ResolveCompensationSteps(lastMigration);
            preview.Message = $"Ready to undo migration '{lastMigration.Name}' ({preview.PendingSteps.Count} steps).";

            return preview;
        }

        public IErrorsInfo UndoLastMigration(string datasourceName)
        {
            var preview = BuildUndoPreview(datasourceName);
            if (!preview.CanUndo)
                return CreateError(preview.Message);

            try
            {
                var history = GetMigrationHistory(datasourceName);
                var lastMigration = history.Migrations.Last();

                lastMigration.Success = false;
                lastMigration.Notes += " | Undone at " + DateTime.UtcNow.ToString("u");

                var ds = _editor.GetDataSource(datasourceName);
                if (ds != null)
                {
                    try
                    {
                        var opened = false;
                        if (ds.ConnectionStatus != ConnectionState.Open)
                        {
                            opened = OpenWithRetrySync(ds);
                            if (!opened)
                            {
                                _editor.AddLogMessage("MigrationTracker",
                                    $"Undo failed: could not open connection to '{datasourceName}'",
                                    DateTime.Now, 0, null, Errors.Failed);
                                return CreateError($"Could not open connection to '{datasourceName}' for undo.");
                            }
                        }

                        foreach (var step in preview.PendingSteps)
                        {
                            if (string.IsNullOrWhiteSpace(step.CompensationSql) ||
                                step.CompensationSql.StartsWith("--"))
                                continue;

                            ds.ExecuteSql(step.CompensationSql);
                        }
                    }
                    catch (Exception ex)
                    {
                        _editor.AddLogMessage("MigrationTracker",
                            $"Undo partial failure: {ex.Message}",
                            DateTime.Now, 0, null, Errors.Failed);
                    }
                }

                var configEditor = _editor.ConfigEditor as ConfigEditor;
                configEditor?.SaveMigrationHistory(history);
                _editor.AddLogMessage("MigrationTracker",
                    $"Undid last migration '{lastMigration.Name}' on '{datasourceName}'",
                    DateTime.Now, 0, null, Errors.Ok);

                return new ErrorsInfo { Flag = Errors.Ok, Message = $"Migration '{lastMigration.Name}' undone." };
            }
            catch (Exception ex)
            {
                return CreateError($"Undo failed: {ex.Message}", ex);
            }
        }

        private bool OpenWithRetrySync(IDataSource ds)
        {
            const int maxRetries = 3;
            var attempt = 0;
            while (attempt < maxRetries)
            {
                try
                {
                    var state = ds.Openconnection();
                    if (state == ConnectionState.Open) return true;
                }
                catch (Exception ex)
                {
                    // Transient open failure: retry up to maxRetries, but surface each attempt so a
                    // persistent connectivity fault is visible rather than silently retried away.
                    _editor?.AddLogMessage("MigrationManager",
                        $"OpenWithRetrySync: connection attempt {attempt + 1}/{maxRetries} failed: {ex.Message}",
                        DateTime.Now, 0, null, Errors.Warning);
                }
                attempt++;
            }
            return false;
        }

        public bool CanUndoLastMigration(string datasourceName)
        {
            return CanUndo(datasourceName);
        }

        private List<UndoStepInfo> ResolveCompensationSteps(MigrationRecord record)
        {
            if (record?.Steps == null || record.Steps.Count == 0)
                return new List<UndoStepInfo>();

            var ops = new List<MigrationPlanOperation>();

            for (int i = 0; i < record.Steps.Count; i++)
            {
                var s = record.Steps[i];
                if (s == null) continue;
                var kind = ParseOperationKind(s.Operation);
                ops.Add(new MigrationPlanOperation
                {
                    EntityName = s.EntityName ?? string.Empty,
                    TargetName = s.EntityName ?? string.Empty,
                    Kind = kind,
                    RiskLevel = kind == MigrationPlanOperationKind.CreateEntity
                        ? MigrationPlanRiskLevel.High
                        : MigrationPlanRiskLevel.Medium,
                    IsDestructive = kind == MigrationPlanOperationKind.CreateEntity
                });
            }

            if (ops.Count == 0)
                return new List<UndoStepInfo>();

            var category = DatasourceCategory.NONE;
            DataSourceType dsType = DataSourceType.Unknown;
            try
            {
                var conn = _editor.ConfigEditor?.DataConnections?
                    .FirstOrDefault(c => string.Equals(c.ConnectionName, record.Name, StringComparison.OrdinalIgnoreCase)
                                      || (c.ConnectionName != null && record.Name != null && c.ConnectionName.Contains(record.Name)));
                if (conn != null)
                {
                    category = conn.Category;
                    dsType = conn.DatabaseType;
                }
            }
            catch (Exception ex)
            {
                _editor?.AddLogMessage("MigrationManager",
                    $"ResolveCompensationSteps: could not resolve connection metadata for '{record.Name}', " +
                    $"falling back to NONE/Unknown: {ex.Message}",
                    DateTime.Now, 0, null, Errors.Warning);
            }

            var plan = new MigrationPlanArtifact
            {
                DataSourceName = record.Name,
                DataSourceType = dsType,
                DataSourceCategory = category,
                Operations = ops
            };

            var manager = new MigrationManager(_editor);
            MigrationCompensationPlan compensation;
            try
            {
                compensation = manager.BuildCompensationPlan(plan);
            }
            catch
            {
                compensation = new MigrationCompensationPlan();
            }

            var result = new List<UndoStepInfo>();
            for (int i = 0; i < record.Steps.Count; i++)
            {
                var s = record.Steps[i];
                if (s == null) continue;

                var comp = compensation?.Actions?
                    .FirstOrDefault(a => string.Equals(a.EntityName, s.EntityName, StringComparison.OrdinalIgnoreCase)
                                      && a.OperationKind == ParseOperationKind(s.Operation));

                result.Add(new UndoStepInfo
                {
                    Operation = s.Operation,
                    EntityName = s.EntityName ?? string.Empty,
                    CompensationSql = comp?.RollbackSqlPreview ?? GenerateFallbackCompensationSql(s.Operation, s.EntityName)
                });
            }

            return result;
        }

        private static string GenerateFallbackCompensationSql(string operation, string entityName)
        {
            if (string.IsNullOrWhiteSpace(operation) || string.IsNullOrWhiteSpace(entityName))
                return string.Empty;

            return operation switch
            {
                "CreateEntity" => $"DROP TABLE {entityName};",
                _ => $"-- No automatic undo for operation '{operation}' on '{entityName}'. Manual review required."
            };
        }

        private static MigrationPlanOperationKind ParseOperationKind(string operation)
        {
            if (string.IsNullOrWhiteSpace(operation))
                return MigrationPlanOperationKind.None;

            return Enum.TryParse<MigrationPlanOperationKind>(operation, ignoreCase: true, out var kind)
                ? kind
                : MigrationPlanOperationKind.None;
        }

        private static bool LastMigrationIsUndone(MigrationHistory history)
        {
            if (history.Migrations == null || history.Migrations.Count == 0) return false;
            var last = history.Migrations.Last();
            return last.Notes != null && last.Notes.Contains("Undone at");
        }

        private static string GenerateMigrationId()
        {
            return Guid.NewGuid().ToString("N")[..12];
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

    public class UndoPreview
    {
        public string DatasourceName { get; set; } = string.Empty;
        public bool CanUndo { get; set; }
        public string LastMigrationId { get; set; } = string.Empty;
        public string LastMigrationName { get; set; } = string.Empty;
        public DateTime LastMigrationDate { get; set; }
        public List<UndoStepInfo> PendingSteps { get; set; } = new();
        public string Message { get; set; } = string.Empty;
    }

    public class UndoStepInfo
    {
        public string Operation { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string CompensationSql { get; set; } = string.Empty;
    }
}
