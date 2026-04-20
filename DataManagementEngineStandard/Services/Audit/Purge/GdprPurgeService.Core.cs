using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Services.Audit.Integrity;

namespace TheTechIdea.Beep.Services.Audit.Purge
{
    /// <summary>
    /// GDPR-style purge service. Removes audit events that match a
    /// privacy request, then re-seals every affected hash chain so the
    /// integrity verifier (Phase 08) keeps reporting a clean chain after
    /// the deletion.
    /// </summary>
    /// <remarks>
    /// Split into four partials:
    /// <list type="bullet">
    ///   <item><c>.Core</c> — fields, ctor, gating, summary emission.</item>
    ///   <item><c>.User</c> — <see cref="PurgeByUserAsync"/>.</item>
    ///   <item><c>.Entity</c> — <see cref="PurgeByEntityAsync"/>.</item>
    ///   <item><c>.ResealChain</c> — chain replay + signer hand-off.</item>
    /// </list>
    /// Every successful purge writes a synthetic
    /// <see cref="Models.AuditCategory.Custom"/> event to the dedicated
    /// <c>"purge"</c> chain so the act of purging is itself auditable
    /// and tamper-evident.
    /// </remarks>
    public sealed partial class GdprPurgeService
    {
        /// <summary>Logical chain id used for synthetic purge events.</summary>
        public const string PurgeChainId = "purge";

        private readonly IReadOnlyList<IAuditPurgeStore> _stores;
        private readonly IHashChainSigner _signer;
        private readonly IPurgePolicy _policy;
        private readonly IBeepAudit _audit;

        /// <summary>Creates a purge service over the supplied storage + chain primitives.</summary>
        public GdprPurgeService(
            IReadOnlyList<IAuditPurgeStore> stores,
            IHashChainSigner signer,
            IPurgePolicy policy,
            IBeepAudit audit)
        {
            _stores = stores ?? throw new ArgumentNullException(nameof(stores));
            _signer = signer;
            _policy = policy;
            _audit = audit;
        }

        /// <summary>Returns <c>true</c> when at least one purge store is registered.</summary>
        public bool IsAvailable => _stores.Count > 0;

        private void EnsureAuthorized(string confirmationToken)
        {
            if (_policy is null)
            {
                throw new InvalidOperationException(
                    "Purge service has no IPurgePolicy. Register one via BeepAuditOptions.PurgeConfirmationToken or a custom IPurgePolicy.");
            }
            if (!_policy.Authorize(confirmationToken))
            {
                throw new UnauthorizedAccessException("Purge confirmation token did not match the configured policy.");
            }
        }
    }
}
