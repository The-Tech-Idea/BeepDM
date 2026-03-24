using System;

namespace TheTechIdea.Beep.Editor.BeepSync
{
    /// <summary>
    /// Immutable version artifact created each time a <see cref="DataSyncSchema"/> is saved or promoted.
    /// Carries the author, timestamp, content hash, and co-versioned mapping/rule-catalog references.
    /// </summary>
    public class SyncSchemaVersion
    {
        /// <summary>The schema this version belongs to.</summary>
        public string SchemaId { get; set; }

        /// <summary>Monotonically increasing version counter (1-based).</summary>
        public int Version { get; set; } = 1;

        /// <summary>Unique identifier for this specific version artifact.</summary>
        public string VersionGuid { get; set; } = Guid.NewGuid().ToString();

        /// <summary>SHA-256 hex digest of the schema's key fields at the time of save.</summary>
        public string SchemaHash { get; set; }

        /// <summary>UTC timestamp when this version was created.</summary>
        public DateTime SavedAt { get; set; } = DateTime.UtcNow;

        /// <summary>User or process that created this version.</summary>
        public string SavedBy { get; set; }

        /// <summary>
        /// Lifecycle state of the schema at this version.
        /// Allowed values: <c>"Draft"</c>, <c>"Review"</c>, <c>"Approved"</c>.
        /// </summary>
        public string ApprovalState { get; set; } = "Draft";

        /// <summary>Reference key to the co-versioned mapping artifact saved alongside this schema version.</summary>
        public string MappingVersion { get; set; }

        /// <summary>Rule catalog version in effect when this version was saved, for rule-drift detection.</summary>
        public string RuleCatalogVersion { get; set; }

        /// <summary>Free-text change notes provided by the author at save/promote time.</summary>
        public string ChangeNotes { get; set; }
    }
}
