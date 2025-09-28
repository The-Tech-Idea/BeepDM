using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Helpers.RDBMSHelpers.DMLHelpers
{
    /// <summary>
    /// Partial class containing legacy methods for backward compatibility.
    /// These methods are marked as obsolete and delegate to the new specialized helper classes.
    /// </summary>
    public static partial class DatabaseDMLHelper
    {
        #region Legacy Methods (Backward Compatibility)

        /// <summary>
        /// Generates value placeholders for parameterized queries.
        /// </summary>
        /// <param name="columnCount">Number of columns</param>
        /// <param name="batchSize">Number of records per batch</param>
        /// <returns>Placeholder string for values</returns>
        [Obsolete("This method is deprecated. Use DatabaseDMLSpecificHelpers.GenerateValuePlaceholders instead.", false)]
        public static string GenerateValuePlaceholders(int columnCount, int batchSize = 1)
        {
            return DatabaseDMLSpecificHelpers.GenerateValuePlaceholders(columnCount, batchSize);
        }

        /// <summary>
        /// Generates Oracle-specific INSERT ALL syntax for bulk operations.
        /// </summary>
        /// <param name="tableName">Name of the table</param>
        /// <param name="columnList">Comma-separated column list</param>
        /// <param name="batchSize">Number of records to insert</param>
        /// <returns>Oracle INSERT ALL syntax</returns>
        [Obsolete("This method is deprecated. Use DatabaseDMLSpecificHelpers.GenerateOracleInsertAll instead.", false)]
        public static string GenerateOracleInsertAll(string tableName, string columnList, int batchSize)
        {
            return DatabaseDMLSpecificHelpers.GenerateOracleInsertAll(tableName, columnList, batchSize);
        }

        /// <summary>
        /// Gets database-specific parameter prefix for parameterized queries.
        /// </summary>
        /// <param name="dataSourceType">Database type</param>
        /// <returns>Parameter prefix string</returns>
        [Obsolete("This method is deprecated. Use DatabaseDMLSpecificHelpers.GetParameterPrefix instead.", false)]
        public static string GetParameterPrefix(DataSourceType dataSourceType)
        {
            return DatabaseDMLSpecificHelpers.GetParameterPrefix(dataSourceType);
        }

        /// <summary>
        /// Validates table name to prevent SQL injection and ensure valid identifier format.
        /// </summary>
        /// <param name="tableName">Table name to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        [Obsolete("This method is deprecated. Use DatabaseDMLUtilities.IsValidTableName instead.", false)]
        public static bool IsValidTableName(string tableName)
        {
            return DatabaseDMLUtilities.IsValidTableName(tableName);
        }

        /// <summary>
        /// Validates column name to prevent SQL injection and ensure valid identifier format.
        /// </summary>
        /// <param name="columnName">Column name to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        [Obsolete("This method is deprecated. Use DatabaseDMLUtilities.IsValidColumnName instead.", false)]
        public static bool IsValidColumnName(string columnName)
        {
            return DatabaseDMLUtilities.IsValidColumnName(columnName);
        }

        /// <summary>
        /// Gets the appropriate quote character for identifiers based on database type.
        /// </summary>
        /// <param name="dataSourceType">Database type</param>
        /// <returns>Quote character for identifiers</returns>
        [Obsolete("This method is deprecated. Use DatabaseDMLUtilities.GetIdentifierQuoteChar instead.", false)]
        public static string GetIdentifierQuoteChar(DataSourceType dataSourceType)
        {
            return DatabaseDMLUtilities.GetIdentifierQuoteChar(dataSourceType);
        }

        /// <summary>
        /// Quotes an identifier if necessary based on database type and identifier content.
        /// </summary>
        /// <param name="identifier">The identifier to quote</param>
        /// <param name="dataSourceType">Database type</param>
        /// <returns>Quoted identifier if necessary</returns>
        [Obsolete("This method is deprecated. Use DatabaseDMLUtilities.QuoteIdentifierIfNeeded instead.", false)]
        public static string QuoteIdentifierIfNeeded(string identifier, DataSourceType dataSourceType)
        {
            return DatabaseDMLUtilities.QuoteIdentifierIfNeeded(identifier, dataSourceType);
        }

        /// <summary>
        /// Gets database-specific syntax for auto-increment/identity columns.
        /// </summary>
        /// <param name="dataSourceType">Database type</param>
        /// <returns>Auto-increment syntax</returns>
        [Obsolete("This method is deprecated. Use DatabaseDMLSpecificHelpers.GetAutoIncrementSyntax instead.", false)]
        public static string GetAutoIncrementSyntax(DataSourceType dataSourceType)
        {
            return DatabaseDMLSpecificHelpers.GetAutoIncrementSyntax(dataSourceType);
        }

        /// <summary>
        /// Gets database-specific syntax for current timestamp.
        /// </summary>
        /// <param name="dataSourceType">Database type</param>
        /// <returns>Current timestamp syntax</returns>
        [Obsolete("This method is deprecated. Use DatabaseDMLSpecificHelpers.GetCurrentTimestampSyntax instead.", false)]
        public static string GetCurrentTimestampSyntax(DataSourceType dataSourceType)
        {
            return DatabaseDMLSpecificHelpers.GetCurrentTimestampSyntax(dataSourceType);
        }

        #endregion
    }
}