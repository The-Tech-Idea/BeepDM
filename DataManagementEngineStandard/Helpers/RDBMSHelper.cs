﻿
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using System.Linq;


namespace TheTechIdea.Beep.Helpers
{
	/// <summary>
	/// Helper class for interacting with a Relational Database Management System (RDBMS).
	/// </summary>
	public static class RDBMSHelper
	{
		/// <summary>Gets the query for fetching the schemas or databased user has privilge.</summary>
		/// <param name="rdbms">The type of RDBMS.</param>
		/// <param name="userName">The name of the user.</param>
		/// <remarks> 
		/// </remarks>
		public static string GetSchemasorDatabases(DataSourceType rdbms, string userName)
		{
			string query = string.Empty;
			userName = userName.Replace("'", "''"); // Protect against SQL injection

			switch (rdbms)
			{
				case DataSourceType.SqlServer:
					query = $"SELECT name FROM sys.databases WHERE HAS_DBACCESS(name) = 1";
					break;
				case DataSourceType.Mysql:
					query = $"SELECT SCHEMA_NAME AS 'Database' FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME IN (SELECT DISTINCT TABLE_SCHEMA FROM INFORMATION_SCHEMA.SCHEMA_PRIVILEGES WHERE GRANTEE LIKE '%{userName}%')";
					break;
				case DataSourceType.Postgre:
					query = $"SELECT datname FROM pg_database WHERE datistemplate = false AND has_database_privilege('{userName}', datname, 'CONNECT')";
					break;
				case DataSourceType.Oracle:
					query = $"SELECT DISTINCT OWNER FROM DBA_TAB_PRIVS WHERE GRANTEE = '{userName}'";
					break;
				case DataSourceType.DB2:
					query = $"SELECT DISTINCT SCHEMANAME FROM SYSCAT.SCHEMAAUTH WHERE GRANTEE = '{userName}'";
					break;
				case DataSourceType.FireBird:
					// Firebird does not provide an easy way to list all databases a user can access; this would normally be managed by the application or DBA
					query = string.Empty;
					break;
				case DataSourceType.SqlLite:
					// SQLite does not support multiple databases in the same connection; typically there's only one database
					query = string.Empty;
					break;
				case DataSourceType.Couchbase:
				case DataSourceType.Redis:
				case DataSourceType.MongoDB:
				case DataSourceType.ElasticSearch:
				case DataSourceType.Cassandra:
				case DataSourceType.Neo4j:
				case DataSourceType.ArangoDB:
				case DataSourceType.InfluxDB:
				case DataSourceType.ClickHouse:
				case DataSourceType.Kudu:
				case DataSourceType.Druid:
				case DataSourceType.Pinot:
				case DataSourceType.DynamoDB:
					// NoSQL databases and systems like Elasticsearch do not use SQL for schema listing and may not have 'schemas' in the traditional sense
					query = string.Empty;
					break;
				default:
					query = string.Empty;
					break;
			}

			return query;
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
		/// <summary>Generates a query to drop a primary key constraint from a table.</summary>
		/// <param name="rdbms">The type of the database management system.</param>
		/// <param name="tableName">The name of the table.</param>
		/// <param name="constraintName">The name of the primary key constraint.</param>
		/// <returns>A query to drop the primary key constraint.</returns>
		/// <remarks>
		/// For SQL Server and PostgreSQL, the query will be in the form "ALTER TABLE [tableName] DROP CONSTRAINT [constraintName]".
		/// For MySQL, Oracle, and DB2, the query will be in the form "ALTER TABLE [tableName] DROP PRIMARY KEY".
		/// For Firebird, the
		public static string GenerateDropPrimaryKeyQuery(DataSourceType rdbms, string tableName, string constraintName)
		{
			string query = "";

			switch (rdbms)
			{
				case DataSourceType.SqlServer:
					query = $"ALTER TABLE {tableName} DROP CONSTRAINT {constraintName}";
					break;
				case DataSourceType.Mysql:
					query = $"ALTER TABLE {tableName} DROP PRIMARY KEY";
					break;
				case DataSourceType.Postgre:
					query = $"ALTER TABLE {tableName} DROP CONSTRAINT {constraintName}";
					break;
				case DataSourceType.Oracle:
					query = $"ALTER TABLE {tableName} DROP PRIMARY KEY";
					break;
				case DataSourceType.DB2:
					query = $"ALTER TABLE {tableName} DROP PRIMARY KEY";
					break;
				case DataSourceType.FireBird:
					query = $"ALTER TABLE {tableName} DROP CONSTRAINT {constraintName}";
					break;
				case DataSourceType.SqlLite:
					query = "SQLite requires recreating the table to drop primary key.";
					break;
				case DataSourceType.Couchbase:
				case DataSourceType.Redis:
				case DataSourceType.MongoDB:
					query = "NoSQL databases typically do not have primary key constraints in the same way RDBMS do.";
					break;
				default:
					query = "RDBMS not supported.";
					break;
			}

			return query;
		}
		/// <summary>Generates a SQL query to drop a foreign key constraint in a specified RDBMS.</summary>
		/// <param name="rdbms">The type of RDBMS.</param>
		/// <param name="tableName">The name of the table.</param>
		/// <param name="constraintName">The name of the foreign key constraint.</param>
		/// <returns>A SQL query to drop the specified foreign key constraint.</returns>
		/// <remarks>
		/// The generated query varies depending on the RDBMS:
		/// - For SQL Server, the query is: ALTER TABLE {tableName} DROP CONSTRAINT {constraintName}
		/// - For MySQL, the query is: ALTER TABLE {tableName} DROP FOREIGN KEY {constraintName}
		///
		public static string GenerateDropForeignKeyQuery(DataSourceType rdbms, string tableName, string constraintName)
		{
			string query = "";

			switch (rdbms)
			{
				case DataSourceType.SqlServer:
					query = $"ALTER TABLE {tableName} DROP CONSTRAINT {constraintName}";
					break;
				case DataSourceType.Mysql:
					query = $"ALTER TABLE {tableName} DROP FOREIGN KEY {constraintName}";
					break;
				case DataSourceType.Postgre:
					query = $"ALTER TABLE {tableName} DROP CONSTRAINT {constraintName}";
					break;
				case DataSourceType.Oracle:
					query = $"ALTER TABLE {tableName} DROP CONSTRAINT {constraintName}";
					break;
				case DataSourceType.DB2:
					query = $"ALTER TABLE {tableName} DROP FOREIGN KEY {constraintName}";
					break;
				case DataSourceType.FireBird:
					query = $"ALTER TABLE {tableName} DROP CONSTRAINT {constraintName}";
					break;
				case DataSourceType.SqlLite:
					query = "SQLite requires recreating the table to drop a foreign key.";
					break;
				case DataSourceType.Couchbase:
				case DataSourceType.Redis:
				case DataSourceType.MongoDB:
					query = "NoSQL databases typically do not have foreign key constraints in the same way RDBMS do.";
					break;
				default:
					query = "RDBMS not supported.";
					break;
			}

			return query;
		}
		/// <summary>Generates a query to disable a foreign key constraint in a specific RDBMS.</summary>
		/// <param name="rdbms">The type of RDBMS.</param>
		/// <param name="tableName">The name of the table.</param>
		/// <param name="constraintName">The name of the foreign key constraint.</param>
		/// <returns>A query to disable the specified foreign key constraint.</returns>
		/// <remarks>
		/// The generated query depends on the type of RDBMS specified. The following RDBMS are supported:
		/// - SqlServer: ALTER TABLE {tableName} NOCHECK CONSTRAINT {constraintName}
		/// - Oracle: ALTER TABLE {tableName} DISABLE CONSTRAINT {constraintName}
		/// - Post
		public static string GenerateDisableForeignKeyQuery(DataSourceType rdbms, string tableName, string constraintName)
		{
			string query = "";

			switch (rdbms)
			{
				case DataSourceType.SqlServer:
					query = $"ALTER TABLE {tableName} NOCHECK CONSTRAINT {constraintName}";
					break;
				case DataSourceType.Oracle:
					query = $"ALTER TABLE {tableName} DISABLE CONSTRAINT {constraintName}";
					break;
				case DataSourceType.Postgre:
					query = $"ALTER TABLE {tableName} DISABLE TRIGGER ALL";
					break;
				case DataSourceType.Mysql:
					query = "SET FOREIGN_KEY_CHECKS = 0";
					break;
				case DataSourceType.DB2:
					query = $"ALTER TABLE {tableName} ALTER FOREIGN KEY {constraintName} NOT ENFORCED";
					break;
				case DataSourceType.FireBird:
					query = $"ALTER TABLE {tableName} DISABLE TRIGGER {constraintName}";
					break;
				case DataSourceType.SqlLite:
					query = "PRAGMA foreign_keys = OFF";
					break;
				case DataSourceType.Couchbase:
				case DataSourceType.Redis:
				case DataSourceType.MongoDB:
					query = "NoSQL databases typically do not have foreign key constraints in the same way RDBMS do.";
					break;
				default:
					query = "RDBMS not supported.";
					break;
			}

			return query;
		}
		/// <summary>Generates a query to enable a foreign key constraint in a specific RDBMS.</summary>
		/// <param name="rdbms">The type of RDBMS.</param>
		/// <param name="tableName">The name of the table.</param>
		/// <param name="constraintName">The name of the foreign key constraint.</param>
		/// <returns>A query to enable the specified foreign key constraint.</returns>
		/// <exception cref="ArgumentException">Thrown when the specified RDBMS is not supported.</exception>
		public static string GenerateEnableForeignKeyQuery(DataSourceType rdbms, string tableName, string constraintName)
		{
			string query = "";

			switch (rdbms)
			{
				case DataSourceType.SqlServer:
					query = $"ALTER TABLE {tableName} WITH CHECK CHECK CONSTRAINT {constraintName}";
					break;
				case DataSourceType.Oracle:
					query = $"ALTER TABLE {tableName} ENABLE CONSTRAINT {constraintName}";
					break;
				case DataSourceType.Postgre:
					// Assumes the constraint needs to be recreated
					query = $"ALTER TABLE {tableName} ENABLE TRIGGER ALL";
					break;
				case DataSourceType.Mysql:
					query = "SET FOREIGN_KEY_CHECKS = 1";
					break;
				case DataSourceType.DB2:
					query = $"ALTER TABLE {tableName} ALTER FOREIGN KEY {constraintName} ENFORCED";
					break;
				case DataSourceType.FireBird:
					query = $"ALTER TABLE {tableName} ENABLE TRIGGER {constraintName}";
					break;
				case DataSourceType.SqlLite:
					query = "PRAGMA foreign_keys = ON";
					break;
				case DataSourceType.Couchbase:
				case DataSourceType.Redis:
				case DataSourceType.MongoDB:
					query = "NoSQL databases typically do not have foreign key constraints in the same way RDBMS do.";
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

// ElasticSearch, MongoDB, Redis, etc., do not conform to this query pattern due to their non-relational nature and different data management models.




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

		// (Additional MySQL queries for FK, Child Table, Parent Table)

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
		// (Additional SQLite queries for FK, Child Table, Parent Table)

		// DuckDB
		 new QuerySqlRepo(DataSourceType.DuckDB, "SELECT * FROM {0} {2}", Sqlcommandtype.getTable),
		new QuerySqlRepo(DataSourceType.DuckDB, "SELECT name FROM sqlite_master WHERE type='table'", Sqlcommandtype.getlistoftables),
        new QuerySqlRepo(DataSourceType.DuckDB, "SELECT column_name FROM pragma_table_info('{0}') WHERE pk != 0;", Sqlcommandtype.getPKforTable),

	   // DB2
		new QuerySqlRepo(DataSourceType.DB2, "SELECT * FROM {0} {2}", Sqlcommandtype.getTable),
		new QuerySqlRepo(DataSourceType.DB2, "SELECT TABNAME FROM SYSCAT.TABLES WHERE TABSCHEMA = CURRENT SCHEMA", Sqlcommandtype.getlistoftables),
		new QuerySqlRepo(DataSourceType.DB2, "SELECT COLNAME COLUMN_NAME FROM SYSCAT.KEYCOLUSE WHERE TABNAME = '{0}' AND CONSTRAINTNAME LIKE 'PK%'", Sqlcommandtype.getPKforTable),
		new QuerySqlRepo(DataSourceType.DB2, "SELECT FK_COLNAMES AS child_column, PK_COLNAMES AS parent_column, PK_TBNAME AS parent_table FROM SYSIBM.SQLFOREIGNKEYS WHERE FK_TBNAME = '{0}'", Sqlcommandtype.getFKforTable),
		new QuerySqlRepo(DataSourceType.DB2, "SELECT FK_TBNAME AS child_table FROM SYSIBM.SQLFOREIGNKEYS WHERE PK_TBNAME = '{0}'", Sqlcommandtype.getChildTable),
		new QuerySqlRepo(DataSourceType.DB2, "SELECT PK_TBNAME AS parent_table FROM SYSIBM.SQLFOREIGNKEYS WHERE FK_TBNAME = '{0}'", Sqlcommandtype.getParentTable),

		new QuerySqlRepo(DataSourceType.MongoDB, "db.{0}.find({})", Sqlcommandtype.getTable), // Get all documents from a collection
		new QuerySqlRepo(DataSourceType.MongoDB, "db.getCollectionNames()", Sqlcommandtype.getlistoftables), // Get all collection names
// MongoDB does not have traditional PK or FK, but you can specify queries to get specific indexed fields or relationships if defined
		new QuerySqlRepo(DataSourceType.Redis, "GET {0}", Sqlcommandtype.getTable), // Get the value of a key
	// There's no direct equivalent of tables or foreign keys in Redis
	new QuerySqlRepo(DataSourceType.Cassandra, "SELECT * FROM {0}", Sqlcommandtype.getTable), // Get all rows from a table
new QuerySqlRepo(DataSourceType.Cassandra, "SELECT table_name FROM system_schema.tables WHERE keyspace_name = 'YourKeyspaceName'", Sqlcommandtype.getlistoftables), // Get list of tables
new QuerySqlRepo(DataSourceType.Cassandra, "SELECT column_name FROM system_schema.columns WHERE table_name = '{0}' AND keyspace_name = 'YourKeyspaceName' AND kind = 'partition_key'", Sqlcommandtype.getPKforTable), // Get PK for a table
// Cassandra does not support foreign keys like relational databases
new QuerySqlRepo(DataSourceType.FireBird, "SELECT * FROM {0}", Sqlcommandtype.getTable), // Get all rows from a table
new QuerySqlRepo(DataSourceType.FireBird, "SELECT RDB$RELATION_NAME FROM RDB$RELATIONS WHERE RDB$SYSTEM_FLAG = 0", Sqlcommandtype.getlistoftables), // Get list of tables
new QuerySqlRepo(DataSourceType.FireBird, "SELECT RDB$INDEX_SEGMENTS.RDB$FIELD_NAME FROM RDB$INDEX_SEGMENTS JOIN RDB$RELATION_CONSTRAINTS ON RDB$INDEX_SEGMENTS.RDB$INDEX_NAME = RDB$RELATION_CONSTRAINTS.RDB$INDEX_NAME WHERE RDB$RELATION_CONSTRAINTS.RDB$RELATION_NAME = '{0}' AND RDB$RELATION_CONSTRAINTS.RDB$CONSTRAINT_TYPE = 'PRIMARY KEY'", Sqlcommandtype.getPKforTable), // Get PK for a table
new QuerySqlRepo(DataSourceType.Couchbase, "SELECT * FROM `{0}`", Sqlcommandtype.getTable), // Get all documents from a bucket
new QuerySqlRepo(DataSourceType.Couchbase, "SELECT name FROM system:keyspaces", Sqlcommandtype.getlistoftables), // Get list of keyspaces/buckets
// Couchbase doesn't use traditional PKs or FKs; keys are usually part of the document structure
new QuerySqlRepo(DataSourceType.Hana, "SELECT * FROM {0}", Sqlcommandtype.getTable), // Get all rows from a table
new QuerySqlRepo(DataSourceType.Hana, "SELECT TABLE_NAME FROM TABLES WHERE SCHEMA_NAME = 'YOUR_SCHEMA_NAME'", Sqlcommandtype.getlistoftables), // Get list of tables
new QuerySqlRepo(DataSourceType.Hana, "SELECT COLUMN_NAME FROM CONSTRAINTS WHERE TABLE_NAME = '{0}' AND SCHEMA_NAME = 'YOUR_SCHEMA_NAME' AND IS_PRIMARY_KEY = 'TRUE'", Sqlcommandtype.getPKforTable), // Get PK for a table
new QuerySqlRepo(DataSourceType.Vertica, "SELECT * FROM {0}", Sqlcommandtype.getTable), // Get all rows from a table
new QuerySqlRepo(DataSourceType.Vertica, "SELECT table_name FROM v_catalog.tables WHERE table_schema = 'YOUR_SCHEMA_NAME'", Sqlcommandtype.getlistoftables), // Get list of tables
new QuerySqlRepo(DataSourceType.Vertica, "SELECT column_name FROM v_catalog.primary_keys WHERE table_name = '{0}' AND table_schema = 'YOUR_SCHEMA_NAME'", Sqlcommandtype.getPKforTable), // Get PK for a table
new QuerySqlRepo(DataSourceType.TerraData, "SELECT * FROM {0}", Sqlcommandtype.getTable), // Get all rows from a table
new QuerySqlRepo(DataSourceType.TerraData, "SELECT TableName Table_name FROM DBC.TablesV WHERE TableKind = 'T' AND DatabaseName = '{1}'", Sqlcommandtype.getlistoftables), // Get list of tables
new QuerySqlRepo(DataSourceType.TerraData, "SELECT ColumnName Column_name FROM DBC.IndicesV WHERE TableName = '{0}' AND DatabaseName = '{1}}' AND IndexType = 'P'", Sqlcommandtype.getPKforTable), // Get PK for a table
new QuerySqlRepo(DataSourceType.AzureCloud, "SELECT * FROM {0}", Sqlcommandtype.getTable), // Get all rows from a table
new QuerySqlRepo(DataSourceType.AzureCloud, "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'", Sqlcommandtype.getlistoftables), // Get list of tables
new QuerySqlRepo(DataSourceType.AzureCloud, "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE OBJECTPROPERTY(OBJECT_ID(CONSTRAINT_SCHEMA + '.' + CONSTRAINT_NAME), 'IsPrimaryKey') = 1 AND TABLE_NAME = '{0}'", Sqlcommandtype.getPKforTable), // Get PK for a table
new QuerySqlRepo(DataSourceType.GoogleBigQuery, "SELECT * FROM {0}", Sqlcommandtype.getTable), // Get all rows from a table
new QuerySqlRepo(DataSourceType.GoogleBigQuery, "SELECT table_name FROM `YOUR_DATASET.INFORMATION_SCHEMA.TABLES`", Sqlcommandtype.getlistoftables), // Get list of tables
// BigQuery does not have a traditional concept of primary keys, but you can query the schema to find fields that might act as a key
new QuerySqlRepo(DataSourceType.SnowFlake, "SELECT * FROM {0}", Sqlcommandtype.getTable), // Get all rows from a table
new QuerySqlRepo(DataSourceType.SnowFlake, "SELECT TABLE_NAME FROM {1}.INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'", Sqlcommandtype.getlistoftables),
new QuerySqlRepo(DataSourceType.SnowFlake, "SELECT kcu.COLUMN_NAME FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu ON tc.TABLE_NAME = kcu.TABLE_NAME AND tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME WHERE tc.TABLE_SCHEMA = '{1}' AND tc.TABLE_NAME = '{0}' AND tc.CONSTRAINT_TYPE = 'PRIMARY KEY'", Sqlcommandtype.getPKforTable),
new QuerySqlRepo(DataSourceType.ElasticSearch, "SELECT * FROM {0}", Sqlcommandtype.getTable), // Executes a search query that, by default, retrieves the first 1000 documents.
// Elasticsearch doesn't have the concept of tables or primary/foreign keys in the traditional sense.
new QuerySqlRepo(DataSourceType.Cassandra, "SELECT * FROM {0}", Sqlcommandtype.getTable), // Get all rows from a table
new QuerySqlRepo(DataSourceType.Cassandra, "SELECT table_name FROM system_schema.tables WHERE keyspace_name = 'YourKeyspaceName'", Sqlcommandtype.getlistoftables), // Get list of tables
new QuerySqlRepo(DataSourceType.Cassandra, "SELECT column_name FROM system_schema.columns WHERE table_name = '{0}' AND keyspace_name = 'YourKeyspaceName' AND kind = 'partition_key'", Sqlcommandtype.getPKforTable), // Get primary key column
new QuerySqlRepo(DataSourceType.CouchDB, "SELECT * FROM {0}", Sqlcommandtype.getTable), // This is a conceptual example; actual querying in CouchDB is done through views and not SQL.
// CouchDB doesn't have a concept of tables in the same way SQL databases do. It stores JSON documents directly.

new QuerySqlRepo(DataSourceType.Neo4j, "MATCH (n) RETURN n", Sqlcommandtype.getTable), // Get all nodes
// Neo4j does not use tables, so there's no direct equivalent to getting a list of tables or primary/foreign keys.
new QuerySqlRepo(DataSourceType.InfluxDB, "SELECT * FROM {0}", Sqlcommandtype.getTable), // Query data from a measurement
new QuerySqlRepo(DataSourceType.InfluxDB, "SHOW MEASUREMENTS", Sqlcommandtype.getlistoftables), // Get list of measurements (similar to tables)
new QuerySqlRepo(DataSourceType.DynamoDB, "Scan {0}", Sqlcommandtype.getTable), // Scan operation (be mindful of performance and cost)
new QuerySqlRepo(DataSourceType.TimeScale, "SELECT * FROM {0}", Sqlcommandtype.getTable), // Get all rows from a hypertable
new QuerySqlRepo(DataSourceType.TimeScale, "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'", Sqlcommandtype.getlistoftables), // Get list of tables (hypertables)
new QuerySqlRepo(DataSourceType.TimeScale, "SELECT a.attname column_name FROM pg_index i JOIN pg_attribute a ON a.attnum = ANY(i.indkey) WHERE i.indrelid = '{0}'::regclass AND i.indisprimary", Sqlcommandtype.getPKforTable), // Get PK for a table
new QuerySqlRepo(DataSourceType.Cockroach, "SELECT * FROM {0}", Sqlcommandtype.getTable), // Get all rows from a table
new QuerySqlRepo(DataSourceType.Cockroach, "SHOW TABLES", Sqlcommandtype.getlistoftables), // Get list of tables
new QuerySqlRepo(DataSourceType.Cockroach, "SELECT column_name FROM information_schema.columns WHERE table_name = '{0}' AND is_nullable = 'NO'", Sqlcommandtype.getPKforTable), // Get PK for a table (simplified)
new QuerySqlRepo(DataSourceType.Kafka, "LIST TOPICS", Sqlcommandtype.getlistoftables), // Conceptual command to list topics
new QuerySqlRepo(DataSourceType.OPC, "READ NODE", Sqlcommandtype.getTable), // Conceptual command to read data from an OPC node

	};
		}

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
