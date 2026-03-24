using System;
using System.Collections.Generic;


namespace TheTechIdea.Beep.Editor.BeepSync
{
    /// <summary>
    /// Carries the per-run CDC evaluation context that is passed to Rule Engine CDC rules.
    /// Populated by the orchestrator before each incremental sync window, and updated
    /// after the window closes to stamp late-arrival and tombstone decisions.
    /// </summary>
    public class CdcFilterContext
    {
        /// <summary>Schema Id this context belongs to.</summary>
        public string SchemaId { get; set; }

        /// <summary>The watermark field name used in this window.</summary>
        public string WatermarkField { get; set; }

        /// <summary>Lower-bound watermark value for this window (inclusive, overlap applied).</summary>
        public object WindowStart { get; set; }

        /// <summary>Upper-bound watermark value for this window (exclusive).</summary>
        public object WindowEnd { get; set; }

        /// <summary>UTC time the window was opened.</summary>
        public DateTime WindowOpenedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Resolved <see cref="AppFilter"/> list produced by the CDC filter rule (or the
        /// default range filter when no rule key is configured).
        /// </summary>
        public List<AppFilter> ResolvedFilters { get; set; } = new List<AppFilter>();

        /// <summary>
        /// Action decided for late-arriving records in this window.
        /// Values: <c>"include"</c>, <c>"quarantine"</c>, <c>"reject"</c>.
        /// </summary>
        public string LateArrivalAction { get; set; } = "include";

        /// <summary>
        /// Action decided for delete / tombstone records in this window.
        /// Values: <c>"soft-delete"</c>, <c>"hard-delete"</c>, <c>"mark-inactive"</c>.
        /// </summary>
        public string TombstoneAction { get; set; } = "soft-delete";

        /// <summary>
        /// Whether mapping drift was detected for the watermark field during preflight.
        /// </summary>
        public bool WatermarkFieldDrifted { get; set; }

        /// <summary>The new watermark high-water mark to commit after a successful run.</summary>
        public object NewWatermarkValue { get; set; }
    }
}
