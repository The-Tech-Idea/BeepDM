namespace TheTechIdea.Beep.Distributed.Audit
{
    /// <summary>
    /// Destination for <see cref="DistributedAuditEvent"/>s raised
    /// by the distribution tier. Mirrors the shape of
    /// <see cref="Proxy.IProxyAuditSink"/> so adapters can bridge
    /// the two tiers when desired.
    /// </summary>
    /// <remarks>
    /// Implementations <strong>must not throw</strong> and must
    /// complete quickly: audit writes sit on the hot path. Off-load
    /// slow I/O to a bounded background queue (see
    /// <see cref="FileDistributedAuditSink"/>).
    /// </remarks>
    public interface IDistributedAuditSink
    {
        /// <summary>Writes one audit event.</summary>
        void Write(DistributedAuditEvent auditEvent);
    }

    /// <summary>
    /// Shared no-op sink used whenever audit is disabled. Safe to
    /// install as the default — allocates nothing per call.
    /// </summary>
    public sealed class NullDistributedAuditSink : IDistributedAuditSink
    {
        /// <summary>Singleton instance.</summary>
        public static readonly NullDistributedAuditSink Instance = new NullDistributedAuditSink();

        private NullDistributedAuditSink() { }

        /// <inheritdoc/>
        public void Write(DistributedAuditEvent auditEvent) { }
    }
}
