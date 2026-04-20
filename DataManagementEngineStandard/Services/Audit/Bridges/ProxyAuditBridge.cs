using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Proxy;
using TheTechIdea.Beep.Services.Audit.Models;

namespace TheTechIdea.Beep.Services.Audit.Bridges
{
    /// <summary>
    /// Implements <see cref="IProxyAuditSink"/> by forwarding each
    /// <see cref="ProxyAuditEntry"/> into the unified <see cref="IBeepAudit"/>
    /// pipeline. Lets operators consolidate proxy-tier auditing alongside
    /// forms-, schema-, and auth-tier auditing without disturbing the
    /// existing <see cref="FileProxyAuditSink"/>.
    /// </summary>
    /// <remarks>
    /// The bridge is **non-blocking** — <see cref="Write"/> swallows every
    /// exception so it satisfies the strict <see cref="IProxyAuditSink"/>
    /// contract ("must not throw"). Use a <see cref="CompositeProxyAuditSink"/>
    /// (or simple chain) to keep the legacy file sink alongside the bridge
    /// during rollout, then drop the legacy sink once the unified pipeline
    /// is trusted.
    /// </remarks>
    public sealed class ProxyAuditBridge : IProxyAuditSink
    {
        private const string SourcePrefix = "Proxy.Operation.";

        private readonly IBeepAudit _audit;

        /// <summary>Creates a bridge that forwards proxy events to <paramref name="audit"/>.</summary>
        public ProxyAuditBridge(IBeepAudit audit)
        {
            _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        }

        /// <inheritdoc />
        public void Write(ProxyAuditEntry entry)
        {
            if (entry is null) return;
            if (!_audit.IsEnabled) return;

            try
            {
                AuditEvent unified = MapToUnified(entry);
                // Audit pipeline is async by contract; fire-and-forget here is
                // safe because the queue is bounded with backpressure
                // (BackpressureMode.Block) so the producer cannot outrun the writer.
                _ = _audit.RecordAsync(unified);
            }
            catch
            {
                // strict no-throw contract on the proxy data path
            }
        }

        private static AuditEvent MapToUnified(ProxyAuditEntry entry)
        {
            AuditEvent unified = new AuditEvent
            {
                EventId = Guid.NewGuid(),
                TimestampUtc = entry.OccurredAtUtc == default ? DateTime.UtcNow : entry.OccurredAtUtc,
                Source = string.Concat(SourcePrefix, entry.OperationName ?? string.Empty),
                EntityName = entry.OperationName,
                CorrelationId = entry.CorrelationId,
                Category = AuditCategory.Custom,
                Operation = entry.OperationName,
                Outcome = entry.Succeeded ? AuditOutcome.Success : AuditOutcome.Failure,
                Reason = entry.FailureReason,
                Properties = BuildProperties(entry)
            };
            return unified;
        }

        private static IDictionary<string, object> BuildProperties(ProxyAuditEntry entry)
        {
            Dictionary<string, object> bag = new Dictionary<string, object>(StringComparer.Ordinal);
            if (!string.IsNullOrEmpty(entry.SelectedSource))
            {
                bag["proxy.selectedSource"] = entry.SelectedSource;
            }
            bag["proxy.totalAttempts"] = entry.TotalAttempts;
            bag["proxy.elapsedMs"] = entry.ElapsedMs;
            bag["proxy.safety"] = entry.Safety.ToString();
            if (entry.FanOutSucceeded is { Count: > 0 })
            {
                bag["proxy.fanOutSucceeded"] = entry.FanOutSucceeded;
            }
            if (entry.Attempts is { Count: > 0 })
            {
                bag["proxy.attempts.count"] = entry.Attempts.Count;
            }
            return bag;
        }
    }
}
