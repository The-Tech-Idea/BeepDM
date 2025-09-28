using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Helpers.RDBMSHelpers
{
    /// <summary>
    /// Helper class for generating database schema and metadata queries for different RDBMS types.
    /// </summary>
    public static class DatabaseSchemaQueryHelper
    {
        /// <summary>
        /// Gets the query for fetching schemas or databases that the specified user has access to.
        /// </summary>
        /// <param name="rdbms">The type of database system.</param>
        /// <param name="userName">The username to check privileges for (can be null for some database systems).</param>
        /// <returns>A SQL query string to retrieve accessible schemas or databases, or empty string if not supported.</returns>
        /// <remarks>
        /// This method generates system-specific queries to list schemas/databases based on the database type.
        /// For relational databases, it returns SQL queries that consider user permissions.
        /// For NoSQL and other database types, it returns appropriate commands or empty string if listing
        /// is not supported through standard queries.
        /// </remarks>
        public static string GetSchemasorDatabases(DataSourceType rdbms, string userName)
        {
            // Safely handle null username
            userName = userName?.Replace("'", "''") ?? ""; // SQL injection protection

            switch (rdbms)
            {
                // Relational Databases
                case DataSourceType.SqlServer:
                    return $"SELECT name FROM sys.databases WHERE HAS_DBACCESS(name) = 1 ORDER BY name";

                case DataSourceType.Mysql:
                    if (!string.IsNullOrEmpty(userName))
                        return $"SELECT SCHEMA_NAME AS 'Database' FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME IN (SELECT DISTINCT TABLE_SCHEMA FROM INFORMATION_SCHEMA.SCHEMA_PRIVILEGES WHERE GRANTEE LIKE '%{userName}%') ORDER BY SCHEMA_NAME";
                    else
                        return "SHOW DATABASES";

                case DataSourceType.Postgre:
                    if (!string.IsNullOrEmpty(userName))
                        return $"SELECT datname FROM pg_database WHERE datistemplate = false AND has_database_privilege('{userName}', datname, 'CONNECT') ORDER BY datname";
                    else
                        return "SELECT datname FROM pg_database WHERE datistemplate = false ORDER BY datname";

                case DataSourceType.Oracle:
                    if (!string.IsNullOrEmpty(userName))
                        return $"SELECT DISTINCT OWNER FROM DBA_TAB_PRIVS WHERE GRANTEE = '{userName}' UNION SELECT USERNAME FROM ALL_USERS WHERE USERNAME = '{userName}' ORDER BY 1";
                    else
                        return "SELECT USERNAME FROM ALL_USERS ORDER BY 1";

                case DataSourceType.DB2:
                    if (!string.IsNullOrEmpty(userName))
                        return $"SELECT DISTINCT SCHEMANAME FROM SYSCAT.SCHEMAAUTH WHERE GRANTEE = '{userName}' ORDER BY SCHEMANAME";
                    else
                        return "SELECT SCHEMANAME FROM SYSCAT.SCHEMATA ORDER BY SCHEMANAME";

                case DataSourceType.SqlLite:
                    // SQLite typically has one main database per connection, but can attach others
                    return "PRAGMA database_list;";

                case DataSourceType.FireBird:
                    // Firebird has limited support for listing databases from within a connection
                    return "SELECT RDB$DATABASE_NAME FROM RDB$DATABASE";

                // Cloud SQL services
                case DataSourceType.SnowFlake:
                    return "SHOW DATABASES";

                case DataSourceType.Hana:
                    return "SELECT SCHEMA_NAME FROM SCHEMAS WHERE HAS_PRIVILEGES_ON_SCHEMA(SCHEMA_NAME) = 'TRUE' ORDER BY SCHEMA_NAME";

                case DataSourceType.AzureSQL:
                case DataSourceType.AWSRDS:
                    return "SELECT name FROM sys.databases WHERE HAS_DBACCESS(name) = 1";

                case DataSourceType.Cockroach:
                    return "SHOW DATABASES";

                case DataSourceType.Spanner:
                    return "SELECT SCHEMA_NAME FROM INFORMATION_SCHEMA.SCHEMATA ORDER BY SCHEMA_NAME";

                case DataSourceType.TerraData:
                    return "SELECT DatabaseName FROM DBC.Databases ORDER BY DatabaseName";

                case DataSourceType.Vertica:
                    return "SELECT DISTINCT schema_name FROM v_catalog.schemata ORDER BY schema_name";

                // BigData/Analytics databases
                case DataSourceType.GoogleBigQuery:
                    return "SELECT schema_name FROM INFORMATION_SCHEMA.SCHEMATA ORDER BY schema_name";

                case DataSourceType.AWSRedshift:
                    return "SELECT DISTINCT schemaname FROM pg_tables ORDER BY schemaname";

                case DataSourceType.AWSAthena:
                    return "SHOW DATABASES";

                case DataSourceType.ClickHouse:
                    return "SHOW DATABASES";

                // NoSQL databases - many don't use SQL syntax but may have equivalent commands
                case DataSourceType.MongoDB:
                    return "show dbs"; // MongoDB shell command

                case DataSourceType.Couchbase:
                    return "SELECT name FROM system:keyspaces"; // N1QL query

                case DataSourceType.Cassandra:
                    return "SELECT keyspace_name FROM system_schema.keyspaces";

                case DataSourceType.Redis:
                    return "INFO keyspace"; // Redis command

                case DataSourceType.ElasticSearch:
                    return "_cat/indices?v"; // REST API endpoint

                case DataSourceType.Neo4j:
                    return "CALL db.schemas()"; // Cypher query

                // For databases that don't support listing or use completely different paradigms
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Gets the query for fetching schemas or databases with built-in error handling.
        /// </summary>
        /// <param name="rdbms">The type of database system.</param>
        /// <param name="userName">The username to check privileges for.</param>
        /// <param name="throwOnError">Whether to throw exceptions for errors (default: false).</param>
        /// <returns>A tuple containing the query string and a success indicator.</returns>
        public static (string Query, bool Success, string ErrorMessage) GetSchemasorDatabasesSafe(
            DataSourceType rdbms,
            string userName,
            bool throwOnError = false)
        {
            try
            {
                // First validate the parameters
                if (rdbms == DataSourceType.NONE)
                {
                    if (throwOnError) throw new ArgumentException("Invalid database type specified", nameof(rdbms));
                    return (string.Empty, false, "Invalid database type specified");
                }

                // Generate the query as before
                string query = GetSchemasorDatabases(rdbms, userName);

                // Validate the generated query
                var validation = ValidateSchemaQuery(rdbms, userName, query);

                if (!validation.IsValid && throwOnError)
                {
                    throw new InvalidOperationException($"Invalid query generated: {validation.ErrorMessage}");
                }

                return (query, validation.IsValid, validation.ErrorMessage);
            }
            catch (Exception ex)
            {
                if (throwOnError) throw;
                return (string.Empty, false, ex.Message);
            }
        }

        /// <summary>
        /// Validates a generated database schema query and provides error information.
        /// </summary>
        /// <param name="rdbms">Database type for the query</param>
        /// <param name="userName">Username used in the query</param>
        /// <param name="query">The generated query string (if already created)</param>
        /// <returns>A QueryValidationResult containing validation status and details</returns>
        public static QueryValidationResult ValidateSchemaQuery(DataSourceType rdbms, string userName, string query = null)
        {
            var result = new QueryValidationResult { IsValid = true };

            // Use the existing query if provided, otherwise generate it
            string queryToValidate = query ?? GetSchemasorDatabases(rdbms, userName);
            result.Query = queryToValidate;

            // If no query was generated, there's a problem
            if (string.IsNullOrEmpty(queryToValidate))
            {
                result.IsValid = false;
                result.ErrorMessage = $"No schema query could be generated for database type: {rdbms}";
                result.ErrorType = QueryErrorType.UnsupportedDatabase;
                return result;
            }

            // Check for basic SQL syntax issues
            if (!IsValidDatabaseCommand(queryToValidate))
            {
                result.IsValid = false;
                result.ErrorMessage = "Generated query does not appear to be valid SQL or a valid command format.";
                result.ErrorType = QueryErrorType.SyntaxError;
                return result;
            }

            // Database-specific validation
            switch (rdbms)
            {
                case DataSourceType.Oracle:
                    if (queryToValidate.Contains("DBA_TAB_PRIVS") &&
                        userName?.ToUpper() != "SYS" &&
                        userName?.ToUpper() != "SYSTEM" &&
                        !(userName?.ToUpper()?.EndsWith("DBA") ?? false))
                    {
                        result.IsValid = false;
                        result.ErrorMessage = "DBA_TAB_PRIVS view requires DBA privileges. Standard users should use ALL_TAB_PRIVS instead.";
                        result.ErrorType = QueryErrorType.PermissionIssue;
                        result.Suggestion = "Either use a user with DBA privileges, or modify the query to use ALL_TAB_PRIVS.";
                    }
                    break;

                case DataSourceType.SqlLite:
                    if (!queryToValidate.Contains("PRAGMA"))
                    {
                        result.AddWarning("SQLite schema queries may be limited in functionality compared to other RDBMS systems.");
                    }
                    break;

                case DataSourceType.Mysql:
                    if (!string.IsNullOrEmpty(userName) && !queryToValidate.Contains("'%"))
                    {
                        result.AddWarning("MySQL username pattern should include % wildcards for host matching, e.g. '%username%' instead of just 'username'");
                    }
                    break;

                case DataSourceType.MongoDB:
                case DataSourceType.Couchbase:
                case DataSourceType.ElasticSearch:
                case DataSourceType.Redis:
                case DataSourceType.Neo4j:
                    result.AddWarning("NoSQL database commands may need to be executed through specific APIs rather than SQL interfaces.");
                    break;
            }

            // Check for SQL injection vulnerabilities despite escaping
            if (!string.IsNullOrEmpty(userName))
            {
                if (userName.Contains("--") || userName.Contains(";") ||
                    userName.Contains("/*") || userName.Contains("*/"))
                {
                    result.IsValid = false;
                    result.ErrorMessage = "Potential SQL injection risk detected in username parameter.";
                    result.ErrorType = QueryErrorType.SecurityRisk;
                    result.Suggestion = "Remove special characters from username before using in query generation.";
                }
            }

            return result;
        }

        /// <summary>
        /// Generates SQL to check if a table exists
        /// </summary>
        /// <param name="dataSourceType">Database type</param>
        /// <param name="tableName">Name of the table to check</param>
        /// <param name="schemaName">Schema name (optional)</param>
        /// <returns>SQL statement to check table existence</returns>
        public static string GetTableExistsQuery(DataSourceType dataSourceType, string tableName, string schemaName = null)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Table name cannot be null or empty", nameof(tableName));

            return dataSourceType switch
            {
                DataSourceType.SqlServer => string.IsNullOrEmpty(schemaName) 
                    ? $"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{tableName}'"
                    : $"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{tableName}' AND TABLE_SCHEMA = '{schemaName}'",
                
                DataSourceType.Mysql => string.IsNullOrEmpty(schemaName)
                    ? $"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{tableName}' AND TABLE_SCHEMA = DATABASE()"
                    : $"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{tableName}' AND TABLE_SCHEMA = '{schemaName}'",
                
                DataSourceType.Postgre => string.IsNullOrEmpty(schemaName)
                    ? $"SELECT COUNT(*) FROM information_schema.tables WHERE table_name = '{tableName}' AND table_schema = 'public'"
                    : $"SELECT COUNT(*) FROM information_schema.tables WHERE table_name = '{tableName}' AND table_schema = '{schemaName}'",
                
                DataSourceType.Oracle => string.IsNullOrEmpty(schemaName)
                    ? $"SELECT COUNT(*) FROM USER_TABLES WHERE TABLE_NAME = UPPER('{tableName}')"
                    : $"SELECT COUNT(*) FROM ALL_TABLES WHERE TABLE_NAME = UPPER('{tableName}') AND OWNER = UPPER('{schemaName}')",
                
                DataSourceType.SqlLite => $"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='{tableName}'",
                
                DataSourceType.DB2 => string.IsNullOrEmpty(schemaName)
                    ? $"SELECT COUNT(*) FROM SYSCAT.TABLES WHERE TABNAME = UPPER('{tableName}')"
                    : $"SELECT COUNT(*) FROM SYSCAT.TABLES WHERE TABNAME = UPPER('{tableName}') AND TABSCHEMA = UPPER('{schemaName}')",
                
                _ => $"SELECT 1 WHERE EXISTS (SELECT 1 FROM {tableName})" // Generic fallback
            };
        }

        /// <summary>
        /// Generates SQL to get column information for a table
        /// </summary>
        /// <param name="dataSourceType">Database type</param>
        /// <param name="tableName">Name of the table</param>
        /// <param name="schemaName">Schema name (optional)</param>
        /// <returns>SQL statement to get column information</returns>
        public static string GetColumnInfoQuery(DataSourceType dataSourceType, string tableName, string schemaName = null)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Table name cannot be null or empty", nameof(tableName));

            return dataSourceType switch
            {
                DataSourceType.SqlServer => string.IsNullOrEmpty(schemaName)
                    ? $"SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, COLUMN_DEFAULT FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}' ORDER BY ORDINAL_POSITION"
                    : $"SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, COLUMN_DEFAULT FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}' AND TABLE_SCHEMA = '{schemaName}' ORDER BY ORDINAL_POSITION",
                
                DataSourceType.Mysql => string.IsNullOrEmpty(schemaName)
                    ? $"SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, COLUMN_DEFAULT FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}' AND TABLE_SCHEMA = DATABASE() ORDER BY ORDINAL_POSITION"
                    : $"SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, COLUMN_DEFAULT FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}' AND TABLE_SCHEMA = '{schemaName}' ORDER BY ORDINAL_POSITION",
                
                DataSourceType.Postgre => string.IsNullOrEmpty(schemaName)
                    ? $"SELECT column_name, data_type, is_nullable, column_default FROM information_schema.columns WHERE table_name = '{tableName}' AND table_schema = 'public' ORDER BY ordinal_position"
                    : $"SELECT column_name, data_type, is_nullable, column_default FROM information_schema.columns WHERE table_name = '{tableName}' AND table_schema = '{schemaName}' ORDER BY ordinal_position",
                
                DataSourceType.Oracle => string.IsNullOrEmpty(schemaName)
                    ? $"SELECT COLUMN_NAME, DATA_TYPE, NULLABLE, DATA_DEFAULT FROM USER_TAB_COLUMNS WHERE TABLE_NAME = UPPER('{tableName}') ORDER BY COLUMN_ID"
                    : $"SELECT COLUMN_NAME, DATA_TYPE, NULLABLE, DATA_DEFAULT FROM ALL_TAB_COLUMNS WHERE TABLE_NAME = UPPER('{tableName}') AND OWNER = UPPER('{schemaName}') ORDER BY COLUMN_ID",
                
                DataSourceType.SqlLite => $"PRAGMA table_info({tableName})",
                
                DataSourceType.DB2 => string.IsNullOrEmpty(schemaName)
                    ? $"SELECT COLNAME, TYPENAME, NULLS, DEFAULT FROM SYSCAT.COLUMNS WHERE TABNAME = UPPER('{tableName}') ORDER BY COLNO"
                    : $"SELECT COLNAME, TYPENAME, NULLS, DEFAULT FROM SYSCAT.COLUMNS WHERE TABNAME = UPPER('{tableName}') AND TABSCHEMA = UPPER('{schemaName}') ORDER BY COLNO",
                
                _ => $"SELECT * FROM {tableName} WHERE 1=0" // Generic fallback to get structure
            };
        }

        /// <summary>
        /// Checks if a command is valid for database operations (SQL or database-specific commands).
        /// </summary>
        /// <param name="command">The command to validate.</param>
        /// <returns>True if the command appears valid, false otherwise.</returns>
        private static bool IsValidDatabaseCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return false;

            // Check for SQL keywords
            string[] sqlKeywords = {
                "SELECT", "INSERT", "UPDATE", "DELETE", "CREATE", "ALTER", "DROP",
                "FROM", "WHERE", "JOIN", "ON", "AND", "OR", "NOT", "IN", "LIKE", "SHOW", "PRAGMA"
            };

            string upperCommand = command.ToUpperInvariant();
            bool hasSqlKeyword = sqlKeywords.Any(keyword => upperCommand.Contains(keyword));

            // Check for database-specific commands
            bool hasDbSpecificCommand = 
                command.StartsWith("_cat/") ||           // ElasticSearch
                command.StartsWith("show ") ||           // MongoDB, MySQL
                command.StartsWith("CALL ") ||           // Neo4j, stored procedures
                command.Contains("PRAGMA") ||            // SQLite
                command.Contains("INFO ");               // Redis

            return hasSqlKeyword || hasDbSpecificCommand;
        }
    }

    /// <summary>
    /// Result class for query validation containing error information.
    /// </summary>
    public class QueryValidationResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the query is valid.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets the validated query string.
        /// </summary>
        public string Query { get; set; }

        /// <summary>
        /// Gets or sets the error message when validation fails.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the type of error encountered.
        /// </summary>
        public QueryErrorType ErrorType { get; set; } = QueryErrorType.None;

        /// <summary>
        /// Gets or sets suggestion for resolving the error.
        /// </summary>
        public string Suggestion { get; set; }

        /// <summary>
        /// Gets the list of warnings that don't invalidate the query but should be considered.
        /// </summary>
        public List<string> Warnings { get; } = new List<string>();

        /// <summary>
        /// Adds a warning to the validation result.
        /// </summary>
        /// <param name="warning">Warning message to add</param>
        public void AddWarning(string warning)
        {
            Warnings.Add(warning);
        }
    }

    /// <summary>
    /// Defines the types of errors that can occur in query validation.
    /// </summary>
    public enum QueryErrorType
    {
        /// <summary>No error.</summary>
        None,

        /// <summary>The database type is not supported.</summary>
        UnsupportedDatabase,

        /// <summary>The query contains syntax errors.</summary>
        SyntaxError,

        /// <summary>The operation requires permissions the user may not have.</summary>
        PermissionIssue,

        /// <summary>The query contains potential security risks.</summary>
        SecurityRisk,

        /// <summary>Other error types.</summary>
        Other
    }
}