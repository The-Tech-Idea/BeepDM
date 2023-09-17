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
        new QuerySqlRepo(DataSourceType.Oracle, "SELECT a.table_name AS child_table, a.column_name AS child_column, a.constraint_name, b.table_name AS parent_table, b.column_name AS parent_column FROM all_cons_columns a, all_constraints c, all_cons_columns b WHERE a.owner = c.owner AND a.constraint_name = c.constraint_name AND c.owner = b.owner AND c.r_constraint_name = b.constraint_name AND c.constraint_type = 'R' AND a.table_name = '{0}'", Sqlcommandtype.getFKforTable),
        new QuerySqlRepo(DataSourceType.Oracle, "SELECT a.table_name AS child_table, a.column_name AS child_column, a.constraint_name, b.table_name AS parent_table, b.column_name AS parent_column FROM all_cons_columns a, all_constraints c, all_cons_columns b WHERE a.owner = c.owner AND a.constraint_name = c.constraint_name AND c.owner = b.owner AND c.r_constraint_name = b.constraint_name AND c.constraint_type = 'R' AND b.table_name = '{0}'", Sqlcommandtype.getChildTable),
        new QuerySqlRepo(DataSourceType.Oracle, "SELECT c.r_constraint_name, b.table_name AS parent_table, b.column_name AS parent_column FROM all_constraints c, all_cons_columns b WHERE c.r_constraint_name = b.constraint_name AND c.table_name = '{0}'", Sqlcommandtype.getParentTable),

        // SQL Server
        new QuerySqlRepo(DataSourceType.SqlServer, "SELECT * FROM {0} WHERE {2}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.SqlServer, "SELECT t.NAME FROM sys.tables t INNER JOIN sys.schemas s ON t.schema_id = s.schema_id", Sqlcommandtype.getlistoftables),
        new QuerySqlRepo(DataSourceType.SqlServer, "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE TABLE_NAME = '{0}' AND CONSTRAINT_NAME LIKE 'PK%'", Sqlcommandtype.getPKforTable),
        new QuerySqlRepo(DataSourceType.SqlServer, "SELECT fk.table_name AS child_table, cu.column_name AS child_column, pk.table_name AS parent_table, pt.column_name AS parent_column FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS C INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS FK ON C.CONSTRAINT_NAME = FK.CONSTRAINT_NAME INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS PK ON C.UNIQUE_CONSTRAINT_NAME = PK.CONSTRAINT_NAME INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE CU ON C.CONSTRAINT_NAME = CU.CONSTRAINT_NAME INNER JOIN ( SELECT i1.TABLE_NAME, i2.COLUMN_NAME FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS i1 INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE i2 on i1.CONSTRAINT_NAME = i2.CONSTRAINT_NAME WHERE i1.CONSTRAINT_TYPE = 'PRIMARY KEY' ) PT ON PT.TABLE_NAME = PK.TABLE_NAME WHERE fk.table_name='{0}'", Sqlcommandtype.getFKforTable),
        new QuerySqlRepo(DataSourceType.SqlServer, "SELECT parent.table_name AS parent_table, pt.column_name AS parent_column, fk.table_name AS child_table, cu.column_name AS child_column FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS C INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS FK ON C.CONSTRAINT_NAME = FK.CONSTRAINT_NAME INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS PK ON C.UNIQUE_CONSTRAINT_NAME = PK.CONSTRAINT_NAME INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE CU ON C.CONSTRAINT_NAME = CU.CONSTRAINT_NAME INNER JOIN ( SELECT i1.TABLE_NAME, i2.COLUMN_NAME FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS i1 INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE i2 on i1.CONSTRAINT_NAME = i2.CONSTRAINT_NAME WHERE i1.CONSTRAINT_TYPE = 'PRIMARY KEY' ) PT ON PT.TABLE_NAME = PK.TABLE_NAME WHERE PT.table_name='{0}'", Sqlcommandtype.getChildTable),
        new QuerySqlRepo(DataSourceType.SqlServer, "SELECT FK.TABLE_NAME AS parent_table, CU.COLUMN_NAME AS parent_column FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS C INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS FK ON C.UNIQUE_CONSTRAINT_NAME = FK.CONSTRAINT_NAME INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE CU ON C.UNIQUE_CONSTRAINT_NAME = CU.CONSTRAINT_NAME WHERE C.CONSTRAINT_NAME IN (SELECT CONSTRAINT_NAME FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE TABLE_NAME = '{0}')", Sqlcommandtype.getParentTable),
 // Mysql
        new QuerySqlRepo(DataSourceType.Mysql, "SELECT * FROM {0} WHERE {2}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.Mysql, "SHOW TABLES", Sqlcommandtype.getlistoftables),
        new QuerySqlRepo(DataSourceType.Mysql, "SHOW KEYS FROM {0} WHERE Key_name = 'PRIMARY'", Sqlcommandtype.getPKforTable),
        new QuerySqlRepo(DataSourceType.Mysql, "SELECT TABLE_NAME AS child_table, COLUMN_NAME AS child_column, REFERENCED_TABLE_NAME AS parent_table, REFERENCED_COLUMN_NAME AS parent_column FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE TABLE_SCHEMA = '{1}' AND TABLE_NAME = '{0}' AND REFERENCED_TABLE_NAME IS NOT NULL", Sqlcommandtype.getFKforTable),
        new QuerySqlRepo(DataSourceType.Mysql, "SELECT TABLE_NAME AS parent_table, COLUMN_NAME AS parent_column, REFERENCED_TABLE_NAME AS child_table, REFERENCED_COLUMN_NAME AS child_column FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE TABLE_SCHEMA = '{1}' AND REFERENCED_TABLE_NAME = '{0}'", Sqlcommandtype.getChildTable),
        new QuerySqlRepo(DataSourceType.Mysql, "SELECT REFERENCED_TABLE_NAME AS parent_table, REFERENCED_COLUMN_NAME AS parent_column FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE TABLE_SCHEMA = '{1}' AND TABLE_NAME = '{0}' AND REFERENCED_TABLE_NAME IS NOT NULL", Sqlcommandtype.getParentTable),

        // Postgre
        new QuerySqlRepo(DataSourceType.Postgre, "SELECT * FROM {0} WHERE {2}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.Postgre, "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'", Sqlcommandtype.getlistoftables),
        new QuerySqlRepo(DataSourceType.Postgre, "SELECT a.attname FROM pg_index i JOIN pg_attribute a ON a.attnum = ANY(i.indkey) WHERE i.indrelid = '{0}'::regclass AND i.indisprimary", Sqlcommandtype.getPKforTable),
        new QuerySqlRepo(DataSourceType.Postgre, "SELECT conname, a.relname AS child_table, a.attname AS child_column, af.attname AS parent_column, b.relname AS parent_table FROM pg_attribute af, pg_attribute a, pg_class cf, pg_class c, pg_constraint con WHERE con.conrelid = c.oid AND con.confrelid = cf.oid AND af.attnum = ANY(con.confkey) AND af.attrelid = con.confrelid AND a.attnum = ANY(con.conkey) AND a.attrelid = con.conrelid AND c.relname = '{0}'", Sqlcommandtype.getFKforTable),
        new QuerySqlRepo(DataSourceType.Postgre, "SELECT conname, a.relname AS parent_table, a.attname AS parent_column, af.attname AS child_column, b.relname AS child_table FROM pg_attribute af, pg_attribute a, pg_class cf, pg_class c, pg_constraint con WHERE con.conrelid = c.oid AND con.confrelid = cf.oid AND af.attnum = ANY(con.conkey) AND af.attrelid = con.conrelid AND a.attnum = ANY(con.confkey) AND a.attrelid = con.confrelid AND cf.relname = '{0}'", Sqlcommandtype.getChildTable),
        new QuerySqlRepo(DataSourceType.Postgre, "SELECT cf.relname AS parent_table, af.attname AS parent_column FROM pg_attribute af, pg_attribute a, pg_class cf, pg_class c, pg_constraint con WHERE con.conrelid = c.oid AND con.confrelid = cf.oid AND af.attnum = ANY(con.confkey) AND af.attrelid = con.confrelid AND a.attnum = ANY(con.conkey) AND a.attrelid = con.conrelid AND c.relname = '{0}'", Sqlcommandtype.getParentTable)
         // SQLite
        new QuerySqlRepo(DataSourceType.SqlLite, "SELECT * FROM {0} WHERE {2}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.SqlLite, "SELECT name FROM sqlite_master WHERE type='table';", Sqlcommandtype.getlistoftables),
        new QuerySqlRepo(DataSourceType.SqlLite, "PRAGMA table_info({0})", Sqlcommandtype.getPKforTable),
        // Note: SQLite does not have native FOREIGN KEY listing, handle programmatically if needed
        // ... (Add more queries for SQLite as per your needs)

        // DB2
        new QuerySqlRepo(DataSourceType.DB2, "SELECT * FROM {0} WHERE {2}", Sqlcommandtype.getTable),
        new QuerySqlRepo(DataSourceType.DB2, "SELECT TABNAME FROM SYSCAT.TABLES WHERE TABSCHEMA = '{1}'", Sqlcommandtype.getlistoftables),
        new QuerySqlRepo(DataSourceType.DB2, "SELECT COLNAME FROM SYSCAT.KEYCOLUSE WHERE TABNAME = '{0}' AND CONSTNAME LIKE 'PK%'", Sqlcommandtype.getPKforTable),
        new QuerySqlRepo(DataSourceType.DB2, "SELECT FK_COLNAMES, PK_COLNAMES, PK_TABSCHEMA, PK_TABNAME FROM SYSCAT.REFERENCES WHERE TABNAME = '{0}'", Sqlcommandtype.getFKforTable),
        new QuerySqlRepo(DataSourceType.DB2, "SELECT FK_COLNAMES, PK_COLNAMES, FK_TABSCHEMA, FK_TABNAME FROM SYSCAT.REFERENCES WHERE PK_TABNAME = '{0}'", Sqlcommandtype.getChildTable),
        new QuerySqlRepo(DataSourceType.DB2, "SELECT PK_TABNAME, PK_COLNAMES FROM SYSCAT.REFERENCES WHERE TABNAME = '{0}'", Sqlcommandtype.getParentTable),
    };
        }


    }
}
