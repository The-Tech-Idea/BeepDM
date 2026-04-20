using System.Collections.Generic;

namespace TheTechIdea.Beep.Services.Audit.Models
{
    /// <summary>
    /// Phase 08 partial extending <see cref="AuditEvent"/> with the
    /// classification, outcome, change-set, and tamper-evident chain
    /// fields. Splitting these into a separate file keeps the original
    /// identity surface in <c>AuditEvent.cs</c> and avoids a single
    /// monolithic class definition.
    /// </summary>
    public partial class AuditEvent
    {
        /// <summary>
        /// High-level routing/classification. Defaults to
        /// <see cref="AuditCategory.Unspecified"/> so legacy producers
        /// that do not set a category continue to compile.
        /// </summary>
        public AuditCategory Category { get; set; } = AuditCategory.Unspecified;

        /// <summary>
        /// Operation name (e.g. <c>Insert</c>, <c>Update</c>, <c>Delete</c>,
        /// <c>Login</c>, <c>Grant</c>, <c>Migrate</c>). Free-form so
        /// subsystem-specific verbs are permitted.
        /// </summary>
        public string Operation { get; set; }

        /// <summary>Result of the audited operation.</summary>
        public AuditOutcome Outcome { get; set; } = AuditOutcome.Success;

        /// <summary>
        /// Failure / denial reason. Optional; present only when
        /// <see cref="Outcome"/> is not <see cref="AuditOutcome.Success"/>.
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// Per-column change set for CRUD-style events. Producers should
        /// emit only the columns that actually changed to keep audit
        /// storage bounded.
        /// </summary>
        public IList<AuditFieldChange> FieldChanges { get; set; } = new List<AuditFieldChange>();

        /// <summary>
        /// Logical chain identifier. Defaults to <c>"default"</c>;
        /// operators may segment by tenant or category to keep chain
        /// verification cheap when audit volumes are large.
        /// </summary>
        public string ChainId { get; set; } = DefaultChainId;

        /// <summary>
        /// Strictly-increasing sequence within the chain, assigned by
        /// the <c>HashChainSigner</c>. Defaults to <c>0</c> (genesis)
        /// and is overwritten on sign.
        /// </summary>
        public long Sequence { get; set; }

        /// <summary>
        /// Hex-encoded HMAC of the prior event in the chain. Empty for
        /// the genesis event of a chain. Set by the signer.
        /// </summary>
        public string PrevHash { get; set; }

        /// <summary>
        /// Hex-encoded HMAC-SHA256 over canonical payload concatenated
        /// with <see cref="PrevHash"/>. Set by the signer; verified by
        /// <c>IntegrityVerifier</c>.
        /// </summary>
        public string Hash { get; set; }

        /// <summary>Default chain identifier when none is supplied.</summary>
        public const string DefaultChainId = "default";
    }
}
