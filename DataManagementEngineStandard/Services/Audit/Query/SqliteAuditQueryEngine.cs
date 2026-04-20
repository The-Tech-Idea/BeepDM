using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Services.Audit.Models;
using TheTechIdea.Beep.Services.Telemetry.Sinks;

namespace TheTechIdea.Beep.Services.Audit.Query
{
    /// <summary>
    /// Thin facade that adapts a <see cref="SqliteSink"/> instance to the
    /// shared <see cref="IAuditQueryEngine"/> contract. The sink already
    /// implements the contract directly; the wrapper exists so DI
    /// composition stays uniform across engines and so a future SQLite
    /// query implementation that opens a dedicated read-only connection
    /// can swap in without touching call sites.
    /// </summary>
    public sealed class SqliteAuditQueryEngine : IAuditQueryEngine
    {
        private readonly SqliteSink _sink;

        /// <summary>Creates a query engine over <paramref name="sink"/>.</summary>
        public SqliteAuditQueryEngine(SqliteSink sink)
        {
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
        }

        /// <summary>Backing sink. Exposed for diagnostics and tests.</summary>
        public SqliteSink Sink => _sink;

        /// <inheritdoc />
        public Task<IReadOnlyList<AuditEvent>> ExecuteAsync(
            AuditQuery query,
            CancellationToken cancellationToken = default)
            => _sink.ExecuteAsync(query, cancellationToken);
    }
}
