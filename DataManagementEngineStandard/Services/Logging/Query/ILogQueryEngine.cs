using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Services.Logging.Query
{
    /// <summary>
    /// Storage-agnostic log query engine. Implementations adapt the
    /// configured logging sinks (SQLite, NDJSON files) to the
    /// <see cref="LogQuery"/> contract.
    /// </summary>
    public interface ILogQueryEngine
    {
        /// <summary>
        /// Executes <paramref name="query"/> and returns the matching
        /// records in the order requested by the query.
        /// </summary>
        Task<IReadOnlyList<LogRecord>> ExecuteAsync(LogQuery query, CancellationToken cancellationToken = default);
    }
}
