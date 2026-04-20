using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Services.Audit.Models;

namespace TheTechIdea.Beep.Services.Audit
{
    /// <summary>
    /// Default <see cref="IBeepAudit"/> used when the audit feature is disabled
    /// or before Phase 02 ships the production pipeline. Every method is a
    /// fast no-op so callers never have to null-check the dependency.
    /// </summary>
    public sealed class NullBeepAudit : IBeepAudit
    {
        private static readonly IReadOnlyList<AuditEvent> EmptyEvents = new AuditEvent[0];

        /// <summary>Singleton instance suitable for direct DI registration.</summary>
        public static readonly NullBeepAudit Instance = new NullBeepAudit();

        /// <inheritdoc />
        public bool IsEnabled => false;

        /// <inheritdoc />
        public Task RecordAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        /// <inheritdoc />
        public Task<IReadOnlyList<AuditEvent>> QueryAsync(
            AuditQuery filter,
            CancellationToken cancellationToken = default)
            => Task.FromResult(EmptyEvents);

        /// <inheritdoc />
        public Task PurgeByUserAsync(string userId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        /// <inheritdoc />
        public Task PurgeByEntityAsync(
            string entityName,
            string recordKey,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        /// <inheritdoc />
        public Task<bool> VerifyIntegrityAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(true);

        /// <inheritdoc />
        public Task FlushAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
