using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Services.Audit.Models;

namespace TheTechIdea.Beep.Services.Audit.Bridges
{
    /// <summary>
    /// Bridge skeleton that will forward distributed-tier audit events
    /// (resharding, plan changes, cross-shard transactions) into the
    /// unified <see cref="IBeepAudit"/> pipeline once the
    /// <c>IDistributedAuditSink</c> contract from the Distributed Phase 13
    /// plan lands.
    /// </summary>
    /// <remarks>
    /// Until the distributed sink interface exists this bridge offers a
    /// strongly typed <see cref="ForwardAsync(string, string, AuditOutcome, IDictionary{string, object}, CancellationToken)"/>
    /// API so callers (resharding planner, transaction coordinator) can
    /// already write through it. Once the canonical interface lands the
    /// bridge will implement it without breaking these call sites.
    /// </remarks>
    public sealed class DistributedAuditBridge
    {
        private const string SourcePrefix = "Distributed.";

        private readonly IBeepAudit _audit;

        /// <summary>Creates a bridge that forwards distributed events to <paramref name="audit"/>.</summary>
        public DistributedAuditBridge(IBeepAudit audit)
        {
            _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        }

        /// <summary>
        /// Records a distributed-tier audit event with category
        /// <see cref="AuditCategory.Distributed"/>.
        /// </summary>
        /// <param name="component">Originating component (e.g. <c>Reshard</c>, <c>Tx</c>, <c>Plan</c>).</param>
        /// <param name="operation">Operation name (e.g. <c>SplitShard</c>, <c>CommitGlobal</c>).</param>
        /// <param name="outcome">Result classification.</param>
        /// <param name="properties">Optional structured property bag.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public Task ForwardAsync(
            string component,
            string operation,
            AuditOutcome outcome,
            IDictionary<string, object> properties = null,
            CancellationToken cancellationToken = default)
        {
            if (!_audit.IsEnabled) return Task.CompletedTask;

            AuditEvent unified = new AuditEvent
            {
                Source = string.Concat(SourcePrefix, component ?? string.Empty),
                EntityName = component,
                Category = AuditCategory.Distributed,
                Operation = operation,
                Outcome = outcome,
                Properties = properties
            };

            try
            {
                return _audit.RecordAsync(unified, cancellationToken);
            }
            catch
            {
                return Task.CompletedTask;
            }
        }
    }
}
