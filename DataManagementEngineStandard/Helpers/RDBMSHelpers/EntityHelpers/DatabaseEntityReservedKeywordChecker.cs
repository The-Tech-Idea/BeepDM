using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Helpers.RDBMSHelpers.EntityHelpers
{
    /// <summary>
    /// Helper class for checking reserved keywords across different database types.
    /// </summary>
    public static class DatabaseEntityReservedKeywordChecker
    {
        /// <summary>
        /// Checks if an identifier is a reserved keyword for the given database type.
        /// </summary>
        /// <param name="identifier">The identifier to check</param>
        /// <param name="databaseType">The database type</param>
        /// <returns>True if it's a reserved keyword</returns>
        public static bool IsReservedKeyword(string identifier, DataSourceType databaseType)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                return false;

            var upperIdentifier = identifier.ToUpperInvariant();
            
            // Common reserved keywords across most databases
            var commonKeywords = GetCommonReservedKeywords();
            if (commonKeywords.Contains(upperIdentifier))
                return true;

            // Database-specific keywords
            return databaseType switch
            {
                DataSourceType.SqlServer => IsSqlServerReservedKeyword(upperIdentifier),
                DataSourceType.Mysql => IsMySqlReservedKeyword(upperIdentifier),
                DataSourceType.Postgre => IsPostgreSQLReservedKeyword(upperIdentifier),
                DataSourceType.Oracle => IsOracleReservedKeyword(upperIdentifier),
                DataSourceType.SqlLite => IsSQLiteReservedKeyword(upperIdentifier),
                _ => false
            };
        }

        /// <summary>
        /// Gets common reserved keywords across most databases.
        /// </summary>
        /// <returns>HashSet of common reserved keywords</returns>
        private static HashSet<string> GetCommonReservedKeywords()
        {
            return new HashSet<string>
            {
                "SELECT", "INSERT", "UPDATE", "DELETE", "CREATE", "DROP", "ALTER",
                "TABLE", "INDEX", "VIEW", "DATABASE", "SCHEMA", "FROM", "WHERE",
                "JOIN", "INNER", "LEFT", "RIGHT", "OUTER", "ON", "AS", "AND",
                "OR", "NOT", "NULL", "TRUE", "FALSE", "IS", "IN", "EXISTS",
                "BETWEEN", "LIKE", "ORDER", "BY", "GROUP", "HAVING", "DISTINCT",
                "COUNT", "SUM", "AVG", "MIN", "MAX", "CASE", "WHEN", "THEN",
                "ELSE", "END", "IF", "UNION", "ALL", "PRIMARY", "KEY", "FOREIGN",
                "REFERENCES", "UNIQUE", "CHECK", "DEFAULT", "CONSTRAINT"
            };
        }

        /// <summary>
        /// Checks SQL Server specific reserved keywords.
        /// </summary>
        private static bool IsSqlServerReservedKeyword(string keyword)
        {
            var sqlServerKeywords = new HashSet<string>
            {
                "IDENTITY", "CLUSTERED", "NONCLUSTERED", "FILLFACTOR", "TEXTSIZE",
                "ROWGUIDCOL", "COLLATE", "COMPUTE", "CONTAINS", "FREETEXT",
                "PIVOT", "UNPIVOT", "MERGE", "OUTPUT", "TRY", "CATCH"
            };
            return sqlServerKeywords.Contains(keyword);
        }

        /// <summary>
        /// Checks MySQL specific reserved keywords.
        /// </summary>
        private static bool IsMySqlReservedKeyword(string keyword)
        {
            var mysqlKeywords = new HashSet<string>
            {
                "AUTO_INCREMENT", "ZEROFILL", "UNSIGNED", "BINARY", "CHARSET",
                "COLLATION", "ENGINE", "PARTITION", "SUBPARTITION", "ALGORITHM",
                "DEFINER", "SQL_SECURITY", "DELIMITER"
            };
            return mysqlKeywords.Contains(keyword);
        }

        /// <summary>
        /// Checks PostgreSQL specific reserved keywords.
        /// </summary>
        private static bool IsPostgreSQLReservedKeyword(string keyword)
        {
            var postgresKeywords = new HashSet<string>
            {
                "SERIAL", "BIGSERIAL", "SMALLSERIAL", "RETURNING", "WINDOW",
                "OVER", "PARTITION", "RANGE", "ROWS", "PRECEDING", "FOLLOWING",
                "UNBOUNDED", "CURRENT", "EXCLUDE", "TIES", "OTHERS"
            };
            return postgresKeywords.Contains(keyword);
        }

        /// <summary>
        /// Checks Oracle specific reserved keywords.
        /// </summary>
        private static bool IsOracleReservedKeyword(string keyword)
        {
            var oracleKeywords = new HashSet<string>
            {
                "DUAL", "SYSDATE", "ROWNUM", "ROWID", "NEXTVAL", "CURRVAL",
                "SEQUENCE", "TRIGGER", "PACKAGE", "PROCEDURE", "FUNCTION",
                "TABLESPACE", "DATAFILE", "ARCHIVELOG", "NOARCHIVELOG"
            };
            return oracleKeywords.Contains(keyword);
        }

        /// <summary>
        /// Checks SQLite specific reserved keywords.
        /// </summary>
        private static bool IsSQLiteReservedKeyword(string keyword)
        {
            var sqliteKeywords = new HashSet<string>
            {
                "AUTOINCREMENT", "PRAGMA", "VACUUM", "ANALYZE", "ATTACH",
                "DETACH", "TEMP", "TEMPORARY", "WITHOUT", "ROWID"
            };
            return sqliteKeywords.Contains(keyword);
        }
    }
}