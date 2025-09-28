using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Helpers.RDBMSHelpers.DMLHelpers
{
    /// <summary>
    /// Helper class for generating parameterized queries with proper parameter placeholders for each database type.
    /// </summary>
    public static class DatabaseDMLParameterizedQueries
    {
        /// <summary>
        /// Generates a parameterized INSERT query with proper parameter placeholders for each database type.
        /// </summary>
        /// <param name="rdbms">The type of RDBMS.</param>
        /// <param name="tableName">The name of the table.</param>
        /// <param name="columns">The column names for the insert.</param>
        /// <returns>A parameterized SQL INSERT query.</returns>
        public static string GenerateParameterizedInsertQuery(DataSourceType rdbms, string tableName, IEnumerable<string> columns)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Table name cannot be null or empty", nameof(tableName));

            if (columns == null || !columns.Any())
                throw new ArgumentException("No columns provided for insertion", nameof(columns));

            var columnList = string.Join(", ", columns);
            
            return rdbms switch
            {
                DataSourceType.SqlServer => $"INSERT INTO {tableName} ({columnList}) VALUES ({string.Join(", ", columns.Select(c => "@" + c))})",
                DataSourceType.Oracle => $"INSERT INTO {tableName} ({columnList}) VALUES ({string.Join(", ", columns.Select(c => ":" + c))})",
                DataSourceType.Mysql or DataSourceType.Postgre or DataSourceType.SqlLite => 
                    $"INSERT INTO {tableName} ({columnList}) VALUES ({string.Join(", ", columns.Select(c => "?"))})",
                _ => $"INSERT INTO {tableName} ({columnList}) VALUES ({string.Join(", ", columns.Select(c => "?"))})"
            };
        }

        /// <summary>
        /// Generates a parameterized UPDATE query with proper parameter placeholders for each database type.
        /// </summary>
        /// <param name="rdbms">The type of RDBMS.</param>
        /// <param name="tableName">The name of the table.</param>
        /// <param name="updateColumns">The columns to update.</param>
        /// <param name="whereColumns">The columns for the WHERE clause.</param>
        /// <returns>A parameterized SQL UPDATE query.</returns>
        public static string GenerateParameterizedUpdateQuery(DataSourceType rdbms, string tableName, 
            IEnumerable<string> updateColumns, IEnumerable<string> whereColumns)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Table name cannot be null or empty", nameof(tableName));

            if (updateColumns == null || !updateColumns.Any())
                throw new ArgumentException("No update columns provided", nameof(updateColumns));

            if (whereColumns == null || !whereColumns.Any())
                throw new ArgumentException("No WHERE columns provided", nameof(whereColumns));

            return rdbms switch
            {
                DataSourceType.SqlServer => 
                    $"UPDATE {tableName} SET {string.Join(", ", updateColumns.Select(c => $"{c} = @{c}"))} WHERE {string.Join(" AND ", whereColumns.Select(c => $"{c} = @{c}"))}",
                DataSourceType.Oracle => 
                    $"UPDATE {tableName} SET {string.Join(", ", updateColumns.Select(c => $"{c} = :{c}"))} WHERE {string.Join(" AND ", whereColumns.Select(c => $"{c} = :{c}"))}",
                DataSourceType.Mysql or DataSourceType.Postgre or DataSourceType.SqlLite => 
                    $"UPDATE {tableName} SET {string.Join(", ", updateColumns.Select(c => $"{c} = ?"))} WHERE {string.Join(" AND ", whereColumns.Select(c => $"{c} = ?"))}",
                _ => 
                    $"UPDATE {tableName} SET {string.Join(", ", updateColumns.Select(c => $"{c} = ?"))} WHERE {string.Join(" AND ", whereColumns.Select(c => $"{c} = ?"))}"
            };
        }

        /// <summary>
        /// Generates a parameterized DELETE query with proper parameter placeholders for each database type.
        /// </summary>
        /// <param name="rdbms">The type of RDBMS.</param>
        /// <param name="tableName">The name of the table.</param>
        /// <param name="whereColumns">The columns for the WHERE clause.</param>
        /// <returns>A parameterized SQL DELETE query.</returns>
        public static string GenerateParameterizedDeleteQuery(DataSourceType rdbms, string tableName, 
            IEnumerable<string> whereColumns)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Table name cannot be null or empty", nameof(tableName));

            if (whereColumns == null || !whereColumns.Any())
                throw new ArgumentException("No WHERE columns provided", nameof(whereColumns));

            return rdbms switch
            {
                DataSourceType.SqlServer => 
                    $"DELETE FROM {tableName} WHERE {string.Join(" AND ", whereColumns.Select(c => $"{c} = @{c}"))}",
                DataSourceType.Oracle => 
                    $"DELETE FROM {tableName} WHERE {string.Join(" AND ", whereColumns.Select(c => $"{c} = :{c}"))}",
                DataSourceType.Mysql or DataSourceType.Postgre or DataSourceType.SqlLite => 
                    $"DELETE FROM {tableName} WHERE {string.Join(" AND ", whereColumns.Select(c => $"{c} = ?"))}",
                _ => 
                    $"DELETE FROM {tableName} WHERE {string.Join(" AND ", whereColumns.Select(c => $"{c} = ?"))}"
            };
        }
    }
}