using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Services.Telemetry.Sinks;

namespace TheTechIdea.Beep.Services.Logging.Query
{
    /// <summary>
    /// Adapts an existing <see cref="SqliteSink"/> to
    /// <see cref="ILogQueryEngine"/>. Mirrors
    /// <c>SqliteAuditQueryEngine</c> so the DI composition stays uniform
    /// and the underlying connection / write gate are reused.
    /// </summary>
    public sealed class SqliteLogQueryEngine : ILogQueryEngine
    {
        private readonly ILogQueryEngine _inner;

        /// <summary>Creates an engine over <paramref name="sink"/>.</summary>
        public SqliteLogQueryEngine(SqliteSink sink)
        {
            if (sink is null)
            {
                throw new ArgumentNullException(nameof(sink));
            }
            _inner = sink;
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<LogRecord>> ExecuteAsync(LogQuery query, CancellationToken cancellationToken = default)
        {
            return _inner.ExecuteAsync(query, cancellationToken);
        }
    }
}
