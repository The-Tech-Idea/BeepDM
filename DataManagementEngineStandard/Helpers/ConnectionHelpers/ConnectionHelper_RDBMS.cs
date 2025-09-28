using System;
using System.Collections.Generic;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Helpers
{
    /// <summary>
    /// Partial class for RDBMS (Relational Database Management System) connection configurations
    /// </summary>
    public static partial class ConnectionHelper
    {
        /// <summary>
        /// Gets all RDBMS connection configurations
        /// </summary>
        /// <returns>List of RDBMS connection configurations</returns>
        public static List<ConnectionDriversConfig> GetRDBMSConfigs()
        {
            var configs = new List<ConnectionDriversConfig>
            {
                CreateSqlServerConfig(),
                CreateMySqlConfig(),
                CreatePostgreConfig(),
                CreateOracleConfig(),
                CreateSQLiteConfig(),
                CreateSqlCompactConfig(),
                CreateFirebirdConfig(),
                CreateDB2Config(),
                CreateSnowFlakeConfig(),
                CreateVistaDBConfig(),
                CreateDuckDBConfig(),
                CreateTimeScaleConfig(),
                CreateCockroachConfig(),
                CreateSpannerConfig(),
                CreateTerraDataConfig(),
                CreateVerticaConfig(),
                CreateAWSRDSConfig(),
                CreateAzureSQLConfig(),
                CreateHanaConfig()
            };

            return configs;
        }

        /// <summary>Creates a configuration object for connecting to a SQL Server database.</summary>
        /// <returns>A configuration object for connecting to a SQL Server database.</returns>
        public static ConnectionDriversConfig CreateSqlServerConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "c71f42dd-ad9f-4bae-898e-a28ac9d6854a",
                PackageName = "System.Data.SqlClient",
                DriverClass = "System.Data.SqlClient",
                version = "4.6.1.1",
                dllname = "System.Data.SqlClient.dll",
                AdapterType = "System.Data.SqlClient.SqlDataAdapter",
                CommandBuilderType = "System.Data.SqlClient.SqlCommandBuilder",
                DbConnectionType = "System.Data.SqlClient.SqlConnection",
                DbTransactionType = "System.Data.SqlClient.SqlTransaction",
                ConnectionString = "Server={Host};Database={Database};User Id ={UserID}; Password ={Password};Trusted_Connection=False",
                iconname = "sqlserver.svg",
                classHandler = "SQLServerDataSource",
                ADOType = true,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.RDBMS,
                DatasourceType = DataSourceType.SqlServer,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for connecting to a MySQL database.</summary>
        /// <returns>A configuration object for connecting to a MySQL database.</returns>
        public static ConnectionDriversConfig CreateMySqlConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "62136e89-6c98-4dc1-8b33-91af0f7566be",
                PackageName = "MySql.Data",
                DriverClass = "MySql.Data",
                version = "8.0.28.0",
                dllname = "MySql.Data.dll",
                AdapterType = "MySql.Data.MySqlClient.MySqlDataAdapter",
                CommandBuilderType = "MySql.Data.MySqlClient.MySqlCommandBuilder",
                DbConnectionType = "MySql.Data.MySqlClient.MySqlConnection",
                DbTransactionType = "MySql.Data.MySqlClient.MySqlTransaction",
                ConnectionString = "Server={Host};PORT={Port};Database={Database};Uid ={UserID}; Pwd = {Password};",
                iconname = "mysql.svg",
                classHandler = "MySQLDataSource",
                ADOType = true,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.RDBMS,
                DatasourceType = DataSourceType.Mysql,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for connecting to a PostgreSQL database.</summary>
        /// <returns>A configuration object for connecting to a PostgreSQL database.</returns>
        public static ConnectionDriversConfig CreatePostgreConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "00b95824-0408-47dc-99d3-b8e053873caf",
                PackageName = "Npgsql",
                DriverClass = "Npgsql",
                version = "4.1.3.0",
                dllname = "Npgsql.dll",
                AdapterType = "Npgsql.NpgsqlDataAdapter",
                CommandBuilderType = "Npgsql.NpgsqlCommandBuilder",
                DbConnectionType = "Npgsql.NpgsqlConnection",
                ConnectionString = "User ID={UserID};Password={Password};Host={Host};Port={Port};Database={DataBase};SSL Mode=Require; Trust Server Certificate=true;",
                iconname = "postgre.svg",
                classHandler = "PostgreDataSource",
                ADOType = true,
                CreateLocal = false,
                InMemory = false,
                Favourite = true,
                DatasourceCategory = DatasourceCategory.RDBMS,
                DatasourceType = DataSourceType.Postgre,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for Oracle database connection drivers.</summary>
        /// <returns>A configuration object for Oracle database connection drivers.</returns>
        public static ConnectionDriversConfig CreateOracleConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "54fd12d5-3ce5-41a4-8d17-75ad26e9e595",
                PackageName = "Oracle.ManagedDataAccess",
                DriverClass = "Oracle.ManagedDataAccess",
                version = "4.122.19.1",
                dllname = "Oracle.ManagedDataAccess.dll",
                AdapterType = "Oracle.ManagedDataAccess.Client.OracleDataAdapter",
                CommandBuilderType = "Oracle.ManagedDataAccess.Client.OracleCommandBuilder",
                DbConnectionType = "Oracle.ManagedDataAccess.Client.OracleConnection",
                ConnectionString = "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={Host})(PORT={Port}))(CONNECT_DATA=(SID={Database})));User Id ={UserID}; Password = {Password}; ",
                iconname = "oracle.svg",
                classHandler = "OracleDataSource",
                ADOType = true,
                CreateLocal = false,
                InMemory = false,
                Favourite = true,
                DatasourceCategory = DatasourceCategory.RDBMS,
                DatasourceType = DataSourceType.Oracle,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for SQLite connection drivers.</summary>
        /// <returns>A configuration object for SQLite connection drivers.</returns>
        public static ConnectionDriversConfig CreateSQLiteConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "b68a607a-8d54-4ca1-b8e3-0feabe3f5589",
                PackageName = "System.Data.SQLite",
                DriverClass = "System.Data.SQLite",
                version = "1.0.113.0",
                dllname = "System.Data.SQLite.dll",
                AdapterType = "System.Data.SQLite.SQLiteDataAdapter",
                CommandBuilderType = "System.Data.SQLite.SQLiteCommandBuilder",
                DbConnectionType = "System.Data.SQLite.SQLiteConnection",
                DbTransactionType = "System.Data.SQLite.SQLiteTransaction2",
                ConnectionString = "Data Source={File};version=3",
                iconname = "Sqlite.svg",
                classHandler = "SQLiteDataSource",
                ADOType = true,
                CreateLocal = true,
                InMemory = false,
                Favourite = true,
                extensionstoHandle = "s3db",
                DatasourceCategory = DatasourceCategory.RDBMS,
                DatasourceType = DataSourceType.SqlLite,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for SQL Compact connection drivers.</summary>
        /// <returns>A configuration object for SQL Compact connection drivers.</returns>
        public static ConnectionDriversConfig CreateSqlCompactConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "11cd816d-4448-43fb-9079-52741f347fd7",
                PackageName = "System.Data.SqlServerCe",
                DriverClass = "System.Data.SqlServerCe",
                version = "4.0.0.0",
                dllname = "System.Data.SqlServerCe.dll",
                AdapterType = "System.Data.SqlServerCe.SqlCeDataAdapter",
                CommandBuilderType = "System.Data.SqlServerCe.SqlCeCommandBuilder",
                DbConnectionType = "System.Data.SqlServerCe.SqlCeConnection",
                DbTransactionType = "System.Data.SqlServerCe.SqlCeTransaction",
                ConnectionString = "Data Source={File};Max Buffer Size=16384;Max Database Size=512;",
                iconname = "sqlserverCompact.svg",
                classHandler = "SQLCompactDataSource",
                ADOType = true,
                CreateLocal = true,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.RDBMS,
                DatasourceType = DataSourceType.SqlCompact,
                IsMissing = false
            };
        }

        /// <summary>
        /// Creates a configuration object for Firebird database connection drivers.
        /// </summary>
        /// <returns>A configuration object for Firebird database connection drivers.</returns>
        public static ConnectionDriversConfig CreateFirebirdConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "6e4be76d-5c8c-404a-a965-767fbe383b32",
                PackageName = "FirebirdSql.Data.FirebirdClient",
                DriverClass = "FirebirdSql.Data.FirebirdClient",
                version = "7.10.1.0",
                dllname = "FirebirdSql.Data.FirebirdClient.dll",
                AdapterType = "FirebirdSql.Data.FirebirdClient.FbDataAdapter",
                CommandBuilderType = "FirebirdSql.Data.FirebirdClient.FbCommandBuilder",
                DbConnectionType = "FirebirdSql.Data.FirebirdClient.FbConnection",
                DbTransactionType = "FirebirdSql.Data.FirebirdClient.FbTransaction",
                ConnectionString = "User={UserID};Password={Password};Database={Database};DataSource={Host};Port={Port};Dialect=3;Charset=NONE;Role=;Connection lifetime=15;Pooling=true;MinPoolSize=0;MaxPoolSize=50;Packet Size=8192;",
                iconname = "Firebird.svg",
                classHandler = "FireBirdDataSource",
                ADOType = true,
                CreateLocal = false,
                InMemory = false,
                Favourite = true,
                DatasourceCategory = DatasourceCategory.RDBMS,
                DatasourceType = DataSourceType.FireBird,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for connecting to a DB2 database.</summary>
        /// <returns>A ConnectionDriversConfig object with the DB2 configuration settings.</returns>
        public static ConnectionDriversConfig CreateDB2Config()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "db2-guid",
                PackageName = "IBM.Data.DB2",
                DriverClass = "IBM.Data.DB2",
                version = "11.5.0.0",
                dllname = "IBM.Data.DB2.dll",
                AdapterType = "IBM.Data.DB2.DB2DataAdapter",
                CommandBuilderType = "IBM.Data.DB2.DB2CommandBuilder",
                DbConnectionType = "IBM.Data.DB2.DB2Connection",
                ConnectionString = "Database={database};Server={host};UserID={username};Password={password};",
                iconname = "db2.svg",
                classHandler = "DB2DataSource",
                ADOType = true,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.RDBMS,
                DatasourceType = DataSourceType.DB2,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for SnowFlake connection drivers.</summary>
        /// <returns>A ConnectionDriversConfig object with the specified properties.</returns>
        public static ConnectionDriversConfig CreateSnowFlakeConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "snowflake-guid",
                PackageName = "SnowFlake",
                DriverClass = "SnowFlake",
                version = "4.10.2.0",
                dllname = "SnowFlake.dll",
                AdapterType = "SnowFlake.SnowFlakeDataAdapter",
                DbConnectionType = "SnowFlake.SnowFlakeConnection",
                ConnectionString = "Account=your-account;Warehouse=your-warehouse;Database=your-database;User=your-user;Password=your-password;",
                iconname = "snowflake.svg",
                classHandler = "SnowFlakeDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.RDBMS,
                DatasourceType = DataSourceType.SnowFlake,
                IsMissing = false
            };
        }

        /// <summary>
        /// Creates a configuration object for VistaDB connection drivers.
        /// </summary>
        /// <returns>A configuration object for VistaDB connection drivers.</returns>
        public static ConnectionDriversConfig CreateVistaDBConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "vistadb-guid",
                PackageName = "VistaDB",
                DriverClass = "VistaDB",
                version = "5.7.0.0",
                dllname = "VistaDB.dll",
                AdapterType = "VistaDB.VistaDBDataAdapter",
                CommandBuilderType = "VistaDB.VistaDBCommandBuilder",
                DbConnectionType = "VistaDB.VistaDBConnection",
                ConnectionString = "Data Source={file};",
                iconname = "vistadb.svg",
                classHandler = "VistaDBDataSource",
                ADOType = true,
                CreateLocal = false,
                InMemory = false,
                Favourite = true,
                DatasourceCategory = DatasourceCategory.RDBMS,
                DatasourceType = DataSourceType.VistaDB,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for DuckDB connection drivers.</summary>
        /// <returns>A configuration object for DuckDB connection drivers.</returns>
        public static ConnectionDriversConfig CreateDuckDBConfig()
        {
            return new ConnectionDriversConfig
            {
                ADOType = true,
                classHandler = "DuckDBDataSource",
                CreateLocal = false,
                DatasourceCategory = DatasourceCategory.RDBMS,
                DatasourceType = DataSourceType.DuckDB,
                DbConnectionType = "DuckDB.NET.Data.DuckDBConnection",
                DbTransactionType = "DuckDB.NET.Data.DuckDBTransaction",
                dllname = "DuckDB.NET.Data.dll",
                DriverClass = "DuckDB.NET.Data",
                Favourite = false,
                GuidID = "e4683996-bb84-48fb-91dc-eb6a4a93616f",
                iconname = "duckdb.svg",
                ID = 0,
                InMemory = true,
                IsMissing = false,
                PackageName = "DuckDB.NET.Data",
                version = "0.8.1.0"
            };
        }

        /// <summary>Creates a configuration object for TimeScale connection drivers.</summary>
        /// <returns>A configuration object for TimeScale connection drivers.</returns>
        public static ConnectionDriversConfig CreateTimeScaleConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "timescale-guid",
                PackageName = "Npgsql",
                DriverClass = "Npgsql",
                version = "4.1.3.0",
                dllname = "Npgsql.dll",
                AdapterType = "Npgsql.NpgsqlDataAdapter",
                CommandBuilderType = "Npgsql.NpgsqlCommandBuilder",
                DbConnectionType = "Npgsql.NpgsqlConnection",
                ConnectionString = "User ID={UserID};Password={Password};Host={Host};Port={Port};Database={DataBase};",
                iconname = "timescale.svg",
                classHandler = "TimeScaleDataSource",
                ADOType = true,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.RDBMS,
                DatasourceType = DataSourceType.TimeScale,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for CockroachDB connection drivers.</summary>
        /// <returns>A configuration object for CockroachDB connection drivers.</returns>
        public static ConnectionDriversConfig CreateCockroachConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "cockroach-guid",
                PackageName = "Npgsql",
                DriverClass = "Npgsql",
                version = "4.1.3.0",
                dllname = "Npgsql.dll",
                AdapterType = "Npgsql.NpgsqlDataAdapter",
                CommandBuilderType = "Npgsql.NpgsqlCommandBuilder",
                DbConnectionType = "Npgsql.NpgsqlConnection",
                ConnectionString = "User ID={UserID};Password={Password};Host={Host};Port={Port};Database={DataBase};",
                iconname = "cockroach.svg",
                classHandler = "CockroachDataSource",
                ADOType = true,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.RDBMS,
                DatasourceType = DataSourceType.Cockroach,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for Google Spanner connection drivers.</summary>
        /// <returns>A configuration object for Google Spanner connection drivers.</returns>
        public static ConnectionDriversConfig CreateSpannerConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "spanner-guid",
                PackageName = "Google.Cloud.Spanner.Data",
                DriverClass = "Google.Cloud.Spanner.Data",
                version = "4.0.0.0",
                dllname = "Google.Cloud.Spanner.Data.dll",
                ConnectionString = "Data Source=projects/{ProjectId}/instances/{InstanceId}/databases/{Database};",
                iconname = "spanner.svg",
                classHandler = "SpannerDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.RDBMS,
                DatasourceType = DataSourceType.Spanner,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for Teradata connection drivers.</summary>
        /// <returns>A configuration object for Teradata connection drivers.</returns>
        public static ConnectionDriversConfig CreateTerraDataConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "teradata-guid",
                PackageName = "Teradata.Client.Provider",
                DriverClass = "Teradata.Client.Provider",
                version = "20.0.0.0",
                dllname = "Teradata.Client.Provider.dll",
                ConnectionString = "Data Source={Host};User Id={UserID};Password={Password};",
                iconname = "teradata.svg",
                classHandler = "TerraDataSource",
                ADOType = true,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.RDBMS,
                DatasourceType = DataSourceType.TerraData,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for Vertica connection drivers.</summary>
        /// <returns>A configuration object for Vertica connection drivers.</returns>
        public static ConnectionDriversConfig CreateVerticaConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "vertica-guid",
                PackageName = "Vertica.Data",
                DriverClass = "Vertica.Data",
                version = "11.0.0.0",
                dllname = "Vertica.Data.dll",
                ConnectionString = "Server={Host};Port={Port};Database={Database};User Id={UserID};Password={Password};",
                iconname = "vertica.svg",
                classHandler = "VerticaDataSource",
                ADOType = true,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.RDBMS,
                DatasourceType = DataSourceType.Vertica,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for AWS RDS connection drivers.</summary>
        /// <returns>A configuration object for AWS RDS connection drivers.</returns>
        public static ConnectionDriversConfig CreateAWSRDSConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "awsrds-guid",
                PackageName = "MySql.Data",
                DriverClass = "MySql.Data",
                version = "8.0.28.0",
                dllname = "MySql.Data.dll",
                AdapterType = "MySql.Data.MySqlClient.MySqlDataAdapter",
                CommandBuilderType = "MySql.Data.MySqlClient.MySqlCommandBuilder",
                DbConnectionType = "MySql.Data.MySqlClient.MySqlConnection",
                DbTransactionType = "MySql.Data.MySqlClient.MySqlTransaction",
                ConnectionString = "Server={Host};PORT={Port};Database={Database};Uid ={UserID}; Pwd = {Password};",
                iconname = "awsrds.svg",
                classHandler = "AWSRDSDataSource",
                ADOType = true,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.RDBMS,
                DatasourceType = DataSourceType.AWSRDS,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for Azure SQL connection drivers.</summary>
        /// <returns>A configuration object for Azure SQL connection drivers.</returns>
        public static ConnectionDriversConfig CreateAzureSQLConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "azuresql-guid",
                PackageName = "System.Data.SqlClient",
                DriverClass = "System.Data.SqlClient",
                version = "4.6.1.1",
                dllname = "System.Data.SqlClient.dll",
                AdapterType = "System.Data.SqlClient.SqlDataAdapter",
                CommandBuilderType = "System.Data.SqlClient.SqlCommandBuilder",
                DbConnectionType = "System.Data.SqlClient.SqlConnection",
                DbTransactionType = "System.Data.SqlClient.SqlTransaction",
                ConnectionString = "Server={Host};Database={Database};User Id ={UserID}; Password ={Password};Encrypt=True;TrustServerCertificate=False;",
                iconname = "azuresql.svg",
                classHandler = "AzureSQLDataSource",
                ADOType = true,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.RDBMS,
                DatasourceType = DataSourceType.AzureSQL,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for SAP HANA connection drivers.</summary>
        /// <returns>A configuration object for SAP HANA connection drivers.</returns>
        public static ConnectionDriversConfig CreateHanaConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "hana-guid",
                PackageName = "Sap.Data.Hana",
                DriverClass = "Sap.Data.Hana",
                version = "2.15.0.0",
                dllname = "Sap.Data.Hana.dll",
                ConnectionString = "Server={Host}:{Port};UserID={UserID};Password={Password};Database={Database};",
                iconname = "hana.svg",
                classHandler = "HanaDataSource",
                ADOType = true,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.RDBMS,
                DatasourceType = DataSourceType.Hana,
                IsMissing = false
            };
        }
    }
}