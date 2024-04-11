using DataManagementModels.DriversConfigurations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.Helpers
{
    public static class ConnectionHelper
    {
        /// <summary>Links a connection to its corresponding drivers in the configuration editor.</summary>
        /// <param name="cn">The connection properties.</param>
        /// <param name="configEditor">The configuration editor.</param>
        /// <returns>The connection drivers configuration.</returns>
        public static ConnectionDriversConfig LinkConnection2Drivers(IConnectionProperties cn, IConfigEditor configEditor)
        {

            string vr = cn.DriverVersion;
            string pk = cn.DriverName;
            ConnectionDriversConfig retval = configEditor.DataDriversClasses.Where(c => c.PackageName.Equals(pk, StringComparison.InvariantCultureIgnoreCase) && c.version == vr).FirstOrDefault();
            if (retval == null)
            {
                retval = configEditor.DataDriversClasses.Where(c => c.PackageName.Equals(pk, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                if (retval == null)
                {
                    retval = configEditor.DataDriversClasses.Where(c => c.DatasourceType == cn.DatabaseType).FirstOrDefault();
                    if (retval == null)
                    {
                        if (cn.Category == DatasourceCategory.FILE)
                        {
                            List<ConnectionDriversConfig> clss = configEditor.DataDriversClasses.Where(p => p.extensionstoHandle != null).ToList();
                            string ext = Path.GetExtension(cn.FileName).Replace(".", "");
                            retval = clss.Where(c => c.extensionstoHandle.Contains(ext)).FirstOrDefault();
                        }
                    }

                }

            }
            return retval;


        }
        /// <summary>Replaces a value in a connection string based on the provided parameters.</summary>
        /// <param name="DataSourceDriver">The driver configuration for the data source.</param>
        /// <param name="ConnectionProp">The connection properties.</param>
        /// <param name="DMEEditor">The DME editor.</param>
        /// <returns>The modified connection string.</returns>
        public static string ReplaceValueFromConnectionString(ConnectionDriversConfig DataSourceDriver, IConnectionProperties ConnectionProp, IDMEEditor DMEEditor)
        {
            bool IsConnectionString = false;
            bool IsUrl = false;
            bool IsFile = false;
            string rep = "";
            string input = "";
            string replacement;
            string pattern;

            if (string.IsNullOrWhiteSpace(ConnectionProp.ConnectionString))
            {
                if (!string.IsNullOrEmpty(DataSourceDriver.ConnectionString))
                {
                    IsConnectionString = true;
                    ConnectionProp.ConnectionString = DataSourceDriver.ConnectionString;

                }
            }
            else
            {
                IsConnectionString = true;
            }

            if (!string.IsNullOrWhiteSpace(ConnectionProp.Url))
            {
                IsUrl = true;

            }
            if (!string.IsNullOrWhiteSpace(ConnectionProp.FilePath) || !string.IsNullOrWhiteSpace(ConnectionProp.FileName))
            {

                IsFile = true;
            }
            if (IsConnectionString)
            {

                input = ConnectionProp.ConnectionString;
            }

            if (IsUrl)
            {
                input = ConnectionProp.Url;
                pattern = "{Url}";
                replacement = ConnectionProp.Url ?? string.Empty; ;
                input = Regex.Replace(input, pattern, replacement, RegexOptions.IgnoreCase);

            }
            if (IsFile)
            {
                if (ConnectionProp.FilePath.StartsWith(".") || ConnectionProp.FilePath.Equals("/") || ConnectionProp.FilePath.Equals("\\"))
                {
                    ConnectionProp.FilePath = ConnectionProp.FilePath.Replace(".", DMEEditor.ConfigEditor.ExePath);
                }
                // input= Path.Combine(ConnectionProp.FilePath, ConnectionProp.FileName);
            }




            pattern = "{Host}";
            replacement = ConnectionProp.Host ?? string.Empty;
            input = Regex.Replace(input, pattern, replacement, RegexOptions.IgnoreCase);

            pattern = "{UserID}";
            replacement = ConnectionProp.UserID ?? string.Empty;
            input = Regex.Replace(input, pattern, replacement, RegexOptions.IgnoreCase);

            pattern = "{Password}";
            replacement = ConnectionProp.Password ?? string.Empty;
            input = Regex.Replace(input, pattern, replacement, RegexOptions.IgnoreCase);

            pattern = "{DataBase}";
            replacement = ConnectionProp.Database ?? string.Empty;
            input = Regex.Replace(input, pattern, replacement, RegexOptions.IgnoreCase);

            pattern = "{Port}";
            replacement = ConnectionProp.Port.ToString() ?? string.Empty;
            input = Regex.Replace(input, pattern, replacement, RegexOptions.IgnoreCase);






            if (IsFile)
            {
                if (!string.IsNullOrWhiteSpace(ConnectionProp.ConnectionString))
                {

                    pattern = "{File}";
                    replacement = Path.Combine(ConnectionProp.FilePath, ConnectionProp.FileName) ?? string.Empty;
                    input = Regex.Replace(input, pattern, replacement, RegexOptions.IgnoreCase);
                }
                else
                {
                    input = Path.Combine(ConnectionProp.FilePath, ConnectionProp.FileName);
                }
            }


            rep = input;
            return rep;
        }
        /// <summary>Returns a list of all connection configurations.</summary>
        /// <returns>A list of ConnectionDriversConfig objects representing different connection configurations.</returns>
        public static List<ConnectionDriversConfig> GetAllConnectionConfigs()
        {
            List<ConnectionDriversConfig> configs = new List<ConnectionDriversConfig>();

            configs.Add(CreatePostgreConfig());
            configs.Add(CreateMongoDBConfig());
            configs.Add(CreateStackExchangeRedisConfig());
            configs.Add(CreateCouchbaseLiteConfig());
            configs.Add(CreateElasticsearchConfig());
            configs.Add(CreateSQLiteConfig());
            configs.Add(CreateRavenDBConfig());
            configs.Add(CreateCSVFileReaderConfig());
            configs.Add(CreateFirebirdConfig());
            configs.Add(CreateCassandraConfig());
            configs.Add(CreateMySqlConnectorConfig());
            configs.Add(CreateSqlServerConfig());
            configs.Add(CreateSqlCompactConfig());
            configs.Add(CreateTxtXlsCSVFileSourceConfig());
            configs.Add(CreateLiteDBDataSourceConfig());
            configs.Add(CreateOracleConfig());
            configs.Add(CreateDuckDBConfig());
            configs.Add(CreateRealmConfig());
            configs.Add(CreateSnowFlakeConfig());
            configs.Add(CreateHadoopConfig());
            configs.Add(CreateRedisConfig());
            configs.Add(CreateKafkaConfig());
            configs.Add(CreateOPCConfig());
            configs.Add(CreateDB2Config());
            configs.Add(CreateCouchDBConfig());
            configs.Add(CreateVistaDBConfig());
            configs.Add(CreateCouchbaseConfig());
            configs.Add(CreateFirebaseConfig());
            configs.Add(CreateDataViewConfig());
            configs.Add(CreateJsonDataSourceConfig());
            

            return configs;
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
                iconname = "snowflake.png",
                classHandler = "SnowFlakeDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.NOSQL,
                DatasourceType = DataSourceType.SnowFlake,
                IsMissing = false
            };
        }
        /// <summary>Creates a configuration object for Hadoop connection drivers.</summary>
        /// <returns>A ConnectionDriversConfig object representing the Hadoop configuration.</returns>
        public static ConnectionDriversConfig CreateHadoopConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "hadoop-guid",
                PackageName = "Hadoop",
                DriverClass = "Hadoop",
                version = "3.3.0.0",
                dllname = "Hadoop.dll",
                AdapterType = "Hadoop.HadoopDataAdapter",
                DbConnectionType = "Hadoop.HadoopConnection",
                ConnectionString = "Server=your-hadoop-server;Port=your-port;",
                iconname = "hadoop.png",
                classHandler = "HadoopDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.NOSQL,
                DatasourceType = DataSourceType.Hadoop,
                IsMissing = false
            };
        }
        /// <summary>Creates a configuration object for Redis connection drivers.</summary>
        /// <returns>A ConnectionDriversConfig object with the specified properties.</returns>
        public static ConnectionDriversConfig CreateRedisConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "redis-guid",
                PackageName = "StackExchange.Redis",
                DriverClass = "StackExchange.Redis",
                version = "2.2.4.0",
                dllname = "StackExchange.Redis.dll",
                AdapterType = "StackExchange.Redis.RedisDataAdapter",
                DbConnectionType = "StackExchange.Redis.RedisConnection",
                ConnectionString = "Configuration=your-redis-server:port;",
                iconname = "redis.png",
                classHandler = "RedisDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.INMEMORY,
                DatasourceType = DataSourceType.Redis,
                IsMissing = false
            };
        }
        /// <summary>Creates a configuration object for Kafka connection drivers.</summary>
        /// <returns>A ConnectionDriversConfig object with Kafka-specific configuration.</returns>
        public static ConnectionDriversConfig CreateKafkaConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "kafka-guid",
                PackageName = "Kafka",
                DriverClass = "Kafka",
                version = "2.8.0.0",
                dllname = "Kafka.dll",
                AdapterType = "Kafka.KafkaDataAdapter",
                DbConnectionType = "Kafka.KafkaConnection",
                ConnectionString = "BrokerList=your-broker-list;ClientId=your-client-id;",
                iconname = "kafka.png",
                classHandler = "KafkaDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.STREAM,
                DatasourceType = DataSourceType.Kafka,
                IsMissing = false
            };
        }
        /// <summary>Creates a configuration object for OPC connection drivers.</summary>
        /// <returns>A ConnectionDriversConfig object representing the OPC configuration.</returns>
        public static ConnectionDriversConfig CreateOPCConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "opc-guid",
                PackageName = "OPC",
                DriverClass = "OPC",
                version = "2.0.0.0",
                dllname = "OPC.dll",
                AdapterType = "OPC.OPCDataAdapter",
                CommandBuilderType = "OPC.OPCCommandBuilder",
                DbConnectionType = "OPC.OPCConnection",
                ConnectionString = "Server=your-opc-server;Port=your-port;Node=your-node;",
                iconname = "opc.png",
                classHandler = "OPCDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.WEBAPI,
                DatasourceType = DataSourceType.OPC,
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
                ConnectionString = "Database=your-database;Server=your-server;UserID=your-userid;Password=your-password;",
                iconname = "db2.png",
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
        /// <summary>
        /// Creates a configuration object for connecting to CouchDB.
        /// </summary>
        /// <returns>A configuration object for connecting to CouchDB.</returns>
        public static ConnectionDriversConfig CreateCouchDBConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "couchdb-guid",
                PackageName = "CouchDB",
                DriverClass = "CouchDB",
                version = "3.1.0.0",
                dllname = "CouchDB.dll",
                AdapterType = "CouchDB.CouchDBDataAdapter",
                CommandBuilderType = "CouchDB.CouchDBCommandBuilder",
                DbConnectionType = "CouchDB.CouchDBConnection",
                ConnectionString = "Server=your-server;Port=your-port;Database=your-database;",
                iconname = "couchdb.png",
                classHandler = "CouchDBDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.NOSQL,
                DatasourceType = DataSourceType.CouchDB,
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
                ConnectionString = "Data Source=your-database-file.vdb5;",
                iconname = "vistadb.png",
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
        /// <summary>Creates a configuration object for connecting to Couchbase.</summary>
        /// <returns>A configuration object for connecting to Couchbase.</returns>
        public static ConnectionDriversConfig CreateCouchbaseConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "couchbase-guid",
                PackageName = "Couchbase",
                DriverClass = "Couchbase",
                version = "3.0.0.0",
                dllname = "Couchbase.dll",
                AdapterType = "Couchbase.CouchbaseDataAdapter",
                CommandBuilderType = "Couchbase.CouchbaseCommandBuilder",
                DbConnectionType = "Couchbase.CouchbaseConnection",
                ConnectionString = "Server=your-server;Bucket=your-bucket;Username=your-username;Password=your-password;",
                iconname = "couchbase.png",
                classHandler = "CouchbaseDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = true,
                DatasourceCategory = DatasourceCategory.NOSQL,
                DatasourceType = DataSourceType.Couchbase,
                IsMissing = false
            };
        }
        /// <summary>Creates a configuration object for Firebase connection drivers.</summary>
        /// <returns>A configuration object for Firebase connection drivers.</returns>
        public static ConnectionDriversConfig CreateFirebaseConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "firebase-guid",
                PackageName = "Firebase",
                DriverClass = "Firebase",
                version = "7.0.0.0",
                dllname = "Firebase.dll",
                AdapterType = "Firebase.FirebaseDataAdapter",
                CommandBuilderType = "Firebase.FirebaseCommandBuilder",
                DbConnectionType = "Firebase.FirebaseConnection",
                ConnectionString = "ApiKey=your-api-key;DatabaseURL=your-database-url",
                iconname = "firebase.png",
                classHandler = "FirebaseDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = true,
                DatasourceCategory = DatasourceCategory.NOSQL,
                DatasourceType = DataSourceType.Firebase,
                IsMissing = false
            };
        }
        /// <summary>Creates a configuration object for connecting to a Realm database.</summary>
        /// <returns>A configuration object for connecting to a Realm database.</returns>
        public static ConnectionDriversConfig CreateRealmConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "realm-guid",
                PackageName = "Realm",
                DriverClass = "Realm",
                version = "5.0.0.0",
                dllname = "Realm.dll",
                AdapterType = "Realm.RealmDataAdapter",
                CommandBuilderType = "Realm.RealmCommandBuilder",
                DbConnectionType = "Realm.RealmConnection",
                ConnectionString = "Path={database}",
                iconname = "realm.png",
                classHandler = "RealMDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = true,
                DatasourceCategory = DatasourceCategory.NOSQL, // Assuming appropriate enum value
                DatasourceType = DataSourceType.RealIM, // Assuming appropriate enum value
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
                ConnectionString = "User ID={UserID};Password={Password};Host={Host};Port={Port};Database=myDataBase;Pooling=true;Min Pool Size=0;Max Pool Size=100;Connection Lifetime=0;",
                iconname = "postgres.png",
                classHandler = "PostgreDataSource",
                ADOType = true,
                CreateLocal = false,
                InMemory = false,
                Favourite = true,
                DatasourceCategory = DatasourceCategory.RDBMS, // Assuming appropriate enum value
                DatasourceType = DataSourceType.Postgre, // Assuming appropriate enum value
                IsMissing = false
            };
        }
        /// <summary>Creates a configuration object for MongoDB connection drivers.</summary>
        /// <returns>A configuration object for MongoDB connection drivers.</returns>
        public static ConnectionDriversConfig CreateMongoDBConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "f0ca152d-a371-4109-a7e9-5fb49b406305",
                PackageName = "MongoDB.Driver",
                DriverClass = "MongoDB.Driver",
                version = "2.10.4.0",
                dllname = "MongoDB.Driver.dll",
                AdapterType = "BsonDocument",
                DbConnectionType = "MongoClient",
                iconname = "mongodb.png",
                classHandler = "MongoDBDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.NOSQL, // Assuming appropriate enum value
                DatasourceType = DataSourceType.MongoDB, // Assuming appropriate enum value
                IsMissing = false
            };
        }
        /// <summary>Creates a configuration object for StackExchange.Redis.</summary>
        /// <returns>A configuration object for StackExchange.Redis.</returns>
        public static ConnectionDriversConfig CreateStackExchangeRedisConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "c90262d9-ffd9-489f-b8b8-aaa39c82beb4",
                PackageName = "StackExchange.Redis",
                DriverClass = "StackExchange.Redis",
                version = "2.0.0.0",
                dllname = "StackExchange.Redis.dll",
                AdapterType = "IDatabase",
                CommandBuilderType = "ISubscriber",
                DbConnectionType = "ConnectionMultiplexer",
                classHandler = "StackExchangeRedisDatasource",
                iconname = "redis.png",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.RDBMS, // Assuming appropriate enum value
                DatasourceType = DataSourceType.Redis, // Assuming appropriate enum value
                IsMissing = false
            };
        }
        /// <summary>
        /// Creates a configuration object for connecting to Couchbase Lite.
        /// </summary>
        /// <returns>A configuration object for connecting to Couchbase Lite.</returns>
        public static ConnectionDriversConfig CreateCouchbaseLiteConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "eeade0d2-2372-40c2-bdec-aaab150ecda8",
                PackageName = "Couchbase.Lite",
                DriverClass = "Couchbase.Lite",
                version = "2.7.1.0",
                dllname = "Couchbase.Lite.dll",
                parameter1 = "Database",
                parameter2 = "DatabaseConfiguration",
                iconname = "couchbase.png",
                classHandler = "CouchbaseLiteDatasource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.RDBMS, // Assuming appropriate enum value
                DatasourceType = DataSourceType.Couchbase, // Assuming appropriate enum value
                IsMissing = false
            };
        }
        /// <summary>Creates a configuration object for Elasticsearch connection drivers.</summary>
        /// <returns>A configuration object for Elasticsearch connection drivers.</returns>
        public static ConnectionDriversConfig CreateElasticsearchConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "f9b81106-c01c-4d10-9f08-735088af953d",
                PackageName = "Elasticsearch.Net",
                DriverClass = "Elasticsearch.Net",
                version = "7.0.0.0",
                dllname = "Elasticsearch.Net.dll",
                parameter1 = "ElasticLowLevelClient",
                parameter2 = "ConnectionConfiguration",
                classHandler = "ElasticsearchDatasource",
                iconname = "elasticsearch.png",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.RDBMS, // Assuming appropriate enum value
                DatasourceType = DataSourceType.ElasticSearch, // Assuming appropriate enum value
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
                iconname = "Sqlite.png",
                classHandler = "SQLiteDataSource",
                ADOType = true,
                CreateLocal = true,
                InMemory = false,
                Favourite = true,
                DatasourceCategory = DatasourceCategory.RDBMS, // Assuming appropriate enum value
                DatasourceType = DataSourceType.SqlLite, // Assuming appropriate enum value
                IsMissing = false
            };
        }
        /// <summary>Creates a configuration object for RavenDB connection drivers.</summary>
        /// <returns>A configuration object for RavenDB connection drivers.</returns>
        public static ConnectionDriversConfig CreateRavenDBConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "30391117-229f-44d4-bb47-7f6ea1dba2c1",
                PackageName = "Raven.Client",
                DriverClass = "Raven.Client",
                version = "5.0.1.0",
                dllname = "Raven.Client",
                parameter1 = "Database",
                parameter2 = "Sesssion",
                parameter3 = "Collection",
                iconname = "ravendb.png",
                classHandler = "RavenDBDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = true,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.RDBMS, // Assuming appropriate enum value
                DatasourceType = DataSourceType.RavenDB, // Assuming appropriate enum value
                IsMissing = false
            };
        }
        /// <summary>
        /// Creates a configuration object for a CSV file reader connection driver.
        /// </summary>
        /// <returns>A configuration object for a CSV file reader connection driver.</returns>
        public static ConnectionDriversConfig CreateCSVFileReaderConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "a5c687c5-71b6-4f2c-b2d9-6291972763ea",
                PackageName = "FileReader",
                DriverClass = "FileReader",
                version = "1",
                dllname = "DataManagmentEngine",
                AdapterType = "DEFAULT",
                iconname = "csv.png",
                classHandler = "CSVDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                extensionstoHandle = "csv" ,
                DatasourceCategory = DatasourceCategory.FILE, // Assuming appropriate enum value
                DatasourceType = DataSourceType.CSV, // Assuming appropriate enum value
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
                iconname = "Firebird.png",
                classHandler = "FireBirdDataSource",
                ADOType = true,
                CreateLocal = false,
                InMemory = false,
                Favourite = true,
                DatasourceCategory = DatasourceCategory.RDBMS, // Assuming appropriate enum value
                DatasourceType = DataSourceType.FireBird, // Assuming appropriate enum value
                IsMissing = false
            };
        }
        /// <summary>Creates a configuration object for Cassandra connection drivers.</summary>
        /// <returns>A configuration object for Cassandra connection drivers.</returns>
        public static ConnectionDriversConfig CreateCassandraConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "47f2ed83-066c-41fd-96f6-1ba4d188c3b6",
                PackageName = "Cassandra",
                DriverClass = "Cassandra",
                version = "3.99.0.0",
                dllname = "Cassandra.dll",
                AdapterType = "Cassandra.Data.CqlDataAdapter",
                DbConnectionType = "Cassandra.Data.CqlConnection",
                DbTransactionType = "Cassandra.Data.CqlBatchTransaction",
                iconname = "Cassandra.png",
                classHandler = "CassandraDataSource",
                ADOType = true,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.RDBMS, // Assuming appropriate enum value
                DatasourceType = DataSourceType.Cassandra, // Assuming appropriate enum value
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
                iconname = "mysql.png",
                classHandler = "MySQLDataSource",
                ADOType = true,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.RDBMS, // Assuming appropriate enum value
                DatasourceType = DataSourceType.Mysql, // Assuming appropriate enum value
                IsMissing = false
            };
        }
        /// <summary>Creates a configuration object for MySqlConnector.</summary>
        /// <returns>A configuration object for MySqlConnector.</returns>
        public static ConnectionDriversConfig CreateMySqlConnectorConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "2b9bcb1d-aacf-4c5b-a282-70b2c0006ffe",
                PackageName = "MySqlConnector",
                DriverClass = "MySqlConnector",
                version = "2.0.0.0",
                dllname = "MySqlConnector.dll",
                AdapterType = "MySqlConnector.MySqlDataAdapter",
                CommandBuilderType = "MySqlConnector.MySqlCommandBuilder",
                DbConnectionType = "MySqlConnector.MySqlConnection",
                DbTransactionType = "MySqlConnector.MySqlTransaction",
                ConnectionString = "Server={Host};PORT={Port};Database={Database};Uid ={UserID}; Pwd = {Password};",
                iconname = "mysql.png",
                classHandler = "MySQLDataSource",
                ADOType = true,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.RDBMS, // Assuming appropriate enum value
                DatasourceType = DataSourceType.Mysql, // Assuming appropriate enum value
                IsMissing = false
            };
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
                iconname = "sqlserver.png",
                classHandler = "SQLServerDataSource",
                ADOType = true,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.RDBMS, // Assuming appropriate enum value
                DatasourceType = DataSourceType.SqlServer, // Assuming appropriate enum value
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
                iconname = "sqlserverCompact.png",
                classHandler = "SQLCompactDataSource",
                ADOType = true,
                CreateLocal = true,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.RDBMS, // Assuming appropriate enum value
                DatasourceType = DataSourceType.SqlCompact, // Assuming appropriate enum value
                IsMissing = false
            };
        }
        /// <summary>Creates a configuration object for connection drivers.</summary>
        /// <returns>A configuration object for connection drivers.</returns>
        public static ConnectionDriversConfig CreateDataViewConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "ad729953-9010-4d1c-9459-3d1a3fab2de8",
                PackageName = "DataViewReader",
                DriverClass = "DataViewReader",
                version = "1",
                dllname = "DataManagmentEngine",
                AdapterType = "DEFAULT",
                iconname = "dataview.png",
                classHandler = "DataViewDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.FILE, // Assuming appropriate enum value
                DatasourceType = DataSourceType.Json, // Assuming appropriate enum value
                IsMissing = false
            };
        }
        /// <summary>
        /// Creates a configuration object for a CSV data source.
        /// </summary>
        /// <returns>A configuration object for a CSV data source.</returns>
        public static ConnectionDriversConfig CreateCSVDataSourceConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "b88f871b-fd5b-4516-b1b3-65e2c54b3fe7",
                PackageName = "CSVDataSource",
                DriverClass = "CSVDataSource",
                version = "1",
                dllname = "DataManagmentEngine",
                AdapterType = "DEFAULT",
                iconname = "csv.png",
                classHandler = "CSVDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                extensionstoHandle = "csv",
                Favourite = true,
                DatasourceCategory = DatasourceCategory.FILE, // Assuming appropriate enum value
                DatasourceType = DataSourceType.CSV, // Assuming appropriate enum value
                IsMissing = false
            };
        }
        /// <summary>
        /// Creates a configuration object for a JSON data source connection driver.
        /// </summary>
        /// <returns>A configuration object for a JSON data source connection driver.</returns>
        public static ConnectionDriversConfig CreateJsonDataSourceConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "970bfedf-0503-474e-b936-79d2d66065c9",
                PackageName = "JSONSource",
                DriverClass = "JSONSource",
                version = "1",
                dllname = "DataManagmentEngine",
                AdapterType = "DEFAULT",
                iconname = "json.png",
                classHandler = "JsonDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                extensionstoHandle = "json",
                Favourite = true,
                DatasourceCategory = DatasourceCategory.FILE, // Assuming appropriate enum value
                DatasourceType = DataSourceType.Json, // Assuming appropriate enum value
                IsMissing = false
            };
        }
        /// <summary>
        /// Creates a configuration object for a text, xls, or csv file data source.
        /// </summary>
        /// <returns>A ConnectionDriversConfig object representing the configuration for the file data source.</returns>
        public static ConnectionDriversConfig CreateTxtXlsCSVFileSourceConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "ac76ffa3-bb78-49dc-bda8-e9b26c9633d2",
                PackageName = "TxtXlsCSVFileSource",
                DriverClass = "TxtXlsCSVFileSource",
                version = "1",
                dllname = "DataManagmentEngine",
                AdapterType = "DEFAULT",
                iconname = "xls.png",
                classHandler = "TxtXlsCSVFileSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                extensionstoHandle = "xls,xlsx",
                Favourite = true,
                DatasourceCategory = DatasourceCategory.FILE, // Assuming appropriate enum value
                DatasourceType = DataSourceType.Xls, // Assuming appropriate enum value
                IsMissing = false
            };
        }
        /// <summary>
        /// Creates a configuration object for a LiteDB data source connection driver.
        /// </summary>
        /// <returns>A configuration object for a LiteDB data source connection driver.</returns>
        public static ConnectionDriversConfig CreateLiteDBDataSourceConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "bfcb718d-608f-4164-86ee-24e35c689638",
                PackageName = "LiteDBDataSource",
                DriverClass = "LiteDBDataSource",
                version = "1",
                dllname = "LiteDB",
                ConnectionString = "{File}",
                iconname = "litedb.png",
                classHandler = "LiteDBDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = true,
                DatasourceCategory = DatasourceCategory.NOSQL, // Assuming appropriate enum value
                DatasourceType = DataSourceType.LiteDB, // Assuming appropriate enum value
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
                iconname = "oracle.png",
                classHandler = "OracleDataSource",
                ADOType = true,
                CreateLocal = false,
                InMemory = false,
                Favourite = true,
                DatasourceCategory = DatasourceCategory.RDBMS, // Assuming appropriate enum value
                DatasourceType = DataSourceType.Oracle, // Assuming appropriate enum value
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
                DatasourceCategory = DatasourceCategory.INMEMORY, // Assuming appropriate enum value
                DatasourceType = DataSourceType.DuckDB, // Assuming appropriate enum value
                DbConnectionType = "DuckDB.NET.Data.DuckDBConnection",
                DbTransactionType = "DuckDB.NET.Data.DuckDBTransaction",
                dllname = "DuckDB.NET.Data.dll",
                DriverClass = "DuckDB.NET.Data",
                Favourite = false,
                GuidID = "e4683996-bb84-48fb-91dc-eb6a4a93616f",
                iconname = "duckdb.png",
                ID = 0,
                InMemory = true,
                IsMissing = false,
                PackageName = "DuckDB.NET.Data",
                version = "0.8.1.0"
            };
        }

    }
}
