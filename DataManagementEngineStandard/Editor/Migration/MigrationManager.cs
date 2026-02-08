
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
    ///    The MigrationManager does NOT do any type mapping — it passes EntityStructure with .NET types
    ///    (e.g., "System.String", "System.Int32") and the datasource converts them.
    /// 2. Type Discovery: Uses ConvertToEntityStructure  to convert POCO/Entity classes to EntityStructure.
    ///    This produces .NET type names which are universal across all datasources.
    /// 3. Schema Modifications: Column operations (AddColumn, AlterColumn, DropColumn) use IDataSourceHelper
    ///    for SQL generation when direct IDataSource methods are not available. These operations are
    ///    primarily for RDBMS datasources that support DDL operations.
    /// 4. Assembly Discovery: Scans registered assemblies, entry assembly references, AppDomain,
    ///    and DMEEditor's assembly handler to find Entity types across projects.
    /// 
    /// This approach ensures compatibility with all 200+ datasource types (RDBMS, NoSQL, File-based, Cloud, etc.)
    /// by leveraging each datasource's own CreateEntityAs implementation rather than generating SQL directly.
    /// </summary>
    public partial class MigrationManager : IMigrationManager
    {
        private readonly IDMEEditor _editor;
        private readonly HashSet<Assembly> _registeredAssemblies = new HashSet<Assembly>();
        private readonly object _assemblyLock = new object();

        public IDMEEditor DMEEditor => _editor;
        public IDataSource MigrateDataSource { get; set; }

        public MigrationManager(IDMEEditor editor, IDataSource dataSource = null)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
            MigrateDataSource = dataSource;
        }

        #region Assembly Registration

        /// <summary>
        /// Register an additional assembly for entity type discovery.
        /// Use this when entity classes live in separate projects/DLLs that may not be
        /// automatically found by AppDomain scanning (e.g., lazily-loaded assemblies).
        /// </summary>
        public void RegisterAssembly(Assembly assembly)
        {
            if (assembly == null) return;
            lock (_assemblyLock)
            {
                if (_registeredAssemblies.Add(assembly))
                {
                    _editor?.AddLogMessage("Beep",
                        $"MigrationManager: Registered assembly '{assembly.GetName().Name}' for entity discovery",
                        DateTime.Now, 0, null, Errors.Ok);
                }
            }
        }

        /// <summary>
        /// Register multiple assemblies for entity type discovery.
        /// </summary>
        public void RegisterAssemblies(IEnumerable<Assembly> assemblies)
        {
            if (assemblies == null) return;
            foreach (var assembly in assemblies)
            {
                RegisterAssembly(assembly);
            }
        }

        /// <summary>
        /// Gets all currently registered assemblies (manual + auto-discovered).
        /// Useful for diagnostics when entity types are not being found.
        /// </summary>
        public IReadOnlyList<Assembly> GetRegisteredAssemblies()
        {
            lock (_assemblyLock)
            {
                return _registeredAssemblies.ToList().AsReadOnly();
            }
        }

        #endregion

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

        // NOTE: EnsureEntityStructureTypes, InferDataSourceTypeFromNetType, and
        // InferNetTypeFromDataSourceType have been intentionally removed.
        //
        // The MigrationManager must NOT do type mapping or SQL generation for entity creation.
        // Each IDataSource.CreateEntityAs() implementation handles .NET type → native type
        // mapping internally. The EntityStructure should contain .NET types as produced by
        // ConvertToEntityStructure  (e.g., "System.String", "System.Int32", "System.DateTime").
        // The datasource converts these to its own native types (TEXT/INTEGER for SQLite,
        // VARCHAR/INT for SQL Server, etc.).

        #region Entity Framework-like Migration Discovery and Application

        /// <summary>
        /// Discovers all types that inherit from Entity in the specified namespace(s).
        /// Searches in the given assembly, registered assemblies, entry assembly and its references,
        /// AppDomain assemblies, and DMEEditor's assembly handler.
        /// </summary>
        public List<Type> DiscoverEntityTypes(string namespaceName = null, Assembly assembly = null, bool includeSubNamespaces = true)
        {
            var entityTypes = new List<Type>();
            IEnumerable<Assembly> assemblies;

            if (assembly != null)
            {
                // When a specific assembly is provided, also scan its referenced assemblies
                // to catch entity types in projects it depends on
                var asmSet = new List<Assembly> { assembly };
                try
                {
                    foreach (var refName in assembly.GetReferencedAssemblies())
                    {
                        try
                        {
                            var refAsm = Assembly.Load(refName);
                            if (refAsm != null && !refAsm.IsDynamic)
                                asmSet.Add(refAsm);
                        }
                        catch { }
                    }
                }
                catch { }
                assemblies = asmSet;
            }
            else
            {
                assemblies = GetSearchableAssemblies();
            }

            var asmList = assemblies.ToList();
            _editor?.AddLogMessage("Beep",
                $"MigrationManager.DiscoverEntityTypes: Scanning {asmList.Count} assembly(ies)" +
                (string.IsNullOrWhiteSpace(namespaceName) ? "" : $" in namespace '{namespaceName}'"),
                DateTime.Now, 0, null, Errors.Ok);

            int scannedCount = 0;
            foreach (var asm in asmList)
            {
                try
                {
                    var types = asm.GetTypes()
                        .Where(t => IsEntityType(t, namespaceName, includeSubNamespaces))
                        .ToList();

                    if (types.Count > 0)
                    {
                        _editor?.AddLogMessage("Beep",
                            $"  Found {types.Count} Entity type(s) in '{asm.GetName().Name}': {string.Join(", ", types.Select(t => t.Name))}",
                            DateTime.Now, 0, null, Errors.Ok);
                    }

                    entityTypes.AddRange(types);
                    scannedCount++;
                }
                catch (ReflectionTypeLoadException ex)
                {
                    // Handle partially loaded assemblies
                    var loadedTypes = ex.Types
                        .Where(t => t != null && IsEntityType(t, namespaceName, includeSubNamespaces))
                        .ToList();
                    entityTypes.AddRange(loadedTypes);
                    scannedCount++;

                    if (loadedTypes.Count > 0)
                    {
                        _editor?.AddLogMessage("Beep",
                            $"  Found {loadedTypes.Count} Entity type(s) in partially-loaded '{asm.GetName().Name}'",
                            DateTime.Now, 0, null, Errors.Warning);
                    }
                }
                catch (Exception ex)
                {
                    _editor?.AddLogMessage("Beep", 
                        $"  Warning: Could not scan assembly '{asm.GetName().Name}': {ex.Message}", 
                        DateTime.Now, 0, null, Errors.Warning);
                }
            }

            var result = entityTypes.Distinct().ToList();

            _editor?.AddLogMessage("Beep",
                $"MigrationManager.DiscoverEntityTypes: Scanned {scannedCount}/{asmList.Count} assemblies, found {result.Count} distinct Entity type(s)",
                DateTime.Now, 0, null, result.Count > 0 ? Errors.Ok : Errors.Warning);

            return result;
        }

        /// <summary>
        /// Discovers all types that inherit from Entity in all searchable assemblies.
        /// Scans registered assemblies, AppDomain, entry assembly references, and DMEEditor's assembly handler.
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
                        var entityStructure = _editor?.classCreator?.ConvertToEntityStructure(entityType);
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
                        var entityStructure = _editor?.classCreator?.ConvertToEntityStructure(entityType);
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
                        var entityStructure = _editor?.classCreator?.ConvertToEntityStructure(entityType);
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
        /// Gets all searchable assemblies from multiple sources:
        /// 1. Manually registered assemblies (RegisterAssembly / RegisterAssemblies)
        /// 2. Entry assembly and all its referenced assemblies (covers projects referenced by the exe)
        /// 3. All loaded assemblies from AppDomain.CurrentDomain
        /// 4. DMEEditor's assembly handler (plugin assemblies)
        /// This ensures entity classes from other projects loaded in the exe are always found.
        /// </summary>
        private IEnumerable<Assembly> GetSearchableAssemblies()
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var assemblies = new List<Assembly>();

            void TryAdd(Assembly asm)
            {
                if (asm == null || asm.IsDynamic) return;
                var name = asm.FullName;
                if (name != null && seen.Add(name))
                {
                    assemblies.Add(asm);
                }
            }

            // 1. Manually registered assemblies — highest priority
            lock (_assemblyLock)
            {
                foreach (var asm in _registeredAssemblies)
                    TryAdd(asm);
            }

            // 2. Entry assembly + all its statically-referenced assemblies
            //    This is the key fix: assemblies from other projects compiled into the exe
            //    may not yet be loaded into the AppDomain (they load on first use).
            //    Walking the entry assembly's references forces them to load.
            try
            {
                var entryAsm = Assembly.GetEntryAssembly();
                if (entryAsm != null)
                {
                    TryAdd(entryAsm);
                    foreach (var referencedName in entryAsm.GetReferencedAssemblies())
                    {
                        try
                        {
                            var refAsm = Assembly.Load(referencedName);
                            TryAdd(refAsm);
                        }
                        catch
                        {
                            // Some references may not be loadable (e.g., platform-specific)
                        }
                    }
                }
            }
            catch
            {
                // Assembly.GetEntryAssembly() can return null in some hosting scenarios
            }

            // 3. Calling assembly and its references (in case MigrationManager is called
            //    from a library that itself references entity assemblies)
            try
            {
                var callingAsm = Assembly.GetCallingAssembly();
                if (callingAsm != null)
                {
                    TryAdd(callingAsm);
                    foreach (var referencedName in callingAsm.GetReferencedAssemblies())
                    {
                        try
                        {
                            var refAsm = Assembly.Load(referencedName);
                            TryAdd(refAsm);
                        }
                        catch { }
                    }
                }
            }
            catch { }

            // 4. All currently loaded assemblies from AppDomain
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!asm.IsDynamic && !string.IsNullOrEmpty(asm.Location))
                    TryAdd(asm);
            }

            // 5. DMEEditor's assembly handler (plugins loaded at runtime)
            if (_editor?.assemblyHandler?.Assemblies != null)
            {
                foreach (var asmInfo in _editor.assemblyHandler.Assemblies)
                {
                    TryAdd(asmInfo.DllLib);
                }
            }

            return assemblies;
        }

        #endregion

        #region Explicit-Type Migration (bypasses discovery)

        /// <summary>
        /// Ensures database is created for the given entity types.
        /// Use this when you know exactly which types to create — bypasses assembly discovery entirely.
        /// This is the most reliable approach for cross-project scenarios where discovery might miss assemblies.
        /// </summary>
        /// <example>
        /// migrationManager.EnsureDatabaseCreatedForTypes(
        ///     new[] { typeof(Customer), typeof(Product), typeof(Invoice) },
        ///     progress: progressReporter);
        /// </example>
        public IErrorsInfo EnsureDatabaseCreatedForTypes(IEnumerable<Type> entityTypes, bool detectRelationships = true, IProgress<PassedArgs> progress = null)
        {
            if (MigrateDataSource == null)
                return CreateErrorsInfo(Errors.Failed, "Migration data source is not set");

            if (entityTypes == null)
                return CreateErrorsInfo(Errors.Failed, "Entity types collection cannot be null");

            var typeList = entityTypes.ToList();
            if (typeList.Count == 0)
                return CreateErrorsInfo(Errors.Warning, "No entity types provided");

            try
            {
                progress?.Report(new PassedArgs { Messege = $"EnsureDatabaseCreatedForTypes: Processing {typeList.Count} explicit type(s)" });

                _editor?.AddLogMessage("Beep",
                    $"MigrationManager.EnsureDatabaseCreatedForTypes: {typeList.Count} type(s): {string.Join(", ", typeList.Select(t => t.Name))}",
                    DateTime.Now, 0, null, Errors.Ok);

                var errors = new List<string>();
                int created = 0;
                int skipped = 0;

                foreach (var entityType in typeList)
                {
                    try
                    {
                        progress?.Report(new PassedArgs { Messege = $"Processing Entity type: {entityType.Name}" });

                        // Convert Entity type to EntityStructure
                        var entityStructure = _editor?.classCreator?.ConvertToEntityStructure(entityType);
                        if (entityStructure == null)
                        {
                            var errMsg = $"Failed to convert {entityType.Name} to EntityStructure (classCreator={(_editor?.classCreator != null ? "available" : "NULL")})";
                            errors.Add(errMsg);
                            progress?.Report(new PassedArgs { Messege = errMsg });
                            continue;
                        }

                        // Use table name from type name or Table attribute if present
                        var tableAttr = entityType.GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.TableAttribute>();
                        if (tableAttr != null && !string.IsNullOrWhiteSpace(tableAttr.Name))
                        {
                            entityStructure.EntityName = tableAttr.Name;
                        }

                        progress?.Report(new PassedArgs { Messege = $"Creating entity '{entityStructure.EntityName}' with {entityStructure.Fields?.Count ?? 0} field(s)" });

                        // Ensure entity exists (create if missing)
                        var result = EnsureEntity(entityStructure, createIfMissing: true, addMissingColumns: false);
                        if (result.Flag == Errors.Ok)
                        {
                            created++;
                            progress?.Report(new PassedArgs { Messege = $"Successfully created entity '{entityStructure.EntityName}'" });
                        }
                        else if (result.Flag == Errors.Warning && result.Message.Contains("already exists"))
                        {
                            skipped++;
                            progress?.Report(new PassedArgs { Messege = $"Entity '{entityStructure.EntityName}' already exists — skipped" });
                        }
                        else
                        {
                            var errMsg = $"{entityType.Name}: {result.Message}";
                            errors.Add(errMsg);
                            progress?.Report(new PassedArgs { Messege = $"Error for '{entityStructure.EntityName}': {result.Message}" });
                        }
                    }
                    catch (Exception ex)
                    {
                        var errMsg = $"{entityType.Name}: {ex.Message}";
                        errors.Add(errMsg);
                        progress?.Report(new PassedArgs { Messege = $"Exception for '{entityType.Name}': {ex.Message}" });
                    }
                }

                var summary = $"Created {created} entity(ies), skipped {skipped} existing";
                if (errors.Count > 0)
                {
                    summary += $", {errors.Count} error(s)";
                    return CreateErrorsInfo(Errors.Failed, $"{summary}. Errors: {string.Join("; ", errors)}");
                }

                progress?.Report(new PassedArgs { Messege = summary });
                return CreateErrorsInfo(Errors.Ok, summary);
            }
            catch (Exception ex)
            {
                return CreateErrorsInfo(Errors.Failed, $"Exception during EnsureDatabaseCreatedForTypes: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Applies migrations for the given entity types.
        /// Use this when you know exactly which types to migrate — bypasses assembly discovery entirely.
        /// </summary>
        public IErrorsInfo ApplyMigrationsForTypes(IEnumerable<Type> entityTypes, bool detectRelationships = true, bool addMissingColumns = true, IProgress<PassedArgs> progress = null)
        {
            if (MigrateDataSource == null)
                return CreateErrorsInfo(Errors.Failed, "Migration data source is not set");

            if (entityTypes == null)
                return CreateErrorsInfo(Errors.Failed, "Entity types collection cannot be null");

            var typeList = entityTypes.ToList();
            if (typeList.Count == 0)
                return CreateErrorsInfo(Errors.Warning, "No entity types provided");

            try
            {
                progress?.Report(new PassedArgs { Messege = $"ApplyMigrationsForTypes: Migrating {typeList.Count} explicit type(s)" });

                _editor?.AddLogMessage("Beep",
                    $"MigrationManager.ApplyMigrationsForTypes: {typeList.Count} type(s): {string.Join(", ", typeList.Select(t => t.Name))}",
                    DateTime.Now, 0, null, Errors.Ok);

                var errors = new List<string>();
                int created = 0;
                int updated = 0;
                int skipped = 0;

                foreach (var entityType in typeList)
                {
                    try
                    {
                        progress?.Report(new PassedArgs { Messege = $"Migrating Entity type: {entityType.Name}" });

                        // Convert Entity type to EntityStructure
                        var entityStructure = _editor?.classCreator?.ConvertToEntityStructure(entityType);
                        if (entityStructure == null)
                        {
                            var errMsg = $"Failed to convert {entityType.Name} to EntityStructure";
                            errors.Add(errMsg);
                            progress?.Report(new PassedArgs { Messege = errMsg });
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
                            {
                                created++;
                                progress?.Report(new PassedArgs { Messege = $"Created entity '{entityStructure.EntityName}'" });
                            }
                            else if (result.Message.Contains("Added") || result.Message.Contains("column"))
                            {
                                updated++;
                                progress?.Report(new PassedArgs { Messege = $"Updated entity '{entityStructure.EntityName}'" });
                            }
                            else
                            {
                                skipped++;
                                progress?.Report(new PassedArgs { Messege = $"Entity '{entityStructure.EntityName}' up to date — skipped" });
                            }
                        }
                        else
                        {
                            var errMsg = $"{entityType.Name}: {result.Message}";
                            errors.Add(errMsg);
                            progress?.Report(new PassedArgs { Messege = $"Error for '{entityStructure.EntityName}': {result.Message}" });
                        }
                    }
                    catch (Exception ex)
                    {
                        var errMsg = $"{entityType.Name}: {ex.Message}";
                        errors.Add(errMsg);
                        progress?.Report(new PassedArgs { Messege = $"Exception for '{entityType.Name}': {ex.Message}" });
                    }
                }

                var summary = $"Created {created} entity(ies), updated {updated} entity(ies), skipped {skipped} unchanged";
                if (errors.Count > 0)
                {
                    summary += $", {errors.Count} error(s)";
                    return CreateErrorsInfo(Errors.Failed, $"{summary}. Errors: {string.Join("; ", errors)}");
                }

                progress?.Report(new PassedArgs { Messege = summary });
                return CreateErrorsInfo(Errors.Ok, summary);
            }
            catch (Exception ex)
            {
                return CreateErrorsInfo(Errors.Failed, $"Exception during ApplyMigrationsForTypes: {ex.Message}", ex);
            }
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
