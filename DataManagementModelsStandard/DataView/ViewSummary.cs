using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.DataView
{
    /// <summary>
    /// Lightweight snapshot of a DataView's current health and structure.
    /// Returned by <see cref="TheTechIdea.Beep.DataBase.IDataViewOperations.GetViewSummary"/>.
    /// </summary>
    public class ViewSummary
    {
        /// <summary>Total number of entities in the view.</summary>
        public int EntityCount { get; set; }

        /// <summary>Total join definitions (manual + auto-discovered).</summary>
        public int JoinCount { get; set; }

        /// <summary>Joins that were created manually via the Relation Builder.</summary>
        public int ManualJoinCount { get; set; }

        /// <summary>Number of distinct DataSource IDs referenced by entities.</summary>
        public int DataSourceCount { get; set; }

        /// <summary>True if ValidateView() returned no errors.</summary>
        public bool IsValid { get; set; }

        /// <summary>Validation warnings or errors (empty when IsValid=true).</summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>Entities not connected to any join (potential orphans).</summary>
        public List<string> UnconnectedEntities { get; set; } = new List<string>();

        /// <summary>Entities whose datasource is currently unavailable.</summary>
        public List<string> BrokenEntities { get; set; } = new List<string>();

        /// <summary>Last time the local cache was successfully materialised.</summary>
        public DateTime CacheLastRefresh { get; set; }

        /// <summary>Configured cache TTL in seconds.</summary>
        public int CacheTTLSeconds { get; set; }

        /// <summary>Whether the cache has expired and needs re-materialisation.</summary>
        public bool IsCacheExpired =>
            (DateTime.UtcNow - CacheLastRefresh).TotalSeconds >= CacheTTLSeconds;
    }
}
