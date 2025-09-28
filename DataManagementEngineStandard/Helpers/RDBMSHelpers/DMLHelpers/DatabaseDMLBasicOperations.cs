using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Helpers.RDBMSHelpers.DMLHelpers
{
    /// <summary>
    /// Helper class for basic DML operations (INSERT, UPDATE, DELETE) across different database types.
    /// </summary>
    public static class DatabaseDMLBasicOperations
    {
        /// <summary>
        /// Generates a SQL query to insert data into a table in a specific RDBMS.
        /// </summary>
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

            switch (rdbms)
            {
                case DataSourceType.SqlServer:
                    return $"INSERT INTO {tableName} ({string.Join(", ", data.Keys)}) VALUES ({string.Join(", ", data.Keys.Select(k => "@" + k))})";
                case DataSourceType.Mysql:
                    return $"INSERT INTO {tableName} ({string.Join(", ", data.Keys)}) VALUES ({string.Join(", ", data.Values.Select(v => "'" + v.ToString().Replace("'", "''") + "'"))})";
                case DataSourceType.Postgre:
                    return $"INSERT INTO {tableName} ({string.Join(", ", data.Keys)}) VALUES ({string.Join(", ", data.Values.Select(v => "'" + v.ToString().Replace("'", "''") + "'"))})";
                case DataSourceType.Oracle:
                    return $"INSERT INTO {tableName} ({string.Join(", ", data.Keys)}) VALUES ({string.Join(", ", data.Values.Select(v => "'" + v.ToString().Replace("'", "''") + "'"))})";
                case DataSourceType.DB2:
                    return $"INSERT INTO {tableName} ({string.Join(", ", data.Keys)}) VALUES ({string.Join(", ", data.Values.Select(v => "'" + v.ToString().Replace("'", "''") + "'"))})";
                case DataSourceType.FireBird:
                    return $"INSERT INTO {tableName} ({string.Join(", ", data.Keys)}) VALUES ({string.Join(", ", data.Values.Select(v => "'" + v.ToString().Replace("'", "''") + "'"))})";
                case DataSourceType.SqlLite:
                    return $"INSERT INTO {tableName} ({string.Join(", ", data.Keys)}) VALUES ({string.Join(", ", data.Values.Select(v => "'" + v.ToString().Replace("'", "''") + "'"))})";
                case DataSourceType.Couchbase:
                case DataSourceType.Redis:
                case DataSourceType.MongoDB:
                    return "NoSQL databases typically do not use standard SQL INSERT statements.";
                default:
                    return "RDBMS not supported.";
            }
        }

        /// <summary>
        /// Generates a SQL query to update data in a table in a specific RDBMS.
        /// </summary>
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

            switch (rdbms)
            {
                case DataSourceType.SqlServer:
                    return $"UPDATE {tableName} SET {string.Join(", ", data.Keys.Select(k => $"{k} = @{k}"))} WHERE {string.Join(" AND ", conditions.Keys.Select(k => $"{k} = @{k}"))}";
                case DataSourceType.Mysql:
                    return $"UPDATE {tableName} SET {string.Join(", ", data.Keys.Select(k => $"{k} = '{data[k].ToString().Replace("'", "''")}'"))} WHERE {string.Join(" AND ", conditions.Keys.Select(k => $"{k} = '{conditions[k].ToString().Replace("'", "''")}'"))}";
                case DataSourceType.Postgre:
                    return $"UPDATE {tableName} SET {string.Join(", ", data.Keys.Select(k => $"{k} = '{data[k].ToString().Replace("'", "''")}'"))} WHERE {string.Join(" AND ", conditions.Keys.Select(k => $"{k} = '{conditions[k].ToString().Replace("'", "''")}'"))}";
                case DataSourceType.Oracle:
                    return $"UPDATE {tableName} SET {string.Join(", ", data.Keys.Select(k => $"{k} = '{data[k].ToString().Replace("'", "''")}'"))} WHERE {string.Join(" AND ", conditions.Keys.Select(k => $"{k} = '{conditions[k].ToString().Replace("'", "''")}'"))}";
                case DataSourceType.DB2:
                    return $"UPDATE {tableName} SET {string.Join(", ", data.Keys.Select(k => $"{k} = '{data[k].ToString().Replace("'", "''")}'"))} WHERE {string.Join(" AND ", conditions.Keys.Select(k => $"{k} = '{conditions[k].ToString().Replace("'", "''")}'"))}";
                case DataSourceType.FireBird:
                    return $"UPDATE {tableName} SET {string.Join(", ", data.Keys.Select(k => $"{k} = '{data[k].ToString().Replace("'", "''")}'"))} WHERE {string.Join(" AND ", conditions.Keys.Select(k => $"{k} = '{conditions[k].ToString().Replace("'", "''")}'"))}";
                case DataSourceType.SqlLite:
                    return $"UPDATE {tableName} SET {string.Join(", ", data.Keys.Select(k => $"{k} = '{data[k].ToString().Replace("'", "''")}'"))} WHERE {string.Join(" AND ", conditions.Keys.Select(k => $"{k} = '{conditions[k].ToString().Replace("'", "''")}'"))}";
                case DataSourceType.Couchbase:
                case DataSourceType.Redis:
                case DataSourceType.MongoDB:
                    return "NoSQL databases typically do not use standard SQL UPDATE statements.";
                default:
                    return "RDBMS not supported.";
            }
        }

        /// <summary>
        /// Generates a SQL query to delete data from a table in a specific RDBMS.
        /// </summary>
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

            switch (rdbms)
            {
                case DataSourceType.SqlServer:
                    return $"DELETE FROM {tableName} WHERE {string.Join(" AND ", conditions.Keys.Select(k => $"{k} = @{k}"))}";
                case DataSourceType.Mysql:
                    return $"DELETE FROM {tableName} WHERE {string.Join(" AND ", conditions.Keys.Select(k => $"{k} = '{conditions[k].ToString().Replace("'", "''")}'"))}";
                case DataSourceType.Postgre:
                    return $"DELETE FROM {tableName} WHERE {string.Join(" AND ", conditions.Keys.Select(k => $"{k} = '{conditions[k].ToString().Replace("'", "''")}'"))}";
                case DataSourceType.Oracle:
                    return $"DELETE FROM {tableName} WHERE {string.Join(" AND ", conditions.Keys.Select(k => $"{k} = '{conditions[k].ToString().Replace("'", "''")}'"))}";
                case DataSourceType.DB2:
                    return $"DELETE FROM {tableName} WHERE {string.Join(" AND ", conditions.Keys.Select(k => $"{k} = '{conditions[k].ToString().Replace("'", "''")}'"))}";
                case DataSourceType.FireBird:
                    return $"DELETE FROM {tableName} WHERE {string.Join(" AND ", conditions.Keys.Select(k => $"{k} = '{conditions[k].ToString().Replace("'", "''")}'"))}";
                case DataSourceType.SqlLite:
                    return $"DELETE FROM {tableName} WHERE {string.Join(" AND ", conditions.Keys.Select(k => $"{k} = '{conditions[k].ToString().Replace("'", "''")}'"))}";
                case DataSourceType.Couchbase:
                case DataSourceType.Redis:
                case DataSourceType.MongoDB:
                    return "NoSQL databases typically do not use standard SQL DELETE statements.";
                default:
                    return "RDBMS not supported.";
            }
        }
    }
}