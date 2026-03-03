using System;

namespace TheTechIdea.Beep.DataView
{
    /// <summary>Defines the type of JOIN to apply between two federated entities.</summary>
    public enum FederatedJoinType
    {
        Inner,
        LeftOuter,
        RightOuter,
        FullOuter,
        Cross
    }

    /// <summary>
    /// Describes a virtual join relationship between two entities in a federated DataView.
    /// Unlike EntityStructure.Relations (physical FK within one source),
    /// this describes cross-source logical join conditions that may be auto-discovered
    /// from FK metadata or manually defined via the Relation Builder API.
    /// </summary>
    public class FederatedJoinDefinition
    {
        /// <summary>Unique identifier for this join definition.</summary>
        public string GuidID { get; set; } = Guid.NewGuid().ToString();

        // ── Left side ─────────────────────────────────────────────────────────
        /// <summary>The logical name of the left entity as it appears in the DataView.</summary>
        public string LeftEntityName { get; set; }

        /// <summary>The column of the left entity to join on.</summary>
        public string LeftColumn { get; set; }

        /// <summary>The datasource connection name that owns the left entity.</summary>
        public string LeftDataSourceID { get; set; }

        // ── Right side ────────────────────────────────────────────────────────
        /// <summary>The logical name of the right entity as it appears in the DataView.</summary>
        public string RightEntityName { get; set; }

        /// <summary>The column of the right entity to join on.</summary>
        public string RightColumn { get; set; }

        /// <summary>The datasource connection name that owns the right entity.</summary>
        public string RightDataSourceID { get; set; }

        // ── Join definition ───────────────────────────────────────────────────
        /// <summary>The type of SQL JOIN to apply.</summary>
        public FederatedJoinType JoinType { get; set; } = FederatedJoinType.Inner;

        /// <summary>
        /// True if this join was defined manually via the Relation Builder API.
        /// False if auto-discovered from FK metadata (GetChildTablesList).
        /// </summary>
        public bool IsManuallyDefined { get; set; } = false;

        /// <summary>
        /// Optional additional filter condition appended to the JOIN ON clause.
        /// E.g. "ORDERS.YEAR = 2024" → ON L.col = R.col AND (ORDERS.YEAR = 2024)
        /// </summary>
        public string AdditionalCondition { get; set; }

        /// <summary>Optional description/label for UI display.</summary>
        public string Description { get; set; }
    }
}
