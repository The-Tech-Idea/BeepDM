
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

            if (!MigrateDataSource.CheckEntityExist(entity.EntityName))
            {
                if (!createIfMissing)
                    return CreateErrorsInfo(Errors.Failed, $"Entity '{entity.EntityName}' does not exist");

                var created = MigrateDataSource.CreateEntityAs(entity);
                var result = created
                    ? CreateErrorsInfo(Errors.Ok, $"Entity '{entity.EntityName}' created successfully")
                    : CreateErrorsInfo(Errors.Failed, $"Failed to create entity '{entity.EntityName}'");
                TrackMigration("CreateEntityAs", entity.EntityName, null, string.Empty, result);
                return result;
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
                    failures.Add($"{column.fieldname}: {addResult.Message}");
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
                current.Fields.Select(f => f.fieldname),
                StringComparer.OrdinalIgnoreCase);

            return desired.Fields.Where(f => !existing.Contains(f.fieldname)).ToList();
        }

        public IErrorsInfo CreateEntity(EntityStructure entity)
        {
            if (entity == null || string.IsNullOrWhiteSpace(entity.EntityName))
                return CreateErrorsInfo(Errors.Failed, "Entity name is missing");

            if (MigrateDataSource == null)
                return CreateErrorsInfo(Errors.Failed, "Migration data source is not set");

            if (IsFileDataSource(MigrateDataSource))
            {
                var fileResult = CreateFileFromEntity(entity);
                TrackMigration("CreateEntity", entity.EntityName, null, string.Empty, fileResult);
                return fileResult;
            }

            var helper = _editor.GetDataSourceHelper(MigrateDataSource.DatasourceType);
            if (helper == null)
                return CreateErrorsInfo(Errors.Failed, $"No helper registered for '{MigrateDataSource.DatasourceType}'");

            var (sql, success, errorMessage) = helper.GenerateCreateTableSql(entity);
            if (!success)
                return CreateErrorsInfo(Errors.Failed, $"Failed to generate create SQL: {errorMessage}");

            if (string.IsNullOrWhiteSpace(sql))
            {
                var noDdlResult = CreateErrorsInfo(Errors.Ok, $"No DDL required for '{MigrateDataSource.DatasourceType}'");
                TrackMigration("CreateEntity", entity.EntityName, null, string.Empty, noDdlResult);
                return noDdlResult;
            }

            var createResult = ExecuteSql(sql);
            TrackMigration("CreateEntity", entity.EntityName, null, sql, createResult);
            return createResult;
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
                TrackMigration("AddColumn", entity.EntityName, column.fieldname, string.Empty, fileResult);
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
                TrackMigration("AddColumn", entity.EntityName, column.fieldname, string.Empty, noDdlResult);
                return noDdlResult;
            }

            var result = MigrateDataSource.ExecuteSql(sql);
            if (result == null)
            {
                var noResult = CreateErrorsInfo(Errors.Failed, "Datasource returned no result for add-column SQL");
                TrackMigration("AddColumn", entity.EntityName, column.fieldname, sql, noResult);
                return noResult;
            }

            var finalResult = result.Flag == Errors.Ok
                ? CreateErrorsInfo(Errors.Ok, $"Added column '{column.fieldname}'")
                : result;
            TrackMigration("AddColumn", entity.EntityName, column.fieldname, sql, finalResult);
            return finalResult;
        }

        private IErrorsInfo AddColumnToFile(EntityField column)
        {
            var filePath = GetFilePath(MigrateDataSource);
            if (string.IsNullOrWhiteSpace(filePath))
                return CreateErrorsInfo(Errors.Failed, "File path is missing for file-based datasource");

            var ok = FileHelper.AddColumnToFile(filePath, column.fieldname, column.DefaultValue ?? string.Empty);
            return ok
                ? CreateErrorsInfo(Errors.Ok, $"Added column '{column.fieldname}' to file")
                : CreateErrorsInfo(Errors.Failed, $"Failed to add column '{column.fieldname}' to file");
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
            var header = string.Join(delimiter, entity.Fields?.Select(f => f.fieldname) ?? Enumerable.Empty<string>());
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

            var updated = new EntityStructure(entityName) { Fields = new List<EntityField> { new EntityField { fieldname = columnName } } };
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
