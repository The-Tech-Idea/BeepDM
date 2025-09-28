using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.DriversConfigurations;

namespace TheTechIdea.Beep.Caching.Examples
{
    /// <summary>
    /// Comprehensive example demonstrating the usage of Cache Connection Helpers in the Beep Data Management Engine.
    /// 
    /// This class provides complete examples of:
    /// - Registering cache connection configurations
    /// - Creating cache data source connections
    /// - Working with different cache types (in-memory, distributed, hybrid)
    /// - Configuring connection properties for various cache systems
    /// - Best practices for cache configuration and usage
    /// 
    /// The Cache Connection Helpers support various caching technologies including:
    /// - Built-in Beep InMemoryCache
    /// - Microsoft Extensions Memory Cache
    /// - Redis distributed cache
    /// - Hazelcast in-memory data grid
    /// - Apache Ignite cache
    /// - Hybrid and multi-level caching systems
    /// </summary>
    public class CacheConnectionHelperExample
    {
        #region Private Fields
        private IDMEEditor _dmeEditor;
        private IDMLogger _logger;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the CacheConnectionHelperExample class.
        /// </summary>
        /// <param name="dmeEditor">The data management editor instance</param>
        /// <param name="logger">The logger instance</param>
        public CacheConnectionHelperExample(IDMEEditor dmeEditor, IDMLogger logger)
        {
            _dmeEditor = dmeEditor;
            _logger = logger;
        }
        #endregion

        #region Public Example Methods

        /// <summary>
        /// Demonstrates all available cache connection configurations.
        /// This method shows how to retrieve and register all cache configurations supported by Beep.
        /// </summary>
        public void DemonstrateAllCacheConfigurations()
        {
            Console.WriteLine("=== Cache Connection Configurations Demo ===");

            // Get all cache configurations
            var cacheConfigs = ConnectionHelper.GetCacheConfigs();
            
            Console.WriteLine($"\nFound {cacheConfigs.Count} cache configurations:");
            
            foreach (var config in cacheConfigs)
            {
                Console.WriteLine($"\n  Cache: {config.classHandler}");
                Console.WriteLine($"    Type: {config.DatasourceType}");
                Console.WriteLine($"    Category: {config.DatasourceCategory}");
                Console.WriteLine($"    Package: {config.PackageName}");
                Console.WriteLine($"    Connection String Template: {config.ConnectionString}");
                Console.WriteLine($"    In-Memory: {config.InMemory}");
                Console.WriteLine($"    ADO Type: {config.ADOType}");
                Console.WriteLine($"    Icon: {config.iconname}");
            }

            // Register all cache configurations with DME Editor
            RegisterAllCacheConfigurations(cacheConfigs);
        }

        /// <summary>
        /// Demonstrates InMemoryCache configuration and usage.
        /// Shows how to set up and use the built-in Beep InMemoryCache data source.
        /// </summary>
        public void DemonstrateInMemoryCacheUsage()
        {
            Console.WriteLine("\n=== InMemoryCache Usage Demo ===");

            try
            {
                // 1. Get InMemoryCache configuration
                var inMemoryConfig = ConnectionHelper.CreateInMemoryCacheConfig();
                
                Console.WriteLine($"InMemoryCache Configuration:");
                Console.WriteLine($"  Class Handler: {inMemoryConfig.classHandler}");
                Console.WriteLine($"  Connection String Template: {inMemoryConfig.ConnectionString}");

                // 2. Register the configuration
                if (!_dmeEditor.ConfigEditor.DataDriversClasses.Any(d => d.classHandler == inMemoryConfig.classHandler))
                {
                    _dmeEditor.ConfigEditor.DataDriversClasses.Add(inMemoryConfig);
                    Console.WriteLine("  ✓ Configuration registered with DME Editor");
                }

                // 3. Create connection properties
                var connectionProps = new ConnectionProperties
                {
                    ConnectionName = "DemoInMemoryCache",
                    DatabaseType = DataSourceType.InMemoryCache,
                    Category = DatasourceCategory.INMEMORY,
                    ConnectionString = "CacheName=DemoCache;MaxItems=10000;ExpiryMinutes=60;CleanupInterval=5",
                    Host = "localhost",
                    Database = "DemoCache"
                };

                // 4. Add connection to DME Editor
                if (!_dmeEditor.ConfigEditor.DataConnections.Any(c => c.ConnectionName == connectionProps.ConnectionName))
                {
                    _dmeEditor.ConfigEditor.DataConnections.Add(connectionProps);
                    Console.WriteLine("  ✓ Connection properties added to DME Editor");
                }

                // 5. Test connection creation (if InMemoryCacheDataSource is available)
                TestCacheConnection("DemoInMemoryCache");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Error in InMemoryCache demo: {ex.Message}");
                _logger?.WriteLog($"InMemoryCache demo error: {ex.Message}");
            }
        }

        /// <summary>
        /// Demonstrates Redis cache configuration and connection setup.
        /// Shows how to configure Redis as a distributed cache data source.
        /// </summary>
        public void DemonstrateRedisCacheUsage()
        {
            Console.WriteLine("\n=== Redis Cache Usage Demo ===");

            try
            {
                // 1. Get Redis configuration
                var redisConfig = ConnectionHelper.CreateRedisCacheConfig();
                
                Console.WriteLine($"Redis Configuration:");
                Console.WriteLine($"  Class Handler: {redisConfig.classHandler}");
                Console.WriteLine($"  Package: {redisConfig.PackageName}");
                Console.WriteLine($"  Connection String Template: {redisConfig.ConnectionString}");

                // 2. Register the configuration
                if (!_dmeEditor.ConfigEditor.DataDriversClasses.Any(d => d.classHandler == redisConfig.classHandler))
                {
                    _dmeEditor.ConfigEditor.DataDriversClasses.Add(redisConfig);
                    Console.WriteLine("  ✓ Configuration registered with DME Editor");
                }

                // 3. Create connection properties for Redis
                var redisProps = new ConnectionProperties
                {
                    ConnectionName = "DemoRedisCache",
                    DatabaseType = DataSourceType.Redis,
                    Category = DatasourceCategory.NOSQL,
                    ConnectionString = "Host=localhost;Port=6379;Password=;Database=0;ConnectTimeout=5000",
                    Host = "localhost",
                    Port = 6379,
                    Password = "",
                    Database = "0",
                    UserID = "",
                    SchemaName = ""
                };

                // 4. Add connection to DME Editor
                if (!_dmeEditor.ConfigEditor.DataConnections.Any(c => c.ConnectionName == redisProps.ConnectionName))
                {
                    _dmeEditor.ConfigEditor.DataConnections.Add(redisProps);
                    Console.WriteLine("  ✓ Connection properties added to DME Editor");
                }

                // 5. Test connection creation (if Redis client is available)
                TestCacheConnection("DemoRedisCache");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Error in Redis demo: {ex.Message}");
                _logger?.WriteLog($"Redis demo error: {ex.Message}");
            }
        }

        /// <summary>
        /// Demonstrates Microsoft Memory Cache configuration.
        /// Shows how to set up Microsoft Extensions Memory Cache as a data source.
        /// </summary>
        public void DemonstrateMemoryCacheUsage()
        {
            Console.WriteLine("\n=== Microsoft Memory Cache Usage Demo ===");

            try
            {
                // 1. Get MemoryCache configuration
                var memoryCacheConfig = ConnectionHelper.CreateMemoryCacheConfig();
                
                Console.WriteLine($"MemoryCache Configuration:");
                Console.WriteLine($"  Class Handler: {memoryCacheConfig.classHandler}");
                Console.WriteLine($"  Package: {memoryCacheConfig.PackageName}");
                Console.WriteLine($"  Connection String Template: {memoryCacheConfig.ConnectionString}");

                // 2. Register the configuration
                if (!_dmeEditor.ConfigEditor.DataDriversClasses.Any(d => d.classHandler == memoryCacheConfig.classHandler))
                {
                    _dmeEditor.ConfigEditor.DataDriversClasses.Add(memoryCacheConfig);
                    Console.WriteLine("  ✓ Configuration registered with DME Editor");
                }

                // 3. Create connection properties
                var memoryCacheProps = new ConnectionProperties
                {
                    ConnectionName = "DemoMemoryCache",
                    DatabaseType = DataSourceType.InMemoryCache,
                    Category = DatasourceCategory.INMEMORY,
                    ConnectionString = "SizeLimit=104857600;CompactionPercentage=0.25", // 100MB limit, 25% compaction
                    Host = "localhost",
                    Database = "MemoryCache"
                };

                // 4. Add connection to DME Editor
                if (!_dmeEditor.ConfigEditor.DataConnections.Any(c => c.ConnectionName == memoryCacheProps.ConnectionName))
                {
                    _dmeEditor.ConfigEditor.DataConnections.Add(memoryCacheProps);
                    Console.WriteLine("  ✓ Connection properties added to DME Editor");
                }

                TestCacheConnection("DemoMemoryCache");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Error in MemoryCache demo: {ex.Message}");
                _logger?.WriteLog($"MemoryCache demo error: {ex.Message}");
            }
        }

        /// <summary>
        /// Demonstrates Hybrid Cache configuration.
        /// Shows how to set up a multi-level cache system with L1 and L2 caches.
        /// </summary>
        public void DemonstrateHybridCacheUsage()
        {
            Console.WriteLine("\n=== Hybrid Cache Usage Demo ===");

            try
            {
                // 1. Get Hybrid Cache configuration
                var hybridConfig = ConnectionHelper.CreateHybridCacheConfig();
                
                Console.WriteLine($"Hybrid Cache Configuration:");
                Console.WriteLine($"  Class Handler: {hybridConfig.classHandler}");
                Console.WriteLine($"  Connection String Template: {hybridConfig.ConnectionString}");

                // 2. Register the configuration
                if (!_dmeEditor.ConfigEditor.DataDriversClasses.Any(d => d.classHandler == hybridConfig.classHandler))
                {
                    _dmeEditor.ConfigEditor.DataDriversClasses.Add(hybridConfig);
                    Console.WriteLine("  ✓ Configuration registered with DME Editor");
                }

                // 3. Create connection properties
                var hybridProps = new ConnectionProperties
                {
                    ConnectionName = "DemoHybridCache",
                    DatabaseType = DataSourceType.InMemoryCache,
                    Category = DatasourceCategory.INMEMORY,
                    ConnectionString = "L1Cache=Memory;L2Cache=Redis;L1MaxItems=1000;L2MaxItems=100000",
                    Host = "localhost",
                    Database = "HybridCache"
                };

                // 4. Add connection to DME Editor
                if (!_dmeEditor.ConfigEditor.DataConnections.Any(c => c.ConnectionName == hybridProps.ConnectionName))
                {
                    _dmeEditor.ConfigEditor.DataConnections.Add(hybridProps);
                    Console.WriteLine("  ✓ Connection properties added to DME Editor");
                }

                TestCacheConnection("DemoHybridCache");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Error in Hybrid Cache demo: {ex.Message}");
                _logger?.WriteLog($"Hybrid Cache demo error: {ex.Message}");
            }
        }

        /// <summary>
        /// Demonstrates all distributed cache configurations.
        /// Shows configurations for Redis, Hazelcast, Apache Ignite, and other distributed caches.
        /// </summary>
        public void DemonstrateDistributedCacheConfigurations()
        {
            Console.WriteLine("\n=== Distributed Cache Configurations Demo ===");

            var distributedCaches = new[]
            {
                ConnectionHelper.CreateRedisCacheConfig(),
                ConnectionHelper.CreateHazelcastCacheConfig(),
                ConnectionHelper.CreateApacheIgniteCacheConfig(),
                ConnectionHelper.CreateNCacheConfig(),
                ConnectionHelper.CreateCouchbaseCacheConfig()
            };

            foreach (var config in distributedCaches)
            {
                Console.WriteLine($"\n  {config.classHandler}:");
                Console.WriteLine($"    Type: {config.DatasourceType}");
                Console.WriteLine($"    Category: {config.DatasourceCategory}");
                Console.WriteLine($"    In-Memory: {config.InMemory}");
                Console.WriteLine($"    Connection Template: {config.ConnectionString}");
                Console.WriteLine($"    Package: {config.PackageName}");

                // Register with DME Editor
                if (!_dmeEditor.ConfigEditor.DataDriversClasses.Any(d => d.GuidID == config.GuidID))
                {
                    _dmeEditor.ConfigEditor.DataDriversClasses.Add(config);
                    Console.WriteLine($"    ✓ Registered with DME Editor");
                }
            }
        }

        /// <summary>
        /// Demonstrates cache configuration validation and best practices.
        /// Shows how to validate cache configurations and connection strings.
        /// </summary>
        public void DemonstrateCacheConfigurationValidation()
        {
            Console.WriteLine("\n=== Cache Configuration Validation Demo ===");

            try
            {
                // 1. Validate connection string templates
                var cacheConfigs = ConnectionHelper.GetCacheConfigs();
                
                Console.WriteLine("Validating cache configurations...");
                
                foreach (var config in cacheConfigs)
                {
                    Console.WriteLine($"\n  Validating {config.classHandler}:");
                    
                    // Check required fields
                    var isValid = true;
                    
                    if (string.IsNullOrEmpty(config.classHandler))
                    {
                        Console.WriteLine("    ✗ Missing classHandler");
                        isValid = false;
                    }
                    
                    if (string.IsNullOrEmpty(config.ConnectionString))
                    {
                        Console.WriteLine("    ✗ Missing ConnectionString template");
                        isValid = false;
                    }
                    
                    if (config.DatasourceType == DataSourceType.Unknown)
                    {
                        Console.WriteLine("    ✗ Invalid DatasourceType");
                        isValid = false;
                    }
                    
                    if (string.IsNullOrEmpty(config.PackageName))
                    {
                        Console.WriteLine("    ✗ Missing PackageName");
                        isValid = false;
                    }

                    if (isValid)
                    {
                        Console.WriteLine("    ✓ Configuration is valid");
                        
                        // Demonstrate connection string parameter extraction
                        var parameters = ExtractConnectionStringParameters(config.ConnectionString);
                        if (parameters.Any())
                        {
                            Console.WriteLine($"    Parameters: {string.Join(", ", parameters)}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Error in validation demo: {ex.Message}");
                _logger?.WriteLog($"Cache validation demo error: {ex.Message}");
            }
        }

        /// <summary>
        /// Runs all cache connection helper demonstrations.
        /// </summary>
        public void RunAllDemonstrations()
        {
            Console.WriteLine("=== Running All Cache Connection Helper Demonstrations ===");

            DemonstrateAllCacheConfigurations();
            DemonstrateInMemoryCacheUsage();
            DemonstrateRedisCacheUsage();
            DemonstrateMemoryCacheUsage();
            DemonstrateHybridCacheUsage();
            DemonstrateDistributedCacheConfigurations();
            DemonstrateCacheConfigurationValidation();

            Console.WriteLine("\n=== All Demonstrations Completed ===");
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Registers all cache configurations with the DME Editor.
        /// </summary>
        /// <param name="cacheConfigs">List of cache configurations to register</param>
        private void RegisterAllCacheConfigurations(List<ConnectionDriversConfig> cacheConfigs)
        {
            Console.WriteLine("\nRegistering cache configurations with DME Editor...");
            
            var registered = 0;
            var skipped = 0;

            foreach (var config in cacheConfigs)
            {
                // Check if already registered
                if (!_dmeEditor.ConfigEditor.DataDriversClasses.Any(d => d.GuidID == config.GuidID))
                {
                    _dmeEditor.ConfigEditor.DataDriversClasses.Add(config);
                    registered++;
                }
                else
                {
                    skipped++;
                }
            }

            Console.WriteLine($"  ✓ Registered: {registered} configurations");
            Console.WriteLine($"  ➤ Skipped: {skipped} already registered");
        }

        /// <summary>
        /// Tests cache connection creation (mock test since actual data sources may not be available).
        /// </summary>
        /// <param name="connectionName">Name of the connection to test</param>
        private void TestCacheConnection(string connectionName)
        {
            try
            {
                Console.WriteLine($"  Testing connection '{connectionName}'...");
                
                // In a real scenario, you would call:
                // var dataSource = _dmeEditor.GetDataSource(connectionName);
                // var state = dataSource.Openconnection();
                
                // For demo purposes, we'll just verify the connection exists
                var connection = _dmeEditor.ConfigEditor.DataConnections
                    .FirstOrDefault(c => c.ConnectionName == connectionName);
                
                if (connection != null)
                {
                    Console.WriteLine($"  ✓ Connection configuration found");
                    Console.WriteLine($"    Connection String: {connection.ConnectionString}");
                    Console.WriteLine($"    Database Type: {connection.DatabaseType}");
                    Console.WriteLine($"    Category: {connection.Category}");
                }
                else
                {
                    Console.WriteLine($"  ✗ Connection configuration not found");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Connection test failed: {ex.Message}");
                _logger?.WriteLog($"Connection test error for '{connectionName}': {ex.Message}");
            }
        }

        /// <summary>
        /// Extracts parameter names from a connection string template.
        /// </summary>
        /// <param name="connectionStringTemplate">Connection string template with {Parameter} placeholders</param>
        /// <returns>List of parameter names</returns>
        private List<string> ExtractConnectionStringParameters(string connectionStringTemplate)
        {
            var parameters = new List<string>();
            
            if (string.IsNullOrEmpty(connectionStringTemplate))
                return parameters;

            // Find all {Parameter} patterns
            var matches = System.Text.RegularExpressions.Regex.Matches(
                connectionStringTemplate, @"\{([^}]+)\}");
                
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    parameters.Add(match.Groups[1].Value);
                }
            }

            return parameters.Distinct().ToList();
        }

        #endregion
    }

    /// <summary>
    /// Static helper class for running cache connection helper examples.
    /// </summary>
    public static class CacheConnectionHelperExampleRunner
    {
        /// <summary>
        /// Runs the cache connection helper example with a provided DME Editor and Logger.
        /// </summary>
        /// <param name="dmeEditor">The DME Editor instance</param>
        /// <param name="logger">The Logger instance</param>
        public static void RunExample(IDMEEditor dmeEditor, IDMLogger logger)
        {
            var example = new CacheConnectionHelperExample(dmeEditor, logger);
            example.RunAllDemonstrations();
        }

        /// <summary>
        /// Runs a specific cache demonstration.
        /// </summary>
        /// <param name="dmeEditor">The DME Editor instance</param>
        /// <param name="logger">The Logger instance</param>
        /// <param name="demonstrationType">The type of demonstration to run</param>
        public static void RunSpecificDemo(IDMEEditor dmeEditor, IDMLogger logger, string demonstrationType)
        {
            var example = new CacheConnectionHelperExample(dmeEditor, logger);
            
            switch (demonstrationType.ToLower())
            {
                case "inmemory":
                    example.DemonstrateInMemoryCacheUsage();
                    break;
                case "redis":
                    example.DemonstrateRedisCacheUsage();
                    break;
                case "memory":
                    example.DemonstrateMemoryCacheUsage();
                    break;
                case "hybrid":
                    example.DemonstrateHybridCacheUsage();
                    break;
                case "distributed":
                    example.DemonstrateDistributedCacheConfigurations();
                    break;
                case "validation":
                    example.DemonstrateCacheConfigurationValidation();
                    break;
                case "all":
                default:
                    example.RunAllDemonstrations();
                    break;
            }
        }
    }
}