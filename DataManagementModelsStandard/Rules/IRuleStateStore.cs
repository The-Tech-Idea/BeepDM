using System;

namespace TheTechIdea.Beep.Rules
{
    /// <summary>
    /// Provides persistent-across-invocations state storage for stateful rules such as
    /// <c>RetryWithBackoff</c>, <c>CircuitBreaker</c>, and <c>Throttle</c>.
    /// Each entry is keyed by a <c>stateKey</c> (typically ruleKey + entityId).
    /// </summary>
    public interface IRuleStateStore
    {
        /// <summary>Retrieves a state value by key.  Returns <c>null</c> if not found.</summary>
        object? Get(string stateKey);

        /// <summary>Stores or replaces a state value.</summary>
        void Set(string stateKey, object value);

        /// <summary>Increments an integer counter by <paramref name="delta"/> and returns the new value.</summary>
        int Increment(string stateKey, int delta = 1);

        /// <summary>Removes the state entry for <paramref name="stateKey"/>.</summary>
        void Remove(string stateKey);

        /// <summary>Returns <c>true</c> when an entry for <paramref name="stateKey"/> exists.</summary>
        bool Contains(string stateKey);

        /// <summary>Clears all state entries.</summary>
        void Clear();
    }
}
