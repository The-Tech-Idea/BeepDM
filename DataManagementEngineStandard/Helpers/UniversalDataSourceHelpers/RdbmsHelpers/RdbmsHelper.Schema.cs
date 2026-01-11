using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Helpers.UniversalDataSourceHelpers.RdbmsHelpers
{
    /// <summary>
    /// Partial class for RdbmsHelper providing advanced DDL operations
    /// (ADD/ALTER/DROP/RENAME columns and tables).
    /// 
    /// These operations support schema evolution for RDBMS databases.
    /// </summary>
    public partial class RdbmsHelper
    {
        /// <summary>
        /// Generates SQL to add a new column to an existing table.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateAddColumnSql(string tableName, EntityField column)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tableName))
                    return ("", false, "Table name is required");

                if (column == null)
                    return ("", false, "Column definition is required");

                if (string.IsNullOrWhiteSpace(column.FieldName))
                    return ("", false, "Column name is required");

                var quotedTable = QuoteIdentifier(tableName);
                var quotedColumn = QuoteIdentifier(column.FieldName);
                var dataType = MapClrTypeToDatasourceType(Type.GetType($"System.{column.FieldType}") ?? typeof(string), 
                    column.Size, null, null);

                var nullConstraint = column.AllowNull ? "NULL" : "NOT NULL";
                var defaultClause = !string.IsNullOrEmpty(column.DefaultValue) ? $" DEFAULT {column.DefaultValue}" : "";

                var sql = $"ALTER TABLE {quotedTable} ADD {quotedColumn} {dataType} {nullConstraint}{defaultClause}";

                return (sql, true, "");
            }
            catch (Exception ex)
            {
                return ("", false, ex.Message);
            }
        }

        /// <summary>
        /// Generates SQL to modify an existing column definition.
        /// Note: ALTER COLUMN support varies by RDBMS (limited in MySQL, full in SQL Server/PostgreSQL).
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateAlterColumnSql(string tableName, string columnName, EntityField newColumn)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tableName))
                    return ("", false, "Table name is required");

                if (string.IsNullOrWhiteSpace(columnName))
                    return ("", false, "Current column name is required");

                if (newColumn == null)
                    return ("", false, "New column definition is required");

                var quotedTable = QuoteIdentifier(tableName);
                var quotedColumn = QuoteIdentifier(columnName);
                var newDataType = MapClrTypeToDatasourceType(
                    Type.GetType($"System.{newColumn.FieldType}") ?? typeof(string),
                    newColumn.Size, null, null);

                var nullConstraint = newColumn.AllowNull ? "NULL" : "NOT NULL";
                var defaultClause = !string.IsNullOrEmpty(newColumn.DefaultValue) ? $" DEFAULT {newColumn.DefaultValue}" : "";

                // SQL Server/T-SQL syntax
                var sql = $"ALTER TABLE {quotedTable} ALTER COLUMN {quotedColumn} {newDataType} {nullConstraint}{defaultClause}";

                // Note: PostgreSQL uses slightly different syntax:
                // ALTER TABLE table_name ALTER COLUMN column_name TYPE data_type;
                // MySQL uses CHANGE or MODIFY:
                // ALTER TABLE table_name MODIFY column_name data_type;

                return (sql, true, "");
            }
            catch (Exception ex)
            {
                return ("", false, ex.Message);
            }
        }

        /// <summary>
        /// Generates SQL to drop a column from a table.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateDropColumnSql(string tableName, string columnName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tableName))
                    return ("", false, "Table name is required");

                if (string.IsNullOrWhiteSpace(columnName))
                    return ("", false, "Column name is required");

                var quotedTable = QuoteIdentifier(tableName);
                var quotedColumn = QuoteIdentifier(columnName);

                var sql = $"ALTER TABLE {quotedTable} DROP COLUMN {quotedColumn}";

                return (sql, true, "");
            }
            catch (Exception ex)
            {
                return ("", false, ex.Message);
            }
        }

        /// <summary>
        /// Generates SQL to rename a table.
        /// Note: RENAME TABLE syntax differs by RDBMS.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateRenameTableSql(string oldTableName, string newTableName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(oldTableName))
                    return ("", false, "Old table name is required");

                if (string.IsNullOrWhiteSpace(newTableName))
                    return ("", false, "New table name is required");

                var quotedOldTable = QuoteIdentifier(oldTableName);
                var quotedNewTable = QuoteIdentifier(newTableName);

                // SQL Server syntax
                var sql = $"EXEC sp_rename '{oldTableName}', '{newTableName}'";

                // Note: Different RDBMS use different syntax:
                // MySQL: RENAME TABLE old_name TO new_name;
                // PostgreSQL: ALTER TABLE old_name RENAME TO new_name;
                // Oracle: RENAME old_name TO new_name;

                return (sql, true, "");
            }
            catch (Exception ex)
            {
                return ("", false, ex.Message);
            }
        }

        /// <summary>
        /// Generates SQL to rename a column in a table.
        /// Note: RENAME COLUMN syntax differs significantly by RDBMS.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateRenameColumnSql(string tableName, string oldColumnName, string newColumnName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tableName))
                    return ("", false, "Table name is required");

                if (string.IsNullOrWhiteSpace(oldColumnName))
                    return ("", false, "Old column name is required");

                if (string.IsNullOrWhiteSpace(newColumnName))
                    return ("", false, "New column name is required");

                var quotedTable = QuoteIdentifier(tableName);
                var quotedOldColumn = QuoteIdentifier(oldColumnName);
                var quotedNewColumn = QuoteIdentifier(newColumnName);

                // SQL Server syntax using sp_rename
                var sql = $"EXEC sp_rename '{tableName}.{oldColumnName}', '{newColumnName}', 'COLUMN'";

                // Note: Different RDBMS use different syntax:
                // MySQL: ALTER TABLE table_name CHANGE COLUMN old_name new_name data_type;
                // PostgreSQL: ALTER TABLE table_name RENAME COLUMN old_name TO new_name;
                // Oracle: ALTER TABLE table_name RENAME COLUMN old_name TO new_name;

                return (sql, true, "");
            }
            catch (Exception ex)
            {
                return ("", false, ex.Message);
            }
        }
    }
}
