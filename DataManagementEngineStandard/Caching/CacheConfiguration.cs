using System;

namespace TheTechIdea.Beep.Caching
{
    /// <summary>
    /// Configuration settings for cache providers and the cache manager.
    /// </summary>
    public class CacheConfiguration
    {
        /// <summary>Gets or sets the default expiration time for cached items.</summary>
        public TimeSpan DefaultExpiry { get; set; } = TimeSpan.FromMinutes(10);

        /// <summary>Gets or sets the maximum number of items in the cache (for in-memory providers).</summary>
        public int MaxItems { get; set; } = 10000;

        /// <summary>Gets or sets the cleanup interval for expired items.</summary>
        public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>Gets or sets whether to enable cache statistics.</summary>
        public bool EnableStatistics { get; set; } = true;

        /// <summary>Gets or sets the cache key prefix.</summary>
        public string KeyPrefix { get; set; } = "beep:";

        /// <summary>Gets or sets whether to compress cached values.</summary>
        public bool EnableCompression { get; set; } = false;

        /// <summary>Gets or sets the compression threshold (in bytes).</summary>
        public int CompressionThreshold { get; set; } = 1024;

        /// <summary>Gets or sets the serialization format.</summary>
        public SerializationFormat SerializationFormat { get; set; } = SerializationFormat.Json;

        /// <summary>Gets or sets Redis-specific configuration.</summary>
        public RedisConfiguration Redis { get; set; } = new RedisConfiguration();

        /// <summary>Gets or sets MemoryCache-specific configuration.</summary>
        public MemoryCacheConfiguration MemoryCache { get; set; } = new MemoryCacheConfiguration();
    }

    /// <summary>
    /// Redis-specific configuration settings.
    /// </summary>
    public class RedisConfiguration
    {
        /// <summary>Gets or sets the Redis connection string.</summary>
        public string ConnectionString { get; set; } = "localhost:6379";

        /// <summary>Gets or sets the Redis database number.</summary>
        public int Database { get; set; } = 0;

        /// <summary>Gets or sets the connection timeout.</summary>
        public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>Gets or sets whether to use connection multiplexing.</summary>
        public bool UseConnectionMultiplexing { get; set; } = true;

        /// <summary>Gets or sets the retry policy configuration.</summary>
        public RetryPolicyConfiguration RetryPolicy { get; set; } = new RetryPolicyConfiguration();
    }

    /// <summary>
    /// MemoryCache-specific configuration settings.
    /// </summary>
    public class MemoryCacheConfiguration
    {
        /// <summary>Gets or sets the size limit for the memory cache.</summary>
        public long? SizeLimit { get; set; } = 100 * 1024 * 1024; // 100MB

        /// <summary>Gets or sets the compaction percentage.</summary>
        public double CompactionPercentage { get; set; } = 0.05; // 5%

        /// <summary>Gets or sets the expiration scan frequency.</summary>
        public TimeSpan ExpirationScanFrequency { get; set; } = TimeSpan.FromMinutes(1);
    }

    /// <summary>
    /// Retry policy configuration for cache operations.
    /// </summary>
    public class RetryPolicyConfiguration
    {
        /// <summary>Gets or sets the maximum number of retry attempts.</summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>Gets or sets the base delay between retries.</summary>
        public TimeSpan BaseDelay { get; set; } = TimeSpan.FromMilliseconds(100);

        /// <summary>Gets or sets the maximum delay between retries.</summary>
        public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>Gets or sets whether to use exponential backoff.</summary>
        public bool UseExponentialBackoff { get; set; } = true;
    }

    /// <summary>
    /// Cache statistics for monitoring and diagnostics.
    /// </summary>
    public class CacheStatistics
    {
        /// <summary>Gets or sets the total number of cache hits.</summary>
        public long Hits { get; set; }

        /// <summary>Gets or sets the total number of cache misses.</summary>
        public long Misses { get; set; }

        /// <summary>Gets or sets the total number of items currently in cache.</summary>
        public long ItemCount { get; set; }

        /// <summary>Gets or sets the total memory used by the cache (in bytes).</summary>
        public long MemoryUsage { get; set; }

        /// <summary>Gets or sets the number of expired items removed.</summary>
        public long ExpiredItems { get; set; }

        /// <summary>Gets or sets the number of evicted items (due to size limits).</summary>
        public long EvictedItems { get; set; }

        /// <summary>Gets the cache hit ratio as a percentage.</summary>
        public double HitRatio => (Hits + Misses) > 0 ? (double)Hits / (Hits + Misses) * 100 : 0;

        /// <summary>Gets or sets the last update time of these statistics.</summary>
        public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Enumeration of supported serialization formats.
    /// </summary>
    public enum SerializationFormat
    {
        /// <summary>JSON serialization (default).</summary>
        Json,
        /// <summary>Binary serialization.</summary>
        Binary,
        /// <summary>MessagePack serialization.</summary>
        MessagePack,
        /// <summary>Protocol Buffers serialization.</summary>
        Protobuf
    }

    /// <summary>
    /// Cache provider types supported by the cache manager.
    /// </summary>
    public enum CacheProviderType
    {
        /// <summary>In-memory cache provider (default).</summary>
        InMemory,
        /// <summary>Microsoft MemoryCache provider.</summary>
        MemoryCache,
        /// <summary>Redis cache provider.</summary>
        Redis,
        /// <summary>Distributed cache provider.</summary>
        Distributed,
        /// <summary>Hybrid cache provider (combination of multiple providers).</summary>
        Hybrid
    }
}