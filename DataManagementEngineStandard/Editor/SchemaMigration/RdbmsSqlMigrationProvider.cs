using System;
using System.Collections.Generic;
using System.Reflection;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.SchemaMigration
{
    /// <summary>
    /// Tier-2 fallback for <see cref="DatasourceCategory.RDBMS"/> (and SQL-speaking cloud engines).
    /// Behavior-preserving: it reproduces exactly what <c>MigrationManager.EntityOperations</c>
    /// did before the Phase 10 rewire — generate SQL via <see cref="IDataSourceHelper"/> and run it
    /// through <see cref="IDataSource.ExecuteSql"/>. RDBMS subclass overrides (SQLite table-rebuild,
    /// Oracle windows) live in their datasource folders and derive from this class.
    /// </summary>
    public class RdbmsSqlMigrationProvider : ISchemaMigrationProvider
    {
        private readonly IDataSource _owner;
        private IDataSourceHelper _helper;

        public RdbmsSqlMigrationProvider(IDataSource owner)
        {
            _owner = owner;
        }

        public virtual DataSourceType DataSourceType => _owner.DatasourceType;
        public virtual DatasourceCategory Category => DatasourceCategory.RDBMS;

        /// <summary>Resolved lazily from <see cref="IDataSource.DMEEditor"/> and cached.</summary>
        protected IDataSourceHelper Helper
        {
            get
            {
                if (_helper == null)
                    _helper = _owner?.DMEEditor?.GetDataSourceHelper(_owner.DatasourceType);
                return _helper;
            }
        }

        public virtual SchemaMigrationCapabilities Capabilities { get; } = new SchemaMigrationCapabilities
        {
            SupportsCreateEntity = true,
            SupportsAddColumn = true,
            SupportsAlterColumn = true,
            SupportsDropColumn = true,
            SupportsRenameColumn = true,
            SupportsRenameEntity = true,
            SupportsDropEntity = true,
            SupportsTruncateEntity = true,
            SupportsCreateIndex = true,
            SupportsDropIndex = true,
            SupportsAddForeignKey = true,
            SupportsDropForeignKey = true,
            SupportsTransactionalDdl = true
        };

        // ── entity lifecycle ──────────────────────────────────────────────

        public virtual IErrorsInfo CreateEntity(EntityStructure entity)
        {
            // Mirrors the pre-Phase-10 create path: the datasource owns type mapping.
            var created = _owner.CreateEntityAs(entity);
            if (!created && _owner.ErrorObject != null)
                return SchemaMigrationResults.Fail(_owner.ErrorObject.Message ?? $"Failed to create entity '{entity?.EntityName}'.", _owner.ErrorObject.Ex);
            return created
                ? SchemaMigrationResults.Ok($"Entity '{entity?.EntityName}' created.")
                : SchemaMigrationResults.Fail($"Failed to create entity '{entity?.EntityName}'.");
        }

        public virtual IErrorsInfo DropEntity(string entityName)
            => RunHelper("DropEntity", entityName, null, () => Helper.GenerateDropTableSql(entityName));

        public virtual IErrorsInfo TruncateEntity(string entityName)
            => RunHelper("TruncateEntity", entityName, null, () => Helper.GenerateTruncateTableSql(entityName));

        public virtual IErrorsInfo RenameEntity(string oldName, string newName)
        {
            var (sql, ok, err) = Helper.GenerateRenameTableSql(oldName, newName);
            return ok
                ? ExecuteOrNoOp(sql, "RenameEntity", $"{oldName}->{newName}")
                : SchemaMigrationResults.Fail(err);
        }

        // ── column ops ────────────────────────────────────────────────────

        public virtual IErrorsInfo AddColumn(string entityName, EntityField column)
        {
            var (sql, ok, err) = Helper.GenerateAddColumnSql(entityName, column);
            if (!ok) return SchemaMigrationResults.Fail(err);
            if (string.IsNullOrWhiteSpace(sql)) return SchemaMigrationResults.Ok("No DDL required.");
            var r = _owner.ExecuteSql(sql);
            return r?.Flag == Errors.Ok ? SchemaMigrationResults.Ok($"Added column '{column?.FieldName}'.") : SchemaMigrationResults.Fail(r?.Message ?? "AddColumn failed.", r?.Ex);
        }

        public virtual IErrorsInfo AlterColumn(string entityName, string columnName, EntityField newColumn)
        {
            var (sql, ok, err) = Helper.GenerateAlterColumnSql(entityName, columnName, newColumn);
            return ok
                ? ExecuteOrNoOp(sql, "AlterColumn", $"{entityName}.{columnName}")
                : SchemaMigrationResults.Fail(err);
        }

        public virtual IErrorsInfo DropColumn(string entityName, string columnName)
        {
            var (sql, ok, err) = Helper.GenerateDropColumnSql(entityName, columnName);
            return ok
                ? ExecuteOrNoOp(sql, "DropColumn", $"{entityName}.{columnName}")
                : SchemaMigrationResults.Fail(err);
        }

        public virtual IErrorsInfo RenameColumn(string entityName, string oldColumnName, string newColumnName)
        {
            var (sql, ok, err) = Helper.GenerateRenameColumnSql(entityName, oldColumnName, newColumnName);
            return ok
                ? ExecuteOrNoOp(sql, "RenameColumn", $"{entityName}.{oldColumnName}->{newColumnName}")
                : SchemaMigrationResults.Fail(err);
        }

        // ── indexes ───────────────────────────────────────────────────────

        public virtual IErrorsInfo CreateIndex(string entityName, string indexName, string[] columns, Dictionary<string, object> options = null)
        {
            var (sql, ok, err) = Helper.GenerateCreateIndexSql(entityName, indexName, columns, options);
            return ok
                ? ExecuteOrNoOp(sql, "CreateIndex", $"{entityName}.{indexName}")
                : SchemaMigrationResults.Fail(err);
        }

        public virtual IErrorsInfo DropIndex(string entityName, string indexName)
        {
            // IDataSourceHelper has no DropIndex generator; mirror the manager's reflection
            // fallback against the concrete helper (RdbmsHelper exposes GenerateDropIndexSql).
            var (sql, ok, err) = InvokeHelperReturningSql("GenerateDropIndexSql", entityName, indexName);
            if (!ok) return SchemaMigrationResults.Fail(err);
            return ExecuteOrNoOp(sql, "DropIndex", $"{entityName}.{indexName}");
        }

        // ── foreign keys ──────────────────────────────────────────────────

        public virtual IErrorsInfo AddForeignKey(string entityName, string[] columnNames, string referencedEntityName, string[] referencedColumnNames, string onDeleteBehavior, string onUpdateBehavior, string constraintName)
        {
            // Try the rich 7-arg overload first (RdbmsHelper), then the 4-arg IDataSourceHelper one.
            var rich = InvokeHelperReturningSql("GenerateAddForeignKeySql",
                new object[] { entityName, columnNames, referencedEntityName, referencedColumnNames, onDeleteBehavior, onUpdateBehavior, constraintName });
            if (rich.Success) return ExecuteOrNoOp(rich.Sql, "AddForeignKey", constraintName);

            var (sql, ok, err) = Helper.GenerateAddForeignKeySql(entityName, columnNames, referencedEntityName, referencedColumnNames);
            if (!ok) return SchemaMigrationResults.Fail(err);
            return ExecuteOrNoOp(sql, "AddForeignKey", constraintName);
        }

        public virtual IErrorsInfo DropForeignKey(string entityName, string constraintName)
        {
            var (sql, ok, err) = InvokeHelperReturningSql("GenerateDropForeignKeySql", entityName, constraintName);
            if (!ok) return SchemaMigrationResults.Fail(err);
            return ExecuteOrNoOp(sql, "DropForeignKey", $"{entityName}.{constraintName}");
        }

        // ── shared plumbing ───────────────────────────────────────────────

        /// <summary>Generate-via-helper → execute, centralizing the "empty SQL = NoOp" rule.</summary>
        private IErrorsInfo RunHelper(string op, string entityName, string detail, System.Func<(string Sql, bool Success, string ErrorMessage)> generate)
        {
            if (Helper == null) return SchemaMigrationResults.Unsupported(op, DataSourceType);
            var (sql, ok, err) = generate();
            if (!ok) return SchemaMigrationResults.Fail(err);
            return ExecuteOrNoOp(sql, op, detail ?? entityName);
        }

        private IErrorsInfo ExecuteOrNoOp(string sql, string op, string target)
        {
            if (string.IsNullOrWhiteSpace(sql))
                return SchemaMigrationResults.Ok("No DDL required.");
            var r = _owner.ExecuteSql(sql);
            return r?.Flag == Errors.Ok
                ? SchemaMigrationResults.Ok($"{op} executed: {target}.")
                : SchemaMigrationResults.Fail(r?.Message ?? $"{op} failed.", r?.Ex);
        }

        private (string Sql, bool Success, string ErrorMessage) InvokeHelperReturningSql(string methodName, params object[] args)
        {
            var helper = Helper;
            if (helper == null) return ("", false, "No helper available.");
            try
            {
                var types = new System.Type[args.Length];
                for (int i = 0; i < args.Length; i++) types[i] = args[i]?.GetType() ?? typeof(object);
                var mi = helper.GetType().GetMethod(methodName, types);
                if (mi == null) return ("", false, $"Helper does not support {methodName}.");
                var result = mi.Invoke(helper, args);
                if (result is ValueTuple<string, bool, string> t) return t;
                return ("", false, $"Helper returned unexpected type for {methodName}.");
            }
            catch (System.Exception ex)
            {
                return ("", false, ex.InnerException?.Message ?? ex.Message);
            }
        }
    }
}
