using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Core;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Helpers.UniversalDataSourceHelpers.Core
{
    /// <summary>
    /// Default helper for datasource types without a dedicated implementation.
    /// Uses capability matrix to determine supported operations.
    /// </summary>
    public class DefaultDataSourceHelper : IDataSourceHelper
    {
        private readonly IDMEEditor _dmeEditor;

        public DefaultDataSourceHelper(IDMEEditor dmeEditor)
        {
            _dmeEditor = dmeEditor ?? throw new ArgumentNullException(nameof(dmeEditor));
        }

        public DataSourceType SupportedType { get; set; } = DataSourceType.Unknown;
        public string Name => $"Default ({SupportedType})";
        public DataSourceCapabilities Capabilities => DataSourceCapabilityMatrix.GetCapabilities(SupportedType);

        #region Schema Operations

        public (string Query, bool Success) GetSchemaQuery(string userName) => (string.Empty, false);
        public (string Query, bool Success) GetTableExistsQuery(string tableName) => (string.Empty, false);
        public (string Query, bool Success) GetColumnInfoQuery(string tableName) => (string.Empty, false);

        #endregion

        #region DDL Operations

        public (string Sql, bool Success, string ErrorMessage) GenerateCreateTableSql(EntityStructure entity, string schemaName = null, DataSourceType? dataSourceType = null)
            => GenerateUnsupportedOrNoOp("CREATE TABLE", operationSupported: Capabilities.SupportsSchemaEvolution);

        public (string Sql, bool Success, string ErrorMessage) GenerateDropTableSql(string tableName, string schemaName = null)
            => GenerateUnsupportedOrNoOp("DROP TABLE", operationSupported: Capabilities.SupportsSchemaEvolution && Capabilities.IsSchemaEnforced);

        public (string Sql, bool Success, string ErrorMessage) GenerateTruncateTableSql(string tableName, string schemaName = null)
            => GenerateUnsupportedOrNoOp("TRUNCATE TABLE", operationSupported: Capabilities.SupportsSchemaEvolution && Capabilities.IsSchemaEnforced);

        public (string Sql, bool Success, string ErrorMessage) GenerateCreateIndexSql(string tableName, string indexName, string[] columns, Dictionary<string, object> options = null)
            => GenerateUnsupportedOrNoOp("CREATE INDEX", operationSupported: Capabilities.SupportsIndexes);

        public (string Sql, bool Success, string ErrorMessage) GenerateAddColumnSql(string tableName, EntityField column)
            => GenerateUnsupportedOrNoOp("ADD COLUMN", operationSupported: Capabilities.SupportsSchemaEvolution);

        public (string Sql, bool Success, string ErrorMessage) GenerateAlterColumnSql(string tableName, string columnName, EntityField newColumn)
            => GenerateUnsupportedOrNoOp("ALTER COLUMN", operationSupported: SupportsAlterOrRenameOperations());

        public (string Sql, bool Success, string ErrorMessage) GenerateDropColumnSql(string tableName, string columnName)
            => GenerateUnsupportedOrNoOp("DROP COLUMN", operationSupported: SupportsAlterOrRenameOperations());

        public (string Sql, bool Success, string ErrorMessage) GenerateRenameTableSql(string oldTableName, string newTableName)
            => GenerateUnsupportedOrNoOp("RENAME TABLE", operationSupported: SupportsAlterOrRenameOperations());

        public (string Sql, bool Success, string ErrorMessage) GenerateRenameColumnSql(string tableName, string oldColumnName, string newColumnName)
            => GenerateUnsupportedOrNoOp("RENAME COLUMN", operationSupported: SupportsAlterOrRenameOperations());

        #endregion

        #region Constraint Operations

        public (string Sql, bool Success, string ErrorMessage) GenerateAddPrimaryKeySql(string tableName, params string[] columnNames)
            => (string.Empty, false, "Primary key constraints are not supported for this datasource type");

        public (string Sql, bool Success, string ErrorMessage) GenerateAddForeignKeySql(string tableName, string[] columnNames, string referencedTableName, string[] referencedColumnNames)
            => (string.Empty, false, "Foreign key constraints are not supported for this datasource type");

        public (string Sql, bool Success, string ErrorMessage) GenerateAddConstraintSql(string tableName, string constraintName, string constraintDefinition)
            => (string.Empty, false, "Constraints are not supported for this datasource type");

        public (string Query, bool Success, string ErrorMessage) GetPrimaryKeyQuery(string tableName)
            => (string.Empty, false, "Primary key metadata is not supported for this datasource type");

        public (string Query, bool Success, string ErrorMessage) GetForeignKeysQuery(string tableName)
            => (string.Empty, false, "Foreign key metadata is not supported for this datasource type");

        public (string Query, bool Success, string ErrorMessage) GetConstraintsQuery(string tableName)
            => (string.Empty, false, "Constraint metadata is not supported for this datasource type");

        #endregion

        #region Transaction Control

        public (string Sql, bool Success, string ErrorMessage) GenerateBeginTransactionSql()
            => Capabilities.SupportsTransactions
                ? (string.Empty, true, "Datasource manages transaction boundaries without generated SQL")
                : (string.Empty, false, "Transactions are not supported for this datasource type");

        public (string Sql, bool Success, string ErrorMessage) GenerateCommitSql()
            => Capabilities.SupportsTransactions
                ? (string.Empty, true, "Datasource manages transaction boundaries without generated SQL")
                : (string.Empty, false, "Transactions are not supported for this datasource type");

        public (string Sql, bool Success, string ErrorMessage) GenerateRollbackSql()
            => Capabilities.SupportsTransactions
                ? (string.Empty, true, "Datasource manages transaction boundaries without generated SQL")
                : (string.Empty, false, "Transactions are not supported for this datasource type");

        #endregion

        #region DML Operations

        public (string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage) GenerateInsertSql(string tableName, Dictionary<string, object> data)
            => (string.Empty, new Dictionary<string, object>(), false, "Insert is not supported for this datasource type");

        public (string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage) GenerateUpdateSql(string tableName, Dictionary<string, object> data, Dictionary<string, object> conditions)
            => (string.Empty, new Dictionary<string, object>(), false, "Update is not supported for this datasource type");

        public (string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage) GenerateDeleteSql(string tableName, Dictionary<string, object> conditions)
            => (string.Empty, new Dictionary<string, object>(), false, "Delete is not supported for this datasource type");

        public (string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage) GenerateSelectSql(string tableName, IEnumerable<string> columns = null, Dictionary<string, object> conditions = null, string orderBy = null, int? skip = null, int? take = null)
            => (string.Empty, new Dictionary<string, object>(), false, "Select is not supported for this datasource type");

        #endregion

        #region Utility Methods

        public string QuoteIdentifier(string identifier) => identifier;

        public string MapClrTypeToDatasourceType(Type clrType, int? size = null, int? precision = null, int? scale = null)
            => "string";

        public Type MapDatasourceTypeToClrType(string datasourceType) => typeof(string);

        public (bool IsValid, List<string> Errors) ValidateEntity(EntityStructure entity)
        {
            var errors = new List<string>();
            if (entity == null)
            {
                errors.Add("Entity cannot be null");
                return (false, errors);
            }

            if (string.IsNullOrWhiteSpace(entity.EntityName))
                errors.Add("Entity name cannot be empty");

            return (errors.Count == 0, errors);
        }

        public bool SupportsCapability(CapabilityType capability) => Capabilities.IsCapable(capability);

        public int GetMaxStringSize() => -1;

        public int GetMaxNumericPrecision() => 0;

        #endregion

        private bool SupportsAlterOrRenameOperations()
        {
            if (!Capabilities.SupportsSchemaEvolution)
                return false;

            // Conservative default:
            // rename/alter operations are primarily safe to assume for schema-enforced RDBMS-like stores.
            return Capabilities.IsSchemaEnforced && SupportedType != DataSourceType.FlatFile &&
                   SupportedType != DataSourceType.CSV &&
                   SupportedType != DataSourceType.TSV &&
                   SupportedType != DataSourceType.Text &&
                   SupportedType != DataSourceType.Json &&
                   SupportedType != DataSourceType.XML &&
                   SupportedType != DataSourceType.YAML;
        }

        private (string Sql, bool Success, string ErrorMessage) GenerateUnsupportedOrNoOp(string operation, bool operationSupported)
        {
            if (!Capabilities.IsSchemaEnforced)
                return (string.Empty, true, "Datasource is schema-flexible - no DDL required");

            if (!Capabilities.SupportsSchemaEvolution || !operationSupported)
                return (string.Empty, false, $"Datasource does not support {operation}");

            return (string.Empty, false, $"No DDL generator available for {operation} on this datasource type. Use provider-specific helper or fallback migration strategy.");
        }
    }
}
