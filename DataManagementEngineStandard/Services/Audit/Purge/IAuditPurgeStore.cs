using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Services.Audit.Models;

namespace TheTechIdea.Beep.Services.Audit.Purge
{
    /// <summary>
    /// Storage capability that the GDPR purge service consumes (Phase 10).
    /// A store must be able to delete events by predicate, enumerate the
    /// remaining events of a chain, and re-write the chain fields in
    /// place after the signer recomputes them.
    /// </summary>
    /// <remarks>
    /// Only stores that can guarantee atomic delete + rewrite semantics
    /// (SQLite WAL is the v1 reference implementation) should advertise
    /// this capability. NDJSON file purge is a Phase 13 follow-up because
    /// rewriting a sealed/gzipped audit file requires cooperation with
    /// the rotation/seal policy.
    /// </remarks>
    public interface IAuditPurgeStore
    {
        /// <summary>Friendly name used in purge audit summaries.</summary>
        string Name { get; }

        /// <summary>
        /// Deletes every audit row whose <c>UserId</c> equals
        /// <paramref name="userId"/>. Returns the affected
        /// <c>chainId</c>s so the caller can re-seal each one.
        /// </summary>
        Task<PurgeImpact> DeleteByUserAsync(string userId, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes every audit row matching the supplied entity / record
        /// pair. <paramref name="recordKey"/> may be <c>null</c> to purge
        /// every record of the entity.
        /// </summary>
        Task<PurgeImpact> DeleteByEntityAsync(string entityName, string recordKey, CancellationToken cancellationToken);

        /// <summary>
        /// Returns every remaining event of <paramref name="chainId"/> in
        /// strict <c>Sequence</c> order so the re-seal pass can recompute
        /// the chain hashes from the first surviving record onward.
        /// </summary>
        Task<IReadOnlyList<AuditEvent>> ReadChainAsync(string chainId, CancellationToken cancellationToken);

        /// <summary>
        /// Persists the recomputed chain fields
        /// (<see cref="AuditEvent.Sequence"/>,
        /// <see cref="AuditEvent.PrevHash"/>,
        /// <see cref="AuditEvent.Hash"/>) for every event in
        /// <paramref name="resealed"/>. Implementations should run the
        /// updates inside a single transaction so a crash either commits
        /// the new chain or leaves the prior chain intact.
        /// </summary>
        Task UpdateChainAsync(string chainId, IReadOnlyList<AuditEvent> resealed, CancellationToken cancellationToken);
    }
}
