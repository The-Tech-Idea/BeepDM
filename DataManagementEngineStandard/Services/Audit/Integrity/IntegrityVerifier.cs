using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Services.Audit.Models;

namespace TheTechIdea.Beep.Services.Audit.Integrity
{
    /// <summary>
    /// High-level façade over <see cref="IHashChainSigner.VerifyChain"/>.
    /// Loads a chain (or sub-chain) from any
    /// <see cref="IAuditEventReader"/> and runs verification against the
    /// persisted anchor. Used by the operator runbook (Phase 13) and
    /// the optional periodic background check (Phase 11).
    /// </summary>
    public sealed class IntegrityVerifier
    {
        private readonly IHashChainSigner _signer;
        private readonly IChainAnchorStore _anchorStore;

        /// <summary>Creates a verifier wrapping the supplied signer and anchor store.</summary>
        public IntegrityVerifier(IHashChainSigner signer, IChainAnchorStore anchorStore)
        {
            _signer = signer ?? throw new ArgumentNullException(nameof(signer));
            _anchorStore = anchorStore ?? throw new ArgumentNullException(nameof(anchorStore));
        }

        /// <summary>
        /// Verifies the supplied in-memory event sequence against the
        /// chain anchor for the chain id of the first event.
        /// </summary>
        public IntegrityCheckResult Verify(IEnumerable<AuditEvent> events)
        {
            if (events is null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            // Materialize once so we can also look up the anchor by the
            // chain id of the first event.
            List<AuditEvent> buffer = new List<AuditEvent>();
            string chainId = null;
            foreach (AuditEvent ev in events)
            {
                if (ev is null) { continue; }
                if (chainId is null)
                {
                    chainId = string.IsNullOrEmpty(ev.ChainId) ? AuditEvent.DefaultChainId : ev.ChainId;
                }
                buffer.Add(ev);
            }

            ChainAnchor anchor = chainId is null ? null : _anchorStore.TryRead(chainId);
            return _signer.VerifyChain(buffer, anchor);
        }

        /// <summary>
        /// Convenience overload that loads events from
        /// <paramref name="reader"/> for the supplied chain id and
        /// verifies them.
        /// </summary>
        public IntegrityCheckResult Verify(IAuditEventReader reader, string chainId)
        {
            if (reader is null)
            {
                throw new ArgumentNullException(nameof(reader));
            }
            string id = string.IsNullOrEmpty(chainId) ? AuditEvent.DefaultChainId : chainId;
            IEnumerable<AuditEvent> events = reader.ReadChain(id);
            ChainAnchor anchor = _anchorStore.TryRead(id);
            return _signer.VerifyChain(events, anchor);
        }
    }
}
