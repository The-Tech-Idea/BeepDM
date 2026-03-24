namespace TheTechIdea.Beep.Editor.BeepSync
{
    /// <summary>
    /// Defines the incremental-sync (watermark / CDC) behaviour for a <see cref="DataSyncSchema"/>.
    /// When present, the orchestrator uses watermark-based filtering rather than full table loads.
    /// </summary>
    public class WatermarkPolicy
    {
        /// <summary>
        /// Watermark tracking strategy.
        /// Supported values: <c>"Timestamp"</c>, <c>"Sequence"</c>, <c>"CompositeKey"</c>.
        /// </summary>
        public string WatermarkMode { get; set; } = "Timestamp";

        /// <summary>Source field whose value advances per sync run (e.g. "UpdatedAt", "RowVersion").</summary>
        public string WatermarkField { get; set; }

        /// <summary>The last committed watermark value from the previous successful run.</summary>
        public object LastWatermarkValue { get; set; }

        /// <summary>
        /// How many seconds to overlap back from <see cref="LastWatermarkValue"/> when building
        /// the lower-bound filter, to catch records that arrived late in the previous window.
        /// Default 300 (5 minutes).
        /// </summary>
        public int OverlapWindowSeconds { get; set; } = 300;

        /// <summary>
        /// De-duplication strategy when the overlap window produces duplicate rows.
        /// Supported values: <c>"LastWrite"</c>, <c>"SourcePrimary"</c>, <c>"None"</c>.
        /// </summary>
        public string DedupeStrategy { get; set; } = "LastWrite";

        /// <summary>
        /// Optional Rule Engine key that builds an <see cref="AppFilter"/> list from the
        /// current watermark state.  When null the orchestrator builds a simple range filter.
        /// </summary>
        public string FilterRuleKey { get; set; }

        /// <summary>
        /// Rule Engine key invoked to classify late-arriving records after the window closes.
        /// Expected output key <c>"action"</c> = <c>"include"</c> | <c>"quarantine"</c> | <c>"reject"</c>.
        /// </summary>
        public string LateArrivalRuleKey { get; set; }

        /// <summary>
        /// Rule Engine key invoked for CDC tombstone / delete records.
        /// Expected output key <c>"action"</c> = <c>"soft-delete"</c> | <c>"hard-delete"</c> | <c>"mark-inactive"</c>.
        /// </summary>
        public string TombstoneRuleKey { get; set; }

        /// <summary>
        /// When <c>true</c> (default), re-running the same window is idempotent:
        /// the orchestrator applies <see cref="DedupeStrategy"/> to avoid inserting duplicate rows.
        /// </summary>
        public bool ReplayEnabled { get; set; } = true;
    }
}
