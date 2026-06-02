using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Helpers.RDBMSHelpers;

namespace TheTechIdea.Beep.Helpers.UniversalDataSourceHelpers.RdbmsHelpers
{
    /// <summary>
    /// Partial class for RdbmsHelper providing constraint and relationship Ddl operations
    /// (PRIMARY KEY, FOREIGN KEY, UNIQUE, CHECK constraints).
    /// 
    /// Each method generates database-agnostic SQL, then adapts for specific RDBMS syntax.
    /// </summary>
    public partial class RdbmsHelper
    {
        /// <summary>
        /// Generates SQL to add a primary key constraint to a table.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateAddPrimaryKeySql(string tableName, params string[] columnNames)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tableName))
                    return ("", false, "Table name is required");

                if (columnNames == null || columnNames.Length == 0)
                    return ("", false, "At least one column must be specified for primary key");

                var quotedTable = QuoteIdentifier(tableName);
                var quotedColumns = string.Join(", ", columnNames.Select(c => QuoteIdentifier(c)));
                var pkName = $"PK_{tableName}_{string.Join("_", columnNames)}";

                var sql = $"ALTER TABLE {quotedTable} ADD CONSTRAINT {QuoteIdentifier(pkName)} PRIMARY KEY ({quotedColumns})";

                return (sql, true, "");
            }
            catch (Exception ex)
            {
                return ("", false, ex.Message);
            }
        }

        /// <summary>
        /// Generates SQL to add a foreign key constraint between tables.
        /// Defaults to ON DELETE CASCADE / ON UPDATE CASCADE for backward compatibility.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateAddForeignKeySql(
            string tableName,
            string[] columnNames,
            string referencedTableName,
            string[] referencedColumnNames)
        {
            return GenerateAddForeignKeySql(tableName, columnNames, referencedTableName, referencedColumnNames,
                onDeleteBehavior: "Cascade", onUpdateBehavior: "Cascade", constraintName: null);
        }

        /// <summary>
        /// Generates SQL to add a foreign key constraint between tables with explicit
        /// ON DELETE / ON UPDATE referential actions and an optional constraint name.
        /// </summary>
        /// <param name="tableName">Dependent table name.</param>
        /// <param name="columnNames">Foreign-key column(s) on the dependent table.</param>
        /// <param name="referencedTableName">Referenced (principal) table name.</param>
        /// <param name="referencedColumnNames">Referenced column(s) on the principal table.</param>
        /// <param name="onDeleteBehavior">
        /// One of: "Cascade", "Restrict", "SetNull", "NoAction". Defaults to "Cascade".
        /// </param>
        /// <param name="onUpdateBehavior">
        /// One of: "Cascade", "Restrict", "SetNull", "NoAction". Defaults to "Cascade".
        /// </param>
        /// <param name="constraintName">
        /// Optional explicit constraint name. When null/empty, a default of
        /// FK_{table}_{refTable}_{columns} is generated.
        /// </param>
        public (string Sql, bool Success, string ErrorMessage) GenerateAddForeignKeySql(
            string tableName,
            string[] columnNames,
            string referencedTableName,
            string[] referencedColumnNames,
            string onDeleteBehavior,
            string onUpdateBehavior,
            string constraintName = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tableName))
                    return ("", false, "Table name is required");

                if (columnNames == null || columnNames.Length == 0)
                    return ("", false, "At least one column must be specified for foreign key");

                if (string.IsNullOrWhiteSpace(referencedTableName))
                    return ("", false, "Referenced table name is required");

                if (referencedColumnNames == null || referencedColumnNames.Length == 0)
                    return ("", false, "At least one referenced column must be specified");

                if (columnNames.Length != referencedColumnNames.Length)
                    return ("", false, "Number of columns must match between foreign and referenced tables");

                var quotedTable = QuoteIdentifier(tableName);
                var quotedRefTable = QuoteIdentifier(referencedTableName);
                var quotedColumns = string.Join(", ", columnNames.Select(c => QuoteIdentifier(c)));
                var quotedRefColumns = string.Join(", ", referencedColumnNames.Select(c => QuoteIdentifier(c)));
                var fkName = string.IsNullOrWhiteSpace(constraintName)
                    ? $"FK_{tableName}_{referencedTableName}_{string.Join("_", columnNames)}"
                    : constraintName;

                var onDelete = NormalizeReferentialAction(onDeleteBehavior, "Cascade");
                var onUpdate = NormalizeReferentialAction(onUpdateBehavior, "Cascade");

                var sql = $@"
ALTER TABLE {quotedTable}
ADD CONSTRAINT {QuoteIdentifier(fkName)}
FOREIGN KEY ({quotedColumns})
REFERENCES {quotedRefTable} ({quotedRefColumns})
ON DELETE {onDelete}
ON UPDATE {onUpdate}";

                return (sql.Trim(), true, "");
            }
            catch (Exception ex)
            {
                return ("", false, ex.Message);
            }
        }

        /// <summary>
        /// Normalizes a referential-action string (e.g. "Cascade", "SetNull") into
        /// the SQL keyword form ("CASCADE", "SET NULL"). Returns the supplied default
        /// when the input is null/whitespace or unrecognized.
        /// </summary>
        private static string NormalizeReferentialAction(string action, string defaultAction)
        {
            if (string.IsNullOrWhiteSpace(action))
                action = defaultAction;

            switch (action.Trim().Replace("_", "").Replace("-", "").ToLowerInvariant())
            {
                case "cascade": return "CASCADE";
                case "restrict": return "RESTRICT";
                case "setnull": return "SET NULL";
                case "noaction": return "NO ACTION";
                default: return defaultAction.Equals("Cascade", StringComparison.OrdinalIgnoreCase) ? "CASCADE" : "NO ACTION";
            }
        }

        /// <summary>
        /// Generates SQL to drop a foreign key constraint.
        /// When <paramref name="constraintName"/> is null/empty, a default name of
        /// FK_{table}_{refTable}_{columns} is assumed.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateDropForeignKeySql(
            string tableName,
            string constraintName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tableName))
                    return ("", false, "Table name is required");

                if (string.IsNullOrWhiteSpace(constraintName))
                    return ("", false, "Constraint name is required");

                var quotedTable = QuoteIdentifier(tableName);
                var quotedConstraint = QuoteIdentifier(constraintName);
                var sql = $"ALTER TABLE {quotedTable} DROP CONSTRAINT {quotedConstraint}";
                return (sql, true, "");
            }
            catch (Exception ex)
            {
                return ("", false, ex.Message);
            }
        }

        /// <summary>
        /// Generates SQL to drop an index. The default form is a portable
        /// <c>DROP INDEX &lt;name&gt;</c> statement — most RDBMS engines accept it
        /// directly. Providers that require <c>DROP INDEX &lt;name&gt; ON &lt;table&gt;</c>
        /// (e.g. MySQL/MariaDB) can be extended later by routing through a
        /// <see cref="DataSourceType"/> switch.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateDropIndexSql(
            string tableName,
            string indexName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(indexName))
                    return ("", false, "Index name is required");

                var quotedIndex = QuoteIdentifier(indexName);
                return ($"DROP INDEX {quotedIndex}", true, "");
            }
            catch (Exception ex)
            {
                return ("", false, ex.Message);
            }
        }

        /// <summary>
        /// Generates SQL to add a generic constraint (UNIQUE, CHECK, etc.).
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateAddConstraintSql(string tableName, string constraintName, string constraintDefinition)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tableName))
                    return ("", false, "Table name is required");

                if (string.IsNullOrWhiteSpace(constraintName))
                    return ("", false, "Constraint name is required");

                if (string.IsNullOrWhiteSpace(constraintDefinition))
                    return ("", false, "Constraint definition is required");

                var quotedTable = QuoteIdentifier(tableName);
                var quotedConstraintName = QuoteIdentifier(constraintName);

                var sql = $"ALTER TABLE {quotedTable} ADD CONSTRAINT {quotedConstraintName} {constraintDefinition}";

                return (sql, true, "");
            }
            catch (Exception ex)
            {
                return ("", false, ex.Message);
            }
        }

        /// <summary>
        /// Generates a query to retrieve primary key information for a table.
        /// Database-specific query for different RDBMS types.
        /// </summary>
        public (string Query, bool Success, string ErrorMessage) GetPrimaryKeyQuery(string tableName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tableName))
                    return ("", false, "Table name is required");

                // Generate database-specific query for primary key information
                var query = SupportedType switch
                {
                    DataSourceType.SqlServer => $"SELECT CONSTRAINT_NAME FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE TABLE_NAME = '{tableName}' AND CONSTRAINT_TYPE = 'PRIMARY KEY'",
                    DataSourceType.Mysql => $"SELECT CONSTRAINT_NAME FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = '{tableName}' AND CONSTRAINT_TYPE = 'PRIMARY KEY'",
                    DataSourceType.Postgre => $"SELECT constraint_name FROM information_schema.table_constraints WHERE table_name = '{tableName}' AND constraint_type = 'PRIMARY KEY'",
                    DataSourceType.Oracle => $"SELECT constraint_name FROM user_constraints WHERE table_name = UPPER('{tableName}') AND constraint_type = 'P'",
                    _ => ""
                };

                return (query, !string.IsNullOrEmpty(query), "");
            }
            catch (Exception ex)
            {
                return ("", false, ex.Message);
            }
        }

        /// <summary>
        /// Generates a query to retrieve foreign key information for a table.
        /// Database-specific query for different RDBMS types.
        /// </summary>
        public (string Query, bool Success, string ErrorMessage) GetForeignKeysQuery(string tableName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tableName))
                    return ("", false, "Table name is required");

                // Generate database-specific query for foreign key information
                var query = SupportedType switch
                {
                    DataSourceType.SqlServer => $"SELECT * FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS WHERE CONSTRAINT_TABLE_NAME = '{tableName}'",
                    DataSourceType.Mysql => $"SELECT * FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE TABLE_NAME = '{tableName}' AND REFERENCED_TABLE_NAME IS NOT NULL",
                    DataSourceType.Postgre => $"SELECT * FROM information_schema.table_constraints WHERE table_name = '{tableName}' AND constraint_type = 'FOREIGN KEY'",
                    DataSourceType.Oracle => $"SELECT * FROM user_constraints WHERE table_name = UPPER('{tableName}') AND constraint_type = 'R'",
                    _ => ""
                };

                return (query, !string.IsNullOrEmpty(query), "");
            }
            catch (Exception ex)
            {
                return ("", false, ex.Message);
            }
        }

        /// <summary>
        /// Generates a query to retrieve all constraints for a table.
        /// </summary>
        public (string Query, bool Success, string ErrorMessage) GetConstraintsQuery(string tableName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tableName))
                    return ("", false, "Table name is required");

                // Generate database-specific query for all constraints
                var query = SupportedType switch
                {
                    DataSourceType.SqlServer => $"SELECT * FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE TABLE_NAME = '{tableName}'",
                    DataSourceType.Mysql => $"SELECT * FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = '{tableName}'",
                    DataSourceType.Postgre => $"SELECT * FROM information_schema.table_constraints WHERE table_name = '{tableName}'",
                    DataSourceType.Oracle => $"SELECT * FROM user_constraints WHERE table_name = UPPER('{tableName}')",
                    _ => ""
                };

                return (query, !string.IsNullOrEmpty(query), "");
            }
            catch (Exception ex)
            {
                return ("", false, ex.Message);
            }
        }
    }
}
