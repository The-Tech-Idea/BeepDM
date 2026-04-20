using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Services.Audit.Models;

namespace TheTechIdea.Beep.Services.Audit
{
    /// <summary>
    /// Unified, opt-in audit-trail contract for the Beep stack.
    /// Audit events are lossless by policy (never sampled, never silently dropped).
    /// </summary>
    /// <remarks>
    /// When the audit feature is disabled, <see cref="IsEnabled"/> is <c>false</c>
    /// and every method is a fast no-op (see the null implementation supplied by
    /// the engine project).
    /// </remarks>
    public interface IBeepAudit
    {
        /// <summary>
        /// Returns <c>true</c> when the audit feature has been activated.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Records a single audit event.
        /// Implementations sign and sequence the event before enqueueing.
        /// </summary>
        Task RecordAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns the audit events that match <paramref name="filter"/>.
        /// Phase 10 implements the SQLite and file-scan engines; the Phase 01
        /// null implementation returns an empty list.
        /// </summary>
        Task<IReadOnlyList<AuditEvent>> QueryAsync(
            AuditQuery filter,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes every audit event whose <c>UserId</c> equals
        /// <paramref name="userId"/> and re-seals the affected chain segments.
        /// Implemented in Phase 10.
        /// </summary>
        Task PurgeByUserAsync(string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes every audit event for the supplied entity / record key pair
        /// and re-seals the affected chain segments. Implemented in Phase 10.
        /// </summary>
        Task PurgeByEntityAsync(
            string entityName,
            string recordKey,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifies the tamper-evident hash chain over every persisted audit
        /// event. Returns <c>true</c> on success. Implemented in Phase 08.
        /// </summary>
        Task<bool> VerifyIntegrityAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Drains the in-memory queue and awaits each sink's flush.
        /// Used during clean shutdown.
        /// </summary>
        Task FlushAsync(CancellationToken cancellationToken = default);
    }
}
