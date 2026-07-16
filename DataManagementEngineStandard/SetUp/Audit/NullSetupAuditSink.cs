using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.SetUp.Audit;

namespace TheTechIdea.Beep.SetUp.Audit
{
    /// <summary>No-op sink used when no audit is configured. Records nothing.</summary>
    public sealed class NullSetupAuditSink : ISetupAuditSink
    {
        public static readonly NullSetupAuditSink Instance = new();

        public Task RecordAsync(SetupAuditEvent evt, CancellationToken token = default) => Task.CompletedTask;

        public Task<IReadOnlyList<SetupAuditEvent>> QueryAsync(string runId, CancellationToken token = default)
            => Task.FromResult<IReadOnlyList<SetupAuditEvent>>(Array.Empty<SetupAuditEvent>());
    }
}
