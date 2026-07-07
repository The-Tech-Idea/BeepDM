using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.BeepSync
{
    /// <summary>
    /// Result of a CI lint gate for a <see cref="DataSyncSchema"/>.
    /// <see cref="Passed"/> is true when <see cref="Diagnostics"/> is empty.
    /// </summary>
    public sealed class SyncCiGateResult
    {
        public string PlanId { get; init; } = string.Empty;
        public bool Passed { get; init; }
        public IReadOnlyList<SyncCiDiagnostic> Diagnostics { get; init; } = new List<SyncCiDiagnostic>();
    }

    /// <summary>
    /// One diagnostic emitted by the schema lint rule registry. Code is the rule
    /// identifier (e.g. <c>"sync.schema.id-required"</c>).
    /// </summary>
    public sealed class SyncCiDiagnostic
    {
        public string Code { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public string Severity { get; init; } = "warning";   // "warning" | "error"
    }
}
