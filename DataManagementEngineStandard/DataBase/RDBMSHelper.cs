using System;
using System.Collections.Generic;
using System.Text;

namespace TheTechIdea.Beep.DataBase
{
    public static class RDBMSHelper
    {
        public static string GeneratePrimaryKeyQuery(string rdbms, string tableName, string primaryKey, string type)
        {
            string query = "";

            switch (rdbms.ToLower())
            {
                case "mssql":
                    query = $"ALTER TABLE {tableName} ADD {primaryKey} {type} PRIMARY KEY IDENTITY;";
                    break;
                case "mysql":
                    query = $"ALTER TABLE {tableName} ADD {primaryKey} {type} PRIMARY KEY AUTO_INCREMENT;";
                    break;
                case "postgresql":
                    query = $"ALTER TABLE {tableName} ADD {primaryKey} {type} PRIMARY KEY GENERATED ALWAYS AS IDENTITY;";
                    break;
                case "oracle":
                    query = $"ALTER TABLE {tableName} ADD {primaryKey} {type} GENERATED ALWAYS AS IDENTITY;";
                    break;
                default:
                    query = "RDBMS not supported.";
                    break;
            }

            return query;
        }
        public static string GenerateFetchIdentityQuery(string rdbms)
        {
            string query = "";

            switch (rdbms.ToLower())
            {
                case "mssql":
                    query = "SELECT SCOPE_IDENTITY();";
                    break;
                case "mysql":
                    query = "SELECT LAST_INSERT_ID();";
                    break;
                case "postgresql":
                    query = "SELECT LASTVAL();";
                    break;
                case "oracle":
                    query = "SELECT LAST_INSERT_ID() FROM dual;";
                    break;
                default:
                    query = "RDBMS not supported.";
                    break;
            }

            return query;
        }
        public static string GenerateDropPrimaryKeyQuery(string rdbms, string tableName, string constraintName)
        {
            string query = "";

            switch (rdbms.ToLower())
            {
                case "mssql":
                    query = $"ALTER TABLE {tableName} DROP CONSTRAINT {constraintName};";
                    break;
                case "mysql":
                    query = $"ALTER TABLE {tableName} DROP PRIMARY KEY;";
                    break;
                case "postgresql":
                    query = $"ALTER TABLE {tableName} DROP CONSTRAINT {constraintName};";
                    break;
                case "oracle":
                    query = $"ALTER TABLE {tableName} DROP PRIMARY KEY;";
                    break;
                default:
                    query = "RDBMS not supported.";
                    break;
            }

            return query;
        }
        public static string GenerateDropForeignKeyQuery(string rdbms, string tableName, string constraintName)
        {
            string query = "";

            switch (rdbms.ToLower())
            {
                case "mssql":
                    query = $"ALTER TABLE {tableName} NOCHECK CONSTRAINT {constraintName};";
                    break;
                case "mysql":
                case "postgresql":
                case "oracle":
                    query = $"ALTER TABLE {tableName} DROP FOREIGN KEY {constraintName};";
                    break;
                default:
                    query = "RDBMS not supported.";
                    break;
            }

            return query;
        }
        public static string GenerateDisableForeignKeyQuery(string rdbms, string tableName, string constraintName)
        {
            string query = "";

            switch (rdbms.ToLower())
            {
                case "mssql":
                    query = $"ALTER TABLE {tableName} NOCHECK CONSTRAINT {constraintName};";
                    break;
                case "oracle":
                    query = $"ALTER TABLE {tableName} DISABLE CONSTRAINT {constraintName};";
                    break;
                case "postgresql":
                    query = $"ALTER TABLE {tableName} DROP CONSTRAINT {constraintName};";
                    break;
                case "mysql":
                    query = "SET FOREIGN_KEY_CHECKS = 0;";
                    break;
                default:
                    query = "RDBMS not supported.";
                    break;
            }

            return query;
        }
        public static string GenerateEnableForeignKeyQuery(string rdbms, string tableName, string constraintName)
        {
            string query = "";

            switch (rdbms.ToLower())
            {
                case "mssql":
                    query = $"ALTER TABLE {tableName} WITH CHECK CHECK CONSTRAINT {constraintName};";
                    break;
                case "oracle":
                    query = $"ALTER TABLE {tableName} ENABLE CONSTRAINT {constraintName};";
                    break;
                case "postgresql":
                    // You have to recreate the constraint. Assuming it's a foreign key to "referencedTable" on "referencedColumn".
                    query = $"ALTER TABLE {tableName} ADD CONSTRAINT {constraintName} FOREIGN KEY (columnName) REFERENCES referencedTable(referencedColumn);";
                    break;
                case "mysql":
                    query = "SET FOREIGN_KEY_CHECKS = 1;";
                    break;
                default:
                    query = "RDBMS not supported.";
                    break;
            }

            return query;
        }

    }
}
