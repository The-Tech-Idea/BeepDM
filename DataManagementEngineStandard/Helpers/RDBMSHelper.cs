using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using System.Linq;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Helpers
{
    /// <summary>
    /// Defines the database features that can be supported by different data sources
    /// </summary>
    public enum DatabaseFeature
    {
        /// <summary>Window functions like ROW_NUMBER(), RANK(), etc.</summary>
        WindowFunctions,
        
        /// <summary>Native JSON data type and operations</summary>
        Json,
        
        /// <summary>Native XML data type and operations</summary>
        Xml,
        
        /// <summary>Temporal tables for time-based data tracking</summary>
        TemporalTables,
        
        /// <summary>Full-text search capabilities</summary>
        FullTextSearch,
        
        /// <summary>Table partitioning support</summary>
        Partitioning,
        
        /// <summary>Columnar storage format</summary>
        ColumnStore
    }

    /// <summary>
    /// Defines transaction operations that can be performed
    /// </summary>
    public enum TransactionOperation
    {
        /// <summary>Begin a new transaction</summary>
        Begin,
        
        /// <summary>Commit the current transaction</summary>
        Commit,
        
        /// <summary>Rollback the current transaction</summary>
        Rollback
    }

	/// <summary>
	/// Helper class for interacting with a Relational Database Management System (RDBMS).
	/// </summary>
	public static class RDBMSHelper
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
            if (!IsSqlStatementValid(queryToValidate) &&
                !queryToValidate.Contains("SHOW") &&     // Handle special cases like "SHOW DATABASES"
                !queryToValidate.StartsWith("_cat/"))    // Handle special cases like ElasticSearch REST endpoints
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

                    // Add more database-specific validations as needed
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

        /// <summary>Checks if a given SQL statement is valid.</summary>
        /// <param name="sqlString">The SQL statement to be validated.</param>
        /// <returns>True if the SQL statement is valid, false otherwise.</returns>
        /// <remarks>
        /// The method checks if the SQL statement contains any of the common SQL keywords such as SELECT, INSERT, UPDATE, DELETE, etc.
        /// It uses a regular expression pattern to match the keywords in a case-insensitive manner.
        /// </remarks>
        public static bool IsSqlStatementValid(string sqlString)
		{
			if (sqlString == null)
			{
				return false;
			}
			// List of SQL keywords
			string[] sqlKeywords = {
			"SELECT", "INSERT", "UPDATE", "DELETE", "CREATE", "ALTER", "DROP",
			"FROM", "WHERE", "JOIN", "ON", "AND", "OR", "NOT", "IN", "LIKE"
			// Add more keywords as needed
			};

			// Create a regular expression pattern to match SQL keywords
			string pattern = @"\b(" + string.Join("|", sqlKeywords) + @")\b";

			// Use Regex to find matches
			MatchCollection matches = Regex.Matches(sqlString, pattern, RegexOptions.IgnoreCase);

			// If any keywords are found, return true
			return matches.Count > 0;
		}
		/// <summary>Generates a SQL query to add a primary key to a table in a specific RDBMS.</summary>
		/// <param name="rdbms">The type of RDBMS.</param>
		/// <param name="tableName">The name of the table.</param>
		/// <param name="primaryKey">The name of the primary key column.</param>
		/// <param name="type">The data type of the primary key column.</param>
		/// <returns>A SQL query to add a primary key to the specified table in the specified RDBMS.</returns>
		/// <exception cref="ArgumentException">Thrown when the specified RDBMS is not supported.</exception>
		public static string GeneratePrimaryKeyQuery(DataSourceType rdbms, string tableName, string primaryKey, string type)
		{
			string query = "";
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Table name cannot be null or empty", nameof(tableName));

            if (string.IsNullOrWhiteSpace(primaryKey))
                throw new ArgumentException("Primary key name cannot be null or empty", nameof(primaryKey));

            switch (rdbms)
			{
				case DataSourceType.SqlServer:
					query = $"ALTER TABLE {tableName} ADD {primaryKey} {type} PRIMARY KEY IDENTITY";
					break;
				case DataSourceType.Mysql:
					query = $"ALTER TABLE {tableName} ADD {primaryKey} {type} PRIMARY KEY AUTO_INCREMENT";
					break;
				case DataSourceType.Postgre:
					query = $"ALTER TABLE {tableName} ADD {primaryKey} {type} PRIMARY KEY GENERATED ALWAYS AS IDENTITY";
					break;
				case DataSourceType.Oracle:
					query = $"ALTER TABLE {tableName} ADD {primaryKey} {type} GENERATED ALWAYS AS IDENTITY";
					break;
				case DataSourceType.DB2:
					query = $"ALTER TABLE {tableName} ADD {primaryKey} {type} GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY";
					break;
				case DataSourceType.FireBird:
					query = $"ALTER TABLE {tableName} ADD {primaryKey} {type} PRIMARY KEY";
					break;
				case DataSourceType.SqlLite:
					query = $"ALTER TABLE {tableName} ADD {primaryKey} {type} PRIMARY KEY AUTOINCREMENT";
					break;
				case DataSourceType.Couchbase:
				case DataSourceType.Redis:
				case DataSourceType.MongoDB:
					query = "NoSQL databases typically do not have primary keys in the same way RDBMS do.";
					break;
				default:
					query = "RDBMS not supported.";
					break;
			}

			return query;
		}
		/// <summary>Generates a query to fetch the next value from a sequence in a specific database.</summary>
		/// <param name="rdbms">The type of the database.</param>
		/// <param name="sequenceName">The name of the sequence.</param>
		/// <returns>A query string to fetch the next value from the specified sequence in the given database.</returns>
		/// <exception cref="ArgumentNullException">Thrown when the sequence name is null or empty.</exception>
		public static string GenerateFetchNextSequenceValueQuery(DataSourceType rdbms, string sequenceName)
		{
			if (string.IsNullOrEmpty(sequenceName))
			{
				return "Sequence name must be provided.";
			}

			string query = "";

			switch (rdbms)
			{
				case DataSourceType.Oracle:
					query = $"SELECT {sequenceName}.NEXTVAL FROM dual";
					break;
				case DataSourceType.Postgre:
					query = $"SELECT nextval('{sequenceName}')";
					break;
				case DataSourceType.SqlServer:
					query = $"SELECT NEXT VALUE FOR {sequenceName}";
					break;
				case DataSourceType.FireBird:
					query = $"SELECT NEXT VALUE FOR {sequenceName} FROM RDB$DATABASE";
					break;
				case DataSourceType.DB2:
					query = $"SELECT NEXTVAL FOR {sequenceName} FROM sysibm.sysdummy1";
					break;
				default:
					query = null;
					break;
			}

			return query;
		}
		/// <summary>Generates a query to fetch the last inserted identity value based on the specified RDBMS.</summary>
		/// <param name="rdbms">The type of RDBMS.</param>
		/// <param name="sequenceName">The name of the sequence or generator (optional for some RDBMS).</param>
		/// <returns>A query string to fetch the last inserted identity value.</returns>
		/// <exception cref="ArgumentException">Thrown when the specified RDBMS is not supported.</exception>
		public static string GenerateFetchLastIdentityQuery(DataSourceType rdbms, string sequenceName = "")
		{
			string query = "";

			switch (rdbms)
			{
				case DataSourceType.SqlServer:
					query = "SELECT SCOPE_IDENTITY()";
					break;
				case DataSourceType.Mysql:
					query = "SELECT LAST_INSERT_ID()";
					break;
				case DataSourceType.Postgre:
					query = "SELECT LASTVAL()";
					break;
				case DataSourceType.Oracle:
					if (string.IsNullOrEmpty(sequenceName))
					{
						query = "Provide a sequence name.";
					}
					else
					{
						query = $"SELECT currval('{sequenceName}') FROM dual";
					}
					break;
				case DataSourceType.FireBird:
					if (string.IsNullOrEmpty(sequenceName))
					{
						query = "Provide a generator name.";
					}
					else
					{
						query = $"SELECT GEN_ID({sequenceName}, 0) FROM RDB$DATABASE";
					}
					break;
				case DataSourceType.SqlLite:
					query = "SELECT last_insert_rowid()";
					break;
				case DataSourceType.DB2:
					query = "SELECT IDENTITY_VAL_LOCAL() FROM sysibm.sysdummy1";
					break;
				case DataSourceType.Cassandra:  // NOTE: Cassandra doesn't support this the same way relational DBs do
					query = "Unsupported in Cassandra";
					break;
				default:
					query = "RDBMS not supported.";
					break;
			}

			return query;
		}
 
        /// <summary>Creates a list of QuerySqlRepo objects.</summary>
        /// <returns>A list of QuerySqlRepo objects.</returns>
        public static List<QuerySqlRepo> CreateQuerySqlRepos()
        {
            return new List<QuerySqlRepo>
            {
                // Getting select from other schema
                // Oracle
                new QuerySqlRepo(DataSourceType.Oracle, "SELECT TABLE_NAME FROM all_tables WHERE OWNER = '{1}'", Sqlcommandtype.getlistoftablesfromotherschema),

                // SQL Server
                new QuerySqlRepo(DataSourceType.SqlServer, "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{1}' AND TABLE_TYPE = 'BASE TABLE'", Sqlcommandtype.getlistoftablesfromotherschema),

                // MySQL
                new QuerySqlRepo(DataSourceType.Mysql, "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{1}'", Sqlcommandtype.getlistoftablesfromotherschema),

                // PostgreSQL
                new QuerySqlRepo(DataSourceType.Postgre, "SELECT table_name FROM information_schema.tables WHERE table_schema = '{1}' AND table_type = 'BASE TABLE'", Sqlcommandtype.getlistoftablesfromotherschema),

                // SQLite (Note: SQLite does not support multiple schemas, but for the sake of completeness, assuming it could)
                new QuerySqlRepo(DataSourceType.SqlLite, "SELECT name AS table_name FROM sqlite_master WHERE type='table' AND sql LIKE '%{1}%'", Sqlcommandtype.getlistoftablesfromotherschema),

                // DB2
                new QuerySqlRepo(DataSourceType.DB2, "SELECT TABNAME AS TABLE_NAME FROM SYSCAT.TABLES WHERE TABSCHEMA = '{1}'", Sqlcommandtype.getlistoftablesfromotherschema),

                // Firebird
                new QuerySqlRepo(DataSourceType.FireBird, "SELECT RDB$RELATION_NAME FROM RDB$RELATIONS WHERE RDB$VIEW_BLR IS NULL AND RDB$RELATION_NAME NOT LIKE 'RDB$%' AND RDB$RELATION_TYPE = 0 AND RDB$SYSTEM_FLAG = 0 AND RDB$RELATION_NAME IN (SELECT RDB$RELATION_NAME FROM RDB$RELATION_FIELDS WHERE RDB$FIELD_SOURCE IN (SELECT RDB$FIELD_NAME FROM RDB$FIELDS WHERE RDB$FIELD_NAME LIKE '%{1}%'))", Sqlcommandtype.getlistoftablesfromotherschema),

                // Additional systems like Hana, Snowflake, etc.:
                // Hana
                new QuerySqlRepo(DataSourceType.Hana, "SELECT TABLE_NAME FROM TABLES WHERE SCHEMA_NAME = '{1}'", Sqlcommandtype.getlistoftablesfromotherschema),

                // Snowflake
                new QuerySqlRepo(DataSourceType.SnowFlake, "SHOW TABLES IN SCHEMA {1}", Sqlcommandtype.getlistoftablesfromotherschema),

                // TerraData
                new QuerySqlRepo(DataSourceType.TerraData, "SELECT TableName FROM DBC.TablesV WHERE TableKind = 'T' AND DatabaseName = '{1}'", Sqlcommandtype.getlistoftablesfromotherschema),

                // Google BigQuery
                new QuerySqlRepo(DataSourceType.GoogleBigQuery, "SELECT table_name FROM `{1}.INFORMATION_SCHEMA.TABLES`", Sqlcommandtype.getlistoftablesfromotherschema),

                // Vertica
                new QuerySqlRepo(DataSourceType.Vertica, "SELECT table_name FROM v_catalog.tables WHERE table_schema = '{1}'", Sqlcommandtype.getlistoftablesfromotherschema),

                // Oracle
                new QuerySqlRepo(DataSourceType.Oracle, "SELECT * FROM {0} {2}", Sqlcommandtype.getTable),
                new QuerySqlRepo(DataSourceType.Oracle, "SELECT TABLE_NAME FROM user_tables", Sqlcommandtype.getlistoftables),
                new QuerySqlRepo(DataSourceType.Oracle, "SELECT cols.column_name FROM all_constraints cons, all_cons_columns cols WHERE cols.table_name = '{0}' AND cons.constraint_type = 'P' AND cons.constraint_name = cols.constraint_name AND cons.owner = cols.owner", Sqlcommandtype.getPKforTable),
                new QuerySqlRepo(DataSourceType.Oracle, "SELECT a.constraint_name, a.column_name, a.table_name FROM all_cons_columns a JOIN all_constraints c ON a.constraint_name = c.constraint_name WHERE c.constraint_type = 'R' AND a.table_name = '{0}'", Sqlcommandtype.getFKforTable),
                new QuerySqlRepo(DataSourceType.Oracle, "SELECT table_name FROM all_constraints WHERE r_constraint_name IN (SELECT constraint_name FROM all_constraints WHERE table_name = '{0}' AND constraint_type = 'P')", Sqlcommandtype.getChildTable),
                new QuerySqlRepo(DataSourceType.Oracle, "SELECT r.table_name FROM all_constraints c JOIN all_constraints r ON c.r_constraint_name = r.constraint_name WHERE c.table_name = '{0}' AND c.constraint_type = 'R'", Sqlcommandtype.getParentTable),
                
                // SQL Server
                new QuerySqlRepo(DataSourceType.SqlServer, "SELECT * FROM {0} {2}", Sqlcommandtype.getTable),
                new QuerySqlRepo(DataSourceType.SqlServer, "select table_name from Information_schema.Tables where Table_type='BASE TABLE'", Sqlcommandtype.getlistoftables),
                new QuerySqlRepo(DataSourceType.SqlServer, "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE TABLE_NAME = '{0}' AND CONSTRAINT_NAME LIKE 'PK%'", Sqlcommandtype.getPKforTable),
                new QuerySqlRepo(DataSourceType.SqlServer, "SELECT FK.COLUMN_NAME, FK.TABLE_NAME FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE FK INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS TC ON FK.CONSTRAINT_NAME = TC.CONSTRAINT_NAME WHERE TC.CONSTRAINT_TYPE = 'FOREIGN KEY' AND FK.TABLE_NAME = '{0}'", Sqlcommandtype.getFKforTable),
                new QuerySqlRepo(DataSourceType.SqlServer, "SELECT FK.TABLE_NAME FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS RC INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE FK ON RC.CONSTRAINT_NAME = FK.CONSTRAINT_NAME WHERE RC.UNIQUE_CONSTRAINT_NAME = (SELECT CONSTRAINT_NAME FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE TABLE_NAME = '{0}' AND CONSTRAINT_TYPE = 'PRIMARY KEY')", Sqlcommandtype.getChildTable),
                new QuerySqlRepo(DataSourceType.SqlServer, "SELECT RC.UNIQUE_CONSTRAINT_TABLE_NAME FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS RC WHERE RC.CONSTRAINT_NAME IN (SELECT CONSTRAINT_NAME FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE TABLE_NAME = '{0}')", Sqlcommandtype.getParentTable),
                
                // MySQL
                new QuerySqlRepo(DataSourceType.Mysql, "SELECT * FROM {0} {2}", Sqlcommandtype.getTable),
                new QuerySqlRepo(DataSourceType.Mysql, "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{1}'", Sqlcommandtype.getlistoftables),
                new QuerySqlRepo(DataSourceType.Mysql, "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE TABLE_NAME = '{0}' AND TABLE_SCHEMA = '{1}' AND CONSTRAINT_NAME LIKE 'PRIMARY'", Sqlcommandtype.getPKforTable),
                new QuerySqlRepo(DataSourceType.Mysql, "SELECT COLUMN_NAME AS child_column, REFERENCED_COLUMN_NAME AS parent_column, REFERENCED_TABLE_NAME AS parent_table FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE TABLE_SCHEMA = 'YourSchema' AND TABLE_NAME = '{0}' AND REFERENCED_TABLE_NAME IS NOT NULL", Sqlcommandtype.getFKforTable),
                new QuerySqlRepo(DataSourceType.Mysql, "SELECT TABLE_NAME AS child_table FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE TABLE_SCHEMA = 'YourSchema' AND REFERENCED_TABLE_NAME = '{0}'", Sqlcommandtype.getChildTable),
                new QuerySqlRepo(DataSourceType.Mysql, "SELECT REFERENCED_TABLE_NAME AS parent_table FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE TABLE_SCHEMA = 'YourSchema' AND TABLE_NAME = '{0}' AND REFERENCED_TABLE_NAME IS NOT NULL", Sqlcommandtype.getParentTable),

                // PostgreSQL
                new QuerySqlRepo(DataSourceType.Postgre, "SELECT * FROM {0} {2}", Sqlcommandtype.getTable),
                new QuerySqlRepo(DataSourceType.Postgre, "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'", Sqlcommandtype.getlistoftables),
                new QuerySqlRepo(DataSourceType.Postgre, "SELECT a.attname FROM pg_index i JOIN pg_attribute a ON a.attnum = ANY(i.indkey) WHERE i.indrelid = '{0}'::regclass AND i.indisprimary", Sqlcommandtype.getPKforTable),
                new QuerySqlRepo(DataSourceType.Postgre, "SELECT conname AS constraint_name, a.attname AS child_column, af.attname AS parent_column, cl.relname AS parent_table FROM pg_attribute a JOIN pg_attribute af ON a.attnum = ANY(pg_constraint.confkey) JOIN pg_class cl ON pg_constraint.confrelid = cl.oid JOIN pg_constraint ON a.attnum = ANY(pg_constraint.conkey) WHERE a.attnum > 0 AND pg_constraint.conrelid = '{0}'::regclass", Sqlcommandtype.getFKforTable),
                new QuerySqlRepo(DataSourceType.Postgre, "SELECT conname AS constraint_name, cl.relname AS child_table FROM pg_constraint JOIN pg_class cl ON pg_constraint.conrelid = cl.oid WHERE confrelid = '{0}'::regclass", Sqlcommandtype.getChildTable),
                new QuerySqlRepo(DataSourceType.Postgre, "SELECT confrelid::regclass AS parent_table FROM pg_constraint WHERE conrelid = '{0}'::regclass", Sqlcommandtype.getParentTable),

                // SQLite
                new QuerySqlRepo(DataSourceType.SqlLite, "SELECT * FROM {0} {2}", Sqlcommandtype.getTable),
                new QuerySqlRepo(DataSourceType.SqlLite, "SELECT name table_name FROM sqlite_master WHERE type='table'", Sqlcommandtype.getlistoftables),
                new QuerySqlRepo(DataSourceType.SqlLite, "PRAGMA table_info({0})", Sqlcommandtype.getPKforTable),

                // DuckDB
                new QuerySqlRepo(DataSourceType.DuckDB, "SELECT * FROM {0} {2}", Sqlcommandtype.getTable),
                new QuerySqlRepo(DataSourceType.DuckDB, "SELECT name FROM information_schema.tables WHERE type='table'", Sqlcommandtype.getlistoftables),
                new QuerySqlRepo(DataSourceType.DuckDB, "SELECT column_name FROM pragma_table_info('{0}') WHERE pk != 0;", Sqlcommandtype.getPKforTable),

                // DB2
                new QuerySqlRepo(DataSourceType.DB2, "SELECT * FROM {0} {2}", Sqlcommandtype.getTable),
                new QuerySqlRepo(DataSourceType.DB2, "SELECT TABNAME FROM SYSCAT.TABLES WHERE TABSCHEMA = CURRENT SCHEMA", Sqlcommandtype.getlistoftables),
                new QuerySqlRepo(DataSourceType.DB2, "SELECT COLNAME COLUMN_NAME FROM SYSCAT.KEYCOLUSE WHERE TABNAME = '{0}' AND CONSTRAINTNAME LIKE 'PK%'", Sqlcommandtype.getPKforTable),
                new QuerySqlRepo(DataSourceType.DB2, "SELECT FK_COLNAMES AS child_column, PK_COLNAMES AS parent_column, PK_TBNAME AS parent_table FROM SYSIBM.SQLFOREIGNKEYS WHERE FK_TBNAME = '{0}'", Sqlcommandtype.getFKforTable),
                new QuerySqlRepo(DataSourceType.DB2, "SELECT FK_TBNAME AS child_table FROM SYSIBM.SQLFOREIGNKEYS WHERE PK_TBNAME = '{0}'", Sqlcommandtype.getChildTable),
                new QuerySqlRepo(DataSourceType.DB2, "SELECT PK_TBNAME AS parent_table FROM SYSIBM.SQLFOREIGNKEYS WHERE FK_TBNAME = '{0}'", Sqlcommandtype.getParentTable),

                // MongoDB
                new QuerySqlRepo(DataSourceType.MongoDB, "db.{0}.find({})", Sqlcommandtype.getTable), // Get all documents from a collection
                new QuerySqlRepo(DataSourceType.MongoDB, "db.getCollectionNames()", Sqlcommandtype.getlistoftables), // Get all collection names

                // Redis
                new QuerySqlRepo(DataSourceType.Redis, "GET {0}", Sqlcommandtype.getTable), // Get the value of a key
// There's no direct equivalent of tables or foreign keys in Redis

                // Cassandra
                new QuerySqlRepo(DataSourceType.Cassandra, "SELECT * FROM {0}", Sqlcommandtype.getTable), // Get all rows from a table
                new QuerySqlRepo(DataSourceType.Cassandra, "SELECT table_name FROM system_schema.tables WHERE keyspace_name = 'YourKeyspaceName'", Sqlcommandtype.getlistoftables), // Get list of tables
                new QuerySqlRepo(DataSourceType.Cassandra, "SELECT column_name FROM system_schema.columns WHERE table_name = '{0}' AND keyspace_name = 'YourKeyspaceName' AND kind = 'partition_key'", Sqlcommandtype.getPKforTable), // Get PK for a table

                // Firebird
                new QuerySqlRepo(DataSourceType.FireBird, "SELECT * FROM {0}", Sqlcommandtype.getTable), // Get all rows from a table
                new QuerySqlRepo(DataSourceType.FireBird, "SELECT RDB$RELATION_NAME FROM RDB$RELATIONS WHERE RDB$SYSTEM_FLAG = 0", Sqlcommandtype.getlistoftables), // Get list of tables
                new QuerySqlRepo(DataSourceType.FireBird, "SELECT RDB$INDEX_SEGMENTS.RDB$FIELD_NAME FROM RDB$INDEX_SEGMENTS JOIN RDB$RELATION_CONSTRAINTS ON RDB$INDEX_SEGMENTS.RDB$INDEX_NAME = RDB$RELATION_CONSTRAINTS.RDB$INDEX_NAME WHERE RDB$RELATION_CONSTRAINTS.RDB$RELATION_NAME = '{0}' AND RDB$RELATION_CONSTRAINTS.RDB$CONSTRAINT_TYPE = 'PRIMARY KEY'", Sqlcommandtype.getPKforTable), // Get PK for a table

                // Couchbase
                new QuerySqlRepo(DataSourceType.Couchbase, "SELECT * FROM `{0}`", Sqlcommandtype.getTable), // Get all documents from a bucket
                new QuerySqlRepo(DataSourceType.Couchbase, "SELECT name FROM system:keyspaces", Sqlcommandtype.getlistoftables), // Get list of keyspaces/buckets

                // Hana
                new QuerySqlRepo(DataSourceType.Hana, "SELECT * FROM {0}", Sqlcommandtype.getTable), // Get all rows from a table
                new QuerySqlRepo(DataSourceType.Hana, "SELECT TABLE_NAME FROM TABLES WHERE SCHEMA_NAME = 'YOUR_SCHEMA_NAME'", Sqlcommandtype.getlistoftables), // Get list of tables
                new QuerySqlRepo(DataSourceType.Hana, "SELECT COLUMN_NAME FROM CONSTRAINTS WHERE TABLE_NAME = '{0}' AND SCHEMA_NAME = 'YOUR_SCHEMA_NAME' AND IS_PRIMARY_KEY = 'TRUE'", Sqlcommandtype.getPKforTable), // Get PK for a table

                // Vertica
                new QuerySqlRepo(DataSourceType.Vertica, "SELECT * FROM {0}", Sqlcommandtype.getTable), // Get all rows from a table
                new QuerySqlRepo(DataSourceType.Vertica, "SELECT table_name FROM v_catalog.tables WHERE table_schema = 'YOUR_SCHEMA_NAME'", Sqlcommandtype.getlistoftables), // Get list of tables
                new QuerySqlRepo(DataSourceType.Vertica, "SELECT column_name FROM v_catalog.primary_keys WHERE table_name = '{0}' AND table_schema = 'YOUR_SCHEMA_NAME'", Sqlcommandtype.getPKforTable), // Get PK for a table

                // TerraData
                new QuerySqlRepo(DataSourceType.TerraData, "SELECT * FROM {0}", Sqlcommandtype.getTable), // Get all rows from a table
                new QuerySqlRepo(DataSourceType.TerraData, "SELECT TableName Table_name FROM DBC.TablesV WHERE TableKind = 'T' AND DatabaseName = '{1}'", Sqlcommandtype.getlistoftablesfromotherschema), // Get list of tables
                new QuerySqlRepo(DataSourceType.TerraData, "SELECT ColumnName Column_name FROM DBC.IndicesV WHERE TableName = '{0}' AND DatabaseName = '{1}' AND IndexType = 'P'", Sqlcommandtype.getPKforTable), // Get PK for a table

                // Azure Cloud
                new QuerySqlRepo(DataSourceType.AzureCloud, "SELECT * FROM {0}", Sqlcommandtype.getTable), // Get all rows from a table
                new QuerySqlRepo(DataSourceType.AzureCloud, "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'", Sqlcommandtype.getlistoftables), // Get list of tables
                new QuerySqlRepo(DataSourceType.AzureCloud, "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE OBJECTPROPERTY(OBJECT_ID(CONTRACT_SCHEMA + '.' + CONSTRAINT_NAME), 'IsPrimaryKey') = 1 AND TABLE_NAME = '{0}'", Sqlcommandtype.getPKforTable), // Get PK for a table

                // Google BigQuery
                new QuerySqlRepo(DataSourceType.GoogleBigQuery, "SELECT * FROM {0}", Sqlcommandtype.getTable), // Get all rows from a table
                new QuerySqlRepo(DataSourceType.GoogleBigQuery, "SELECT table_name FROM `YOUR_DATASET.INFORMATION_SCHEMA.TABLES`", Sqlcommandtype.getlistoftables), // Get list of tables
// BigQuery does not have a traditional concept of primary keys, but you can query the schema to find fields that might act as a key

                // Snowflake
                new QuerySqlRepo(DataSourceType.SnowFlake, "SELECT * FROM {0}", Sqlcommandtype.getTable), // Get all rows from a table
                new QuerySqlRepo(DataSourceType.SnowFlake, "SELECT TABLE_NAME FROM {1}.INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'", Sqlcommandtype.getlistoftables),
                new QuerySqlRepo(DataSourceType.SnowFlake, "SELECT kcu.COLUMN_NAME FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu ON tc.TABLE_NAME = kcu.TABLE_NAME AND tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME WHERE tc.TABLE_SCHEMA = '{1}' AND tc.TABLE_NAME = '{0}' AND tc.CONSTRAINT_TYPE = 'PRIMARY KEY'", Sqlcommandtype.getPKforTable),

                // ElasticSearch
                new QuerySqlRepo(DataSourceType.ElasticSearch, "SELECT * FROM {0}", Sqlcommandtype.getTable), // Executes a search query that, by default, retrieves the first 1000 documents.
                // Elasticsearch doesn't have the concept of tables or primary/foreign keys in the traditional sense.

                // InfluxDB
                new QuerySqlRepo(DataSourceType.InfluxDB, "SELECT * FROM {0}", Sqlcommandtype.getTable), // Query data from a measurement
                new QuerySqlRepo(DataSourceType.InfluxDB, "SHOW MEASUREMENTS", Sqlcommandtype.getlistoftables), // Get list of measurements (similar to tables)

                // DynamoDB
                new QuerySqlRepo(DataSourceType.DynamoDB, "Scan {0}", Sqlcommandtype.getTable), // Scan operation (be mindful of performance and cost)

                // TimeScale
                new QuerySqlRepo(DataSourceType.TimeScale, "SELECT * FROM {0}", Sqlcommandtype.getTable),
                new QuerySqlRepo(DataSourceType.TimeScale, "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'", Sqlcommandtype.getlistoftables),
                new QuerySqlRepo(DataSourceType.TimeScale, "SELECT a.attname column_name FROM pg_index i JOIN pg_attribute a ON a.attnum = ANY(i.indkey) WHERE i.indrelid = '{0}'::regclass AND i.indisprimary", Sqlcommandtype.getPKforTable), // Get PK for a table

                // Cockroach
                new QuerySqlRepo(DataSourceType.Cockroach, "SELECT * FROM {0}", Sqlcommandtype.getTable), // Get all rows from a table
                new QuerySqlRepo(DataSourceType.Cockroach, "SHOW TABLES", Sqlcommandtype.getlistoftables), // Get list of tables
                new QuerySqlRepo(DataSourceType.Cockroach, "SELECT column_name FROM information_schema.columns WHERE table_name = '{0}' AND is_nullable = 'NO'", Sqlcommandtype.getPKforTable), // Get PK for a table (simplified)

                // Kafka
                new QuerySqlRepo(DataSourceType.Kafka, "LIST TOPICS", Sqlcommandtype.getlistoftables), // Conceptual command to list topics

                // OPC
                new QuerySqlRepo(DataSourceType.OPC, "READ NODE", Sqlcommandtype.getTable), // Conceptual command to read data from an OPC node

                // Excel/CSV Query Sql Set
                new QuerySqlRepo(DataSourceType.Xls, "select * from [{0}] {2}", Sqlcommandtype.getTable),
                new QuerySqlRepo(DataSourceType.Text, "select * from [{0}] {2}", Sqlcommandtype.getTable)
            };
        }

        private static readonly Dictionary<(DataSourceType, Sqlcommandtype), string> QueryCache =
            new Dictionary<(DataSourceType, Sqlcommandtype), string>();

        static RDBMSHelper()
        {
            // Initialize the query cache with queries from CreateQuerySqlRepos
            var queries = CreateQuerySqlRepos();
            foreach (var query in queries)
            {
                QueryCache[(query.DatabaseType, query.Sqltype)] = query.Sql;
            }
        }

        public static string GetQuery(DataSourceType dataSourceType, Sqlcommandtype queryType)
        {
            if (QueryCache.TryGetValue((dataSourceType, queryType), out string query))
                return query;

            return string.Empty;
        }

        public static string SafeQuote(string value, DataSourceType dataSourceType)
        {
            switch (dataSourceType)
            {
                case DataSourceType.Oracle:
                case DataSourceType.SqlServer:
                case DataSourceType.Mysql:
                    return value?.Replace("'", "''");
                case DataSourceType.Postgre:
                    return value?.Replace("'", "''").Replace("\\", "\\\\");
                default:
                    return value?.Replace("'", "''");
            }
        }

        /// <summary>Generates SQL to create a table based on an EntityStructure.</summary>
        /// <param name="entity">The EntityStructure containing entity definition</param>
        /// <returns>A tuple containing the SQL statement, success flag, and any error message</returns>
        public static (string Sql, bool Success, string ErrorMessage) GenerateCreateTableSQL(EntityStructure entity)
        {
            try
            {
                var sql = GenerateCreateTableSQLInternal(entity);
                return (sql, true, string.Empty);
            }
            catch (Exception ex)
            {
                return (string.Empty, false, ex.Message);
            }
        }

        private static string GenerateCreateTableSQLInternal(EntityStructure entity)
        {
            if (entity?.Fields == null || !entity.Fields.Any())
                throw new ArgumentException("Entity has no fields defined");

            var tableName = entity.EntityName;
            var columns = new List<string>();

            foreach (var field in entity.Fields)
            {
                var columnDef = GenerateColumnDefinition(field, entity.DatabaseType);
                columns.Add(columnDef);
            }

            var sql = $"CREATE TABLE {tableName} ({string.Join(", ", columns)})";
            return sql;
        }

        private static string GenerateColumnDefinition(EntityField field, DataSourceType databaseType)
        {
            var dataType = MapDataType(field.fieldtype, databaseType);
            var nullable = field.AllowDBNull ? "" : " NOT NULL";
            var identity = field.IsAutoIncrement ? GetIdentityClause(databaseType) : "";
            
            return $"{field.fieldname} {dataType}{nullable}{identity}";
        }

        private static string MapDataType(string fieldType, DataSourceType databaseType)
        {
            if (string.IsNullOrEmpty(fieldType))
                return "VARCHAR(255)"; // Default fallback

            // Normalize the field type
            string normalizedType = fieldType.ToUpper().Trim();
            
            // Remove System. prefix if present for mapping purposes
            string baseType = normalizedType.StartsWith("SYSTEM.") ? normalizedType.Substring(7) : normalizedType;

            // Try to get mappings from DataTypeFieldMappingHelper first
            var mappings = DataTypeFieldMappingHelper.GetDataTypes(databaseType, null);
            if (mappings != null && mappings.Any())
            {
                // Look for exact .NET type match
                var exactMatch = mappings.FirstOrDefault(m => 
                    m.NetDataType.Equals(fieldType, StringComparison.OrdinalIgnoreCase) && m.Fav);
                
                if (exactMatch == null)
                {
                    exactMatch = mappings.FirstOrDefault(m => 
                        m.NetDataType.Equals(fieldType, StringComparison.OrdinalIgnoreCase));
                }

                if (exactMatch != null)
                {
                    return exactMatch.DataType;
                }

                // Try to match common type patterns
                var patternMatch = mappings.FirstOrDefault(m => 
                    m.NetDataType.EndsWith("." + baseType, StringComparison.OrdinalIgnoreCase) && m.Fav);
                
                if (patternMatch == null)
                {
                    patternMatch = mappings.FirstOrDefault(m => 
                        m.NetDataType.EndsWith("." + baseType, StringComparison.OrdinalIgnoreCase));
                }

                if (patternMatch != null)
                {
                    return patternMatch.DataType;
                }
            }

            // Fallback to original mapping logic if DataTypeFieldMappingHelper doesn't have the mapping
            return databaseType switch
            {
                DataSourceType.SqlServer => baseType switch
                {
                    "STRING" or "TEXT" => "NVARCHAR(MAX)",
                    "INT" or "INTEGER" or "INT32" => "INT",
                    "LONG" or "INT64" => "BIGINT",
                    "SHORT" or "INT16" => "SMALLINT",
                    "BYTE" => "TINYINT",
                    "DECIMAL" or "DOUBLE" => "DECIMAL(18,2)",
                    "SINGLE" or "FLOAT" => "REAL",
                    "DATETIME" => "DATETIME2",
                    "DATETIMEOFFSET" => "DATETIMEOFFSET",
                    "TIMESPAN" => "TIME",
                    "BOOL" or "BOOLEAN" => "BIT",
                    "GUID" => "UNIQUEIDENTIFIER",
                    "BYTE[]" => "VARBINARY(MAX)",
                    "CHAR" => "NCHAR(1)",
                    _ => "NVARCHAR(255)"
                },
                DataSourceType.Mysql => baseType switch
                {
                    "STRING" or "TEXT" => "TEXT",
                    "INT" or "INTEGER" or "INT32" => "INT",
                    "LONG" or "INT64" => "BIGINT",
                    "SHORT" or "INT16" => "SMALLINT",
                    "BYTE" => "TINYINT UNSIGNED",
                    "DECIMAL" or "DOUBLE" => "DECIMAL(18,2)",
                    "SINGLE" or "FLOAT" => "FLOAT",
                    "DATETIME" => "DATETIME",
                    "TIMESPAN" => "TIME",
                    "BOOL" or "BOOLEAN" => "BOOLEAN",
                    "GUID" => "CHAR(36)",
                    "BYTE[]" => "LONGBLOB",
                    "CHAR" => "CHAR(1)",
                    _ => "VARCHAR(255)"
                },
                DataSourceType.Postgre => baseType switch
                {
                    "STRING" or "TEXT" => "TEXT",
                    "INT" or "INTEGER" or "INT32" => "INTEGER",
                    "LONG" or "INT64" => "BIGINT",
                    "SHORT" or "INT16" => "SMALLINT",
                    "BYTE" => "SMALLINT",
                    "DECIMAL" => "NUMERIC(18,2)",
                    "DOUBLE" => "DOUBLE PRECISION",
                    "SINGLE" or "FLOAT" => "REAL",
                    "DATETIME" => "TIMESTAMP",
                    "DATETIMEOFFSET" => "TIMESTAMPTZ",
                    "TIMESPAN" => "INTERVAL",
                    "BOOL" or "BOOLEAN" => "BOOLEAN",
                    "GUID" => "UUID",
                    "BYTE[]" => "BYTEA",
                    "CHAR" => "CHAR(1)",
                    _ => "VARCHAR(255)"
                },
                DataSourceType.Oracle => baseType switch
                {
                    "STRING" or "TEXT" => "NVARCHAR2(4000)",
                    "INT" or "INTEGER" or "INT32" => "NUMBER(10)",
                    "LONG" or "INT64" => "NUMBER(19)",
                    "SHORT" or "INT16" => "NUMBER(5)",
                    "BYTE" => "NUMBER(3)",
                    "DECIMAL" => "NUMBER(18,2)",
                    "DOUBLE" or "SINGLE" or "FLOAT" => "BINARY_DOUBLE",
                    "DATETIME" => "TIMESTAMP",
                    "BOOL" or "BOOLEAN" => "NUMBER(1)",
                    "GUID" => "RAW(16)",
                    "BYTE[]" => "BLOB",
                    "CHAR" => "NCHAR(1)",
                    _ => "NVARCHAR2(255)"
                },
                DataSourceType.SqlLite => baseType switch
                {
                    "STRING" or "TEXT" => "TEXT",
                    "INT" or "INTEGER" or "INT32" or "LONG" or "INT64" or "SHORT" or "INT16" or "BYTE" => "INTEGER",
                    "DECIMAL" or "DOUBLE" or "SINGLE" or "FLOAT" => "REAL",
                    "DATETIME" => "TEXT", // SQLite stores dates as text
                    "BOOL" or "BOOLEAN" => "INTEGER",
                    "GUID" => "TEXT",
                    "BYTE[]" => "BLOB",
                    "CHAR" => "TEXT",
                    _ => "TEXT"
                },
                DataSourceType.DB2 => baseType switch
                {
                    "STRING" or "TEXT" => "VARCHAR(255)",
                    "INT" or "INTEGER" or "INT32" => "INTEGER",
                    "LONG" or "INT64" => "BIGINT",
                    "SHORT" or "INT16" => "SMALLINT",
                    "BYTE" => "SMALLINT",
                    "DECIMAL" => "DECIMAL(18,2)",
                    "DOUBLE" => "DOUBLE",
                    "SINGLE" or "FLOAT" => "REAL",
                    "DATETIME" => "TIMESTAMP",
                    "BOOL" or "BOOLEAN" => "SMALLINT",
                    "GUID" => "CHAR(36)",
                    "BYTE[]" => "BLOB",
                    "CHAR" => "CHAR(1)",
                    _ => "VARCHAR(255)"
                },
                DataSourceType.FireBird => baseType switch
                {
                    "STRING" or "TEXT" => "VARCHAR(255)",
                    "INT" or "INTEGER" or "INT32" => "INTEGER",
                    "LONG" or "INT64" => "BIGINT",
                    "SHORT" or "INT16" => "SMALLINT",
                    "BYTE" => "SMALLINT",
                    "DECIMAL" => "DECIMAL(18,2)",
                    "DOUBLE" => "DOUBLE PRECISION",
                    "SINGLE" or "FLOAT" => "FLOAT",
                    "DATETIME" => "TIMESTAMP",
                    "BOOL" or "BOOLEAN" => "SMALLINT",
                    "GUID" => "CHAR(36)",
                    "BYTE[]" => "BLOB",
                    "CHAR" => "CHAR(1)",
                    _ => "VARCHAR(255)"
                },
                DataSourceType.SnowFlake => baseType switch
                {
                    "STRING" or "TEXT" => "VARCHAR",
                    "INT" or "INTEGER" or "INT32" => "INTEGER",
                    "LONG" or "INT64" => "INTEGER",
                    "SHORT" or "INT16" => "INTEGER",
                    "BYTE" => "INTEGER",
                    "DECIMAL" => "DECIMAL(18,2)",
                    "DOUBLE" or "SINGLE" or "FLOAT" => "FLOAT",
                    "DATETIME" => "TIMESTAMP_NTZ",
                    "DATETIMEOFFSET" => "TIMESTAMP_TZ",
                    "TIMESPAN" => "TIME",
                    "BOOL" or "BOOLEAN" => "BOOLEAN",
                    "GUID" => "VARCHAR(36)",
                    "BYTE[]" => "BINARY",
                    "CHAR" => "CHAR(1)",
                    _ => "VARCHAR"
                },
                DataSourceType.Cockroach => baseType switch
                {
                    "STRING" or "TEXT" => "STRING",
                    "INT" or "INTEGER" or "INT32" => "INT4",
                    "LONG" or "INT64" => "INT8",
                    "SHORT" or "INT16" => "INT2",
                    "BYTE" => "INT2",
                    "DECIMAL" => "DECIMAL(18,2)",
                    "DOUBLE" or "SINGLE" or "FLOAT" => "FLOAT8",
                    "DATETIME" => "TIMESTAMP",
                    "TIMESPAN" => "INTERVAL",
                    "BOOL" or "BOOLEAN" => "BOOL",
                    "GUID" => "UUID",
                    "BYTE[]" => "BYTES",
                    "CHAR" => "CHAR(1)",
                    _ => "STRING"
                },
                DataSourceType.MongoDB => baseType switch
                {
                    "STRING" or "TEXT" => "String",
                    "INT" or "INTEGER" or "INT32" => "Int32",
                    "LONG" or "INT64" => "Int64",
                    "DECIMAL" or "DOUBLE" => "Double",
                    "DATETIME" => "DateTime",
                    "BOOL" or "BOOLEAN" => "Boolean",
                    "GUID" => "String",
                    "BYTE[]" => "Binary",
                    _ => "String"
                },
                // Add more database types as needed
                _ => baseType switch
                {
                    "STRING" or "TEXT" => "VARCHAR(255)",
                    "INT" or "INTEGER" or "INT32" => "INTEGER",
                    "LONG" or "INT64" => "BIGINT",
                    "DECIMAL" or "DOUBLE" => "DECIMAL(18,2)",
                    "DATETIME" => "TIMESTAMP",
                    "BOOL" or "BOOLEAN" => "BOOLEAN",
                    "BYTE[]" => "BLOB",
                    _ => "VARCHAR(255)"
                }
            };
        }

        /// <summary>Generates SQL to add a primary key to a table based on its entity structure.</summary>
        /// <param name="entity">The entity structure containing table and primary key information.</param>
        /// <returns>A tuple containing the SQL statement, success flag, and error message (if any).</returns>
        public static (string Sql, bool Success, string ErrorMessage) GeneratePrimaryKeyFromEntity(EntityStructure entity)
        {
            try
            {
                if (entity == null)
                    throw new ArgumentNullException(nameof(entity), "Entity structure must be provided.");

                if (string.IsNullOrWhiteSpace(entity.EntityName))
                    return (string.Empty, false, "Entity name cannot be empty.");

                // Determine the primary key field(s)
                var primaryKeyFields = entity.Fields.Where(f => f.IsKey).ToList();

                if (!primaryKeyFields.Any())
                    return (string.Empty, false, "No primary key defined for the entity.");

                // Generate the primary key query for each field (assuming composite keys are not used for simplicity)
                var queries = new List<string>();
                foreach (var primaryKeyField in primaryKeyFields)
                {
                    string query = GeneratePrimaryKeyQuery(entity.DatabaseType, entity.EntityName, primaryKeyField.fieldname, MapDataType(primaryKeyField.fieldtype, entity.DatabaseType));
                    queries.Add(query);
                }

                // Combine queries with "; " for execution
                string combinedQuery = string.Join("; ", queries);

                return (combinedQuery, true, string.Empty);
            }
            catch (Exception ex)
            {
                return (string.Empty, false, ex.Message);
            }
        }

        /// <summary>
        /// Generates SQL to create a unique index on a table based on its entity structure.
        /// </summary>
        /// <param name="entity">The entity structure containing table and index information.</param>
        /// <returns>A tuple containing the SQL statement, success flag, and error message (if any).</returns>
        public static (string Sql, bool Success, string ErrorMessage) GenerateUniqueIndexFromEntity(EntityStructure entity)
        {
            try
            {
                if (entity == null)
                    throw new ArgumentNullException(nameof(entity), "Entity structure must be provided.");

                if (string.IsNullOrWhiteSpace(entity.EntityName))
                    return (string.Empty, false, "Entity name cannot be empty.");

                // Determine the unique fields (assuming unique index is based on one field for simplicity)
                var uniqueFields = entity.Fields.Where(f => f.IsUnique).ToList();

                if (!uniqueFields.Any())
                    return (string.Empty, false, "No unique fields defined for the entity.");

                // Generate the unique index query for each field
                var queries = new List<string>();
                foreach (var uniqueField in uniqueFields)
                {
                    string query = GenerateCreateIndexQuery(entity.DatabaseType, entity.EntityName, $"{entity.EntityName}_{uniqueField.fieldname}_ux", new[] { uniqueField.fieldname }, new Dictionary<string, object> { { "unique", true } });
                    queries.Add(query);
                }

                // Combine queries with "; " for execution
                string combinedQuery = string.Join("; ", queries);

                return (combinedQuery, true, string.Empty);
            }
            catch (Exception ex)
            {
                return (string.Empty, false, ex.Message);
            }
        }
		/// <summary>Generates a SQL query to insert data into a table in a specific RDBMS.</summary>
		/// <param name="rdbms">The type of RDBMS.</param>
		/// <param name="tableName">The name of the table.</param>
		/// <param name="data">The data to insert, in key-value pair format.</param>
		/// <returns>A SQL query to insert the data into the specified table in the specified RDBMS.</returns>
		/// <exception cref="ArgumentException">Thrown when the specified RDBMS or table name is not supported.</exception>
		public static string GenerateInsertQuery(DataSourceType rdbms, string tableName, Dictionary<string, object> data)
		{
			if (string.IsNullOrWhiteSpace(tableName))
				throw new ArgumentException("Table name cannot be null or empty", nameof(tableName));

			if (data == null || data.Count == 0)
				throw new ArgumentException("No data provided for insertion", nameof(data));

			string query = "";

			switch (rdbms)
			{
				case DataSourceType.SqlServer:
					query = $"INSERT INTO {tableName} ({string.Join(", ", data.Keys)}) VALUES ({string.Join(", ", data.Keys.Select(k => "@" + k))})";
					break;
				case DataSourceType.Mysql:
					query = $"INSERT INTO {tableName} ({string.Join(", ", data.Keys)}) VALUES ({string.Join(", ", data.Values.Select(v => "'" + v.ToString().Replace("'", "''") + "'"))})";
					break;
				case DataSourceType.Postgre:
					query = $"INSERT INTO {tableName} ({string.Join(", ", data.Keys)}) VALUES ({string.Join(", ", data.Values.Select(v => "'" + v.ToString().Replace("'", "''") + "'"))})";
					break;
				case DataSourceType.Oracle:
					query = $"INSERT INTO {tableName} ({string.Join(", ", data.Keys)}) VALUES ({string.Join(", ", data.Values.Select(v => "'" + v.ToString().Replace("'", "''") + "'"))})";
					break;
				case DataSourceType.DB2:
					query = $"INSERT INTO {tableName} ({string.Join(", ", data.Keys)}) VALUES ({string.Join(", ", data.Values.Select(v => "'" + v.ToString().Replace("'", "''") + "'"))})";
					break;
				case DataSourceType.FireBird:
					query = $"INSERT INTO {tableName} ({string.Join(", ", data.Keys)}) VALUES ({string.Join(", ", data.Values.Select(v => "'" + v.ToString().Replace("'", "''") + "'"))})";
					break;
				case DataSourceType.SqlLite:
					query = $"INSERT INTO {tableName} ({string.Join(", ", data.Keys)}) VALUES ({string.Join(", ", data.Values.Select(v => "'" + v.ToString().Replace("'", "''") + "'"))})";
					break;
				case DataSourceType.Couchbase:
				case DataSourceType.Redis:
				case DataSourceType.MongoDB:
					query = "NoSQL databases typically do not use standard SQL INSERT statements.";
					break;
				default:
					query = "RDBMS not supported.";
					break;
			}

			return query;
		}
		/// <summary>Generates a SQL query to update data in a table in a specific RDBMS.</summary>
		/// <param name="rdbms">The type of RDBMS.</param>
		/// <param name="tableName">The name of the table.</param>
		/// <param name="data">The data to update, in key-value pair format.</param>
		/// <param name="conditions">The conditions for the update, in key-value pair format.</param>
		/// <returns>A SQL query to update the data in the specified table in the specified RDBMS.</returns>
		/// <exception cref="ArgumentException">Thrown when the specified RDBMS or table name is not supported.</exception>
		public static string GenerateUpdateQuery(DataSourceType rdbms, string tableName, Dictionary<string, object> data, Dictionary<string, object> conditions)
		{
			if (string.IsNullOrWhiteSpace(tableName))
				throw new ArgumentException("Table name cannot be null or empty", nameof(tableName));

			if (data == null || data.Count == 0)
				throw new ArgumentException("No data provided for update", nameof(data));

			if (conditions == null || conditions.Count == 0)
				throw new ArgumentException("No conditions provided for update", nameof(conditions));

			string query = "";

			switch (rdbms)
			{
				case DataSourceType.SqlServer:
					query = $"UPDATE {tableName} SET {string.Join(", ", data.Keys.Select(k => $"{k} = @{k}"))} WHERE {string.Join(" AND ", conditions.Keys.Select(k => $"{k} = @{k}"))}";
					break;
				case DataSourceType.Mysql:
					query = $"UPDATE {tableName} SET {string.Join(", ", data.Keys.Select(k => $"{k} = '{data[k].ToString().Replace("'", "''")}'"))} WHERE {string.Join(" AND ", conditions.Keys.Select(k => $"{k} = '{conditions[k].ToString().Replace("'", "''")}'"))}";
					break;
				case DataSourceType.Postgre:
					query = $"UPDATE {tableName} SET {string.Join(", ", data.Keys.Select(k => $"{k} = '{data[k].ToString().Replace("'", "''")}'"))} WHERE {string.Join(" AND ", conditions.Keys.Select(k => $"{k} = '{conditions[k].ToString().Replace("'", "''")}'"))}";
					break;
				case DataSourceType.Oracle:
					query = $"UPDATE {tableName} SET {string.Join(", ", data.Keys.Select(k => $"{k} = '{data[k].ToString().Replace("'", "''")}'"))} WHERE {string.Join(" AND ", conditions.Keys.Select(k => $"{k} = '{conditions[k].ToString().Replace("'", "''")}'"))}";
					break;
				case DataSourceType.DB2:
					query = $"UPDATE {tableName} SET {string.Join(", ", data.Keys.Select(k => $"{k} = '{data[k].ToString().Replace("'", "''")}'"))} WHERE {string.Join(" AND ", conditions.Keys.Select(k => $"{k} = '{conditions[k].ToString().Replace("'", "''")}'"))}";
					break;
				case DataSourceType.FireBird:
					query = $"UPDATE {tableName} SET {string.Join(", ", data.Keys.Select(k => $"{k} = '{data[k].ToString().Replace("'", "''")}'"))} WHERE {string.Join(" AND ", conditions.Keys.Select(k => $"{k} = '{conditions[k].ToString().Replace("'", "''")}'"))}";
					break;
				case DataSourceType.SqlLite:
					query = $"UPDATE {tableName} SET {string.Join(", ", data.Keys.Select(k => $"{k} = '{data[k].ToString().Replace("'", "''")}'"))} WHERE {string.Join(" AND ", conditions.Keys.Select(k => $"{k} = '{conditions[k].ToString().Replace("'", "''")}'"))}";
					break;
				case DataSourceType.Couchbase:
				case DataSourceType.Redis:
				case DataSourceType.MongoDB:
					query = "NoSQL databases typically do not use standard SQL UPDATE statements.";
					break;
				default:
					query = "RDBMS not supported.";
					break;
			}

			return query;
		}
		/// <summary>Generates a SQL query to delete data from a table in a specific RDBMS.</summary>
		/// <param name="rdbms">The type of RDBMS.</param>
		/// <param name="tableName">The name of the table.</param>
		/// <param name="conditions">The conditions for the deletion, in key-value pair format.</param>
		/// <returns>A SQL query to delete the data from the specified table in the specified RDBMS.</returns>
		/// <exception cref="ArgumentException">Thrown when the specified RDBMS or table name is not supported.</exception>
		public static string GenerateDeleteQuery(DataSourceType rdbms, string tableName, Dictionary<string, object> conditions)
		{
			if (string.IsNullOrWhiteSpace(tableName))
				throw new ArgumentException("Table name cannot be null or empty", nameof(tableName));

			if (conditions == null || conditions.Count == 0)
				throw new ArgumentException("No conditions provided for deletion", nameof(conditions));

			string query = "";

			switch (rdbms)
			{
				case DataSourceType.SqlServer:
					query = $"DELETE FROM {tableName} WHERE {string.Join(" AND ", conditions.Keys.Select(k => $"{k} = @{k}"))}";
					break;
				case DataSourceType.Mysql:
					query = $"DELETE FROM {tableName} WHERE {string.Join(" AND ", conditions.Keys.Select(k => $"{k} = '{conditions[k].ToString().Replace("'", "''")}'"))}";
					break;
				case DataSourceType.Postgre:
					query = $"DELETE FROM {tableName} WHERE {string.Join(" AND ", conditions.Keys.Select(k => $"{k} = '{conditions[k].ToString().Replace("'", "''")}'"))}";
					break;
				case DataSourceType.Oracle:
					query = $"DELETE FROM {tableName} WHERE {string.Join(" AND ", conditions.Keys.Select(k => $"{k} = '{conditions[k].ToString().Replace("'", "''")}'"))}";
					break;
				case DataSourceType.DB2:
					query = $"DELETE FROM {tableName} WHERE {string.Join(" AND ", conditions.Keys.Select(k => $"{k} = '{conditions[k].ToString().Replace("'", "''")}'"))}";
					break;
				case DataSourceType.FireBird:
					query = $"DELETE FROM {tableName} WHERE {string.Join(" AND ", conditions.Keys.Select(k => $"{k} = '{conditions[k].ToString().Replace("'", "''")}'"))}";
					break;
				case DataSourceType.SqlLite:
					query = $"DELETE FROM {tableName} WHERE {string.Join(" AND ", conditions.Keys.Select(k => $"{k} = '{conditions[k].ToString().Replace("'", "''")}'"))}";
					break;
				case DataSourceType.Couchbase:
				case DataSourceType.Redis:
				case DataSourceType.MongoDB:
					query = "NoSQL databases typically do not use standard SQL DELETE statements.";
					break;
				default:
					query = "RDBMS not supported.";
					break;
			}

			return query;
		}

        /// <summary>
        /// Gets the SQL syntax for paging results
        /// </summary>
        /// <param name="dataSourceType">Database type</param>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Number of records per page</param>
        /// <returns>SQL paging syntax</returns>
        public static string GetPagingSyntax(DataSourceType dataSourceType, int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            var offset = (pageNumber - 1) * pageSize;

            return dataSourceType switch
            {
                DataSourceType.SqlServer => $"OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY",
                DataSourceType.Mysql => $"LIMIT {pageSize} OFFSET {offset}",
                DataSourceType.Postgre => $"LIMIT {pageSize} OFFSET {offset}",
                DataSourceType.Oracle => $"OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY",
                DataSourceType.SqlLite => $"LIMIT {pageSize} OFFSET {offset}",
                DataSourceType.DB2 => $"OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY",
                DataSourceType.FireBird => $"ROWS {offset + 1} TO {offset + pageSize}",
                DataSourceType.SnowFlake => $"LIMIT {pageSize} OFFSET {offset}",
                DataSourceType.Cockroach => $"LIMIT {pageSize} OFFSET {offset}",
                DataSourceType.Vertica => $"LIMIT {pageSize} OFFSET {offset}",
                DataSourceType.GoogleBigQuery => $"LIMIT {pageSize} OFFSET {offset}",
                DataSourceType.AWSRedshift => $"LIMIT {pageSize} OFFSET {offset}",
                DataSourceType.ClickHouse => $"LIMIT {pageSize} OFFSET {offset}",
                _ => $"LIMIT {pageSize} OFFSET {offset}" // Default to common syntax
            };
        }

        /// <summary>
        /// Generates SQL to drop an entity
        /// </summary>
        /// <param name="dataSourceType">Database type</param>
        /// <param name="entityName">Name of the entity to drop</param>
        /// <returns>SQL statement to drop the entity</returns>
        public static string GetDropEntity(DataSourceType dataSourceType, string entityName)
        {
            if (string.IsNullOrWhiteSpace(entityName))
                throw new ArgumentException("Entity name cannot be null or empty", nameof(entityName));

            return dataSourceType switch
            {
                DataSourceType.SqlServer => $"DROP TABLE IF EXISTS {entityName}",
                DataSourceType.Mysql => $"DROP TABLE IF EXISTS {entityName}",
                DataSourceType.Postgre => $"DROP TABLE IF EXISTS {entityName}",
                DataSourceType.Oracle => $"DROP TABLE {entityName}",
                DataSourceType.SqlLite => $"DROP TABLE IF EXISTS {entityName}",
                DataSourceType.DB2 => $"DROP TABLE {entityName}",
                DataSourceType.FireBird => $"DROP TABLE {entityName}",
                DataSourceType.SnowFlake => $"DROP TABLE IF EXISTS {entityName}",
                DataSourceType.Cockroach => $"DROP TABLE IF EXISTS {entityName}",
                DataSourceType.Vertica => $"DROP TABLE IF EXISTS {entityName}",
                DataSourceType.GoogleBigQuery => $"DROP TABLE IF EXISTS {entityName}",
                DataSourceType.AWSRedshift => $"DROP TABLE IF EXISTS {entityName}",
                DataSourceType.ClickHouse => $"DROP TABLE IF EXISTS {entityName}",
                _ => $"DROP TABLE {entityName}"
            };
        }

        /// <summary>
        /// Generates a query to create an index
        /// </summary>
        /// <param name="databaseType">Database type</param>
        /// <param name="tableName">Name of the table</param>
        /// <param name="indexName">Name of the index</param>
        /// <param name="columns">Array of column names</param>
        /// <param name="options">Optional index creation options</param>
        /// <returns>SQL statement to create the index</returns>
        public static string GenerateCreateIndexQuery(DataSourceType databaseType, string tableName, string indexName, 
            string[] columns, Dictionary<string, object> options = null)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Table name cannot be null or empty", nameof(tableName));
            
            if (string.IsNullOrWhiteSpace(indexName))
                throw new ArgumentException("Index name cannot be null or empty", nameof(indexName));
            
            if (columns == null || columns.Length == 0)
                throw new ArgumentException("Columns cannot be null or empty", nameof(columns));

            var columnList = string.Join(", ", columns);
            var isUnique = options?.ContainsKey("unique") == true && (bool)options["unique"];
            var uniqueKeyword = isUnique ? "UNIQUE " : "";

            return databaseType switch
            {
                DataSourceType.SqlServer => $"CREATE {uniqueKeyword}INDEX {indexName} ON {tableName} ({columnList})",
                DataSourceType.Mysql => $"CREATE {uniqueKeyword}INDEX {indexName} ON {tableName} ({columnList})",
                DataSourceType.Postgre => $"CREATE {uniqueKeyword}INDEX {indexName} ON {tableName} ({columnList})",
                DataSourceType.Oracle => $"CREATE {uniqueKeyword}INDEX {indexName} ON {tableName} ({columnList})",
                DataSourceType.SqlLite => $"CREATE {uniqueKeyword}INDEX {indexName} ON {tableName} ({columnList})",
                DataSourceType.DB2 => $"CREATE {uniqueKeyword}INDEX {indexName} ON {tableName} ({columnList})",
                DataSourceType.FireBird => $"CREATE {uniqueKeyword}INDEX {indexName} ON {tableName} ({columnList})",
                DataSourceType.SnowFlake => $"CREATE {uniqueKeyword}INDEX {indexName} ON {tableName} ({columnList})",
                DataSourceType.Cockroach => $"CREATE {uniqueKeyword}INDEX {indexName} ON {tableName} ({columnList})",
                DataSourceType.Vertica => $"CREATE {uniqueKeyword}INDEX {indexName} ON {tableName} ({columnList})",
                _ => $"CREATE {uniqueKeyword}INDEX {indexName} ON {tableName} ({columnList})"
            };
        }

        /// <summary>
        /// Generates SQL statements for transaction operations
        /// </summary>
        /// <param name="databaseType">Database type</param>
        /// <param name="operation">Transaction operation (Begin, Commit, Rollback)</param>
        /// <returns>SQL statement for the transaction operation</returns>
        public static string GetTransactionStatement(DataSourceType databaseType, TransactionOperation operation)
        {
            return operation switch
            {
                TransactionOperation.Begin => databaseType switch
                {
                    DataSourceType.SqlServer => "BEGIN TRANSACTION",
                    DataSourceType.Mysql => "START TRANSACTION",
                    DataSourceType.Postgre => "BEGIN",
                    DataSourceType.Oracle => "BEGIN",
                    DataSourceType.SqlLite => "BEGIN TRANSACTION",
                    DataSourceType.DB2 => "BEGIN",
                    DataSourceType.FireBird => "SET TRANSACTION",
                    DataSourceType.SnowFlake => "BEGIN",
                    DataSourceType.Cockroach => "BEGIN",
                    DataSourceType.Vertica => "BEGIN",
                    _ => "BEGIN TRANSACTION"
                },
                TransactionOperation.Commit => databaseType switch
                {
                    DataSourceType.SqlServer => "COMMIT TRANSACTION",
                    DataSourceType.Mysql => "COMMIT",
                    DataSourceType.Postgre => "COMMIT",
                    DataSourceType.Oracle => "COMMIT",
                    DataSourceType.SqlLite => "COMMIT",
                    DataSourceType.DB2 => "COMMIT",
                    DataSourceType.FireBird => "COMMIT",
                    DataSourceType.SnowFlake => "COMMIT",
                    DataSourceType.Cockroach => "COMMIT",
                    DataSourceType.Vertica => "COMMIT",
                    _ => "COMMIT"
                },
                TransactionOperation.Rollback => databaseType switch
                {
                    DataSourceType.SqlServer => "ROLLBACK TRANSACTION",
                    DataSourceType.Mysql => "ROLLBACK",
                    DataSourceType.Postgre => "ROLLBACK",
                    DataSourceType.Oracle => "ROLLBACK",
                    DataSourceType.SqlLite => "ROLLBACK",
                    DataSourceType.DB2 => "ROLLBACK",
                    DataSourceType.FireBird => "ROLLBACK",
                    DataSourceType.SnowFlake => "ROLLBACK",
                    DataSourceType.Cockroach => "ROLLBACK",
                    DataSourceType.Vertica => "ROLLBACK",
                    _ => "ROLLBACK"
                },
                _ => throw new ArgumentException($"Unknown transaction operation: {operation}")
            };
        }

        /// <summary>
        /// Determines if the database type supports specific features
        /// </summary>
        /// <param name="dataSourceType">Database type</param>
        /// <param name="feature">Feature to check</param>
        /// <returns>True if the feature is supported</returns>
        public static bool SupportsFeature(DataSourceType dataSourceType, DatabaseFeature feature)
        {
            return (dataSourceType, feature) switch
            {
                // Window Functions
                (DataSourceType.SqlServer, DatabaseFeature.WindowFunctions) => true,
                (DataSourceType.Mysql, DatabaseFeature.WindowFunctions) => true, // MySQL 8.0+
                (DataSourceType.Postgre, DatabaseFeature.WindowFunctions) => true,
                (DataSourceType.Oracle, DatabaseFeature.WindowFunctions) => true,
                (DataSourceType.DB2, DatabaseFeature.WindowFunctions) => true,
                (DataSourceType.SnowFlake, DatabaseFeature.WindowFunctions) => true,
                (DataSourceType.Cockroach, DatabaseFeature.WindowFunctions) => true,
                (DataSourceType.Vertica, DatabaseFeature.WindowFunctions) => true,
                (DataSourceType.GoogleBigQuery, DatabaseFeature.WindowFunctions) => true,
                (DataSourceType.AWSRedshift, DatabaseFeature.WindowFunctions) => true,

                // JSON Support
                (DataSourceType.SqlServer, DatabaseFeature.Json) => true, // SQL Server 2016+
                (DataSourceType.Mysql, DatabaseFeature.Json) => true, // MySQL 5.7+
                (DataSourceType.Postgre, DatabaseFeature.Json) => true,
                (DataSourceType.Oracle, DatabaseFeature.Json) => true, // Oracle 12c+
                (DataSourceType.SnowFlake, DatabaseFeature.Json) => true,
                (DataSourceType.Cockroach, DatabaseFeature.Json) => true,
                (DataSourceType.GoogleBigQuery, DatabaseFeature.Json) => true,

                // XML Support
                (DataSourceType.SqlServer, DatabaseFeature.Xml) => true,
                (DataSourceType.Oracle, DatabaseFeature.Xml) => true,
                (DataSourceType.DB2, DatabaseFeature.Xml) => true,
                (DataSourceType.Postgre, DatabaseFeature.Xml) => true,

                // Temporal Tables
                (DataSourceType.SqlServer, DatabaseFeature.TemporalTables) => true, // SQL Server 2016+
                (DataSourceType.Oracle, DatabaseFeature.TemporalTables) => true, // Oracle 12c+

                // Full-Text Search
                (DataSourceType.SqlServer, DatabaseFeature.FullTextSearch) => true,
                (DataSourceType.Mysql, DatabaseFeature.FullTextSearch) => true,
                (DataSourceType.Postgre, DatabaseFeature.FullTextSearch) => true,
                (DataSourceType.Oracle, DatabaseFeature.FullTextSearch) => true,
                (DataSourceType.ElasticSearch, DatabaseFeature.FullTextSearch) => true,

                // Partitioning
                (DataSourceType.SqlServer, DatabaseFeature.Partitioning) => true,
                (DataSourceType.Mysql, DatabaseFeature.Partitioning) => true,
                (DataSourceType.Postgre, DatabaseFeature.Partitioning) => true,
                (DataSourceType.Oracle, DatabaseFeature.Partitioning) => true,
                (DataSourceType.SnowFlake, DatabaseFeature.Partitioning) => true,
                (DataSourceType.GoogleBigQuery, DatabaseFeature.Partitioning) => true,
                (DataSourceType.AWSRedshift, DatabaseFeature.Partitioning) => true,

                // Columnar Storage
                (DataSourceType.SqlServer, DatabaseFeature.ColumnStore) => true, // SQL Server 2012+
                (DataSourceType.Oracle, DatabaseFeature.ColumnStore) => true, // Oracle 12c+
                (DataSourceType.Vertica, DatabaseFeature.ColumnStore) => true,
                (DataSourceType.GoogleBigQuery, DatabaseFeature.ColumnStore) => true,
                (DataSourceType.AWSRedshift, DatabaseFeature.ColumnStore) => true,
                (DataSourceType.SnowFlake, DatabaseFeature.ColumnStore) => true,
                (DataSourceType.ClickHouse, DatabaseFeature.ColumnStore) => true,

                // Default case - feature not supported
                _ => false
            };
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
        /// Generates SQL to get the count of records in a table
        /// </summary>
        /// <param name="dataSourceType">Database type</param>
        /// <param name="tableName">Name of the table</param>
        /// <param name="schemaName">Schema name (optional)</param>
        /// <param name="whereClause">Optional WHERE clause</param>
        /// <returns>SQL statement to count records</returns>
        public static string GetRecordCountQuery(DataSourceType dataSourceType, string tableName, string schemaName = null, string whereClause = null)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Table name cannot be null or empty", nameof(tableName));

            var fullTableName = string.IsNullOrEmpty(schemaName) ? tableName : $"{schemaName}.{tableName}";
            var whereClauseText = string.IsNullOrEmpty(whereClause) ? "" : $" WHERE {whereClause}";
            
            return $"SELECT COUNT(*) FROM {fullTableName}{whereClauseText}";
        }

        /// <summary>
        /// Generates SQL to truncate a table
        /// </summary>
        /// <param name="dataSourceType">Database type</param>
        /// <param name="tableName">Name of the table to truncate</param>
        /// <param name="schemaName">Schema name (optional)</param>
        /// <returns>SQL statement to truncate the table</returns>
        public static string GetTruncateTableQuery(DataSourceType dataSourceType, string tableName, string schemaName = null)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Table name cannot be null or empty", nameof(tableName));

            var fullTableName = string.IsNullOrEmpty(schemaName) ? tableName : $"{schemaName}.{tableName}";
            
            return dataSourceType switch
            {
                DataSourceType.SqlServer => $"TRUNCATE TABLE {fullTableName}",
                DataSourceType.Mysql => $"TRUNCATE TABLE {fullTableName}",
                DataSourceType.Postgre => $"TRUNCATE TABLE {fullTableName}",
                DataSourceType.Oracle => $"TRUNCATE TABLE {fullTableName}",
                DataSourceType.DB2 => $"TRUNCATE TABLE {fullTableName} IMMEDIATE",
                DataSourceType.FireBird => $"DELETE FROM {fullTableName}", // Firebird doesn't support TRUNCATE
                DataSourceType.SqlLite => $"DELETE FROM {fullTableName}", // SQLite doesn't support TRUNCATE
                _ => $"DELETE FROM {fullTableName}"
            };
        }

        /// <summary>
        /// Generates SQL to delete records from an entity with provided values.
        /// </summary>
        /// <param name="entity">The EntityStructure containing entity information</param>
        /// <param name="values">Dictionary containing values for the WHERE clause</param>
        /// <returns>A tuple containing the SQL statement, success flag, and any error message</returns>
        public static (string Sql, bool Success, string ErrorMessage) GenerateDeleteEntityWithValues(EntityStructure entity, Dictionary<string, object> values)
        {
            if (entity == null || string.IsNullOrWhiteSpace(entity.EntityName) || values == null || !values.Any())
                return (null, false, "Invalid entity or values for delete");
            try
            {
                string sql = GenerateDeleteQuery(entity.DatabaseType, entity.EntityName, values);
                return (sql, true, string.Empty);
            }
            catch (Exception ex)
            {
                return (null, false, ex.Message);
            }
        }

        /// <summary>
        /// Generates SQL to insert records into an entity with provided values.
        /// </summary>
        /// <param name="entity">The EntityStructure containing entity information</param>
        /// <param name="values">Dictionary containing field values to insert</param>
        /// <returns>A tuple containing the SQL statement, parameters, success flag, and any error message</returns>
        public static (string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage) GenerateInsertWithValues(EntityStructure entity, Dictionary<string, object> values)
        {
            if (entity == null || string.IsNullOrWhiteSpace(entity.EntityName) || values == null || !values.Any())
                return (null, null, false, "Invalid entity or values for insert");
            try
            {
                string sql = GenerateInsertQuery(entity.DatabaseType, entity.EntityName, values);
                return (sql, values, true, string.Empty);
            }
            catch (Exception ex)
            {
                return (null, null, false, ex.Message);
            }
        }

        /// <summary>
        /// Generates SQL to update records in an entity with provided values and conditions.
        /// </summary>
        /// <param name="entity">The EntityStructure containing entity information</param>
        /// <param name="values">Dictionary containing field values to update</param>
        /// <param name="conditions">Dictionary containing values for the WHERE clause</param>
        /// <returns>A tuple containing the SQL statement, success flag, and any error message</returns>
        public static (bool Success, string ErrorMessage) GenerateUpdateEntityWithValues(EntityStructure entity, Dictionary<string, object> values, Dictionary<string, object> conditions)
        {
            if (entity == null || string.IsNullOrWhiteSpace(entity.EntityName) || values == null || !values.Any() || conditions == null || !conditions.Any())
                return (false, "Invalid entity, values, or conditions for update");
            try
            {
                string sql = GenerateUpdateQuery(entity.DatabaseType, entity.EntityName, values, conditions);
                return (true, sql);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        /// <summary>
        /// Validates an entity structure and returns errors if any.
        /// </summary>
        /// <param name="entity">The EntityStructure to validate</param>
        /// <returns>Tuple with validation result and error list</returns>
        public static (bool IsValid, List<string> ValidationErrors) ValidateEntityStructure(EntityStructure entity)
        {
            var errors = new List<string>();
            bool valid = true;
            if (entity == null)
            {
                errors.Add("Entity is null");
                valid = false;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(entity.EntityName))
                {
                    errors.Add("Entity name is empty");
                    valid = false;
                }
                if (entity.Fields == null || !entity.Fields.Any())
                {
                    errors.Add("Entity has no fields");
                    valid = false;
                }
            }
            return (valid, errors);
        }

        /// <summary>
        /// Gets the identity clause SQL syntax for a specific database type
        /// </summary>
        /// <param name="databaseType">The database type</param>
        /// <returns>The identity clause syntax</returns>
        private static string GetIdentityClause(DataSourceType databaseType)
        {
            return databaseType switch
            {
                DataSourceType.SqlServer => " IDENTITY(1,1)",
                DataSourceType.Mysql => " AUTO_INCREMENT",
                DataSourceType.Postgre => " GENERATED ALWAYS AS IDENTITY",
                DataSourceType.Oracle => " GENERATED ALWAYS AS IDENTITY",
                DataSourceType.DB2 => " GENERATED BY DEFAULT AS IDENTITY",
                DataSourceType.SqlLite => " AUTOINCREMENT",
                _ => string.Empty
            };
        }
	}
}
