using System.Collections.Generic;
using System.Threading.Tasks;
using TheTechIdea.Beep.Pipelines.Models;
using TheTechIdea.Beep.Pipelines.Observability;

namespace TheTechIdea.Beep.Pipelines.Interfaces
{
    /// <summary>
    /// Query interface for column-level data lineage records accumulated during pipeline runs.
    /// Implemented by <c>ObservabilityStore</c>.
    /// </summary>
    public interface ILineageStore
    {
        /// <summary>Get all lineage records recorded for a specific run.</summary>
        Task<IReadOnlyList<DataLineageRecord>> GetByRunAsync(string runId);

        /// <summary>
        /// Trace which source columns fed a destination field (backward lineage).
        /// Returns a chain: <c>destField ← transformer ← sourceField ← ...</c>
        /// </summary>
        Task<IReadOnlyList<DataLineageRecord>> TraceBackwardAsync(
            string destDataSource, string destEntity, string destField);

        /// <summary>
        /// Trace all downstream fields that received data from a source column (forward).
        /// </summary>
        Task<IReadOnlyList<DataLineageRecord>> TraceForwardAsync(
            string srcDataSource, string srcEntity, string srcField);

        /// <summary>Return the full lineage graph between two data sources.</summary>
        Task<LineageGraph> GetGraphAsync(string srcDataSource, string destDataSource);
    }
}
