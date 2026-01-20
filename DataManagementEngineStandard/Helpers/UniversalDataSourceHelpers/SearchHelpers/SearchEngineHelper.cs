using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Core;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Helpers.UniversalDataSourceHelpers.SearchHelpers
{
    /// <summary>
    /// Helper for search engines (Elasticsearch, Solr).
    /// Schema is flexible and mappings are updated dynamically.
    /// </summary>
    public class SearchEngineHelper : IDataSourceHelper
    {
        private readonly IDMEEditor _dmeEditor;

        public SearchEngineHelper(IDMEEditor dmeEditor)
        {
            _dmeEditor = dmeEditor ?? throw new ArgumentNullException(nameof(dmeEditor));
        }

        public DataSourceType SupportedType { get; set; } = DataSourceType.ElasticSearch;
        public string Name => $"SearchEngine ({SupportedType})";
        public DataSourceCapabilities Capabilities => DataSourceCapabilityMatrix.GetCapabilities(SupportedType);

        #region Schema Operations
        public (string Query, bool Success) GetSchemaQuery(string userName) => (string.Empty, false);
        public (string Query, bool Success) GetTableExistsQuery(string tableName) => (string.Empty, false);
        public (string Query, bool Success) GetColumnInfoQuery(string tableName) => (string.Empty, false);
        #endregion

        #region DDL Operations
        public (string Sql, bool Success, string ErrorMessage) GenerateCreateTableSql(EntityStructure entity, string schemaName = null, DataSourceType? dataSourceType = null)
            => (string.Empty, true, "Search indexes are created implicitly");

        public (string Sql, bool Success, string ErrorMessage) GenerateDropTableSql(string tableName, string schemaName = null)
            => (string.Empty, false, "Drop index is not supported by this helper");

        public (string Sql, bool Success, string ErrorMessage) GenerateTruncateTableSql(string tableName, string schemaName = null)
            => (string.Empty, false, "Truncate is not supported by this helper");

        public (string Sql, bool Success, string ErrorMessage) GenerateCreateIndexSql(string tableName, string indexName, string[] columns, Dictionary<string, object> options = null)
            => (string.Empty, true, "Indexes are managed by the search engine");

        public (string Sql, bool Success, string ErrorMessage) GenerateAddColumnSql(string tableName, EntityField column)
        {
            if (string.IsNullOrWhiteSpace(tableName) || column == null)
                return (string.Empty, false, "Index name or column is missing");

            switch (SupportedType)
            {
                case DataSourceType.ElasticSearch:
                    return GenerateElasticAddField(tableName, column);
                case DataSourceType.Solr:
                    return GenerateSolrAddField(column);
                default:
                    return (string.Empty, true, "Search mappings are flexible - no DDL required");
            }
        }

        public (string Sql, bool Success, string ErrorMessage) GenerateAlterColumnSql(string tableName, string columnName, EntityField newColumn)
            => (string.Empty, false, "Altering fields is not supported by this helper");

        public (string Sql, bool Success, string ErrorMessage) GenerateDropColumnSql(string tableName, string columnName)
            => (string.Empty, false, "Dropping fields is not supported by this helper");

        public (string Sql, bool Success, string ErrorMessage) GenerateRenameTableSql(string oldTableName, string newTableName)
            => (string.Empty, false, "Renaming indexes is not supported by this helper");

        public (string Sql, bool Success, string ErrorMessage) GenerateRenameColumnSql(string tableName, string oldColumnName, string newColumnName)
            => (string.Empty, false, "Renaming fields is not supported by this helper");
        #endregion

        private (string Sql, bool Success, string ErrorMessage) GenerateElasticAddField(string indexName, EntityField column)
        {
            var elasticType = MapToElasticType(column);
            var json = $"PUT /{indexName}/_mapping{Environment.NewLine}{{\"properties\":{{\"{column.FieldName}\":{{\"type\":\"{elasticType}\"}}}}}}";
            return (json, true, "Elasticsearch mapping update");
        }

        private (string Sql, bool Success, string ErrorMessage) GenerateSolrAddField(EntityField column)
        {
            var solrType = MapToSolrType(column);
            var json = $"POST /solr/{Environment.NewLine}{{\"add-field\":{{\"name\":\"{column.FieldName}\",\"type\":\"{solrType}\",\"stored\":true,\"indexed\":true}}}}";
            return (json, true, "Solr schema API add-field");
        }

        private string MapToElasticType(EntityField column)
        {
            if (column == null || string.IsNullOrWhiteSpace(column.Fieldtype))
                return "keyword";

            var t = column.Fieldtype.ToLowerInvariant();
            if (t.Contains("int"))
                return "integer";
            if (t.Contains("long"))
                return "long";
            if (t.Contains("double") || t.Contains("float") || t.Contains("decimal") || t.Contains("numeric"))
                return "double";
            if (t.Contains("bool"))
                return "boolean";
            if (t.Contains("date") || t.Contains("time"))
                return "date";
            if (t.Contains("text") || t.Contains("string"))
                return "text";
            return "keyword";
        }

        private string MapToSolrType(EntityField column)
        {
            if (column == null || string.IsNullOrWhiteSpace(column.Fieldtype))
                return "string";

            var t = column.Fieldtype.ToLowerInvariant();
            if (t.Contains("int"))
                return "pint";
            if (t.Contains("long"))
                return "plong";
            if (t.Contains("double") || t.Contains("float") || t.Contains("decimal") || t.Contains("numeric"))
                return "pdouble";
            if (t.Contains("bool"))
                return "boolean";
            if (t.Contains("date") || t.Contains("time"))
                return "pdate";
            if (t.Contains("text"))
                return "text_general";
            return "string";
        }

        #region Constraint Operations
        public (string Sql, bool Success, string ErrorMessage) GenerateAddPrimaryKeySql(string tableName, params string[] columnNames)
            => (string.Empty, false, "Constraints are not supported");
        public (string Sql, bool Success, string ErrorMessage) GenerateAddForeignKeySql(string tableName, string[] columnNames, string referencedTableName, string[] referencedColumnNames)
            => (string.Empty, false, "Constraints are not supported");
        public (string Sql, bool Success, string ErrorMessage) GenerateAddConstraintSql(string tableName, string constraintName, string constraintDefinition)
            => (string.Empty, false, "Constraints are not supported");
        public (string Query, bool Success, string ErrorMessage) GetPrimaryKeyQuery(string tableName)
            => (string.Empty, false, "Constraints are not supported");
        public (string Query, bool Success, string ErrorMessage) GetForeignKeysQuery(string tableName)
            => (string.Empty, false, "Constraints are not supported");
        public (string Query, bool Success, string ErrorMessage) GetConstraintsQuery(string tableName)
            => (string.Empty, false, "Constraints are not supported");
        #endregion

        #region Transaction Control
        public (string Sql, bool Success, string ErrorMessage) GenerateBeginTransactionSql()
            => (string.Empty, false, "Transactions are not supported");
        public (string Sql, bool Success, string ErrorMessage) GenerateCommitSql()
            => (string.Empty, false, "Transactions are not supported");
        public (string Sql, bool Success, string ErrorMessage) GenerateRollbackSql()
            => (string.Empty, false, "Transactions are not supported");
        #endregion

        #region DML Operations
        public (string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage) GenerateInsertSql(string tableName, Dictionary<string, object> data)
            => (string.Empty, new Dictionary<string, object>(), false, "Insert not implemented");
        public (string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage) GenerateUpdateSql(string tableName, Dictionary<string, object> data, Dictionary<string, object> conditions)
            => (string.Empty, new Dictionary<string, object>(), false, "Update not implemented");
        public (string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage) GenerateDeleteSql(string tableName, Dictionary<string, object> conditions)
            => (string.Empty, new Dictionary<string, object>(), false, "Delete not implemented");
        public (string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage) GenerateSelectSql(string tableName, IEnumerable<string> columns = null, Dictionary<string, object> conditions = null, string orderBy = null, int? skip = null, int? take = null)
            => (string.Empty, new Dictionary<string, object>(), false, "Select not implemented");
        #endregion

        #region Utility Methods
        public string QuoteIdentifier(string identifier) => identifier;
        public string MapClrTypeToDatasourceType(Type clrType, int? size = null, int? precision = null, int? scale = null) => "string";
        public Type MapDatasourceTypeToClrType(string datasourceType) => typeof(string);
        public (bool IsValid, List<string> Errors) ValidateEntity(EntityStructure entity) => (entity != null, new List<string>());
        public bool SupportsCapability(CapabilityType capability) => Capabilities.IsCapable(capability);
        public int GetMaxStringSize() => -1;
        public int GetMaxNumericPrecision() => 0;
        #endregion
    }
}
