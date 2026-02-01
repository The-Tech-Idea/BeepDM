
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.UOW.Helpers;
using TheTechIdea.Beep.Editor.UOW.Interfaces;
using TheTechIdea.Beep.Helpers.FileandFolderHelpers;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.Migration
{
    /// <summary>
    /// MigrationManager provides datasource-agnostic schema migration capabilities.
    /// 
    /// Key Design Principles:
    /// 1. Entity Creation: Uses IDataSource.CreateEntityAs() for datasource-agnostic entity creation.
    ///    Each datasource implementation handles .NET type mapping to its native type system internally.
    /// 2. Type Mapping: Uses DataTypesHelper to map .NET types (Fieldtype) to datasource-specific types (FieldType).
    ///    This ensures CreateEntityAs receives properly typed EntityStructure for any datasource type.
    /// 3. Schema Modifications: Column operations (AddColumn, AlterColumn, DropColumn) use IDataSourceHelper
    ///    for SQL generation when direct IDataSource methods are not available. These operations are
    ///    primarily for RDBMS datasources that support DDL operations.
    /// 4. Validation: Uses IDataSourceHelper.ValidateEntity() before creation to catch issues early.
    /// 
    /// This approach ensures compatibility with all 200+ datasource types (RDBMS, NoSQL, File-based, Cloud, etc.)
    /// by leveraging each datasource's own CreateEntityAs implementation rather than generating SQL directly.
    /// </summary>
    public partial class MigrationManager : IMigrationManager
    {
        private readonly IDMEEditor _editor;

        public IDMEEditor DMEEditor => _editor;
        public IDataSource MigrateDataSource { get; set; }

        public MigrationManager(IDMEEditor editor, IDataSource dataSource = null)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
            MigrateDataSource = dataSource;
        }

        public IErrorsInfo EnsureEntity(EntityStructure entity, bool createIfMissing = true, bool addMissingColumns = true)
        {
            if (entity == null)
                return CreateErrorsInfo(Errors.Failed, "Entity structure cannot be null");

            if (string.IsNullOrWhiteSpace(entity.EntityName))
                return CreateErrorsInfo(Errors.Failed, "Entity name cannot be empty");

            if (MigrateDataSource == null)
                return CreateErrorsInfo(Errors.Failed, "Migration data source is not set");

            // Ensure entity structure has proper type mappings for target datasource
            EnsureEntityStructureTypes(entity);

            if (!MigrateDataSource.CheckEntityExist(entity.EntityName))
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

            var entity = _editor.classCreator.ConvertPocoToEntity(pocoType, detectRelationships);
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

            // Ensure entity structure has proper type mappings for target datasource
            EnsureEntityStructureTypes(entity);

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
                return fileResult;
            }

            var helper = _editor.GetDataSourceHelper(MigrateDataSource.DatasourceType);
            if (helper == null)
                return CreateErrorsInfo(Errors.Failed, $"No helper registered for '{MigrateDataSource.DatasourceType}'");

            var (sql, success, errorMessage) = helper.GenerateDropTableSql(entityName);
            if (!success)
                return CreateErrorsInfo(Errors.Failed, $"Failed to generate drop SQL: {errorMessage}");

            if (string.IsNullOrWhiteSpace(sql))
            {
                var noDdlResult = CreateErrorsInfo(Errors.Ok, $"No DDL required for '{MigrateDataSource.DatasourceType}'");
                TrackMigration("DropEntity", entityName, null, string.Empty, noDdlResult);
                return noDdlResult;
            }

            var dropResult = ExecuteSql(sql);
            TrackMigration("DropEntity", entityName, null, sql, dropResult);
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
                return fileResult;
            }

            var helper = _editor.GetDataSourceHelper(MigrateDataSource.DatasourceType);
            if (helper == null)
                return CreateErrorsInfo(Errors.Failed, $"No helper registered for '{MigrateDataSource.DatasourceType}'");

            var (sql, success, errorMessage) = helper.GenerateTruncateTableSql(entityName);
            if (!success)
                return CreateErrorsInfo(Errors.Failed, $"Failed to generate truncate SQL: {errorMessage}");

            if (string.IsNullOrWhiteSpace(sql))
            {
                var noDdlResult = CreateErrorsInfo(Errors.Ok, $"No DDL required for '{MigrateDataSource.DatasourceType}'");
                TrackMigration("TruncateEntity", entityName, null, string.Empty, noDdlResult);
                return noDdlResult;
            }

            var truncateResult = ExecuteSql(sql);
            TrackMigration("TruncateEntity", entityName, null, sql, truncateResult);
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
                return fileResult;
            }

            var helper = _editor.GetDataSourceHelper(MigrateDataSource.DatasourceType);
            if (helper == null)
                return CreateErrorsInfo(Errors.Failed, $"No helper registered for '{MigrateDataSource.DatasourceType}'");

            var (sql, success, errorMessage) = helper.GenerateRenameTableSql(oldName, newName);
            if (!success)
                return CreateErrorsInfo(Errors.Failed, $"Failed to generate rename SQL: {errorMessage}");

            if (string.IsNullOrWhiteSpace(sql))
            {
                var noDdlResult = CreateErrorsInfo(Errors.Ok, $"No DDL required for '{MigrateDataSource.DatasourceType}'");
                TrackMigration("RenameEntity", $"{oldName}->{newName}", null, string.Empty, noDdlResult);
                return noDdlResult;
            }

            var renameResult = ExecuteSql(sql);
            TrackMigration("RenameEntity", $"{oldName}->{newName}", null, sql, renameResult);
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
                return CreateErrorsInfo(Errors.Failed, $"No helper registered for '{MigrateDataSource.DatasourceType}'");

            var (sql, success, errorMessage) = helper.GenerateAlterColumnSql(entityName, columnName, newColumn);
            if (!success)
                return CreateErrorsInfo(Errors.Failed, $"Failed to generate alter SQL: {errorMessage}");

            if (string.IsNullOrWhiteSpace(sql))
            {
                var noDdlResult = CreateErrorsInfo(Errors.Ok, $"No DDL required for '{MigrateDataSource.DatasourceType}'");
                TrackMigration("AlterColumn", entityName, columnName, string.Empty, noDdlResult);
                return noDdlResult;
            }

            var alterResult = ExecuteSql(sql);
            TrackMigration("AlterColumn", entityName, columnName, sql, alterResult);
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
                return fileResult;
            }

            var helper = _editor.GetDataSourceHelper(MigrateDataSource.DatasourceType);
            if (helper == null)
                return CreateErrorsInfo(Errors.Failed, $"No helper registered for '{MigrateDataSource.DatasourceType}'");

            var (sql, success, errorMessage) = helper.GenerateDropColumnSql(entityName, columnName);
            if (!success)
                return CreateErrorsInfo(Errors.Failed, $"Failed to generate drop-column SQL: {errorMessage}");

            if (string.IsNullOrWhiteSpace(sql))
            {
                var noDdlResult = CreateErrorsInfo(Errors.Ok, $"No DDL required for '{MigrateDataSource.DatasourceType}'");
                TrackMigration("DropColumn", entityName, columnName, string.Empty, noDdlResult);
                return noDdlResult;
            }

            var dropResult = ExecuteSql(sql);
            TrackMigration("DropColumn", entityName, columnName, sql, dropResult);
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
                return fileResult;
            }

            var helper = _editor.GetDataSourceHelper(MigrateDataSource.DatasourceType);
            if (helper == null)
                return CreateErrorsInfo(Errors.Failed, $"No helper registered for '{MigrateDataSource.DatasourceType}'");

            var (sql, success, errorMessage) = helper.GenerateRenameColumnSql(entityName, oldColumnName, newColumnName);
            if (!success)
                return CreateErrorsInfo(Errors.Failed, $"Failed to generate rename-column SQL: {errorMessage}");

            if (string.IsNullOrWhiteSpace(sql))
            {
                var noDdlResult = CreateErrorsInfo(Errors.Ok, $"No DDL required for '{MigrateDataSource.DatasourceType}'");
                TrackMigration("RenameColumn", entityName, $"{oldColumnName}->{newColumnName}", string.Empty, noDdlResult);
                return noDdlResult;
            }

            var renameResult = ExecuteSql(sql);
            TrackMigration("RenameColumn", entityName, $"{oldColumnName}->{newColumnName}", sql, renameResult);
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
                return noDdlResult;
            }

            var indexResult = ExecuteSql(sql);
            TrackMigration("CreateIndex", entityName, indexName, sql, indexResult);
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
                return fileResult;
            }

            var helper = _editor.GetDataSourceHelper(MigrateDataSource.DatasourceType);
            if (helper == null)
                return CreateErrorsInfo(Errors.Failed, $"No helper registered for '{MigrateDataSource.DatasourceType}'");

            var (sql, success, errorMessage) = helper.GenerateAddColumnSql(entity.EntityName, column);
            if (!success)
                return CreateErrorsInfo(Errors.Failed, $"Failed to generate add-column SQL: {errorMessage}");

            if (string.IsNullOrWhiteSpace(sql))
            {
                var noDdlResult = CreateErrorsInfo(Errors.Ok, $"No DDL required for '{MigrateDataSource.DatasourceType}'");
                TrackMigration("AddColumn", entity.EntityName, column.FieldName , string.Empty, noDdlResult);
                return noDdlResult;
            }

            var result = MigrateDataSource.ExecuteSql(sql);
            if (result == null)
            {
                var noResult = CreateErrorsInfo(Errors.Failed, "Datasource returned no result for add-column SQL");
                TrackMigration("AddColumn", entity.EntityName, column.FieldName, sql, noResult);
                return noResult;
            }

            var finalResult = result.Flag == Errors.Ok
                ? CreateErrorsInfo(Errors.Ok, $"Added column '{column.FieldName}'")
                : result;
            TrackMigration("AddColumn", entity.EntityName, column.FieldName, sql, finalResult);
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

        /// <summary>
        /// Ensures EntityStructure fields have proper type mappings for the target datasource using DataTypesHelper.
        /// This is critical for datasource-agnostic entity creation via CreateEntityAs.
        /// Maps .NET types (Fieldtype) to datasource-specific types (FieldType) for the target datasource.
        /// Each datasource's CreateEntityAs implementation uses these mappings to create entities in its native format.
        /// </summary>
        private void EnsureEntityStructureTypes(EntityStructure entity)
        {
            if (entity?.Fields == null || _editor?.typesHelper == null || MigrateDataSource == null)
                return;

            try
            {
                var dataSourceName = MigrateDataSource.DatasourceName ?? MigrateDataSource.DatasourceType.ToString();

                foreach (var field in entity.Fields)
                {
                    // Priority 1: If Fieldtype (.NET type) is set, map it to datasource-specific FieldType
                    // This is the preferred approach as .NET types are universal
                    if (!string.IsNullOrWhiteSpace(field.Fieldtype))
                    {
                        // Use DataTypesHelper to map .NET type to datasource-specific type
                        // GetDataType uses the field's Fieldtype property to determine the mapping
                        var datasourceType = _editor.typesHelper.GetDataType(dataSourceName, field);
                        
                        if (!string.IsNullOrWhiteSpace(datasourceType))
                        {
                            field.Fieldtype = datasourceType;
                        }
                        else if (string.IsNullOrWhiteSpace(field.Fieldtype))
                        {
                            // Fallback: infer datasource type from .NET type name
                            field.Fieldtype = InferDataSourceTypeFromNetType(field.Fieldtype);
                        }
                    }
                    // Priority 2: If FieldType (datasource-specific) is set but Fieldtype is not, map back to .NET type
                    else if (!string.IsNullOrWhiteSpace(field.Fieldtype) && 
                             !field.Fieldtype.Contains("System.") && 
                             !field.Fieldtype.Contains("Microsoft."))
                    {
                        // FieldType is datasource-specific, map it back to .NET type using IDataSourceHelper
                        var helper = _editor.GetDataSourceHelper(MigrateDataSource.DatasourceType);
                        if (helper != null)
                        {
                            try
                            {
                                var clrType = helper.MapDatasourceTypeToClrType(field.Fieldtype);
                                if (clrType != null)
                                {
                                    field.Fieldtype = clrType.FullName ?? clrType.Name;
                                }
                            }
                            catch
                            {
                                // If mapping fails, infer .NET type from datasource type name
                                field.Fieldtype = InferNetTypeFromDataSourceType(field.Fieldtype);
                            }
                        }
                        else
                        {
                            // No helper available, infer .NET type
                            field.Fieldtype = InferNetTypeFromDataSourceType(field.Fieldtype);
                        }
                    }
                    // Priority 3: If neither is set, set defaults
                    else if (string.IsNullOrWhiteSpace(field.Fieldtype) && string.IsNullOrWhiteSpace(field.Fieldtype))
                    {
                        field.Fieldtype = "System.String";
                        field.Fieldtype = "VARCHAR"; // Common default, will be mapped by datasource
                    }
                }
            }
            catch (Exception ex)
            {
                _editor?.AddLogMessage("Beep", 
                    $"Warning: Could not ensure entity structure types for '{entity?.EntityName}': {ex.Message}", 
                    DateTime.Now, 0, null, Errors.Warning);
            }
        }

        /// <summary>
        /// Infers .NET type from datasource-specific type name (fallback method).
        /// </summary>
        private static string InferNetTypeFromDataSourceType(string datasourceType)
        {
            if (string.IsNullOrWhiteSpace(datasourceType))
                return "System.String";

            var typeLower = datasourceType.ToLowerInvariant();
            
            if (typeLower.Contains("int") || typeLower.Contains("integer"))
                return "System.Int32";
            if (typeLower.Contains("bigint") || typeLower.Contains("long"))
                return "System.Int64";
            if (typeLower.Contains("decimal") || typeLower.Contains("numeric") || typeLower.Contains("money"))
                return "System.Decimal";
            if (typeLower.Contains("float") || typeLower.Contains("real"))
                return "System.Double";
            if (typeLower.Contains("bool") || typeLower.Contains("bit"))
                return "System.Boolean";
            if (typeLower.Contains("date") || typeLower.Contains("time"))
                return "System.DateTime";
            if (typeLower.Contains("guid") || typeLower.Contains("uniqueidentifier"))
                return "System.Guid";
            if (typeLower.Contains("binary") || typeLower.Contains("varbinary") || typeLower.Contains("blob"))
                return "System.Byte[]";

            return "System.String"; // Default fallback
        }

        /// <summary>
        /// Infers datasource-specific type from .NET type name (fallback method).
        /// </summary>
        private static string InferDataSourceTypeFromNetType(string netType)
        {
            if (string.IsNullOrWhiteSpace(netType))
                return "VARCHAR";

            var typeLower = netType.ToLowerInvariant();
            
            if (typeLower.Contains("int32") || typeLower == "int")
                return "INT";
            if (typeLower.Contains("int64") || typeLower == "long")
                return "BIGINT";
            if (typeLower.Contains("decimal"))
                return "DECIMAL";
            if (typeLower.Contains("double") || typeLower.Contains("float"))
                return "FLOAT";
            if (typeLower.Contains("bool") || typeLower == "boolean")
                return "BIT";
            if (typeLower.Contains("datetime"))
                return "DATETIME";
            if (typeLower.Contains("guid"))
                return "UNIQUEIDENTIFIER";
            if (typeLower.Contains("byte[]") || typeLower.Contains("bytearray"))
                return "VARBINARY";

            return "VARCHAR"; // Default fallback
        }

        #region Entity Framework-like Migration Discovery and Application

        /// <summary>
        /// Discovers all types that inherit from Entity in the specified namespace(s).
        /// Similar to EF Core's DbContext discovery pattern.
        /// </summary>
        public List<Type> DiscoverEntityTypes(string namespaceName = null, Assembly assembly = null, bool includeSubNamespaces = true)
        {
            var entityTypes = new List<Type>();
            var assemblies = assembly != null 
                ? new[] { assembly } 
                : GetSearchableAssemblies();

            foreach (var asm in assemblies)
            {
                try
                {
                    var types = asm.GetTypes()
                        .Where(t => IsEntityType(t, namespaceName, includeSubNamespaces))
                        .ToList();

                    entityTypes.AddRange(types);
                }
                catch (ReflectionTypeLoadException ex)
                {
                    // Handle partially loaded assemblies
                    var loadedTypes = ex.Types.Where(t => t != null && IsEntityType(t, namespaceName, includeSubNamespaces));
                    entityTypes.AddRange(loadedTypes);
                }
                catch (Exception ex)
                {
                    _editor?.AddLogMessage("Beep", 
                        $"Warning: Could not scan assembly '{asm.FullName}' for Entity types: {ex.Message}", 
                        DateTime.Now, 0, null, Errors.Warning);
                }
            }

            return entityTypes.Distinct().ToList();
        }

        /// <summary>
        /// Discovers all types that inherit from Entity in all loaded assemblies.
        /// Scans all assemblies in AppDomain and DMEEditor's assembly handler.
        /// </summary>
        public List<Type> DiscoverAllEntityTypes(bool includeSubNamespaces = true)
        {
            return DiscoverEntityTypes(null, null, includeSubNamespaces);
        }

        /// <summary>
        /// Ensures database is created with all discovered Entity types.
        /// Similar to EF Core's Database.EnsureCreated().
        /// Creates entities for all classes that inherit from Entity.
        /// </summary>
        public IErrorsInfo EnsureDatabaseCreated(string namespaceName = null, Assembly assembly = null, bool detectRelationships = true, IProgress<PassedArgs> progress = null)
        {
            if (MigrateDataSource == null)
                return CreateErrorsInfo(Errors.Failed, "Migration data source is not set");

            try
            {
                var entityTypes = DiscoverEntityTypes(namespaceName, assembly, includeSubNamespaces: true);
                if (entityTypes.Count == 0)
                {
                    var msg = string.IsNullOrWhiteSpace(namespaceName) 
                        ? "No Entity types found in loaded assemblies" 
                        : $"No Entity types found in namespace '{namespaceName}'";
                    return CreateErrorsInfo(Errors.Warning, msg);
                }

                progress?.Report(new PassedArgs { Messege = $"Found {entityTypes.Count} Entity type(s) to migrate" });

                var errors = new List<string>();
                int created = 0;
                int skipped = 0;

                foreach (var entityType in entityTypes)
                {
                    try
                    {
                        progress?.Report(new PassedArgs { Messege = $"Processing Entity type: {entityType.Name}" });

                        // Convert Entity type to EntityStructure
                        var entityStructure = _editor?.classCreator?.ConvertPocoToEntity(entityType, detectRelationships);
                        if (entityStructure == null)
                        {
                            errors.Add($"Failed to convert {entityType.Name} to EntityStructure");
                            continue;
                        }

                        // Use table name from type name or Table attribute if present
                        var tableAttr = entityType.GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.TableAttribute>();
                        if (tableAttr != null && !string.IsNullOrWhiteSpace(tableAttr.Name))
                        {
                            entityStructure.EntityName = tableAttr.Name;
                        }

                        // Ensure entity exists (create if missing)
                        var result = EnsureEntity(entityStructure, createIfMissing: true, addMissingColumns: false);
                        if (result.Flag == Errors.Ok)
                        {
                            created++;
                        }
                        else if (result.Flag == Errors.Warning && result.Message.Contains("already exists"))
                        {
                            skipped++;
                        }
                        else
                        {
                            errors.Add($"{entityType.Name}: {result.Message}");
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"{entityType.Name}: {ex.Message}");
                    }
                }

                var summary = $"Created {created} entity(ies), skipped {skipped} existing";
                if (errors.Count > 0)
                {
                    summary += $", {errors.Count} error(s)";
                    return CreateErrorsInfo(Errors.Failed, $"{summary}. Errors: {string.Join("; ", errors)}");
                }

                return CreateErrorsInfo(Errors.Ok, summary);
            }
            catch (Exception ex)
            {
                return CreateErrorsInfo(Errors.Failed, $"Exception during EnsureDatabaseCreated: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Applies migrations for all discovered Entity types.
        /// Compares Entity classes with database schema and applies changes.
        /// Similar to EF Core's Database.Migrate().
        /// </summary>
        public IErrorsInfo ApplyMigrations(string namespaceName = null, Assembly assembly = null, bool detectRelationships = true, bool addMissingColumns = true, IProgress<PassedArgs> progress = null)
        {
            if (MigrateDataSource == null)
                return CreateErrorsInfo(Errors.Failed, "Migration data source is not set");

            try
            {
                var entityTypes = DiscoverEntityTypes(namespaceName, assembly, includeSubNamespaces: true);
                if (entityTypes.Count == 0)
                {
                    var msg = string.IsNullOrWhiteSpace(namespaceName) 
                        ? "No Entity types found in loaded assemblies" 
                        : $"No Entity types found in namespace '{namespaceName}'";
                    return CreateErrorsInfo(Errors.Warning, msg);
                }

                progress?.Report(new PassedArgs { Messege = $"Applying migrations for {entityTypes.Count} Entity type(s)" });

                var errors = new List<string>();
                int created = 0;
                int updated = 0;
                int skipped = 0;

                foreach (var entityType in entityTypes)
                {
                    try
                    {
                        progress?.Report(new PassedArgs { Messege = $"Migrating Entity type: {entityType.Name}" });

                        // Convert Entity type to EntityStructure
                        var entityStructure = _editor?.classCreator?.ConvertPocoToEntity(entityType, detectRelationships);
                        if (entityStructure == null)
                        {
                            errors.Add($"Failed to convert {entityType.Name} to EntityStructure");
                            continue;
                        }

                        // Use table name from type name or Table attribute if present
                        var tableAttr = entityType.GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.TableAttribute>();
                        if (tableAttr != null && !string.IsNullOrWhiteSpace(tableAttr.Name))
                        {
                            entityStructure.EntityName = tableAttr.Name;
                        }

                        // Ensure entity exists and add missing columns
                        var existed = MigrateDataSource.CheckEntityExist(entityStructure.EntityName);
                        var result = EnsureEntity(entityStructure, createIfMissing: true, addMissingColumns: addMissingColumns);
                        
                        if (result.Flag == Errors.Ok)
                        {
                            if (!existed)
                                created++;
                            else if (result.Message.Contains("Added") || result.Message.Contains("column"))
                                updated++;
                            else
                                skipped++;
                        }
                        else
                        {
                            errors.Add($"{entityType.Name}: {result.Message}");
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"{entityType.Name}: {ex.Message}");
                    }
                }

                var summary = $"Created {created} entity(ies), updated {updated} entity(ies), skipped {skipped} unchanged";
                if (errors.Count > 0)
                {
                    summary += $", {errors.Count} error(s)";
                    return CreateErrorsInfo(Errors.Failed, $"{summary}. Errors: {string.Join("; ", errors)}");
                }

                return CreateErrorsInfo(Errors.Ok, summary);
            }
            catch (Exception ex)
            {
                return CreateErrorsInfo(Errors.Failed, $"Exception during ApplyMigrations: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets migration summary comparing Entity classes with current database state.
        /// Returns list of entities that need creation or updates.
        /// </summary>
        public MigrationSummary GetMigrationSummary(string namespaceName = null, Assembly assembly = null, bool detectRelationships = true)
        {
            var summary = new MigrationSummary
            {
                EntitiesToCreate = new List<string>(),
                EntitiesToUpdate = new List<string>(),
                EntitiesUpToDate = new List<string>(),
                Errors = new List<string>()
            };

            if (MigrateDataSource == null)
            {
                summary.Errors.Add("Migration data source is not set");
                return summary;
            }

            try
            {
                var entityTypes = DiscoverEntityTypes(namespaceName, assembly, includeSubNamespaces: true);
                
                foreach (var entityType in entityTypes)
                {
                    try
                    {
                        // Convert Entity type to EntityStructure
                        var entityStructure = _editor?.classCreator?.ConvertPocoToEntity(entityType, detectRelationships);
                        if (entityStructure == null)
                        {
                            summary.Errors.Add($"Failed to convert {entityType.Name} to EntityStructure");
                            continue;
                        }

                        // Use table name from type name or Table attribute if present
                        var tableAttr = entityType.GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.TableAttribute>();
                        if (tableAttr != null && !string.IsNullOrWhiteSpace(tableAttr.Name))
                        {
                            entityStructure.EntityName = tableAttr.Name;
                        }

                        var exists = MigrateDataSource.CheckEntityExist(entityStructure.EntityName);
                        if (!exists)
                        {
                            summary.EntitiesToCreate.Add(entityStructure.EntityName);
                        }
                        else
                        {
                            // Check for missing columns
                            var current = MigrateDataSource.GetEntityStructure(entityStructure.EntityName, true);
                            if (current != null)
                            {
                                var missingColumns = GetMissingColumns(current, entityStructure);
                                if (missingColumns.Count > 0)
                                {
                                    summary.EntitiesToUpdate.Add($"{entityStructure.EntityName} ({missingColumns.Count} missing column(s))");
                                }
                                else
                                {
                                    summary.EntitiesUpToDate.Add(entityStructure.EntityName);
                                }
                            }
                            else
                            {
                                summary.EntitiesUpToDate.Add(entityStructure.EntityName);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        summary.Errors.Add($"{entityType.Name}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                summary.Errors.Add($"Exception during GetMigrationSummary: {ex.Message}");
            }

            return summary;
        }

        /// <summary>
        /// Checks if a type inherits from Entity (or implements IEntity) and matches namespace filter.
        /// </summary>
        private bool IsEntityType(Type type, string namespaceFilter, bool includeSubNamespaces)
        {
            if (type == null) return false;
            if (!type.IsClass) return false;
            if (type.IsAbstract) return false;
            if (type.IsInterface) return false;
            if (type.IsGenericTypeDefinition) return false;
            if (type.IsNested && !type.IsNestedPublic) return false;

            // Check if type inherits from Entity class
            var baseType = type.BaseType;
            bool inheritsFromEntity = false;
            while (baseType != null)
            {
                if (baseType.Name == "Entity" || baseType.FullName == "TheTechIdea.Beep.Editor.Entity")
                {
                    inheritsFromEntity = true;
                    break;
                }
                baseType = baseType.BaseType;
            }

            // Also check if type implements IEntity interface (for cases where Entity base class might not be used)
            if (!inheritsFromEntity)
            {
                var interfaces = type.GetInterfaces();
                inheritsFromEntity = interfaces.Any(i => 
                    i.Name == "IEntity" || 
                    i.FullName == "TheTechIdea.Beep.Editor.IEntity");
            }

            if (!inheritsFromEntity) return false;

            // Check namespace filter
            if (string.IsNullOrWhiteSpace(namespaceFilter))
                return true;

            if (includeSubNamespaces)
            {
                return type.Namespace != null && 
                       (type.Namespace.Equals(namespaceFilter, StringComparison.OrdinalIgnoreCase) ||
                        type.Namespace.StartsWith(namespaceFilter + ".", StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                return type.Namespace != null && 
                       type.Namespace.Equals(namespaceFilter, StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Gets all searchable assemblies from current AppDomain and DMEEditor's assembly handler.
        /// </summary>
        private IEnumerable<Assembly> GetSearchableAssemblies()
        {
            var assemblies = new List<Assembly>();
            
            // Add loaded assemblies from AppDomain
            assemblies.AddRange(AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location)));

            // Add assemblies from DMEEditor's assembly handler if available
            if (_editor?.assemblyHandler?.Assemblies != null)
            {
                assemblies.AddRange(_editor.assemblyHandler.Assemblies
                    .Select(a => a.DllLib)
                    .Where(a => a != null));
            }

            return assemblies.Distinct();
        }

        #endregion

        private static IErrorsInfo CreateErrorsInfo(Errors flag, string message, Exception ex = null)
        {
            return new ErrorsInfo
            {
                Flag = flag,
                Message = message,
                Ex = ex
            };
        }

        private void TrackMigration(string operation, string entityName, string columnName, string sql, IErrorsInfo result)
        {
            try
            {
                var configEditor = _editor?.ConfigEditor as ConfigEditor;
                if (configEditor == null)
                    return;

                var dataSourceName = MigrateDataSource?.DatasourceName ?? string.Empty;
                var dataSourceType = MigrateDataSource?.DatasourceType ?? DataSourceType.Unknown;

                var record = new MigrationRecord
                {
                    MigrationId = Guid.NewGuid().ToString(),
                    Name = operation,
                    AppliedOnUtc = DateTime.UtcNow,
                    Success = result?.Flag == Errors.Ok,
                    Steps = new List<MigrationStep>
                    {
                        new MigrationStep
                        {
                            Operation = operation,
                            EntityName = entityName,
                            ColumnName = columnName,
                            Sql = sql ?? string.Empty,
                            Success = result?.Flag == Errors.Ok,
                            Message = result?.Message
                        }
                    }
                };

                configEditor.AppendMigrationRecord(dataSourceName, dataSourceType, record);
            }
            catch (Exception ex)
            {
                _editor?.AddLogMessage("Beep", $"Failed to track migration operation '{operation}': {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
        }
    }
}
