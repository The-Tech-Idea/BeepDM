using System;

namespace TheTechIdea.Beep.Editor.BeepSync
{
    /// <summary>
    /// Plan-level metadata that travels with a sync schema through its lifecycle
    /// (draft → review → approved → active → archived).
    /// </summary>
    public class SyncPlanMetadata
    {
        /// <summary>Unique plan identifier (same as <c>DataSyncSchema.Id</c>).</summary>
        public string PlanId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>Human-readable plan name.</summary>
        public string Name { get; set; }

        /// <summary>Team or service that owns this plan.</summary>
        public string OwnerTeam { get; set; }

        /// <summary>User who created this plan. Stamped by DefaultsManager (:USERNAME).</summary>
        public string CreatedBy { get; set; }

        /// <summary>UTC creation timestamp. Stamped by DefaultsManager (:NOW).</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>User who last modified this plan.</summary>
        public string LastModifiedBy { get; set; }

        /// <summary>UTC last-modified timestamp.</summary>
        public DateTime LastModifiedAt { get; set; }

        /// <summary>User who approved this plan for production use.</summary>
        public string ApprovedBy { get; set; }

        /// <summary>UTC approval timestamp.</summary>
        public DateTime? ApprovedAt { get; set; }

        /// <summary>
        /// Current lifecycle state: "Draft" | "PendingApproval" | "Approved" | "Active" | "Archived".
        /// </summary>
        public string LifecycleState { get; set; } = "Draft";

        /// <summary>Schema version integer, incremented on each save.</summary>
        public int SchemaVersion { get; set; } = 1;

        /// <summary>Target deployment environment: "Dev" | "Staging" | "Production".</summary>
        public string Environment { get; set; }

        /// <summary>Free-text description of this plan's purpose.</summary>
        public string Description { get; set; }

        /// <summary>
        /// Rule catalog version string expected for all rules referenced by this plan.
        /// Used by CI lint to detect catalog version drift.
        /// </summary>
        public string RuleCatalogVersion { get; set; }

        /// <summary>Version of the mapping plan recorded at the time this metadata was last saved.</summary>
        public int MappingVersion { get; set; } = -1;
    }
}
