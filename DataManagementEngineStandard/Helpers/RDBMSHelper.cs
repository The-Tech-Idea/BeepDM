using System;
using System.Collections.Generic;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.Helpers
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
                case "Mysql":
                    query = $"ALTER TABLE {tableName} ADD {primaryKey} {type} PRIMARY KEY AUTO_INCREMENT;";
                    break;
                case "Postgre":
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
                case "Mysql":
                    query = "SELECT LAST_INSERT_ID();";
                    break;
                case "Postgre":
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
                case "Mysql":
                    query = $"ALTER TABLE {tableName} DROP PRIMARY KEY;";
                    break;
                case "Postgre":
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
                case "Mysql":
                case "Postgre":
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
                case "Postgre":
                    query = $"ALTER TABLE {tableName} DROP CONSTRAINT {constraintName};";
                    break;
                case "Mysql":
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
                case "Postgre":
                    // You have to recreate the constraint. Assuming it's a foreign key to "referencedTable" on "referencedColumn".
                    query = $"ALTER TABLE {tableName} ADD CONSTRAINT {constraintName} FOREIGN KEY (columnName) REFERENCES referencedTable(referencedColumn);";
                    break;
                case "Mysql":
                    query = "SET FOREIGN_KEY_CHECKS = 1;";
                    break;
                default:
                    query = "RDBMS not supported.";
                    break;
            }

            return query;
        }
        public static List<QuerySqlRepo> CreateQuerySqlRepos()
        {
            return new List<QuerySqlRepo>
    {
        // Oracle
        new QuerySqlRepo(DataSourceType.Oracle, "SELECT * FROM {0} {2}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.Oracle, "SELECT TABLE_NAME FROM user_tables", Sqlcommandtype.getlistoftables),
        new QuerySqlRepo(DataSourceType.Oracle, "SELECT cols.column_name FROM all_constraints cons, all_cons_columns cols WHERE cols.table_name = '{0}' AND cons.constraint_type = 'P' AND cons.constraint_name = cols.constraint_name AND cons.owner = cols.owner", Sqlcommandtype.getPKforTable),
        new QuerySqlRepo(DataSourceType.Oracle, "SELECT a.constraint_name, a.column_name, a.table_name FROM all_cons_columns a JOIN all_constraints c ON a.constraint_name = c.constraint_name WHERE c.constraint_type = 'R' AND a.table_name = '{0}'", Sqlcommandtype.getFKforTable),
        new QuerySqlRepo(DataSourceType.Oracle, "SELECT table_name FROM all_constraints WHERE r_constraint_name IN (SELECT constraint_name FROM all_constraints WHERE table_name = '{0}' AND constraint_type = 'P')", Sqlcommandtype.getChildTable),
        new QuerySqlRepo(DataSourceType.Oracle, "SELECT r.table_name FROM all_constraints c JOIN all_constraints r ON c.r_constraint_name = r.constraint_name WHERE c.table_name = '{0}' AND c.constraint_type = 'R'", Sqlcommandtype.getParentTable),
        
        // SQL Server
        new QuerySqlRepo(DataSourceType.SqlServer, "SELECT * FROM {0} {2}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.SqlServer, "SELECT t.NAME FROM sys.tables t INNER JOIN sys.schemas s ON t.schema_id = s.schema_id", Sqlcommandtype.getlistoftables),
        new QuerySqlRepo(DataSourceType.SqlServer, "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE TABLE_NAME = '{0}' AND CONSTRAINT_NAME LIKE 'PK%'", Sqlcommandtype.getPKforTable),
        new QuerySqlRepo(DataSourceType.SqlServer, "SELECT FK.COLUMN_NAME, FK.TABLE_NAME FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE FK INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS TC ON FK.CONSTRAINT_NAME = TC.CONSTRAINT_NAME WHERE TC.CONSTRAINT_TYPE = 'FOREIGN KEY' AND FK.TABLE_NAME = '{0}'", Sqlcommandtype.getFKforTable),
        new QuerySqlRepo(DataSourceType.SqlServer, "SELECT FK.TABLE_NAME FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS RC INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE FK ON RC.CONSTRAINT_NAME = FK.CONSTRAINT_NAME WHERE RC.UNIQUE_CONSTRAINT_NAME = (SELECT CONSTRAINT_NAME FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE TABLE_NAME = '{0}' AND CONSTRAINT_TYPE = 'PRIMARY KEY')", Sqlcommandtype.getChildTable),
        new QuerySqlRepo(DataSourceType.SqlServer, "SELECT RC.UNIQUE_CONSTRAINT_TABLE_NAME FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS RC WHERE RC.CONSTRAINT_NAME IN (SELECT CONSTRAINT_NAME FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE TABLE_NAME = '{0}')", Sqlcommandtype.getParentTable),
           // MySQL
        new QuerySqlRepo(DataSourceType.Mysql, "SELECT * FROM {0} {2}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.Mysql, "SHOW TABLES", Sqlcommandtype.getlistoftables),
        new QuerySqlRepo(DataSourceType.Mysql, "SHOW KEYS FROM {0} WHERE Key_name = 'PRIMARY'", Sqlcommandtype.getPKforTable),
        new QuerySqlRepo(DataSourceType.Mysql, "SELECT COLUMN_NAME AS child_column, REFERENCED_COLUMN_NAME AS parent_column, REFERENCED_TABLE_NAME AS parent_table FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE TABLE_SCHEMA = 'YourSchema' AND TABLE_NAME = '{0}' AND REFERENCED_TABLE_NAME IS NOT NULL", Sqlcommandtype.getFKforTable),
        new QuerySqlRepo(DataSourceType.Mysql, "SELECT TABLE_NAME AS child_table FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE TABLE_SCHEMA = 'YourSchema' AND REFERENCED_TABLE_NAME = '{0}'", Sqlcommandtype.getChildTable),
        new QuerySqlRepo(DataSourceType.Mysql, "SELECT REFERENCED_TABLE_NAME AS parent_table FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE TABLE_SCHEMA = 'YourSchema' AND TABLE_NAME = '{0}' AND REFERENCED_TABLE_NAME IS NOT NULL", Sqlcommandtype.getParentTable),

        // (Additional MySQL queries for FK, Child Table, Parent Table)

        // PostgreSQL
        new QuerySqlRepo(DataSourceType.Postgre, "SELECT * FROM {0} {2}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.Postgre, "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'", Sqlcommandtype.getlistoftables),
        new QuerySqlRepo(DataSourceType.Postgre, "SELECT a.attname FROM pg_index i JOIN pg_attribute a ON a.attnum = ANY(i.indkey) WHERE i.indrelid = '{0}'::regclass AND i.indisprimary", Sqlcommandtype.getPKforTable),
        new QuerySqlRepo(DataSourceType.Postgre, "SELECT conname AS constraint_name, a.attname AS child_column, af.attname AS parent_column, cl.relname AS parent_table FROM pg_attribute a JOIN pg_attribute af ON a.attnum = ANY(pg_constraint.confkey) JOIN pg_class cl ON pg_constraint.confrelid = cl.oid JOIN pg_constraint ON a.attnum = ANY(pg_constraint.conkey) WHERE a.attnum > 0 AND pg_constraint.conrelid = '{0}'::regclass", Sqlcommandtype.getFKforTable),
        new QuerySqlRepo(DataSourceType.Postgre, "SELECT conname AS constraint_name, cl.relname AS child_table FROM pg_constraint JOIN pg_class cl ON pg_constraint.conrelid = cl.oid WHERE confrelid = '{0}'::regclass", Sqlcommandtype.getChildTable),
        new QuerySqlRepo(DataSourceType.Postgre, "SELECT confrelid::regclass AS parent_table FROM pg_constraint WHERE conrelid = '{0}'::regclass", Sqlcommandtype.getParentTable),


        // SQLite
        new QuerySqlRepo(DataSourceType.SqlLite, "SELECT * FROM {0} {2}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.SqlLite, "SELECT name table_name FROM sqlite_master WHERE type='table'", Sqlcommandtype.getlistoftables),
        new QuerySqlRepo(DataSourceType.SqlLite, "PRAGMA table_info({0})", Sqlcommandtype.getPKforTable),
        // (Additional SQLite queries for FK, Child Table, Parent Table)

       // DB2
        new QuerySqlRepo(DataSourceType.DB2, "SELECT * FROM {0} {2}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.DB2, "SELECT TABNAME FROM SYSCAT.TABLES WHERE TABSCHEMA = CURRENT SCHEMA", Sqlcommandtype.getlistoftables),
        new QuerySqlRepo(DataSourceType.DB2, "SELECT COLNAME COLUMN_NAME FROM SYSCAT.KEYCOLUSE WHERE TABNAME = '{0}' AND CONSTRAINTNAME LIKE 'PK%'", Sqlcommandtype.getPKforTable),
        new QuerySqlRepo(DataSourceType.DB2, "SELECT FK_COLNAMES AS child_column, PK_COLNAMES AS parent_column, PK_TBNAME AS parent_table FROM SYSIBM.SQLFOREIGNKEYS WHERE FK_TBNAME = '{0}'", Sqlcommandtype.getFKforTable),
        new QuerySqlRepo(DataSourceType.DB2, "SELECT FK_TBNAME AS child_table FROM SYSIBM.SQLFOREIGNKEYS WHERE PK_TBNAME = '{0}'", Sqlcommandtype.getChildTable),
        new QuerySqlRepo(DataSourceType.DB2, "SELECT PK_TBNAME AS parent_table FROM SYSIBM.SQLFOREIGNKEYS WHERE FK_TBNAME = '{0}'", Sqlcommandtype.getParentTable),
     // (Additional DB2 queries for FK, Child Table, Parent Table)
        // ... (You can add similar entries for MySQL, PostgreSQL, SQLite, and DB2)
    };
        }
    }
}
