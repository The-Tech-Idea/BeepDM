using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Helpers.FileandFolderHelpers;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.SchemaMigration
{
    /// <summary>
    /// Tier-2 fallback for <see cref="DatasourceCategory.FILE"/> (delimited text / spreadsheet
    /// sources such as CSV, TSV, XLS). Replicates the file-mutation behavior previously inlined
    /// in <c>MigrationManager.EntityOperations</c>: structural changes are applied by mutating the
    /// backing file (header rewrite, <see cref="FileHelper"/>, etc.). Alter-column, indexes and
    /// foreign keys are not supported for file sources.
    /// </summary>
    public class FileMutationMigrationProvider : ISchemaMigrationProvider
    {
        private readonly IDataSource _owner;

        public FileMutationMigrationProvider(IDataSource owner)
        {
            _owner = owner;
        }

        public DataSourceType DataSourceType => _owner.DatasourceType;
        public DatasourceCategory Category => DatasourceCategory.FILE;

        public SchemaMigrationCapabilities Capabilities { get; } = new SchemaMigrationCapabilities
        {
            SupportsCreateEntity = true,
            SupportsAddColumn = true,
            SupportsDropColumn = true,
            SupportsRenameColumn = true,
            SupportsRenameEntity = true,
            SupportsDropEntity = true,
            SupportsTruncateEntity = true,
            // AlterColumn / CreateIndex / DropIndex / AddFK / DropFK: not supported for files
            IsReadOnly = false
        };

        public IErrorsInfo CreateEntity(EntityStructure entity)
        {
            // File creation mirrors the datasource-native path (CreateEntityAs handles the file).
            var created = _owner.CreateEntityAs(entity);
            return created
                ? SchemaMigrationResults.Ok($"File entity '{entity?.EntityName}' created.")
                : SchemaMigrationResults.Fail(_owner.ErrorObject?.Message ?? $"Failed to create file entity '{entity?.EntityName}'.", _owner.ErrorObject?.Ex);
        }

        public IErrorsInfo AddColumn(string entityName, EntityField column)
        {
            var filePath = ResolveFilePath();
            if (string.IsNullOrWhiteSpace(filePath)) return MissingPath();
            var ok = FileHelper.AddColumnToFile(filePath, column?.FieldName, column?.DefaultValue ?? string.Empty);
            return ok
                ? SchemaMigrationResults.Ok($"Added column '{column?.FieldName}' to file.")
                : SchemaMigrationResults.Fail($"Failed to add column '{column?.FieldName}' to file.");
        }

        public IErrorsInfo DropColumn(string entityName, string columnName)
        {
            var filePath = ResolveFilePath();
            if (string.IsNullOrWhiteSpace(filePath)) return MissingPath();
            if (!File.Exists(filePath)) return NotFound(filePath);

            var updated = new EntityStructure(entityName) { Fields = new List<EntityField> { new EntityField { FieldName = columnName } } };
            var ok = FileHelper.UpdateFileStructure(_owner?.DMEEditor, updated, filePath, addColumn: false);
            return ok
                ? SchemaMigrationResults.Ok($"Removed column '{columnName}' from file.")
                : SchemaMigrationResults.Fail($"Failed to remove column '{columnName}' from file.");
        }

        public IErrorsInfo RenameColumn(string entityName, string oldColumnName, string newColumnName)
        {
            var filePath = ResolveFilePath();
            if (string.IsNullOrWhiteSpace(filePath)) return MissingPath();
            if (!File.Exists(filePath)) return NotFound(filePath);

            var delimiter = ResolveDelimiter();
            var lines = File.ReadAllLines(filePath).ToList();
            if (lines.Count == 0) return SchemaMigrationResults.Fail("File has no header row.");

            var headers = lines[0].Split(delimiter);
            var idx = Array.FindIndex(headers, h => string.Equals(h, oldColumnName, StringComparison.OrdinalIgnoreCase));
            if (idx < 0) return SchemaMigrationResults.Fail($"Column '{oldColumnName}' not found.");

            headers[idx] = newColumnName;
            lines[0] = string.Join(delimiter, headers);
            File.WriteAllLines(filePath, lines);
            return SchemaMigrationResults.Ok($"Renamed column '{oldColumnName}' to '{newColumnName}'.");
        }

        public IErrorsInfo RenameEntity(string oldName, string newName)
        {
            var filePath = ResolveFilePath();
            if (string.IsNullOrWhiteSpace(filePath)) return MissingPath();
            if (!File.Exists(filePath)) return NotFound(filePath);

            var dir = Path.GetDirectoryName(filePath);
            var newPath = Path.Combine(dir ?? string.Empty, newName);
            File.Move(filePath, newPath);
            return SchemaMigrationResults.Ok($"File renamed to '{newPath}'.");
        }

        public IErrorsInfo DropEntity(string entityName)
        {
            var filePath = ResolveFilePath();
            if (string.IsNullOrWhiteSpace(filePath)) return MissingPath();
            if (!File.Exists(filePath)) return NotFound(filePath);
            File.Delete(filePath);
            return SchemaMigrationResults.Ok($"File '{filePath}' deleted.");
        }

        public IErrorsInfo TruncateEntity(string entityName)
        {
            var filePath = ResolveFilePath();
            if (string.IsNullOrWhiteSpace(filePath)) return MissingPath();
            if (!File.Exists(filePath)) return NotFound(filePath);

            var lines = File.ReadAllLines(filePath);
            var header = lines.Length > 0 ? lines[0] : string.Empty;
            File.WriteAllText(filePath, string.IsNullOrEmpty(header) ? string.Empty : header + Environment.NewLine);
            return SchemaMigrationResults.Ok($"File '{filePath}' truncated.");
        }

        public IErrorsInfo AlterColumn(string entityName, string columnName, EntityField newColumn)
            => SchemaMigrationResults.Unsupported(nameof(AlterColumn), DataSourceType);
        public IErrorsInfo CreateIndex(string entityName, string indexName, string[] columns, Dictionary<string, object> options = null)
            => SchemaMigrationResults.Unsupported(nameof(CreateIndex), DataSourceType);
        public IErrorsInfo DropIndex(string entityName, string indexName)
            => SchemaMigrationResults.Unsupported(nameof(DropIndex), DataSourceType);
        public IErrorsInfo AddForeignKey(string entityName, string[] columnNames, string referencedEntityName, string[] referencedColumnNames, string onDeleteBehavior, string onUpdateBehavior, string constraintName)
            => SchemaMigrationResults.Unsupported(nameof(AddForeignKey), DataSourceType);
        public IErrorsInfo DropForeignKey(string entityName, string constraintName)
            => SchemaMigrationResults.Unsupported(nameof(DropForeignKey), DataSourceType);

        // ── file-path / delimiter resolution (mirrors the old MigrationManager helpers) ──

        private string ResolveFilePath()
        {
            var props = _owner?.Dataconnection?.ConnectionProp;
            if (props == null) return null;
            if (!string.IsNullOrWhiteSpace(props.FilePath) && !string.IsNullOrWhiteSpace(props.FileName))
                return Path.Combine(props.FilePath, props.FileName);
            return props.FilePath;
        }

        private char ResolveDelimiter()
        {
            var props = _owner?.Dataconnection?.ConnectionProp;
            if (props != null && props.Delimiter != default(char))
                return props.Delimiter;
            if (!string.IsNullOrWhiteSpace(_owner?.ColumnDelimiter))
                return _owner.ColumnDelimiter[0];
            return ',';
        }

        private static IErrorsInfo MissingPath() => SchemaMigrationResults.Fail("File path is missing for file-based datasource.");
        private static IErrorsInfo NotFound(string path) => SchemaMigrationResults.Fail($"File '{path}' does not exist.");
    }
}
