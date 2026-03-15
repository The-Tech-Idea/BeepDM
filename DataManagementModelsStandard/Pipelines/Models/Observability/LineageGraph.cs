using System.Collections.Generic;
using TheTechIdea.Beep.Pipelines.Models;

namespace TheTechIdea.Beep.Pipelines.Observability
{
    /// <summary>
    /// A graph-shaped view of column-level data lineage between data sources.
    /// Returned by <c>ILineageStore.GetGraphAsync</c> and the Dashboard API.
    /// </summary>
    public class LineageGraph
    {
        /// <summary>All lineage records that form the vertices of this graph.</summary>
        public List<DataLineageRecord> Nodes { get; set; } = new();
        /// <summary>Directed edges between lineage records.</summary>
        public List<LineageEdge> Edges { get; set; } = new();
    }

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Directed edge connecting two lineage nodes via a labelled transformation.</summary>
    public record LineageEdge(string FromNodeId, string ToNodeId, string Label);
}
