using System;

namespace TheTechIdea.Beep.DataView
{
    /// <summary>
    /// Defines how a federated DataView resolves its data at query time.
    /// </summary>
    public enum FederationExecutionMode
    {
        /// <summary>
        /// Data is materialized into a local/in-memory engine and cached for CacheTTLSeconds.
        /// Best for read-heavy analytical workloads.
        /// </summary>
        Cached,

        /// <summary>
        /// Data is always fetched live from source databases on every query.
        /// Best for real-time or low-volume data where staleness is unacceptable.
        /// </summary>
        DirectQuery
    }
}
