using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.SetUp.Audit
{
    /// <summary>
    /// Where setup events are recorded so a run is answerable: who ran what, against which
    /// environment, when, with what result.
    /// </summary>
    /// <remarks>
    /// Before this, <c>SetupReport</c> was in-memory and dropped, and the checkpoint was overwritten
    /// in place — each run destroyed the prior run's record. A sink is append-only (solo) or backed
    /// by the tamper-evident <c>IBeepAudit</c> chain (enterprise).
    /// </remarks>
    public interface ISetupAuditSink
    {
        Task RecordAsync(SetupAuditEvent evt, CancellationToken token = default);

        Task<IReadOnlyList<SetupAuditEvent>> QueryAsync(string runId, CancellationToken token = default);
    }

    public enum SetupAuditAction
    {
        RunStarted,
        StepStarted,
        StepCompleted,
        StepSkipped,
        StepFailed,
        Denied,
        RollbackStarted,
        RollbackCompleted,
        RunCompleted,
        RunFailed
    }

    public sealed class SetupAuditEvent
    {
        public string RunId { get; set; }
        public string WizardId { get; set; }
        public string AppId { get; set; }
        public string Environment { get; set; }

        /// <summary>The Phase 2 definition ContentHash — <em>what</em> was applied.</summary>
        public string DefinitionHash { get; set; }

        public string StepId { get; set; }
        public SetupAuditAction Action { get; set; }

        public string ActorId { get; set; }
        /// <summary>Never imply a solo run was authenticated.</summary>
        public bool ActorAuthenticated { get; set; }

        public bool Succeeded { get; set; }
        public string Message { get; set; }
        public TimeSpan Elapsed { get; set; }
        public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
