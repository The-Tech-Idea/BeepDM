using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Services.Audit.Models;

namespace TheTechIdea.Beep.Services.Audit.Query
{
    /// <summary>
    /// Storage-agnostic query surface used by <see cref="BeepAudit"/>
    /// (Phase 10) to translate an <see cref="AuditQuery"/> into a
    /// concrete result set. SQLite, file-scan, and composite engines
    /// all satisfy this contract; the audit recorder is engine-agnostic.
    /// </summary>
    /// <remarks>
    /// Engines must be safe for concurrent callers, must honor
    /// cancellation, and must respect <see cref="AuditQuery.Take"/>
    /// (0 = unbounded). Implementations should sort the result set
    /// according to <see cref="AuditQuery.OrderByField"/> +
    /// <see cref="AuditQuery.OrderDescending"/> so a host can render
    /// without an extra LINQ pass.
    /// </remarks>
    public interface IAuditQueryEngine
    {
        /// <summary>
        /// Executes <paramref name="query"/> and returns the matching
        /// audit events. Returns an empty list when the engine has no
        /// matching data.
        /// </summary>
        Task<IReadOnlyList<AuditEvent>> ExecuteAsync(
            AuditQuery query,
            CancellationToken cancellationToken = default);
    }
}
