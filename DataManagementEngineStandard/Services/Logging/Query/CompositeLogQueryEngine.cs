using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Services.Logging.Query
{
    /// <summary>
    /// Fans a query out to every configured engine and merges results.
    /// Logs lack a stable global id, so de-duplication is done on a
    /// composite key (<c>ts</c> + <c>category</c> + <c>message</c>);
    /// duplicates from overlapping sinks are still rare in practice.
    /// </summary>
    public sealed class CompositeLogQueryEngine : ILogQueryEngine
    {
        private readonly IReadOnlyList<ILogQueryEngine> _engines;

        /// <summary>Creates a composite over the supplied engines.</summary>
        public CompositeLogQueryEngine(IReadOnlyList<ILogQueryEngine> engines)
        {
            _engines = engines ?? throw new ArgumentNullException(nameof(engines));
        }

        /// <summary>Number of engines composed.</summary>
        public int Count => _engines.Count;

        /// <inheritdoc />
        public async Task<IReadOnlyList<LogRecord>> ExecuteAsync(LogQuery query, CancellationToken cancellationToken = default)
        {
            if (_engines.Count == 0)
            {
                return Array.Empty<LogRecord>();
            }
            if (_engines.Count == 1)
            {
                return await _engines[0].ExecuteAsync(query, cancellationToken).ConfigureAwait(false);
            }

            var combined = new List<LogRecord>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (ILogQueryEngine engine in _engines)
            {
                if (cancellationToken.IsCancellationRequested) { break; }
                IReadOnlyList<LogRecord> partial =
                    await engine.ExecuteAsync(query, cancellationToken).ConfigureAwait(false);
                if (partial is null || partial.Count == 0) { continue; }
                for (int i = 0; i < partial.Count; i++)
                {
                    LogRecord record = partial[i];
                    if (record is null) { continue; }
                    string key = string.Concat(
                        record.TimestampUtc.Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        '|',
                        record.Category ?? string.Empty,
                        '|',
                        record.Message ?? string.Empty);
                    if (!seen.Add(key)) { continue; }
                    combined.Add(record);
                }
            }

            combined.Sort((a, b) =>
            {
                int cmp = a.TimestampUtc.CompareTo(b.TimestampUtc);
                return query.OrderDescending ? -cmp : cmp;
            });
            if (query.Take > 0 && combined.Count > query.Take)
            {
                combined.RemoveRange(query.Take, combined.Count - query.Take);
            }
            return combined;
        }
    }
}
