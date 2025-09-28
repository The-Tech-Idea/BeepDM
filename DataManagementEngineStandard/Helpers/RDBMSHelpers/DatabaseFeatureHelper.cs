using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Helpers.RDBMSHelpers
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
    /// Helper class for database feature detection, sequence operations, and transaction management.
    /// </summary>
    public static class DatabaseFeatureHelper
    {
        /// <summary>
        /// Generates a query to fetch the next value from a sequence in a specific database.
        /// </summary>
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

            return rdbms switch
            {
                DataSourceType.Oracle => $"SELECT {sequenceName}.NEXTVAL FROM dual",
                DataSourceType.Postgre => $"SELECT nextval('{sequenceName}')",
                DataSourceType.SqlServer => $"SELECT NEXT VALUE FOR {sequenceName}",
                DataSourceType.FireBird => $"SELECT NEXT VALUE FOR {sequenceName} FROM RDB$DATABASE",
                DataSourceType.DB2 => $"SELECT NEXTVAL FOR {sequenceName} FROM sysibm.sysdummy1",
                _ => null
            };
        }

        /// <summary>
        /// Generates a query to fetch the last inserted identity value based on the specified RDBMS.
        /// </summary>
        /// <param name="rdbms">The type of RDBMS.</param>
        /// <param name="sequenceName">The name of the sequence or generator (optional for some RDBMS).</param>
        /// <returns>A query string to fetch the last inserted identity value.</returns>
        /// <exception cref="ArgumentException">Thrown when the specified RDBMS is not supported.</exception>
        public static string GenerateFetchLastIdentityQuery(DataSourceType rdbms, string sequenceName = "")
        {
            return rdbms switch
            {
                DataSourceType.SqlServer => "SELECT SCOPE_IDENTITY()",
                DataSourceType.Mysql => "SELECT LAST_INSERT_ID()",
                DataSourceType.Postgre => "SELECT LASTVAL()",
                DataSourceType.Oracle => string.IsNullOrEmpty(sequenceName) 
                    ? "Provide a sequence name." 
                    : $"SELECT currval('{sequenceName}') FROM dual",
                DataSourceType.FireBird => string.IsNullOrEmpty(sequenceName) 
                    ? "Provide a generator name." 
                    : $"SELECT GEN_ID({sequenceName}, 0) FROM RDB$DATABASE",
                DataSourceType.SqlLite => "SELECT last_insert_rowid()",
                DataSourceType.DB2 => "SELECT IDENTITY_VAL_LOCAL() FROM sysibm.sysdummy1",
                DataSourceType.Cassandra => "Unsupported in Cassandra", // NOTE: Cassandra doesn't support this the same way relational DBs do
                _ => "RDBMS not supported."
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
        /// Gets all supported features for a given database type.
        /// </summary>
        /// <param name="dataSourceType">Database type to check</param>
        /// <returns>List of supported database features</returns>
        public static List<DatabaseFeature> GetSupportedFeatures(DataSourceType dataSourceType)
        {
            var supportedFeatures = new List<DatabaseFeature>();
            
            foreach (DatabaseFeature feature in Enum.GetValues<DatabaseFeature>())
            {
                if (SupportsFeature(dataSourceType, feature))
                {
                    supportedFeatures.Add(feature);
                }
            }
            
            return supportedFeatures;
        }

        /// <summary>
        /// Checks if a database supports sequences (auto-incrementing values).
        /// </summary>
        /// <param name="dataSourceType">Database type to check</param>
        /// <returns>True if sequences are supported</returns>
        public static bool SupportsSequences(DataSourceType dataSourceType)
        {
            return dataSourceType switch
            {
                DataSourceType.Oracle => true,
                DataSourceType.Postgre => true,
                DataSourceType.SqlServer => true, // SQL Server 2012+
                DataSourceType.FireBird => true,
                DataSourceType.DB2 => true,
                DataSourceType.H2Database => true,
                _ => false
            };
        }

        /// <summary>
        /// Checks if a database supports auto-increment/identity columns.
        /// </summary>
        /// <param name="dataSourceType">Database type to check</param>
        /// <returns>True if auto-increment is supported</returns>
        public static bool SupportsAutoIncrement(DataSourceType dataSourceType)
        {
            return dataSourceType switch
            {
                DataSourceType.SqlServer => true,
                DataSourceType.Mysql => true,
                DataSourceType.Postgre => true,
                DataSourceType.Oracle => true, // Oracle 12c+
                DataSourceType.SqlLite => true,
                DataSourceType.DB2 => true,
                DataSourceType.FireBird => true,
                DataSourceType.H2Database => true,
                DataSourceType.Cockroach => true,
                _ => false
            };
        }

        /// <summary>
        /// Gets the maximum identifier length for a given database type.
        /// </summary>
        /// <param name="dataSourceType">Database type</param>
        /// <returns>Maximum identifier length in characters</returns>
        public static int GetMaxIdentifierLength(DataSourceType dataSourceType)
        {
            return dataSourceType switch
            {
                DataSourceType.SqlServer => 128,
                DataSourceType.Mysql => 64,
                DataSourceType.Postgre => 63,
                DataSourceType.Oracle => 30, // Pre-12.2, 128 in 12.2+
                DataSourceType.SqlLite => 1000, // No hard limit, but practical limit
                DataSourceType.DB2 => 128,
                DataSourceType.FireBird => 31,
                DataSourceType.SnowFlake => 255,
                DataSourceType.Cockroach => 63,
                DataSourceType.Vertica => 128,
                DataSourceType.GoogleBigQuery => 1024,
                DataSourceType.AWSRedshift => 127,
                DataSourceType.ClickHouse => 127,
                _ => 64 // Conservative default
            };
        }

        /// <summary>
        /// Checks if a database type supports stored procedures.
        /// </summary>
        /// <param name="dataSourceType">Database type to check</param>
        /// <returns>True if stored procedures are supported</returns>
        public static bool SupportsStoredProcedures(DataSourceType dataSourceType)
        {
            return dataSourceType switch
            {
                DataSourceType.SqlServer => true,
                DataSourceType.Mysql => true,
                DataSourceType.Postgre => true, // Functions, not traditional stored procedures
                DataSourceType.Oracle => true,
                DataSourceType.DB2 => true,
                DataSourceType.FireBird => true,
                DataSourceType.SnowFlake => true,
                DataSourceType.Vertica => true,
                _ => false
            };
        }

        /// <summary>
        /// Checks if a database type supports user-defined functions.
        /// </summary>
        /// <param name="dataSourceType">Database type to check</param>
        /// <returns>True if user-defined functions are supported</returns>
        public static bool SupportsUserDefinedFunctions(DataSourceType dataSourceType)
        {
            return dataSourceType switch
            {
                DataSourceType.SqlServer => true,
                DataSourceType.Mysql => true,
                DataSourceType.Postgre => true,
                DataSourceType.Oracle => true,
                DataSourceType.DB2 => true,
                DataSourceType.FireBird => true,
                DataSourceType.SqlLite => true, // Application-defined functions
                DataSourceType.SnowFlake => true,
                DataSourceType.Vertica => true,
                DataSourceType.ClickHouse => true,
                _ => false
            };
        }

        /// <summary>
        /// Checks if a database type supports views.
        /// </summary>
        /// <param name="dataSourceType">Database type to check</param>
        /// <returns>True if views are supported</returns>
        public static bool SupportsViews(DataSourceType dataSourceType)
        {
            return dataSourceType switch
            {
                DataSourceType.SqlServer => true,
                DataSourceType.Mysql => true,
                DataSourceType.Postgre => true,
                DataSourceType.Oracle => true,
                DataSourceType.DB2 => true,
                DataSourceType.FireBird => true,
                DataSourceType.SqlLite => true,
                DataSourceType.SnowFlake => true,
                DataSourceType.Vertica => true,
                DataSourceType.Cockroach => true,
                DataSourceType.GoogleBigQuery => true,
                DataSourceType.AWSRedshift => true,
                DataSourceType.ClickHouse => true,
                _ => false
            };
        }

        /// <summary>
        /// Gets database-specific information including version requirements for features.
        /// </summary>
        /// <param name="dataSourceType">Database type</param>
        /// <returns>Dictionary containing database information</returns>
        public static Dictionary<string, object> GetDatabaseInfo(DataSourceType dataSourceType)
        {
            return dataSourceType switch
            {
                DataSourceType.SqlServer => new Dictionary<string, object>
                {
                    ["Name"] = "Microsoft SQL Server",
                    ["Type"] = "Relational",
                    ["SupportsTransactions"] = true,
                    ["SupportsCTE"] = true,
                    ["SupportsRecursiveCTE"] = true,
                    ["DefaultPort"] = 1433,
                    ["CaseSensitive"] = false,
                    ["MaxConnections"] = 32767,
                    ["WindowFunctionsMinVersion"] = "2005",
                    ["JSONMinVersion"] = "2016",
                    ["TemporalTablesMinVersion"] = "2016"
                },
                DataSourceType.Mysql => new Dictionary<string, object>
                {
                    ["Name"] = "MySQL",
                    ["Type"] = "Relational", 
                    ["SupportsTransactions"] = true,
                    ["SupportsCTE"] = true, // MySQL 8.0+
                    ["SupportsRecursiveCTE"] = true, // MySQL 8.0+
                    ["DefaultPort"] = 3306,
                    ["CaseSensitive"] = true, // Depends on OS and settings
                    ["MaxConnections"] = 151, // Default, configurable
                    ["WindowFunctionsMinVersion"] = "8.0",
                    ["JSONMinVersion"] = "5.7",
                    ["CTEMinVersion"] = "8.0"
                },
                DataSourceType.Postgre => new Dictionary<string, object>
                {
                    ["Name"] = "PostgreSQL",
                    ["Type"] = "Relational",
                    ["SupportsTransactions"] = true,
                    ["SupportsCTE"] = true,
                    ["SupportsRecursiveCTE"] = true,
                    ["DefaultPort"] = 5432,
                    ["CaseSensitive"] = true,
                    ["MaxConnections"] = 100, // Default, configurable
                    ["WindowFunctionsMinVersion"] = "8.4",
                    ["JSONMinVersion"] = "9.2",
                    ["JSONBMinVersion"] = "9.4"
                },
                DataSourceType.Oracle => new Dictionary<string, object>
                {
                    ["Name"] = "Oracle Database",
                    ["Type"] = "Relational",
                    ["SupportsTransactions"] = true,
                    ["SupportsCTE"] = true, // Oracle 11g R2+
                    ["SupportsRecursiveCTE"] = true,
                    ["DefaultPort"] = 1521,
                    ["CaseSensitive"] = false, // Uppercase by default
                    ["MaxConnections"] = "Unlimited", // License dependent
                    ["WindowFunctionsMinVersion"] = "8i",
                    ["JSONMinVersion"] = "12c",
                    ["TemporalTablesMinVersion"] = "12c"
                },
                _ => new Dictionary<string, object>
                {
                    ["Name"] = dataSourceType.ToString(),
                    ["Type"] = "Unknown",
                    ["SupportsTransactions"] = false,
                    ["SupportsCTE"] = false,
                    ["SupportsRecursiveCTE"] = false
                }
            };
        }
    }
}