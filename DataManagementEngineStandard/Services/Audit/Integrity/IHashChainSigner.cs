using TheTechIdea.Beep.Services.Audit.Models;

namespace TheTechIdea.Beep.Services.Audit.Integrity
{
    /// <summary>
    /// Assigns the tamper-evident chain fields (<c>Sequence</c>,
    /// <c>PrevHash</c>, <c>Hash</c>) to an <see cref="AuditEvent"/>
    /// before it is enqueued onto the audit pipeline. Verifies a chain
    /// after the fact via <see cref="VerifyChain"/>.
    /// </summary>
    /// <remarks>
    /// Sign operations are single-threaded per <c>ChainId</c> so the
    /// Sequence numbers stay strictly monotonic even when many producers
    /// audit concurrently. The signer reads its initial state from the
    /// configured <see cref="IChainAnchorStore"/> on first use of a
    /// chain and writes the new anchor after every sign.
    /// </remarks>
    public interface IHashChainSigner
    {
        /// <summary>
        /// Assigns <see cref="AuditEvent.Sequence"/>,
        /// <see cref="AuditEvent.PrevHash"/>, and
        /// <see cref="AuditEvent.Hash"/> on <paramref name="auditEvent"/>
        /// in place. Returns the same instance for fluent chaining.
        /// </summary>
        AuditEvent Sign(AuditEvent auditEvent);

        /// <summary>
        /// Recomputes the hash chain over the supplied event sequence
        /// and returns the first divergence (if any). Pass an
        /// <paramref name="expectedAnchor"/> to also verify continuity
        /// with the persisted anchor.
        /// </summary>
        IntegrityCheckResult VerifyChain(System.Collections.Generic.IEnumerable<AuditEvent> events, ChainAnchor expectedAnchor = null);

        /// <summary>
        /// Re-numbers and re-hashes <paramref name="survivors"/> in the
        /// supplied order for <paramref name="chainId"/>. Used by the
        /// GDPR purge service after deleting events out of the middle
        /// of a chain. The signer's in-memory chain state and the
        /// configured <see cref="IChainAnchorStore"/> are updated to
        /// the new tail so subsequent <see cref="Sign(AuditEvent)"/>
        /// calls continue without producing a sequence gap.
        /// </summary>
        System.Collections.Generic.IReadOnlyList<AuditEvent> Reseal(string chainId, System.Collections.Generic.IReadOnlyList<AuditEvent> survivors);
    }
}
