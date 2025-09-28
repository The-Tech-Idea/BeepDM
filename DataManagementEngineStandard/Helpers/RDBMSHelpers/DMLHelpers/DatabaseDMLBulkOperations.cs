using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Helpers.RDBMSHelpers.DMLHelpers
{
    /// <summary>
    /// Helper class for bulk database operations with optimized performance.
    /// </summary>
    public static class DatabaseDMLBulkOperations
    {
        /// <summary>
        /// Generates bulk insert SQL for multiple records with optimized performance.
        /// </summary>
        /// <param name="dataSourceType">Database type</param>
        /// <param name="tableName">Name of the table</param>
        /// <param name="columns">Column names</param>
        /// <param name="batchSize">Number of records per batch (default: 1000)</param>
        /// <returns>Optimized bulk insert SQL</returns>
        public static string GenerateBulkInsertQuery(DataSourceType dataSourceType, string tableName, 
            IEnumerable<string> columns, int batchSize = 1000)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Table name cannot be null or empty", nameof(tableName));

            if (columns == null || !columns.Any())
                throw new ArgumentException("No columns provided for bulk insert", nameof(columns));

            var columnList = string.Join(", ", columns);
            
            return dataSourceType switch
            {
                DataSourceType.SqlServer => GenerateSqlServerBulkInsert(tableName, columnList, batchSize),
                DataSourceType.Mysql => GenerateMySqlBulkInsert(tableName, columnList, batchSize),
                DataSourceType.Postgre => GeneratePostgresBulkInsert(tableName, columnList, batchSize),
                DataSourceType.Oracle => GenerateOracleBulkInsert(tableName, columnList, batchSize),
                DataSourceType.SqlLite => GenerateSqliteBulkInsert(tableName, columnList, batchSize),
                _ => $"INSERT INTO {tableName} ({columnList}) VALUES {DatabaseDMLSpecificHelpers.GenerateValuePlaceholders(columns.Count())}"
            };
        }

        /// <summary>
        /// Generates an UPSERT (INSERT or UPDATE) query for database-specific merge operations.
        /// </summary>
        /// <param name="dataSourceType">Database type</param>
        /// <param name="tableName">Name of the table</param>
        /// <param name="keyColumns">Key columns for matching</param>
        /// <param name="updateColumns">Columns to update</param>
        /// <param name="insertColumns">Columns for insert</param>
        /// <returns>Database-specific UPSERT query</returns>
        public static string GenerateUpsertQuery(DataSourceType dataSourceType, string tableName,
            IEnumerable<string> keyColumns, IEnumerable<string> updateColumns, IEnumerable<string> insertColumns)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Table name cannot be null or empty", nameof(tableName));

            var keys = keyColumns?.ToList() ?? throw new ArgumentException("Key columns cannot be null", nameof(keyColumns));
            var updates = updateColumns?.ToList() ?? throw new ArgumentException("Update columns cannot be null", nameof(updateColumns));
            var inserts = insertColumns?.ToList() ?? throw new ArgumentException("Insert columns cannot be null", nameof(insertColumns));

            return dataSourceType switch
            {
                DataSourceType.SqlServer => GenerateSqlServerMerge(tableName, keys, updates, inserts),
                DataSourceType.Mysql => GenerateMySqlUpsert(tableName, keys, updates, inserts),
                DataSourceType.Postgre => GeneratePostgresUpsert(tableName, keys, updates, inserts),
                DataSourceType.Oracle => GenerateOracleMerge(tableName, keys, updates, inserts),
                DataSourceType.SqlLite => GenerateSqliteUpsert(tableName, keys, updates, inserts),
                _ => throw new NotSupportedException($"UPSERT not supported for {dataSourceType}")
            };
        }

        /// <summary>
        /// Generates bulk delete query with IN clause optimization.
        /// </summary>
        /// <param name="dataSourceType">Database type</param>
        /// <param name="tableName">Name of the table</param>
        /// <param name="keyColumn">Key column for deletion</param>
        /// <param name="batchSize">Number of IDs per batch</param>
        /// <returns>Optimized bulk delete query</returns>
        public static string GenerateBulkDeleteQuery(DataSourceType dataSourceType, string tableName, 
            string keyColumn, int batchSize = 1000)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Table name cannot be null or empty", nameof(tableName));

            if (string.IsNullOrWhiteSpace(keyColumn))
                throw new ArgumentException("Key column cannot be null or empty", nameof(keyColumn));

            var placeholders = string.Join(", ", Enumerable.Range(0, batchSize).Select(i => "?"));

            return dataSourceType switch
            {
                DataSourceType.SqlServer => $"DELETE FROM {tableName} WHERE {keyColumn} IN ({placeholders})",
                DataSourceType.Mysql => $"DELETE FROM {tableName} WHERE {keyColumn} IN ({placeholders})",
                DataSourceType.Postgre => $"DELETE FROM {tableName} WHERE {keyColumn} = ANY($1)",
                DataSourceType.Oracle => $"DELETE FROM {tableName} WHERE {keyColumn} IN ({placeholders})",
                DataSourceType.SqlLite => $"DELETE FROM {tableName} WHERE {keyColumn} IN ({placeholders})",
                _ => $"DELETE FROM {tableName} WHERE {keyColumn} IN ({placeholders})"
            };
        }

        #region Private Database-Specific Helpers

        private static string GenerateSqlServerBulkInsert(string tableName, string columnList, int batchSize)
        {
            return $"INSERT INTO {tableName} ({columnList}) VALUES {DatabaseDMLSpecificHelpers.GenerateValuePlaceholders(columnList.Split(',').Length, batchSize)}";
        }

        private static string GenerateMySqlBulkInsert(string tableName, string columnList, int batchSize)
        {
            return $"INSERT INTO {tableName} ({columnList}) VALUES {DatabaseDMLSpecificHelpers.GenerateValuePlaceholders(columnList.Split(',').Length, batchSize)}";
        }

        private static string GeneratePostgresBulkInsert(string tableName, string columnList, int batchSize)
        {
            return $"INSERT INTO {tableName} ({columnList}) VALUES {DatabaseDMLSpecificHelpers.GenerateValuePlaceholders(columnList.Split(',').Length, batchSize)}";
        }

        private static string GenerateOracleBulkInsert(string tableName, string columnList, int batchSize)
        {
            return $"INSERT ALL {DatabaseDMLSpecificHelpers.GenerateOracleInsertAll(tableName, columnList, batchSize)} SELECT * FROM dual";
        }

        private static string GenerateSqliteBulkInsert(string tableName, string columnList, int batchSize)
        {
            return $"INSERT INTO {tableName} ({columnList}) VALUES {DatabaseDMLSpecificHelpers.GenerateValuePlaceholders(columnList.Split(',').Length, batchSize)}";
        }

        // UPSERT/MERGE implementations for different databases
        private static string GenerateSqlServerMerge(string tableName, List<string> keys, List<string> updates, List<string> inserts)
        {
            var keyConditions = string.Join(" AND ", keys.Select(k => $"target.{k} = source.{k}"));
            var updateSets = string.Join(", ", updates.Select(u => $"{u} = source.{u}"));
            var insertColumns = string.Join(", ", inserts);
            var insertValues = string.Join(", ", inserts.Select(i => $"source.{i}"));

            return $@"MERGE {tableName} AS target
                     USING (VALUES ({string.Join(", ", inserts.Select(i => "?"))})) AS source ({insertColumns})
                     ON {keyConditions}
                     WHEN MATCHED THEN UPDATE SET {updateSets}
                     WHEN NOT MATCHED THEN INSERT ({insertColumns}) VALUES ({insertValues});";
        }

        private static string GenerateMySqlUpsert(string tableName, List<string> keys, List<string> updates, List<string> inserts)
        {
            var columnList = string.Join(", ", inserts);
            var valueList = string.Join(", ", inserts.Select(i => "?"));
            var updateSets = string.Join(", ", updates.Select(u => $"{u} = VALUES({u})"));

            return $"INSERT INTO {tableName} ({columnList}) VALUES ({valueList}) ON DUPLICATE KEY UPDATE {updateSets}";
        }

        private static string GeneratePostgresUpsert(string tableName, List<string> keys, List<string> updates, List<string> inserts)
        {
            var columnList = string.Join(", ", inserts);
            var valueList = string.Join(", ", inserts.Select(i => "?"));
            var conflictColumns = string.Join(", ", keys);
            var updateSets = string.Join(", ", updates.Select(u => $"{u} = EXCLUDED.{u}"));

            return $"INSERT INTO {tableName} ({columnList}) VALUES ({valueList}) ON CONFLICT ({conflictColumns}) DO UPDATE SET {updateSets}";
        }

        private static string GenerateOracleMerge(string tableName, List<string> keys, List<string> updates, List<string> inserts)
        {
            var keyConditions = string.Join(" AND ", keys.Select(k => $"target.{k} = source.{k}"));
            var updateSets = string.Join(", ", updates.Select(u => $"{u} = source.{u}"));
            var insertColumns = string.Join(", ", inserts);
            var insertValues = string.Join(", ", inserts.Select(i => $"source.{i}"));

            return $@"MERGE INTO {tableName} target
                     USING (SELECT {string.Join(", ", inserts.Select((i, idx) => $":{idx + 1} AS {i}"))} FROM dual) source
                     ON ({keyConditions})
                     WHEN MATCHED THEN UPDATE SET {updateSets}
                     WHEN NOT MATCHED THEN INSERT ({insertColumns}) VALUES ({insertValues})";
        }

        private static string GenerateSqliteUpsert(string tableName, List<string> keys, List<string> updates, List<string> inserts)
        {
            var columnList = string.Join(", ", inserts);
            var valueList = string.Join(", ", inserts.Select(i => "?"));
            var conflictColumns = string.Join(", ", keys);
            var updateSets = string.Join(", ", updates.Select(u => $"{u} = excluded.{u}"));

            return $"INSERT INTO {tableName} ({columnList}) VALUES ({valueList}) ON CONFLICT ({conflictColumns}) DO UPDATE SET {updateSets}";
        }

        #endregion
    }
}