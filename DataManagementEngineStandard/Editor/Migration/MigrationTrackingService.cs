using System;
using System.Collections.Generic;
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

            var configEditor = _editor.ConfigEditor;
            return configEditor?.LoadMigrationHistory(datasourceName) ?? new MigrationHistory();
        }

        public IErrorsInfo ExecuteTrackedMigration(
            string datasourceName,
            IEnumerable<Type> entityTypes,
            bool detectRelationships = true,
            IProgress<PassedArgs> progress = null)
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
                    var state = ds.OpenConnection();
                    if (state != ConnectionState.Open)
                        return CreateError($"Could not open connection to '{datasourceName}'.");
                }

                var migration = new MigrationManager(_editor, ds)
                {
                    MigrateDataSource = ds
                };

                var plan = migration.BuildMigrationPlanForTypes(typesList, detectRelationships);
                if (plan == null || plan.Operations == null || plan.Operations.Count == 0)
                {
                    return new ErrorsInfo
                    {
                        Flag = Errors.Ok,
                        Message = "Schema is already up to date. No migration needed."
                    };
                }

                var result = migration.ExecuteMigrationPlan(plan, null, null, progress);

                var record = new MigrationRecord
                {
                    MigrationId = GenerateMigrationId(),
                    Name = $"Migration_{datasourceName}_{DateTime.UtcNow:yyyyMMddHHmmss}",
                    AppliedOnUtc = DateTime.UtcNow,
                    Success = result?.Success ?? false,
                    Notes = result?.Message ?? string.Empty,
                    Steps = new List<MigrationStep>()
                };

                if (plan.Operations != null)
                {
                    foreach (var op in plan.Operations)
                    {
                        record.Steps.Add(new MigrationStep
                        {
                            Operation = op.Kind?.ToString() ?? op.OperationType?.ToString() ?? "Unknown",
                            EntityName = op.EntityName ?? op.TargetName ?? string.Empty,
                            Success = result?.Success ?? false
                        });
                    }
                }

                var dataSourceType = DataSourceType.Unknown;
                var conn = _editor.ConfigEditor?.DataConnections
                    ?.FirstOrDefault(c => string.Equals(c.ConnectionName, datasourceName, StringComparison.OrdinalIgnoreCase));
                if (conn != null)
                    dataSourceType = conn.DatabaseType;

                _editor.ConfigEditor?.AppendMigrationRecord(datasourceName, dataSourceType, record);

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
            preview.PendingSteps = lastMigration.Steps?.Select(s =>
                new UndoStepInfo
                {
                    Operation = s.Operation,
                    EntityName = s.EntityName,
                    CompensationSql = GenerateCompensationSql(s.Operation, s.EntityName)
                }).ToList() ?? new List<UndoStepInfo>();
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
                        if (ds.ConnectionStatus != ConnectionState.Open)
                            ds.OpenConnection();

                        foreach (var step in preview.PendingSteps)
                        {
                            if (!string.IsNullOrWhiteSpace(step.CompensationSql))
                            {
                                ds.ExecuteSql(step.CompensationSql);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _editor.AddLogMessage("MigrationTracker",
                            $"Undo partial failure: {ex.Message}",
                            DateTime.Now, 0, null, Errors.Failed);
                    }
                }

                _editor.ConfigEditor?.SaveMigrationHistory(history);
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

        public bool CanUndoLastMigration(string datasourceName)
        {
            return CanUndo(datasourceName);
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

        private static string GenerateCompensationSql(string operation, string entityName)
        {
            if (string.IsNullOrWhiteSpace(operation) || string.IsNullOrWhiteSpace(entityName))
                return string.Empty;

            return operation switch
            {
                "CreateEntity" => $"DROP TABLE IF EXISTS \"{entityName}\"",
                "AddMissingColumns" => $"-- Cannot undo column additions for '{entityName}'. Manual review required.",
                "AddForeignKey" => $"-- Cannot undo foreign key additions for '{entityName}'. Manual review required.",
                "CreateIndex" => $"-- Cannot undo index creation for '{entityName}'. Manual review required.",
                _ => $"-- No automatic undo for operation '{operation}' on '{entityName}'."
            };
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
