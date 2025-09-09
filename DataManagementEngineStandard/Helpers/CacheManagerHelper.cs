using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Helpers
{
    /// <summary>
    /// Lightweight in-memory cache helper for small objects used by DMEEditor.
    /// Not meant to replace production caches; provides expiry and basic stats.
    /// </summary>
    public static class CacheManagerHelper
    {
        private class CacheItem
        {
            public object Value { get; set; }
            public DateTimeOffset Expiry { get; set; }
        }

        private static readonly ConcurrentDictionary<string, CacheItem> _cache = new ConcurrentDictionary<string, CacheItem>(StringComparer.OrdinalIgnoreCase);

        public static T GetCached<T>(string key, Func<T> factory, TimeSpan? expiry = null)
        {
            if (string.IsNullOrEmpty(key))
                return default;

            if (_cache.TryGetValue(key, out var item))
            {
                if (item.Expiry > DateTimeOffset.UtcNow)
                    return (T)item.Value;
                _cache.TryRemove(key, out _);
            }

            var value = factory();
            if (value != null)
            {
                var exp = expiry.HasValue ? DateTimeOffset.UtcNow.Add(expiry.Value) : DateTimeOffset.UtcNow.AddMinutes(10);
                _cache[key] = new CacheItem { Value = value, Expiry = exp };
            }

            return value;
        }

        public static async Task<T> GetCachedAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null)
        {
            if (string.IsNullOrEmpty(key))
                return default;

            if (_cache.TryGetValue(key, out var item))
            {
                if (item.Expiry > DateTimeOffset.UtcNow)
                    return (T)item.Value;
                _cache.TryRemove(key, out _);
            }

            var value = await factory();
            if (value != null)
            {
                var exp = expiry.HasValue ? DateTimeOffset.UtcNow.Add(expiry.Value) : DateTimeOffset.UtcNow.AddMinutes(10);
                _cache[key] = new CacheItem { Value = value, Expiry = exp };
            }

            return value;
        }

        public static void InvalidateCache(string pattern = "*")
        {
            if (pattern == "*" )
            {
                _cache.Clear();
                return;
            }

            foreach (var key in _cache.Keys)
            {
                if (key.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0)
                    _cache.TryRemove(key, out _);
            }
        }

        public static bool Contains(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            return _cache.ContainsKey(key) && _cache[key].Expiry > DateTimeOffset.UtcNow;
        }

        public static void Remove(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            _cache.TryRemove(key, out _);
        }
    }
}
