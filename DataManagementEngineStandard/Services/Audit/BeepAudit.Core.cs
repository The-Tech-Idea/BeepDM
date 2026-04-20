using System;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Services.Audit.Integrity;
using TheTechIdea.Beep.Services.Audit.Models;
using TheTechIdea.Beep.Services.Audit.Purge;
using TheTechIdea.Beep.Services.Audit.Query;
using TheTechIdea.Beep.Services.Logging;
using TheTechIdea.Beep.Services.Telemetry;

namespace TheTechIdea.Beep.Services.Audit
{
    /// <summary>
    /// Production <see cref="IBeepAudit"/> backed by the shared
    /// <see cref="TelemetryPipeline"/>. Created by
    /// <c>AddBeepAudit</c> when <see cref="BeepAuditOptions.Enabled"/>
    /// is <c>true</c>. When disabled, callers receive
    /// <see cref="NullBeepAudit"/> instead.
    /// </summary>
    /// <remarks>
    /// The class is split across three partial files:
    /// <list type="bullet">
    ///   <item><c>.Core</c> — fields, ctor, <see cref="RecordAsync"/> entry point.</item>
    ///   <item><c>.Query</c> — query / purge / verify (Phase 02 stubs).</item>
    ///   <item><c>.Lifetime</c> — flush semantics.</item>
    /// </list>
    /// </remarks>
    public sealed partial class BeepAudit : IBeepAudit
    {
        private readonly TelemetryPipeline _pipeline;
        private readonly BeepAuditOptions _options;
        private readonly IHashChainSigner _signer;
        private IAuditQueryEngine _queryEngine;
        private GdprPurgeService _purgeService;
        private IntegrityVerifier _integrityVerifier;

        /// <summary>Creates a new audit recorder bound to the supplied pipeline.</summary>
        public BeepAudit(BeepAuditOptions options, TelemetryPipeline pipeline, IHashChainSigner signer = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
            // Signer is optional so legacy hosts that bind audit without
            // tamper-evidence still work; the DI extension provides a
            // real signer when BeepAuditOptions.HashChain is true.
            _signer = signer;
        }

        /// <summary>
        /// Late-binds the Phase 10 query / purge / verify dependencies.
        /// Called once by the DI extension after every storage sink has
        /// been registered so the engines see the same connections /
        /// directories the writer uses.
        /// </summary>
        public void AttachComplianceServices(
            IAuditQueryEngine queryEngine = null,
            GdprPurgeService purgeService = null,
            IntegrityVerifier integrityVerifier = null)
        {
            _queryEngine = queryEngine;
            _purgeService = purgeService;
            _integrityVerifier = integrityVerifier;
        }

        /// <inheritdoc />
        public bool IsEnabled => _options.Enabled;

        /// <inheritdoc />
        public ValueTask RecordValueTaskAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            if (!IsEnabled || auditEvent is null)
            {
                return default;
            }

            // Sign BEFORE enqueue so the chain fields are part of every
            // sink write. A failure here drops the event by design — a
            // missing tamper-evidence stamp must never escape the audit
            // boundary.
            if (_signer is not null && _options.HashChain)
            {
                try
                {
                    _signer.Sign(auditEvent);
                    _pipeline.Metrics?.IncrementChainSigned();
                }
                catch
                {
                    return default;
                }
            }

            TelemetryEnvelope envelope = new TelemetryEnvelope
            {
                Kind = TelemetryKind.Audit,
                Level = BeepLogLevel.Information,
                TimestampUtc = auditEvent.TimestampUtc == default ? DateTime.UtcNow : auditEvent.TimestampUtc,
                Category = auditEvent.Source,
                Message = auditEvent.EntityName,
                CorrelationId = auditEvent.CorrelationId,
                TraceId = auditEvent.TraceId,
                Audit = auditEvent
            };

            return _pipeline.SubmitAuditAsync(envelope, cancellationToken);
        }

        /// <inheritdoc />
        public Task RecordAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            ValueTask vt = RecordValueTaskAsync(auditEvent, cancellationToken);
            return vt.IsCompletedSuccessfully ? Task.CompletedTask : vt.AsTask();
        }
    }
}
