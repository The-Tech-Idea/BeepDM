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
        public IErrorsInfo EnsureEntity(EntityStructure entity, bool createIfMissing = true, bool addMissingColumns = true)
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
                    var result = created
                        ? CreateErrorsInfo(Errors.Ok, $"Entity '{entity.EntityName}' created successfully")
                        : CreateErrorsInfo(Errors.Failed, $"Failed to create entity '{entity.EntityName}'. Check ErrorObject for details.");

                    // Check ErrorObject for additional details
                    if (!created && MigrateDataSource.ErrorObject != null)
                    {
                        result.Message = MigrateDataSource.ErrorObject.Message ?? result.Message;
                        result.Ex = MigrateDataSource.ErrorObject.Ex;
                    }

                    TrackMigration("CreateEntityAs", entity.EntityName, null, string.Empty, result);
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
                return CreateErrorsInfo(Errors.Ok, $"Entity '{entity.EntityName}' already exists");

            var current = MigrateDataSource.GetEntityStructure(entity.EntityName, true);
            if (current == null)
                return CreateErrorsInfo(Errors.Failed, $"Failed to load structure for '{entity.EntityName}'");

            var missingColumns = GetMissingColumns(current, entity);
            if (missingColumns.Count == 0)
                return CreateErrorsInfo(Errors.Ok, $"Entity '{entity.EntityName}' is up to date");

            var failures = new List<string>();
            foreach (var column in missingColumns)
            {
                var addResult = AddColumn(entity, column);
                if (addResult.Flag != Errors.Ok)
                    failures.Add($"{column.FieldName}: {addResult.Message}");
            }

            if (failures.Count > 0)
            {
                return CreateErrorsInfo(
                    Errors.Failed,
                    $"Failed to add {failures.Count} column(s) to '{entity.EntityName}': {string.Join("; ", failures)}");
            }

            return CreateErrorsInfo(
                Errors.Ok,
                $"Added {missingColumns.Count} column(s) to '{entity.EntityName}' successfully");
        }

        public IErrorsInfo EnsureEntity(Type pocoType, bool createIfMissing = true, bool addMissingColumns = true, bool detectRelationships = true)
        {
            if (pocoType == null)
                return CreateErrorsInfo(Errors.Failed, "POCO type cannot be null");

            if (_editor?.classCreator == null)
                return CreateErrorsInfo(Errors.Failed, "Class creator is not available");

            var entity = _editor.classCreator.ConvertToEntityStructure(pocoType);
            if (entity == null)
                return CreateErrorsInfo(Errors.Failed, $"Failed to convert POCO '{pocoType.Name}' to EntityStructure");

            return EnsureEntity(entity, createIfMissing, addMissingColumns);
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
