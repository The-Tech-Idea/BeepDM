using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using TheTechIdea.Beep.Caching;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Caching.Examples
{
    /// <summary>
    /// Comprehensive example demonstrating the InMemoryCacheDataSource functionality.
    /// 
    /// This class provides complete examples of:
    /// - Initializing and configuring InMemoryCacheDataSource for in-memory data operations
    /// - Creating and managing entities with automatic schema discovery
    /// - Performing full CRUD operations (Create, Read, Update, Delete) on cached data
    /// - Advanced filtering and querying with multiple operators (equals, contains, greater than, etc.)
    /// - Paging support for large datasets with configurable page sizes
    /// - Entity structure management including custom entity creation
    /// - Integration with CacheManager for advanced caching scenarios
    /// - Performance testing and benchmarking of cache operations
    /// - Statistics collection and monitoring of cache performance
    /// - Proper resource cleanup and disposal patterns
    /// 
    /// The InMemoryCacheDataSource serves as a high-performance, thread-safe in-memory data store
    /// that implements the full IDataSource interface, making it compatible with the entire
    /// Beep data management ecosystem while providing microsecond-level response times.
    /// 
    /// Key Features Demonstrated:
    /// - Thread-safe concurrent operations using ConcurrentDictionary
    /// - Automatic entity structure inference from data objects
    /// - Support for complex filtering with multiple conditions
    /// - Integration with external cache providers for persistence
    /// - Memory-efficient data storage and retrieval
    /// - Comprehensive error handling and logging
    /// </summary>
    public class CacheMemoryDataSourceExample
    {
        #region Private Fields
        /// <summary>
        /// The main InMemoryCacheDataSource instance used for all demonstration operations.
        /// Provides in-memory data storage with full IDataSource interface compatibility.
        /// </summary>
        private InMemoryCacheDataSource _cacheDataSource;
        
        /// <summary>
        /// Data Management Editor interface providing access to configuration, logging,
        /// and other core Beep framework functionality.
        /// </summary>
        private IDMEEditor _dmeEditor;
        
        /// <summary>
        /// Logger interface for recording operations, errors, and performance metrics
        /// throughout the example execution.
        /// </summary>
        private IDMLogger _logger;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the CacheMemoryDataSourceExample class.
        /// 
        /// Sets up the example environment with the provided editor and logger instances,
        /// preparing for demonstration of InMemoryCacheDataSource capabilities.
        /// </summary>
        /// <param name="dmeEditor">The data management editor for framework integration</param>
        /// <param name="logger">The logger for recording operations and events</param>
        public CacheMemoryDataSourceExample(IDMEEditor dmeEditor, IDMLogger logger)
        {
            _dmeEditor = dmeEditor;
            _logger = logger;
        }
        #endregion

        #region Basic Example Methods
        /// <summary>
        /// Runs a comprehensive basic example demonstrating core InMemoryCacheDataSource functionality.
        /// 
        /// This method orchestrates a complete demonstration including:
        /// 1. Data source initialization and connection establishment
        /// 2. Sample data creation with multiple entity types (Users, Products)
        /// 3. Full CRUD operations with error handling and validation
        /// 4. Advanced filtering and querying capabilities
        /// 5. Entity management and structure discovery
        /// 6. Proper resource cleanup and disposal
        /// 
        /// The example uses realistic business data (users, products, orders) to show
        /// practical usage patterns and best practices for the InMemoryCacheDataSource.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task RunBasicExample()
        {
            Console.WriteLine("=== Cache Memory Data Source Example ===");

            // Initialize the cache data source
            InitializeCacheDataSource();

            // Create sample data
            await CreateSampleData();

            // Demonstrate CRUD operations
            await DemonstrateCrudOperations();

            // Demonstrate filtering and querying
            await DemonstrateQuerying();

            // Demonstrate entity management
            await DemonstrateEntityManagement();

            // Clean up
            Cleanup();
        }

        /// <summary>
        /// Initializes and configures the InMemoryCacheDataSource for demonstration.
        /// 
        /// This method:
        /// - Creates a new InMemoryCacheDataSource instance with proper configuration
        /// - Sets up error handling and logging integration
        /// - Establishes the connection to the in-memory cache
        /// - Validates the connection status and reports results
        /// 
        /// The data source is configured for in-memory cache operations with
        /// automatic entity discovery and thread-safe concurrent access.
        /// </summary>
        private void InitializeCacheDataSource()
        {
            Console.WriteLine("\n1. Initializing Cache Memory Data Source...");

            var errorInfo = new ErrorsInfo();
            _cacheDataSource = new InMemoryCacheDataSource(
                "ExampleCacheSource", 
                _logger, 
                _dmeEditor, 
                DataSourceType.InMemoryCache, 
                errorInfo
            );

            var connectionResult = _cacheDataSource.Openconnection();
            Console.WriteLine($"   Connection Status: {connectionResult}");
        }

        /// <summary>
        /// Creates comprehensive sample data to demonstrate various data operations.
        /// 
        /// This method populates the cache with:
        /// - User entities with diverse attributes (name, email, age, department)
        /// - Product entities with pricing and inventory information
        /// - Realistic business data for testing filtering and querying
        /// 
        /// Each insert operation is validated and logged, demonstrating proper
        /// error handling and success verification patterns.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        private async Task CreateSampleData()
        {
            Console.WriteLine("\n2. Creating Sample Data...");

            // Create sample users with diverse attributes for filtering demonstrations
            var users = new[]
            {
                new { Id = 1, Name = "John Doe", Email = "john@example.com", Age = 30, Department = "Engineering" },
                new { Id = 2, Name = "Jane Smith", Email = "jane@example.com", Age = 28, Department = "Marketing" },
                new { Id = 3, Name = "Bob Johnson", Email = "bob@example.com", Age = 35, Department = "Engineering" },
                new { Id = 4, Name = "Alice Brown", Email = "alice@example.com", Age = 32, Department = "Sales" }
            };

            foreach (var user in users)
            {
                var result = _cacheDataSource.InsertEntity("Users", user);
                if (result.Flag == Errors.Ok)
                {
                    Console.WriteLine($"   Inserted user: {user.Name}");
                }
                else
                {
                    Console.WriteLine($"   Failed to insert user: {user.Name} - {result.Message}");
                }
            }

            // Create sample products with pricing and inventory data
            var products = new[]
            {
                new { Id = 1, Name = "Laptop", Price = 999.99m, Category = "Electronics", InStock = true },
                new { Id = 2, Name = "Mouse", Price = 29.99m, Category = "Electronics", InStock = true },
                new { Id = 3, Name = "Desk Chair", Price = 199.99m, Category = "Furniture", InStock = false },
                new { Id = 4, Name = "Monitor", Price = 299.99m, Category = "Electronics", InStock = true }
            };

            foreach (var product in products)
            {
                var result = _cacheDataSource.InsertEntity("Products", product);
                if (result.Flag == Errors.Ok)
                {
                    Console.WriteLine($"   Inserted product: {product.Name}");
                }
            }
        }

        /// <summary>
        /// Demonstrates comprehensive CRUD (Create, Read, Update, Delete) operations.
        /// 
        /// This method showcases:
        /// - Reading all entities from a collection
        /// - Updating existing entities with new information
        /// - Deleting entities by primary key
        /// - Verifying changes with subsequent read operations
        /// - Proper error handling and status validation
        /// 
        /// Each operation is logged and verified to ensure data integrity
        /// and demonstrate proper usage patterns for the InMemoryCacheDataSource.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        private async Task DemonstrateCrudOperations()
        {
            Console.WriteLine("\n3. Demonstrating CRUD Operations...");

            // Read operations - demonstrating data retrieval
            Console.WriteLine("\n   Reading all users:");
            var allUsers = _cacheDataSource.GetEntity("Users", null);
            if (allUsers != null)
            {
                foreach (var user in allUsers)
                {
                    if (user is Dictionary<string, object> userDict)
                    {
                        Console.WriteLine($"   - {userDict["Name"]} ({userDict["Email"]})");
                    }
                }
            }

            // Update operation - demonstrating data modification
            Console.WriteLine("\n   Updating user...");
            var updatedUser = new { Id = 1, Name = "John Doe", Email = "john.doe@newcompany.com", Age = 31, Department = "Engineering" };
            var updateResult = _cacheDataSource.UpdateEntity("Users", updatedUser);
            if (updateResult.Flag == Errors.Ok)
            {
                Console.WriteLine("   User updated successfully");
            }

            // Delete operation - demonstrating data removal
            Console.WriteLine("\n   Deleting user...");
            var userToDelete = new { Id = 4 };
            var deleteResult = _cacheDataSource.DeleteEntity("Users", userToDelete);
            if (deleteResult.Flag == Errors.Ok)
            {
                Console.WriteLine("   User deleted successfully");
            }

            // Verify changes - demonstrating data consistency
            Console.WriteLine("\n   Users after update and delete:");
            var updatedUsers = _cacheDataSource.GetEntity("Users", null);
            if (updatedUsers != null)
            {
                foreach (var user in updatedUsers)
                {
                    if (user is Dictionary<string, object> userDict)
                    {
                        Console.WriteLine($"   - {userDict["Name"]} ({userDict["Email"]})");
                    }
                }
            }
        }

        /// <summary>
        /// Demonstrates advanced filtering and querying capabilities.
        /// 
        /// This method showcases:
        /// - Single-condition filtering (department equals "Engineering")
        /// - Multi-condition filtering (category and price range)
        /// - Various filter operators (equals, less than, contains, etc.)
        /// - Paged result retrieval with configurable page sizes
        /// - Complex query patterns for business scenarios
        /// 
        /// The filtering system supports all standard comparison operators
        /// and can handle multiple conditions with implicit AND logic.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        private async Task DemonstrateQuerying()
        {
            Console.WriteLine("\n4. Demonstrating Filtering and Querying...");

            // Single-condition filter - department filtering
            var engineeringFilter = new List<AppFilter>
            {
                new AppFilter { FieldName = "Department", Operator = "=", FilterValue = "Engineering" }
            };

            Console.WriteLine("\n   Engineering department users:");
            var engineeringUsers = _cacheDataSource.GetEntity("Users", engineeringFilter);
            foreach (var user in engineeringUsers)
            {
                if (user is Dictionary<string, object> userDict)
                {
                    Console.WriteLine($"   - {userDict["Name"]} ({userDict["Department"]})");
                }
            }

            // Multi-condition filter - category and price range
            var electronicsFilter = new List<AppFilter>
            {
                new AppFilter { FieldName = "Category", Operator = "=", FilterValue = "Electronics" },
                new AppFilter { FieldName = "Price", Operator = "<", FilterValue = "300" }
            };

            Console.WriteLine("\n   Electronics under $300:");
            var affordableElectronics = _cacheDataSource.GetEntity("Products", electronicsFilter);
            foreach (var product in affordableElectronics)
            {
                if (product is Dictionary<string, object> productDict)
                {
                    Console.WriteLine($"   - {productDict["Name"]}: ${productDict["Price"]}");
                }
            }

            // Paging demonstration - efficient handling of large datasets
            Console.WriteLine("\n   Paged results (page 1, size 2):");
            var pagedResult = _cacheDataSource.GetEntity("Products", null, 1, 2);
            if (pagedResult?.Data != null && pagedResult.Data is IEnumerable<object> pagedData)
            {
                foreach (var product in pagedData)
                {
                    if (product is Dictionary<string, object> productDict)
                    {
                        Console.WriteLine($"   - {productDict["Name"]}");
                    }
                }
            }
        }

        /// <summary>
        /// Demonstrates entity management and structure discovery capabilities.
        /// 
        /// This method showcases:
        /// - Listing all available entities in the data source
        /// - Inspecting entity structures and field definitions
        /// - Creating custom entity structures programmatically
        /// - Setting primary keys and field properties
        /// - Adding data to newly created entities
        /// 
        /// The entity management system supports automatic schema discovery
        /// from data objects as well as explicit entity structure definition.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        private async Task DemonstrateEntityManagement()
        {
            Console.WriteLine("\n5. Demonstrating Entity Management...");

            // List available entities
            Console.WriteLine("\n   Available entities:");
            var entityNames = _cacheDataSource.GetEntitesList();
            foreach (var entityName in entityNames)
            {
                Console.WriteLine($"   - {entityName}");
            }

            // Inspect entity structure
            Console.WriteLine("\n   Users entity structure:");
            var userEntity = _cacheDataSource.GetEntityStructure("Users", false);
            if (userEntity?.Fields != null)
            {
                foreach (var field in userEntity.Fields)
                {
                    Console.WriteLine($"   - {field.fieldname} ({field.fieldtype})");
                }
            }

            // Create custom entity structure
            var orderEntity = new EntityStructure
            {
                EntityName = "Orders",
                DatasourceEntityName = "Orders",
                Caption = "Orders",
                DatabaseType = DataSourceType.InMemoryCache,
                DataSourceID = "ExampleCacheSource",
                Fields = new List<EntityField>
                {
                    new EntityField { fieldname = "Id", fieldtype = "System.Int32", IsKey = true, AllowDBNull = false },
                    new EntityField { fieldname = "UserId", fieldtype = "System.Int32", AllowDBNull = false },
                    new EntityField { fieldname = "ProductId", fieldtype = "System.Int32", AllowDBNull = false },
                    new EntityField { fieldname = "Quantity", fieldtype = "System.Int32", AllowDBNull = false },
                    new EntityField { fieldname = "OrderDate", fieldtype = "System.DateTime", AllowDBNull = false },
                    new EntityField { fieldname = "TotalAmount", fieldtype = "System.Decimal", AllowDBNull = false }
                }
            };

            // Set primary key
            orderEntity.PrimaryKeys = new List<EntityField> 
            { 
                orderEntity.Fields.First(f => f.fieldname == "Id") 
            };

            // Create the entity
            var createResult = _cacheDataSource.CreateEntityAs(orderEntity);
            if (createResult)
            {
                Console.WriteLine("\n   Orders entity created successfully");

                // Add sample order data
                var sampleOrder = new 
                { 
                    Id = 1, 
                    UserId = 1, 
                    ProductId = 1, 
                    Quantity = 2, 
                    OrderDate = DateTime.Now, 
                    TotalAmount = 1999.98m 
                };

                var insertResult = _cacheDataSource.InsertEntity("Orders", sampleOrder);
                if (insertResult.Flag == Errors.Ok)
                {
                    Console.WriteLine("   Sample order inserted successfully");
                }
            }
        }

        /// <summary>
        /// Performs proper cleanup and resource disposal.
        /// 
        /// This method demonstrates:
        /// - Proper connection closure
        /// - Resource disposal patterns
        /// - Exception handling during cleanup
        /// - Logging of cleanup operations
        /// 
        /// Following proper cleanup patterns is essential for memory management
        /// and preventing resource leaks in production applications.
        /// </summary>
        private void Cleanup()
        {
            Console.WriteLine("\n6. Cleaning up...");

            try
            {
                _cacheDataSource?.Closeconnection();
                _cacheDataSource?.Dispose();
                Console.WriteLine("   Cache data source disposed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   Error during cleanup: {ex.Message}");
            }
        }
        #endregion

        #region Advanced Example Methods
        /// <summary>
        /// Demonstrates advanced cache operations with external cache provider integration.
        /// 
        /// This method showcases:
        /// - Custom cache configuration with specific settings
        /// - Integration with CacheManager for advanced scenarios
        /// - Direct cache provider access and manipulation
        /// - Performance testing and benchmarking
        /// - Statistics collection and analysis
        /// - Memory usage optimization techniques
        /// 
        /// The advanced example shows how to leverage the full power of the caching
        /// system for high-performance, production-ready applications.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task RunAdvancedExample()
        {
            Console.WriteLine("\n=== Advanced Cache Memory Data Source Example ===");

            // Initialize cache manager with custom configuration
            var config = new CacheConfiguration
            {
                DefaultExpiry = TimeSpan.FromMinutes(30),
                MaxItems = 1000,
                EnableStatistics = true,
                CleanupInterval = TimeSpan.FromMinutes(5)
            };

            CacheManager.Initialize(config, CacheProviderType.InMemory);

            // Initialize data source (will use initialized cache manager)
            InitializeCacheDataSource();

            // Demonstrate direct cache integration
            await DemonstrateCacheIntegration();

            // Performance testing and benchmarking
            await PerformanceTest();

            Cleanup();
        }

        /// <summary>
        /// Demonstrates integration between InMemoryCacheDataSource and CacheManager.
        /// 
        /// This method shows:
        /// - Dual access to cached data through different interfaces
        /// - Cache key naming conventions and patterns
        /// - Cross-system data accessibility
        /// - Integration verification and validation
        /// 
        /// The integration allows data stored through InMemoryCacheDataSource
        /// to be accessible through the CacheManager API and vice versa.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        private async Task DemonstrateCacheIntegration()
        {
            Console.WriteLine("\n   Cache Integration:");

            // Insert data through InMemoryCacheDataSource
            var testData = new { Id = 999, Name = "Cache Test", Value = "Integration Demo" };
            
            var result = _cacheDataSource.InsertEntity("TestEntity", testData);
            if (result.Flag == Errors.Ok)
            {
                Console.WriteLine("   Data inserted through InMemoryCacheDataSource");

                // Access the same data through CacheManager
                var cachedValue = await CacheManager.GetAsync<Dictionary<string, object>>("ExampleCacheSource:TestEntity:999");
                if (cachedValue != null)
                {
                    Console.WriteLine($"   Data accessible through CacheManager: {cachedValue["Name"]}");
                }
            }
        }

        /// <summary>
        /// Performs comprehensive performance testing and benchmarking.
        /// 
        /// This method:
        /// - Measures insert performance for large datasets (1000+ records)
        /// - Tests filtering performance with complex conditions
        /// - Collects and reports cache statistics
        /// - Analyzes hit/miss ratios and memory usage
        /// - Provides performance metrics for optimization
        /// 
        /// Performance testing is essential for understanding the scalability
        /// and efficiency characteristics of the caching system.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        private async Task PerformanceTest()
        {
            Console.WriteLine("\n   Performance Test:");

            var watch = System.Diagnostics.Stopwatch.StartNew();
            
            // Bulk insert performance test
            for (int i = 0; i < 1000; i++)
            {
                var testRecord = new { Id = i, Name = $"Test Record {i}", Value = i * 10 };
                _cacheDataSource.InsertEntity("PerformanceTest", testRecord);
            }

            watch.Stop();
            Console.WriteLine($"   Inserted 1000 records in {watch.ElapsedMilliseconds}ms");

            // Filtering performance test
            watch.Restart();
            var filter = new List<AppFilter>
            {
                new AppFilter { FieldName = "Value", Operator = ">", FilterValue = "5000" }
            };

            var filteredResults = _cacheDataSource.GetEntity("PerformanceTest", filter);
            var count = filteredResults.Count();
            watch.Stop();

            Console.WriteLine($"   Filtered {count} records in {watch.ElapsedMilliseconds}ms");

            // Cache statistics analysis
            var stats = CacheManager.GetStatistics();
            Console.WriteLine($"   Cache Statistics:");
            Console.WriteLine($"   - Primary Provider: {stats.PrimaryProviderName} (Available: {stats.PrimaryProviderAvailable})");
            if (stats.PrimaryProvider != null)
            {
                Console.WriteLine($"   - Items: {stats.PrimaryProvider.ItemCount}");
                Console.WriteLine($"   - Hits: {stats.PrimaryProvider.Hits}");
                Console.WriteLine($"   - Misses: {stats.PrimaryProvider.Misses}");
            }
        }
        #endregion
    }
}