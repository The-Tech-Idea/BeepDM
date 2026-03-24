using System;

namespace TheTechIdea.Beep.Editor.BeepSync
{
    /// <summary>
    /// Controls how the Rule Engine is applied during a sync run.
    /// All properties are opt-in; null / false = feature inactive.
    /// </summary>
    public class SyncRulePolicy
    {
        /// <summary>When false the Rule Engine is never invoked for this schema.</summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Catalog version tag expected to be present in the registered RuleCatalog.
        /// Used by CI lint to detect version drift.
        /// </summary>
        public string CatalogVersion { get; set; }

        /// <summary>
        /// Maximum rule evaluation depth.  Maps directly to <c>RuleExecutionPolicy.MaxDepth</c>.
        /// 0 = use engine default.
        /// </summary>
        public int MaxDepth { get; set; } = 10;

        /// <summary>
        /// Maximum wall-clock time in ms before a rule evaluation is aborted.
        /// 0 = no timeout.
        /// </summary>
        public int MaxExecutionMs { get; set; } = 5000;
    }

    /// <summary>
    /// Controls how DefaultsManager is applied before destination writes.
    /// </summary>
    public class SyncDefaultsPolicy
    {
        /// <summary>When false DefaultsManager is never invoked for this schema.</summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// When true, <c>DefaultsManager.Apply</c> is called before every destination INSERT.
        /// </summary>
        public bool ApplyOnInsert { get; set; } = true;

        /// <summary>
        /// When true, <c>DefaultsManager.Apply</c> is called before every destination UPDATE
        /// (typically to refresh audit-trail fields such as UpdatedAt / UpdatedBy).
        /// </summary>
        public bool ApplyOnUpdate { get; set; } = false;

        /// <summary>
        /// Optional override key for the <c>EntityDefaultsProfile</c>.
        /// When null the (datasourceName, entityName) pair is used as the lookup key.
        /// </summary>
        public string ProfileKey { get; set; }
    }

    /// <summary>
    /// Controls how MappingManager is consulted during a sync run.
    /// </summary>
    public class SyncMappingPolicy
    {
        /// <summary>When false MappingManager integrations are skipped.</summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Minimum mapping quality score (0–100) required before a sync plan is approved.
        /// Scores below this threshold produce an Error-severity preflight issue.
        /// </summary>
        public int MinQualityScore { get; set; } = 70;

        /// <summary>
        /// Approval state that must be recorded on the compiled mapping plan before
        /// the schema is considered eligible for production rollout.
        /// Serialised as string so models project has no dependency on MappingApprovalState enum.
        /// Expected values: "Draft" | "ReviewedAndApproved" | "Approved"
        /// </summary>
        public string RequiredApprovalState { get; set; } = "Draft";

        /// <summary>
        /// Action taken when drift is detected between the stored mapping plan and the
        /// live entity structures.
        /// Expected values: "Warn" | "Block" | "AutoRemapAndReview"
        /// </summary>
        public string OnDriftAction { get; set; } = "Warn";

        /// <summary>
        /// When true, the compiled mapping plan is cached on the synchronisation context
        /// for the lifetime of the run and reused across all parallel batch tasks.
        /// </summary>
        public bool CacheCompiledPlan { get; set; } = true;
    }
}
