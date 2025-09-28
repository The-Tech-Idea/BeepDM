using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Helpers.RDBMSHelpers;

namespace TheTechIdea.Beep.ConfigUtil.Managers
{
    /// <summary>
    /// Manages SQL queries and query repositories with enhanced capabilities through RDBMSHelper integration
    /// </summary>
    public class QueryManager
    {
        private readonly IDMLogger _logger;
        private readonly IJsonLoader _jsonLoader;
        private readonly string _configPath;

        public List<QuerySqlRepo> QueryList { get; set; }

        public QueryManager(IDMLogger logger, IJsonLoader jsonLoader, string configPath)
        {
            _logger = logger;
            _jsonLoader = jsonLoader;
            _configPath = configPath;
            QueryList = new List<QuerySqlRepo>();
        }

        /// <summary>
        /// Generates a SQL query based on the specified parameters using RDBMSHelper for enhanced query generation
        /// </summary>
        public string GetSql(Sqlcommandtype cmdType, string tableName, string schemaName,
            string filterParameters, DataSourceType databaseType)
        {
            // First try to get from RDBMSHelper's enhanced query cache
            var helperQuery = RDBMSHelper.GetQuery(databaseType, cmdType);
            if (!string.IsNullOrEmpty(helperQuery))
            {
                return string.Format(helperQuery, tableName, schemaName, filterParameters);
            }

            // Fallback to existing implementation
            var sql = QueryList
                .Where(a => a.DatabaseType == databaseType && a.Sqltype == cmdType)
                .Select(a => a.Sql)
                .FirstOrDefault();

            return sql == null ? "" : string.Format(sql, tableName, schemaName, filterParameters);
        }

        /// <summary>
        /// Retrieves a list of SQL queries based on the specified parameters.
        /// </summary>
        public List<string> GetSqlList(Sqlcommandtype cmdType, string tableName, string schemaName,
            string filterParameters, DataSourceType databaseType)
        {
            var queries = QueryList
                .Where(a => a.DatabaseType == databaseType && a.Sqltype == cmdType)
                .Select(a => a.Sql)
                .ToList();

            var result = new List<string>();
            foreach (var query in queries)
            {
                result.Add(string.Format(query, tableName, schemaName, filterParameters));
            }

            return result;
        }

        /// <summary>
        /// Gets the SQL statement from a custom query.
        /// </summary>
        public string GetSqlFromCustomQuery(Sqlcommandtype cmdType, string tableName,
            string customQuery, DataSourceType databaseType)
        {
            var sql = QueryList
                .Where(a => a.DatabaseType == databaseType && a.Sqltype == cmdType)
                .Select(a => a.Sql)
                .FirstOrDefault();

            return sql == null ? "" : string.Format(sql, tableName);
        }

        /// <summary>
        /// Gets the query for fetching schemas or databases that the specified user has access to using RDBMSHelper
        /// </summary>
        /// <param name="databaseType">The type of database system</param>
        /// <param name="userName">The username to check privileges for</param>
        /// <returns>A SQL query string to retrieve accessible schemas or databases</returns>
        public string GetSchemaOrDatabasesQuery(DataSourceType databaseType, string userName = null)
        {
            try
            {
                return RDBMSHelper.GetSchemasorDatabases(databaseType, userName);
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error getting schema/database query: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets the query for fetching schemas or databases with built-in error handling using RDBMSHelper
        /// </summary>
        /// <param name="databaseType">The type of database system</param>
        /// <param name="userName">The username to check privileges for</param>
        /// <param name="throwOnError">Whether to throw exceptions for errors</param>
        /// <returns>A tuple containing the query string and success information</returns>
        public (string Query, bool Success, string ErrorMessage) GetSchemaOrDatabasesQuerySafe(
            DataSourceType databaseType, 
            string userName = null, 
            bool throwOnError = false)
        {
            try
            {
                return RDBMSHelper.GetSchemasorDatabasesSafe(databaseType, userName, throwOnError);
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error in safe schema/database query: {ex.Message}");
                return (string.Empty, false, ex.Message);
            }
        }

        /// <summary>
        /// Validates a database schema query using RDBMSHelper
        /// </summary>
        /// <param name="databaseType">Database type for the query</param>
        /// <param name="userName">Username used in the query</param>
        /// <param name="query">The query string to validate</param>
        /// <returns>A QueryValidationResult containing validation status and details</returns>
        public QueryValidationResult ValidateSchemaQuery(DataSourceType databaseType, string userName, string query = null)
        {
            try
            {
                return RDBMSHelper.ValidateSchemaQuery(databaseType, userName, query);
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error validating schema query: {ex.Message}");
                return new QueryValidationResult 
                { 
                    IsValid = false, 
                    ErrorMessage = ex.Message,
                    ErrorType = QueryErrorType.Other
                };
            }
        }

        /// <summary>
        /// Generates a SQL query to add a primary key to a table using RDBMSHelper
        /// </summary>
        /// <param name="databaseType">The type of RDBMS</param>
        /// <param name="tableName">The name of the table</param>
        /// <param name="primaryKey">The name of the primary key column</param>
        /// <param name="type">The data type of the primary key column</param>
        /// <returns>A SQL query to add a primary key</returns>
        public string GeneratePrimaryKeyQuery(DataSourceType databaseType, string tableName, string primaryKey, string type)
        {
            try
            {
                return RDBMSHelper.GeneratePrimaryKeyQuery(databaseType, tableName, primaryKey, type);
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error generating primary key query: {ex.Message}");
                return "-- Error generating primary key query";
            }
        }

        /// <summary>
        /// Generates a query to fetch the next value from a sequence using RDBMSHelper
        /// </summary>
        /// <param name="databaseType">The type of the database</param>
        /// <param name="sequenceName">The name of the sequence</param>
        /// <returns>A query string to fetch the next value from the sequence</returns>
        public string GenerateNextSequenceValueQuery(DataSourceType databaseType, string sequenceName)
        {
            try
            {
                return RDBMSHelper.GenerateFetchNextSequenceValueQuery(databaseType, sequenceName);
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error generating next sequence value query: {ex.Message}");
                return "-- Error generating sequence query";
            }
        }

        /// <summary>
        /// Generates a query to fetch the last inserted identity value using RDBMSHelper
        /// </summary>
        /// <param name="databaseType">The type of RDBMS</param>
        /// <param name="sequenceName">The name of the sequence or generator</param>
        /// <returns>A query string to fetch the last inserted identity value</returns>
        public string GenerateLastIdentityQuery(DataSourceType databaseType, string sequenceName = "")
        {
            try
            {
                return RDBMSHelper.GenerateFetchLastIdentityQuery(databaseType, sequenceName);
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error generating last identity query: {ex.Message}");
                return "-- Error generating identity query";
            }
        }

        /// <summary>
        /// Gets the SQL syntax for paging results using RDBMSHelper
        /// </summary>
        /// <param name="dataSourceType">Database type</param>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Number of records per page</param>
        /// <returns>SQL paging syntax</returns>
        public string GetPagingSyntax(DataSourceType dataSourceType, int pageNumber, int pageSize)
        {
            try
            {
                return RDBMSHelper.GetPagingSyntax(dataSourceType, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error generating paging syntax: {ex.Message}");
                return "-- Error generating paging syntax";
            }
        }

        /// <summary>
        /// Generates SQL to drop an entity using RDBMSHelper
        /// </summary>
        /// <param name="dataSourceType">Database type</param>
        /// <param name="entityName">Name of the entity to drop</param>
        /// <returns>SQL statement to drop the entity</returns>
        public string GetDropEntityQuery(DataSourceType dataSourceType, string entityName)
        {
            try
            {
                return RDBMSHelper.GetDropEntity(dataSourceType, entityName);
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error generating drop entity query: {ex.Message}");
                return "-- Error generating drop entity query";
            }
        }

        /// <summary>
        /// Generates a query to create an index using RDBMSHelper
        /// </summary>
        /// <param name="databaseType">Database type</param>
        /// <param name="tableName">Name of the table</param>
        /// <param name="indexName">Name of the index</param>
        /// <param name="columns">Array of column names</param>
        /// <param name="options">Optional index creation options</param>
        /// <returns>SQL statement to create the index</returns>
        public string GenerateCreateIndexQuery(DataSourceType databaseType, string tableName, string indexName, 
            string[] columns, Dictionary<string, object> options = null)
        {
            try
            {
                return RDBMSHelper.GenerateCreateIndexQuery(databaseType, tableName, indexName, columns, options);
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error generating create index query: {ex.Message}");
                return "-- Error generating create index query";
            }
        }

        /// <summary>
        /// Generates SQL statements for transaction operations using RDBMSHelper
        /// </summary>
        /// <param name="databaseType">Database type</param>
        /// <param name="operation">Transaction operation (Begin, Commit, Rollback)</param>
        /// <returns>SQL statement for the transaction operation</returns>
        public string GetTransactionStatement(DataSourceType databaseType, TransactionOperation operation)
        {
            try
            {
                return RDBMSHelper.GetTransactionStatement(databaseType, operation);
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error generating transaction statement: {ex.Message}");
                return "-- Error generating transaction statement";
            }
        }

        /// <summary>
        /// Safely quotes a value for SQL queries using RDBMSHelper
        /// </summary>
        /// <param name="value">Value to quote</param>
        /// <param name="dataSourceType">Database type</param>
        /// <returns>Safely quoted value</returns>
        public string SafeQuoteValue(string value, DataSourceType dataSourceType)
        {
            try
            {
                return RDBMSHelper.SafeQuote(value, dataSourceType);
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error safely quoting value: {ex.Message}");
                return value?.Replace("'", "''") ?? string.Empty; // Basic fallback
            }
        }

        /// <summary>
        /// Determines if the database type supports specific features using RDBMSHelper
        /// </summary>
        /// <param name="dataSourceType">Database type</param>
        /// <param name="feature">Feature to check</param>
        /// <returns>True if the feature is supported</returns>
        public bool SupportsFeature(DataSourceType dataSourceType, DatabaseFeature feature)
        {
            try
            {
                return RDBMSHelper.SupportsFeature(dataSourceType, feature);
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error checking feature support: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Generates SQL to create a table based on an EntityStructure using RDBMSHelper
        /// </summary>
        /// <param name="entity">The EntityStructure containing entity definition</param>
        /// <returns>A tuple containing the SQL statement, success flag, and any error message</returns>
        public (string Sql, bool Success, string ErrorMessage) GenerateCreateTableSQL(EntityStructure entity)
        {
            try
            {
                return RDBMSHelper.GenerateCreateTableSQL(entity);
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error generating create table SQL: {ex.Message}");
                return (null, false, ex.Message);
            }
        }

        /// <summary>
        /// Generates SQL to insert records into an entity using RDBMSHelper
        /// </summary>
        /// <param name="entity">The EntityStructure containing entity information</param>
        /// <param name="fieldValues">Dictionary containing field values to insert</param>
        /// <returns>A tuple containing the SQL statement, parameters, success flag, and any error message</returns>
        public (string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage) GenerateInsertSQL(
            EntityStructure entity, Dictionary<string, object> fieldValues)
        {
            try
            {
                return RDBMSHelper.GenerateInsertWithValues(entity, fieldValues);
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error generating insert SQL: {ex.Message}");
                return (null, null, false, ex.Message);
            }
        }

        /// <summary>
        /// Generates SQL to update records in an entity using RDBMSHelper
        /// </summary>
        /// <param name="entity">The EntityStructure containing entity information</param>
        /// <param name="fieldValues">Dictionary containing field values to update</param>
        /// <param name="whereValues">Dictionary containing values for the WHERE clause</param>
        /// <returns>A tuple containing the SQL statement, success flag, and any error message</returns>
        public (string Sql, bool Success, string ErrorMessage) GenerateUpdateSQL(
            EntityStructure entity, 
            Dictionary<string, object> fieldValues, 
            Dictionary<string, object> whereValues)
        {
            try
            {
                var result = RDBMSHelper.GenerateUpdateEntityWithValues(entity, fieldValues, whereValues);
                // Fix: The method returns (bool Success, string ErrorMessage), but ErrorMessage contains the SQL
                // We need to generate the SQL separately
                var updateSql = GenerateUpdateSQLInternal(entity, fieldValues, whereValues);
                return (updateSql, result.Success, result.ErrorMessage);
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error generating update SQL: {ex.Message}");
                return (null, false, ex.Message);
            }
        }

        /// <summary>
        /// Internal method to generate update SQL
        /// </summary>
        private string GenerateUpdateSQLInternal(EntityStructure entity, 
            Dictionary<string, object> fieldValues, Dictionary<string, object> whereValues)
        {
            var tableName = entity.EntityName;
            var setClauses = string.Join(", ", fieldValues.Keys.Select(k => $"{k} = @{k}"));
            var whereClauses = string.Join(" AND ", whereValues.Keys.Select(k => $"{k} = @where_{k}"));
            
            return $"UPDATE {tableName} SET {setClauses} WHERE {whereClauses}";
        }

        /// <summary>
        /// Generates SQL to delete records from an entity using RDBMSHelper
        /// </summary>
        /// <param name="entity">The EntityStructure containing entity information</param>
        /// <param name="whereValues">Dictionary containing values for the WHERE clause</param>
        /// <returns>A tuple containing the SQL statement, success flag, and any error message</returns>
        public (string Sql, bool Success, string ErrorMessage) GenerateDeleteSQL(
            EntityStructure entity, 
            Dictionary<string, object> whereValues)
        {
            try
            {
                return RDBMSHelper.GenerateDeleteEntityWithValues(entity, whereValues);
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error generating delete SQL: {ex.Message}");
                return (null, false, ex.Message);
            }
        }

        /// <summary>
        /// Validates an entity structure using RDBMSHelper
        /// </summary>
        /// <param name="entity">The EntityStructure to validate</param>
        /// <returns>Validation result with errors if any were found</returns>
        public (bool IsValid, List<string> ValidationErrors) ValidateEntityStructure(EntityStructure entity)
        {
            try
            {
                return RDBMSHelper.ValidateEntityStructure(entity);
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error validating entity structure: {ex.Message}");
                return (false, new List<string> { ex.Message });
            }
        }

        /// <summary>
        /// Initializes the query list with enhanced queries from RDBMSHelper.
        /// </summary>
        public IErrorsInfo InitQueryList()
        {
            var errorInfo = new ErrorsInfo { Flag = Errors.Ok };
            try
            {
                string path = Path.Combine(_configPath, "QueryList.json");
                if (File.Exists(path))
                {
                    QueryList = LoadQueryFile();
                }
                else
                {
                    // Use RDBMSHelper's comprehensive query list as default
                    QueryList = RDBMSHelper.CreateQuerySqlRepos();
                    SaveQueryFile();
                }

                // Merge any additional queries from RDBMSHelper that might not be in the loaded list
                MergeRDBMSHelperQueries();
            }
            catch (Exception ex)
            {
                errorInfo.Flag = Errors.Failed;
                errorInfo.Ex = ex;
                errorInfo.Message = ex.Message;
                _logger?.WriteLog($"Error initializing query list: {ex.Message}");
            }
            return errorInfo;
        }

        /// <summary>
        /// Merges additional queries from RDBMSHelper into the current QueryList
        /// </summary>
        private void MergeRDBMSHelperQueries()
        {
            try
            {
                var helperQueries = RDBMSHelper.CreateQuerySqlRepos();
                var existingKeys = QueryList.Select(q => new { q.DatabaseType, q.Sqltype }).ToHashSet();

                foreach (var helperQuery in helperQueries)
                {
                    var key = new { helperQuery.DatabaseType, helperQuery.Sqltype };
                    if (!existingKeys.Contains(key))
                    {
                        QueryList.Add(helperQuery);
                        existingKeys.Add(key);
                    }
                }

                _logger?.WriteLog($"Merged {helperQueries.Count} queries from RDBMSHelper");
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error merging RDBMSHelper queries: {ex.Message}");
            }
        }

        /// <summary>
        /// Saves the query list to a JSON file.
        /// </summary>
        public void SaveQueryFile()
        {
            try
            {
                string path = Path.Combine(_configPath, "QueryList.json");
                _jsonLoader.Serialize(path, QueryList);
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error saving query file: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads a query file and returns a list of QuerySqlRepo objects.
        /// </summary>
        public List<QuerySqlRepo> LoadQueryFile()
        {
            try
            {
                string path = Path.Combine(_configPath, "QueryList.json");
                if (File.Exists(path))
                {
                    QueryList = _jsonLoader.DeserializeObject<QuerySqlRepo>(path);
                }
                return QueryList ?? new List<QuerySqlRepo>();
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error loading query file: {ex.Message}");
                return new List<QuerySqlRepo>();
            }
        }

        /// <summary>
        /// Initializes a list of default query values using RDBMSHelper for comprehensive coverage.
        /// </summary>
        public List<QuerySqlRepo> InitQueryDefaultValues()
        {
            try
            {
                // Use RDBMSHelper's comprehensive query repository instead of the limited hardcoded ones
                return RDBMSHelper.CreateQuerySqlRepos();
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error initializing default query values from RDBMSHelper: {ex.Message}");
                // Fallback to the original hardcoded queries if RDBMSHelper fails
                return GetFallbackQueries();
            }
        }

        /// <summary>
        /// Fallback method that returns the original hardcoded queries
        /// </summary>
        private List<QuerySqlRepo> GetFallbackQueries()
        {
            return new List<QuerySqlRepo>
            {
                //-------------------------------------- Oracle Query Sql Set -------------------------------------------
                new QuerySqlRepo(DataSourceType.Oracle, "select * from {0} {2}", Sqlcommandtype.getTable),
                new QuerySqlRepo(DataSourceType.Oracle, "select TABLE_NAME from tabs", Sqlcommandtype.getlistoftables),
                new QuerySqlRepo(DataSourceType.Oracle, "select * from {0} {2}", Sqlcommandtype.getPKforTable),
                new QuerySqlRepo(DataSourceType.Oracle, "select * from {0} {2}", Sqlcommandtype.getFKforTable),
                new QuerySqlRepo(DataSourceType.Oracle, @"SELECT a.position,a.table_name child_table, a.column_name child_column, a.constraint_name, 
                                        b.table_name parent_table, b.column_name parent_column
                                FROM all_cons_columns a,all_constraints c,all_cons_columns b 
                                    where a.owner = c.owner AND a.constraint_name = c.constraint_name
                                    and  c.owner = b.owner and c.r_constraint_name = b.constraint_name
                                    and c.constraint_type = 'R'
                                    AND b.table_name = '{0}'", Sqlcommandtype.getChildTable),
                new QuerySqlRepo(DataSourceType.Oracle, "select * from {0} {2}", Sqlcommandtype.getParentTable),

                //-------------------------------------- SqlServer Query Sql Set -------------------------------------------
                new QuerySqlRepo(DataSourceType.SqlServer, "select * from {0} {2}", Sqlcommandtype.getTable),
                new QuerySqlRepo(DataSourceType.SqlServer, "select TABLE_NAME from INFORMATION_SCHEMA.TABLES where TABLE_SCHEMA='{0}'", Sqlcommandtype.getlistoftables),
                new QuerySqlRepo(DataSourceType.SqlServer, "select * from {0} {2}", Sqlcommandtype.getPKforTable),
                new QuerySqlRepo(DataSourceType.SqlServer, "select * from {0} {2}", Sqlcommandtype.getFKforTable),
                new QuerySqlRepo(DataSourceType.SqlServer, @"SELECT OBJECT_NAME(fkeys.constraint_object_id) constraint_name
                                            ,OBJECT_NAME(fkeys.parent_object_id) child_table
                                            ,COL_NAME(fkeys.parent_object_id, fkeys.parent_column_id) child_column
                                            ,OBJECT_SCHEMA_NAME(fkeys.parent_object_id) referencing_schema_name
                                            ,OBJECT_NAME (fkeys.referenced_object_id) parent_table
                                            ,COL_NAME(fkeys.referenced_object_id, fkeys.referenced_column_id) parent_column
                                            ,OBJECT_SCHEMA_NAME(fkeys.referenced_object_id) referenced_schema_name
                                            FROM sys.foreign_key_columns AS fkeys
                                    WHERE OBJECT_NAME(fkeys.parent_object_id) = '{0}' AND 
                                          OBJECT_SCHEMA_NAME(fkeys.parent_object_id) = '{1}'", Sqlcommandtype.getChildTable),
                new QuerySqlRepo(DataSourceType.SqlServer, "select * from {0} {1}", Sqlcommandtype.getParentTable),

                //-------------------------------------- MySQL Query Sql Set -------------------------------------------
                new QuerySqlRepo(DataSourceType.Mysql, "select * from {0} {2}", Sqlcommandtype.getTable),
                new QuerySqlRepo(DataSourceType.Mysql, "select table_name from information_schema.tables where table_schema='{0}'", Sqlcommandtype.getlistoftables),
                new QuerySqlRepo(DataSourceType.Mysql, "select * from {0} {2}", Sqlcommandtype.getPKforTable),
                new QuerySqlRepo(DataSourceType.Mysql, "select * from {0} {2}", Sqlcommandtype.getFKforTable),
                new QuerySqlRepo(DataSourceType.Mysql, @"SELECT CONSTRAINT_NAME as constraint_name,
                                                TABLE_NAME as child_table,
                                                COLUMN_NAME as child_column,
                                                REFERENCED_TABLE_NAME as parent_table,
                                                REFERENCED_COLUMN_NAME as parent_column
                                        FROM information_schema.KEY_COLUMN_USAGE 
                                        WHERE REFERENCED_TABLE_NAME = '{0}' AND TABLE_SCHEMA = DATABASE()", Sqlcommandtype.getChildTable),
                new QuerySqlRepo(DataSourceType.Mysql, "select * from {0} {2}", Sqlcommandtype.getParentTable),

                //-------------------------------------- SqlCompact Query Sql Set -------------------------------------------
                new QuerySqlRepo(DataSourceType.SqlCompact, "select * from {0} {2}", Sqlcommandtype.getTable),
                new QuerySqlRepo(DataSourceType.SqlCompact, "select TABLE_NAME from INFORMATION_SCHEMA.TABLES where TABLE_SCHEMA='{0}'", Sqlcommandtype.getlistoftables),
                new QuerySqlRepo(DataSourceType.SqlCompact, "select * from {0} {2}", Sqlcommandtype.getPKforTable),
                new QuerySqlRepo(DataSourceType.SqlCompact, "select * from {0} {2}", Sqlcommandtype.getFKforTable),
                new QuerySqlRepo(DataSourceType.SqlCompact, "select * from {0} {2}", Sqlcommandtype.getChildTable),
                new QuerySqlRepo(DataSourceType.SqlCompact, "select * from {0} {2}", Sqlcommandtype.getParentTable),

                //-------------------------------------- SQLite Query Sql Set -------------------------------------------
                new QuerySqlRepo(DataSourceType.SqlLite, "select * from {0} {2}", Sqlcommandtype.getTable),
                new QuerySqlRepo(DataSourceType.SqlLite, "select name as table_name from sqlite_master where type ='table'", Sqlcommandtype.getlistoftables),
                new QuerySqlRepo(DataSourceType.SqlLite, "select * from {0} {2}", Sqlcommandtype.getPKforTable),
                new QuerySqlRepo(DataSourceType.SqlLite, "select * from {0} {2}", Sqlcommandtype.getFKforTable),
                new QuerySqlRepo(DataSourceType.SqlLite, "select name as table_name from sqlite_master where type ='table'", Sqlcommandtype.getChildTable),
                new QuerySqlRepo(DataSourceType.SqlLite, "select * from {0} {2}", Sqlcommandtype.getParentTable),

                //-------------------------------------- Excel/CSV Query Sql Set -------------------------------------------
                new QuerySqlRepo(DataSourceType.Xls, "select * from [{0}] {2}", Sqlcommandtype.getTable),
                new QuerySqlRepo(DataSourceType.Text, "select * from [{0}] {2}", Sqlcommandtype.getTable)
            };
        }

        /// <summary>
        /// Generates SQL to check if a table exists using RDBMSHelper
        /// </summary>
        /// <param name="dataSourceType">Database type</param>
        /// <param name="tableName">Name of the table to check</param>
        /// <param name="schemaName">Schema name (optional)</param>
        /// <returns>SQL statement to check table existence</returns>
        public string GetTableExistsQuery(DataSourceType dataSourceType, string tableName, string schemaName = null)
        {
            try
            {
                return RDBMSHelper.GetTableExistsQuery(dataSourceType, tableName, schemaName);
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error generating table exists query: {ex.Message}");
                return "-- Error generating table exists query";
            }
        }

        /// <summary>
        /// Generates SQL to get column information for a table using RDBMSHelper
        /// </summary>
        /// <param name="dataSourceType">Database type</param>
        /// <param name="tableName">Name of the table</param>
        /// <param name="schemaName">Schema name (optional)</param>
        /// <returns>SQL statement to get column information</returns>
        public string GetColumnInfoQuery(DataSourceType dataSourceType, string tableName, string schemaName = null)
        {
            try
            {
                return RDBMSHelper.GetColumnInfoQuery(dataSourceType, tableName, schemaName);
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error generating column info query: {ex.Message}");
                return "-- Error generating column info query";
            }
        }

        /// <summary>
        /// Generates SQL to get the count of records in a table using RDBMSHelper
        /// </summary>
        /// <param name="dataSourceType">Database type</param>
        /// <param name="tableName">Name of the table</param>
        /// <param name="schemaName">Schema name (optional)</param>
        /// <param name="whereClause">Optional WHERE clause</param>
        /// <returns>SQL statement to count records</returns>
        public string GetRecordCountQuery(DataSourceType dataSourceType, string tableName, string schemaName = null, string whereClause = null)
        {
            try
            {
                return RDBMSHelper.GetRecordCountQuery(dataSourceType, tableName, schemaName, whereClause);
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error generating record count query: {ex.Message}");
                return "-- Error generating record count query";
            }
        }

        /// <summary>
        /// Generates SQL to truncate a table using RDBMSHelper
        /// </summary>
        /// <param name="dataSourceType">Database type</param>
        /// <param name="tableName">Name of the table to truncate</param>
        /// <param name="schemaName">Schema name (optional)</param>
        /// <returns>SQL statement to truncate the table</returns>
        public string GetTruncateTableQuery(DataSourceType dataSourceType, string tableName, string schemaName = null)
        {
            try
            {
                return RDBMSHelper.GetTruncateTableQuery(dataSourceType, tableName, schemaName);
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error generating truncate table query: {ex.Message}");
                return "-- Error generating truncate table query";
            }
        }
    }
}