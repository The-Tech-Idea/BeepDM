using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Helpers.RDBMSHelpers.DMLHelpers
{
    /// <summary>
    /// Utility methods for database operations including paging, counting, and value handling.
    /// </summary>
    public static class DatabaseDMLUtilities
    {
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
        /// Safely quotes a string value for SQL to prevent injection attacks.
        /// </summary>
        /// <param name="value">The value to quote</param>
        /// <param name="dataSourceType">The database type</param>
        /// <returns>Safely quoted string</returns>
        public static string SafeQuote(string value, DataSourceType dataSourceType)
        {
            if (value == null) return "NULL";
            
            return dataSourceType switch
            {
                DataSourceType.Oracle or DataSourceType.SqlServer or DataSourceType.Mysql => 
                    "'" + value.Replace("'", "''") + "'",
                DataSourceType.Postgre => 
                    "'" + value.Replace("'", "''").Replace("\\", "\\\\") + "'",
                _ => 
                    "'" + value.Replace("'", "''") + "'"
            };
        }

        /// <summary>
        /// Validates table name to prevent SQL injection and ensure valid identifier format.
        /// </summary>
        /// <param name="tableName">Table name to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool IsValidTableName(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                return false;

            // Basic validation - alphanumeric, underscore, and dot (for schema.table)
            return System.Text.RegularExpressions.Regex.IsMatch(tableName, @"^[a-zA-Z][a-zA-Z0-9_]*(\.[a-zA-Z][a-zA-Z0-9_]*)?$");
        }

        /// <summary>
        /// Validates column name to prevent SQL injection and ensure valid identifier format.
        /// </summary>
        /// <param name="columnName">Column name to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool IsValidColumnName(string columnName)
        {
            if (string.IsNullOrWhiteSpace(columnName))
                return false;

            // Basic validation - alphanumeric and underscore, must start with letter or underscore
            return System.Text.RegularExpressions.Regex.IsMatch(columnName, @"^[a-zA-Z_][a-zA-Z0-9_]*$");
        }

        /// <summary>
        /// Gets the appropriate quote character for identifiers based on database type.
        /// </summary>
        /// <param name="dataSourceType">Database type</param>
        /// <returns>Quote character for identifiers</returns>
        public static string GetIdentifierQuoteChar(DataSourceType dataSourceType)
        {
            return dataSourceType switch
            {
                DataSourceType.SqlServer => "[",
                DataSourceType.Mysql => "`",
                DataSourceType.Postgre => "\"",
                DataSourceType.Oracle => "\"",
                DataSourceType.SqlLite => "\"",
                DataSourceType.DB2 => "\"",
                DataSourceType.FireBird => "\"",
                _ => "\""
            };
        }

        /// <summary>
        /// Quotes an identifier if necessary based on database type and identifier content.
        /// </summary>
        /// <param name="identifier">The identifier to quote</param>
        /// <param name="dataSourceType">Database type</param>
        /// <returns>Quoted identifier if necessary</returns>
        public static string QuoteIdentifierIfNeeded(string identifier, DataSourceType dataSourceType)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                return identifier;

            // Check if identifier needs quoting (contains spaces, special chars, or is a reserved word)
            var needsQuoting = !IsValidColumnName(identifier) || 
                              identifier.Contains(' ') || 
                              IsReservedKeyword(identifier, dataSourceType);

            if (!needsQuoting)
                return identifier;

            var quoteChar = GetIdentifierQuoteChar(dataSourceType);
            var endQuoteChar = dataSourceType == DataSourceType.SqlServer ? "]" : quoteChar;

            return $"{quoteChar}{identifier}{endQuoteChar}";
        }

        /// <summary>
        /// Checks if an identifier is a reserved keyword for basic SQL operations.
        /// </summary>
        /// <param name="identifier">The identifier to check</param>
        /// <param name="dataSourceType">Database type</param>
        /// <returns>True if it's a reserved keyword</returns>
        private static bool IsReservedKeyword(string identifier, DataSourceType dataSourceType)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                return false;

            var upperIdentifier = identifier.ToUpperInvariant();
            
            // Common reserved keywords that would affect DML operations
            var commonKeywords = new HashSet<string>
            {
                "SELECT", "INSERT", "UPDATE", "DELETE", "FROM", "WHERE", "AND", "OR", "NOT",
                "NULL", "TRUE", "FALSE", "IS", "IN", "EXISTS", "BETWEEN", "LIKE", "ORDER", "BY",
                "GROUP", "HAVING", "DISTINCT", "COUNT", "SUM", "AVG", "MIN", "MAX", "AS",
                "JOIN", "INNER", "LEFT", "RIGHT", "OUTER", "ON", "UNION", "ALL"
            };

            return commonKeywords.Contains(upperIdentifier);
        }
    }
}