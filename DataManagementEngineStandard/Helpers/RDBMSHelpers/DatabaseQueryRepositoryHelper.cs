using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Helpers.RDBMSHelpers
{
    /// <summary>
    /// Helper class for managing and accessing predefined SQL query repositories for different database types.
    /// </summary>
    public static class DatabaseQueryRepositoryHelper
    {
        private static readonly Dictionary<(DataSourceType, Sqlcommandtype), string> QueryCache =
            new Dictionary<(DataSourceType, Sqlcommandtype), string>();

        static DatabaseQueryRepositoryHelper()
        {
            // Initialize the query cache with queries from CreateQuerySqlRepos
            var queries = CreateQuerySqlRepos();
            foreach (var query in queries)
            {
                QueryCache[(query.DatabaseType, query.Sqltype)] = query.Sql;
            }
        }

        /// <summary>
        /// Gets a predefined query for the specified database type and query type.
        /// </summary>
        /// <param name="dataSourceType">The database type</param>
        /// <param name="queryType">The type of query needed</param>
        /// <returns>The SQL query string, or empty string if not found</returns>
        public static string GetQuery(DataSourceType dataSourceType, Sqlcommandtype queryType)
        {
            if (QueryCache.TryGetValue((dataSourceType, queryType), out string query))
                return query;

            return string.Empty;
        }

        /// <summary>
        /// Gets all available queries for a specific database type.
        /// </summary>
        /// <param name="dataSourceType">The database type</param>
        /// <returns>Dictionary of query types and their corresponding SQL</returns>
        public static Dictionary<Sqlcommandtype, string> GetQueriesForDatabase(DataSourceType dataSourceType)
        {
            return QueryCache
                .Where(kvp => kvp.Key.Item1 == dataSourceType)
                .ToDictionary(kvp => kvp.Key.Item2, kvp => kvp.Value);
        }

        /// <summary>
        /// Gets all database types that have queries for a specific query type.
        /// </summary>
        /// <param name="queryType">The query type</param>
        /// <returns>List of database types that support this query type</returns>
        public static List<DataSourceType> GetDatabasesForQueryType(Sqlcommandtype queryType)
        {
            return QueryCache
                .Where(kvp => kvp.Key.Item2 == queryType)
                .Select(kvp => kvp.Key.Item1)
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Checks if a query exists for the specified database and query type combination.
        /// </summary>
        /// <param name="dataSourceType">The database type</param>
        /// <param name="queryType">The query type</param>
        /// <returns>True if the query exists</returns>
        public static bool QueryExists(DataSourceType dataSourceType, Sqlcommandtype queryType)
        {
            return QueryCache.ContainsKey((dataSourceType, queryType));
        }

        /// <summary>
        /// Checks if a given SQL statement is valid by looking for common SQL keywords.
        /// </summary>
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
                "FROM", "WHERE", "JOIN", "ON", "AND", "OR", "NOT", "IN", "LIKE",
                "SHOW", "PRAGMA", "CALL", "EXEC", "EXECUTE"
            };

            // Create a regular expression pattern to match SQL keywords
            string pattern = @"\b(" + string.Join("|", sqlKeywords) + @")\b";

            // Use Regex to find matches
            MatchCollection matches = Regex.Matches(sqlString, pattern, RegexOptions.IgnoreCase);

            // If any keywords are found, return true
            return matches.Count > 0;
        }

        /// <summary>
        /// Creates a comprehensive list of QuerySqlRepo objects for different database types and operations.
        /// </summary>
        /// <returns>A list of QuerySqlRepo objects representing different query configurations.</returns>
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

                // Standard table operations
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

                // NoSQL Databases with available query equivalents
                // MongoDB
                new QuerySqlRepo(DataSourceType.MongoDB, "db.{0}.find({})", Sqlcommandtype.getTable), // Get all documents from a collection
                new QuerySqlRepo(DataSourceType.MongoDB, "db.getCollectionNames()", Sqlcommandtype.getlistoftables), // Get all collection names

                // Redis
                new QuerySqlRepo(DataSourceType.Redis, "GET {0}", Sqlcommandtype.getTable), // Get the value of a key

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

                // Cloud and Enterprise Databases
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

                // Snowflake
                new QuerySqlRepo(DataSourceType.SnowFlake, "SELECT * FROM {0}", Sqlcommandtype.getTable), // Get all rows from a table
                new QuerySqlRepo(DataSourceType.SnowFlake, "SELECT TABLE_NAME FROM {1}.INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'", Sqlcommandtype.getlistoftables),
                new QuerySqlRepo(DataSourceType.SnowFlake, "SELECT kcu.COLUMN_NAME FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu ON tc.TABLE_NAME = kcu.TABLE_NAME AND tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME WHERE tc.TABLE_SCHEMA = '{1}' AND tc.TABLE_NAME = '{0}' AND tc.CONSTRAINT_TYPE = 'PRIMARY KEY'", Sqlcommandtype.getPKforTable),

                // ElasticSearch
                new QuerySqlRepo(DataSourceType.ElasticSearch, "SELECT * FROM {0}", Sqlcommandtype.getTable), // Executes a search query that, by default, retrieves the first 1000 documents.

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

                // Streaming and Messaging
                // Kafka
                new QuerySqlRepo(DataSourceType.Kafka, "LIST TOPICS", Sqlcommandtype.getlistoftables), // Conceptual command to list topics

                // OPC
                new QuerySqlRepo(DataSourceType.OPC, "READ NODE", Sqlcommandtype.getTable), // Conceptual command to read data from an OPC node

                // File-based queries
                // Excel/CSV Query Sql Set
                new QuerySqlRepo(DataSourceType.Xls, "select * from [{0}] {2}", Sqlcommandtype.getTable),
                new QuerySqlRepo(DataSourceType.Text, "select * from [{0}] {2}", Sqlcommandtype.getTable)
            };
        }

        /// <summary>
        /// Gets query statistics for analysis and debugging purposes.
        /// </summary>
        /// <returns>Dictionary with statistics about the query repository</returns>
        public static Dictionary<string, object> GetQueryStatistics()
        {
            var stats = new Dictionary<string, object>();
            
            var totalQueries = QueryCache.Count;
            var databaseTypes = QueryCache.Keys.Select(k => k.Item1).Distinct().Count();
            var queryTypes = QueryCache.Keys.Select(k => k.Item2).Distinct().Count();
            
            var queriesPerDatabase = QueryCache.Keys
                .GroupBy(k => k.Item1)
                .ToDictionary(g => g.Key.ToString(), g => g.Count());
                
            var queriesPerType = QueryCache.Keys
                .GroupBy(k => k.Item2)
                .ToDictionary(g => g.Key.ToString(), g => g.Count());
            
            stats["TotalQueries"] = totalQueries;
            stats["DatabaseTypes"] = databaseTypes;
            stats["QueryTypes"] = queryTypes;
            stats["QueriesPerDatabase"] = queriesPerDatabase;
            stats["QueriesPerType"] = queriesPerType;
            
            return stats;
        }

        /// <summary>
        /// Validates all queries in the repository for basic syntax issues.
        /// </summary>
        /// <returns>List of validation issues found</returns>
        public static List<string> ValidateAllQueries()
        {
            var issues = new List<string>();
            
            foreach (var kvp in QueryCache)
            {
                var (dataSourceType, queryType) = kvp.Key;
                var query = kvp.Value;
                
                if (string.IsNullOrWhiteSpace(query))
                {
                    issues.Add($"{dataSourceType}.{queryType}: Query is empty or null");
                    continue;
                }
                
                if (!IsSqlStatementValid(query) && 
                    !query.StartsWith("db.") && // MongoDB
                    !query.StartsWith("GET ") && // Redis
                    !query.StartsWith("LIST ") && // Kafka
                    !query.StartsWith("READ ") && // OPC
                    !query.StartsWith("Scan ") && // DynamoDB
                    !query.StartsWith("SHOW ") && // Various
                    !query.Contains("NoSQL databases")) // NoSQL message
                {
                    issues.Add($"{dataSourceType}.{queryType}: Query appears to have syntax issues: {query}");
                }
            }
            
            return issues;
        }
    }
}