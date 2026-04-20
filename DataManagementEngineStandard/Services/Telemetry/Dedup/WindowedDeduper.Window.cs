using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Services.Logging;

namespace TheTechIdea.Beep.Services.Telemetry.Dedup
{
    /// <summary>
    /// Eviction and summary-emission half of <see cref="WindowedDeduper"/>.
    /// Holds the per-key tracking record and the helpers that walk the LRU
    /// list when entries expire or when the key cap is hit.
    /// </summary>
    public sealed partial class WindowedDeduper
    {
        private sealed class WindowEntry
        {
            public string Key;
            public string Template;
            public BeepLogLevel Level;
            public string Category;
            public DateTime FirstSeenUtc;
            public DateTime ExpiresAtUtc;
            public long Count;
            public LinkedListNode<string> Node;
        }

        private void Touch_NoLock(WindowEntry entry)
        {
            if (entry.Node is null)
            {
                return;
            }
            _lru.Remove(entry.Node);
            entry.Node = _lru.AddLast(entry.Key);
        }

        private void EvictIfNeeded_NoLock()
        {
            while (_windows.Count >= _maxKeys && _lru.First is not null)
            {
                string oldest = _lru.First.Value;
                _lru.RemoveFirst();
                _windows.Remove(oldest);
            }
        }

        private List<TelemetryEnvelope> ExpireOldEntries_NoLock(DateTime now)
        {
            List<TelemetryEnvelope> result = null;
            LinkedListNode<string> node = _lru.First;
            while (node is not null)
            {
                LinkedListNode<string> next = node.Next;
                if (!_windows.TryGetValue(node.Value, out WindowEntry entry))
                {
                    _lru.Remove(node);
                    node = next;
                    continue;
                }
                if (entry.ExpiresAtUtc > now)
                {
                    node = next;
                    continue;
                }

                _lru.Remove(node);
                _windows.Remove(entry.Key);

                if (entry.Count > 1)
                {
                    result ??= new List<TelemetryEnvelope>(4);
                    result.Add(BuildSummary(entry));
                }
                node = next;
            }
            return result;
        }

        private static TelemetryEnvelope BuildSummary(WindowEntry entry)
        {
            long suppressed = entry.Count - 1;
            string message = string.Concat(
                "[dedup] ",
                suppressed.ToString(),
                " additional occurrences of: ",
                entry.Template ?? string.Empty);

            var properties = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                [DedupCountProperty] = suppressed,
                [DedupTemplateProperty] = entry.Template ?? string.Empty
            };

            return new TelemetryEnvelope
            {
                Kind = TelemetryKind.Log,
                TimestampUtc = DateTime.UtcNow,
                Level = entry.Level,
                Category = entry.Category,
                Message = message,
                Properties = properties
            };
        }
    }
}
