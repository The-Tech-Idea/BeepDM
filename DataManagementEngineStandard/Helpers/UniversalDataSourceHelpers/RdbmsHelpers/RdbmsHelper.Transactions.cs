using System;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Helpers.UniversalDataSourceHelpers.RdbmsHelpers
{
    /// <summary>
    /// Partial class for RdbmsHelper providing transaction control operations.
    /// 
    /// Supports BEGIN/COMMIT/ROLLBACK for ACID compliance across RDBMS databases.
    /// Syntax varies by RDBMS (SQL Server uses BEGIN TRANSACTION, MySQL uses START TRANSACTION).
    /// </summary>
    public partial class RdbmsHelper
    {
        /// <summary>
        /// Generates SQL to begin a transaction.
        /// Syntax varies by RDBMS:
        /// - SQL Server: BEGIN TRANSACTION
        /// - MySQL/PostgreSQL/SQLite/Firebird: BEGIN TRANSACTION or START TRANSACTION
        /// - Oracle: implicit (transactions start with first DML statement)
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateBeginTransactionSql()
        {
            try
            {
                // Standard SQL syntax works across most RDBMS
                var sql = "BEGIN TRANSACTION";

                // Alternative syntax for maximum compatibility:
                // - MySQL: START TRANSACTION (also accepts BEGIN, but not standard)
                // - PostgreSQL: BEGIN (in standard SQL mode)
                // - SQLite: BEGIN TRANSACTION / BEGIN IMMEDIATE / BEGIN EXCLUSIVE

                return (sql, true, "");
            }
            catch (Exception ex)
            {
                return ("", false, ex.Message);
            }
        }

        /// <summary>
        /// Generates SQL to commit a transaction.
        /// Standard SQL syntax: COMMIT
        /// Also supported: COMMIT TRANSACTION (SQL Server), COMMIT WORK (PostgreSQL)
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateCommitSql()
        {
            try
            {
                var sql = "COMMIT";

                // Extended syntax options per RDBMS:
                // - SQL Server: COMMIT TRANSACTION [transaction_name]
                // - PostgreSQL: COMMIT or COMMIT TRANSACTION or COMMIT WORK
                // - MySQL: COMMIT
                // - Oracle: COMMIT

                return (sql, true, "");
            }
            catch (Exception ex)
            {
                return ("", false, ex.Message);
            }
        }

        /// <summary>
        /// Generates SQL to rollback (abort) a transaction.
        /// Standard SQL syntax: ROLLBACK
        /// Also supported: ROLLBACK TRANSACTION (SQL Server), ROLLBACK TO SAVEPOINT (PostgreSQL)
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateRollbackSql()
        {
            try
            {
                var sql = "ROLLBACK";

                // Extended syntax options per RDBMS:
                // - SQL Server: ROLLBACK TRANSACTION [transaction_name]
                // - PostgreSQL: ROLLBACK or ROLLBACK TRANSACTION or ROLLBACK TO SAVEPOINT
                // - MySQL: ROLLBACK
                // - Oracle: ROLLBACK

                return (sql, true, "");
            }
            catch (Exception ex)
            {
                return ("", false, ex.Message);
            }
        }

        /// <summary>
        /// Generates SQL to create a savepoint within a transaction.
        /// Useful for partial rollback in complex transaction scenarios.
        /// Syntax: SAVEPOINT savepoint_name (PostgreSQL, Oracle, MySQL 5.7+)
        /// SQL Server uses: SAVE TRANSACTION savepoint_name
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateSavepointSql(string savepointName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(savepointName))
                    return ("", false, "Savepoint name is required");

                // Standard SQL syntax (PostgreSQL, Oracle, MySQL 5.7+)
                var sql = $"SAVEPOINT {savepointName}";

                // SQL Server uses: SAVE TRANSACTION savepointName

                return (sql, true, "");
            }
            catch (Exception ex)
            {
                return ("", false, ex.Message);
            }
        }

        /// <summary>
        /// Generates SQL to rollback to a specific savepoint.
        /// Useful for undoing part of a transaction without committing or rolling back entirely.
        /// Syntax: ROLLBACK TO SAVEPOINT savepoint_name (PostgreSQL, Oracle)
        /// SQL Server uses: ROLLBACK TRANSACTION savepoint_name
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateRollbackToSavepointSql(string savepointName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(savepointName))
                    return ("", false, "Savepoint name is required");

                // Standard SQL syntax (PostgreSQL, Oracle, MySQL)
                var sql = $"ROLLBACK TO SAVEPOINT {savepointName}";

                // SQL Server uses: ROLLBACK TRANSACTION savepointName

                return (sql, true, "");
            }
            catch (Exception ex)
            {
                return ("", false, ex.Message);
            }
        }

        /// <summary>
        /// Gets the current transaction isolation level for the RDBMS.
        /// Returns SQL query to retrieve isolation level information.
        /// Different RDBMS store this in different system views/functions.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GetTransactionIsolationLevelQuery()
        {
            try
            {
                // SQL Server: SELECT CASE ... WHEN transaction_isolation_level = 0 THEN 'Unspecified'
                // PostgreSQL: SHOW transaction_isolation;
                // MySQL: SELECT @@transaction_isolation;
                // Oracle: SELECT * FROM v$transaction;

                // Return SQL Server version as default (most common)
                var sql = @"SELECT 
                    CASE WHEN transaction_isolation_level = 0 THEN 'Unspecified'
                         WHEN transaction_isolation_level = 1 THEN 'ReadUncommitted'
                         WHEN transaction_isolation_level = 2 THEN 'ReadCommitted'
                         WHEN transaction_isolation_level = 3 THEN 'Repeatable'
                         WHEN transaction_isolation_level = 4 THEN 'Serializable'
                         ELSE 'Unknown'
                    END AS IsolationLevel
                FROM sys.dm_exec_sessions
                WHERE session_id = @@SPID";

                return (sql, true, "");
            }
            catch (Exception ex)
            {
                return ("", false, ex.Message);
            }
        }

        /// <summary>
        /// Generates SQL to set the transaction isolation level.
        /// Controls how transactions interact with concurrent access.
        /// Levels: ReadUncommitted < ReadCommitted < RepeatableRead < Serializable
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateSetTransactionIsolationLevelSql(string isolationLevel)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(isolationLevel))
                    return ("", false, "Isolation level is required");

                // SQL Server syntax: SET TRANSACTION ISOLATION LEVEL { SERIALIZABLE | REPEATABLE READ | READ COMMITTED | READ UNCOMMITTED }
                var validLevels = new[] { "SERIALIZABLE", "REPEATABLE READ", "READ COMMITTED", "READ UNCOMMITTED" };
                
                var normalizedLevel = isolationLevel.ToUpper();
                var sql = $"SET TRANSACTION ISOLATION LEVEL {normalizedLevel}";

                return (sql, true, "");
            }
            catch (Exception ex)
            {
                return ("", false, ex.Message);
            }
        }
    }
}
