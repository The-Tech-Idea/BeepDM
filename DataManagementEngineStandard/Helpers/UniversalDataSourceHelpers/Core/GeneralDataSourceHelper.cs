using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Helpers.UniversalDataSourceHelpers.Core
{
    /// <summary>
    /// General implementation of IDataSourceHelper that delegates to specific helpers based on datasource type.
    /// </summary>
    public class GeneralDataSourceHelper : IDataSourceHelper
    {
        private IDataSourceHelper _currentHelper;

        public GeneralDataSourceHelper(DataSourceType dataSourceType)
        {
            SupportedType = dataSourceType;
            _currentHelper = DataSourceHelperFactory.CreateHelper(dataSourceType);
        }

        public DataSourceType SupportedType { get; set; }
        public string Name => _currentHelper?.Name ?? "General";
        public DataSourceCapabilities Capabilities => _currentHelper?.Capabilities;

        #region Schema Operations
        public (string Query, bool Success) GetSchemaQuery(string userName) => _currentHelper.GetSchemaQuery(userName);
        public (string Query, bool Success) GetTableExistsQuery(string tableName) => _currentHelper.GetTableExistsQuery(tableName);
        public (string Query, bool Success) GetColumnInfoQuery(string tableName) => _currentHelper.GetColumnInfoQuery(tableName);
        #endregion

        #region DDL Operations
        public (string Sql, bool Success, string ErrorMessage) GenerateCreateTableSql(EntityStructure entity, string schemaName = null, DataSourceType? dataSourceType = null) 
            => _currentHelper.GenerateCreateTableSql(entity, schemaName, dataSourceType);
        
        public (string Sql, bool Success, string ErrorMessage) GenerateDropTableSql(string tableName, string schemaName = null) 
            => _currentHelper.GenerateDropTableSql(tableName, schemaName);
        
        public (string Sql, bool Success, string ErrorMessage) GenerateTruncateTableSql(string tableName, string schemaName = null) 
            => _currentHelper.GenerateTruncateTableSql(tableName, schemaName);
        
        public (string Sql, bool Success, string ErrorMessage) GenerateCreateIndexSql(string tableName, string indexName, string[] columns, Dictionary<string, object> options = null) 
            => _currentHelper.GenerateCreateIndexSql(tableName, indexName, columns, options);
        
        public (string Sql, bool Success, string ErrorMessage) GenerateAddColumnSql(string tableName, EntityField column) 
            => _currentHelper.GenerateAddColumnSql(tableName, column);
        
        public (string Sql, bool Success, string ErrorMessage) GenerateAlterColumnSql(string tableName, string columnName, EntityField newColumn) 
            => _currentHelper.GenerateAlterColumnSql(tableName, columnName, newColumn);
        
        public (string Sql, bool Success, string ErrorMessage) GenerateDropColumnSql(string tableName, string columnName) 
            => _currentHelper.GenerateDropColumnSql(tableName, columnName);
        
        public (string Sql, bool Success, string ErrorMessage) GenerateRenameTableSql(string oldTableName, string newTableName) 
            => _currentHelper.GenerateRenameTableSql(oldTableName, newTableName);
        
        public (string Sql, bool Success, string ErrorMessage) GenerateRenameColumnSql(string tableName, string oldColumnName, string newColumnName) 
            => _currentHelper.GenerateRenameColumnSql(tableName, oldColumnName, newColumnName);
        #endregion

        #region Constraint Operations
        public (string Sql, bool Success, string ErrorMessage) GenerateAddPrimaryKeySql(string tableName, params string[] columnNames) 
            => _currentHelper.GenerateAddPrimaryKeySql(tableName, columnNames);
        
        public (string Sql, bool Success, string ErrorMessage) GenerateAddForeignKeySql(string tableName, string[] columnNames, string referencedTableName, string[] referencedColumnNames) 
            => _currentHelper.GenerateAddForeignKeySql(tableName, columnNames, referencedTableName, referencedColumnNames);
        
        public (string Sql, bool Success, string ErrorMessage) GenerateAddConstraintSql(string tableName, string constraintName, string constraintDefinition) 
            => _currentHelper.GenerateAddConstraintSql(tableName, constraintName, constraintDefinition);
        
        public (string Query, bool Success, string ErrorMessage) GetPrimaryKeyQuery(string tableName) 
            => _currentHelper.GetPrimaryKeyQuery(tableName);
        
        public (string Query, bool Success, string ErrorMessage) GetForeignKeysQuery(string tableName) 
            => _currentHelper.GetForeignKeysQuery(tableName);
        
        public (string Query, bool Success, string ErrorMessage) GetConstraintsQuery(string tableName) 
            => _currentHelper.GetConstraintsQuery(tableName);
        #endregion

        #region Transaction Control
        public (string Sql, bool Success, string ErrorMessage) GenerateBeginTransactionSql() => _currentHelper.GenerateBeginTransactionSql();
        public (string Sql, bool Success, string ErrorMessage) GenerateCommitSql() => _currentHelper.GenerateCommitSql();
        public (string Sql, bool Success, string ErrorMessage) GenerateRollbackSql() => _currentHelper.GenerateRollbackSql();
        #endregion

        #region DML Operations
        public (string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage) GenerateInsertSql(string tableName, Dictionary<string, object> data) 
            => _currentHelper.GenerateInsertSql(tableName, data);
        
        public (string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage) GenerateUpdateSql(string tableName, Dictionary<string, object> data, Dictionary<string, object> conditions) 
            => _currentHelper.GenerateUpdateSql(tableName, data, conditions);
        
        public (string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage) GenerateDeleteSql(string tableName, Dictionary<string, object> conditions) 
            => _currentHelper.GenerateDeleteSql(tableName, conditions);
        
        public (string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage) GenerateSelectSql(string tableName, IEnumerable<string> columns = null, Dictionary<string, object> conditions = null, string orderBy = null, int? skip = null, int? take = null) 
            => _currentHelper.GenerateSelectSql(tableName, columns, conditions, orderBy, skip, take);
        #endregion

        #region Utility Methods
        public string QuoteIdentifier(string identifier) => _currentHelper.QuoteIdentifier(identifier);
        public string MapClrTypeToDatasourceType(Type clrType, int? size = null, int? precision = null, int? scale = null) 
            => _currentHelper.MapClrTypeToDatasourceType(clrType, size, precision, scale);
        public Type MapDatasourceTypeToClrType(string datasourceType) => _currentHelper.MapDatasourceTypeToClrType(datasourceType);
        public (bool IsValid, List<string> Errors) ValidateEntity(EntityStructure entity) => _currentHelper.ValidateEntity(entity);
        public bool SupportsCapability(CapabilityType capability) => _currentHelper.SupportsCapability(capability);
        public int GetMaxStringSize() => _currentHelper.GetMaxStringSize();
        public int GetMaxNumericPrecision() => _currentHelper.GetMaxNumericPrecision();
        #endregion
    }
}
