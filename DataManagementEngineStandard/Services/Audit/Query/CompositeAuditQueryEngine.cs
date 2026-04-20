using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Services.Audit.Models;

namespace TheTechIdea.Beep.Services.Audit.Query
{
    /// <summary>
    /// Fans a query out to every configured engine, merges the results,
    /// de-duplicates by <see cref="AuditEvent.EventId"/>, and applies a
    /// final sort + take pass. Used by the DI extension when more than
    /// one storage backend (SQLite + file scan, for example) is wired
    /// up under the same <see cref="BeepAudit"/> instance.
    /// </summary>
    public sealed class CompositeAuditQueryEngine : IAuditQueryEngine
    {
        private readonly IReadOnlyList<IAuditQueryEngine> _engines;

        /// <summary>Creates a composite over the supplied engines.</summary>
        public CompositeAuditQueryEngine(IReadOnlyList<IAuditQueryEngine> engines)
        {
            _engines = engines ?? throw new ArgumentNullException(nameof(engines));
        }

        /// <summary>Number of engines composed.</summary>
        public int Count => _engines.Count;

        /// <inheritdoc />
        public async Task<IReadOnlyList<AuditEvent>> ExecuteAsync(
            AuditQuery query,
            CancellationToken cancellationToken = default)
        {
            if (_engines.Count == 0)
            {
                return Array.Empty<AuditEvent>();
            }
            if (_engines.Count == 1)
            {
                return await _engines[0].ExecuteAsync(query, cancellationToken).ConfigureAwait(false);
            }

            var seen = new HashSet<Guid>();
            var combined = new List<AuditEvent>();

            foreach (IAuditQueryEngine engine in _engines)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                IReadOnlyList<AuditEvent> partial =
                    await engine.ExecuteAsync(query, cancellationToken).ConfigureAwait(false);
                if (partial is null || partial.Count == 0)
                {
                    continue;
                }
                for (int i = 0; i < partial.Count; i++)
                {
                    AuditEvent ev = partial[i];
                    if (ev is null || !seen.Add(ev.EventId))
                    {
                        continue;
                    }
                    combined.Add(ev);
                }
            }

            combined.Sort((a, b) => CompareForOrder(a, b, query));
            if (query.Take > 0 && combined.Count > query.Take)
            {
                combined.RemoveRange(query.Take, combined.Count - query.Take);
            }
            return combined;
        }

        private static int CompareForOrder(AuditEvent a, AuditEvent b, AuditQuery query)
        {
            int cmp;
            switch ((query.OrderByField ?? "ts").ToLowerInvariant())
            {
                case "sequence":
                case "seq":
                    cmp = a.Sequence.CompareTo(b.Sequence);
                    break;
                case "user":
                case "user_id":
                    cmp = string.CompareOrdinal(a.UserId ?? string.Empty, b.UserId ?? string.Empty);
                    break;
                case "entity":
                case "entity_name":
                    cmp = string.CompareOrdinal(a.EntityName ?? string.Empty, b.EntityName ?? string.Empty);
                    break;
                default:
                    cmp = a.TimestampUtc.CompareTo(b.TimestampUtc);
                    break;
            }
            return query.OrderDescending ? -cmp : cmp;
        }
    }
}
