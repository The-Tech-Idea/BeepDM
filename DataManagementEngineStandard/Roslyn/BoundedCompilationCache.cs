using System;
using System.Collections.Generic;
using System.Threading;

namespace TheTechIdea.Beep.Roslyn
{
    internal sealed class BoundedCompilationCache<TKey, TValue> where TKey : notnull
    {
        private sealed class Entry
        {
            public readonly Lazy<TValue> Value;
            public bool Retained;
            public Entry(Func<TValue> factory) => Value = new(factory, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        private readonly object _gate = new();
        private readonly Dictionary<TKey, Entry> _entries = new();
        private readonly Queue<KeyValuePair<TKey, Entry>> _completed = new();
        private readonly int _limit;

        public BoundedCompilationCache(int limit) => _limit = limit > 0 ? limit : throw new ArgumentOutOfRangeException(nameof(limit));

        public int Count { get { lock (_gate) return _entries.Count; } }

        public TValue GetOrAdd(TKey key, Func<TValue> factory)
        {
            Entry entry;
            lock (_gate)
            {
                if (!_entries.TryGetValue(key, out entry))
                {
                    entry = new Entry(factory);
                    _entries.Add(key, entry);
                }
            }
            try
            {
                // Compilation and host callbacks must never execute under the cache lock.
                var result = entry.Value.Value;
                lock (_gate)
                {
                    if (_entries.TryGetValue(key, out var current) && ReferenceEquals(current, entry) && !entry.Retained)
                    {
                        entry.Retained = true;
                        _completed.Enqueue(new(key, entry));
                        while (_completed.Count > _limit)
                        {
                            var oldest = _completed.Dequeue();
                            if (_entries.TryGetValue(oldest.Key, out current) && ReferenceEquals(current, oldest.Value))
                                _entries.Remove(oldest.Key);
                        }
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                // Propagate the original failure and evict only this failed attempt, not a newer one.
                System.Diagnostics.Debug.WriteLine($"Generated compilation failed: {ex.GetType().Name}");
                lock (_gate)
                    if (_entries.TryGetValue(key, out var current) && ReferenceEquals(current, entry)) _entries.Remove(key);
                throw;
            }
        }

        public void Clear()
        {
            lock (_gate)
            {
                _entries.Clear();
                _completed.Clear();
            }
        }

        public bool RemoveWhere(Func<TKey, bool> predicate)
        {
            lock (_gate)
            {
                var remove = new List<TKey>();
                foreach (var key in _entries.Keys) if (predicate(key)) remove.Add(key);
                foreach (var key in remove) _entries.Remove(key);
                // Drop removed queue references, including their generated Types/assemblies.
                int count = _completed.Count;
                for (int i = 0; i < count; i++)
                {
                    var item = _completed.Dequeue();
                    if (_entries.TryGetValue(item.Key, out var current) && ReferenceEquals(current, item.Value))
                        _completed.Enqueue(item);
                }
                return remove.Count > 0;
            }
        }
    }
}
