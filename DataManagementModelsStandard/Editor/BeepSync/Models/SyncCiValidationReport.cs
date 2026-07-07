using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.BeepSync
{
    /// <summary>
    /// Composite CI report: the lint gate, the plan diff summary, and the artifact bundle
    /// that the export produced. The diff is a human-readable summary string; the
    /// artifact bundle is the on-disk path the caller can re-locate.
    /// </summary>
    public sealed class SyncCiValidationReport
    {
        public string PlanId { get; init; } = string.Empty;
        public SyncCiGateResult Lint { get; init; } = new();
        public string PlanDiffSummary { get; init; } = string.Empty;
        public string ArtifactBundlePath { get; init; } = string.Empty;
        public IReadOnlyList<string> ArtifactFiles { get; init; } = new List<string>();
        public string RuleCatalogVersion { get; init; } = string.Empty;
    }
}
