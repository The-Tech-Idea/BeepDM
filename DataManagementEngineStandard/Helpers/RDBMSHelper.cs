
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
//        public static List<QuerySqlRepo> CreateQuerySqlRepos()
//		{
//			return new List<QuerySqlRepo>
//	{
//				// Getting select from other schema
//				// Oracle
//new QuerySqlRepo(DataSourceType.Oracle, "SELECT TABLE_NAME FROM all_tables WHERE OWNER = '{1}'", Sqlcommandtype.getlistoftablesfromotherschema),

//// SQL Server
//new QuerySqlRepo(DataSourceType.SqlServer, "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{1}' AND TABLE_TYPE = 'BASE TABLE'", Sqlcommandtype.getlistoftablesfromotherschema),

//// MySQL
//new QuerySqlRepo(DataSourceType.Mysql, "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{1}'", Sqlcommandtype.getlistoftablesfromotherschema),

//// PostgreSQL
//new QuerySqlRepo(DataSourceType.Postgre, "SELECT table_name FROM information_schema.tables WHERE table_schema = '{1}' AND table_type = 'BASE TABLE'", Sqlcommandtype.getlistoftablesfromotherschema),

//// SQLite (Note: SQLite does not support multiple schemas, but for the sake of completeness, assuming it could)
//new QuerySqlRepo(DataSourceType.SqlLite, "SELECT name AS table_name FROM sqlite_master WHERE type='table' AND sql LIKE '%{1}%'", Sqlcommandtype.getlistoftablesfromotherschema),

//// DB2
//new QuerySqlRepo(DataSourceType.DB2, "SELECT TABNAME AS TABLE_NAME FROM SYSCAT.TABLES WHERE TABSCHEMA = '{1}'", Sqlcommandtype.getlistoftablesfromotherschema),

//// Firebird
//new QuerySqlRepo(DataSourceType.FireBird, "SELECT RDB$RELATION_NAME FROM RDB$RELATIONS WHERE RDB$VIEW_BLR IS NULL AND RDB$RELATION_NAME NOT LIKE 'RDB$%' AND RDB$RELATION_TYPE = 0 AND RDB$SYSTEM_FLAG = 0 AND RDB$RELATION_NAME IN (SELECT RDB$RELATION_NAME FROM RDB$RELATION_FIELDS WHERE RDB$FIELD_SOURCE IN (SELECT RDB$FIELD_NAME FROM RDB$FIELDS WHERE RDB$FIELD_NAME LIKE '%{1}%'))", Sqlcommandtype.getlistoftablesfromotherschema),

//// Additional systems like Hana, Snowflake, etc.:
//// Hana
//new QuerySqlRepo(DataSourceType.Hana, "SELECT TABLE_NAME FROM TABLES WHERE SCHEMA_NAME = '{1}'", Sqlcommandtype.getlistoftablesfromotherschema),

//// Snowflake
//new QuerySqlRepo(DataSourceType.SnowFlake, "SHOW TABLES IN SCHEMA {1}", Sqlcommandtype.getlistoftablesfromotherschema),

//// TerraData
//new QuerySqlRepo(DataSourceType.TerraData, "SELECT TableName FROM DBC.TablesV WHERE TableKind = 'T' AND DatabaseName = '{1}'", Sqlcommandtype.getlistoftablesfromotherschema),

//// Google BigQuery
//new QuerySqlRepo(DataSourceType.GoogleBigQuery, "SELECT table_name FROM `{1}.INFORMATION_SCHEMA.TABLES`", Sqlcommandtype.getlistoftablesfromotherschema),

//// Vertica
//new QuerySqlRepo(DataSourceType.Vertica, "SELECT table_name FROM v_catalog.tables WHERE table_schema = '{1}'", Sqlcommandtype.getlistoftablesfromotherschema),

//// ElasticSearch, MongoDB, Redis, etc., do not conform to this query pattern due to their non-relational nature and different data management models.




//		// Oracle
//		new QuerySqlRepo(DataSourceType.Oracle, "SELECT * FROM {0} {2}", Sqlcommandtype.getTable),
//		new QuerySqlRepo(DataSourceType.Oracle, "SELECT TABLE_NAME FROM user_tables", Sqlcommandtype.getlistoftables),
//		new QuerySqlRepo(DataSourceType.Oracle, "SELECT cols.column_name FROM all_constraints cons, all_cons_columns cols WHERE cols.table_name = '{0}' AND cons.constraint_type = 'P' AND cons.constraint_name = cols.constraint_name AND cons.owner = cols.owner", Sqlcommandtype.getPKforTable),
//		new QuerySqlRepo(DataSourceType.Oracle, "SELECT a.constraint_name, a.column_name, a.table_name FROM all_cons_columns a JOIN all_constraints c ON a.constraint_name = c.constraint_name WHERE c.constraint_type = 'R' AND a.table_name = '{0}'", Sqlcommandtype.getFKforTable),
//		new QuerySqlRepo(DataSourceType.Oracle, "SELECT table_name FROM all_constraints WHERE r_constraint_name IN (SELECT constraint_name FROM all_constraints WHERE table_name = '{0}' AND constraint_type = 'P')", Sqlcommandtype.getChildTable),
//		new QuerySqlRepo(DataSourceType.Oracle, "SELECT r.table_name FROM all_constraints c JOIN all_constraints r ON c.r_constraint_name = r.constraint_name WHERE c.table_name = '{0}' AND c.constraint_type = 'R'", Sqlcommandtype.getParentTable),
		
//		// SQL Server
//		new QuerySqlRepo(DataSourceType.SqlServer, "SELECT * FROM {0} {2}", Sqlcommandtype.getTable),
//		new QuerySqlRepo(DataSourceType.SqlServer, "select table_name from Information_schema.Tables where Table_type='BASE TABLE'", Sqlcommandtype.getlistoftables),
//		new QuerySqlRepo(DataSourceType.SqlServer, "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE TABLE_NAME = '{0}' AND CONSTRAINT_NAME LIKE 'PK%'", Sqlcommandtype.getPKforTable),
//		new QuerySqlRepo(DataSourceType.SqlServer, "SELECT FK.COLUMN_NAME, FK.TABLE_NAME FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE FK INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS TC ON FK.CONSTRAINT_NAME = TC.CONSTRAINT_NAME WHERE TC.CONSTRAINT_TYPE = 'FOREIGN KEY' AND FK.TABLE_NAME = '{0}'", Sqlcommandtype.getFKforTable),
//		new QuerySqlRepo(DataSourceType.SqlServer, "SELECT FK.TABLE_NAME FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS RC INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE FK ON RC.CONSTRAINT_NAME = FK.CONSTRAINT_NAME WHERE RC.UNIQUE_CONSTRAINT_NAME = (SELECT CONSTRAINT_NAME FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE TABLE_NAME = '{0}' AND CONSTRAINT_TYPE = 'PRIMARY KEY')", Sqlcommandtype.getChildTable),
//		new QuerySqlRepo(DataSourceType.SqlServer, "SELECT RC.UNIQUE_CONSTRAINT_TABLE_NAME FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS RC WHERE RC.CONSTRAINT_NAME IN (SELECT CONSTRAINT_NAME FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE TABLE_NAME = '{0}')", Sqlcommandtype.getParentTable),
//		   // MySQL
//		new QuerySqlRepo(DataSourceType.Mysql, "SELECT * FROM {0} {2}", Sqlcommandtype.getTable),
//        new QuerySqlRepo(DataSourceType.Mysql, "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{1}'", Sqlcommandtype.getlistoftables),
//        new QuerySqlRepo(DataSourceType.Mysql, "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE TABLE_NAME = '{0}' AND TABLE_SCHEMA = '{1}' AND CONSTRAINT_NAME LIKE 'PRIMARY'", Sqlcommandtype.getPKforTable),
//		new QuerySqlRepo(DataSourceType.Mysql, "SELECT COLUMN_NAME AS child_column, REFERENCED_COLUMN_NAME AS parent_column, REFERENCED_TABLE_NAME AS parent_table FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE TABLE_SCHEMA = 'YourSchema' AND TABLE_NAME = '{0}' AND REFERENCED_TABLE_NAME IS NOT NULL", Sqlcommandtype.getFKforTable),
//		new QuerySqlRepo(DataSourceType.Mysql, "SELECT TABLE_NAME AS child_table FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE TABLE_SCHEMA = 'YourSchema' AND REFERENCED_TABLE_NAME = '{0}'", Sqlcommandtype.getChildTable),
//		new QuerySqlRepo(DataSourceType.Mysql, "SELECT REFERENCED_TABLE_NAME AS parent_table FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE TABLE_SCHEMA = 'YourSchema' AND TABLE_NAME = '{0}' AND REFERENCED_TABLE_NAME IS NOT NULL", Sqlcommandtype.getParentTable),

//		// (Additional MySQL queries for FK, Child Table, Parent Table)

//		// PostgreSQL
//		new QuerySqlRepo(DataSourceType.Postgre, "SELECT * FROM {0} {2}", Sqlcommandtype.getTable),
//		new QuerySqlRepo(DataSourceType.Postgre, "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'", Sqlcommandtype.getlistoftables),
//		new QuerySqlRepo(DataSourceType.Postgre, "SELECT a.attname FROM pg_index i JOIN pg_attribute a ON a.attnum = ANY(i.indkey) WHERE i.indrelid = '{0}'::regclass AND i.indisprimary", Sqlcommandtype.getPKforTable),
//		new QuerySqlRepo(DataSourceType.Postgre, "SELECT conname AS constraint_name, a.attname AS child_column, af.attname AS parent_column, cl.relname AS parent_table FROM pg_attribute a JOIN pg_attribute af ON a.attnum = ANY(pg_constraint.confkey) JOIN pg_class cl ON pg_constraint.confrelid = cl.oid JOIN pg_constraint ON a.attnum = ANY(pg_constraint.conkey) WHERE a.attnum > 0 AND pg_constraint.conrelid = '{0}'::regclass", Sqlcommandtype.getFKforTable),
//		new QuerySqlRepo(DataSourceType.Postgre, "SELECT conname AS constraint_name, cl.relname AS child_table FROM pg_constraint JOIN pg_class cl ON pg_constraint.conrelid = cl.oid WHERE confrelid = '{0}'::regclass", Sqlcommandtype.getChildTable),
//		new QuerySqlRepo(DataSourceType.Postgre, "SELECT confrelid::regclass AS parent_table FROM pg_constraint WHERE conrelid = '{0}'::regclass", Sqlcommandtype.getParentTable),


//		// SQLite
//		new QuerySqlRepo(DataSourceType.SqlLite, "SELECT * FROM {0} {2}", Sqlcommandtype.getTable),
//		new QuerySqlRepo(DataSourceType.SqlLite, "SELECT name table_name FROM sqlite_master WHERE type='table'", Sqlcommandtype.getlistoftables),
//		new QuerySqlRepo(DataSourceType.SqlLite, "PRAGMA table_info({0})", Sqlcommandtype.getPKforTable),
//		// (Additional SQLite queries for FK, Child Table, Parent Table)

//		// DuckDB
//		 new QuerySqlRepo(DataSourceType.DuckDB, "SELECT * FROM {0} {2}", Sqlcommandtype.getTable),
//		new QuerySqlRepo(DataSourceType.DuckDB, "SELECT name FROM sqlite_master WHERE type='table'", Sqlcommandtype.getlistoftables),
//        new QuerySqlRepo(DataSourceType.DuckDB, "SELECT column_name FROM pragma_table_info('{0}') WHERE pk != 0;", Sqlcommandtype.getPKforTable),

//	   // DB2
//		new QuerySqlRepo(DataSourceType.DB2, "SELECT * FROM {0} {2}", Sqlcommandtype.getTable),
//		new QuerySqlRepo(DataSourceType.DB2, "SELECT TABNAME FROM SYSCAT.TABLES WHERE TABSCHEMA = CURRENT SCHEMA", Sqlcommandtype.getlistoftables),
//		new QuerySqlRepo(DataSourceType.DB2, "SELECT COLNAME COLUMN_NAME FROM SYSCAT.KEYCOLUSE WHERE TABNAME = '{0}' AND CONSTRAINTNAME LIKE 'PK%'", Sqlcommandtype.getPKforTable),
//		new QuerySqlRepo(DataSourceType.DB2, "SELECT FK_COLNAMES AS child_column, PK_COLNAMES AS parent_column, PK_TBNAME AS parent_table FROM SYSIBM.SQLFOREIGNKEYS WHERE FK_TBNAME = '{0}'", Sqlcommandtype.getFKforTable),
//		new QuerySqlRepo(DataSourceType.DB2, "SELECT FK_TBNAME AS child_table FROM SYSIBM.SQLFOREIGNKEYS WHERE PK_TBNAME = '{0}'", Sqlcommandtype.getChildTable),
//		new QuerySqlRepo(DataSourceType.DB2, "SELECT PK_TBNAME AS parent_table FROM SYSIBM.SQLFOREIGNKEYS WHERE FK_TBNAME = '{0}'", Sqlcommandtype.getParentTable),

//		new QuerySqlRepo(DataSourceType.MongoDB, "db.{0}.find({})", Sqlcommandtype.getTable), // Get all documents from a collection
//		new QuerySqlRepo(DataSourceType.MongoDB, "db.getCollectionNames()", Sqlcommandtype.getlistoftables), // Get all collection names
//// MongoDB does not have traditional PK or FK, but you can specify queries to get specific indexed fields or relationships if defined
//		new QuerySqlRepo(DataSourceType.Redis, "GET {0}", Sqlcommandtype.getTable), // Get the value of a key
//	// There's no direct equivalent of tables or foreign keys in Redis
//	new QuerySqlRepo(DataSourceType.Cassandra, "SELECT * FROM {0}", Sqlcommandtype.getTable), // Get all rows from a table
//new QuerySqlRepo(DataSourceType.Cassandra, "SELECT table_name FROM system_schema.tables WHERE keyspace_name = 'YourKeyspaceName'", Sqlcommandtype.getlistoftables), // Get list of tables
//new QuerySqlRepo(DataSourceType.Cassandra, "SELECT column_name FROM system_schema.columns WHERE table_name = '{0}' AND keyspace_name = 'YourKeyspaceName' AND kind = 'partition_key'", Sqlcommandtype.getPKforTable), // Get PK for a table
//// Cassandra does not support foreign keys like relational databases
//new QuerySqlRepo(DataSourceType.FireBird, "SELECT * FROM {0}", Sqlcommandtype.getTable), // Get all rows from a table
//new QuerySqlRepo(DataSourceType.FireBird, "SELECT RDB$RELATION_NAME FROM RDB$RELATIONS WHERE RDB$SYSTEM_FLAG = 0", Sqlcommandtype.getlistoftables), // Get list of tables
//new QuerySqlRepo(DataSourceType.FireBird, "SELECT RDB$INDEX_SEGMENTS.RDB$FIELD_NAME FROM RDB$INDEX_SEGMENTS JOIN RDB$RELATION_CONSTRAINTS ON RDB$INDEX_SEGMENTS.RDB$INDEX_NAME = RDB$RELATION_CONSTRAINTS.RDB$INDEX_NAME WHERE RDB$RELATION_CONSTRAINTS.RDB$RELATION_NAME = '{0}' AND RDB$RELATION_CONSTRAINTS.RDB$CONSTRAINT_TYPE = 'PRIMARY KEY'", Sqlcommandtype.getPKforTable), // Get PK for a table
//new QuerySqlRepo(DataSourceType.Couchbase, "SELECT * FROM `{0}`", Sqlcommandtype.getTable), // Get all documents from a bucket
//new QuerySqlRepo(DataSourceType.Couchbase, "SELECT name FROM system:keyspaces", Sqlcommandtype.getlistoftables), // Get list of keyspaces/buckets
//// Couchbase doesn't use traditional PKs or FKs; keys are usually part of the document structure
//new QuerySqlRepo(DataSourceType.Hana, "SELECT * FROM {0}", Sqlcommandtype.getTable), // Get all rows from a table
//new QuerySqlRepo(DataSourceType.Hana, "SELECT TABLE_NAME FROM TABLES WHERE SCHEMA_NAME = 'YOUR_SCHEMA_NAME'", Sqlcommandtype.getlistoftables), // Get list of tables
//new QuerySqlRepo(DataSourceType.Hana, "SELECT COLUMN_NAME FROM CONSTRAINTS WHERE TABLE_NAME = '{0}' AND SCHEMA_NAME = 'YOUR_SCHEMA_NAME' AND IS_PRIMARY_KEY = 'TRUE'", Sqlcommandtype.getPKforTable), // Get PK for a table
//new QuerySqlRepo(DataSourceType.Vertica, "SELECT * FROM {0}", Sqlcommandtype.getTable), // Get all rows from a table
//new QuerySqlRepo(DataSourceType.Vertica, "SELECT table_name FROM v_catalog.tables WHERE table_schema = 'YOUR_SCHEMA_NAME'", Sqlcommandtype.getlistoftables), // Get list of tables
//new QuerySqlRepo(DataSourceType.Vertica, "SELECT column_name FROM v_catalog.primary_keys WHERE table_name = '{0}' AND table_schema = 'YOUR_SCHEMA_NAME'", Sqlcommandtype.getPKforTable), // Get PK for a table
//new QuerySqlRepo(DataSourceType.TerraData, "SELECT * FROM {0}", Sqlcommandtype.getTable), // Get all rows from a table
//new QuerySqlRepo(DataSourceType.TerraData, "SELECT TableName Table_name FROM DBC.TablesV WHERE TableKind = 'T' AND DatabaseName = '{1}'", Sqlcommandtype.getlistoftables), // Get list of tables
//new QuerySqlRepo(DataSourceType.TerraData, "SELECT ColumnName Column_name FROM DBC.IndicesV WHERE TableName = '{0}' AND DatabaseName = '{1}}' AND IndexType = 'P'", Sqlcommandtype.getPKforTable), // Get PK for a table
//new QuerySqlRepo(DataSourceType.AzureCloud, "SELECT * FROM {0}", Sqlcommandtype.getTable), // Get all rows from a table
//new QuerySqlRepo(DataSourceType.AzureCloud, "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'", Sqlcommandtype.getlistoftables), // Get list of tables
//new QuerySqlRepo(DataSourceType.AzureCloud, "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE OBJECTPROPERTY(OBJECT_ID(CONSTRAINT_SCHEMA + '.' + CONSTRAINT_NAME), 'IsPrimaryKey') = 1 AND TABLE_NAME = '{0}'", Sqlcommandtype.getPKforTable), // Get PK for a table
//new QuerySqlRepo(DataSourceType.GoogleBigQuery, "SELECT * FROM {0}", Sqlcommandtype.getTable), // Get all rows from a table
//new QuerySqlRepo(DataSourceType.GoogleBigQuery, "SELECT table_name FROM `YOUR_DATASET.INFORMATION_SCHEMA.TABLES`", Sqlcommandtype.getlistoftables), // Get list of tables
//// BigQuery does not have a traditional concept of primary keys, but you can query the schema to find fields that might act as a key
//new QuerySqlRepo(DataSourceType.SnowFlake, "SELECT * FROM {0}", Sqlcommandtype.getTable), // Get all rows from a table
//new QuerySqlRepo(DataSourceType.SnowFlake, "SELECT TABLE_NAME FROM {1}.INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'", Sqlcommandtype.getlistoftables),
//new QuerySqlRepo(DataSourceType.SnowFlake, "SELECT kcu.COLUMN_NAME FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu ON tc.TABLE_NAME = kcu.TABLE_NAME AND tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME WHERE tc.TABLE_SCHEMA = '{1}' AND tc.TABLE_NAME = '{0}' AND tc.CONSTRAINT_TYPE = 'PRIMARY KEY'", Sqlcommandtype.getPKforTable),
//new QuerySqlRepo(DataSourceType.ElasticSearch, "SELECT * FROM {0}", Sqlcommandtype.getTable), // Executes a search query that, by default, retrieves the first 1000 documents.
//// Elasticsearch doesn't have the concept of tables or primary/foreign keys in the traditional sense.
//new QuerySqlRepo(DataSourceType.Cassandra, "SELECT * FROM {0}", Sqlcommandtype.getTable), // Get all rows from a table
//new QuerySqlRepo(DataSourceType.Cassandra, "SELECT table_name FROM system_schema.tables WHERE keyspace_name = 'YourKeyspaceName'", Sqlcommandtype.getlistoftables), // Get list of tables
//new QuerySqlRepo(DataSourceType.Cassandra, "SELECT column_name FROM system_schema.columns WHERE table_name = '{0}' AND keyspace_name = 'YourKeyspaceName' AND kind = 'partition_key'", Sqlcommandtype.getPKforTable), // Get primary key column
//new QuerySqlRepo(DataSourceType.CouchDB, "SELECT * FROM {0}", Sqlcommandtype.getTable), // This is a conceptual example; actual querying in CouchDB is done through views and not SQL.
//// CouchDB doesn't have a concept of tables in the same way SQL databases do. It stores JSON documents directly.

//new QuerySqlRepo(DataSourceType.Neo4j, "MATCH (n) RETURN n", Sqlcommandtype.getTable), // Get all nodes
//// Neo4j does not use tables, so there's no direct equivalent to getting a list of tables or primary/foreign keys.
//new QuerySqlRepo(DataSourceType.InfluxDB, "SELECT * FROM {0}", Sqlcommandtype.getTable), // Query data from a measurement
//new QuerySqlRepo(DataSourceType.InfluxDB, "SHOW MEASUREMENTS", Sqlcommandtype.getlistoftables), // Get list of measurements (similar to tables)
//new QuerySqlRepo(DataSourceType.DynamoDB, "Scan {0}", Sqlcommandtype.getTable), // Scan operation (be mindful of performance and cost)
//new QuerySqlRepo(DataSourceType.TimeScale, "SELECT * FROM {0}", Sqlcommandtype.getTable), // Get all rows from a hypertable
//new QuerySqlRepo(DataSourceType.TimeScale, "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'", Sqlcommandtype.getlistoftables), // Get list of tables (hypertables)
//new QuerySqlRepo(DataSourceType.TimeScale, "SELECT a.attname column_name FROM pg_index i JOIN pg_attribute a ON a.attnum = ANY(i.indkey) WHERE i.indrelid = '{0}'::regclass AND i.indisprimary", Sqlcommandtype.getPKforTable), // Get PK for a table
//new QuerySqlRepo(DataSourceType.Cockroach, "SELECT * FROM {0}", Sqlcommandtype.getTable), // Get all rows from a table
//new QuerySqlRepo(DataSourceType.Cockroach, "SHOW TABLES", Sqlcommandtype.getlistoftables), // Get list of tables
//new QuerySqlRepo(DataSourceType.Cockroach, "SELECT column_name FROM information_schema.columns WHERE table_name = '{0}' AND is_nullable = 'NO'", Sqlcommandtype.getPKforTable), // Get PK for a table (simplified)
//new QuerySqlRepo(DataSourceType.Kafka, "LIST TOPICS", Sqlcommandtype.getlistoftables), // Conceptual command to list topics
//new QuerySqlRepo(DataSourceType.OPC, "READ NODE", Sqlcommandtype.getTable), // Conceptual command to read data from an OPC node

//	};
//		}

		/// <summary>Gets the SQL syntax for paging results in a database-agnostic way.</summary>
		/// 
		public static string GetPagingSyntax(DataSourceType dataSourceType, int pageNumber, int pageSize)
		{
			int offset = (pageNumber - 1) * pageSize;
			string pagingSyntax = "";

			switch (dataSourceType)
			{
				case DataSourceType.SqlServer:
				case DataSourceType.SqlCompact: // Assuming similar to SQL Server
					pagingSyntax = $"OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";
					break;

				case DataSourceType.Mysql:
				case DataSourceType.SqlLite:
				case DataSourceType.Postgre:
				case DataSourceType.FireBird:
					pagingSyntax = $"LIMIT {pageSize} OFFSET {offset}";
					break;

				case DataSourceType.Oracle:
					// Oracle 12c and later versions support the OFFSET-FETCH syntax.
					pagingSyntax = $"OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";
					break;

				case DataSourceType.DB2:
					// DB2 uses a similar syntax to SQL Server.
					pagingSyntax = $"FETCH FIRST {pageSize} ROWS ONLY SKIP {offset}";
					break;

				case DataSourceType.Hana:
					// SAP Hana supports LIMIT and OFFSET.
					pagingSyntax = $"LIMIT {pageSize} OFFSET {offset}";
					break;

				case DataSourceType.TerraData:
				case DataSourceType.Vertica:
					// Some databases like Teradata and Vertica use QUALIFY.
					pagingSyntax = $"QUALIFY ROW_NUMBER() OVER (ORDER BY (SELECT 1)) BETWEEN {offset + 1} AND {offset + pageSize}";
					break;

				// For databases or data sources that don't support standard SQL paging or where it's not applicable,
				// throw an exception or handle accordingly.
				default:
					throw new NotSupportedException($"Paging is not supported or not defined for {dataSourceType}");
			}

			return pagingSyntax;
		}
        public static string GetDropEntity(DataSourceType dataSourceType, string entityName)
        {
            string ddl = "";
            switch (dataSourceType)
            {
				case DataSourceType.SqlServer:
					ddl = $"DROP TABLE IF EXISTS { entityName}; ";
                    break;
                case DataSourceType.Mysql:
                    ddl = $"DROP TABLE IF EXISTS {entityName};";
                    break;
                case DataSourceType.Oracle:
                    ddl = $"BEGIN EXECUTE IMMEDIATE 'DROP TABLE {entityName}'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -942 THEN RAISE; END IF; END;";
                    break;
                case DataSourceType.Postgre:
                    ddl = $"DROP TABLE IF EXISTS {entityName};";
                    break;
                case DataSourceType.SqlLite:
                    ddl = $"DROP TABLE IF EXISTS {entityName};";
                    break;
                case DataSourceType.Couchbase:
                    ddl = $"DROP COLLECTION `{entityName}`;";
				break;
                case DataSourceType.MongoDB:
                    ddl = $"db.{entityName}.drop();";
                    break;
                case DataSourceType.LiteDB:
                    ddl = $"db.DropCollection('{entityName}');";
                    break;
                case DataSourceType.InfluxDB:
                    ddl = $"DROP MEASUREMENT {entityName};";
                    break;

                    
            // Add additional cases for each database type...
            default:
            throw new NotImplementedException($"DataSourceType {dataSourceType} is not supported.");
        }
    return ddl;
}
        /// <summary>Generates a query to create an index on a table.</summary>
        public static string GenerateCreateIndexQuery(DataSourceType rdbms, string tableName, string indexName, string[] columns, bool unique = false)
        {
            string columnList = string.Join(", ", columns);
            string uniqueStr = unique ? "UNIQUE " : "";

            switch (rdbms)
            {
                case DataSourceType.SqlServer:
                case DataSourceType.Mysql:
                case DataSourceType.Postgre:
                    return $"CREATE {uniqueStr}INDEX {indexName} ON {tableName} ({columnList})";
                case DataSourceType.Oracle:
                    return $"CREATE {uniqueStr}INDEX {indexName} ON {tableName} ({columnList})";
                // Add cases for other databases
                default:
                    return $"CREATE {uniqueStr}INDEX {indexName} ON {tableName} ({columnList})";
            }
        }
        /// <summary>Generates SQL statements to begin, commit, or rollback a transaction.</summary>
        public static string GetTransactionStatement(DataSourceType rdbms, TransactionOperation operation)
        {
            switch (rdbms)
            {
                case DataSourceType.SqlServer:
                case DataSourceType.Mysql:
                    return operation switch
                    {
                        TransactionOperation.Begin => "BEGIN TRANSACTION",
                        TransactionOperation.Commit => "COMMIT",
                        TransactionOperation.Rollback => "ROLLBACK",
                        _ => throw new ArgumentException("Invalid transaction operation")
                    };
                // Add cases for other databases
                default:
                    throw new NotSupportedException($"Transaction operations not defined for {rdbms}");
            }
        }

        private static readonly Dictionary<(DataSourceType, Sqlcommandtype), string> QueryCache =
     CreateQuerySqlRepos().ToDictionary(
         q => (q.DatabaseType, q.Sqltype),
         q => q.Sql);

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
        /// <summary>Determines if the database type supports specific features.</summary>
        public static bool SupportsFeature(DataSourceType dataSourceType, DatabaseFeature feature)
        {
            return (dataSourceType, feature) switch
            {
                (DataSourceType.SqlServer, DatabaseFeature.WindowFunctions) => true,
                (DataSourceType.SqlServer, DatabaseFeature.Json) => true,
                (DataSourceType.Mysql, DatabaseFeature.WindowFunctions) => true,
                (DataSourceType.Mysql, DatabaseFeature.Json) => true,
                (DataSourceType.SqlLite, DatabaseFeature.WindowFunctions) => false,
                // Add more combinations
                _ => false
            };
        }

        #region "Update"
        /// <summary>
        /// Generates SQL to update an entity in the database based on an EntityStructure.
        /// </summary>
        /// <param name="entity">The EntityStructure containing updated information</param>
        /// <param name="whereClauseFields">List of field names to use for the WHERE clause (usually primary keys)</param>
        /// <param name="fieldsToUpdate">Optional list of specific fields to update. If null, all non-key fields will be updated.</param>
        /// <returns>A tuple containing the SQL update statement and parameters information</returns>
        public static (string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage) GenerateUpdateEntitySQL(
            EntityStructure entity,
            List<string> whereClauseFields,
            List<string> fieldsToUpdate = null)
        {
            if (entity == null)
                return (null, null, false, "Entity structure cannot be null");

            if (whereClauseFields == null || whereClauseFields.Count == 0)
                return (null, null, false, "Where clause fields must be provided for update operation");

            if (entity.Fields == null || entity.Fields.Count == 0)
                return (null, null, false, "Entity must contain fields to update");

            try
            {
                var parameters = new Dictionary<string, object>();
                var updateFields = new List<string>();
                var whereClause = new List<string>();
                int paramIndex = 1;

                // Determine which fields to update
                var fieldsToProcess = fieldsToUpdate != null && fieldsToUpdate.Count > 0
                    ? entity.Fields.Where(f => fieldsToUpdate.Contains(f.fieldname))
                    : entity.Fields.Where(f => !whereClauseFields.Contains(f.fieldname) && !f.IsAutoIncrement && !f.IsIdentity);

                // Build SQL based on database type
                switch (entity.DatabaseType)
                {
                    case DataSourceType.SqlServer:
                    case DataSourceType.AzureSQL:
                        // Build update clause
                        foreach (var field in fieldsToProcess)
                        {
                            string paramName = $"@p{paramIndex++}";
                            updateFields.Add($"[{field.fieldname}] = {paramName}");
                            parameters.Add(paramName, field.fieldname);
                        }

                        // Build where clause
                        foreach (var keyField in whereClauseFields)
                        {
                            var field = entity.Fields.FirstOrDefault(f => f.fieldname == keyField);
                            if (field != null)
                            {
                                string paramName = $"@p{paramIndex++}";
                                whereClause.Add($"[{field.fieldname}] = {paramName}");
                                parameters.Add(paramName, field.fieldname);
                            }
                        }

                        return (
                            $"UPDATE [{entity.SchemaOrOwnerOrDatabase}].[{entity.EntityName}] SET {string.Join(", ", updateFields)} WHERE {string.Join(" AND ", whereClause)}",
                            parameters,
                            true,
                            null
                        );

                    case DataSourceType.Mysql:
                        // Build update clause
                        foreach (var field in fieldsToProcess)
                        {
                            string paramName = $"@p{paramIndex++}";
                            updateFields.Add($"`{field.fieldname}` = {paramName}");
                            parameters.Add(paramName, field.fieldname);
                        }

                        // Build where clause
                        foreach (var keyField in whereClauseFields)
                        {
                            var field = entity.Fields.FirstOrDefault(f => f.fieldname == keyField);
                            if (field != null)
                            {
                                string paramName = $"@p{paramIndex++}";
                                whereClause.Add($"`{field.fieldname}` = {paramName}");
                                parameters.Add(paramName, field.fieldname);
                            }
                        }

                        string schemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"`{entity.SchemaOrOwnerOrDatabase}`."
                            : "";

                        return (
                            $"UPDATE {schemaPrefix}`{entity.EntityName}` SET {string.Join(", ", updateFields)} WHERE {string.Join(" AND ", whereClause)}",
                            parameters,
                            true,
                            null
                        );

                    case DataSourceType.Oracle:
                        // Build update clause
                        foreach (var field in fieldsToProcess)
                        {
                            string paramName = $":p{paramIndex++}";
                            updateFields.Add($"\"{field.fieldname}\" = {paramName}");
                            parameters.Add(paramName, field.fieldname);
                        }

                        // Build where clause
                        foreach (var keyField in whereClauseFields)
                        {
                            var field = entity.Fields.FirstOrDefault(f => f.fieldname == keyField);
                            if (field != null)
                            {
                                string paramName = $":p{paramIndex++}";
                                whereClause.Add($"\"{field.fieldname}\" = {paramName}");
                                parameters.Add(paramName, field.fieldname);
                            }
                        }

                        string ownerPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"{entity.SchemaOrOwnerOrDatabase}."
                            : "";

                        return (
                            $"UPDATE {ownerPrefix}\"{entity.EntityName}\" SET {string.Join(", ", updateFields)} WHERE {string.Join(" AND ", whereClause)}",
                            parameters,
                            true,
                            null
                        );

                    case DataSourceType.Postgre:
                        // Build update clause
                        foreach (var field in fieldsToProcess)
                        {
                            string paramName = $"@p{paramIndex++}";
                            updateFields.Add($"\"{field.fieldname}\" = {paramName}");
                            parameters.Add(paramName, field.fieldname);
                        }

                        // Build where clause
                        foreach (var keyField in whereClauseFields)
                        {
                            var field = entity.Fields.FirstOrDefault(f => f.fieldname == keyField);
                            if (field != null)
                            {
                                string paramName = $"@p{paramIndex++}";
                                whereClause.Add($"\"{field.fieldname}\" = {paramName}");
                                parameters.Add(paramName, field.fieldname);
                            }
                        }

                        string schemaName = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"\"{entity.SchemaOrOwnerOrDatabase}\"."
                            : "";

                        return (
                            $"UPDATE {schemaName}\"{entity.EntityName}\" SET {string.Join(", ", updateFields)} WHERE {string.Join(" AND ", whereClause)}",
                            parameters,
                            true,
                            null
                        );

                    case DataSourceType.SqlLite:
                        // Build update clause
                        foreach (var field in fieldsToProcess)
                        {
                            string paramName = $"@p{paramIndex++}";
                            updateFields.Add($"\"{field.fieldname}\" = {paramName}");
                            parameters.Add(paramName, field.fieldname);
                        }

                        // Build where clause
                        foreach (var keyField in whereClauseFields)
                        {
                            var field = entity.Fields.FirstOrDefault(f => f.fieldname == keyField);
                            if (field != null)
                            {
                                string paramName = $"@p{paramIndex++}";
                                whereClause.Add($"\"{field.fieldname}\" = {paramName}");
                                parameters.Add(paramName, field.fieldname);
                            }
                        }

                        return (
                            $"UPDATE \"{entity.EntityName}\" SET {string.Join(", ", updateFields)} WHERE {string.Join(" AND ", whereClause)}",
                            parameters,
                            true,
                            null
                        );

                    case DataSourceType.MongoDB:
                        // For MongoDB, we'll return a JSON update document format that can be parsed later
                        var updateObject = new Dictionary<string, object>();
                        foreach (var field in fieldsToProcess)
                        {
                            updateObject[field.fieldname] = field.fieldname; // Placeholder for actual value
                        }

                        var whereObject = new Dictionary<string, object>();
                        foreach (var keyField in whereClauseFields)
                        {
                            var field = entity.Fields.FirstOrDefault(f => f.fieldname == keyField);
                            if (field != null)
                            {
                                whereObject[field.fieldname] = field.fieldname; // Placeholder for actual value
                            }
                        }

                        return (
                            $"{{\"update\": \"{entity.EntityName}\", \"set\": {System.Text.Json.JsonSerializer.Serialize(updateObject)}, \"where\": {System.Text.Json.JsonSerializer.Serialize(whereObject)}}}",
                            parameters,
                            true,
                            null
                        );

                    default:
                        return (null, null, false, $"Update generation is not implemented for database type: {entity.DatabaseType}");
                }
            }
            catch (Exception ex)
            {
                return (null, null, false, $"Error generating update SQL: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates an entity in the database with actual values.
        /// </summary>
        /// <param name="entity">The EntityStructure defining the entity to update</param>
        /// <param name="dataRow">Dictionary containing the actual field values to update</param>
        /// <param name="whereValues">Dictionary containing values for the WHERE clause (usually primary key values)</param>
        /// <returns>A result object with success status and error information</returns>
        public static (bool Success, string ErrorMessage) GenerateUpdateEntityWithValues(
            EntityStructure entity,
            Dictionary<string, object> dataRow,
            Dictionary<string, object> whereValues)
        {
            if (entity == null)
                return (false, "Entity structure cannot be null");

            if (dataRow == null || dataRow.Count == 0)
                return (false, "No data provided for update");

            if (whereValues == null || whereValues.Count == 0)
                return (false, "Where clause values must be provided for update operation");

            try
            {
                var updateFields = new List<string>();
                var whereClause = new List<string>();
                var parameters = new Dictionary<string, object>();
                int paramIndex = 1;

                // Build SQL based on database type
                switch (entity.DatabaseType)
                {
                    case DataSourceType.SqlServer:
                    case DataSourceType.AzureSQL:
                        // Build update clause
                        foreach (var kvp in dataRow)
                        {
                            if (!whereValues.ContainsKey(kvp.Key))
                            {
                                string paramName = $"@p{paramIndex++}";
                                updateFields.Add($"[{kvp.Key}] = {paramName}");
                                parameters.Add(paramName, kvp.Value ?? DBNull.Value);
                            }
                        }

                        // Build where clause
                        foreach (var kvp in whereValues)
                        {
                            string paramName = $"@p{paramIndex++}";
                            whereClause.Add($"[{kvp.Key}] = {paramName}");
                            parameters.Add(paramName, kvp.Value ?? DBNull.Value);
                        }

                        string schemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"[{entity.SchemaOrOwnerOrDatabase}]."
                            : "";

                        string sql = $"UPDATE {schemaPrefix}[{entity.EntityName}] SET {string.Join(", ", updateFields)} WHERE {string.Join(" AND ", whereClause)}";

                        // Here you would normally execute the SQL with parameters
                        // For demonstration purposes, we'll return the constructed SQL
                        return (true, sql);

                    case DataSourceType.Oracle:
                    // Similar implementation for Oracle with appropriate syntax
                    // ...

                    // Implement other database types here
                    // ...

                    default:
                        return (false, $"Update generation with values is not implemented for database type: {entity.DatabaseType}");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Error generating update SQL with values: {ex.Message}");
            }
        }

        /// <summary>
        /// Validates an entity structure before performing database operations
        /// </summary>
        /// <param name="entity">The EntityStructure to validate</param>
        /// <returns>Validation result with errors if any were found</returns>
        public static (bool IsValid, List<string> ValidationErrors) ValidateEntityStructure(EntityStructure entity)
        {
            var errors = new List<string>();

            if (entity == null)
            {
                errors.Add("Entity structure cannot be null");
                return (false, errors);
            }

            if (string.IsNullOrWhiteSpace(entity.EntityName))
                errors.Add("Entity name cannot be empty");

            if (entity.Fields == null || entity.Fields.Count == 0)
                errors.Add("Entity must have at least one field");

            // Check for at least one key field for updating
            if (entity.PrimaryKeys == null || entity.PrimaryKeys.Count == 0)
                errors.Add("Entity should have at least one primary key field for safe updates");

            // Validate individual fields
            if (entity.Fields != null)
            {
                foreach (var field in entity.Fields)
                {
                    if (string.IsNullOrWhiteSpace(field.fieldname))
                        errors.Add($"Field at index {field.FieldIndex} has no name");

                    // Validate field types based on the database type
                    if (string.IsNullOrWhiteSpace(field.fieldtype))
                        errors.Add($"Field '{field.fieldname}' has no type specified");
                }
            }

            return (errors.Count == 0, errors);
        }

        #endregion "Update"
        #region "Delete"
        /// <summary>
        /// Generates SQL to delete records from an entity in the database based on an EntityStructure.
        /// </summary>
        /// <param name="entity">The EntityStructure containing entity information</param>
        /// <param name="whereClauseFields">List of field names to use for the WHERE clause (usually primary keys)</param>
        /// <returns>A tuple containing the SQL delete statement, parameters information, success flag, and any error message</returns>
        public static (string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage) GenerateDeleteEntitySQL(
            EntityStructure entity,
            List<string> whereClauseFields)
        {
            if (entity == null)
                return (null, null, false, "Entity structure cannot be null");

            if (whereClauseFields == null || whereClauseFields.Count == 0)
                return (null, null, false, "Where clause fields must be provided for delete operation");

            if (entity.Fields == null || entity.Fields.Count == 0)
                return (null, null, false, "Entity must contain fields");

            try
            {
                var parameters = new Dictionary<string, object>();
                var whereClause = new List<string>();
                int paramIndex = 1;

                // Build SQL based on database type
                switch (entity.DatabaseType)
                {
                    case DataSourceType.SqlServer:
                    case DataSourceType.AzureSQL:
                        // Build where clause
                        foreach (var keyField in whereClauseFields)
                        {
                            var field = entity.Fields.FirstOrDefault(f => f.fieldname == keyField);
                            if (field != null)
                            {
                                string paramName = $"@p{paramIndex++}";
                                whereClause.Add($"[{field.fieldname}] = {paramName}");
                                parameters.Add(paramName, field.fieldname);
                            }
                        }

                        string schemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"[{entity.SchemaOrOwnerOrDatabase}]."
                            : "";

                        return (
                            $"DELETE FROM {schemaPrefix}[{entity.EntityName}] WHERE {string.Join(" AND ", whereClause)}",
                            parameters,
                            true,
                            null
                        );

                    case DataSourceType.Mysql:
                        // Build where clause
                        foreach (var keyField in whereClauseFields)
                        {
                            var field = entity.Fields.FirstOrDefault(f => f.fieldname == keyField);
                            if (field != null)
                            {
                                string paramName = $"@p{paramIndex++}";
                                whereClause.Add($"`{field.fieldname}` = {paramName}");
                                parameters.Add(paramName, field.fieldname);
                            }
                        }

                        string mysqlSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"`{entity.SchemaOrOwnerOrDatabase}`."
                            : "";

                        return (
                            $"DELETE FROM {mysqlSchemaPrefix}`{entity.EntityName}` WHERE {string.Join(" AND ", whereClause)}",
                            parameters,
                            true,
                            null
                        );

                    case DataSourceType.Oracle:
                        // Build where clause
                        foreach (var keyField in whereClauseFields)
                        {
                            var field = entity.Fields.FirstOrDefault(f => f.fieldname == keyField);
                            if (field != null)
                            {
                                string paramName = $":p{paramIndex++}";
                                whereClause.Add($"\"{field.fieldname}\" = {paramName}");
                                parameters.Add(paramName, field.fieldname);
                            }
                        }

                        string ownerPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"{entity.SchemaOrOwnerOrDatabase}."
                            : "";

                        return (
                            $"DELETE FROM {ownerPrefix}\"{entity.EntityName}\" WHERE {string.Join(" AND ", whereClause)}",
                            parameters,
                            true,
                            null
                        );

                    case DataSourceType.Postgre:
                        // Build where clause
                        foreach (var keyField in whereClauseFields)
                        {
                            var field = entity.Fields.FirstOrDefault(f => f.fieldname == keyField);
                            if (field != null)
                            {
                                string paramName = $"@p{paramIndex++}";
                                whereClause.Add($"\"{field.fieldname}\" = {paramName}");
                                parameters.Add(paramName, field.fieldname);
                            }
                        }

                        string schemaName = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"\"{entity.SchemaOrOwnerOrDatabase}\"."
                            : "";

                        return (
                            $"DELETE FROM {schemaName}\"{entity.EntityName}\" WHERE {string.Join(" AND ", whereClause)}",
                            parameters,
                            true,
                            null
                        );

                    case DataSourceType.SqlLite:
                        // Build where clause
                        foreach (var keyField in whereClauseFields)
                        {
                            var field = entity.Fields.FirstOrDefault(f => f.fieldname == keyField);
                            if (field != null)
                            {
                                string paramName = $"@p{paramIndex++}";
                                whereClause.Add($"\"{field.fieldname}\" = {paramName}");
                                parameters.Add(paramName, field.fieldname);
                            }
                        }

                        return (
                            $"DELETE FROM \"{entity.EntityName}\" WHERE {string.Join(" AND ", whereClause)}",
                            parameters,
                            true,
                            null
                        );

                    case DataSourceType.MongoDB:
                        // For MongoDB, we'll return a JSON delete document format that can be parsed later
                        var whereObject = new Dictionary<string, object>();
                        foreach (var keyField in whereClauseFields)
                        {
                            var field = entity.Fields.FirstOrDefault(f => f.fieldname == keyField);
                            if (field != null)
                            {
                                whereObject[field.fieldname] = field.fieldname; // Placeholder for actual value
                            }
                        }

                        return (
                            $"{{\"delete\": \"{entity.EntityName}\", \"where\": {System.Text.Json.JsonSerializer.Serialize(whereObject)}}}",
                            parameters,
                            true,
                            null
                        );

                    case DataSourceType.DB2:
                        // Build where clause for DB2
                        foreach (var keyField in whereClauseFields)
                        {
                            var field = entity.Fields.FirstOrDefault(f => f.fieldname == keyField);
                            if (field != null)
                            {
                                string paramName = $"@p{paramIndex++}";
                                whereClause.Add($"\"{field.fieldname}\" = {paramName}");
                                parameters.Add(paramName, field.fieldname);
                            }
                        }

                        string db2SchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"{entity.SchemaOrOwnerOrDatabase}."
                            : "";

                        return (
                            $"DELETE FROM {db2SchemaPrefix}\"{entity.EntityName}\" WHERE {string.Join(" AND ", whereClause)}",
                            parameters,
                            true,
                            null
                        );

                    case DataSourceType.FireBird:
                        // Build where clause for FireBird
                        foreach (var keyField in whereClauseFields)
                        {
                            var field = entity.Fields.FirstOrDefault(f => f.fieldname == keyField);
                            if (field != null)
                            {
                                string paramName = $"@p{paramIndex++}";
                                whereClause.Add($"\"{field.fieldname}\" = {paramName}");
                                parameters.Add(paramName, field.fieldname);
                            }
                        }

                        return (
                            $"DELETE FROM \"{entity.EntityName}\" WHERE {string.Join(" AND ", whereClause)}",
                            parameters,
                            true,
                            null
                        );

                    default:
                        return (null, null, false, $"Delete generation is not implemented for database type: {entity.DatabaseType}");
                }
            }
            catch (Exception ex)
            {
                return (null, null, false, $"Error generating delete SQL: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes records from an entity based on actual where clause values.
        /// </summary>
        /// <param name="entity">The EntityStructure defining the entity</param>
        /// <param name="whereValues">Dictionary containing values for the WHERE clause (usually primary key values)</param>
        /// <returns>A result object with success status and error information</returns>
        public static (string Sql, bool Success, string ErrorMessage) GenerateDeleteEntityWithValues(
            EntityStructure entity,
            Dictionary<string, object> whereValues)
        {
            if (entity == null)
                return (null, false, "Entity structure cannot be null");

            if (whereValues == null || whereValues.Count == 0)
                return (null, false, "Where clause values must be provided for delete operation");

            try
            {
                var whereClause = new List<string>();
                var parameters = new Dictionary<string, object>();
                int paramIndex = 1;

                // Build SQL based on database type
                switch (entity.DatabaseType)
                {
                    case DataSourceType.SqlServer:
                    case DataSourceType.AzureSQL:
                        // Build where clause
                        foreach (var kvp in whereValues)
                        {
                            string paramName = $"@p{paramIndex++}";
                            whereClause.Add($"[{kvp.Key}] = {paramName}");
                            parameters.Add(paramName, kvp.Value ?? DBNull.Value);
                        }

                        string schemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"[{entity.SchemaOrOwnerOrDatabase}]."
                            : "";

                        string sql = $"DELETE FROM {schemaPrefix}[{entity.EntityName}] WHERE {string.Join(" AND ", whereClause)}";

                        // Here you would normally execute the SQL with parameters
                        return (sql, true, null);

                    case DataSourceType.Mysql:
                        // Build where clause
                        foreach (var kvp in whereValues)
                        {
                            string paramName = $"@p{paramIndex++}";
                            whereClause.Add($"`{kvp.Key}` = {paramName}");
                            parameters.Add(paramName, kvp.Value ?? DBNull.Value);
                        }

                        string mysqlSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"`{entity.SchemaOrOwnerOrDatabase}`."
                            : "";

                        return (
                            $"DELETE FROM {mysqlSchemaPrefix}`{entity.EntityName}` WHERE {string.Join(" AND ", whereClause)}",
                            true,
                            null
                        );

                    case DataSourceType.Oracle:
                        // Build where clause
                        foreach (var kvp in whereValues)
                        {
                            string paramName = $":p{paramIndex++}";
                            whereClause.Add($"\"{kvp.Key}\" = {paramName}");
                            parameters.Add(paramName, kvp.Value ?? DBNull.Value);
                        }

                        string ownerPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"{entity.SchemaOrOwnerOrDatabase}."
                            : "";

                        return (
                            $"DELETE FROM {ownerPrefix}\"{entity.EntityName}\" WHERE {string.Join(" AND ", whereClause)}",
                            true,
                            null
                        );

                    case DataSourceType.Postgre:
                        // Build where clause
                        foreach (var kvp in whereValues)
                        {
                            string paramName = $"@p{paramIndex++}";
                            whereClause.Add($"\"{kvp.Key}\" = {paramName}");
                            parameters.Add(paramName, kvp.Value ?? DBNull.Value);
                        }

                        string schemaName = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"\"{entity.SchemaOrOwnerOrDatabase}\"."
                            : "";

                        return (
                            $"DELETE FROM {schemaName}\"{entity.EntityName}\" WHERE {string.Join(" AND ", whereClause)}",
                            true,
                            null
                        );

                    // Add other database types as needed

                    default:
                        return (null, false, $"Delete generation with values is not implemented for database type: {entity.DatabaseType}");
                }
            }
            catch (Exception ex)
            {
                return (null, false, $"Error generating delete SQL with values: {ex.Message}");
            }
        }

        /// <summary>
        /// Generates SQL to delete all records from an entity (TRUNCATE or DELETE without WHERE)
        /// </summary>
        /// <param name="entity">The EntityStructure containing entity information</param>
        /// <param name="useTruncate">Whether to use TRUNCATE (faster but may not be allowed in all contexts)</param>
        /// <returns>A tuple containing the SQL statement, success flag, and any error message</returns>
        public static (string Sql, bool Success, string ErrorMessage) GenerateTruncateEntitySQL(
            EntityStructure entity,
            bool useTruncate = true)
        {
            if (entity == null)
                return (null, false, "Entity structure cannot be null");

            try
            {
                string sql;
                switch (entity.DatabaseType)
                {
                    case DataSourceType.SqlServer:
                    case DataSourceType.AzureSQL:
                        string sqlServerSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"[{entity.SchemaOrOwnerOrDatabase}]."
                            : "";

                        sql = useTruncate
                            ? $"TRUNCATE TABLE {sqlServerSchemaPrefix}[{entity.EntityName}]"
                            : $"DELETE FROM {sqlServerSchemaPrefix}[{entity.EntityName}]";
                        break;

                    case DataSourceType.Mysql:
                        string mysqlSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"`{entity.SchemaOrOwnerOrDatabase}`."
                            : "";

                        sql = useTruncate
                            ? $"TRUNCATE TABLE {mysqlSchemaPrefix}`{entity.EntityName}`"
                            : $"DELETE FROM {mysqlSchemaPrefix}`{entity.EntityName}`";
                        break;

                    case DataSourceType.Oracle:
                        string oracleSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"{entity.SchemaOrOwnerOrDatabase}."
                            : "";

                        sql = useTruncate
                            ? $"TRUNCATE TABLE {oracleSchemaPrefix}\"{entity.EntityName}\""
                            : $"DELETE FROM {oracleSchemaPrefix}\"{entity.EntityName}\"";
                        break;

                    case DataSourceType.Postgre:
                        string pgSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"\"{entity.SchemaOrOwnerOrDatabase}\"."
                            : "";

                        sql = useTruncate
                            ? $"TRUNCATE TABLE {pgSchemaPrefix}\"{entity.EntityName}\""
                            : $"DELETE FROM {pgSchemaPrefix}\"{entity.EntityName}\"";
                        break;

                    case DataSourceType.SqlLite:
                        // SQLite doesn't support TRUNCATE, always use DELETE
                        sql = $"DELETE FROM \"{entity.EntityName}\"";
                        break;

                    case DataSourceType.MongoDB:
                        sql = $"db.{entity.EntityName}.deleteMany({{}})";
                        break;

                    default:
                        return (null, false, $"Truncate/delete all generation is not implemented for database type: {entity.DatabaseType}");
                }

                return (sql, true, null);
            }
            catch (Exception ex)
            {
                return (null, false, $"Error generating truncate/delete all SQL: {ex.Message}");
            }
        }

        /// <summary>
        /// Generates SQL to delete multiple records based on a list of IDs
        /// </summary>
        /// <param name="entity">The EntityStructure containing entity information</param>
        /// <param name="idFieldName">The name of the ID field to use in the WHERE clause</param>
        /// <param name="ids">List of ID values to delete</param>
        /// <returns>A tuple containing the SQL statement, success flag, and any error message</returns>
        public static (string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage) GenerateDeleteMultipleSQL(
            EntityStructure entity,
            string idFieldName,
            List<object> ids)
        {
            if (entity == null)
                return (null, null, false, "Entity structure cannot be null");

            if (string.IsNullOrEmpty(idFieldName))
                return (null, null, false, "ID field name must be provided");

            if (ids == null || ids.Count == 0)
                return (null, null, false, "List of IDs must not be empty");

            try
            {
                var parameters = new Dictionary<string, object>();

                switch (entity.DatabaseType)
                {
                    case DataSourceType.SqlServer:
                    case DataSourceType.AzureSQL:
                        string sqlServerSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"[{entity.SchemaOrOwnerOrDatabase}]."
                            : "";

                        // Create parameter list
                        List<string> paramNames = new List<string>();
                        for (int i = 0; i < ids.Count; i++)
                        {
                            string paramName = $"@p{i + 1}";
                            paramNames.Add(paramName);
                            parameters.Add(paramName, ids[i] ?? DBNull.Value);
                        }

                        return (
                            $"DELETE FROM {sqlServerSchemaPrefix}[{entity.EntityName}] WHERE [{idFieldName}] IN ({string.Join(", ", paramNames)})",
                            parameters,
                            true,
                            null
                        );

                    case DataSourceType.Mysql:
                        string mysqlSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"`{entity.SchemaOrOwnerOrDatabase}`."
                            : "";

                        // Create parameter list for MySQL
                        List<string> mysqlParamNames = new List<string>();
                        for (int i = 0; i < ids.Count; i++)
                        {
                            string paramName = $"@p{i + 1}";
                            mysqlParamNames.Add(paramName);
                            parameters.Add(paramName, ids[i] ?? DBNull.Value);
                        }

                        return (
                            $"DELETE FROM {mysqlSchemaPrefix}`{entity.EntityName}` WHERE `{idFieldName}` IN ({string.Join(", ", mysqlParamNames)})",
                            parameters,
                            true,
                            null
                        );

                    // Add implementations for other database types

                    default:
                        return (null, null, false, $"Multi-delete generation is not implemented for database type: {entity.DatabaseType}");
                }
            }
            catch (Exception ex)
            {
                return (null, null, false, $"Error generating multi-delete SQL: {ex.Message}");
            }
        }

        #endregion "Delete"
        #region "Insert"
        /// <summary>
        /// Generates SQL to insert a new record into an entity in the database based on an EntityStructure.
        /// </summary>
        /// <param name="entity">The EntityStructure containing entity information</param>
        /// <param name="fieldsToInsert">Optional list of specific fields to insert. If null, all non-identity/non-autoincrement fields will be included.</param>
        /// <returns>A tuple containing the SQL insert statement, parameters information, success flag, and any error message</returns>
        public static (string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage) GenerateInsertEntitySQL(
            EntityStructure entity,
            List<string> fieldsToInsert = null)
        {
            if (entity == null)
                return (null, null, false, "Entity structure cannot be null");

            if (entity.Fields == null || entity.Fields.Count == 0)
                return (null, null, false, "Entity must contain fields to insert");

            try
            {
                var parameters = new Dictionary<string, object>();
                var columnNames = new List<string>();
                var parameterPlaceholders = new List<string>();
                int paramIndex = 1;

                // Determine which fields to insert
                var fieldsToProcess = fieldsToInsert != null && fieldsToInsert.Count > 0
                    ? entity.Fields.Where(f => fieldsToInsert.Contains(f.fieldname))
                    : entity.Fields.Where(f => !f.IsAutoIncrement && !f.IsIdentity);

                if (!fieldsToProcess.Any())
                    return (null, null, false, "No valid fields to insert (all fields may be auto-increment or identity fields)");

                // Build SQL based on database type
                switch (entity.DatabaseType)
                {
                    case DataSourceType.SqlServer:
                    case DataSourceType.AzureSQL:
                    case DataSourceType.AWSRDS:
                        foreach (var field in fieldsToProcess)
                        {
                            string paramName = $"@p{paramIndex++}";
                            columnNames.Add($"[{field.fieldname}]");
                            parameterPlaceholders.Add(paramName);
                            parameters.Add(paramName, field.fieldname); // Placeholder for actual value
                        }

                        string sqlServerSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"[{entity.SchemaOrOwnerOrDatabase}]."
                            : "";

                        return (
                            $"INSERT INTO {sqlServerSchemaPrefix}[{entity.EntityName}] ({string.Join(", ", columnNames)}) VALUES ({string.Join(", ", parameterPlaceholders)})",
                            parameters,
                            true,
                            null
                        );

                    case DataSourceType.Mysql:
                        foreach (var field in fieldsToProcess)
                        {
                            string paramName = $"@p{paramIndex++}";
                            columnNames.Add($"`{field.fieldname}`");
                            parameterPlaceholders.Add(paramName);
                            parameters.Add(paramName, field.fieldname); // Placeholder for actual value
                        }

                        string mysqlSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"`{entity.SchemaOrOwnerOrDatabase}`."
                            : "";

                        return (
                            $"INSERT INTO {mysqlSchemaPrefix}`{entity.EntityName}` ({string.Join(", ", columnNames)}) VALUES ({string.Join(", ", parameterPlaceholders)})",
                            parameters,
                            true,
                            null
                        );

                    case DataSourceType.Oracle:
                        foreach (var field in fieldsToProcess)
                        {
                            string paramName = $":{field.fieldname.ToLower()}";
                            columnNames.Add($"\"{field.fieldname}\"");
                            parameterPlaceholders.Add(paramName);
                            parameters.Add(paramName, field.fieldname); // Placeholder for actual value
                        }

                        string oracleSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"{entity.SchemaOrOwnerOrDatabase}."
                            : "";

                        return (
                            $"INSERT INTO {oracleSchemaPrefix}\"{entity.EntityName}\" ({string.Join(", ", columnNames)}) VALUES ({string.Join(", ", parameterPlaceholders)})",
                            parameters,
                            true,
                            null
                        );

                    case DataSourceType.Postgre:
                        foreach (var field in fieldsToProcess)
                        {
                            string paramName = $"@{field.fieldname.ToLower()}";
                            columnNames.Add($"\"{field.fieldname}\"");
                            parameterPlaceholders.Add(paramName);
                            parameters.Add(paramName, field.fieldname); // Placeholder for actual value
                        }

                        string pgSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"\"{entity.SchemaOrOwnerOrDatabase}\"."
                            : "";

                        return (
                            $"INSERT INTO {pgSchemaPrefix}\"{entity.EntityName}\" ({string.Join(", ", columnNames)}) VALUES ({string.Join(", ", parameterPlaceholders)})",
                            parameters,
                            true,
                            null
                        );

                    case DataSourceType.SqlLite:
                        foreach (var field in fieldsToProcess)
                        {
                            string paramName = $"@{field.fieldname}";
                            columnNames.Add($"\"{field.fieldname}\"");
                            parameterPlaceholders.Add(paramName);
                            parameters.Add(paramName, field.fieldname); // Placeholder for actual value
                        }

                        return (
                            $"INSERT INTO \"{entity.EntityName}\" ({string.Join(", ", columnNames)}) VALUES ({string.Join(", ", parameterPlaceholders)})",
                            parameters,
                            true,
                            null
                        );

                    case DataSourceType.DB2:
                        foreach (var field in fieldsToProcess)
                        {
                            string paramName = $"?"; // DB2 typically uses positional parameters
                            columnNames.Add($"\"{field.fieldname}\"");
                            parameterPlaceholders.Add(paramName);
                            parameters.Add($"p{paramIndex++}", field.fieldname); // Placeholder for actual value, using index as parameter name
                        }

                        string db2SchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"{entity.SchemaOrOwnerOrDatabase}."
                            : "";

                        return (
                            $"INSERT INTO {db2SchemaPrefix}\"{entity.EntityName}\" ({string.Join(", ", columnNames)}) VALUES ({string.Join(", ", parameterPlaceholders)})",
                            parameters,
                            true,
                            null
                        );

                    case DataSourceType.FireBird:
                        foreach (var field in fieldsToProcess)
                        {
                            string paramName = $"@{field.fieldname}";
                            columnNames.Add($"\"{field.fieldname}\"");
                            parameterPlaceholders.Add(paramName);
                            parameters.Add(paramName, field.fieldname); // Placeholder for actual value
                        }

                        return (
                            $"INSERT INTO \"{entity.EntityName}\" ({string.Join(", ", columnNames)}) VALUES ({string.Join(", ", parameterPlaceholders)})",
                            parameters,
                            true,
                            null
                        );

                    case DataSourceType.SnowFlake:
                        foreach (var field in fieldsToProcess)
                        {
                            string paramName = $":{field.fieldname}";
                            columnNames.Add($"\"{field.fieldname}\"");
                            parameterPlaceholders.Add(paramName);
                            parameters.Add(paramName, field.fieldname); // Placeholder for actual value
                        }

                        string snowflakeSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"{entity.SchemaOrOwnerOrDatabase}."
                            : "";

                        return (
                            $"INSERT INTO {snowflakeSchemaPrefix}\"{entity.EntityName}\" ({string.Join(", ", columnNames)}) VALUES ({string.Join(", ", parameterPlaceholders)})",
                            parameters,
                            true,
                            null
                        );

                    case DataSourceType.MongoDB:
                        // For MongoDB, we'll return a JSON insert document format
                        var documentObject = new Dictionary<string, object>();
                        foreach (var field in fieldsToProcess)
                        {
                            documentObject[field.fieldname] = field.fieldname; // Placeholder for actual value
                        }

                        return (
                            $"{{\"collection\": \"{entity.EntityName}\", \"document\": {System.Text.Json.JsonSerializer.Serialize(documentObject)}}}",
                            parameters,
                            true,
                            null
                        );

                    case DataSourceType.Couchbase:
                        // For Couchbase, we'll construct an N1QL query
                        var couchbaseDoc = new Dictionary<string, object>();
                        foreach (var field in fieldsToProcess)
                        {
                            couchbaseDoc[field.fieldname] = $"${field.fieldname}";
                            parameters.Add(field.fieldname, field.fieldname); // Placeholder for actual value
                        }

                        return (
                            $"INSERT INTO `{entity.EntityName}` (KEY, VALUE) VALUES (UUID(), {System.Text.Json.JsonSerializer.Serialize(couchbaseDoc)})",
                            parameters,
                            true,
                            null
                        );

                    default:
                        return (null, null, false, $"Insert generation is not implemented for database type: {entity.DatabaseType}");
                }
            }
            catch (Exception ex)
            {
                return (null, null, false, $"Error generating insert SQL: {ex.Message}");
            }
        }

        /// <summary>
        /// Generates SQL to insert a new record with provided values into an entity in the database.
        /// </summary>
        /// <param name="entity">The EntityStructure containing entity information</param>
        /// <param name="fieldValues">Dictionary containing field values to insert</param>
        /// <returns>A tuple containing the SQL insert statement, parameters information, success flag, and any error message</returns>
        public static (string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage) GenerateInsertWithValues(
            EntityStructure entity,
            Dictionary<string, object> fieldValues)
        {
            if (entity == null)
                return (null, null, false, "Entity structure cannot be null");

            if (fieldValues == null || fieldValues.Count == 0)
                return (null, null, false, "Field values must be provided for insert operation");

            if (entity.Fields == null || entity.Fields.Count == 0)
                return (null, null, false, "Entity must contain fields");

            try
            {
                var parameters = new Dictionary<string, object>();
                var columnNames = new List<string>();
                var parameterPlaceholders = new List<string>();
                int paramIndex = 1;

                // Process only fields that have values provided and are not auto-increment/identity
                var fieldsToProcess = entity.Fields
                    .Where(f => fieldValues.ContainsKey(f.fieldname) && !f.IsAutoIncrement && !f.IsIdentity)
                    .ToList();

                if (!fieldsToProcess.Any())
                    return (null, null, false, "No valid fields to insert (all provided fields may be auto-increment or identity fields)");

                // Build SQL based on database type
                switch (entity.DatabaseType)
                {
                    case DataSourceType.SqlServer:
                    case DataSourceType.AzureSQL:
                        foreach (var field in fieldsToProcess)
                        {
                            string paramName = $"@p{paramIndex++}";
                            columnNames.Add($"[{field.fieldname}]");
                            parameterPlaceholders.Add(paramName);
                            parameters.Add(paramName, fieldValues[field.fieldname] ?? DBNull.Value);
                        }

                        string sqlServerSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"[{entity.SchemaOrOwnerOrDatabase}]."
                            : "";

                        return (
                            $"INSERT INTO {sqlServerSchemaPrefix}[{entity.EntityName}] ({string.Join(", ", columnNames)}) VALUES ({string.Join(", ", parameterPlaceholders)})",
                            parameters,
                            true,
                            null
                        );

                    case DataSourceType.Mysql:
                        foreach (var field in fieldsToProcess)
                        {
                            string paramName = $"@p{paramIndex++}";
                            columnNames.Add($"`{field.fieldname}`");
                            parameterPlaceholders.Add(paramName);
                            parameters.Add(paramName, fieldValues[field.fieldname] ?? DBNull.Value);
                        }

                        string mysqlSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"`{entity.SchemaOrOwnerOrDatabase}`."
                            : "";

                        return (
                            $"INSERT INTO {mysqlSchemaPrefix}`{entity.EntityName}` ({string.Join(", ", columnNames)}) VALUES ({string.Join(", ", parameterPlaceholders)})",
                            parameters,
                            true,
                            null
                        );

                    case DataSourceType.Oracle:
                        foreach (var field in fieldsToProcess)
                        {
                            string paramName = $":{field.fieldname.ToLower()}";
                            columnNames.Add($"\"{field.fieldname}\"");
                            parameterPlaceholders.Add(paramName);
                            parameters.Add(paramName, fieldValues[field.fieldname] ?? DBNull.Value);
                        }

                        string oracleSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"{entity.SchemaOrOwnerOrDatabase}."
                            : "";

                        return (
                            $"INSERT INTO {oracleSchemaPrefix}\"{entity.EntityName}\" ({string.Join(", ", columnNames)}) VALUES ({string.Join(", ", parameterPlaceholders)})",
                            parameters,
                            true,
                            null
                        );

                    // Additional database types would be implemented similarly

                    default:
                        return (null, null, false, $"Insert with values is not implemented for database type: {entity.DatabaseType}");
                }
            }
            catch (Exception ex)
            {
                return (null, null, false, $"Error generating insert SQL with values: {ex.Message}");
            }
        }

        /// <summary>
        /// Generates SQL to insert multiple records into an entity in the database.
        /// </summary>
        /// <param name="entity">The EntityStructure containing entity information</param>
        /// <param name="multipleRecords">List of dictionaries containing field values for multiple records</param>
        /// <returns>A tuple containing the SQL insert statement, parameters information, success flag, and any error message</returns>
        public static (string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage) GenerateBulkInsertSQL(
            EntityStructure entity,
            List<Dictionary<string, object>> multipleRecords)
        {
            if (entity == null)
                return (null, null, false, "Entity structure cannot be null");

            if (multipleRecords == null || multipleRecords.Count == 0)
                return (null, null, false, "No records provided for bulk insert");

            if (entity.Fields == null || entity.Fields.Count == 0)
                return (null, null, false, "Entity must contain fields");

            try
            {
                switch (entity.DatabaseType)
                {
                    case DataSourceType.SqlServer:
                    case DataSourceType.AzureSQL:
                        // For SQL Server, we can use a table-valued parameter approach
                        // or construct a SQL statement with multiple VALUES clauses
                        var parameters = new Dictionary<string, object>();
                        var columnNames = new List<string>();
                        var valuesClauses = new List<string>();

                        // Determine fields to include (use fields from first record as template)
                        var fieldsToInclude = entity.Fields
                            .Where(f => multipleRecords[0].ContainsKey(f.fieldname) && !f.IsAutoIncrement && !f.IsIdentity)
                            .ToList();

                        // Build column names list
                        foreach (var field in fieldsToInclude)
                        {
                            columnNames.Add($"[{field.fieldname}]");
                        }

                        // Build values clauses for each record
                        int paramIndex = 1;
                        foreach (var record in multipleRecords)
                        {
                            var paramNames = new List<string>();
                            foreach (var field in fieldsToInclude)
                            {
                                string paramName = $"@p{paramIndex++}";
                                paramNames.Add(paramName);
                                parameters.Add(paramName, record.ContainsKey(field.fieldname) ? record[field.fieldname] ?? DBNull.Value : DBNull.Value);
                            }
                            valuesClauses.Add($"({string.Join(", ", paramNames)})");
                        }

                        string sqlServerSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"[{entity.SchemaOrOwnerOrDatabase}]."
                            : "";

                        return (
                            $"INSERT INTO {sqlServerSchemaPrefix}[{entity.EntityName}] ({string.Join(", ", columnNames)}) VALUES {string.Join(", ", valuesClauses)}",
                            parameters,
                            true,
                            null
                        );

                    // Other database types would be implemented similarly

                    default:
                        return (null, null, false, $"Bulk insert generation is not implemented for database type: {entity.DatabaseType}");
                }
            }
            catch (Exception ex)
            {
                return (null, null, false, $"Error generating bulk insert SQL: {ex.Message}");
            }
        }

        /// <summary>
        /// Generates SQL to insert a new record and return the identity value.
        /// </summary>
        /// <param name="entity">The EntityStructure containing entity information</param>
        /// <param name="fieldValues">Dictionary containing field values to insert</param>
        /// <param name="identityColumn">Name of the identity column (if known)</param>
        /// <returns>A tuple containing the SQL insert statement, parameters information, success flag, and any error message</returns>
        public static (string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage) GenerateInsertWithIdentitySQL(
            EntityStructure entity,
            Dictionary<string, object> fieldValues,
            string identityColumn = null)
        {
            var result = GenerateInsertWithValues(entity, fieldValues);

            if (!result.Success)
                return result;

            try
            {
                // Append database-specific syntax to get the identity value
                string identitySql;
                switch (entity.DatabaseType)
                {
                    case DataSourceType.SqlServer:
                    case DataSourceType.AzureSQL:
                        identitySql = $"{result.Sql}; SELECT SCOPE_IDENTITY() AS [Identity]";
                        break;

                    case DataSourceType.Mysql:
                        identitySql = $"{result.Sql}; SELECT LAST_INSERT_ID() AS [Identity]";
                        break;

                    case DataSourceType.Oracle:
                        // For Oracle, we typically need to use a sequence
                        // This is simplified and may need adjustment based on the actual Oracle setup
                        if (!string.IsNullOrEmpty(identityColumn))
                        {
                            identitySql = $"DECLARE v_id NUMBER; BEGIN {result.Sql} RETURNING {identityColumn} INTO v_id; :new_id := v_id; END;";
                            result.Parameters["new_id"] = DBNull.Value; // Output parameter
                        }
                        else
                        {
                            return (null, null, false, "Identity column name must be provided for Oracle");
                        }
                        break;

                    case DataSourceType.Postgre:
                        // Identify the serial/identity column if not specified
                        if (string.IsNullOrEmpty(identityColumn))
                        {
                            var identityField = entity.Fields.FirstOrDefault(f => f.IsIdentity || f.IsAutoIncrement);
                            if (identityField != null)
                            {
                                identityColumn = identityField.fieldname;
                            }
                        }

                        if (!string.IsNullOrEmpty(identityColumn))
                        {
                            identitySql = $"{result.Sql} RETURNING \"{identityColumn}\" AS \"Identity\"";
                        }
                        else
                        {
                            return (null, null, false, "Could not determine identity column for PostgreSQL");
                        }
                        break;

                    default:
                        return (null, null, false, $"Insert with identity retrieval is not implemented for database type: {entity.DatabaseType}");
                }

                return (identitySql, result.Parameters, true, null);
            }
            catch (Exception ex)
            {
                return (null, null, false, $"Error generating insert with identity SQL: {ex.Message}");
            }
        }
        #endregion "Insert"
        #region "Alter"
        /// <summary>
        /// Generates SQL to add a new column to an existing table
        /// </summary>
        /// <param name="entity">The EntityStructure containing entity information</param>
        /// <param name="field">The field/column to add</param>
        /// <returns>A tuple containing the SQL statement and success information</returns>
        public static (string Sql, bool Success, string ErrorMessage) GenerateAddColumnSQL(
            EntityStructure entity,
            EntityField field)
        {
            if (entity == null)
                return (null, false, "Entity structure cannot be null");

            if (field == null)
                return (null, false, "Field information cannot be null");

            try
            {
                string sql;
                string columnDefinition = GenerateColumnDefinition(entity.DatabaseType, field);

                switch (entity.DatabaseType)
                {
                    case DataSourceType.SqlServer:
                    case DataSourceType.AzureSQL:
                        string sqlServerSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"[{entity.SchemaOrOwnerOrDatabase}]."
                            : "";

                        sql = $"ALTER TABLE {sqlServerSchemaPrefix}[{entity.EntityName}] ADD [{field.fieldname}] {columnDefinition}";
                        break;

                    case DataSourceType.Mysql:
                        string mysqlSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"`{entity.SchemaOrOwnerOrDatabase}`."
                            : "";

                        sql = $"ALTER TABLE {mysqlSchemaPrefix}`{entity.EntityName}` ADD COLUMN `{field.fieldname}` {columnDefinition}";
                        break;

                    case DataSourceType.Oracle:
                        string oracleSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"{entity.SchemaOrOwnerOrDatabase}."
                            : "";

                        sql = $"ALTER TABLE {oracleSchemaPrefix}\"{entity.EntityName}\" ADD (\"{field.fieldname}\" {columnDefinition})";
                        break;

                    case DataSourceType.Postgre:
                        string pgSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"\"{entity.SchemaOrOwnerOrDatabase}\"."
                            : "";

                        sql = $"ALTER TABLE {pgSchemaPrefix}\"{entity.EntityName}\" ADD COLUMN \"{field.fieldname}\" {columnDefinition}";
                        break;

                    case DataSourceType.SqlLite:
                        // SQLite has limited ALTER TABLE support
                        sql = $"ALTER TABLE \"{entity.EntityName}\" ADD COLUMN \"{field.fieldname}\" {columnDefinition}";
                        break;

                    case DataSourceType.DB2:
                        string db2SchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"{entity.SchemaOrOwnerOrDatabase}."
                            : "";

                        sql = $"ALTER TABLE {db2SchemaPrefix}\"{entity.EntityName}\" ADD COLUMN \"{field.fieldname}\" {columnDefinition}";
                        break;

                    case DataSourceType.FireBird:
                        sql = $"ALTER TABLE \"{entity.EntityName}\" ADD \"{field.fieldname}\" {columnDefinition}";
                        break;

                    default:
                        return (null, false, $"Column addition is not implemented for database type: {entity.DatabaseType}");
                }

                return (sql, true, null);
            }
            catch (Exception ex)
            {
                return (null, false, $"Error generating add column SQL: {ex.Message}");
            }
        }

        /// <summary>
        /// Generates SQL to drop a column from an existing table
        /// </summary>
        /// <param name="entity">The EntityStructure containing entity information</param>
        /// <param name="fieldName">The name of the field/column to drop</param>
        /// <returns>A tuple containing the SQL statement and success information</returns>
        public static (string Sql, bool Success, string ErrorMessage) GenerateDropColumnSQL(
            EntityStructure entity,
            string fieldName)
        {
            if (entity == null)
                return (null, false, "Entity structure cannot be null");

            if (string.IsNullOrWhiteSpace(fieldName))
                return (null, false, "Field name cannot be empty");

            try
            {
                string sql;

                switch (entity.DatabaseType)
                {
                    case DataSourceType.SqlServer:
                    case DataSourceType.AzureSQL:
                        string sqlServerSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"[{entity.SchemaOrOwnerOrDatabase}]."
                            : "";

                        sql = $"ALTER TABLE {sqlServerSchemaPrefix}[{entity.EntityName}] DROP COLUMN [{fieldName}]";
                        break;

                    case DataSourceType.Mysql:
                        string mysqlSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"`{entity.SchemaOrOwnerOrDatabase}`."
                            : "";

                        sql = $"ALTER TABLE {mysqlSchemaPrefix}`{entity.EntityName}` DROP COLUMN `{fieldName}`";
                        break;

                    case DataSourceType.Oracle:
                        string oracleSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"{entity.SchemaOrOwnerOrDatabase}."
                            : "";

                        sql = $"ALTER TABLE {oracleSchemaPrefix}\"{entity.EntityName}\" DROP COLUMN \"{fieldName}\"";
                        break;

                    case DataSourceType.Postgre:
                        string pgSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"\"{entity.SchemaOrOwnerOrDatabase}\"."
                            : "";

                        sql = $"ALTER TABLE {pgSchemaPrefix}\"{entity.EntityName}\" DROP COLUMN \"{fieldName}\"";
                        break;

                    case DataSourceType.SqlLite:
                        // SQLite doesn't support dropping columns in older versions
                        return (null, false, "SQLite doesn't support DROP COLUMN directly. Table recreation is needed.");

                    case DataSourceType.DB2:
                        string db2SchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"{entity.SchemaOrOwnerOrDatabase}."
                            : "";

                        sql = $"ALTER TABLE {db2SchemaPrefix}\"{entity.EntityName}\" DROP COLUMN \"{fieldName}\"";
                        break;

                    case DataSourceType.FireBird:
                        sql = $"ALTER TABLE \"{entity.EntityName}\" DROP \"{fieldName}\"";
                        break;

                    default:
                        return (null, false, $"Column dropping is not implemented for database type: {entity.DatabaseType}");
                }

                return (sql, true, null);
            }
            catch (Exception ex)
            {
                return (null, false, $"Error generating drop column SQL: {ex.Message}");
            }
        }

        /// <summary>
        /// Generates SQL to modify the properties of an existing column
        /// </summary>
        /// <param name="entity">The EntityStructure containing entity information</param>
        /// <param name="field">The field with updated properties</param>
        /// <returns>A tuple containing the SQL statement and success information</returns>
        public static (string Sql, bool Success, string ErrorMessage) GenerateModifyColumnSQL(
            EntityStructure entity,
            EntityField field)
        {
            if (entity == null)
                return (null, false, "Entity structure cannot be null");

            if (field == null)
                return (null, false, "Field information cannot be null");

            try
            {
                string sql;
                string columnDefinition = GenerateColumnDefinition(entity.DatabaseType, field);

                switch (entity.DatabaseType)
                {
                    case DataSourceType.SqlServer:
                    case DataSourceType.AzureSQL:
                        string sqlServerSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"[{entity.SchemaOrOwnerOrDatabase}]."
                            : "";

                        sql = $"ALTER TABLE {sqlServerSchemaPrefix}[{entity.EntityName}] ALTER COLUMN [{field.fieldname}] {columnDefinition}";
                        break;

                    case DataSourceType.Mysql:
                        string mysqlSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"`{entity.SchemaOrOwnerOrDatabase}`."
                            : "";

                        sql = $"ALTER TABLE {mysqlSchemaPrefix}`{entity.EntityName}` MODIFY COLUMN `{field.fieldname}` {columnDefinition}";
                        break;

                    case DataSourceType.Oracle:
                        string oracleSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"{entity.SchemaOrOwnerOrDatabase}."
                            : "";

                        sql = $"ALTER TABLE {oracleSchemaPrefix}\"{entity.EntityName}\" MODIFY (\"{field.fieldname}\" {columnDefinition})";
                        break;

                    case DataSourceType.Postgre:
                        string pgSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"\"{entity.SchemaOrOwnerOrDatabase}\"."
                            : "";

                        // PostgreSQL handles data type and constraints separately
                        sql = $"ALTER TABLE {pgSchemaPrefix}\"{entity.EntityName}\" ALTER COLUMN \"{field.fieldname}\" TYPE {GetPostgreSQLDataType(field)}";

                        // Add nullability constraint if specified
                        if (!field.AllowDBNull)
                            sql += $";\nALTER TABLE {pgSchemaPrefix}\"{entity.EntityName}\" ALTER COLUMN \"{field.fieldname}\" SET NOT NULL";
                        else
                            sql += $";\nALTER TABLE {pgSchemaPrefix}\"{entity.EntityName}\" ALTER COLUMN \"{field.fieldname}\" DROP NOT NULL";

                        break;

                    case DataSourceType.SqlLite:
                        // SQLite doesn't support altering column properties directly
                        return (null, false, "SQLite doesn't support modifying columns directly. Table recreation is needed.");

                    case DataSourceType.DB2:
                        string db2SchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"{entity.SchemaOrOwnerOrDatabase}."
                            : "";

                        sql = $"ALTER TABLE {db2SchemaPrefix}\"{entity.EntityName}\" ALTER COLUMN \"{field.fieldname}\" SET DATA TYPE {columnDefinition}";
                        break;

                    case DataSourceType.FireBird:
                        // Firebird handles ALTER COLUMN similar to PostgreSQL
                        sql = $"ALTER TABLE \"{entity.EntityName}\" ALTER COLUMN \"{field.fieldname}\" TYPE {columnDefinition}";
                        break;

                    default:
                        return (null, false, $"Column modification is not implemented for database type: {entity.DatabaseType}");
                }

                return (sql, true, null);
            }
            catch (Exception ex)
            {
                return (null, false, $"Error generating modify column SQL: {ex.Message}");
            }
        }

        /// <summary>
        /// Generates SQL to rename a column in an existing table
        /// </summary>
        /// <param name="entity">The EntityStructure containing entity information</param>
        /// <param name="oldFieldName">The current name of the field</param>
        /// <param name="newFieldName">The new name for the field</param>
        /// <returns>A tuple containing the SQL statement and success information</returns>
        public static (string Sql, bool Success, string ErrorMessage) GenerateRenameColumnSQL(
            EntityStructure entity,
            string oldFieldName,
            string newFieldName)
        {
            if (entity == null)
                return (null, false, "Entity structure cannot be null");

            if (string.IsNullOrWhiteSpace(oldFieldName) || string.IsNullOrWhiteSpace(newFieldName))
                return (null, false, "Field names cannot be empty");

            try
            {
                string sql;

                switch (entity.DatabaseType)
                {
                    case DataSourceType.SqlServer:
                    case DataSourceType.AzureSQL:
                        string sqlServerSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"[{entity.SchemaOrOwnerOrDatabase}]."
                            : "";

                        sql = $"EXEC sp_rename '{sqlServerSchemaPrefix}[{entity.EntityName}].[{oldFieldName}]', '{newFieldName}', 'COLUMN'";
                        break;

                    case DataSourceType.Mysql:
                        // MySQL requires the full column definition for rename
                        // We'll need to find the existing column definition
                        var field = entity.Fields.FirstOrDefault(f => f.fieldname == oldFieldName);
                        if (field == null)
                            return (null, false, $"Field '{oldFieldName}' not found in entity structure");

                        string mysqlSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"`{entity.SchemaOrOwnerOrDatabase}`."
                            : "";

                        string columnDefinition = GenerateColumnDefinition(entity.DatabaseType, field);
                        sql = $"ALTER TABLE {mysqlSchemaPrefix}`{entity.EntityName}` CHANGE COLUMN `{oldFieldName}` `{newFieldName}` {columnDefinition}";
                        break;

                    case DataSourceType.Oracle:
                        string oracleSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"{entity.SchemaOrOwnerOrDatabase}."
                            : "";

                        sql = $"ALTER TABLE {oracleSchemaPrefix}\"{entity.EntityName}\" RENAME COLUMN \"{oldFieldName}\" TO \"{newFieldName}\"";
                        break;

                    case DataSourceType.Postgre:
                        string pgSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"\"{entity.SchemaOrOwnerOrDatabase}\"."
                            : "";

                        sql = $"ALTER TABLE {pgSchemaPrefix}\"{entity.EntityName}\" RENAME COLUMN \"{oldFieldName}\" TO \"{newFieldName}\"";
                        break;

                    case DataSourceType.SqlLite:
                        // SQLite doesn't support renaming columns directly in older versions
                        return (null, false, "SQLite doesn't support renaming columns directly in all versions. Table recreation may be needed.");

                    case DataSourceType.DB2:
                        string db2SchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"{entity.SchemaOrOwnerOrDatabase}."
                            : "";

                        sql = $"ALTER TABLE {db2SchemaPrefix}\"{entity.EntityName}\" RENAME COLUMN \"{oldFieldName}\" TO \"{newFieldName}\"";
                        break;

                    case DataSourceType.FireBird:
                        // Firebird 3.0+ syntax
                        sql = $"ALTER TABLE \"{entity.EntityName}\" ALTER COLUMN \"{oldFieldName}\" TO \"{newFieldName}\"";
                        break;

                    default:
                        return (null, false, $"Column renaming is not implemented for database type: {entity.DatabaseType}");
                }

                return (sql, true, null);
            }
            catch (Exception ex)
            {
                return (null, false, $"Error generating rename column SQL: {ex.Message}");
            }
        }

        /// <summary>
        /// Generates SQL to rename a table
        /// </summary>
        /// <param name="entity">The EntityStructure containing entity information</param>
        /// <param name="newEntityName">The new name for the table</param>
        /// <returns>A tuple containing the SQL statement and success information</returns>
        public static (string Sql, bool Success, string ErrorMessage) GenerateRenameTableSQL(
            EntityStructure entity,
            string newEntityName)
        {
            if (entity == null)
                return (null, false, "Entity structure cannot be null");

            if (string.IsNullOrWhiteSpace(newEntityName))
                return (null, false, "New entity name cannot be empty");

            try
            {
                string sql;

                switch (entity.DatabaseType)
                {
                    case DataSourceType.SqlServer:
                    case DataSourceType.AzureSQL:
                        string sqlServerSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"[{entity.SchemaOrOwnerOrDatabase}]."
                            : "";

                        // SQL Server syntax for renaming objects
                        sql = $"EXEC sp_rename '{sqlServerSchemaPrefix}[{entity.EntityName}]', '{newEntityName}'";
                        break;

                    case DataSourceType.Mysql:
                        string mysqlSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"`{entity.SchemaOrOwnerOrDatabase}`."
                            : "";

                        // MySQL rename table syntax
                        sql = $"RENAME TABLE {mysqlSchemaPrefix}`{entity.EntityName}` TO {mysqlSchemaPrefix}`{newEntityName}`";
                        break;

                    case DataSourceType.Oracle:
                        string oracleSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"{entity.SchemaOrOwnerOrDatabase}."
                            : "";

                        // Oracle rename table syntax
                        sql = $"ALTER TABLE {oracleSchemaPrefix}\"{entity.EntityName}\" RENAME TO \"{newEntityName}\"";
                        break;

                    case DataSourceType.Postgre:
                        string pgSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"\"{entity.SchemaOrOwnerOrDatabase}\"."
                            : "";

                        // PostgreSQL rename table syntax
                        sql = $"ALTER TABLE {pgSchemaPrefix}\"{entity.EntityName}\" RENAME TO \"{newEntityName}\"";
                        break;

                    case DataSourceType.SqlLite:
                        // SQLite rename table syntax
                        sql = $"ALTER TABLE \"{entity.EntityName}\" RENAME TO \"{newEntityName}\"";
                        break;

                    case DataSourceType.DB2:
                        string db2SchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"{entity.SchemaOrOwnerOrDatabase}."
                            : "";

                        // DB2 rename table syntax
                        sql = $"RENAME TABLE {db2SchemaPrefix}\"{entity.EntityName}\" TO \"{newEntityName}\"";
                        break;

                    case DataSourceType.FireBird:
                        // Firebird rename syntax
                        sql = $"ALTER TABLE \"{entity.EntityName}\" TO \"{newEntityName}\"";
                        break;

                    default:
                        return (null, false, $"Table renaming is not implemented for database type: {entity.DatabaseType}");
                }

                return (sql, true, null);
            }
            catch (Exception ex)
            {
                return (null, false, $"Error generating rename table SQL: {ex.Message}");
            }
        }

        /// <summary>
        /// Generates SQL to add a primary key constraint to a table
        /// </summary>
        /// <param name="entity">The EntityStructure containing entity information</param>
        /// <param name="constraintName">The name for the new primary key constraint</param>
        /// <param name="primaryKeyColumns">List of column names to include in the primary key</param>
        /// <returns>A tuple containing the SQL statement and success information</returns>
        public static (string Sql, bool Success, string ErrorMessage) GenerateAddPrimaryKeySQL(
            EntityStructure entity,
            string constraintName,
            List<string> primaryKeyColumns)
        {
            if (entity == null)
                return (null, false, "Entity structure cannot be null");

            if (primaryKeyColumns == null || primaryKeyColumns.Count == 0)
                return (null, false, "Primary key columns cannot be empty");

            try
            {
                string sql;
                string columnsJoined = string.Join(", ", primaryKeyColumns.Select(col => {
                    switch (entity.DatabaseType)
                    {
                        case DataSourceType.SqlServer:
                        case DataSourceType.AzureSQL:
                            return $"[{col}]";
                        case DataSourceType.Oracle:
                        case DataSourceType.Postgre:
                        case DataSourceType.DB2:
                        case DataSourceType.FireBird:
                            return $"\"{col}\"";
                        case DataSourceType.Mysql:
                            return $"`{col}`";
                        default:
                            return col;
                    }
                }));

                switch (entity.DatabaseType)
                {
                    case DataSourceType.SqlServer:
                    case DataSourceType.AzureSQL:
                        string sqlServerSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"[{entity.SchemaOrOwnerOrDatabase}]."
                            : "";

                        sql = $"ALTER TABLE {sqlServerSchemaPrefix}[{entity.EntityName}] ADD CONSTRAINT [{constraintName}] PRIMARY KEY ({columnsJoined})";
                        break;

                    case DataSourceType.Mysql:
                        string mysqlSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"`{entity.SchemaOrOwnerOrDatabase}`."
                            : "";

                        sql = $"ALTER TABLE {mysqlSchemaPrefix}`{entity.EntityName}` ADD CONSTRAINT `{constraintName}` PRIMARY KEY ({columnsJoined})";
                        break;

                    case DataSourceType.Oracle:
                        string oracleSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"{entity.SchemaOrOwnerOrDatabase}."
                            : "";

                        sql = $"ALTER TABLE {oracleSchemaPrefix}\"{entity.EntityName}\" ADD CONSTRAINT \"{constraintName}\" PRIMARY KEY ({columnsJoined})";
                        break;

                    case DataSourceType.Postgre:
                        string pgSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"\"{entity.SchemaOrOwnerOrDatabase}\"."
                            : "";

                        sql = $"ALTER TABLE {pgSchemaPrefix}\"{entity.EntityName}\" ADD CONSTRAINT \"{constraintName}\" PRIMARY KEY ({columnsJoined})";
                        break;

                    case DataSourceType.SqlLite:
                        // SQLite can't directly add primary key constraints after table creation
                        return (null, false, "SQLite doesn't support adding primary key constraints after table creation. Table recreation is needed.");

                    case DataSourceType.DB2:
                        string db2SchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"{entity.SchemaOrOwnerOrDatabase}."
                            : "";

                        sql = $"ALTER TABLE {db2SchemaPrefix}\"{entity.EntityName}\" ADD CONSTRAINT \"{constraintName}\" PRIMARY KEY ({columnsJoined})";
                        break;

                    case DataSourceType.FireBird:
                        sql = $"ALTER TABLE \"{entity.EntityName}\" ADD CONSTRAINT \"{constraintName}\" PRIMARY KEY ({columnsJoined})";
                        break;

                    default:
                        return (null, false, $"Adding primary key is not implemented for database type: {entity.DatabaseType}");
                }

                return (sql, true, null);
            }
            catch (Exception ex)
            {
                return (null, false, $"Error generating add primary key SQL: {ex.Message}");
            }
        }

        /// <summary>
        /// Generates SQL to add a foreign key constraint to a table
        /// </summary>
        /// <param name="entity">The EntityStructure containing entity information</param>
        /// <param name="constraintName">The name for the new foreign key constraint</param>
        /// <param name="foreignKeyColumns">List of column names from the current table</param>
        /// <param name="referencedEntity">The entity being referenced</param>
        /// <param name="referencedColumns">List of column names from the referenced table</param>
        /// <param name="cascadeDelete">Whether to cascade deletes</param>
        /// <param name="cascadeUpdate">Whether to cascade updates</param>
        /// <returns>A tuple containing the SQL statement and success information</returns>
        public static (string Sql, bool Success, string ErrorMessage) GenerateAddForeignKeySQL(
            EntityStructure entity,
            string constraintName,
            List<string> foreignKeyColumns,
            EntityStructure referencedEntity,
            List<string> referencedColumns,
            bool cascadeDelete = false,
            bool cascadeUpdate = false)
        {
            if (entity == null || referencedEntity == null)
                return (null, false, "Entity structures cannot be null");

            if (foreignKeyColumns == null || foreignKeyColumns.Count == 0 || referencedColumns == null || referencedColumns.Count == 0)
                return (null, false, "Foreign key columns cannot be empty");

            if (foreignKeyColumns.Count != referencedColumns.Count)
                return (null, false, "Foreign key and referenced columns count must match");

            try
            {
                string sql;

                // Format column names according to database syntax
                string formatColumns(List<string> columns, DataSourceType dbType)
                {
                    return string.Join(", ", columns.Select(col => {
                        switch (dbType)
                        {
                            case DataSourceType.SqlServer:
                            case DataSourceType.AzureSQL:
                                return $"[{col}]";
                            case DataSourceType.Oracle:
                            case DataSourceType.Postgre:
                            case DataSourceType.DB2:
                            case DataSourceType.FireBird:
                                return $"\"{col}\"";
                            case DataSourceType.Mysql:
                                return $"`{col}`";
                            default:
                                return col;
                        }
                    }));
                }

                string fkColumnsJoined = formatColumns(foreignKeyColumns, entity.DatabaseType);
                string pkColumnsJoined = formatColumns(referencedColumns, entity.DatabaseType);

                // Generate the ON DELETE/UPDATE clause
                string onDeleteClause = cascadeDelete
                    ? "ON DELETE CASCADE"
                    : entity.DatabaseType == DataSourceType.Oracle ? "ON DELETE SET NULL" : "ON DELETE NO ACTION";

                string onUpdateClause = cascadeUpdate
                    ? "ON UPDATE CASCADE"
                    : entity.DatabaseType == DataSourceType.Oracle ? "" : "ON UPDATE NO ACTION"; // Oracle doesn't support ON UPDATE

                switch (entity.DatabaseType)
                {
                    case DataSourceType.SqlServer:
                    case DataSourceType.AzureSQL:
                        string sqlServerSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"[{entity.SchemaOrOwnerOrDatabase}]."
                            : "";

                        string referencedSchemaPrefix = !string.IsNullOrEmpty(referencedEntity.SchemaOrOwnerOrDatabase)
                            ? $"[{referencedEntity.SchemaOrOwnerOrDatabase}]."
                            : "";

                        sql = $"ALTER TABLE {sqlServerSchemaPrefix}[{entity.EntityName}] " +
                              $"ADD CONSTRAINT [{constraintName}] FOREIGN KEY ({fkColumnsJoined}) " +
                              $"REFERENCES {referencedSchemaPrefix}[{referencedEntity.EntityName}] ({pkColumnsJoined}) " +
                              $"{onDeleteClause} {onUpdateClause}";
                        break;

                    case DataSourceType.Mysql:
                        string mysqlSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"`{entity.SchemaOrOwnerOrDatabase}`."
                            : "";

                        string mysqlReferencedSchemaPrefix = !string.IsNullOrEmpty(referencedEntity.SchemaOrOwnerOrDatabase)
                            ? $"`{referencedEntity.SchemaOrOwnerOrDatabase}`."
                            : "";

                        sql = $"ALTER TABLE {mysqlSchemaPrefix}`{entity.EntityName}` " +
                              $"ADD CONSTRAINT `{constraintName}` FOREIGN KEY ({fkColumnsJoined}) " +
                              $"REFERENCES {mysqlReferencedSchemaPrefix}`{referencedEntity.EntityName}` ({pkColumnsJoined}) " +
                              $"{onDeleteClause} {onUpdateClause}";
                        break;

                    case DataSourceType.Oracle:
                        string oracleSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"{entity.SchemaOrOwnerOrDatabase}."
                            : "";

                        string oracleReferencedSchemaPrefix = !string.IsNullOrEmpty(referencedEntity.SchemaOrOwnerOrDatabase)
                            ? $"{referencedEntity.SchemaOrOwnerOrDatabase}."
                            : "";

                        sql = $"ALTER TABLE {oracleSchemaPrefix}\"{entity.EntityName}\" " +
                              $"ADD CONSTRAINT \"{constraintName}\" FOREIGN KEY ({fkColumnsJoined}) " +
                              $"REFERENCES {oracleReferencedSchemaPrefix}\"{referencedEntity.EntityName}\" ({pkColumnsJoined}) " +
                              $"{onDeleteClause}";
                        break;

                    case DataSourceType.Postgre:
                        string pgSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"\"{entity.SchemaOrOwnerOrDatabase}\"."
                            : "";

                        string pgReferencedSchemaPrefix = !string.IsNullOrEmpty(referencedEntity.SchemaOrOwnerOrDatabase)
                            ? $"\"{referencedEntity.SchemaOrOwnerOrDatabase}\"."
                            : "";

                        sql = $"ALTER TABLE {pgSchemaPrefix}\"{entity.EntityName}\" " +
                              $"ADD CONSTRAINT \"{constraintName}\" FOREIGN KEY ({fkColumnsJoined}) " +
                              $"REFERENCES {pgReferencedSchemaPrefix}\"{referencedEntity.EntityName}\" ({pkColumnsJoined}) " +
                              $"{onDeleteClause} {onUpdateClause}";
                        break;

                    case DataSourceType.SqlLite:
                        // SQLite doesn't support adding foreign key constraints after table creation
                        return (null, false, "SQLite doesn't support adding foreign key constraints after table creation. Table recreation is needed.");

                    case DataSourceType.DB2:
                        string db2SchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"{entity.SchemaOrOwnerOrDatabase}."
                            : "";

                        string db2ReferencedSchemaPrefix = !string.IsNullOrEmpty(referencedEntity.SchemaOrOwnerOrDatabase)
                            ? $"{referencedEntity.SchemaOrOwnerOrDatabase}."
                            : "";

                        sql = $"ALTER TABLE {db2SchemaPrefix}\"{entity.EntityName}\" " +
                              $"ADD CONSTRAINT \"{constraintName}\" FOREIGN KEY ({fkColumnsJoined}) " +
                              $"REFERENCES {db2ReferencedSchemaPrefix}\"{referencedEntity.EntityName}\" ({pkColumnsJoined}) " +
                              $"{onDeleteClause} {onUpdateClause}";
                        break;

                    case DataSourceType.FireBird:
                        sql = $"ALTER TABLE \"{entity.EntityName}\" " +
                              $"ADD CONSTRAINT \"{constraintName}\" FOREIGN KEY ({fkColumnsJoined}) " +
                              $"REFERENCES \"{referencedEntity.EntityName}\" ({pkColumnsJoined}) " +
                              $"{onDeleteClause} {onUpdateClause}";
                        break;

                    default:
                        return (null, false, $"Adding foreign key is not implemented for database type: {entity.DatabaseType}");
                }

                return (sql, true, null);
            }
            catch (Exception ex)
            {
                return (null, false, $"Error generating add foreign key SQL: {ex.Message}");
            }
        }

        /// <summary>
        /// Generates SQL to add a unique constraint to a table
        /// </summary>
        /// <param name="entity">The EntityStructure containing entity information</param>
        /// <param name="constraintName">The name for the new unique constraint</param>
        /// <param name="columns">List of column names to include in the unique constraint</param>
        /// <returns>A tuple containing the SQL statement and success information</returns>
        public static (string Sql, bool Success, string ErrorMessage) GenerateAddUniqueConstraintSQL(
            EntityStructure entity,
            string constraintName,
            List<string> columns)
        {
            if (entity == null)
                return (null, false, "Entity structure cannot be null");

            if (columns == null || columns.Count == 0)
                return (null, false, "Constraint columns cannot be empty");

            try
            {
                string sql;
                string columnsJoined = string.Join(", ", columns.Select(col => {
                    switch (entity.DatabaseType)
                    {
                        case DataSourceType.SqlServer:
                        case DataSourceType.AzureSQL:
                            return $"[{col}]";
                        case DataSourceType.Oracle:
                        case DataSourceType.Postgre:
                        case DataSourceType.DB2:
                        case DataSourceType.FireBird:
                            return $"\"{col}\"";
                        case DataSourceType.Mysql:
                            return $"`{col}`";
                        default:
                            return col;
                    }
                }));

                switch (entity.DatabaseType)
                {
                    case DataSourceType.SqlServer:
                    case DataSourceType.AzureSQL:
                        string sqlServerSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"[{entity.SchemaOrOwnerOrDatabase}]."
                            : "";

                        sql = $"ALTER TABLE {sqlServerSchemaPrefix}[{entity.EntityName}] ADD CONSTRAINT [{constraintName}] UNIQUE ({columnsJoined})";
                        break;

                    case DataSourceType.Mysql:
                        string mysqlSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"`{entity.SchemaOrOwnerOrDatabase}`."
                            : "";

                        sql = $"ALTER TABLE {mysqlSchemaPrefix}`{entity.EntityName}` ADD CONSTRAINT `{constraintName}` UNIQUE ({columnsJoined})";
                        break;

                    case DataSourceType.Oracle:
                        string oracleSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"{entity.SchemaOrOwnerOrDatabase}."
                            : "";

                        sql = $"ALTER TABLE {oracleSchemaPrefix}\"{entity.EntityName}\" ADD CONSTRAINT \"{constraintName}\" UNIQUE ({columnsJoined})";
                        break;

                    case DataSourceType.Postgre:
                        string pgSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"\"{entity.SchemaOrOwnerOrDatabase}\"."
                            : "";

                        sql = $"ALTER TABLE {pgSchemaPrefix}\"{entity.EntityName}\" ADD CONSTRAINT \"{constraintName}\" UNIQUE ({columnsJoined})";
                        break;

                    case DataSourceType.SqlLite:
                        // SQLite does support adding unique constraints through normal ALTER TABLE
                        sql = $"CREATE UNIQUE INDEX \"{constraintName}\" ON \"{entity.EntityName}\" ({columnsJoined})";
                        break;

                    case DataSourceType.DB2:
                        string db2SchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"{entity.SchemaOrOwnerOrDatabase}."
                            : "";

                        sql = $"ALTER TABLE {db2SchemaPrefix}\"{entity.EntityName}\" ADD CONSTRAINT \"{constraintName}\" UNIQUE ({columnsJoined})";
                        break;

                    case DataSourceType.FireBird:
                        sql = $"ALTER TABLE \"{entity.EntityName}\" ADD CONSTRAINT \"{constraintName}\" UNIQUE ({columnsJoined})";
                        break;

                    default:
                        return (null, false, $"Adding unique constraint is not implemented for database type: {entity.DatabaseType}");
                }

                return (sql, true, null);
            }
            catch (Exception ex)
            {
                return (null, false, $"Error generating add unique constraint SQL: {ex.Message}");
            }
        }

        /// <summary>
        /// Generates SQL to drop a constraint from a table
        /// </summary>
        /// <param name="entity">The EntityStructure containing entity information</param>
        /// <param name="constraintName">The name of the constraint to drop</param>
        /// <param name="constraintType">The type of constraint (e.g., PRIMARY KEY, FOREIGN KEY, UNIQUE, etc.)</param>
        /// <returns>A tuple containing the SQL statement and success information</returns>
        public static (string Sql, bool Success, string ErrorMessage) GenerateDropConstraintSQL(
            EntityStructure entity,
            string constraintName,
            string constraintType)
        {
            if (entity == null)
                return (null, false, "Entity structure cannot be null");

            if (string.IsNullOrWhiteSpace(constraintName))
                return (null, false, "Constraint name cannot be empty");

            try
            {
                string sql;

                switch (entity.DatabaseType)
                {
                    case DataSourceType.SqlServer:
                    case DataSourceType.AzureSQL:
                        string sqlServerSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"[{entity.SchemaOrOwnerOrDatabase}]."
                            : "";

                        sql = $"ALTER TABLE {sqlServerSchemaPrefix}[{entity.EntityName}] DROP CONSTRAINT [{constraintName}]";
                        break;

                    case DataSourceType.Mysql:
                        string mysqlSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"`{entity.SchemaOrOwnerOrDatabase}`."
                            : "";

                        // MySQL uses different syntax for foreign keys
                        if (constraintType.Equals("FOREIGN KEY", StringComparison.OrdinalIgnoreCase))
                        {
                            sql = $"ALTER TABLE {mysqlSchemaPrefix}`{entity.EntityName}` DROP FOREIGN KEY `{constraintName}`";
                        }
                        else if (constraintType.Equals("PRIMARY KEY", StringComparison.OrdinalIgnoreCase))
                        {
                            sql = $"ALTER TABLE {mysqlSchemaPrefix}`{entity.EntityName}` DROP PRIMARY KEY";
                        }
                        else
                        {
                            sql = $"ALTER TABLE {mysqlSchemaPrefix}`{entity.EntityName}` DROP INDEX `{constraintName}`";
                        }
                        break;

                    case DataSourceType.Oracle:
                        string oracleSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"{entity.SchemaOrOwnerOrDatabase}."
                            : "";

                        sql = $"ALTER TABLE {oracleSchemaPrefix}\"{entity.EntityName}\" DROP CONSTRAINT \"{constraintName}\"";
                        break;

                    case DataSourceType.Postgre:
                        string pgSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"\"{entity.SchemaOrOwnerOrDatabase}\"."
                            : "";

                        sql = $"ALTER TABLE {pgSchemaPrefix}\"{entity.EntityName}\" DROP CONSTRAINT \"{constraintName}\"";
                        break;

                    case DataSourceType.SqlLite:
                        // SQLite has limited ALTER TABLE support, cannot drop constraints directly
                        return (null, false, "SQLite doesn't support dropping constraints directly. Table recreation is needed.");

                    case DataSourceType.DB2:
                        string db2SchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                            ? $"{entity.SchemaOrOwnerOrDatabase}."
                            : "";

                        sql = $"ALTER TABLE {db2SchemaPrefix}\"{entity.EntityName}\" DROP {constraintType} \"{constraintName}\"";
                        break;

                    case DataSourceType.FireBird:
                        sql = $"ALTER TABLE \"{entity.EntityName}\" DROP CONSTRAINT \"{constraintName}\"";
                        break;

                    default:
                        return (null, false, $"Dropping constraint is not implemented for database type: {entity.DatabaseType}");
                }

                return (sql, true, null);
            }
            catch (Exception ex)
            {
                return (null, false, $"Error generating drop constraint SQL: {ex.Message}");
            }
        }

        /// <summary>
        /// Helper method to generate column definition for different database types
        /// </summary>
        /// <param name="dbType">Database type</param>
        /// <param name="field">Field definition</param>
        /// <returns>Column definition string</returns>
        private static string GenerateColumnDefinition(DataSourceType dbType, EntityField field)
        {
            // Get data type string for this database
            string dataType = GetDatabaseTypeString(dbType, field);

            // Build column definition with appropriate constraints
            string nullability = field.AllowDBNull ? "NULL" : "NOT NULL";
            string defaultValue = !string.IsNullOrEmpty(field.DefaultValue) ? $"DEFAULT {field.DefaultValue}" : "";

            // Special handling for auto-increment/identity fields
            if (field.IsAutoIncrement || field.IsIdentity)
            {
                switch (dbType)
                {
                    case DataSourceType.SqlServer:
                    case DataSourceType.AzureSQL:
                        return $"{dataType} IDENTITY(1,1) {nullability} {defaultValue}".Trim();

                    case DataSourceType.Mysql:
                        return $"{dataType} AUTO_INCREMENT {nullability} {defaultValue}".Trim();

                    case DataSourceType.Postgre:
                        if (field.fieldtype.Contains("int", StringComparison.OrdinalIgnoreCase))
                            return $"{dataType} GENERATED ALWAYS AS IDENTITY {nullability} {defaultValue}".Trim();
                        else
                            return $"{dataType} {nullability} {defaultValue}".Trim();

                    case DataSourceType.Oracle:
                        if (field.fieldtype.Contains("number", StringComparison.OrdinalIgnoreCase))
                            return $"{dataType} GENERATED AS IDENTITY {nullability} {defaultValue}".Trim();
                        else
                            return $"{dataType} {nullability} {defaultValue}".Trim();

                    default:
                        return $"{dataType} {nullability} {defaultValue}".Trim();
                }
            }

            // Normal column
            return $"{dataType} {nullability} {defaultValue}".Trim();
        }

        /// <summary>
        /// Gets the PostgreSQL data type string for a field
        /// </summary>
        /// <param name="field">Field definition</param>
        /// <returns>PostgreSQL data type string</returns>
        private static string GetPostgreSQLDataType(EntityField field)
        {
            string baseType = field.fieldtype.ToLower();

            // Handle common data types
            if (baseType.Contains("varchar") || baseType.Contains("char") || baseType.Contains("string"))
            {
                return field.Size > 0 ? $"VARCHAR({field.Size})" : "TEXT";
            }
            else if (baseType.Contains("int"))
            {
                if (baseType.Contains("smallint") || baseType.Contains("tinyint"))
                    return "SMALLINT";
                else if (baseType.Contains("bigint"))
                    return "BIGINT";
                else
                    return "INTEGER";
            }
            else if (baseType.Contains("decimal") || baseType.Contains("numeric"))
            {
                return field.NumericPrecision > 0 && field.NumericScale >= 0
                    ? $"NUMERIC({field.NumericPrecision},{field.NumericScale})"
                    : "NUMERIC";
            }
            else if (baseType.Contains("float") || baseType.Contains("double"))
            {
                return "DOUBLE PRECISION";
            }
            else if (baseType.Contains("date"))
            {
                if (baseType.Contains("datetime") || baseType.Contains("timestamp"))
                    return "TIMESTAMP";
                else
                    return "DATE";
            }
            else if (baseType.Contains("bool"))
            {
                return "BOOLEAN";
            }
            else if (baseType.Contains("text") || baseType.Contains("memo"))
            {
                return "TEXT";
            }
            else if (baseType.Contains("blob") || baseType.Contains("binary"))
            {
                return "BYTEA";
            }
            else
            {
                // Default for unknown types
                return "TEXT";
            }
        }

        /// <summary>
        /// Maps a common field type to a specific database type string
        /// </summary>
        /// <param name="dbType">Database type</param>
        /// <param name="field">Field definition</param>
        /// <returns>Database-specific data type string</returns>
        private static string GetDatabaseTypeString(DataSourceType dbType, EntityField field)
        {
            string baseType = field.fieldtype.ToLower();

            switch (dbType)
            {
                case DataSourceType.SqlServer:
                case DataSourceType.AzureSQL:
                    // SQL Server types
                    if (baseType.Contains("varchar") || baseType.Contains("string"))
                        return field.Size > 0 ? $"NVARCHAR({field.Size})" : "NVARCHAR(MAX)";
                    else if (baseType.Contains("char"))
                        return field.Size > 0 ? $"CHAR({field.Size})" : "CHAR(1)";
                    else if (baseType.Contains("text"))
                        return "NTEXT";
                    else if (baseType.Contains("int"))
                    {
                        if (baseType.Contains("smallint"))
                            return "SMALLINT";
                        else if (baseType.Contains("tinyint"))
                            return "TINYINT";
                        else if (baseType.Contains("bigint"))
                            return "BIGINT";
                        else
                            return "INT";
                    }
                    else if (baseType.Contains("decimal") || baseType.Contains("numeric"))
                        return field.NumericPrecision > 0 ? $"DECIMAL({field.NumericPrecision},{field.NumericScale})" : "DECIMAL(18,2)";
                    else if (baseType.Contains("float"))
                        return "FLOAT";
                    else if (baseType.Contains("double"))
                        return "FLOAT";
                    else if (baseType.Contains("datetime") || baseType.Contains("timestamp"))
                        return "DATETIME2";
                    else if (baseType.Contains("date"))
                        return "DATE";
                    else if (baseType.Contains("time"))
                        return "TIME";
                    else if (baseType.Contains("bool"))
                        return "BIT";
                    else if (baseType.Contains("blob") || baseType.Contains("binary"))
                        return "VARBINARY(MAX)";
                    else
                        return "NVARCHAR(100)"; // Default

                case DataSourceType.Mysql:
                    // MySQL types
                    if (baseType.Contains("varchar") || baseType.Contains("string"))
                        return field.Size > 0 ? $"VARCHAR({field.Size})" : "TEXT";
                    else if (baseType.Contains("char"))
                        return field.Size > 0 ? $"CHAR({field.Size})" : "CHAR(1)";
                    else if (baseType.Contains("text"))
                        return "TEXT";
                    else if (baseType.Contains("int"))
                    {
                        if (baseType.Contains("smallint"))
                            return "SMALLINT";
                        else if (baseType.Contains("tinyint"))
                            return "TINYINT";
                        else if (baseType.Contains("bigint"))
                            return "BIGINT";
                        else
                            return "INT";
                    }
                    else if (baseType.Contains("decimal") || baseType.Contains("numeric"))
                        return field.NumericPrecision > 0 ? $"DECIMAL({field.NumericPrecision},{field.NumericScale})" : "DECIMAL(10,2)";
                    else if (baseType.Contains("float"))
                        return "FLOAT";
                    else if (baseType.Contains("double"))
                        return "DOUBLE";
                    else if (baseType.Contains("datetime") || baseType.Contains("timestamp"))
                        return "DATETIME";
                    else if (baseType.Contains("date"))
                        return "DATE";
                    else if (baseType.Contains("time"))
                        return "TIME";
                    else if (baseType.Contains("bool"))
                        return "BOOLEAN";
                    else if (baseType.Contains("blob") || baseType.Contains("binary"))
                        return "BLOB";
                    else
                        return "VARCHAR(100)"; // Default

                case DataSourceType.Oracle:
                    // Oracle types
                    if (baseType.Contains("varchar") || baseType.Contains("string"))
                        return field.Size > 0 ? $"VARCHAR2({field.Size})" : "VARCHAR2(4000)";
                    else if (baseType.Contains("char"))
                        return field.Size > 0 ? $"CHAR({field.Size})" : "CHAR(1)";
                    else if (baseType.Contains("text"))
                        return "CLOB";
                    else if (baseType.Contains("int"))
                        return "NUMBER(10)";
                    else if (baseType.Contains("decimal") || baseType.Contains("numeric"))
                        return field.NumericPrecision > 0 ? $"NUMBER({field.NumericPrecision},{field.NumericScale})" : "NUMBER(19,4)";
                    else if (baseType.Contains("float") || baseType.Contains("double"))
                        return "FLOAT";
                    else if (baseType.Contains("datetime") || baseType.Contains("timestamp"))
                        return "TIMESTAMP";
                    else if (baseType.Contains("date"))
                        return "DATE";
                    else if (baseType.Contains("bool"))
                        return "NUMBER(1)";
                    else if (baseType.Contains("blob") || baseType.Contains("binary"))
                        return "BLOB";
                    else
                        return "VARCHAR2(100)"; // Default

                case DataSourceType.Postgre:
                    // PostgreSQL types (use the helper method)
                    return GetPostgreSQLDataType(field);

                case DataSourceType.SqlLite:
                    // SQLite types
                    if (baseType.Contains("int"))
                        return "INTEGER";
                    else if (baseType.Contains("varchar") || baseType.Contains("string") || baseType.Contains("char"))
                        return "TEXT";
                    else if (baseType.Contains("decimal") || baseType.Contains("numeric") || baseType.Contains("float") || baseType.Contains("double"))
                        return "REAL";
                    else if (baseType.Contains("bool"))
                        return "INTEGER"; // SQLite uses 0/1 for boolean
                    else if (baseType.Contains("blob") || baseType.Contains("binary"))
                        return "BLOB";
                    else if (baseType.Contains("date") || baseType.Contains("time"))
                        return "TEXT"; // SQLite stores dates as text in ISO format
                    else
                        return "TEXT"; // Default

                default:
                    // Generic SQL type for other databases
                    if (baseType.Contains("varchar") || baseType.Contains("string"))
                        return field.Size > 0 ? $"VARCHAR({field.Size})" : "VARCHAR(255)";
                    else if (baseType.Contains("char"))
                        return field.Size > 0 ? $"CHAR({field.Size})" : "CHAR(1)";
                    else if (baseType.Contains("text"))
                        return "TEXT";
                    else if (baseType.Contains("int"))
                    {
                        if (baseType.Contains("smallint"))
                            return "SMALLINT";
                        else if (baseType.Contains("tinyint"))
                            return "TINYINT";
                        else if (baseType.Contains("bigint"))
                            return "BIGINT";
                        else
                            return "INTEGER";
                    }
                    else if (baseType.Contains("decimal") || baseType.Contains("numeric"))
                        return field.NumericPrecision > 0 ? $"NUMERIC({field.NumericPrecision},{field.NumericScale})" : "NUMERIC(10,2)";
                    else if (baseType.Contains("float"))
                        return "FLOAT";
                    else if (baseType.Contains("double"))
                        return "DOUBLE PRECISION";
                    else if (baseType.Contains("datetime") || baseType.Contains("timestamp"))
                        return "TIMESTAMP";
                    else if (baseType.Contains("date"))
                        return "DATE";
                    else if (baseType.Contains("time"))
                        return "TIME";
                    else if (baseType.Contains("bool"))
                        return "BOOLEAN";
                    else if (baseType.Contains("blob") || baseType.Contains("binary"))
                        return "BLOB";
                    else
                        return "VARCHAR(100)"; // Default
            }
        }
        #endregion "Alter"
        #region "Create"
        /// <summary>
        /// Generates SQL to create a new table based on an EntityStructure
        /// </summary>
        /// <param name="entity">The EntityStructure containing entity definition</param>
        /// <returns>A tuple containing the SQL statement, success flag, and any error message</returns>
        public static (string Sql, bool Success, string ErrorMessage) GenerateCreateTableSQL(EntityStructure entity)
        {
            if (entity == null)
                return (null, false, "Entity structure cannot be null");

            if (entity.Fields == null || entity.Fields.Count == 0)
                return (null, false, "Entity must contain fields");

            try
            {
                var columnDefinitions = new List<string>();
                var primaryKeyConstraint = string.Empty;
                var primaryKeyColumns = new List<string>();

                // Identify primary key columns
                if (entity.PrimaryKeys != null && entity.PrimaryKeys.Count > 0)
                {
                    foreach (var pkField in entity.PrimaryKeys)
                    {
                        primaryKeyColumns.Add(pkField.fieldname);
                    }
                }
                else
                {
                    // If no explicit primary keys are defined, look for fields marked as keys
                    var keyFields = entity.Fields.Where(f => f.IsKey).ToList();
                    if (keyFields.Any())
                    {
                        foreach (var keyField in keyFields)
                        {
                            primaryKeyColumns.Add(keyField.fieldname);
                        }
                    }
                }

                // Build column definitions
                foreach (var field in entity.Fields)
                {
                    string columnDef = GenerateColumnDefinition(entity.DatabaseType, field);
                    columnDefinitions.Add(GetFormattedColumnName(entity.DatabaseType, field.fieldname) + " " + columnDef);
                }

                // Add primary key constraint at table level if needed
                if (primaryKeyColumns.Count > 0 &&
                    entity.DatabaseType != DataSourceType.MongoDB &&
                    entity.DatabaseType != DataSourceType.Couchbase &&
                    entity.DatabaseType != DataSourceType.Redis)
                {
                    string constraintName = $"PK_{entity.EntityName}";

                    // Format PK columns according to database syntax
                    string formattedColumns = string.Join(", ", primaryKeyColumns.Select(col =>
                        GetFormattedColumnName(entity.DatabaseType, col)));

                    primaryKeyConstraint = GetPrimaryKeyConstraintSyntax(entity.DatabaseType, constraintName, formattedColumns);
                }

                // Build the CREATE TABLE statement based on database type
                string createTableSQL = BuildCreateTableStatement(entity, columnDefinitions, primaryKeyConstraint);

                return (createTableSQL, true, null);
            }
            catch (Exception ex)
            {
                return (null, false, $"Error generating create table SQL: {ex.Message}");
            }
        }

        /// <summary>
        /// Builds a complete CREATE TABLE statement for a specific database type
        /// </summary>
        private static string BuildCreateTableStatement(
            EntityStructure entity,
            List<string> columnDefinitions,
            string primaryKeyConstraint)
        {
            string sql;
            string columnsList = string.Join(",\n    ", columnDefinitions);
            bool hasPrimaryKey = !string.IsNullOrEmpty(primaryKeyConstraint);

            switch (entity.DatabaseType)
            {
                case DataSourceType.SqlServer:
                case DataSourceType.AzureSQL:
                case DataSourceType.AWSRDS when entity.SchemaOrOwnerOrDatabase == "MSSQL":
                    string sqlServerSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                        ? $"[{entity.SchemaOrOwnerOrDatabase}]."
                        : "";

                    sql = $"CREATE TABLE {sqlServerSchemaPrefix}[{entity.EntityName}] (\n    {columnsList}";
                    if (hasPrimaryKey)
                        sql += ",\n    " + primaryKeyConstraint;
                    sql += "\n)";
                    break;

                case DataSourceType.Mysql:
                    string mysqlSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                        ? $"`{entity.SchemaOrOwnerOrDatabase}`."
                        : "";

                    sql = $"CREATE TABLE {mysqlSchemaPrefix}`{entity.EntityName}` (\n    {columnsList}";
                    if (hasPrimaryKey)
                        sql += ",\n    " + primaryKeyConstraint;
                    sql += "\n) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci";
                    break;

                case DataSourceType.Oracle:
                    string oracleSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                        ? $"{entity.SchemaOrOwnerOrDatabase}."
                        : "";

                    sql = $"CREATE TABLE {oracleSchemaPrefix}\"{entity.EntityName}\" (\n    {columnsList}";
                    if (hasPrimaryKey)
                        sql += ",\n    " + primaryKeyConstraint;
                    sql += "\n)";
                    break;

                case DataSourceType.Postgre:
                    string pgSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                        ? $"\"{entity.SchemaOrOwnerOrDatabase}\"."
                        : "";

                    sql = $"CREATE TABLE {pgSchemaPrefix}\"{entity.EntityName}\" (\n    {columnsList}";
                    if (hasPrimaryKey)
                        sql += ",\n    " + primaryKeyConstraint;
                    sql += "\n)";
                    break;

                case DataSourceType.SqlLite:
                    sql = $"CREATE TABLE \"{entity.EntityName}\" (\n    {columnsList}";
                    if (hasPrimaryKey)
                        sql += ",\n    " + primaryKeyConstraint;
                    sql += "\n)";
                    break;

                case DataSourceType.DB2:
                    string db2SchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                        ? $"{entity.SchemaOrOwnerOrDatabase}."
                        : "";

                    sql = $"CREATE TABLE {db2SchemaPrefix}\"{entity.EntityName}\" (\n    {columnsList}";
                    if (hasPrimaryKey)
                        sql += ",\n    " + primaryKeyConstraint;
                    sql += "\n)";
                    break;

                case DataSourceType.FireBird:
                    sql = $"CREATE TABLE \"{entity.EntityName}\" (\n    {columnsList}";
                    if (hasPrimaryKey)
                        sql += ",\n    " + primaryKeyConstraint;
                    sql += "\n)";
                    break;

                case DataSourceType.SnowFlake:
                    string snowflakeSchemaPrefix = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                        ? $"{entity.SchemaOrOwnerOrDatabase}."
                        : "";

                    sql = $"CREATE TABLE {snowflakeSchemaPrefix}\"{entity.EntityName}\" (\n    {columnsList}";
                    if (hasPrimaryKey)
                        sql += ",\n    " + primaryKeyConstraint;
                    sql += "\n)";
                    break;

                case DataSourceType.MongoDB:
                    // For MongoDB, we generate a schema validation document
                    var schemaFields = new Dictionary<string, object>();
                    foreach (var field in entity.Fields)
                    {
                        var fieldType = GetMongoDBSchemaType(field);
                        schemaFields[field.fieldname] = fieldType;
                    }

                    sql = $"db.createCollection(\"{entity.EntityName}\", {{ validator: {{ $jsonSchema: {{ bsonType: \"object\", required: [{string.Join(", ", entity.Fields.Where(f => !f.AllowDBNull).Select(f => $"\"{f.fieldname}\""))}], properties: {System.Text.Json.JsonSerializer.Serialize(schemaFields)} }} }} }})";
                    break;

                case DataSourceType.Couchbase:
                    // For Couchbase, we return a statement to create a scope and collection
                    string scope = !string.IsNullOrEmpty(entity.SchemaOrOwnerOrDatabase)
                        ? entity.SchemaOrOwnerOrDatabase
                        : "default_scope";

                    sql = $"CREATE SCOPE IF NOT EXISTS {scope};\nCREATE COLLECTION IF NOT EXISTS {scope}.{entity.EntityName}";
                    break;

                default:
                    sql = $"-- Create table syntax for {entity.DatabaseType} is not implemented";
                    break;
            }

            return sql;
        }

        /// <summary>
        /// Gets the primary key constraint syntax for a specific database type
        /// </summary>
        private static string GetPrimaryKeyConstraintSyntax(DataSourceType dbType, string constraintName, string columns)
        {
            switch (dbType)
            {
                case DataSourceType.SqlServer:
                case DataSourceType.AzureSQL:
                case DataSourceType.Oracle:
                case DataSourceType.Postgre:
                case DataSourceType.DB2:
                case DataSourceType.SnowFlake:
                    return $"CONSTRAINT {GetFormattedConstraintName(dbType, constraintName)} PRIMARY KEY ({columns})";

                case DataSourceType.Mysql:
                    return $"PRIMARY KEY ({columns})";

                case DataSourceType.SqlLite:
                    return $"PRIMARY KEY ({columns})";

                case DataSourceType.FireBird:
                    return $"CONSTRAINT {GetFormattedConstraintName(dbType, constraintName)} PRIMARY KEY ({columns})";

                default:
                    return $"PRIMARY KEY ({columns})";
            }
        }

        /// <summary>
        /// Formats column names according to database syntax conventions
        /// </summary>
        private static string GetFormattedColumnName(DataSourceType dbType, string columnName)
        {
            switch (dbType)
            {
                case DataSourceType.SqlServer:
                case DataSourceType.AzureSQL:
                    return $"[{columnName}]";

                case DataSourceType.Oracle:
                case DataSourceType.Postgre:
                case DataSourceType.DB2:
                case DataSourceType.SnowFlake:
                case DataSourceType.FireBird:
                    return $"\"{columnName}\"";

                case DataSourceType.Mysql:
                    return $"`{columnName}`";

                case DataSourceType.SqlLite:
                default:
                    return $"\"{columnName}\"";
            }
        }

        /// <summary>
        /// Formats constraint names according to database syntax conventions
        /// </summary>
        private static string GetFormattedConstraintName(DataSourceType dbType, string constraintName)
        {
            switch (dbType)
            {
                case DataSourceType.SqlServer:
                case DataSourceType.AzureSQL:
                    return $"[{constraintName}]";

                case DataSourceType.Oracle:
                case DataSourceType.Postgre:
                case DataSourceType.DB2:
                case DataSourceType.SnowFlake:
                case DataSourceType.FireBird:
                    return $"\"{constraintName}\"";

                case DataSourceType.Mysql:
                    return $"`{constraintName}`";

                default:
                    return $"\"{constraintName}\"";
            }
        }

        /// <summary>
        /// Gets MongoDB schema type definition for a field
        /// </summary>
        private static Dictionary<string, object> GetMongoDBSchemaType(EntityField field)
        {
            var result = new Dictionary<string, object>();

            string baseType = field.fieldtype.ToLower();

            if (baseType.Contains("int") || baseType.Contains("decimal") || baseType.Contains("numeric") ||
                baseType.Contains("float") || baseType.Contains("double"))
            {
                result["bsonType"] = "number";
            }
            else if (baseType.Contains("bool"))
            {
                result["bsonType"] = "bool";
            }
            else if (baseType.Contains("date"))
            {
                result["bsonType"] = "date";
            }
            else if (baseType.Contains("binary") || baseType.Contains("blob"))
            {
                result["bsonType"] = "binData";
            }
            else
            {
                result["bsonType"] = "string";
            }

            return result;
        }

        /// <summary>
        /// Generates SQL to create a database or schema
        /// </summary>
        /// <param name="dataSourceType">Database type</param>
        /// <param name="databaseName">Name of the database or schema to create</param>
        /// <returns>A tuple containing the SQL statement, success flag, and any error message</returns>
        public static (string Sql, bool Success, string ErrorMessage) GenerateCreateDatabaseSQL(
            DataSourceType dataSourceType,
            string databaseName)
        {
            if (string.IsNullOrWhiteSpace(databaseName))
                return (null, false, "Database name cannot be empty");

            try
            {
                string sql;

                switch (dataSourceType)
                {
                    case DataSourceType.SqlServer:
                    case DataSourceType.AzureSQL:
                        sql = $"CREATE DATABASE [{databaseName}]";
                        break;

                    case DataSourceType.Mysql:
                        sql = $"CREATE DATABASE `{databaseName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci";
                        break;

                    case DataSourceType.Oracle:
                        // Oracle doesn't directly create databases in the same way, you create users/schemas
                        sql = $"CREATE USER {databaseName} IDENTIFIED BY password DEFAULT TABLESPACE USERS TEMPORARY TABLESPACE TEMP;\nGRANT CONNECT, RESOURCE TO {databaseName}";
                        break;

                    case DataSourceType.Postgre:
                        sql = $"CREATE DATABASE \"{databaseName}\" WITH ENCODING = 'UTF8'";
                        break;

                    case DataSourceType.SqlLite:
                        // SQLite creates databases as files, no SQL required
                        sql = $"-- For SQLite, create a new connection to file: {databaseName}.db";
                        break;

                    case DataSourceType.DB2:
                        sql = $"CREATE DATABASE {databaseName} USING CODESET UTF-8 TERRITORY US";
                        break;

                    case DataSourceType.SnowFlake:
                        sql = $"CREATE DATABASE {databaseName}";
                        break;

                    case DataSourceType.MongoDB:
                        sql = $"use {databaseName}";
                        break;

                    case DataSourceType.Couchbase:
                        sql = $"CREATE BUCKET {databaseName} WITH (ramQuota=100)";
                        break;

                    default:
                        return (null, false, $"Database creation not implemented for {dataSourceType}");
                }

                return (sql, true, null);
            }
            catch (Exception ex)
            {
                return (null, false, $"Error generating create database SQL: {ex.Message}");
            }
        }

        /// <summary>
        /// Generates SQL to create a schema within a database
        /// </summary>
        /// <param name="dataSourceType">Database type</param>
        /// <param name="schemaName">Name of the schema to create</param>
        /// <returns>A tuple containing the SQL statement, success flag, and any error message</returns>
        public static (string Sql, bool Success, string ErrorMessage) GenerateCreateSchemaSQL(
            DataSourceType dataSourceType,
            string schemaName)
        {
            if (string.IsNullOrWhiteSpace(schemaName))
                return (null, false, "Schema name cannot be empty");

            try
            {
                string sql;

                switch (dataSourceType)
                {
                    case DataSourceType.SqlServer:
                    case DataSourceType.AzureSQL:
                        sql = $"CREATE SCHEMA [{schemaName}]";
                        break;

                    case DataSourceType.Postgre:
                        sql = $"CREATE SCHEMA \"{schemaName}\"";
                        break;

                    case DataSourceType.DB2:
                        sql = $"CREATE SCHEMA {schemaName}";
                        break;

                    case DataSourceType.SnowFlake:
                        sql = $"CREATE SCHEMA {schemaName}";
                        break;

                    case DataSourceType.Oracle:
                        // Oracle uses users as schemas
                        sql = $"CREATE USER {schemaName} IDENTIFIED BY password DEFAULT TABLESPACE USERS TEMPORARY TABLESPACE TEMP;\nGRANT CONNECT, RESOURCE TO {schemaName}";
                        break;

                    case DataSourceType.Mysql:
                        // MySQL uses databases as schemas
                        sql = $"CREATE DATABASE `{schemaName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci";
                        break;

                    case DataSourceType.SqlLite:
                        // SQLite doesn't support schemas
                        return (null, false, "SQLite does not support schemas");

                    default:
                        return (null, false, $"Schema creation not implemented for {dataSourceType}");
                }

                return (sql, true, null);
            }
            catch (Exception ex)
            {
                return (null, false, $"Error generating create schema SQL: {ex.Message}");
            }
        }

        /// <summary>
        /// Generates SQL to create a sequence in a database
        /// </summary>
        /// <param name="dataSourceType">Database type</param>
        /// <param name="sequenceName">Name of the sequence to create</param>
        /// <param name="startWith">Initial value for the sequence</param>
        /// <param name="incrementBy">Increment value for the sequence</param>
        /// <param name="schemaName">Optional schema name for the sequence</param>
        /// <returns>A tuple containing the SQL statement, success flag, and any error message</returns>
        public static (string Sql, bool Success, string ErrorMessage) GenerateCreateSequenceSQL(
            DataSourceType dataSourceType,
            string sequenceName,
            long startWith = 1,
            int incrementBy = 1,
            string schemaName = null)
        {
            if (string.IsNullOrWhiteSpace(sequenceName))
                return (null, false, "Sequence name cannot be empty");

            try
            {
                string sql;
                string sequenceFullName = !string.IsNullOrEmpty(schemaName)
                    ? GetFormattedSchemaObjectName(dataSourceType, schemaName, sequenceName)
                    : GetFormattedObjectName(dataSourceType, sequenceName);

                switch (dataSourceType)
                {
                    case DataSourceType.SqlServer:
                    case DataSourceType.AzureSQL:
                        sql = $"CREATE SEQUENCE {sequenceFullName} START WITH {startWith} INCREMENT BY {incrementBy}";
                        break;

                    case DataSourceType.Oracle:
                        sql = $"CREATE SEQUENCE {sequenceFullName} START WITH {startWith} INCREMENT BY {incrementBy} NOCACHE";
                        break;

                    case DataSourceType.Postgre:
                        sql = $"CREATE SEQUENCE {sequenceFullName} START {startWith} INCREMENT {incrementBy}";
                        break;

                    case DataSourceType.DB2:
                        sql = $"CREATE SEQUENCE {sequenceFullName} START WITH {startWith} INCREMENT BY {incrementBy} NO CACHE NO CYCLE";
                        break;

                    case DataSourceType.FireBird:
                        sql = $"CREATE SEQUENCE {sequenceFullName} START WITH {startWith} INCREMENT BY {incrementBy}";
                        break;

                    case DataSourceType.SnowFlake:
                        sql = $"CREATE SEQUENCE {sequenceFullName} START = {startWith} INCREMENT = {incrementBy}";
                        break;

                    case DataSourceType.Mysql:
                        // MySQL 8.0+ supports sequences
                        sql = $"CREATE SEQUENCE {sequenceFullName} START WITH {startWith} INCREMENT BY {incrementBy}";
                        break;

                    // Databases that don't support sequences
                    case DataSourceType.SqlLite:
                        return (null, false, "SQLite does not support sequences");

                    case DataSourceType.MongoDB:
                    case DataSourceType.Couchbase:
                    case DataSourceType.Redis:
                        return (null, false, $"{dataSourceType} does not support SQL sequences");

                    default:
                        return (null, false, $"Sequence creation not implemented for {dataSourceType}");
                }

                return (sql, true, null);
            }
            catch (Exception ex)
            {
                return (null, false, $"Error generating create sequence SQL: {ex.Message}");
            }
        }

        /// <summary>
        /// Generates SQL to create a view in a database
        /// </summary>
        /// <param name="dataSourceType">Database type</param>
        /// <param name="viewName">Name of the view to create</param>
        /// <param name="selectStatement">The SELECT statement that defines the view</param>
        /// <param name="schemaName">Optional schema name for the view</param>
        /// <param name="replaceIfExists">Whether to replace the view if it exists</param>
        /// <returns>A tuple containing the SQL statement, success flag, and any error message</returns>
        public static (string Sql, bool Success, string ErrorMessage) GenerateCreateViewSQL(
            DataSourceType dataSourceType,
            string viewName,
            string selectStatement,
            string schemaName = null,
            bool replaceIfExists = false)
        {
            if (string.IsNullOrWhiteSpace(viewName))
                return (null, false, "View name cannot be empty");

            if (string.IsNullOrWhiteSpace(selectStatement))
                return (null, false, "SELECT statement cannot be empty");

            try
            {
                string sql;
                string viewFullName = !string.IsNullOrEmpty(schemaName)
                    ? GetFormattedSchemaObjectName(dataSourceType, schemaName, viewName)
                    : GetFormattedObjectName(dataSourceType, viewName);

                string createOrReplace = replaceIfExists ? "CREATE OR REPLACE" : "CREATE";

                switch (dataSourceType)
                {
                    case DataSourceType.SqlServer:
                    case DataSourceType.AzureSQL:
                        if (replaceIfExists)
                        {
                            sql = $"IF OBJECT_ID('{viewFullName}', 'V') IS NOT NULL\n    DROP VIEW {viewFullName};\nGO\nCREATE VIEW {viewFullName} AS\n{selectStatement}";
                        }
                        else
                        {
                            sql = $"CREATE VIEW {viewFullName} AS\n{selectStatement}";
                        }
                        break;

                    case DataSourceType.Oracle:
                    case DataSourceType.Postgre:
                    case DataSourceType.Mysql:
                    case DataSourceType.DB2:
                    case DataSourceType.SnowFlake:
                        sql = $"{createOrReplace} VIEW {viewFullName} AS\n{selectStatement}";
                        break;

                    case DataSourceType.SqlLite:
                        if (replaceIfExists)
                        {
                            sql = $"DROP VIEW IF EXISTS {viewFullName};\nCREATE VIEW {viewFullName} AS\n{selectStatement}";
                        }
                        else
                        {
                            sql = $"CREATE VIEW {viewFullName} AS\n{selectStatement}";
                        }
                        break;

                    // Databases that have different syntax for views
                    case DataSourceType.FireBird:
                        if (replaceIfExists)
                        {
                            sql = $"RECREATE VIEW {viewFullName} AS\n{selectStatement}";
                        }
                        else
                        {
                            sql = $"CREATE VIEW {viewFullName} AS\n{selectStatement}";
                        }
                        break;

                    case DataSourceType.MongoDB:
                        return (null, false, "MongoDB does not support SQL views in the traditional sense. Use aggregation pipelines instead.");

                    case DataSourceType.Couchbase:
                        return (null, false, "Use Couchbase's N1QL to create a similar functionality with 'CREATE INDEX' commands.");

                    default:
                        return (null, false, $"View creation not implemented for {dataSourceType}");
                }

                return (sql, true, null);
            }
            catch (Exception ex)
            {
                return (null, false, $"Error generating create view SQL: {ex.Message}");
            }
        }

        /// <summary>
        /// Formats a schema-qualified object name according to database syntax conventions
        /// </summary>
        private static string GetFormattedSchemaObjectName(DataSourceType dbType, string schemaName, string objectName)
        {
            switch (dbType)
            {
                case DataSourceType.SqlServer:
                case DataSourceType.AzureSQL:
                    return $"[{schemaName}].[{objectName}]";

                case DataSourceType.Oracle:
                case DataSourceType.Postgre:
                case DataSourceType.DB2:
                case DataSourceType.SnowFlake:
                    return $"\"{schemaName}\".\"{objectName}\"";

                case DataSourceType.Mysql:
                    return $"`{schemaName}`.`{objectName}`";

                case DataSourceType.SqlLite:
                case DataSourceType.FireBird:
                default:
                    return $"\"{objectName}\"";
            }
        }

        /// <summary>
        /// Formats an object name according to database syntax conventions
        /// </summary>
        private static string GetFormattedObjectName(DataSourceType dbType, string objectName)
        {
            switch (dbType)
            {
                case DataSourceType.SqlServer:
                case DataSourceType.AzureSQL:
                    return $"[{objectName}]";

                case DataSourceType.Oracle:
                case DataSourceType.Postgre:
                case DataSourceType.DB2:
                case DataSourceType.SnowFlake:
                case DataSourceType.FireBird:
                    return $"\"{objectName}\"";

                case DataSourceType.Mysql:
                    return $"`{objectName}`";

                case DataSourceType.SqlLite:
                default:
                    return $"\"{objectName}\"";
            }
        }

        /// <summary>
        /// Generates SQL to create a stored procedure in a database
        /// </summary>
        /// <param name="dataSourceType">Database type</param>
        /// <param name="procedureName">Name of the stored procedure</param>
        /// <param name="parameters">List of parameters with their types</param>
        /// <param name="procedureBody">The body of the stored procedure</param>
        /// <param name="schemaName">Optional schema name</param>
        /// <param name="replaceIfExists">Whether to replace if exists</param>
        /// <returns>A tuple containing the SQL statement, success flag, and any error message</returns>
        public static (string Sql, bool Success, string ErrorMessage) GenerateCreateProcedureSQL(
            DataSourceType dataSourceType,
            string procedureName,
            List<(string Name, string DataType, string Direction)> parameters,
            string procedureBody,
            string schemaName = null,
            bool replaceIfExists = false)
        {
            if (string.IsNullOrWhiteSpace(procedureName))
                return (null, false, "Procedure name cannot be empty");

            if (string.IsNullOrWhiteSpace(procedureBody))
                return (null, false, "Procedure body cannot be empty");

            try
            {
                string fullProcName = !string.IsNullOrEmpty(schemaName)
                    ? GetFormattedSchemaObjectName(dataSourceType, schemaName, procedureName)
                    : GetFormattedObjectName(dataSourceType, procedureName);

                string parameterList = FormatProcedureParameters(dataSourceType, parameters);
                string sql;

                switch (dataSourceType)
                {
                    case DataSourceType.SqlServer:
                    case DataSourceType.AzureSQL:
                        string dropIfExists = replaceIfExists
                            ? $"IF OBJECT_ID('{fullProcName}', 'P') IS NOT NULL\n    DROP PROCEDURE {fullProcName};\nGO\n"
                            : "";

                        sql = $"{dropIfExists}CREATE PROCEDURE {fullProcName}\n{parameterList}\nAS\nBEGIN\n{procedureBody}\nEND";
                        break;

                    case DataSourceType.Oracle:
                        string oracleCreateOrReplace = replaceIfExists ? "CREATE OR REPLACE" : "CREATE";
                        sql = $"{oracleCreateOrReplace} PROCEDURE {fullProcName}\n{parameterList}\nAS\nBEGIN\n{procedureBody}\nEND;";
                        break;

                    case DataSourceType.Mysql:
                        string mysqlDropIfExists = replaceIfExists
                            ? $"DROP PROCEDURE IF EXISTS {fullProcName};\n"
                            : "";

                        sql = $"{mysqlDropIfExists}CREATE PROCEDURE {fullProcName}\n{parameterList}\nBEGIN\n{procedureBody}\nEND";
                        break;

                    case DataSourceType.Postgre:
                        string pgCreateOrReplace = replaceIfExists ? "CREATE OR REPLACE" : "CREATE";
                        sql = $"{pgCreateOrReplace} FUNCTION {fullProcName}\n{parameterList}\nRETURNS VOID AS $$\nBEGIN\n{procedureBody}\nEND;\n$$ LANGUAGE plpgsql;";
                        break;

                    case DataSourceType.DB2:
                        string db2DropIfExists = replaceIfExists
                            ? $"BEGIN\n  DECLARE CONTINUE HANDLER FOR SQLSTATE '42704' BEGIN END;\n  EXECUTE IMMEDIATE 'DROP PROCEDURE {fullProcName}';\nEND;\n"
                            : "";

                        sql = $"{db2DropIfExists}CREATE PROCEDURE {fullProcName}\n{parameterList}\nBEGIN\n{procedureBody}\nEND";
                        break;

                    case DataSourceType.SqlLite:
                        // SQLite doesn't have stored procedures
                        return (null, false, "SQLite does not support stored procedures");

                    default:
                        return (null, false, $"Procedure creation is not implemented for {dataSourceType}");
                }

                return (sql, true, null);
            }
            catch (Exception ex)
            {
                return (null, false, $"Error generating create procedure SQL: {ex.Message}");
            }
        }

        /// <summary>
        /// Formats procedure parameters according to the database requirements
        /// </summary>
        private static string FormatProcedureParameters(
            DataSourceType dbType,
            List<(string Name, string DataType, string Direction)> parameters)
        {
            if (parameters == null || parameters.Count == 0)
            {
                // Return empty parameter list for the specific database
                switch (dbType)
                {
                    case DataSourceType.SqlServer:
                    case DataSourceType.AzureSQL:
                    case DataSourceType.Mysql:
                    case DataSourceType.DB2:
                        return "()";
                    case DataSourceType.Oracle:
                        return "";
                    case DataSourceType.Postgre:
                        return "()";
                    default:
                        return "";
                }
            }

            var formattedParams = new List<string>();

            foreach (var param in parameters)
            {
                string formattedParam;

                switch (dbType)
                {
                    case DataSourceType.SqlServer:
                    case DataSourceType.AzureSQL:
                        string sqlServerDirection = param.Direction.ToUpper() == "OUT" ? "OUTPUT" : param.Direction;
                        formattedParam = $"@{param.Name} {param.DataType} {sqlServerDirection}".Trim();
                        break;

                    case DataSourceType.Oracle:
                        formattedParam = $"{param.Name} {param.Direction} {param.DataType}".Trim();
                        break;

                    case DataSourceType.Mysql:
                        string mysqlDirection = param.Direction.ToUpper() switch
                        {
                            "IN" => "IN",
                            "OUT" => "OUT",
                            "INOUT" => "INOUT",
                            _ => ""
                        };
                        formattedParam = $"{mysqlDirection} {param.Name} {param.DataType}".Trim();
                        break;

                    case DataSourceType.Postgre:
                        string pgDirection = param.Direction.ToUpper() switch
                        {
                            "IN" => "IN",
                            "OUT" => "OUT", // Note: For PostgreSQL return values, this would be handled differently
                            "INOUT" => "INOUT",
                            _ => "IN" // Default to IN
                        };
                        formattedParam = $"{param.Name} {pgDirection} {param.DataType}".Trim();
                        break;

                    case DataSourceType.DB2:
                        string db2Direction = param.Direction.ToUpper() switch
                        {
                            "IN" => "IN",
                            "OUT" => "OUT",
                            "INOUT" => "INOUT",
                            _ => "IN" // Default to IN
                        };
                        formattedParam = $"{db2Direction} {param.Name} {param.DataType}".Trim();
                        break;

                    default:
                        formattedParam = $"{param.Name} {param.DataType}";
                        break;
                }

                formattedParams.Add(formattedParam);
            }

            // Format the full parameter list according to database syntax
            string result = string.Join(", ", formattedParams);

            // Add parentheses if needed
            switch (dbType)
            {
                case DataSourceType.SqlServer:
                case DataSourceType.AzureSQL:
                case DataSourceType.Mysql:
                case DataSourceType.DB2:
                case DataSourceType.Postgre:
                    result = $"({result})";
                    break;
                    // Oracle doesn't use parentheses around parameter lists
            }

            return result;
        }
        #endregion "Create"
        #region "Index"
        /// <summary>
        /// Generates a SQL query to create an index on a table with advanced options.
        /// </summary>
        /// <param name="rdbms">Database type for which to generate the SQL</param>
        /// <param name="tableName">Name of the table to create the index on</param>
        /// <param name="indexName">Name of the index to create</param>
        /// <param name="columns">Array of column names to include in the index</param>
        /// <param name="options">Optional dictionary of index creation options</param>
        /// <returns>A SQL statement for creating the index</returns>
        /// <remarks>
        /// The options parameter can include:
        /// - "unique": bool - Whether to create a unique index
        /// - "clustered": bool - For SQL Server, whether to create a clustered index
        /// - "concurrently": bool - For PostgreSQL, whether to create the index concurrently 
        /// - "includeColumns": string[] - For SQL Server, columns to include but not index
        /// - "filterPredicate": string - For SQL Server/PostgreSQL, WHERE clause for filtered indexes
        /// - "tablespace": string - For Oracle/PostgreSQL, tablespace for the index
        /// - "fillFactor": int - For SQL Server, percentage of fullness for index pages
        /// </remarks>
        public static string GenerateCreateIndexQuery(
            DataSourceType rdbms,
            string tableName,
            string indexName,
            string[] columns,
            Dictionary<string, object> options = null)
        {
            if (string.IsNullOrEmpty(tableName))
                throw new ArgumentException("Table name cannot be null or empty", nameof(tableName));

            if (string.IsNullOrEmpty(indexName))
                throw new ArgumentException("Index name cannot be null or empty", nameof(indexName));

            if (columns == null || columns.Length == 0)
                throw new ArgumentException("At least one column must be specified", nameof(columns));

            // Default options if null
            options = options ?? new Dictionary<string, object>();

            // Extract common options
            bool unique = options.ContainsKey("unique") && (bool)options["unique"];

            // Format the column list according to database conventions
            string columnList = FormatIndexColumns(rdbms, columns, options);

            // Build the SQL statement based on database type
            switch (rdbms)
            {
                case DataSourceType.SqlServer:
                case DataSourceType.AzureSQL:
                    return GenerateSqlServerIndexQuery(tableName, indexName, columnList, options);

                case DataSourceType.Oracle:
                    return GenerateOracleIndexQuery(tableName, indexName, columnList, options);

                case DataSourceType.Postgre:
                    return GeneratePostgreSqlIndexQuery(tableName, indexName, columnList, options);

                case DataSourceType.Mysql:
                    return GenerateMySqlIndexQuery(tableName, indexName, columnList, options);

                case DataSourceType.DB2:
                    return GenerateDb2IndexQuery(tableName, indexName, columnList, options);

                case DataSourceType.SqlLite:
                    return GenerateSqliteIndexQuery(tableName, indexName, columnList, options);

                case DataSourceType.SnowFlake:
                    return GenerateSnowflakeIndexQuery(tableName, indexName, columnList, options);

                default:
                    // Generic fallback
                    return $"CREATE {(unique ? "UNIQUE " : "")}INDEX {indexName} ON {tableName} ({columnList})";
            }
        }

        // Helper methods for database-specific index creation
        private static string GenerateSqlServerIndexQuery(string tableName, string indexName, string columnList,
            Dictionary<string, object> options)
        {
            bool unique = options.ContainsKey("unique") && (bool)options["unique"];
            bool clustered = options.ContainsKey("clustered") && (bool)options["clustered"];

            string indexType = clustered ? "CLUSTERED" : "NONCLUSTERED";
            string includeClause = "";
            string whereClause = "";
            string withOptions = "";

            // Handle INCLUDE columns
            if (options.ContainsKey("includeColumns") && options["includeColumns"] is string[] includeColumns && includeColumns.Length > 0)
            {
                includeClause = $" INCLUDE ([{string.Join("], [", includeColumns)}])";
            }

            // Handle filtered indexes
            if (options.ContainsKey("filterPredicate") && options["filterPredicate"] is string filterPredicate &&
                !string.IsNullOrEmpty(filterPredicate))
            {
                whereClause = $" WHERE {filterPredicate}";
            }

            // Handle additional options
            List<string> withOptionsList = new List<string>();

            if (options.ContainsKey("fillFactor") && options["fillFactor"] is int fillFactor && fillFactor > 0 && fillFactor <= 100)
            {
                withOptionsList.Add($"FILLFACTOR = {fillFactor}");
            }

            if (options.ContainsKey("online") && options["online"] is bool online && online)
            {
                withOptionsList.Add("ONLINE = ON");
            }

            if (withOptionsList.Count > 0)
            {
                withOptions = $" WITH ({string.Join(", ", withOptionsList)})";
            }

            return $"CREATE {(unique ? "UNIQUE " : "")}{indexType} INDEX [{indexName}] ON [{tableName}] ({columnList}){includeClause}{whereClause}{withOptions}";
        }

        private static string GenerateOracleIndexQuery(string tableName, string indexName, string columnList,
            Dictionary<string, object> options)
        {
            bool unique = options.ContainsKey("unique") && (bool)options["unique"];
            string tablespaceClause = "";
            string parallelClause = "";

            if (options.ContainsKey("tablespace") && options["tablespace"] is string tablespace &&
                !string.IsNullOrEmpty(tablespace))
            {
                tablespaceClause = $" TABLESPACE {tablespace}";
            }

            if (options.ContainsKey("parallel") && options["parallel"] is int parallelDegree && parallelDegree > 1)
            {
                parallelClause = $" PARALLEL {parallelDegree}";
            }

            return $"CREATE {(unique ? "UNIQUE " : "")}INDEX \"{indexName}\" ON \"{tableName}\" ({columnList}){tablespaceClause}{parallelClause}";
        }

        private static string GeneratePostgreSqlIndexQuery(string tableName, string indexName, string columnList,
            Dictionary<string, object> options)
        {
            bool unique = options.ContainsKey("unique") && (bool)options["unique"];
            bool concurrently = options.ContainsKey("concurrently") && (bool)options["concurrently"];
            string method = options.ContainsKey("method") ? options["method"].ToString() : "BTREE";
            string whereClause = "";
            string tablespaceClause = "";

            // Handle filtered indexes
            if (options.ContainsKey("filterPredicate") && options["filterPredicate"] is string filterPredicate &&
                !string.IsNullOrEmpty(filterPredicate))
            {
                whereClause = $" WHERE {filterPredicate}";
            }

            if (options.ContainsKey("tablespace") && options["tablespace"] is string tablespace &&
                !string.IsNullOrEmpty(tablespace))
            {
                tablespaceClause = $" TABLESPACE \"{tablespace}\"";
            }

            return $"CREATE {(unique ? "UNIQUE " : "")}{(concurrently ? "INDEX CONCURRENTLY " : "INDEX ")}\"{indexName}\" ON \"{tableName}\" USING {method} ({columnList}){whereClause}{tablespaceClause}";
        }

        private static string GenerateMySqlIndexQuery(string tableName, string indexName, string columnList,
            Dictionary<string, object> options)
        {
            bool unique = options.ContainsKey("unique") && (bool)options["unique"];
            string indexType = "";

            if (options.ContainsKey("indexType") && options["indexType"] is string idxType &&
                !string.IsNullOrEmpty(idxType))
            {
                indexType = $" USING {idxType}";
            }

            return $"CREATE {(unique ? "UNIQUE " : "")}INDEX `{indexName}` ON `{tableName}`{indexType} ({columnList})";
        }

        private static string GenerateDb2IndexQuery(string tableName, string indexName, string columnList,
            Dictionary<string, object> options)
        {
            bool unique = options.ContainsKey("unique") && (bool)options["unique"];
            string clusterClause = "";

            if (options.ContainsKey("clustered") && (bool)options["clustered"])
            {
                clusterClause = " CLUSTER";
            }

            return $"CREATE {(unique ? "UNIQUE " : "")}INDEX \"{indexName}\" ON \"{tableName}\" ({columnList}){clusterClause}";
        }

        private static string GenerateSqliteIndexQuery(string tableName, string indexName, string columnList,
            Dictionary<string, object> options)
        {
            bool unique = options.ContainsKey("unique") && (bool)options["unique"];
            string whereClause = "";

            // Handle partial indexes
            if (options.ContainsKey("filterPredicate") && options["filterPredicate"] is string filterPredicate &&
                !string.IsNullOrEmpty(filterPredicate))
            {
                whereClause = $" WHERE {filterPredicate}";
            }

            return $"CREATE {(unique ? "UNIQUE " : "")}INDEX \"{indexName}\" ON \"{tableName}\" ({columnList}){whereClause}";
        }

        private static string GenerateSnowflakeIndexQuery(string tableName, string indexName, string columnList,
            Dictionary<string, object> options)
        {
            // Snowflake doesn't support explicit index creation, so this is a comment
            return $"/* Snowflake doesn't support explicit CREATE INDEX commands. Consider using clustering keys instead: ALTER TABLE {tableName} CLUSTER BY ({columnList}) */";
        }

        // Format columns for the index with sort direction and additional options
        private static string FormatIndexColumns(DataSourceType rdbms, string[] columns, Dictionary<string, object> options)
        {
            string[] sortDirs = null;
            if (options.ContainsKey("sortDirections") && options["sortDirections"] is string[] dirs)
            {
                sortDirs = dirs;
            }

            List<string> formattedColumns = new List<string>();

            for (int i = 0; i < columns.Length; i++)
            {
                string col = columns[i];
                string dir = (sortDirs != null && i < sortDirs.Length) ? sortDirs[i] : "";

                switch (rdbms)
                {
                    case DataSourceType.SqlServer:
                    case DataSourceType.AzureSQL:
                        formattedColumns.Add($"[{col}]{(string.IsNullOrEmpty(dir) ? "" : " " + dir)}");
                        break;

                    case DataSourceType.Mysql:
                        formattedColumns.Add($"`{col}`{(string.IsNullOrEmpty(dir) ? "" : " " + dir)}");
                        break;

                    case DataSourceType.Oracle:
                    case DataSourceType.Postgre:
                    case DataSourceType.DB2:
                    case DataSourceType.SnowFlake:
                        formattedColumns.Add($"\"{col}\"{(string.IsNullOrEmpty(dir) ? "" : " " + dir)}");
                        break;

                    default:
                        formattedColumns.Add($"{col}{(string.IsNullOrEmpty(dir) ? "" : " " + dir)}");
                        break;
                }
            }

            return string.Join(", ", formattedColumns);
        }

        #endregion "Index"
        #region "Constraints"
        /// <summary>
        /// Generates a query to drop a primary key constraint from a table.
        /// </summary>
        /// <param name="rdbms">The type of the database management system.</param>
        /// <param name="tableName">The name of the table.</param>
        /// <param name="constraintName">The name of the primary key constraint.</param>
        /// <returns>A query to drop the primary key constraint.</returns>
        /// <remarks>
        /// For SQL Server and PostgreSQL, the query will be in the form "ALTER TABLE [tableName] DROP CONSTRAINT [constraintName]".
        /// For MySQL, Oracle, and DB2, the query will be in the form "ALTER TABLE [tableName] DROP PRIMARY KEY".
        /// For Firebird, the query will drop the named constraint.
        /// For SQLite, table recreation is required as it doesn't support direct constraint dropping.
        /// </remarks>
        public static string GenerateDropPrimaryKeyQuery(DataSourceType rdbms, string tableName, string constraintName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Table name cannot be null or empty", nameof(tableName));

            string query;

            switch (rdbms)
            {
                case DataSourceType.SqlServer:
                case DataSourceType.AzureSQL:
                    query = $"ALTER TABLE [{tableName}] DROP CONSTRAINT [{constraintName}]";
                    break;
                case DataSourceType.Mysql:
                    query = $"ALTER TABLE `{tableName}` DROP PRIMARY KEY";
                    break;
                case DataSourceType.Postgre:
                    query = $"ALTER TABLE \"{tableName}\" DROP CONSTRAINT \"{constraintName}\"";
                    break;
                case DataSourceType.Oracle:
                    query = $"ALTER TABLE \"{tableName}\" DROP PRIMARY KEY";
                    break;
                case DataSourceType.DB2:
                    query = $"ALTER TABLE \"{tableName}\" DROP PRIMARY KEY";
                    break;
                case DataSourceType.FireBird:
                    query = $"ALTER TABLE \"{tableName}\" DROP CONSTRAINT \"{constraintName}\"";
                    break;
                case DataSourceType.SqlLite:
                    query = "-- SQLite requires recreating the table to drop primary key.";
                    break;
                case DataSourceType.Couchbase:
                case DataSourceType.Redis:
                case DataSourceType.MongoDB:
                    query = "-- NoSQL databases typically do not have primary key constraints in the same way RDBMS do.";
                    break;
                default:
                    query = "-- RDBMS not supported.";
                    break;
            }

            return query;
        }

        /// <summary>
        /// Generates a SQL query to drop a foreign key constraint in a specified RDBMS.
        /// </summary>
        /// <param name="rdbms">The type of RDBMS.</param>
        /// <param name="tableName">The name of the table.</param>
        /// <param name="constraintName">The name of the foreign key constraint.</param>
        /// <returns>A SQL query to drop the specified foreign key constraint.</returns>
        /// <remarks>
        /// The generated query varies depending on the RDBMS:
        /// - For SQL Server: ALTER TABLE [tableName] DROP CONSTRAINT [constraintName]
        /// - For MySQL: ALTER TABLE `tableName` DROP FOREIGN KEY `constraintName`
        /// - For PostgreSQL: ALTER TABLE "tableName" DROP CONSTRAINT "constraintName"
        /// - For Oracle: ALTER TABLE "tableName" DROP CONSTRAINT "constraintName"
        /// - For DB2: ALTER TABLE "tableName" DROP FOREIGN KEY "constraintName"
        /// - For Firebird: ALTER TABLE "tableName" DROP CONSTRAINT "constraintName"
        /// </remarks>
        public static string GenerateDropForeignKeyQuery(DataSourceType rdbms, string tableName, string constraintName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Table name cannot be null or empty", nameof(tableName));

            if (string.IsNullOrWhiteSpace(constraintName))
                throw new ArgumentException("Constraint name cannot be null or empty", nameof(constraintName));

            string query;

            switch (rdbms)
            {
                case DataSourceType.SqlServer:
                case DataSourceType.AzureSQL:
                    query = $"ALTER TABLE [{tableName}] DROP CONSTRAINT [{constraintName}]";
                    break;
                case DataSourceType.Mysql:
                    query = $"ALTER TABLE `{tableName}` DROP FOREIGN KEY `{constraintName}`";
                    break;
                case DataSourceType.Postgre:
                    query = $"ALTER TABLE \"{tableName}\" DROP CONSTRAINT \"{constraintName}\"";
                    break;
                case DataSourceType.Oracle:
                    query = $"ALTER TABLE \"{tableName}\" DROP CONSTRAINT \"{constraintName}\"";
                    break;
                case DataSourceType.DB2:
                    query = $"ALTER TABLE \"{tableName}\" DROP FOREIGN KEY \"{constraintName}\"";
                    break;
                case DataSourceType.FireBird:
                    query = $"ALTER TABLE \"{tableName}\" DROP CONSTRAINT \"{constraintName}\"";
                    break;
                case DataSourceType.SqlLite:
                    query = "-- SQLite requires recreating the table to drop a foreign key.";
                    break;
                case DataSourceType.Couchbase:
                case DataSourceType.Redis:
                case DataSourceType.MongoDB:
                    query = "-- NoSQL databases typically do not have foreign key constraints in the same way RDBMS do.";
                    break;
                default:
                    query = "-- RDBMS not supported.";
                    break;
            }

            return query;
        }

        /// <summary>
        /// Generates a query to disable a foreign key constraint in a specific RDBMS.
        /// </summary>
        /// <param name="rdbms">The type of RDBMS.</param>
        /// <param name="tableName">The name of the table.</param>
        /// <param name="constraintName">The name of the foreign key constraint.</param>
        /// <returns>A query to disable the specified foreign key constraint.</returns>
        /// <remarks>
        /// The generated query depends on the type of RDBMS specified. The following RDBMS are supported:
        /// - SQL Server: ALTER TABLE [tableName] NOCHECK CONSTRAINT [constraintName]
        /// - Oracle: ALTER TABLE "tableName" DISABLE CONSTRAINT "constraintName"
        /// - PostgreSQL: ALTER TABLE "tableName" DISABLE TRIGGER ALL (disables all FK triggers)
        /// - MySQL: SET FOREIGN_KEY_CHECKS = 0 (disables all FK checks globally)
        /// - DB2: ALTER TABLE "tableName" ALTER FOREIGN KEY "constraintName" NOT ENFORCED
        /// - Firebird: ALTER TABLE "tableName" DISABLE TRIGGER "constraintName"
        /// - SQLite: PRAGMA foreign_keys = OFF (disables all FK checks globally)
        /// </remarks>
        public static string GenerateDisableForeignKeyQuery(DataSourceType rdbms, string tableName, string constraintName)
        {
            if (string.IsNullOrWhiteSpace(tableName) && rdbms != DataSourceType.Mysql && rdbms != DataSourceType.SqlLite)
                throw new ArgumentException("Table name cannot be null or empty for this RDBMS", nameof(tableName));

            if (string.IsNullOrWhiteSpace(constraintName) && rdbms != DataSourceType.Postgre &&
                rdbms != DataSourceType.Mysql && rdbms != DataSourceType.SqlLite)
                throw new ArgumentException("Constraint name cannot be null or empty for this RDBMS", nameof(constraintName));

            string query;

            switch (rdbms)
            {
                case DataSourceType.SqlServer:
                case DataSourceType.AzureSQL:
                    query = $"ALTER TABLE [{tableName}] NOCHECK CONSTRAINT [{constraintName}]";
                    break;
                case DataSourceType.Oracle:
                    query = $"ALTER TABLE \"{tableName}\" DISABLE CONSTRAINT \"{constraintName}\"";
                    break;
                case DataSourceType.Postgre:
                    query = $"ALTER TABLE \"{tableName}\" DISABLE TRIGGER ALL";
                    break;
                case DataSourceType.Mysql:
                    query = "SET FOREIGN_KEY_CHECKS = 0";
                    break;
                case DataSourceType.DB2:
                    query = $"ALTER TABLE \"{tableName}\" ALTER FOREIGN KEY \"{constraintName}\" NOT ENFORCED";
                    break;
                case DataSourceType.FireBird:
                    query = $"ALTER TABLE \"{tableName}\" DISABLE TRIGGER \"{constraintName}\"";
                    break;
                case DataSourceType.SqlLite:
                    query = "PRAGMA foreign_keys = OFF";
                    break;
                case DataSourceType.Couchbase:
                case DataSourceType.Redis:
                case DataSourceType.MongoDB:
                    query = "-- NoSQL databases typically do not have foreign key constraints in the same way RDBMS do.";
                    break;
                default:
                    query = "-- RDBMS not supported.";
                    break;
            }

            return query;
        }

        /// <summary>
        /// Generates a query to enable a foreign key constraint in a specific RDBMS.
        /// </summary>
        /// <param name="rdbms">The type of RDBMS.</param>
        /// <param name="tableName">The name of the table.</param>
        /// <param name="constraintName">The name of the foreign key constraint.</param>
        /// <returns>A query to enable the specified foreign key constraint.</returns>
        /// <remarks>
        /// The generated query depends on the type of RDBMS specified. The following RDBMS are supported:
        /// - SQL Server: ALTER TABLE [tableName] WITH CHECK CHECK CONSTRAINT [constraintName]
        /// - Oracle: ALTER TABLE "tableName" ENABLE CONSTRAINT "constraintName"
        /// - PostgreSQL: ALTER TABLE "tableName" ENABLE TRIGGER ALL (enables all FK triggers)
        /// - MySQL: SET FOREIGN_KEY_CHECKS = 1 (enables all FK checks globally)
        /// - DB2: ALTER TABLE "tableName" ALTER FOREIGN KEY "constraintName" ENFORCED
        /// - Firebird: ALTER TABLE "tableName" ENABLE TRIGGER "constraintName"
        /// - SQLite: PRAGMA foreign_keys = ON (enables all FK checks globally)
        /// </remarks>
        public static string GenerateEnableForeignKeyQuery(DataSourceType rdbms, string tableName, string constraintName)
        {
            if (string.IsNullOrWhiteSpace(tableName) && rdbms != DataSourceType.Mysql && rdbms != DataSourceType.SqlLite)
                throw new ArgumentException("Table name cannot be null or empty for this RDBMS", nameof(tableName));

            if (string.IsNullOrWhiteSpace(constraintName) && rdbms != DataSourceType.Postgre &&
                rdbms != DataSourceType.Mysql && rdbms != DataSourceType.SqlLite)
                throw new ArgumentException("Constraint name cannot be null or empty for this RDBMS", nameof(constraintName));

            string query;

            switch (rdbms)
            {
                case DataSourceType.SqlServer:
                case DataSourceType.AzureSQL:
                    query = $"ALTER TABLE [{tableName}] WITH CHECK CHECK CONSTRAINT [{constraintName}]";
                    break;
                case DataSourceType.Oracle:
                    query = $"ALTER TABLE \"{tableName}\" ENABLE CONSTRAINT \"{constraintName}\"";
                    break;
                case DataSourceType.Postgre:
                    query = $"ALTER TABLE \"{tableName}\" ENABLE TRIGGER ALL";
                    break;
                case DataSourceType.Mysql:
                    query = "SET FOREIGN_KEY_CHECKS = 1";
                    break;
                case DataSourceType.DB2:
                    query = $"ALTER TABLE \"{tableName}\" ALTER FOREIGN KEY \"{constraintName}\" ENFORCED";
                    break;
                case DataSourceType.FireBird:
                    query = $"ALTER TABLE \"{tableName}\" ENABLE TRIGGER \"{constraintName}\"";
                    break;
                case DataSourceType.SqlLite:
                    query = "PRAGMA foreign_keys = ON";
                    break;
                case DataSourceType.Couchbase:
                case DataSourceType.Redis:
                case DataSourceType.MongoDB:
                    query = "-- NoSQL databases typically do not have foreign key constraints in the same way RDBMS do.";
                    break;
                default:
                    query = "-- RDBMS not supported.";
                    break;
            }

            return query;
        }

        /// <summary>
        /// Generates a query to check if a constraint exists on a table in a specific RDBMS.
        /// </summary>
        /// <param name="rdbms">The type of RDBMS.</param>
        /// <param name="tableName">The name of the table.</param>
        /// <param name="constraintName">The name of the constraint to check.</param>
        /// <param name="schemaName">Optional schema name for the table.</param>
        /// <returns>A query that will return records if the constraint exists.</returns>
        public static string GenerateCheckConstraintExistsQuery(DataSourceType rdbms, string tableName, string constraintName, string schemaName = null)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Table name cannot be null or empty", nameof(tableName));

            if (string.IsNullOrWhiteSpace(constraintName))
                throw new ArgumentException("Constraint name cannot be null or empty", nameof(constraintName));

            string query;
            string schema = !string.IsNullOrWhiteSpace(schemaName) ? schemaName : "";

            switch (rdbms)
            {
                case DataSourceType.SqlServer:
                case DataSourceType.AzureSQL:
                    query = $"SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'{schema}.{constraintName}') AND type in (N'C', N'F', N'PK', N'UQ')";
                    break;
                case DataSourceType.Oracle:
                    string owner = !string.IsNullOrWhiteSpace(schema) ? $"AND owner = '{schema}'" : "";
                    query = $"SELECT * FROM all_constraints WHERE CONSTRAINT_NAME = '{constraintName}' AND TABLE_NAME = '{tableName}' {owner}";
                    break;
                case DataSourceType.Postgre:
                    string schemaClause = !string.IsNullOrWhiteSpace(schema) ? $"AND n.nspname = '{schema}'" : "AND n.nspname = 'public'";
                    query = $"SELECT * FROM pg_constraint c JOIN pg_namespace n ON n.oid = c.connamespace JOIN pg_class t ON t.oid = c.conrelid " +
                           $"WHERE c.conname = '{constraintName}' AND t.relname = '{tableName}' {schemaClause}";
                    break;
                case DataSourceType.Mysql:
                    string db = !string.IsNullOrWhiteSpace(schema) ? schema : "DATABASE()";
                    query = $"SELECT * FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_NAME = '{constraintName}' " +
                           $"AND TABLE_NAME = '{tableName}' AND TABLE_SCHEMA = '{db}'";
                    break;
                case DataSourceType.DB2:
                     schemaName = !string.IsNullOrWhiteSpace(schema) ? schema : "CURRENT_SCHEMA";
                    query = $"SELECT * FROM SYSCAT.TABCONST WHERE CONSTNAME = '{constraintName}' AND TABNAME = '{tableName}' AND TABSCHEMA = '{schemaName}'";
                    break;
                case DataSourceType.SqlLite:
                    query = $"SELECT * FROM sqlite_master WHERE type = 'table' AND tbl_name = '{tableName}' AND sql LIKE '%CONSTRAINT%{constraintName}%'";
                    break;
                case DataSourceType.FireBird:
                    query = $"SELECT * FROM RDB$RELATION_CONSTRAINTS WHERE RDB$CONSTRAINT_NAME = '{constraintName}' AND RDB$RELATION_NAME = '{tableName}'";
                    break;
                default:
                    query = "-- Constraint existence check not implemented for this RDBMS.";
                    break;
            }

            return query;
        }
        #endregion "Constraints"

        #region "Meta Data Queries"
        /// <summary>
        /// Creates a list of QuerySqlRepo objects for all supported database types.
        /// </summary>
        /// <returns>A list of QuerySqlRepo objects.</returns>
        public static List<QuerySqlRepo> CreateQuerySqlRepos()
        {
            var repos = new List<QuerySqlRepo>();

            // Add relational database queries
            repos.AddRange(CreateRelationalDatabaseQueries());

            // Add NoSQL database queries
            repos.AddRange(CreateNoSQLDatabaseQueries());

            // Add file-based database queries
            repos.AddRange(CreateFileDatabaseQueries());

            // Add cloud and specialized database queries
            repos.AddRange(CreateCloudDatabaseQueries());

            // Add time-series and specialized database queries
            repos.AddRange(CreateTimeSeriesAndSpecializedDBQueries());

            // Add streaming and messaging platform queries
            repos.AddRange(CreateStreamingPlatformQueries());

            return repos;
        }

        /// <summary>
        /// Creates queries for relational databases (SQL Server, MySQL, Oracle, PostgreSQL, etc.)
        /// </summary>
        private static List<QuerySqlRepo> CreateRelationalDatabaseQueries()
        {
            var repos = new List<QuerySqlRepo>();

            // SQL Server queries
            repos.AddRange(CreateSqlServerQueries());

            // MySQL queries
            repos.AddRange(CreateMySqlQueries());

            // Oracle queries
            repos.AddRange(CreateOracleQueries());

            // PostgreSQL queries
            repos.AddRange(CreatePostgreSqlQueries());

            // SQLite queries
            repos.AddRange(CreateSqliteQueries());

            // DB2 queries
            repos.AddRange(CreateDB2Queries());

            // Firebird queries
            repos.AddRange(CreateFirebirdQueries());
            repos.AddRange(CreateDuckDBQueries());
            repos.AddRange(CreateVistaDBQueries());
            repos.AddRange(CreateHanaQueries());
            repos.AddRange(CreateTerraDataQueries());
            repos.AddRange(CreateVerticaQueries());

            // Other relational databases
            repos.AddRange(CreateOtherRelationalDatabaseQueries());

            return repos;
        }

        /// <summary>
        /// Creates queries for NoSQL databases (MongoDB, Couchbase, Redis, etc.)
        /// </summary>
        private static List<QuerySqlRepo> CreateNoSQLDatabaseQueries()
        {
            var repos = new List<QuerySqlRepo>();

            // MongoDB
            repos.AddRange(CreateMongoDBQueries());

            // Couchbase
            repos.AddRange(CreateCouchbaseQueries());

            // Redis
            repos.AddRange(CreateRedisQueries());

            // Cassandra
            repos.AddRange(CreateCassandraQueries());

            // ElasticSearch
            repos.AddRange(CreateElasticSearchQueries());

            // CouchDB
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.CouchDB, "SELECT * FROM {0}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.CouchDB, "_all_dbs", Sqlcommandtype.getlistoftables)
    });

            // Neo4j
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.Neo4j, "MATCH (n) RETURN n", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.Neo4j, "CALL db.labels()", Sqlcommandtype.getlistoftables)
    });

            // DynamoDB
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.DynamoDB, "Scan {0}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.DynamoDB, "ListTables", Sqlcommandtype.getlistoftables)
    });

            // RavenDB
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.RavenDB, "from {0}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.RavenDB, "Collection Names", Sqlcommandtype.getlistoftables)
    });

            // ArangoDB
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.ArangoDB, "FOR doc IN {0} RETURN doc", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.ArangoDB, "db._collections()", Sqlcommandtype.getlistoftables)
    });

            // OrientDB
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.OrientDB, "SELECT FROM {0}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.OrientDB, "SELECT name FROM (SELECT expand(classes) FROM metadata:schema)", Sqlcommandtype.getlistoftables)
    });

            // Firebase
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.Firebase, "/{0}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.Firebase, "/", Sqlcommandtype.getlistoftables)
    });

            // LiteDB
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.LiteDB, "SELECT $ FROM {0}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.LiteDB, "$.Collections", Sqlcommandtype.getlistoftables)
    });

            return repos;
        }

        /// <summary>
        /// Creates queries for file-based databases and sources
        /// </summary>
        private static List<QuerySqlRepo> CreateFileDatabaseQueries()
        {
            var repos = new List<QuerySqlRepo>();

            // CSV
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.CSV, "SELECT * FROM {0}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.CSV, "Dir *.csv", Sqlcommandtype.getlistoftables)
    });

            // TSV
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.TSV, "SELECT * FROM {0}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.TSV, "Dir *.tsv", Sqlcommandtype.getlistoftables)
    });

            // JSON
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.Json, "SELECT * FROM {0}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.Json, "Dir *.json", Sqlcommandtype.getlistoftables)
    });

            // Excel
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.Xls, "SELECT * FROM [{0}$] {2}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.Xls, "SELECT * FROM [{0}$]", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.Xls, "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES", Sqlcommandtype.getlistoftables)
    });

            // XML
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.XML, "/{0}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.XML, "/*", Sqlcommandtype.getlistoftables)
    });

            // YAML
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.YAML, "/{0}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.YAML, "/*", Sqlcommandtype.getlistoftables)
    });

            // Text
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.Text, "SELECT * FROM {0}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.Text, "Dir *.txt", Sqlcommandtype.getlistoftables)
    });

            // Flat File
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.FlatFile, "SELECT * FROM {0}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.FlatFile, "Dir *.*", Sqlcommandtype.getlistoftables)
    });

            // PDF, DOC, PPT
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.PDF, "Content: {0}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.Doc, "Content: {0}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.Docx, "Content: {0}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.PPT, "Content: {0}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.PPTX, "Content: {0}", Sqlcommandtype.getTable)
    });

            return repos;
        }

        /// <summary>
        /// Creates queries for cloud databases and specialized services
        /// </summary>
        private static List<QuerySqlRepo> CreateCloudDatabaseQueries()
        {
            var repos = new List<QuerySqlRepo>();

            // Azure SQL (similar to SQL Server)
            repos.AddRange(CreateAzureSqlQueries());

            // AzureCloud
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.AzureCloud, "SELECT * FROM {0}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.AzureCloud, "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'", Sqlcommandtype.getlistoftables),
        new QuerySqlRepo(DataSourceType.AzureCloud, "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE OBJECTPROPERTY(OBJECT_ID(CONSTRAINT_SCHEMA + '.' + CONSTRAINT_NAME), 'IsPrimaryKey') = 1 AND TABLE_NAME = '{0}'", Sqlcommandtype.getPKforTable)
    });

            // Snowflake
            repos.AddRange(CreateSnowflakeQueries());

            // BigQuery
            repos.AddRange(CreateBigQueryQueries());

            // AWS databases
            repos.AddRange(CreateAWSQueries());

            // Databricks
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.DataBricks, "SELECT * FROM {0}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.DataBricks, "SHOW TABLES", Sqlcommandtype.getlistoftables)
    });

            // Firebolt
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.Firebolt, "SELECT * FROM {0}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.Firebolt, "SHOW TABLES", Sqlcommandtype.getlistoftables)
    });

            // Hologres
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.Hologres, "SELECT * FROM {0}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.Hologres, "SELECT tablename FROM pg_tables WHERE schemaname = 'public'", Sqlcommandtype.getlistoftables)
    });

            // Supabase (PostgreSQL-based)
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.Supabase, "SELECT * FROM {0}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.Supabase, "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'", Sqlcommandtype.getlistoftables)
    });

            return repos;
        }

        /// <summary>
        /// Creates queries for time series and specialized databases
        /// </summary>
        private static List<QuerySqlRepo> CreateTimeSeriesAndSpecializedDBQueries()
        {
            var repos = new List<QuerySqlRepo>();

            // InfluxDB
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.InfluxDB, "SELECT * FROM {0}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.InfluxDB, "SHOW MEASUREMENTS", Sqlcommandtype.getlistoftables)
    });

            // TimeScale
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.TimeScale, "SELECT * FROM {0}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.TimeScale, "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'", Sqlcommandtype.getlistoftables),
        new QuerySqlRepo(DataSourceType.TimeScale, "SELECT a.attname column_name FROM pg_index i JOIN pg_attribute a ON a.attnum = ANY(i.indkey) WHERE i.indrelid = '{0}'::regclass AND i.indisprimary", Sqlcommandtype.getPKforTable)
    });

            // Cockroach
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.Cockroach, "SELECT * FROM {0}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.Cockroach, "SHOW TABLES", Sqlcommandtype.getlistoftables),
        new QuerySqlRepo(DataSourceType.Cockroach, "SELECT column_name FROM information_schema.columns WHERE table_name = '{0}' AND is_nullable = 'NO'", Sqlcommandtype.getPKforTable)
    });

            // ClickHouse
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.ClickHouse, "SELECT * FROM {0}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.ClickHouse, "SHOW TABLES", Sqlcommandtype.getlistoftables)
    });

            // TigerGraph
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.TigerGraph, "SELECT * FROM {0}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.TigerGraph, "SHOW VERTEX TYPES", Sqlcommandtype.getlistoftables)
    });

            // JanusGraph
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.JanusGraph, "g.V().hasLabel('{0}')", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.JanusGraph, "g.V().label().dedup()", Sqlcommandtype.getlistoftables)
    });

            // Hadoop/HDFS queries
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.Hadoop, "SELECT * FROM {0}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.Hadoop, "SHOW TABLES", Sqlcommandtype.getlistoftables)
    });

            // Kudu
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.Kudu, "SELECT * FROM {0}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.Kudu, "SHOW TABLES", Sqlcommandtype.getlistoftables)
    });

            // Druid
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.Druid, "SELECT * FROM {0}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.Druid, "SHOW TABLES", Sqlcommandtype.getlistoftables)
    });

            // Pinot
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.Pinot, "SELECT * FROM {0}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.Pinot, "SHOW TABLES", Sqlcommandtype.getlistoftables)
    });

            // In-Memory databases
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.RealIM, "SELECT * FROM {0}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.RealIM, "SHOW TABLES", Sqlcommandtype.getlistoftables),

        new QuerySqlRepo(DataSourceType.Petastorm, "from petastorm import make_reader", Sqlcommandtype.getTable),

        new QuerySqlRepo(DataSourceType.RocketSet, "SELECT * FROM {0}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.RocketSet, "LIST COLLECTIONS", Sqlcommandtype.getlistoftables)
    });

            // Machine Learning data formats
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.TFRecord, "ReadTFRecord: {0}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.ONNX, "ReadONNX: {0}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.PyTorchData, "ReadPyTorch: {0}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.ScikitLearnData, "ReadScikit: {0}", Sqlcommandtype.getTable)
    });

            return repos;
        }

        /// <summary>
        /// Creates queries for streaming and messaging platforms
        /// </summary>
        private static List<QuerySqlRepo> CreateStreamingPlatformQueries()
        {
            var repos = new List<QuerySqlRepo>();

            // Kafka
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.Kafka, "CONSUME {0}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.Kafka, "LIST TOPICS", Sqlcommandtype.getlistoftables)
    });

            // RabbitMQ
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.RabbitMQ, "GET MESSAGES {0}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.RabbitMQ, "LIST QUEUES", Sqlcommandtype.getlistoftables)
    });

            // ActiveMQ
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.ActiveMQ, "RECEIVE FROM {0}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.ActiveMQ, "SHOW QUEUES", Sqlcommandtype.getlistoftables)
    });

            // Pulsar
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.Pulsar, "CONSUME FROM {0}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.Pulsar, "LIST TOPICS", Sqlcommandtype.getlistoftables)
    });

            // MassTransit
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.MassTransit, "RECEIVE FROM {0}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.MassTransit, "LIST ENDPOINTS", Sqlcommandtype.getlistoftables)
    });

            // NATS
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.Nats, "SUBSCRIBE {0}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.Nats, "SHOW SUBJECTS", Sqlcommandtype.getlistoftables)
    });

            // ZeroMQ
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.ZeroMQ, "RECEIVE FROM {0}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.ZeroMQ, "LIST SOCKETS", Sqlcommandtype.getlistoftables)
    });

            // AWS Messaging services
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.AWSKinesis, "GET RECORDS FROM {0}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.AWSKinesis, "LIST STREAMS", Sqlcommandtype.getlistoftables),

        new QuerySqlRepo(DataSourceType.AWSSQS, "RECEIVE MESSAGES FROM {0}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.AWSSQS, "LIST QUEUES", Sqlcommandtype.getlistoftables),

        new QuerySqlRepo(DataSourceType.AWSSNS, "LIST MESSAGES FROM {0}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.AWSSNS, "LIST TOPICS", Sqlcommandtype.getlistoftables)
    });

            // Azure Service Bus
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.AzureServiceBus, "RECEIVE FROM {0}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.AzureServiceBus, "LIST QUEUES", Sqlcommandtype.getlistoftables)
    });

            // OPC UA
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.OPC, "READ NODE {0}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.OPC, "BROWSE", Sqlcommandtype.getlistoftables)
    });

            return repos;
        }

        #region SQL Server Query Methods

        /// <summary>
        /// Creates SQL queries for SQL Server database operations
        /// </summary>
        private static List<QuerySqlRepo> CreateSqlServerQueries()
        {
            return new List<QuerySqlRepo>
    {
        // Schema queries
        new QuerySqlRepo(DataSourceType.SqlServer,
            "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{1}' AND TABLE_TYPE = 'BASE TABLE'",
            Sqlcommandtype.getlistoftablesfromotherschema),
        
        // Table queries
        new QuerySqlRepo(DataSourceType.SqlServer,
            "SELECT * FROM {0} {2}",
            Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.SqlServer,
            "select table_name from Information_schema.Tables where Table_type='BASE TABLE'",
            Sqlcommandtype.getlistoftables),
        
        // Primary key queries
        new QuerySqlRepo(DataSourceType.SqlServer,
            "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE TABLE_NAME = '{0}' AND CONSTRAINT_NAME LIKE 'PK%'",
            Sqlcommandtype.getPKforTable),
        
        // Foreign key queries
        new QuerySqlRepo(DataSourceType.SqlServer,
            "SELECT FK.COLUMN_NAME, FK.TABLE_NAME FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE FK INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS TC ON FK.CONSTRAINT_NAME = TC.CONSTRAINT_NAME WHERE TC.CONSTRAINT_TYPE = 'FOREIGN KEY' AND FK.TABLE_NAME = '{0}'",
            Sqlcommandtype.getFKforTable),
        
        // Related table queries
        new QuerySqlRepo(DataSourceType.SqlServer,
            "SELECT FK.TABLE_NAME FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS RC INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE FK ON RC.CONSTRAINT_NAME = FK.CONSTRAINT_NAME WHERE RC.UNIQUE_CONSTRAINT_NAME = (SELECT CONSTRAINT_NAME FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE TABLE_NAME = '{0}' AND CONSTRAINT_TYPE = 'PRIMARY KEY')",
            Sqlcommandtype.getChildTable),
        new QuerySqlRepo(DataSourceType.SqlServer,
            "SELECT RC.UNIQUE_CONSTRAINT_TABLE_NAME FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS RC WHERE RC.CONSTRAINT_NAME IN (SELECT CONSTRAINT_NAME FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE TABLE_NAME = '{0}')",
            Sqlcommandtype.getParentTable),
        
        // Table existence check
        new QuerySqlRepo(DataSourceType.SqlServer,
            "SELECT CASE WHEN EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{0}') THEN 1 ELSE 0 END",
            Sqlcommandtype.CheckTableExist)
    };
        }

        // Also create queries for AzureSQL which shares same syntax
        private static List<QuerySqlRepo> CreateAzureSqlQueries()
        {
            var queries = CreateSqlServerQueries();

            // Clone the SQL Server queries but change the data source type
            return queries.Select(q => new QuerySqlRepo(DataSourceType.AzureSQL, q.Sql, q.Sqltype)).ToList();
        }

        #endregion

        #region MySQL Query Methods

        /// <summary>
        /// Creates SQL queries for MySQL database operations
        /// </summary>
        private static List<QuerySqlRepo> CreateMySqlQueries()
        {
            return new List<QuerySqlRepo>
    {
        // Schema queries
        new QuerySqlRepo(DataSourceType.Mysql,
            "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{1}'",
            Sqlcommandtype.getlistoftablesfromotherschema),
        
        // Table queries
        new QuerySqlRepo(DataSourceType.Mysql,
            "SELECT * FROM {0} {2}",
            Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.Mysql,
            "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{1}'",
            Sqlcommandtype.getlistoftables),
        
        // Primary key queries
        new QuerySqlRepo(DataSourceType.Mysql,
            "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE TABLE_NAME = '{0}' AND TABLE_SCHEMA = '{1}' AND CONSTRAINT_NAME LIKE 'PRIMARY'",
            Sqlcommandtype.getPKforTable),
        
        // Foreign key queries
        new QuerySqlRepo(DataSourceType.Mysql,
            "SELECT COLUMN_NAME AS child_column, REFERENCED_COLUMN_NAME AS parent_column, REFERENCED_TABLE_NAME AS parent_table FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE TABLE_SCHEMA = '{1}' AND TABLE_NAME = '{0}' AND REFERENCED_TABLE_NAME IS NOT NULL",
            Sqlcommandtype.getFKforTable),
        
        // Related table queries
        new QuerySqlRepo(DataSourceType.Mysql,
            "SELECT TABLE_NAME AS child_table FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE TABLE_SCHEMA = '{1}' AND REFERENCED_TABLE_NAME = '{0}'",
            Sqlcommandtype.getChildTable),
        new QuerySqlRepo(DataSourceType.Mysql,
            "SELECT REFERENCED_TABLE_NAME AS parent_table FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE TABLE_SCHEMA = '{1}' AND TABLE_NAME = '{0}' AND REFERENCED_TABLE_NAME IS NOT NULL",
            Sqlcommandtype.getParentTable),
        
        // Table existence check
        new QuerySqlRepo(DataSourceType.Mysql,
            "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{1}' AND TABLE_NAME = '{0}'",
            Sqlcommandtype.CheckTableExist)
    };
        }

        #endregion

        #region Oracle Query Methods

        /// <summary>
        /// Creates SQL queries for Oracle database operations
        /// </summary>
        private static List<QuerySqlRepo> CreateOracleQueries()
        {
            return new List<QuerySqlRepo>
    {
        // Schema queries
        new QuerySqlRepo(DataSourceType.Oracle,
            "SELECT TABLE_NAME FROM all_tables WHERE OWNER = '{1}'",
            Sqlcommandtype.getlistoftablesfromotherschema),
        
        // Table queries
        new QuerySqlRepo(DataSourceType.Oracle,
            "SELECT * FROM {0} {2}",
            Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.Oracle,
            "SELECT TABLE_NAME FROM user_tables",
            Sqlcommandtype.getlistoftables),
        
        // Primary key queries
        new QuerySqlRepo(DataSourceType.Oracle,
            "SELECT cols.column_name FROM all_constraints cons, all_cons_columns cols WHERE cols.table_name = '{0}' AND cons.constraint_type = 'P' AND cons.constraint_name = cols.constraint_name AND cons.owner = cols.owner",
            Sqlcommandtype.getPKforTable),
        
        // Foreign key queries
        new QuerySqlRepo(DataSourceType.Oracle,
            "SELECT a.constraint_name, a.column_name, a.table_name FROM all_cons_columns a JOIN all_constraints c ON a.constraint_name = c.constraint_name WHERE c.constraint_type = 'R' AND a.table_name = '{0}'",
            Sqlcommandtype.getFKforTable),
        
        // Related table queries
        new QuerySqlRepo(DataSourceType.Oracle,
            "SELECT table_name FROM all_constraints WHERE r_constraint_name IN (SELECT constraint_name FROM all_constraints WHERE table_name = '{0}' AND constraint_type = 'P')",
            Sqlcommandtype.getChildTable),
        new QuerySqlRepo(DataSourceType.Oracle,
            "SELECT r.table_name FROM all_constraints c JOIN all_constraints r ON c.r_constraint_name = r.constraint_name WHERE c.table_name = '{0}' AND c.constraint_type = 'R'",
            Sqlcommandtype.getParentTable),
        
        // Table existence check
        new QuerySqlRepo(DataSourceType.Oracle,
            "SELECT COUNT(*) FROM all_tables WHERE table_name = '{0}'",
            Sqlcommandtype.CheckTableExist)
    };
        }

        #endregion

        // Other database-specific methods follow the same pattern...
        // Include all remaining methods from the refactored version (PostgreSQL, SQLite, DB2, Firebird, etc.)

        #region Additional Methods for Database Types Previously Missing

        /// <summary>
        /// Creates SQL queries for DuckDB database operations
        /// </summary>
        private static List<QuerySqlRepo> CreateDuckDBQueries()
        {
            return new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.DuckDB,
            "SELECT * FROM {0} {2}",
            Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.DuckDB,
            "SELECT name FROM sqlite_master WHERE type='table'",
            Sqlcommandtype.getlistoftables),
        new QuerySqlRepo(DataSourceType.DuckDB,
            "SELECT column_name FROM pragma_table_info('{0}') WHERE pk != 0;",
            Sqlcommandtype.getPKforTable)
    };
        }

        /// <summary>
        /// Creates queries for VistaDB
        /// </summary>
        private static List<QuerySqlRepo> CreateVistaDBQueries()
        {
            return new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.VistaDB,
            "SELECT * FROM {0}",
            Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.VistaDB,
            "SELECT TableName FROM $TABLES",
            Sqlcommandtype.getlistoftables),
        new QuerySqlRepo(DataSourceType.VistaDB,
            "SELECT ColumnName FROM $COLUMNS WHERE TableName = '{0}' AND IsPrimaryKey = 1",
            Sqlcommandtype.getPKforTable)
    };
        }

        /// <summary>
        /// Creates SQL queries for Hana database operations
        /// </summary>
        private static List<QuerySqlRepo> CreateHanaQueries()
        {
            return new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.Hana,
            "SELECT TABLE_NAME FROM TABLES WHERE SCHEMA_NAME = '{1}'",
            Sqlcommandtype.getlistoftablesfromotherschema),
        new QuerySqlRepo(DataSourceType.Hana,
            "SELECT * FROM {0}",
            Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.Hana,
            "SELECT TABLE_NAME FROM TABLES WHERE SCHEMA_NAME = CURRENT_SCHEMA",
            Sqlcommandtype.getlistoftables),
        new QuerySqlRepo(DataSourceType.Hana,
            "SELECT COLUMN_NAME FROM CONSTRAINTS WHERE TABLE_NAME = '{0}' AND SCHEMA_NAME = CURRENT_SCHEMA AND IS_PRIMARY_KEY = 'TRUE'",
            Sqlcommandtype.getPKforTable)
    };
        }

        /// <summary>
        /// Creates SQL queries for TerraData database operations
        /// </summary>
        private static List<QuerySqlRepo> CreateTerraDataQueries()
        {
            return new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.TerraData,
            "SELECT TableName FROM DBC.TablesV WHERE TableKind = 'T' AND DatabaseName = '{1}'",
            Sqlcommandtype.getlistoftablesfromotherschema),
        new QuerySqlRepo(DataSourceType.TerraData,
            "SELECT * FROM {0}",
            Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.TerraData,
            "SELECT TableName FROM DBC.TablesV WHERE TableKind = 'T' AND DatabaseName = CURRENT_DATABASE",
            Sqlcommandtype.getlistoftables),
        new QuerySqlRepo(DataSourceType.TerraData,
            "SELECT ColumnName FROM DBC.IndicesV WHERE TableName = '{0}' AND DatabaseName = CURRENT_DATABASE AND IndexType = 'P'",
            Sqlcommandtype.getPKforTable)
    };
        }

        /// <summary>
        /// Creates SQL queries for Vertica database operations
        /// </summary>
        private static List<QuerySqlRepo> CreateVerticaQueries()
        {
            return new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.Vertica,
            "SELECT table_name FROM v_catalog.tables WHERE table_schema = '{1}'",
            Sqlcommandtype.getlistoftablesfromotherschema),
        new QuerySqlRepo(DataSourceType.Vertica,
            "SELECT * FROM {0}",
            Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.Vertica,
            "SELECT table_name FROM v_catalog.tables WHERE table_schema = CURRENT_SCHEMA",
            Sqlcommandtype.getlistoftables),
        new QuerySqlRepo(DataSourceType.Vertica,
            "SELECT column_name FROM v_catalog.primary_keys WHERE table_name = '{0}' AND table_schema = CURRENT_SCHEMA",
            Sqlcommandtype.getPKforTable)
    };
        }

        // Include REST API and other specialized data source types
        private static List<QuerySqlRepo> CreateRestApiQueries()
        {
            return new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.RestApi,
            "GET /{0}",
            Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.RestApi,
            "GET /",
            Sqlcommandtype.getlistoftables)
    };
        }

        #endregion
        /// <summary>
        /// Creates PostgreSQL-specific SQL queries
        /// </summary>
        private static List<QuerySqlRepo> CreatePostgreSqlQueries()
        {
            return new List<QuerySqlRepo>
    {
        // Schema queries
        new QuerySqlRepo(DataSourceType.Postgre,
            "SELECT table_name FROM information_schema.tables WHERE table_schema = '{1}' AND table_type = 'BASE TABLE'",
            Sqlcommandtype.getlistoftablesfromotherschema),
        
        // Table queries
        new QuerySqlRepo(DataSourceType.Postgre,
            "SELECT * FROM {0} {2}",
            Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.Postgre,
            "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'",
            Sqlcommandtype.getlistoftables),
        
        // Primary key queries
        new QuerySqlRepo(DataSourceType.Postgre,
            "SELECT a.attname FROM pg_index i JOIN pg_attribute a ON a.attnum = ANY(i.indkey) WHERE i.indrelid = '{0}'::regclass AND i.indisprimary",
            Sqlcommandtype.getPKforTable),
        
        // Foreign key queries
        new QuerySqlRepo(DataSourceType.Postgre,
            "SELECT conname AS constraint_name, a.attname AS child_column, af.attname AS parent_column, cl.relname AS parent_table FROM pg_attribute a JOIN pg_attribute af ON a.attnum = ANY(pg_constraint.confkey) JOIN pg_class cl ON pg_constraint.confrelid = cl.oid JOIN pg_constraint ON a.attnum = ANY(pg_constraint.conkey) WHERE a.attnum > 0 AND pg_constraint.conrelid = '{0}'::regclass",
            Sqlcommandtype.getFKforTable),
        
        // Related table queries
        new QuerySqlRepo(DataSourceType.Postgre,
            "SELECT conname AS constraint_name, cl.relname AS child_table FROM pg_constraint JOIN pg_class cl ON pg_constraint.conrelid = cl.oid WHERE confrelid = '{0}'::regclass",
            Sqlcommandtype.getChildTable),
        new QuerySqlRepo(DataSourceType.Postgre,
            "SELECT confrelid::regclass AS parent_table FROM pg_constraint WHERE conrelid = '{0}'::regclass",
            Sqlcommandtype.getParentTable),
        
        // Table existence check
        new QuerySqlRepo(DataSourceType.Postgre,
            "SELECT EXISTS(SELECT 1 FROM information_schema.tables WHERE table_name = '{0}' AND table_schema = '{1}')",
            Sqlcommandtype.CheckTableExist)
    };
        }

        /// <summary>
        /// Creates SQLite-specific SQL queries
        /// </summary>
        private static List<QuerySqlRepo> CreateSqliteQueries()
        {
            return new List<QuerySqlRepo>
    {
        // Schema queries (SQLite doesn't have true schemas)
        new QuerySqlRepo(DataSourceType.SqlLite,
            "SELECT name AS table_name FROM sqlite_master WHERE type='table' AND sql LIKE '%{1}%'",
            Sqlcommandtype.getlistoftablesfromotherschema),
        
        // Table queries
        new QuerySqlRepo(DataSourceType.SqlLite,
            "SELECT * FROM {0} {2}",
            Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.SqlLite,
            "SELECT name AS table_name FROM sqlite_master WHERE type='table'",
            Sqlcommandtype.getlistoftables),
        
        // Primary key queries
        new QuerySqlRepo(DataSourceType.SqlLite,
            "PRAGMA table_info({0})",
            Sqlcommandtype.getPKforTable),
        
        // Foreign key queries
        new QuerySqlRepo(DataSourceType.SqlLite,
            "PRAGMA foreign_key_list({0})",
            Sqlcommandtype.getFKforTable),
        
        // Table existence check
        new QuerySqlRepo(DataSourceType.SqlLite,
            "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='{0}'",
            Sqlcommandtype.CheckTableExist)
    };
        }

        /// <summary>
        /// Creates DB2-specific SQL queries
        /// </summary>
        private static List<QuerySqlRepo> CreateDB2Queries()
        {
            return new List<QuerySqlRepo>
    {
        // Schema queries
        new QuerySqlRepo(DataSourceType.DB2,
            "SELECT TABNAME AS TABLE_NAME FROM SYSCAT.TABLES WHERE TABSCHEMA = '{1}'",
            Sqlcommandtype.getlistoftablesfromotherschema),
        
        // Table queries
        new QuerySqlRepo(DataSourceType.DB2,
            "SELECT * FROM {0} {2}",
            Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.DB2,
            "SELECT TABNAME FROM SYSCAT.TABLES WHERE TABSCHEMA = CURRENT SCHEMA",
            Sqlcommandtype.getlistoftables),
        
        // Primary key queries
        new QuerySqlRepo(DataSourceType.DB2,
            "SELECT COLNAME COLUMN_NAME FROM SYSCAT.KEYCOLUSE WHERE TABNAME = '{0}' AND CONSTRAINTNAME LIKE 'PK%'",
            Sqlcommandtype.getPKforTable),
        
        // Foreign key queries
        new QuerySqlRepo(DataSourceType.DB2,
            "SELECT FK_COLNAMES AS child_column, PK_COLNAMES AS parent_column, PK_TBNAME AS parent_table FROM SYSIBM.SQLFOREIGNKEYS WHERE FK_TBNAME = '{0}'",
            Sqlcommandtype.getFKforTable),
        
        // Related table queries
        new QuerySqlRepo(DataSourceType.DB2,
            "SELECT FK_TBNAME AS child_table FROM SYSIBM.SQLFOREIGNKEYS WHERE PK_TBNAME = '{0}'",
            Sqlcommandtype.getChildTable),
        new QuerySqlRepo(DataSourceType.DB2,
            "SELECT PK_TBNAME AS parent_table FROM SYSIBM.SQLFOREIGNKEYS WHERE FK_TBNAME = '{0}'",
            Sqlcommandtype.getParentTable),
        
        // Table existence check
        new QuerySqlRepo(DataSourceType.DB2,
            "SELECT COUNT(*) FROM SYSCAT.TABLES WHERE TABNAME = '{0}'",
            Sqlcommandtype.CheckTableExist)
    };
        }

        /// <summary>
        /// Creates Firebird-specific SQL queries
        /// </summary>
        private static List<QuerySqlRepo> CreateFirebirdQueries()
        {
            return new List<QuerySqlRepo>
    {
        // Schema queries
        new QuerySqlRepo(DataSourceType.FireBird,
            "SELECT RDB$RELATION_NAME FROM RDB$RELATIONS WHERE RDB$VIEW_BLR IS NULL AND RDB$RELATION_NAME NOT LIKE 'RDB$%' AND RDB$RELATION_TYPE = 0 AND RDB$SYSTEM_FLAG = 0 AND RDB$RELATION_NAME IN (SELECT RDB$RELATION_NAME FROM RDB$RELATION_FIELDS WHERE RDB$FIELD_SOURCE IN (SELECT RDB$FIELD_NAME FROM RDB$FIELDS WHERE RDB$FIELD_NAME LIKE '%{1}%'))",
            Sqlcommandtype.getlistoftablesfromotherschema),
        
        // Table queries
        new QuerySqlRepo(DataSourceType.FireBird,
            "SELECT * FROM {0}",
            Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.FireBird,
            "SELECT RDB$RELATION_NAME FROM RDB$RELATIONS WHERE RDB$SYSTEM_FLAG = 0",
            Sqlcommandtype.getlistoftables),
        
        // Primary key queries
        new QuerySqlRepo(DataSourceType.FireBird,
            "SELECT RDB$INDEX_SEGMENTS.RDB$FIELD_NAME FROM RDB$INDEX_SEGMENTS JOIN RDB$RELATION_CONSTRAINTS ON RDB$INDEX_SEGMENTS.RDB$INDEX_NAME = RDB$RELATION_CONSTRAINTS.RDB$INDEX_NAME WHERE RDB$RELATION_CONSTRAINTS.RDB$RELATION_NAME = '{0}' AND RDB$RELATION_CONSTRAINTS.RDB$CONSTRAINT_TYPE = 'PRIMARY KEY'",
            Sqlcommandtype.getPKforTable),
        
        // Table existence check
        new QuerySqlRepo(DataSourceType.FireBird,
            "SELECT COUNT(*) FROM RDB$RELATIONS WHERE RDB$RELATION_NAME = '{0}' AND RDB$SYSTEM_FLAG = 0",
            Sqlcommandtype.CheckTableExist)
    };
        }

        /// <summary>
        /// Creates queries for other relational databases not covered by specific methods
        /// </summary>
        private static List<QuerySqlRepo> CreateOtherRelationalDatabaseQueries()
        {
            var repos = new List<QuerySqlRepo>();

            // Add queries for Cockroach DB
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.Cockroach,
            "SELECT table_name FROM information_schema.tables WHERE table_schema = '{1}'",
            Sqlcommandtype.getlistoftablesfromotherschema),
        new QuerySqlRepo(DataSourceType.Cockroach,
            "SELECT * FROM {0}",
            Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.Cockroach,
            "SHOW TABLES",
            Sqlcommandtype.getlistoftables),
        new QuerySqlRepo(DataSourceType.Cockroach,
            "SELECT column_name FROM information_schema.columns WHERE table_name = '{0}' AND is_nullable = 'NO'",
            Sqlcommandtype.getPKforTable)
    });

            // Add queries for Spanner
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.Spanner,
            "SELECT table_name FROM information_schema.tables WHERE table_schema = '{1}'",
            Sqlcommandtype.getlistoftablesfromotherschema),
        new QuerySqlRepo(DataSourceType.Spanner,
            "SELECT * FROM {0}",
            Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.Spanner,
            "SELECT table_name FROM information_schema.tables WHERE table_catalog = ''",
            Sqlcommandtype.getlistoftables),
        new QuerySqlRepo(DataSourceType.Spanner,
            "SELECT column_name FROM information_schema.key_column_usage WHERE table_name = '{0}' AND constraint_name LIKE 'PRIMARY_KEY%'",
            Sqlcommandtype.getPKforTable)
    });

            return repos;
        }

        /// <summary>
        /// Creates MongoDB-specific queries
        /// </summary>
        private static List<QuerySqlRepo> CreateMongoDBQueries()
        {
            return new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.MongoDB,
            "db.{0}.find({})",
            Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.MongoDB,
            "db.getCollectionNames()",
            Sqlcommandtype.getlistoftables)
        // MongoDB does not have traditional PK or FK concepts
    };
        }

        /// <summary>
        /// Creates Couchbase-specific queries
        /// </summary>
        private static List<QuerySqlRepo> CreateCouchbaseQueries()
        {
            return new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.Couchbase,
            "SELECT * FROM `{0}`",
            Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.Couchbase,
            "SELECT name FROM system:keyspaces",
            Sqlcommandtype.getlistoftables)
        // Couchbase doesn't use traditional PKs or FKs
    };
        }

        /// <summary>
        /// Creates Redis-specific queries
        /// </summary>
        private static List<QuerySqlRepo> CreateRedisQueries()
        {
            return new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.Redis,
            "GET {0}",
            Sqlcommandtype.getTable)
        // Redis doesn't have concept of listoftables
    };
        }

        /// <summary>
        /// Creates Cassandra-specific queries
        /// </summary>
        private static List<QuerySqlRepo> CreateCassandraQueries()
        {
            return new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.Cassandra,
            "SELECT * FROM {0}",
            Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.Cassandra,
            "SELECT table_name FROM system_schema.tables WHERE keyspace_name = 'YourKeyspaceName'",
            Sqlcommandtype.getlistoftables),
        new QuerySqlRepo(DataSourceType.Cassandra,
            "SELECT column_name FROM system_schema.columns WHERE table_name = '{0}' AND keyspace_name = 'YourKeyspaceName' AND kind = 'partition_key'",
            Sqlcommandtype.getPKforTable)
    };
        }

        /// <summary>
        /// Creates ElasticSearch-specific queries
        /// </summary>
        private static List<QuerySqlRepo> CreateElasticSearchQueries()
        {
            return new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.ElasticSearch,
            "SELECT * FROM {0}",
            Sqlcommandtype.getTable)
        // ElasticSearch doesn't have traditional tables concept
    };
        }

        /// <summary>
        /// Creates Snowflake-specific queries
        /// </summary>
        private static List<QuerySqlRepo> CreateSnowflakeQueries()
        {
            return new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.SnowFlake,
            "SHOW TABLES IN SCHEMA {1}",
            Sqlcommandtype.getlistoftablesfromotherschema),
        new QuerySqlRepo(DataSourceType.SnowFlake,
            "SELECT * FROM {0} {2}",
            Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.SnowFlake,
            "SELECT TABLE_NAME FROM {1}.INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'",
            Sqlcommandtype.getlistoftables),
        new QuerySqlRepo(DataSourceType.SnowFlake,
            "SELECT kcu.COLUMN_NAME FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu ON tc.TABLE_NAME = kcu.TABLE_NAME AND tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME WHERE tc.TABLE_SCHEMA = '{1}' AND tc.TABLE_NAME = '{0}' AND tc.CONSTRAINT_TYPE = 'PRIMARY KEY'",
            Sqlcommandtype.getPKforTable)
    };
        }

        /// <summary>
        /// Creates Google BigQuery-specific queries
        /// </summary>
        private static List<QuerySqlRepo> CreateBigQueryQueries()
        {
            return new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.GoogleBigQuery,
            "SELECT table_name FROM `{1}.INFORMATION_SCHEMA.TABLES`",
            Sqlcommandtype.getlistoftablesfromotherschema),
        new QuerySqlRepo(DataSourceType.GoogleBigQuery,
            "SELECT * FROM {0}",
            Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.GoogleBigQuery,
            "SELECT table_name FROM `YOUR_DATASET.INFORMATION_SCHEMA.TABLES`",
            Sqlcommandtype.getlistoftables)
        // BigQuery doesn't have a traditional concept of primary keys
    };
        }

        /// <summary>
        /// Creates AWS-specific database queries
        /// </summary>
        private static List<QuerySqlRepo> CreateAWSQueries()
        {
            var repos = new List<QuerySqlRepo>();

            // Add Redshift queries
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.AWSRedshift,
            "SELECT DISTINCT schemaname FROM pg_tables",
            Sqlcommandtype.getlistoftablesfromotherschema),
        new QuerySqlRepo(DataSourceType.AWSRedshift,
            "SELECT * FROM {0} {2}",
            Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.AWSRedshift,
            "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'",
            Sqlcommandtype.getlistoftables),
        new QuerySqlRepo(DataSourceType.AWSRedshift,
            "SELECT kcu.column_name FROM information_schema.table_constraints tc JOIN information_schema.key_column_usage kcu ON tc.constraint_name = kcu.constraint_name WHERE tc.constraint_type = 'PRIMARY KEY' AND tc.table_name = '{0}'",
            Sqlcommandtype.getPKforTable)
    });

            // Add Athena queries
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.AWSAthena,
            "SHOW DATABASES",
            Sqlcommandtype.getlistoftablesfromotherschema),
        new QuerySqlRepo(DataSourceType.AWSAthena,
            "SELECT * FROM {0} {2}",
            Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.AWSAthena,
            "SHOW TABLES",
            Sqlcommandtype.getlistoftables)
        // Athena doesn't have traditional keys
    });

            // Add RDS queries (based on underlying database engine)
            repos.AddRange(new List<QuerySqlRepo>
    {
        new QuerySqlRepo(DataSourceType.AWSRDS,
            "SELECT table_name FROM information_schema.tables WHERE table_schema = '{1}'",
            Sqlcommandtype.getlistoftablesfromotherschema),
        new QuerySqlRepo(DataSourceType.AWSRDS,
            "SELECT * FROM {0} {2}",
            Sqlcommandtype.getTable)
        // Other queries depend on specific RDS engine
    });

            return repos;
        }

        #endregion "Meta Data Queries"



    }
    public enum DatabaseFeature
    {
        WindowFunctions,
        Json,
        Xml,
        TemporalTables,
        FullTextSearch,
        Partitioning,
        ColumnStore
    }
    public enum TransactionOperation
    {
        Begin,
        Commit,
        Rollback
    }
}
