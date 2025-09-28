using System;
using System.Collections.Generic;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Helpers
{
    /// <summary>
    /// Partial class for In-Memory database connection configurations
    /// </summary>
    public static partial class ConnectionHelper
    {
        /// <summary>
        /// Gets all In-Memory connection configurations
        /// </summary>
        /// <returns>List of In-Memory connection configurations</returns>
        public static List<ConnectionDriversConfig> GetInMemoryConfigs()
        {
            var configs = new List<ConnectionDriversConfig>
            {
                CreateSQLiteMemoryConfig(),
                CreateMauiSQLiteConfig(),
                CreateDuckDBMemoryConfig(),
                CreateRealIMMemoryConfig(),
                CreateHadoopConfig(),
                CreatePetastormConfig(),
                CreateRocketSetConfig(),
                CreateApacheIgniteConfig(),
                CreateHazelcastConfig(),
                CreateRedisMemoryConfig(),
                CreateMemcachedConfig(),
                CreateGridGainConfig(),
                CreateChronicleMapConfig(),
                CreateH2DatabaseConfig()
            };

            return configs;
        }

        /// <summary>Creates a configuration object for SQLite in-memory connection drivers.</summary>
        /// <returns>A configuration object for SQLite in-memory connection drivers.</returns>
        public static ConnectionDriversConfig CreateSQLiteMemoryConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "System.Data.SQLite",
                DriverClass = "System.Data.SQLite",
                version = "1.0.113.0",
                dllname = "System.Data.SQLite.dll",
                AdapterType = "System.Data.SQLite.SQLiteDataAdapter",
                CommandBuilderType = "System.Data.SQLite.SQLiteCommandBuilder",
                DbConnectionType = "System.Data.SQLite.SQLiteConnection",
                DbTransactionType = "System.Data.SQLite.SQLiteTransaction2",
                ConnectionString = "Data Source=:memory:;version=3",
                iconname = "sqlite-memory.svg",
                classHandler = "SQLiteMemoryDataSource",
                ADOType = true,
                CreateLocal = false,
                InMemory = true,
                Favourite = true,
                DatasourceCategory = DatasourceCategory.INMEMORY,
                DatasourceType = DataSourceType.SqlLite,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for SQLite connection drivers.</summary>
        /// <returns>A configuration object for SQLite connection drivers.</returns>
        public static ConnectionDriversConfig CreateMauiSQLiteConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "b68a607a-8d54-4ca1-b8e3-0feabe3f5590",
                PackageName = "TheTechIdea.Beep.Maui.DataSource.Sqlite",
                DriverClass = "TheTechIdea.Beep.Maui.DataSource.Sqlite",
                version = "1.0.113.0",
                dllname = "TheTechIdea.Beep.Maui.DataSource.Sqlite.dll",
                ConnectionString = "{File}",
                iconname = "Sqlite.svg",
                classHandler = "SQLiteMauiDataSource",
                ADOType = false,
                CreateLocal = true,
                InMemory = false,
                Favourite = true,
                extensionstoHandle = "s3db",
                DatasourceCategory = DatasourceCategory.INMEMORY,
                DatasourceType = DataSourceType.SqlLite,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for DuckDB in-memory connection drivers.</summary>
        /// <returns>A configuration object for DuckDB in-memory connection drivers.</returns>
        public static ConnectionDriversConfig CreateDuckDBMemoryConfig()
        {
            return new ConnectionDriversConfig
            {
                ADOType = true,
                classHandler = "DuckDBMemoryDataSource",
                CreateLocal = false,
                DatasourceCategory = DatasourceCategory.INMEMORY,
                DatasourceType = DataSourceType.DuckDB,
                DbConnectionType = "DuckDB.NET.Data.DuckDBConnection",
                DbTransactionType = "DuckDB.NET.Data.DuckDBTransaction",
                dllname = "DuckDB.NET.Data.dll",
                DriverClass = "DuckDB.NET.Data",
                Favourite = true,
                GuidID = Guid.NewGuid().ToString(),
                iconname = "duckdb-memory.svg",
                ID = 0,
                InMemory = true,
                IsMissing = false,
                PackageName = "DuckDB.NET.Data",
                ConnectionString = ":memory:",
                version = "0.8.1.0"
            };
        }

        /// <summary>Creates a configuration object for RealIM in-memory connection drivers.</summary>
        /// <returns>A configuration object for RealIM in-memory connection drivers.</returns>
        public static ConnectionDriversConfig CreateRealIMMemoryConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "RealIM",
                DriverClass = "RealIM",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.RealIMDataSource.dll",
                ConnectionString = "BufferSize={BufferSize};CompressionLevel={CompressionLevel};",
                iconname = "realim.svg",
                classHandler = "RealIMDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = true,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.INMEMORY,
                DatasourceType = DataSourceType.RealIM,
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
                iconname = "hadoop.svg",
                classHandler = "HadoopDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.BigData,
                DatasourceType = DataSourceType.Hadoop,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for Petastorm connection drivers.</summary>
        /// <returns>A configuration object for Petastorm connection drivers.</returns>
        public static ConnectionDriversConfig CreatePetastormConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Petastorm",
                DriverClass = "Petastorm",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.PetastormDataSource.dll",
                ConnectionString = "WorkersCount={WorkersCount};RowGroupSizeBytes={RowGroupSizeBytes};",
                iconname = "petastorm.svg",
                classHandler = "PetastormDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = true,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.INMEMORY,
                DatasourceType = DataSourceType.Petastorm,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for RocketSet connection drivers.</summary>
        /// <returns>A configuration object for RocketSet connection drivers.</returns>
        public static ConnectionDriversConfig CreateRocketSetConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "RocketSet",
                DriverClass = "RocketSet",
                version = "1.0.0.0",
                dllname = "TheTechIdea.Beep.RocketSetDataSource.dll",
                ConnectionString = "MemoryLimit={MemoryLimit};CacheSize={CacheSize};",
                iconname = "rocketset.svg",
                classHandler = "RocketSetDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = true,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.INMEMORY,
                DatasourceType = DataSourceType.RocketSet,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for Apache Ignite connection drivers.</summary>
        /// <returns>A configuration object for Apache Ignite connection drivers.</returns>
        public static ConnectionDriversConfig CreateApacheIgniteConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Apache.Ignite",
                DriverClass = "Apache.Ignite",
                version = "2.14.0.0",
                dllname = "Apache.Ignite.Core.dll",
                ConnectionString = "Host={Host};Port={Port};",
                iconname = "ignite.svg",
                classHandler = "ApacheIgniteDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = true,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.INMEMORY,
                DatasourceType = DataSourceType.ApacheIgnite,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for Hazelcast connection drivers.</summary>
        /// <returns>A configuration object for Hazelcast connection drivers.</returns>
        public static ConnectionDriversConfig CreateHazelcastConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Hazelcast.Net",
                DriverClass = "Hazelcast.Net",
                version = "5.0.0.0",
                dllname = "Hazelcast.Net.dll",
                ConnectionString = "Host={Host};Port={Port};ClusterName={Database};",
                iconname = "hazelcast.svg",
                classHandler = "HazelcastDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = true,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.INMEMORY,
                DatasourceType = DataSourceType.Hazelcast,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for Redis in-memory connection drivers.</summary>
        /// <returns>A configuration object for Redis in-memory connection drivers.</returns>
        public static ConnectionDriversConfig CreateRedisMemoryConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "StackExchange.Redis",
                DriverClass = "StackExchange.Redis",
                version = "2.2.4.0",
                dllname = "StackExchange.Redis.dll",
                AdapterType = "IDatabase",
                CommandBuilderType = "ISubscriber",
                DbConnectionType = "ConnectionMultiplexer",
                ConnectionString = "Configuration={Host}:{Port};",
                iconname = "redis-memory.svg",
                classHandler = "RedisMemoryDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = true,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.INMEMORY,
                DatasourceType = DataSourceType.Redis,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for Memcached connection drivers.</summary>
        /// <returns>A configuration object for Memcached connection drivers.</returns>
        public static ConnectionDriversConfig CreateMemcachedConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "EnyimMemcached",
                DriverClass = "EnyimMemcached",
                version = "3.0.0.0",
                dllname = "EnyimMemcached.dll",
                ConnectionString = "Host={Host};Port={Port};",
                iconname = "memcached.svg",
                classHandler = "MemcachedDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = true,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.INMEMORY,
                DatasourceType = DataSourceType.Memcached,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for GridGain connection drivers.</summary>
        /// <returns>A configuration object for GridGain connection drivers.</returns>
        public static ConnectionDriversConfig CreateGridGainConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "GridGain.Net",
                DriverClass = "GridGain.Net",
                version = "8.8.0.0",
                dllname = "GridGain.Net.dll",
                ConnectionString = "Host={Host};Port={Port};",
                iconname = "gridgain.svg",
                classHandler = "GridGainDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = true,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.INMEMORY,
                DatasourceType = DataSourceType.GridGain,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for Chronicle Map connection drivers.</summary>
        /// <returns>A configuration object for Chronicle Map connection drivers.</returns>
        public static ConnectionDriversConfig CreateChronicleMapConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "ChronicleMap.Net",
                DriverClass = "ChronicleMap.Net",
                version = "1.0.0.0",
                dllname = "ChronicleMap.Net.dll",
                ConnectionString = "FilePath={File};",
                iconname = "chroniclemap.svg",
                classHandler = "ChronicleMapDataSource",
                ADOType = false,
                CreateLocal = true,
                InMemory = true,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.INMEMORY,
                DatasourceType = DataSourceType.ChronicleMap,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for H2 Database connection drivers.</summary>
        /// <returns>A configuration object for H2 Database connection drivers.</returns>
        public static ConnectionDriversConfig CreateH2DatabaseConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "H2.Net",
                DriverClass = "H2.Net",
                version = "2.0.0.0",
                dllname = "H2.Net.dll",
                ConnectionString = "jdbc:h2:mem:{Database};DB_CLOSE_DELAY=-1;DB_CLOSE_ON_EXIT=FALSE",
                iconname = "h2.svg",
                classHandler = "H2DatabaseDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = true,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.INMEMORY,
                DatasourceType = DataSourceType.H2Database,
                IsMissing = false
            };
        }
    }
}