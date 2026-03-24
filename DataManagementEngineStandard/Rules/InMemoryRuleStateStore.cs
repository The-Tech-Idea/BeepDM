using System.Collections.Concurrent;

namespace TheTechIdea.Beep.Rules
{
    /// <summary>
    /// Thread-safe in-process implementation of <see cref="IRuleStateStore"/>.
    /// State is scoped to the application lifetime (not persisted across restarts).
    /// Replace with a Redis/DB-backed implementation for distributed or durable scenarios.
    /// </summary>
    public sealed class InMemoryRuleStateStore : IRuleStateStore
    {
        private readonly ConcurrentDictionary<string, object> _store = new();

        public object? Get(string stateKey)
        {
            _store.TryGetValue(stateKey, out var val);
            return val;
        }

        public void Set(string stateKey, object value) =>
            _store[stateKey] = value;

        public int Increment(string stateKey, int delta = 1)
        {
            return (int)_store.AddOrUpdate(
                stateKey,
                _  => delta,
                (_, existing) => (int)existing + delta);
        }

        public void Remove(string stateKey) =>
            _store.TryRemove(stateKey, out _);

        public bool Contains(string stateKey) =>
            _store.ContainsKey(stateKey);

        public void Clear() =>
            _store.Clear();
    }
}
