using System;
using System.Collections.Generic;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Helpers
{
    /// <summary>
    /// Partial class for Cache-based connection configurations
    /// </summary>
    public static partial class ConnectionHelper
    {
        /// <summary>
        /// Gets all Cache connection configurations
        /// </summary>
        /// <returns>List of Cache connection configurations</returns>
        public static List<ConnectionDriversConfig> GetCacheConfigs()
        {
            var configs = new List<ConnectionDriversConfig>
            {
                CreateInMemoryCacheConfig(),
                CreateCachedMemoryConfig(),
                CreateDistributedCacheConfig(),
                CreateHybridCacheConfig(),
                CreateL1L2CacheConfig(),
                CreateMemoryCacheConfig(),
                CreateRedisCacheConfig(),
                CreateNCacheConfig(),
                CreateCouchbaseCacheConfig(),
                CreateHazelcastCacheConfig(),
                CreateApacheIgniteCacheConfig(),
                CreateInfinispanConfig(),
                CreateEhCacheConfig(),
                CreateCaffeineCacheConfig()
            };

            return configs;
        }

        /// <summary>Creates a configuration object for InMemoryCache connection drivers.</summary>
        /// <returns>A configuration object for InMemoryCache connection drivers.</returns>
        public static ConnectionDriversConfig CreateInMemoryCacheConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "TheTechIdea.Beep.InMemoryCache",
                DriverClass = "TheTechIdea.Beep.Caching.DataSources",
                version = "1.0.0.0",
                dllname = "DataManagementEngine.dll",
                ConnectionString = "CacheName={Database};MaxItems={MaxItems};ExpiryMinutes={ExpiryMinutes};CleanupInterval={CleanupInterval}",
                iconname = "inmemorycache.svg",
                classHandler = "InMemoryCacheDataSource",
                ADOType = false,
                CreateLocal = true,
                InMemory = true,
                Favourite = true,
                DatasourceCategory = DatasourceCategory.INMEMORY,
                DatasourceType = DataSourceType.InMemoryCache,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "TheTechIdea.Beep.InMemoryCache",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for CachedMemory connection drivers.</summary>
        /// <returns>A configuration object for CachedMemory connection drivers.</returns>
        public static ConnectionDriversConfig CreateCachedMemoryConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "TheTechIdea.Beep.CachedMemory",
                DriverClass = "TheTechIdea.Beep.Caching.DataSources",
                version = "1.0.0.0",
                dllname = "DataManagementEngine.dll",
                ConnectionString = "CacheName={Database};MemoryLimit={MemoryLimit};CompressionEnabled={CompressionEnabled}",
                iconname = "cachedmemory.svg",
                classHandler = "CachedMemoryDataSource",
                ADOType = false,
                CreateLocal = true,
                InMemory = true,
                Favourite = true,
                DatasourceCategory = DatasourceCategory.INMEMORY,
                DatasourceType = DataSourceType.CachedMemory,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "TheTechIdea.Beep.CachedMemory",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Distributed Cache connection drivers.</summary>
        /// <returns>A configuration object for Distributed Cache connection drivers.</returns>
        public static ConnectionDriversConfig CreateDistributedCacheConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Microsoft.Extensions.Caching.Distributed",
                DriverClass = "Microsoft.Extensions.Caching.Distributed",
                version = "8.0.0.0",
                dllname = "Microsoft.Extensions.Caching.Abstractions.dll",
                ConnectionString = "Provider={Provider};ConnectionString={ConnectionString}",
                iconname = "distributedcache.svg",
                classHandler = "DistributedCacheDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = true,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.INMEMORY,
                DatasourceType = DataSourceType.InMemoryCache,
                IsMissing = false,
                NuggetVersion = "8.0.0.0",
                NuggetSource = "Microsoft.Extensions.Caching.Distributed",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Hybrid Cache connection drivers.</summary>
        /// <returns>A configuration object for Hybrid Cache connection drivers.</returns>
        public static ConnectionDriversConfig CreateHybridCacheConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "TheTechIdea.Beep.HybridCache",
                DriverClass = "TheTechIdea.Beep.Caching.Providers",
                version = "1.0.0.0",
                dllname = "DataManagementEngine.dll",
                ConnectionString = "L1Cache={L1Cache};L2Cache={L2Cache};L1MaxItems={L1MaxItems};L2MaxItems={L2MaxItems}",
                iconname = "hybridcache.svg",
                classHandler = "HybridCacheDataSource",
                ADOType = false,
                CreateLocal = true,
                InMemory = true,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.INMEMORY,
                DatasourceType = DataSourceType.InMemoryCache,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "TheTechIdea.Beep.HybridCache",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for L1L2 Cache connection drivers.</summary>
        /// <returns>A configuration object for L1L2 Cache connection drivers.</returns>
        public static ConnectionDriversConfig CreateL1L2CacheConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "TheTechIdea.Beep.L1L2Cache",
                DriverClass = "TheTechIdea.Beep.Caching.L1L2",
                version = "1.0.0.0",
                dllname = "DataManagementEngine.dll",
                ConnectionString = "L1Size={L1Size};L2Size={L2Size};EvictionPolicy={EvictionPolicy}",
                iconname = "l1l2cache.svg",
                classHandler = "L1L2CacheDataSource",
                ADOType = false,
                CreateLocal = true,
                InMemory = true,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.INMEMORY,
                DatasourceType = DataSourceType.InMemoryCache,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "TheTechIdea.Beep.L1L2Cache",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for MemoryCache connection drivers.</summary>
        /// <returns>A configuration object for MemoryCache connection drivers.</returns>
        public static ConnectionDriversConfig CreateMemoryCacheConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Microsoft.Extensions.Caching.Memory",
                DriverClass = "Microsoft.Extensions.Caching.Memory",
                version = "8.0.0.0",
                dllname = "Microsoft.Extensions.Caching.Memory.dll",
                ConnectionString = "SizeLimit={SizeLimit};CompactionPercentage={CompactionPercentage}",
                iconname = "memorycache.svg",
                classHandler = "MemoryCacheDataSource",
                ADOType = false,
                CreateLocal = true,
                InMemory = true,
                Favourite = true,
                DatasourceCategory = DatasourceCategory.INMEMORY,
                DatasourceType = DataSourceType.InMemoryCache,
                IsMissing = false,
                NuggetVersion = "8.0.0.0",
                NuggetSource = "Microsoft.Extensions.Caching.Memory",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Redis Cache connection drivers.</summary>
        /// <returns>A configuration object for Redis Cache connection drivers.</returns>
        public static ConnectionDriversConfig CreateRedisCacheConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "StackExchange.Redis",
                DriverClass = "StackExchange.Redis",
                version = "2.7.4.0",
                dllname = "StackExchange.Redis.dll",
                ConnectionString = "Host={Host};Port={Port};Password={Password};Database={Database};ConnectTimeout={Timeout}",
                iconname = "redis.svg",
                classHandler = "RedisCacheDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = true,
                DatasourceCategory = DatasourceCategory.NOSQL,
                DatasourceType = DataSourceType.Redis,
                IsMissing = false,
                NuggetVersion = "2.7.4.0",
                NuggetSource = "StackExchange.Redis",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for NCache connection drivers.</summary>
        /// <returns>A configuration object for NCache connection drivers.</returns>
        public static ConnectionDriversConfig CreateNCacheConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Alachisoft.NCache",
                DriverClass = "Alachisoft.NCache.Client",
                version = "5.3.0.0",
                dllname = "Alachisoft.NCache.Client.dll",
                ConnectionString = "CacheName={Database};Server={Host};Port={Port}",
                iconname = "ncache.svg",
                classHandler = "NCacheDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = true,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.INMEMORY,
                DatasourceType = DataSourceType.InMemoryCache,
                IsMissing = false,
                NuggetVersion = "5.3.0.0",
                NuggetSource = "Alachisoft.NCache",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Couchbase Cache connection drivers.</summary>
        /// <returns>A configuration object for Couchbase Cache connection drivers.</returns>
        public static ConnectionDriversConfig CreateCouchbaseCacheConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "CouchbaseNetClient",
                DriverClass = "Couchbase.Extensions.Caching",
                version = "3.4.0.0",
                dllname = "Couchbase.Extensions.Caching.dll",
                ConnectionString = "ConnectionString={Host};Username={UserID};Password={Password};BucketName={Database}",
                iconname = "couchbase.svg",
                classHandler = "CouchbaseCacheDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.NOSQL,
                DatasourceType = DataSourceType.Couchbase,
                IsMissing = false,
                NuggetVersion = "3.4.0.0",
                NuggetSource = "CouchbaseNetClient",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Hazelcast Cache connection drivers.</summary>
        /// <returns>A configuration object for Hazelcast Cache connection drivers.</returns>
        public static ConnectionDriversConfig CreateHazelcastCacheConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Hazelcast.Net",
                DriverClass = "Hazelcast.Net",
                version = "5.0.0.0",
                dllname = "Hazelcast.Net.dll",
                ConnectionString = "ClusterName={Database};Host={Host};Port={Port}",
                iconname = "hazelcast.svg",
                classHandler = "HazelcastCacheDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = true,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.INMEMORY,
                DatasourceType = DataSourceType.Hazelcast,
                IsMissing = false,
                NuggetVersion = "5.0.0.0",
                NuggetSource = "Hazelcast.Net",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Apache Ignite Cache connection drivers.</summary>
        /// <returns>A configuration object for Apache Ignite Cache connection drivers.</returns>
        public static ConnectionDriversConfig CreateApacheIgniteCacheConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Apache.Ignite",
                DriverClass = "Apache.Ignite",
                version = "2.15.0.0",
                dllname = "Apache.Ignite.Core.dll",
                ConnectionString = "Host={Host};Port={Port};ClientMode={ClientMode}",
                iconname = "ignite.svg",
                classHandler = "ApacheIgniteCacheDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = true,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.INMEMORY,
                DatasourceType = DataSourceType.ApacheIgnite,
                IsMissing = false,
                NuggetVersion = "2.15.0.0",
                NuggetSource = "Apache.Ignite",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Infinispan connection drivers.</summary>
        /// <returns>A configuration object for Infinispan connection drivers.</returns>
        public static ConnectionDriversConfig CreateInfinispanConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Infinispan.HotRod",
                DriverClass = "Infinispan.HotRod.Client",
                version = "14.0.0.0",
                dllname = "Infinispan.HotRod.dll",
                ConnectionString = "Host={Host};Port={Port};CacheName={Database}",
                iconname = "infinispan.svg",
                classHandler = "InfinispanDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = true,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.INMEMORY,
                DatasourceType = DataSourceType.InMemoryCache,
                IsMissing = false,
                NuggetVersion = "14.0.0.0",
                NuggetSource = "Infinispan.HotRod",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for EhCache connection drivers.</summary>
        /// <returns>A configuration object for EhCache connection drivers.</returns>
        public static ConnectionDriversConfig CreateEhCacheConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "EhCache.Net",
                DriverClass = "EhCache.Net",
                version = "1.0.0.0",
                dllname = "EhCache.Net.dll",
                ConnectionString = "ConfigFile={File};CacheName={Database}",
                iconname = "ehcache.svg",
                classHandler = "EhCacheDataSource",
                ADOType = false,
                CreateLocal = true,
                InMemory = true,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.INMEMORY,
                DatasourceType = DataSourceType.InMemoryCache,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "EhCache.Net",
                NuggetMissing = false
            };
        }

        /// <summary>Creates a configuration object for Caffeine Cache connection drivers.</summary>
        /// <returns>A configuration object for Caffeine Cache connection drivers.</returns>
        public static ConnectionDriversConfig CreateCaffeineCacheConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Caffeine.Cache",
                DriverClass = "Caffeine.Cache",
                version = "1.0.0.0",
                dllname = "Caffeine.Cache.dll",
                ConnectionString = "MaxSize={MaxSize};ExpireAfterWrite={ExpireAfterWrite};ExpireAfterAccess={ExpireAfterAccess}",
                iconname = "caffeine.svg",
                classHandler = "CaffeineCacheDataSource",
                ADOType = false,
                CreateLocal = true,
                InMemory = true,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.INMEMORY,
                DatasourceType = DataSourceType.InMemoryCache,
                IsMissing = false,
                NuggetVersion = "1.0.0.0",
                NuggetSource = "Caffeine.Cache",
                NuggetMissing = false
            };
        }
    }
}