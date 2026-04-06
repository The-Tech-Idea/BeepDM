using System;
using System.Collections.Concurrent;
using System.Threading;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;

namespace TheTechIdea.Beep.Editor.Forms.Helpers
{
    /// <summary>
    /// In-memory implementation of ISequenceProvider.
    /// Provides named auto-increment counters that persist for the lifetime of the FormsManager.
    /// Corresponds to Oracle Forms :SEQUENCE.NEXTVAL usage patterns.
    /// Thread-safe via Interlocked operations.
    /// </summary>
    public class SequenceProvider : ISequenceProvider
    {
        private readonly ConcurrentDictionary<string, SequenceEntry> _sequences =
            new(StringComparer.OrdinalIgnoreCase);

        private sealed class SequenceEntry
        {
            public long Current;
            public readonly long IncrementBy;

            public SequenceEntry(long start, long incrementBy)
            {
                Current = start - incrementBy; // so first NEXTVAL returns start
                IncrementBy = incrementBy;
            }
        }

        /// <inheritdoc/>
        public void CreateSequence(string sequenceName, long startValue = 1, long incrementBy = 1)
        {
            if (string.IsNullOrWhiteSpace(sequenceName)) throw new ArgumentNullException(nameof(sequenceName));
            if (incrementBy == 0) throw new ArgumentException("incrementBy must not be zero", nameof(incrementBy));
            _sequences[sequenceName] = new SequenceEntry(startValue, incrementBy);
        }

        /// <inheritdoc/>
        public long GetNextSequence(string sequenceName)
        {
            var entry = GetOrCreate(sequenceName);
            return Interlocked.Add(ref entry.Current, entry.IncrementBy);
        }

        /// <inheritdoc/>
        public long PeekNextSequence(string sequenceName)
        {
            var entry = GetOrCreate(sequenceName);
            return Interlocked.Read(ref entry.Current) + entry.IncrementBy;
        }

        /// <inheritdoc/>
        public void ResetSequence(string sequenceName, long startValue = 1)
        {
            var entry = GetOrCreate(sequenceName);
            Interlocked.Exchange(ref entry.Current, startValue - entry.IncrementBy);
        }

        /// <inheritdoc/>
        public bool SequenceExists(string sequenceName) =>
            !string.IsNullOrWhiteSpace(sequenceName) && _sequences.ContainsKey(sequenceName);

        private SequenceEntry GetOrCreate(string sequenceName)
        {
            if (string.IsNullOrWhiteSpace(sequenceName)) throw new ArgumentNullException(nameof(sequenceName));
            return _sequences.GetOrAdd(sequenceName, _ => new SequenceEntry(1, 1));
        }
    }
}
