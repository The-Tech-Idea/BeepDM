using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Caching;

namespace TheTechIdea.Beep.Caching.Examples
{
    /// <summary>
    /// Example usage of the optimized CacheManager with multiple providers.
    /// </summary>
    public static class CacheManagerExamples
    {
        /// <summary>
        /// Basic usage example of the cache manager.
        /// </summary>
        public static async Task BasicUsageExample()
        {
            // Initialize with default settings (SimpleCacheProvider)
            CacheManager.Initialize();

            // Store some data
            await CacheManager.SetAsync("user:123", new { Name = "John Doe", Age = 30 });
            await CacheManager.SetAsync("product:456", new { Name = "Widget", Price = 9.99m }, TimeSpan.FromMinutes(5));

            // Retrieve data
            var user = await CacheManager.GetAsync<dynamic>("user:123");
            var product = await CacheManager.GetAsync<dynamic>("product:456");

            Console.WriteLine($"User: {user?.Name}, Age: {user?.Age}");
            Console.WriteLine($"Product: {product?.Name}, Price: {product?.Price}");

            // Use GetOrCreate pattern
            var expensiveData = await CacheManager.GetOrCreateAsync("expensive:data", async () =>
            {
                // Simulate expensive operation
                await Task.Delay(1000);
                return "This took a long time to compute";
            }, TimeSpan.FromMinutes(10));

            Console.WriteLine($"Expensive data: {expensiveData}");

            // Check if key exists
            var exists = await CacheManager.ExistsAsync("user:123");
            Console.WriteLine($"User exists in cache: {exists}");

            // Get statistics
            var stats = CacheManager.GetStatistics();
            Console.WriteLine($"Cache hits: {stats.PrimaryProvider?.Hits}");
            Console.WriteLine($"Cache misses: {stats.PrimaryProvider?.Misses}");
            Console.WriteLine($"Hit ratio: {stats.PrimaryProvider?.HitRatio:F2}%");
        }

        /// <summary>
        /// Advanced usage with custom configuration and multiple providers.
        /// </summary>
        public static async Task AdvancedUsageExample()
        {
            // Custom configuration
            var config = new CacheConfiguration
            {
                DefaultExpiry = TimeSpan.FromMinutes(30),
                MaxItems = 5000,
                CleanupInterval = TimeSpan.FromMinutes(2),
                EnableStatistics = true,
                KeyPrefix = "myapp:",
                EnableCompression = true,
                CompressionThreshold = 1024
            };

            // Initialize with custom configuration and Redis as primary, InMemory as fallback
            CacheManager.Initialize(config, CacheProviderType.Redis, CacheProviderType.InMemory);

            // Batch operations
            var batchData = new Dictionary<string, string>
            {
                ["key1"] = "value1",
                ["key2"] = "value2",
                ["key3"] = "value3"
            };

            var setCount = await CacheManager.SetManyAsync(batchData, TimeSpan.FromMinutes(15));
            Console.WriteLine($"Set {setCount} items in batch");

            var retrievedData = await CacheManager.GetManyAsync<string>(batchData.Keys);
            Console.WriteLine($"Retrieved {retrievedData.Count} items in batch");

            // Clear cache with pattern
            await CacheManager.ClearAsync("temp:*");
            Console.WriteLine("Cleared temporary cache items");

            // Get comprehensive statistics
            var stats = CacheManager.GetStatistics();
            Console.WriteLine($"Primary provider: {stats.PrimaryProviderName} - Available: {stats.PrimaryProviderAvailable}");
            Console.WriteLine($"Fallback provider: {stats.FallbackProviderName} - Available: {stats.FallbackProviderAvailable}");
            
            if (stats.PrimaryProvider != null)
            {
                Console.WriteLine($"Primary cache hits: {stats.PrimaryProvider.Hits}");
                Console.WriteLine($"Primary cache misses: {stats.PrimaryProvider.Misses}");
                Console.WriteLine($"Primary hit ratio: {stats.PrimaryProvider.HitRatio:F2}%");
            }
        }

        /// <summary>
        /// Synchronous usage example (backward compatibility).
        /// </summary>
        public static void SynchronousUsageExample()
        {
            // Initialize cache
            CacheManager.Initialize();

            // Synchronous operations (compatible with existing code)
            CacheManager.Set("sync:key", "Synchronous value");
            var value = CacheManager.Get<string>("sync:key");
            var exists = CacheManager.Contains("sync:key");

            Console.WriteLine($"Sync value: {value}, Exists: {exists}");

            // GetOrCreate pattern (synchronous)
            var computed = CacheManager.GetOrCreate("computed:value", () =>
            {
                // Simulate some work
                Thread.Sleep(100);
                return $"Computed at {DateTime.Now}";
            }, TimeSpan.FromMinutes(5));

            Console.WriteLine($"Computed value: {computed}");

            // Clear cache with pattern
            CacheManager.InvalidateCache("computed:*");
            
            var afterClear = CacheManager.Contains("computed:value");
            Console.WriteLine($"Exists after clear: {afterClear}");
        }
    }
}