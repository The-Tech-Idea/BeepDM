namespace TheTechIdea.Beep.Services.Audit.Integrity
{
    /// <summary>
    /// Reason a chain verification call rejected an event. The verifier
    /// reports the <strong>first</strong> kind it encounters and stops
    /// — once divergence happens, every later hash is suspect.
    /// </summary>
    public enum IntegrityIssueKind
    {
        /// <summary>No divergence was detected.</summary>
        None = 0,

        /// <summary>Sequence is not <c>previous + 1</c> (gap or duplicate).</summary>
        SequenceGap = 1,

        /// <summary><c>PrevHash</c> does not match the prior event's <c>Hash</c>.</summary>
        PrevHashMismatch = 2,

        /// <summary>Recomputed <c>Hash</c> does not match the stored one.</summary>
        HashMismatch = 3,

        /// <summary>Persisted anchor disagrees with the chain's last hash/sequence.</summary>
        AnchorMismatch = 4,

        /// <summary>Event references a different <c>ChainId</c> than the rest.</summary>
        ChainIdMismatch = 5,

        /// <summary>Event payload was malformed and could not be hashed.</summary>
        PayloadInvalid = 6
    }
}
