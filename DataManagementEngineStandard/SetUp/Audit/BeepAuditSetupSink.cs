using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Services.Audit;
using TheTechIdea.Beep.Services.Audit.Models;
using TheTechIdea.Beep.SetUp.Audit;

namespace TheTechIdea.Beep.SetUp.Audit
{
    /// <summary>
    /// Enterprise <see cref="ISetupAuditSink"/>: adapts setup events onto the tamper-evident,
    /// hash-chained <see cref="IBeepAudit"/> pipeline.
    /// </summary>
    /// <remarks>
    /// The mapping is deliberate: <see cref="IBeepAudit"/>'s <c>AuditEvent</c> has its own field
    /// names (<c>Operation</c>, <c>Category</c>, <c>Outcome</c>, <c>CorrelationId</c>, …) — the same
    /// drift the Studio effort tripped on. Setup-specific fields that don't map to a first-class
    /// column go into <c>Properties</c> so nothing is lost.
    /// </remarks>
    public sealed class BeepAuditSetupSink : ISetupAuditSink
    {
        private readonly IBeepAudit _audit;

        public BeepAuditSetupSink(IBeepAudit audit)
            => _audit = audit ?? throw new ArgumentNullException(nameof(audit));

        public Task RecordAsync(SetupAuditEvent evt, CancellationToken token = default)
        {
            if (evt == null) return Task.CompletedTask;

            var mapped = new AuditEvent
            {
                TimestampUtc = evt.OccurredAt.UtcDateTime,
                Source = "SetUp",
                Operation = evt.Action.ToString(),
                // A schema step is DDL; everything else in setup is configuration.
                Category = evt.Action == SetupAuditAction.StepCompleted && evt.StepId == "schema-setup"
                    ? AuditCategory.Schema
                    : AuditCategory.Config,
                Outcome = evt.Succeeded ? AuditOutcome.Success
                          : evt.Action == SetupAuditAction.Denied ? AuditOutcome.Denied
                          : AuditOutcome.Failure,
                EntityName = evt.StepId,
                UserId = evt.ActorId,
                UserName = evt.ActorId,
                CorrelationId = evt.RunId,
                Reason = evt.Message,
                Properties = new Dictionary<string, object>
                {
                    ["WizardId"] = evt.WizardId,
                    ["AppId"] = evt.AppId,
                    ["Environment"] = evt.Environment,
                    ["DefinitionHash"] = evt.DefinitionHash,
                    ["Action"] = evt.Action.ToString(),
                    ["ActorAuthenticated"] = evt.ActorAuthenticated,
                    ["ElapsedMs"] = evt.Elapsed.TotalMilliseconds
                }
            };

            return _audit.RecordAsync(mapped, token);
        }

        /// <summary>
        /// Not supported: the engine chain stores <c>AuditEvent</c>s, not <c>SetupAuditEvent</c>s, so
        /// a faithful round-trip isn't possible here. Query the underlying <see cref="IBeepAudit"/>
        /// directly (filter <c>Source == "SetUp"</c>). Honest empty rather than a lossy reconstruction.
        /// </summary>
        public Task<IReadOnlyList<SetupAuditEvent>> QueryAsync(string runId, CancellationToken token = default)
            => Task.FromResult<IReadOnlyList<SetupAuditEvent>>(Array.Empty<SetupAuditEvent>());
    }
}
