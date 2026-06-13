using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace TheTechIdea.Beep.Editor.EntityDiscovery
{
    public interface IDiscoveryCache
    {
        IReadOnlyList<DiscoveredEntity> GetOrAdd(
            string cacheKey,
            Func<IReadOnlyList<DiscoveredEntity>> factory);

        void Invalidate(string cacheKey);
        void InvalidateAll();
    }

    public sealed class InMemoryDiscoveryCache : IDiscoveryCache
    {
        private readonly ConcurrentDictionary<string, IReadOnlyList<DiscoveredEntity>> _cache = new();

        public IReadOnlyList<DiscoveredEntity> GetOrAdd(
            string cacheKey,
            Func<IReadOnlyList<DiscoveredEntity>> factory)
        {
            return _cache.GetOrAdd(cacheKey, _ => factory().ToList().AsReadOnly());
        }

        public void Invalidate(string cacheKey)
        {
            _cache.TryRemove(cacheKey, out _);
        }

        public void InvalidateAll()
        {
            _cache.Clear();
        }
    }
}
