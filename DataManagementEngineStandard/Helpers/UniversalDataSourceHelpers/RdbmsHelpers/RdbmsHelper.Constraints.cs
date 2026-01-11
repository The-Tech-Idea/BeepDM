using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Helpers.UniversalDataSourceHelpers.RdbmsHelpers
{
    /// <summary>
    /// Partial class for RdbmsHelper providing constraint and relationship DDL operations
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
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateAddForeignKeySql(
            string tableName,
            string[] columnNames,
            string referencedTableName,
            string[] referencedColumnNames)
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
                var fkName = $"FK_{tableName}_{referencedTableName}_{string.Join("_", columnNames)}";

                var sql = $@"
ALTER TABLE {quotedTable}
ADD CONSTRAINT {QuoteIdentifier(fkName)}
FOREIGN KEY ({quotedColumns})
REFERENCES {quotedRefTable} ({quotedRefColumns})
ON DELETE CASCADE
ON UPDATE CASCADE";

                return (sql.Trim(), true, "");
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

                // Use delegation to existing helper if available
                // Otherwise, generate SQL Server compatible query as default
                var query = RDBMSHelpers.DatabaseSchemaQueryHelper.GetPrimaryKeyQuery(SupportedType, tableName);

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

                var query = RDBMSHelpers.DatabaseSchemaQueryHelper.GetForeignKeysQuery(SupportedType, tableName);

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

                var query = RDBMSHelpers.DatabaseSchemaQueryHelper.GetConstraintsQuery(SupportedType, tableName);

                return (query, !string.IsNullOrEmpty(query), "");
            }
            catch (Exception ex)
            {
                return ("", false, ex.Message);
            }
        }
    }
}
