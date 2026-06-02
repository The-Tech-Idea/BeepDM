using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Helpers.FileandFolderHelpers;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.Migration
{
    public partial class MigrationManager
    {
        public IErrorsInfo EnsureEntity(EntityStructure entity, bool createIfMissing = true, bool addMissingColumns = true, bool applyForeignKeys = false, bool applyIndexes = false)
        {
            if (entity == null)
                return CreateErrorsInfo(Errors.Failed, "Entity structure cannot be null");

            if (string.IsNullOrWhiteSpace(entity.EntityName))
                return CreateErrorsInfo(Errors.Failed, "Entity name cannot be empty");

            if (MigrateDataSource == null)
                return CreateErrorsInfo(Errors.Failed, "Migration data source is not set");

            // NOTE: Do NOT pre-map types here. The datasource's CreateEntityAs handles all
            // type conversion from .NET types (Fieldtype like "System.String") to its own
            // native type system internally. Pre-mapping corrupts the EntityStructure.

            bool entityExists;
            try
            {
                entityExists = MigrateDataSource.CheckEntityExist(entity.EntityName);

                // Check if there was an error during the existence check itself
                // (as opposed to just "entity doesn't exist")
                if (MigrateDataSource.ErrorObject != null && MigrateDataSource.ErrorObject.Flag == Errors.Failed)
                {
                    var checkError = CreateErrorsInfo(
                        Errors.Failed,
                        $"Error checking if entity '{entity.EntityName}' exists: {MigrateDataSource.ErrorObject.Message}",
                        MigrateDataSource.ErrorObject.Ex);
                    return checkError;
                }
            }
            catch (Exception checkEx)
            {
                return CreateErrorsInfo(Errors.Failed, $"Exception checking entity existence '{entity.EntityName}': {checkEx.Message}", checkEx);
            }

            if (!entityExists)
            {
                if (!createIfMissing)
                    return CreateErrorsInfo(Errors.Failed, $"Entity '{entity.EntityName}' does not exist");

                // Use IDataSource.CreateEntityAs for datasource-agnostic entity creation
                try
                {
                    var created = MigrateDataSource.CreateEntityAs(entity);
                    IErrorsInfo result;
                    if (created)
                    {
                        result = CreateErrorsInfo(Errors.Ok, $"Entity '{entity.EntityName}' created successfully");
                    }
                    else
                    {
                        result = CreateErrorsInfo(Errors.Failed, $"Failed to create entity '{entity.EntityName}'. Check ErrorObject for details.");
                    }

                    // Check ErrorObject for additional details
                    if (!created && MigrateDataSource.ErrorObject != null)
                    {
                        result.Message = MigrateDataSource.ErrorObject.Message ?? result.Message;
                        result.Ex = MigrateDataSource.ErrorObject.Ex;
                    }

                    TrackMigration("CreateEntityAs", entity.EntityName, null, string.Empty, result);
                    if (!created)
                        return result;

                    // Entity was just created. Apply indexes and FKs if requested.
                    // The create path bypasses the addMissingColumns branch, so
                    // we must apply relational artifacts here to honor the
                    // applyForeignKeys / applyIndexes opt-in contract.
                    if (applyIndexes || applyForeignKeys)
                    {
                        var postCreateFailures = new List<string>();
                        if (applyIndexes)
                        {
                            var indexFailures = ApplyIndexesForEntity(entity);
                            if (indexFailures != null) postCreateFailures.AddRange(indexFailures);
                        }
                        if (applyForeignKeys)
                        {
                            var fkFailures = ApplyForeignKeysForEntity(entity);
                            if (fkFailures != null) postCreateFailures.AddRange(fkFailures);
                        }

                        if (postCreateFailures.Count > 0)
                        {
                            var msg = $"Entity '{entity.EntityName}' was created, but relational artifacts failed: {string.Join("; ", postCreateFailures)}";
                            var partial = CreateErrorsInfo(Errors.Failed, msg);
                            TrackMigration("EnsureEntity.PostCreate", entity.EntityName, null, string.Empty, partial);
                            return partial;
                        }
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    var errorResult = CreateErrorsInfo(Errors.Failed, $"Exception creating entity '{entity.EntityName}': {ex.Message}", ex);
                    TrackMigration("CreateEntityAs", entity.EntityName, null, string.Empty, errorResult);
                    return errorResult;
                }
            }

            if (!addMissingColumns)
            {
                // Even when not adding missing columns, the caller may still
                // want the relational artifacts applied. Honor that.
                if (applyForeignKeys || applyIndexes)
                {
                    var postExistFailures = new List<string>();
                    if (applyIndexes)
                    {
                        var indexFailures = ApplyIndexesForEntity(entity);
                        if (indexFailures != null) postExistFailures.AddRange(indexFailures);
                    }
                    if (applyForeignKeys)
                    {
                        var fkFailures = ApplyForeignKeysForEntity(entity);
                        if (fkFailures != null) postExistFailures.AddRange(fkFailures);
                    }
                    if (postExistFailures.Count > 0)
                    {
                        var msg = $"Entity '{entity.EntityName}' exists, but relational artifacts failed: {string.Join("; ", postExistFailures)}";
                        var partial = CreateErrorsInfo(Errors.Failed, msg);
                        TrackMigration("EnsureEntity.PostExist", entity.EntityName, null, string.Empty, partial);
                        return partial;
                    }
                }
                return CreateErrorsInfo(Errors.Ok, $"Entity '{entity.EntityName}' already exists");
            }

            var current = MigrateDataSource.GetEntityStructure(entity.EntityName, true);
            if (current == null)
                return CreateErrorsInfo(Errors.Failed, $"Failed to load structure for '{entity.EntityName}'");

            var missingColumns = GetMissingColumns(current, entity);
            var failures = new List<string>();
            if (missingColumns.Count > 0)
            {
                foreach (var column in missingColumns)
                {
                    var addResult = AddColumn(entity, column);
                    if (addResult.Flag != Errors.Ok)
                        failures.Add($"{column.FieldName}: {addResult.Message}");
                }
            }

            // After schema is in sync, optionally apply relational artifacts.
            // These are opt-in because the caller (especially EF Core adapters)
            // may have already created them via the datasource's own provisioning.
            if (applyIndexes)
            {
                var indexFailures = ApplyIndexesForEntity(entity);
                if (indexFailures != null) failures.AddRange(indexFailures);
            }

            if (applyForeignKeys)
            {
                var fkFailures = ApplyForeignKeysForEntity(entity);
                if (fkFailures != null) failures.AddRange(fkFailures);
            }

            if (failures.Count > 0)
            {
                return CreateErrorsInfo(
                    Errors.Failed,
                    $"Failed schema sync for '{entity.EntityName}': {string.Join("; ", failures)}");
            }

            if (missingColumns.Count == 0 && !applyForeignKeys && !applyIndexes)
                return CreateErrorsInfo(Errors.Ok, $"Entity '{entity.EntityName}' is up to date");

            return CreateErrorsInfo(
                Errors.Ok,
                $"Entity '{entity.EntityName}' synchronized ({missingColumns.Count} new column(s)" +
                (applyIndexes ? ", indexes applied" : string.Empty) +
                (applyForeignKeys ? ", foreign keys applied" : string.Empty) + ")");
        }

        public IErrorsInfo EnsureEntity(Type pocoType, bool createIfMissing = true, bool addMissingColumns = true, bool detectRelationships = true, bool applyForeignKeys = false, bool applyIndexes = false)
        {
            if (pocoType == null)
                return CreateErrorsInfo(Errors.Failed, "POCO type cannot be null");

            // Use TryGetEntityStructure (not classCreator directly) so the
            // model-interop cache populated by BuildMigrationPlanForModel is
            // honored. EF Core callers that populated MigrationModel then
            // BuildMigrationPlanForModel have already primed the cache with
            // ORM-shaped structures (with table name from GetTableName(), etc.).
            var entity = TryGetEntityStructure(pocoType);
            if (entity == null)
            {
                if (_editor?.classCreator == null)
                    return CreateErrorsInfo(Errors.Failed, "Class creator is not available");
                return CreateErrorsInfo(Errors.Failed, $"Failed to convert POCO '{pocoType.Name}' to EntityStructure");
            }

            return EnsureEntity(entity, createIfMissing, addMissingColumns, applyForeignKeys, applyIndexes);
        }

        public IReadOnlyList<EntityField> GetMissingColumns(EntityStructure current, EntityStructure desired)
        {
            if (current?.Fields == null || desired?.Fields == null)
                return new List<EntityField>();

            var existing = new HashSet<string>(
                current.Fields.Select(f => f.FieldName),
                StringComparer.OrdinalIgnoreCase);

            return desired.Fields.Where(f => !existing.Contains(f.FieldName)).ToList();
        }

        public IErrorsInfo CreateEntity(EntityStructure entity)
        {
            if (entity == null || string.IsNullOrWhiteSpace(entity.EntityName))
                return CreateErrorsInfo(Errors.Failed, "Entity name is missing");

            if (MigrateDataSource == null)
                return CreateErrorsInfo(Errors.Failed, "Migration data source is not set");

            // NOTE: Do NOT pre-map types here. The datasource's CreateEntityAs handles all
            // type conversion internally. Passing .NET types (Fieldtype) is correct.

            // Validate entity structure before creation (if helper supports validation)
            var helper = _editor?.GetDataSourceHelper(MigrateDataSource.DatasourceType);
            if (helper != null)
            {
                try
                {
                    var (isValid, errors) = helper.ValidateEntity(entity);
                    if (!isValid && errors != null && errors.Count > 0)
                    {
                        var errorMsg = $"Entity validation failed: {string.Join("; ", errors)}";
                        var validationResult = CreateErrorsInfo(Errors.Failed, errorMsg);
                        TrackMigration("CreateEntity", entity.EntityName, null, string.Empty, validationResult);
                        return validationResult;
                    }
                }
                catch
                {
                    // Validation not supported or failed, continue with creation
                }
            }

            // Use IDataSource.CreateEntityAs for datasource-agnostic entity creation
            // This allows each datasource to handle creation according to its own capabilities
            // Each datasource implementation maps .NET types to its own type system internally
            try
            {
                var created = MigrateDataSource.CreateEntityAs(entity);
                var result = created
                    ? CreateErrorsInfo(Errors.Ok, $"Entity '{entity.EntityName}' created successfully")
                    : CreateErrorsInfo(Errors.Failed, $"Failed to create entity '{entity.EntityName}'. Check ErrorObject for details.");

                // Check ErrorObject for additional details
                if (!created && MigrateDataSource.ErrorObject != null)
                {
                    result.Message = MigrateDataSource.ErrorObject.Message ?? result.Message;
                    result.Ex = MigrateDataSource.ErrorObject.Ex;
                }

                TrackMigration("CreateEntity", entity.EntityName, null, string.Empty, result);
                return result;
            }
            catch (Exception ex)
            {
                var errorResult = CreateErrorsInfo(Errors.Failed, $"Exception creating entity '{entity.EntityName}': {ex.Message}", ex);
                TrackMigration("CreateEntity", entity.EntityName, null, string.Empty, errorResult);
                return errorResult;
            }
        }

        public IErrorsInfo DropEntity(string entityName)
        {
            if (string.IsNullOrWhiteSpace(entityName))
                return CreateErrorsInfo(Errors.Failed, "Entity name is missing");

            if (MigrateDataSource == null)
                return CreateErrorsInfo(Errors.Failed, "Migration data source is not set");

            if (IsFileDataSource(MigrateDataSource))
            {
                var fileResult = DeleteFile(entityName);
                TrackMigration("DropEntity", entityName, null, string.Empty, fileResult);
                EmitDdlEvidence("DropEntity", entityName, null, null, null,
                    fileResult.Flag == Errors.Ok ? DdlOperationOutcome.Emulated : DdlOperationOutcome.Failed,
                    DdlHelperSource.FileMutation, fileResult.Flag == Errors.Ok ? "DDL-FILE-DROP" : "DDL-FILE-DROP-FAILED");
                return fileResult;
            }

            var helper = _editor.GetDataSourceHelper(MigrateDataSource.DatasourceType);
            if (helper == null)
            {
                EmitDdlEvidence("DropEntity", entityName, null, null, null,
                    DdlOperationOutcome.Unsupported, DdlHelperSource.Direct, "DDL-UNSUPPORTED-NO-HELPER");
                return CreateErrorsInfo(Errors.Failed, $"No helper registered for '{MigrateDataSource.DatasourceType}'");
            }

            var (sql, success, errorMessage) = helper.GenerateDropTableSql(entityName);
            if (!success)
                return CreateErrorsInfo(Errors.Failed, $"Failed to generate drop SQL: {errorMessage}");

            if (string.IsNullOrWhiteSpace(sql))
            {
                var noDdlResult = CreateErrorsInfo(Errors.Ok, $"No DDL required for '{MigrateDataSource.DatasourceType}'");
                TrackMigration("DropEntity", entityName, null, string.Empty, noDdlResult);
                EmitDdlEvidence("DropEntity", entityName, null, null, null, DdlOperationOutcome.NoOp, DdlHelperSource.UniversalRdbmsHelper, "DDL-NOOP-EMPTY-SQL");
                return noDdlResult;
            }

            var dropResult = ExecuteSql(sql);
            TrackMigration("DropEntity", entityName, null, sql, dropResult);
            EmitDdlEvidence("DropEntity", entityName, null, null, sql,
                dropResult.Flag == Errors.Ok ? DdlOperationOutcome.Executed : DdlOperationOutcome.Failed,
                DdlHelperSource.UniversalRdbmsHelper, dropResult.Flag == Errors.Ok ? "DDL-EXECUTED" : "DDL-EXEC-FAILED");
            return dropResult;
        }

        public IErrorsInfo TruncateEntity(string entityName)
        {
            if (string.IsNullOrWhiteSpace(entityName))
                return CreateErrorsInfo(Errors.Failed, "Entity name is missing");

            if (MigrateDataSource == null)
                return CreateErrorsInfo(Errors.Failed, "Migration data source is not set");

            if (IsFileDataSource(MigrateDataSource))
            {
                var fileResult = TruncateFile(entityName);
                TrackMigration("TruncateEntity", entityName, null, string.Empty, fileResult);
                EmitDdlEvidence("TruncateEntity", entityName, null, null, null,
                    fileResult.Flag == Errors.Ok ? DdlOperationOutcome.Emulated : DdlOperationOutcome.Failed,
                    DdlHelperSource.FileMutation, fileResult.Flag == Errors.Ok ? "DDL-FILE-TRUNCATE" : "DDL-FILE-TRUNCATE-FAILED");
                return fileResult;
            }

            var helper = _editor.GetDataSourceHelper(MigrateDataSource.DatasourceType);
            if (helper == null)
            {
                EmitDdlEvidence("TruncateEntity", entityName, null, null, null,
                    DdlOperationOutcome.Unsupported, DdlHelperSource.Direct, "DDL-UNSUPPORTED-NO-HELPER");
                return CreateErrorsInfo(Errors.Failed, $"No helper registered for '{MigrateDataSource.DatasourceType}'");
            }

            var (sql, success, errorMessage) = helper.GenerateTruncateTableSql(entityName);
            if (!success)
                return CreateErrorsInfo(Errors.Failed, $"Failed to generate truncate SQL: {errorMessage}");

            if (string.IsNullOrWhiteSpace(sql))
            {
                var noDdlResult = CreateErrorsInfo(Errors.Ok, $"No DDL required for '{MigrateDataSource.DatasourceType}'");
                TrackMigration("TruncateEntity", entityName, null, string.Empty, noDdlResult);
                EmitDdlEvidence("TruncateEntity", entityName, null, null, null, DdlOperationOutcome.NoOp, DdlHelperSource.UniversalRdbmsHelper, "DDL-NOOP-EMPTY-SQL");
                return noDdlResult;
            }

            var truncateResult = ExecuteSql(sql);
            TrackMigration("TruncateEntity", entityName, null, sql, truncateResult);
            EmitDdlEvidence("TruncateEntity", entityName, null, null, sql,
                truncateResult.Flag == Errors.Ok ? DdlOperationOutcome.Executed : DdlOperationOutcome.Failed,
                DdlHelperSource.UniversalRdbmsHelper, truncateResult.Flag == Errors.Ok ? "DDL-EXECUTED" : "DDL-EXEC-FAILED");
            return truncateResult;
        }

        public IErrorsInfo RenameEntity(string oldName, string newName)
        {
            if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName))
                return CreateErrorsInfo(Errors.Failed, "Entity names are missing");

            if (MigrateDataSource == null)
                return CreateErrorsInfo(Errors.Failed, "Migration data source is not set");

            if (IsFileDataSource(MigrateDataSource))
            {
                var fileResult = RenameFile(oldName, newName);
                TrackMigration("RenameEntity", $"{oldName}->{newName}", null, string.Empty, fileResult);
                EmitDdlEvidence("RenameEntity", oldName, null, null, null,
                    fileResult.Flag == Errors.Ok ? DdlOperationOutcome.Emulated : DdlOperationOutcome.Failed,
                    DdlHelperSource.FileMutation, fileResult.Flag == Errors.Ok ? "DDL-FILE-RENAME" : "DDL-FILE-RENAME-FAILED");
                return fileResult;
            }

            var helper = _editor.GetDataSourceHelper(MigrateDataSource.DatasourceType);
            if (helper == null)
            {
                EmitDdlEvidence("RenameEntity", oldName, null, null, null,
                    DdlOperationOutcome.Unsupported, DdlHelperSource.Direct, "DDL-UNSUPPORTED-NO-HELPER");
                return CreateErrorsInfo(Errors.Failed, $"No helper registered for '{MigrateDataSource.DatasourceType}'");
            }

            var (sql, success, errorMessage) = helper.GenerateRenameTableSql(oldName, newName);
            if (!success)
                return CreateErrorsInfo(Errors.Failed, $"Failed to generate rename SQL: {errorMessage}");

            if (string.IsNullOrWhiteSpace(sql))
            {
                var noDdlResult = CreateErrorsInfo(Errors.Ok, $"No DDL required for '{MigrateDataSource.DatasourceType}'");
                TrackMigration("RenameEntity", $"{oldName}->{newName}", null, string.Empty, noDdlResult);
                EmitDdlEvidence("RenameEntity", oldName, null, null, null, DdlOperationOutcome.NoOp, DdlHelperSource.UniversalRdbmsHelper, "DDL-NOOP-EMPTY-SQL");
                return noDdlResult;
            }

            var renameResult = ExecuteSql(sql);
            TrackMigration("RenameEntity", $"{oldName}->{newName}", null, sql, renameResult);
            EmitDdlEvidence("RenameEntity", oldName, null, null, sql,
                renameResult.Flag == Errors.Ok ? DdlOperationOutcome.Executed : DdlOperationOutcome.Failed,
                DdlHelperSource.UniversalRdbmsHelper, renameResult.Flag == Errors.Ok ? "DDL-EXECUTED" : "DDL-EXEC-FAILED");
            return renameResult;
        }

        public IErrorsInfo AlterColumn(string entityName, string columnName, EntityField newColumn)
        {
            if (string.IsNullOrWhiteSpace(entityName) || string.IsNullOrWhiteSpace(columnName) || newColumn == null)
                return CreateErrorsInfo(Errors.Failed, "Entity or column information is missing");

            if (MigrateDataSource == null)
                return CreateErrorsInfo(Errors.Failed, "Migration data source is not set");

            if (IsFileDataSource(MigrateDataSource))
                return CreateErrorsInfo(Errors.Failed, "Alter column is not supported for file datasources");

            var helper = _editor.GetDataSourceHelper(MigrateDataSource.DatasourceType);
            if (helper == null)
            {
                EmitDdlEvidence("AlterColumn", entityName, columnName, null, null,
                    DdlOperationOutcome.Unsupported, DdlHelperSource.Direct, "DDL-UNSUPPORTED-NO-HELPER");
                return CreateErrorsInfo(Errors.Failed, $"No helper registered for '{MigrateDataSource.DatasourceType}'");
            }

            var (sql, success, errorMessage) = helper.GenerateAlterColumnSql(entityName, columnName, newColumn);
            if (!success)
                return CreateErrorsInfo(Errors.Failed, $"Failed to generate alter SQL: {errorMessage}");

            if (string.IsNullOrWhiteSpace(sql))
            {
                var noDdlResult = CreateErrorsInfo(Errors.Ok, $"No DDL required for '{MigrateDataSource.DatasourceType}'");
                TrackMigration("AlterColumn", entityName, columnName, string.Empty, noDdlResult);
                EmitDdlEvidence("AlterColumn", entityName, columnName, null, null, DdlOperationOutcome.NoOp, DdlHelperSource.UniversalRdbmsHelper, "DDL-NOOP-EMPTY-SQL");
                return noDdlResult;
            }

            var alterResult = ExecuteSql(sql);
            TrackMigration("AlterColumn", entityName, columnName, sql, alterResult);
            EmitDdlEvidence("AlterColumn", entityName, columnName, null, sql,
                alterResult.Flag == Errors.Ok ? DdlOperationOutcome.Executed : DdlOperationOutcome.Failed,
                DdlHelperSource.UniversalRdbmsHelper, alterResult.Flag == Errors.Ok ? "DDL-EXECUTED" : "DDL-EXEC-FAILED");
            return alterResult;
        }

        public IErrorsInfo DropColumn(string entityName, string columnName)
        {
            if (string.IsNullOrWhiteSpace(entityName) || string.IsNullOrWhiteSpace(columnName))
                return CreateErrorsInfo(Errors.Failed, "Entity or column name is missing");

            if (MigrateDataSource == null)
                return CreateErrorsInfo(Errors.Failed, "Migration data source is not set");

            if (IsFileDataSource(MigrateDataSource))
            {
                var fileResult = RemoveColumnFromFile(entityName, columnName);
                TrackMigration("DropColumn", entityName, columnName, string.Empty, fileResult);
                EmitDdlEvidence("DropColumn", entityName, columnName, null, null,
                    fileResult.Flag == Errors.Ok ? DdlOperationOutcome.Emulated : DdlOperationOutcome.Failed,
                    DdlHelperSource.FileMutation, fileResult.Flag == Errors.Ok ? "DDL-FILE-DROP-COL" : "DDL-FILE-DROP-COL-FAILED");
                return fileResult;
            }

            var helper = _editor.GetDataSourceHelper(MigrateDataSource.DatasourceType);
            if (helper == null)
            {
                EmitDdlEvidence("DropColumn", entityName, columnName, null, null,
                    DdlOperationOutcome.Unsupported, DdlHelperSource.Direct, "DDL-UNSUPPORTED-NO-HELPER");
                return CreateErrorsInfo(Errors.Failed, $"No helper registered for '{MigrateDataSource.DatasourceType}'");
            }

            var (sql, success, errorMessage) = helper.GenerateDropColumnSql(entityName, columnName);
            if (!success)
                return CreateErrorsInfo(Errors.Failed, $"Failed to generate drop-column SQL: {errorMessage}");

            if (string.IsNullOrWhiteSpace(sql))
            {
                var noDdlResult = CreateErrorsInfo(Errors.Ok, $"No DDL required for '{MigrateDataSource.DatasourceType}'");
                TrackMigration("DropColumn", entityName, columnName, string.Empty, noDdlResult);
                EmitDdlEvidence("DropColumn", entityName, columnName, null, null, DdlOperationOutcome.NoOp, DdlHelperSource.UniversalRdbmsHelper, "DDL-NOOP-EMPTY-SQL");
                return noDdlResult;
            }

            var dropResult = ExecuteSql(sql);
            TrackMigration("DropColumn", entityName, columnName, sql, dropResult);
            EmitDdlEvidence("DropColumn", entityName, columnName, null, sql,
                dropResult.Flag == Errors.Ok ? DdlOperationOutcome.Executed : DdlOperationOutcome.Failed,
                DdlHelperSource.UniversalRdbmsHelper, dropResult.Flag == Errors.Ok ? "DDL-EXECUTED" : "DDL-EXEC-FAILED");
            return dropResult;
        }

        public IErrorsInfo RenameColumn(string entityName, string oldColumnName, string newColumnName)
        {
            if (string.IsNullOrWhiteSpace(entityName) || string.IsNullOrWhiteSpace(oldColumnName) || string.IsNullOrWhiteSpace(newColumnName))
                return CreateErrorsInfo(Errors.Failed, "Entity or column name is missing");

            if (MigrateDataSource == null)
                return CreateErrorsInfo(Errors.Failed, "Migration data source is not set");

            if (IsFileDataSource(MigrateDataSource))
            {
                var fileResult = RenameColumnInFile(entityName, oldColumnName, newColumnName);
                TrackMigration("RenameColumn", entityName, $"{oldColumnName}->{newColumnName}", string.Empty, fileResult);
                EmitDdlEvidence("RenameColumn", entityName, $"{oldColumnName}->{newColumnName}", null, null,
                    fileResult.Flag == Errors.Ok ? DdlOperationOutcome.Emulated : DdlOperationOutcome.Failed,
                    DdlHelperSource.FileMutation, fileResult.Flag == Errors.Ok ? "DDL-FILE-RENAME-COL" : "DDL-FILE-RENAME-COL-FAILED");
                return fileResult;
            }

            var helper = _editor.GetDataSourceHelper(MigrateDataSource.DatasourceType);
            if (helper == null)
            {
                EmitDdlEvidence("RenameColumn", entityName, $"{oldColumnName}->{newColumnName}", null, null,
                    DdlOperationOutcome.Unsupported, DdlHelperSource.Direct, "DDL-UNSUPPORTED-NO-HELPER");
                return CreateErrorsInfo(Errors.Failed, $"No helper registered for '{MigrateDataSource.DatasourceType}'");
            }

            var (sql, success, errorMessage) = helper.GenerateRenameColumnSql(entityName, oldColumnName, newColumnName);
            if (!success)
                return CreateErrorsInfo(Errors.Failed, $"Failed to generate rename-column SQL: {errorMessage}");

            if (string.IsNullOrWhiteSpace(sql))
            {
                var noDdlResult = CreateErrorsInfo(Errors.Ok, $"No DDL required for '{MigrateDataSource.DatasourceType}'");
                TrackMigration("RenameColumn", entityName, $"{oldColumnName}->{newColumnName}", string.Empty, noDdlResult);
                EmitDdlEvidence("RenameColumn", entityName, $"{oldColumnName}->{newColumnName}", null, null, DdlOperationOutcome.NoOp, DdlHelperSource.UniversalRdbmsHelper, "DDL-NOOP-EMPTY-SQL");
                return noDdlResult;
            }

            var renameResult = ExecuteSql(sql);
            TrackMigration("RenameColumn", entityName, $"{oldColumnName}->{newColumnName}", sql, renameResult);
            EmitDdlEvidence("RenameColumn", entityName, $"{oldColumnName}->{newColumnName}", null, sql,
                renameResult.Flag == Errors.Ok ? DdlOperationOutcome.Executed : DdlOperationOutcome.Failed,
                DdlHelperSource.UniversalRdbmsHelper, renameResult.Flag == Errors.Ok ? "DDL-EXECUTED" : "DDL-EXEC-FAILED");
            return renameResult;
        }

        public IErrorsInfo CreateIndex(string entityName, string indexName, string[] columns, Dictionary<string, object> options = null)
        {
            if (string.IsNullOrWhiteSpace(entityName) || columns == null || columns.Length == 0)
                return CreateErrorsInfo(Errors.Failed, "Entity or columns are missing");

            if (MigrateDataSource == null)
                return CreateErrorsInfo(Errors.Failed, "Migration data source is not set");

            if (IsFileDataSource(MigrateDataSource))
                return CreateErrorsInfo(Errors.Failed, "Indexes are not supported for file datasources");

            var helper = _editor.GetDataSourceHelper(MigrateDataSource.DatasourceType);
            if (helper == null)
                return CreateErrorsInfo(Errors.Failed, $"No helper registered for '{MigrateDataSource.DatasourceType}'");

            var (sql, success, errorMessage) = helper.GenerateCreateIndexSql(entityName, indexName, columns, options);
            if (!success)
                return CreateErrorsInfo(Errors.Failed, $"Failed to generate create-index SQL: {errorMessage}");

            if (string.IsNullOrWhiteSpace(sql))
            {
                var noDdlResult = CreateErrorsInfo(Errors.Ok, $"No DDL required for '{MigrateDataSource.DatasourceType}'");
                TrackMigration("CreateIndex", entityName, indexName, string.Empty, noDdlResult);
                EmitDdlEvidence("CreateIndex", entityName, null, indexName, null, DdlOperationOutcome.NoOp, DdlHelperSource.UniversalRdbmsHelper, "DDL-NOOP-EMPTY-SQL");
                return noDdlResult;
            }

            var indexResult = ExecuteSql(sql);
            TrackMigration("CreateIndex", entityName, indexName, sql, indexResult);
            EmitDdlEvidence("CreateIndex", entityName, null, indexName, sql,
                indexResult.Flag == Errors.Ok ? DdlOperationOutcome.Executed : DdlOperationOutcome.Failed,
                DdlHelperSource.UniversalRdbmsHelper, indexResult.Flag == Errors.Ok ? "DDL-EXECUTED" : "DDL-EXEC-FAILED");
            return indexResult;
        }

        public IErrorsInfo DropIndex(string entityName, string indexName)
        {
            if (string.IsNullOrWhiteSpace(entityName))
                return CreateErrorsInfo(Errors.Failed, "Entity name is missing");

            if (string.IsNullOrWhiteSpace(indexName))
                return CreateErrorsInfo(Errors.Failed, "Index name is required");

            if (MigrateDataSource == null)
                return CreateErrorsInfo(Errors.Failed, "Migration data source is not set");

            if (IsFileDataSource(MigrateDataSource))
                return CreateErrorsInfo(Errors.Failed, "Indexes are not supported for file datasources");

            var helper = _editor.GetDataSourceHelper(MigrateDataSource.DatasourceType);
            if (helper == null)
            {
                EmitDdlEvidence("DropIndex", entityName, null, indexName, null,
                    DdlOperationOutcome.Unsupported, DdlHelperSource.Direct, "DDL-UNSUPPORTED-NO-HELPER");
                return CreateErrorsInfo(Errors.Failed, $"No helper registered for '{MigrateDataSource.DatasourceType}'");
            }

            // The universal RDBMS helper exposes GenerateDropIndexSql; check there first
            // and fall back to IDataSourceHelper via reflection for custom helpers.
            (string Sql, bool Success, string ErrorMessage) genResult = helper switch
            {
                Helpers.UniversalDataSourceHelpers.RdbmsHelpers.RdbmsHelper rh
                    => rh.GenerateDropIndexSql(entityName, indexName),
                _ => TryGenerateDropIndexViaInterface(helper, entityName, indexName)
            };

            if (!genResult.Success)
            {
                EmitDdlEvidence("DropIndex", entityName, null, indexName, null,
                    DdlOperationOutcome.Failed, DdlHelperSource.UniversalRdbmsHelper, "DDL-GEN-FAILED");
                return CreateErrorsInfo(Errors.Failed, $"Failed to generate drop-index SQL: {genResult.ErrorMessage}");
            }

            if (string.IsNullOrWhiteSpace(genResult.Sql))
            {
                var noDdlResult = CreateErrorsInfo(Errors.Ok, $"No DDL required for '{MigrateDataSource.DatasourceType}'");
                TrackMigration("DropIndex", entityName, indexName, string.Empty, noDdlResult);
                EmitDdlEvidence("DropIndex", entityName, null, indexName, null,
                    DdlOperationOutcome.NoOp, DdlHelperSource.UniversalRdbmsHelper, "DDL-NOOP-EMPTY-SQL");
                return noDdlResult;
            }

            var dropResult = ExecuteSql(genResult.Sql);
            TrackMigration("DropIndex", entityName, indexName, genResult.Sql, dropResult);
            EmitDdlEvidence("DropIndex", entityName, null, indexName, genResult.Sql,
                dropResult.Flag == Errors.Ok ? DdlOperationOutcome.Executed : DdlOperationOutcome.Failed,
                DdlHelperSource.UniversalRdbmsHelper, dropResult.Flag == Errors.Ok ? "DDL-EXECUTED" : "DDL-EXEC-FAILED");
            return dropResult;
        }

        // Reflection-based fallback for custom IDataSourceHelper implementations that
        // expose their own GenerateDropIndexSql method.
        private static (string Sql, bool Success, string ErrorMessage) TryGenerateDropIndexViaInterface(
            IDataSourceHelper helper,
            string tableName, string indexName)
        {
            try
            {
                var mi = helper.GetType().GetMethod("GenerateDropIndexSql",
                    new[] { typeof(string), typeof(string) });
                if (mi != null)
                {
                    var result = mi.Invoke(helper, new object[] { tableName, indexName });
                    if (result is ValueTuple<string, bool, string> tuple)
                        return tuple;
                }
                return ("", false, "Helper does not support index drop");
            }
            catch (Exception ex)
            {
                return ("", false, ex.Message);
            }
        }

        public IErrorsInfo AddForeignKey(
            string entityName,
            string[] columnNames,
            string referencedEntityName,
            string[] referencedColumnNames,
            string onDeleteBehavior = "Cascade",
            string onUpdateBehavior = "Cascade",
            string constraintName = null)
        {
            if (string.IsNullOrWhiteSpace(entityName))
                return CreateErrorsInfo(Errors.Failed, "Dependent entity name is missing");

            if (columnNames == null || columnNames.Length == 0)
                return CreateErrorsInfo(Errors.Failed, "At least one foreign-key column must be specified");

            if (string.IsNullOrWhiteSpace(referencedEntityName))
                return CreateErrorsInfo(Errors.Failed, "Referenced entity name is missing");

            if (referencedColumnNames == null || referencedColumnNames.Length == 0)
                return CreateErrorsInfo(Errors.Failed, "At least one referenced column must be specified");

            if (columnNames.Length != referencedColumnNames.Length)
                return CreateErrorsInfo(Errors.Failed, "Number of foreign-key columns must match referenced columns");

            if (MigrateDataSource == null)
                return CreateErrorsInfo(Errors.Failed, "Migration data source is not set");

            if (IsFileDataSource(MigrateDataSource))
                return CreateErrorsInfo(Errors.Failed, "Foreign keys are not supported for file datasources");

            var helper = _editor.GetDataSourceHelper(MigrateDataSource.DatasourceType);
            if (helper == null)
            {
                EmitDdlEvidence("AddForeignKey", entityName, null, constraintName, null,
                    DdlOperationOutcome.Unsupported, DdlHelperSource.Direct, "DDL-UNSUPPORTED-NO-HELPER");
                return CreateErrorsInfo(Errors.Failed, $"No helper registered for '{MigrateDataSource.DatasourceType}'");
            }

            // The universal RDBMS helper is the only one that currently exposes
            // GenerateAddForeignKeySql with the full action set; check there first
            // and fall back to IDataSourceHelper if the helper is a custom one.
            (string Sql, bool Success, string ErrorMessage) genResult = helper switch
            {
                Helpers.UniversalDataSourceHelpers.RdbmsHelpers.RdbmsHelper rh
                    => rh.GenerateAddForeignKeySql(entityName, columnNames, referencedEntityName,
                        referencedColumnNames, onDeleteBehavior, onUpdateBehavior, constraintName),
                _ => TryGenerateAddForeignKeyViaInterface(helper, entityName, columnNames,
                        referencedEntityName, referencedColumnNames, onDeleteBehavior,
                        onUpdateBehavior, constraintName)
            };

            if (!genResult.Success)
            {
                EmitDdlEvidence("AddForeignKey", entityName, null, constraintName, null,
                    DdlOperationOutcome.Failed, DdlHelperSource.UniversalRdbmsHelper, "DDL-GEN-FAILED");
                return CreateErrorsInfo(Errors.Failed, $"Failed to generate add-FK SQL: {genResult.ErrorMessage}");
            }

            if (string.IsNullOrWhiteSpace(genResult.Sql))
            {
                var noDdlResult = CreateErrorsInfo(Errors.Ok, $"No FK DDL required for '{MigrateDataSource.DatasourceType}'");
                TrackMigration("AddForeignKey", entityName, constraintName ?? string.Join(",", columnNames),
                    string.Empty, noDdlResult);
                EmitDdlEvidence("AddForeignKey", entityName, null, constraintName, null,
                    DdlOperationOutcome.NoOp, DdlHelperSource.UniversalRdbmsHelper, "DDL-NOOP-EMPTY-SQL");
                return noDdlResult;
            }

            var fkResult = ExecuteSql(genResult.Sql);
            TrackMigration("AddForeignKey", entityName, constraintName ?? string.Join(",", columnNames),
                genResult.Sql, fkResult);
            EmitDdlEvidence("AddForeignKey", entityName, null, constraintName, genResult.Sql,
                fkResult.Flag == Errors.Ok ? DdlOperationOutcome.Executed : DdlOperationOutcome.Failed,
                DdlHelperSource.UniversalRdbmsHelper, fkResult.Flag == Errors.Ok ? "DDL-EXECUTED" : "DDL-EXEC-FAILED");
            return fkResult;
        }

        public IErrorsInfo DropForeignKey(string entityName, string constraintName)
        {
            if (string.IsNullOrWhiteSpace(entityName))
                return CreateErrorsInfo(Errors.Failed, "Entity name is missing");

            if (string.IsNullOrWhiteSpace(constraintName))
                return CreateErrorsInfo(Errors.Failed, "Constraint name is required");

            if (MigrateDataSource == null)
                return CreateErrorsInfo(Errors.Failed, "Migration data source is not set");

            if (IsFileDataSource(MigrateDataSource))
                return CreateErrorsInfo(Errors.Failed, "Foreign keys are not supported for file datasources");

            var helper = _editor.GetDataSourceHelper(MigrateDataSource.DatasourceType);
            if (helper == null)
            {
                EmitDdlEvidence("DropForeignKey", entityName, null, constraintName, null,
                    DdlOperationOutcome.Unsupported, DdlHelperSource.Direct, "DDL-UNSUPPORTED-NO-HELPER");
                return CreateErrorsInfo(Errors.Failed, $"No helper registered for '{MigrateDataSource.DatasourceType}'");
            }

            (string Sql, bool Success, string ErrorMessage) genResult = helper switch
            {
                Helpers.UniversalDataSourceHelpers.RdbmsHelpers.RdbmsHelper rh
                    => rh.GenerateDropForeignKeySql(entityName, constraintName),
                _ => TryGenerateDropForeignKeyViaInterface(helper, entityName, constraintName)
            };

            if (!genResult.Success)
            {
                EmitDdlEvidence("DropForeignKey", entityName, null, constraintName, null,
                    DdlOperationOutcome.Failed, DdlHelperSource.UniversalRdbmsHelper, "DDL-GEN-FAILED");
                return CreateErrorsInfo(Errors.Failed, $"Failed to generate drop-FK SQL: {genResult.ErrorMessage}");
            }

            if (string.IsNullOrWhiteSpace(genResult.Sql))
            {
                var noDdlResult = CreateErrorsInfo(Errors.Ok, $"No FK DDL required for '{MigrateDataSource.DatasourceType}'");
                TrackMigration("DropForeignKey", entityName, constraintName, string.Empty, noDdlResult);
                EmitDdlEvidence("DropForeignKey", entityName, null, constraintName, null,
                    DdlOperationOutcome.NoOp, DdlHelperSource.UniversalRdbmsHelper, "DDL-NOOP-EMPTY-SQL");
                return noDdlResult;
            }

            var dropResult = ExecuteSql(genResult.Sql);
            TrackMigration("DropForeignKey", entityName, constraintName, genResult.Sql, dropResult);
            EmitDdlEvidence("DropForeignKey", entityName, null, constraintName, genResult.Sql,
                dropResult.Flag == Errors.Ok ? DdlOperationOutcome.Executed : DdlOperationOutcome.Failed,
                DdlHelperSource.UniversalRdbmsHelper, dropResult.Flag == Errors.Ok ? "DDL-EXECUTED" : "DDL-EXEC-FAILED");
            return dropResult;
        }

        // Fallback for non-RdbmsHelper IDataSourceHelper implementations that already
        // expose GenerateAddForeignKeySql through the interface.
        private static (string Sql, bool Success, string ErrorMessage) TryGenerateAddForeignKeyViaInterface(
            IDataSourceHelper helper,
            string tableName, string[] columnNames, string referencedTableName, string[] referencedColumnNames,
            string onDeleteBehavior, string onUpdateBehavior, string constraintName)
        {
            try
            {
                // Most custom helpers expose only the 4-arg signature; the constraint name
                // and behavior are ignored in that case (helpers return CASCADE behavior).
                var mi = helper.GetType().GetMethod("GenerateAddForeignKeySql",
                    new[] { typeof(string), typeof(string[]), typeof(string), typeof(string[]) });
                if (mi != null)
                {
                    var result = mi.Invoke(helper, new object[] { tableName, columnNames, referencedTableName, referencedColumnNames });
                    if (result is ValueTuple<string, bool, string> tuple)
                        return tuple;
                }
                return ("", false, "Helper does not support foreign-key generation");
            }
            catch (Exception ex)
            {
                return ("", false, ex.Message);
            }
        }

        private static (string Sql, bool Success, string ErrorMessage) TryGenerateDropForeignKeyViaInterface(
            IDataSourceHelper helper,
            string tableName, string constraintName)
        {
            try
            {
                var mi = helper.GetType().GetMethod("GenerateDropForeignKeySql",
                    new[] { typeof(string), typeof(string) });
                if (mi != null)
                {
                    var result = mi.Invoke(helper, new object[] { tableName, constraintName });
                    if (result is ValueTuple<string, bool, string> tuple)
                        return tuple;
                }
                return ("", false, "Helper does not support foreign-key drop");
            }
            catch (Exception ex)
            {
                return ("", false, ex.Message);
            }
        }

        private IErrorsInfo AddColumn(EntityStructure entity, EntityField column)
        {
            if (MigrateDataSource == null)
                return CreateErrorsInfo(Errors.Failed, "Migration data source is not set");

            if (IsFileDataSource(MigrateDataSource))
            {
                var fileResult = AddColumnToFile(column);
                TrackMigration("AddColumn", entity.EntityName, column.FieldName, string.Empty, fileResult);
                EmitDdlEvidence("AddColumn", entity.EntityName, column.FieldName, null, null,
                    fileResult.Flag == Errors.Ok ? DdlOperationOutcome.Emulated : DdlOperationOutcome.Failed,
                    DdlHelperSource.FileMutation, fileResult.Flag == Errors.Ok ? "DDL-FILE-ADD-COL" : "DDL-FILE-ADD-COL-FAILED");
                return fileResult;
            }

            var helper = _editor.GetDataSourceHelper(MigrateDataSource.DatasourceType);
            if (helper == null)
            {
                EmitDdlEvidence("AddColumn", entity.EntityName, column.FieldName, null, null,
                    DdlOperationOutcome.Unsupported, DdlHelperSource.Direct, "DDL-UNSUPPORTED-NO-HELPER");
                return CreateErrorsInfo(Errors.Failed, $"No helper registered for '{MigrateDataSource.DatasourceType}'");
            }

            var (sql, success, errorMessage) = helper.GenerateAddColumnSql(entity.EntityName, column);
            if (!success)
                return CreateErrorsInfo(Errors.Failed, $"Failed to generate add-column SQL: {errorMessage}");

            if (string.IsNullOrWhiteSpace(sql))
            {
                var noDdlResult = CreateErrorsInfo(Errors.Ok, $"No DDL required for '{MigrateDataSource.DatasourceType}'");
                TrackMigration("AddColumn", entity.EntityName, column.FieldName, string.Empty, noDdlResult);
                EmitDdlEvidence("AddColumn", entity.EntityName, column.FieldName, null, null, DdlOperationOutcome.NoOp, DdlHelperSource.UniversalRdbmsHelper, "DDL-NOOP-EMPTY-SQL");
                return noDdlResult;
            }

            var result = MigrateDataSource.ExecuteSql(sql);
            if (result == null)
            {
                var noResult = CreateErrorsInfo(Errors.Failed, "Datasource returned no result for add-column SQL");
                TrackMigration("AddColumn", entity.EntityName, column.FieldName, sql, noResult);
                EmitDdlEvidence("AddColumn", entity.EntityName, column.FieldName, null, sql, DdlOperationOutcome.Failed, DdlHelperSource.UniversalRdbmsHelper, "DDL-NO-RESULT");
                return noResult;
            }

            var finalResult = result.Flag == Errors.Ok
                ? CreateErrorsInfo(Errors.Ok, $"Added column '{column.FieldName}'")
                : result;
            TrackMigration("AddColumn", entity.EntityName, column.FieldName, sql, finalResult);
            EmitDdlEvidence("AddColumn", entity.EntityName, column.FieldName, null, sql,
                finalResult.Flag == Errors.Ok ? DdlOperationOutcome.Executed : DdlOperationOutcome.Failed,
                DdlHelperSource.UniversalRdbmsHelper, finalResult.Flag == Errors.Ok ? "DDL-EXECUTED" : "DDL-EXEC-FAILED");
            return finalResult;
        }

        private IErrorsInfo AddColumnToFile(EntityField column)
        {
            var filePath = GetFilePath(MigrateDataSource);
            if (string.IsNullOrWhiteSpace(filePath))
                return CreateErrorsInfo(Errors.Failed, "File path is missing for file-based datasource");

            var ok = FileHelper.AddColumnToFile(filePath, column.FieldName, column.DefaultValue ?? string.Empty);
            return ok
                ? CreateErrorsInfo(Errors.Ok, $"Added column '{column.FieldName}' to file")
                : CreateErrorsInfo(Errors.Failed, $"Failed to add column '{column.FieldName}' to file");
        }

        private static bool IsFileDataSource(IDataSource dataSource)
        {
            var props = dataSource?.Dataconnection?.ConnectionProp;
            return dataSource?.Category == DatasourceCategory.FILE || (props?.IsFile ?? false);
        }

        private static string GetFilePath(IDataSource dataSource)
        {
            var props = dataSource?.Dataconnection?.ConnectionProp;
            if (props == null)
                return null;

            if (!string.IsNullOrWhiteSpace(props.FilePath) && !string.IsNullOrWhiteSpace(props.FileName))
                return Path.Combine(props.FilePath, props.FileName);

            return props.FilePath;
        }

        private IErrorsInfo ExecuteSql(string sql)
        {
            var result = MigrateDataSource.ExecuteSql(sql);
            if (result == null)
                return CreateErrorsInfo(Errors.Failed, "Datasource returned no result for SQL execution");

            return result.Flag == Errors.Ok ? CreateErrorsInfo(Errors.Ok, "DDL executed successfully") : result;
        }

        /// <summary>
        /// Applies all indexes declared on an entity. Returns null when the entity
        /// has no indexes; otherwise returns a list of failure messages.
        /// </summary>
        private List<string> ApplyIndexesForEntity(EntityStructure entity)
        {
            var failures = new List<string>();
            if (entity?.Indexes == null || entity.Indexes.Count == 0)
                return null;

            // Field-level unique/indexed markers create implicit indexes that the
            // DDL layer may have already emitted. Track them here only when an
            // explicit EntityIndex row is present.
            foreach (var idx in entity.Indexes)
            {
                if (idx == null || idx.Columns == null || idx.Columns.Count == 0)
                    continue;

                var name = string.IsNullOrWhiteSpace(idx.Name)
                    ? $"IX_{entity.EntityName}_{string.Join("_", idx.Columns)}"
                    : idx.Name;

                var options = idx.Options != null && idx.Options.Count > 0
                    ? new Dictionary<string, object>(idx.Options)
                    : null;
                if (idx.IsUnique && (options == null || !options.ContainsKey("UNIQUE")))
                    options ??= new Dictionary<string, object>();
                if (idx.IsUnique && !options.ContainsKey("UNIQUE"))
                    options["UNIQUE"] = true;

                var result = CreateIndex(entity.EntityName, name, idx.Columns.ToArray(), options);
                if (result.Flag != Errors.Ok)
                    failures.Add($"index '{name}': {result.Message}");
            }

            return failures.Count == 0 ? null : failures;
        }

        /// <summary>
        /// Applies all foreign-key relations declared on an entity. Returns null
        /// when the entity has no relations; otherwise returns a list of failure
        /// messages. Each <see cref="RelationShipKeys"/> produces one FK.
        /// </summary>
        private List<string> ApplyForeignKeysForEntity(EntityStructure entity)
        {
            var failures = new List<string>();
            if (entity?.Relations == null || entity.Relations.Count == 0)
                return null;

            foreach (var rel in entity.Relations)
            {
                if (rel == null || string.IsNullOrWhiteSpace(rel.EntityColumnID))
                    continue;

                var parentEntity = rel.RelatedEntityID;
                var parentColumn = rel.RelatedEntityColumnID;
                if (string.IsNullOrWhiteSpace(parentEntity) || string.IsNullOrWhiteSpace(parentColumn))
                    continue;

                var fkName = rel.RalationName;
                var result = AddForeignKey(
                    entityName: entity.EntityName,
                    columnNames: new[] { rel.EntityColumnID },
                    referencedEntityName: parentEntity,
                    referencedColumnNames: new[] { parentColumn },
                    onDeleteBehavior: rel.OnDeleteBehavior ?? "Cascade",
                    onUpdateBehavior: rel.OnUpdateBehavior ?? "Cascade",
                    constraintName: fkName);

                if (result.Flag != Errors.Ok)
                    failures.Add($"FK '{fkName}': {result.Message}");
            }

            return failures.Count == 0 ? null : failures;
        }

        private IErrorsInfo CreateFileFromEntity(EntityStructure entity)
        {
            var filePath = GetFilePath(MigrateDataSource);
            if (string.IsNullOrWhiteSpace(filePath))
                return CreateErrorsInfo(Errors.Failed, "File path is missing for file-based datasource");

            var delimiter = GetFileDelimiter(MigrateDataSource);
            var header = string.Join(delimiter, entity.Fields?.Select(f => f.FieldName) ?? Enumerable.Empty<string>());
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllText(filePath, header + Environment.NewLine);
            return CreateErrorsInfo(Errors.Ok, $"File created at '{filePath}'");
        }

        private IErrorsInfo TruncateFile(string entityName)
        {
            var filePath = GetFilePath(MigrateDataSource);
            if (string.IsNullOrWhiteSpace(filePath))
                return CreateErrorsInfo(Errors.Failed, "File path is missing for file-based datasource");

            if (!File.Exists(filePath))
                return CreateErrorsInfo(Errors.Failed, $"File '{filePath}' does not exist");

            var lines = File.ReadAllLines(filePath);
            var header = lines.Length > 0 ? lines[0] : string.Empty;
            File.WriteAllText(filePath, string.IsNullOrEmpty(header) ? string.Empty : header + Environment.NewLine);
            return CreateErrorsInfo(Errors.Ok, $"File '{filePath}' truncated");
        }

        private IErrorsInfo DeleteFile(string entityName)
        {
            var filePath = GetFilePath(MigrateDataSource);
            if (string.IsNullOrWhiteSpace(filePath))
                return CreateErrorsInfo(Errors.Failed, "File path is missing for file-based datasource");

            if (!File.Exists(filePath))
                return CreateErrorsInfo(Errors.Failed, $"File '{filePath}' does not exist");

            File.Delete(filePath);
            return CreateErrorsInfo(Errors.Ok, $"File '{filePath}' deleted");
        }

        private IErrorsInfo RenameFile(string oldName, string newName)
        {
            var filePath = GetFilePath(MigrateDataSource);
            if (string.IsNullOrWhiteSpace(filePath))
                return CreateErrorsInfo(Errors.Failed, "File path is missing for file-based datasource");

            if (!File.Exists(filePath))
                return CreateErrorsInfo(Errors.Failed, $"File '{filePath}' does not exist");

            var dir = Path.GetDirectoryName(filePath);
            var newPath = Path.Combine(dir ?? string.Empty, newName);
            File.Move(filePath, newPath);
            return CreateErrorsInfo(Errors.Ok, $"File renamed to '{newPath}'");
        }

        private IErrorsInfo RemoveColumnFromFile(string entityName, string columnName)
        {
            var filePath = GetFilePath(MigrateDataSource);
            if (string.IsNullOrWhiteSpace(filePath))
                return CreateErrorsInfo(Errors.Failed, "File path is missing for file-based datasource");

            if (!File.Exists(filePath))
                return CreateErrorsInfo(Errors.Failed, $"File '{filePath}' does not exist");

            var updated = new EntityStructure(entityName) { Fields = new List<EntityField> { new EntityField { FieldName = columnName } } };
            var ok = FileHelper.UpdateFileStructure(_editor, updated, filePath, addColumn: false);
            return ok
                ? CreateErrorsInfo(Errors.Ok, $"Removed column '{columnName}' from file")
                : CreateErrorsInfo(Errors.Failed, $"Failed to remove column '{columnName}' from file");
        }

        private IErrorsInfo RenameColumnInFile(string entityName, string oldColumnName, string newColumnName)
        {
            var filePath = GetFilePath(MigrateDataSource);
            if (string.IsNullOrWhiteSpace(filePath))
                return CreateErrorsInfo(Errors.Failed, "File path is missing for file-based datasource");

            if (!File.Exists(filePath))
                return CreateErrorsInfo(Errors.Failed, $"File '{filePath}' does not exist");

            var delimiter = GetFileDelimiter(MigrateDataSource);
            var lines = File.ReadAllLines(filePath).ToList();
            if (lines.Count == 0)
                return CreateErrorsInfo(Errors.Failed, "File has no header row");

            var headers = lines[0].Split(delimiter);
            var idx = Array.FindIndex(headers, h => string.Equals(h, oldColumnName, StringComparison.OrdinalIgnoreCase));
            if (idx < 0)
                return CreateErrorsInfo(Errors.Failed, $"Column '{oldColumnName}' not found");

            headers[idx] = newColumnName;
            lines[0] = string.Join(delimiter, headers);
            File.WriteAllLines(filePath, lines);
            return CreateErrorsInfo(Errors.Ok, $"Renamed column '{oldColumnName}' to '{newColumnName}'");
        }

        private static char GetFileDelimiter(IDataSource dataSource)
        {
            var props = dataSource?.Dataconnection?.ConnectionProp;
            if (props != null && props.Delimiter != default(char))
                return props.Delimiter;

            if (!string.IsNullOrWhiteSpace(dataSource?.ColumnDelimiter))
                return dataSource.ColumnDelimiter[0];

            return ',';
        }

        // NOTE: EnsureEntityStructureTypes, InferDataSourceTypeFromNetType, and
        // InferNetTypeFromDataSourceType have been intentionally removed.
        //
        // The MigrationManager must NOT do type mapping or SQL generation for entity creation.
        // Each IDataSource.CreateEntityAs() implementation handles .NET type → native type
        // mapping internally. The EntityStructure should contain .NET types as produced by
        // ConvertToEntityStructure  (e.g., "System.String", "System.Int32", "System.DateTime").
        // The datasource converts these to its own native types (TEXT/INTEGER for SQLite,
        // VARCHAR/INT for SQL Server, etc.).
    }
}
