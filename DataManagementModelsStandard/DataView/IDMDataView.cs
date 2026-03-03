using System;
using System.Collections.Generic;
using TheTechIdea.Beep.DataView;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.DataBase
{
    /// <summary>
    /// Pure data model interface for a federated DataView definition.
    /// This interface describes WHAT the DataView IS — its identity, constituent entities,
    /// cross-source join definitions, and federation execution policy.
    /// 
    /// Implemented by: <see cref="DMDataView"/> (POCO/serialisable model).
    /// 
    /// For the runtime DataView ENGINE (generating views, running queries, persisting),
    /// see <see cref="IDataViewOperations"/>, implemented by DataViewDataSource.
    /// </summary>
    public interface IDMDataView
    {
        // ── Identity ──────────────────────────────────────────────────────────
        int ID { get; set; }
        string GuidID { get; set; }

        /// <summary>Display name of this DataView.</summary>
        string ViewName { get; set; }
        int ViewID { get; set; }
        string VID { get; set; }

        /// <summary>The primary ViewType that governs how this view behaves.</summary>
        ViewType Viewtype { get; set; }

        bool Editable { get; set; }

        /// <summary>Optional human-readable description for UI display.</summary>
        string Description { get; set; }

        // ── Source & Engine Registration ──────────────────────────────────────
        /// <summary>The primary single datasource ID (for single-source views).</summary>
        string EntityDataSourceID { get; set; }

        /// <summary>The connection name of the local/in-memory engine used to evaluate federated queries.</summary>
        string LocalEngineConnectionName { get; set; }

        /// <summary>The connection name of the DataView's own datasource (file-based view metadata).</summary>
        string DataViewDataSourceID { get; set; }

        /// <summary>Legacy: composite layer reference.</summary>
        string CompositeLayerDataSourceID { get; set; }

        // ── Federated Entities & Joins ────────────────────────────────────────
        /// <summary>The constituent entities (from one or more sources) that form this view.</summary>
        List<EntityStructure> Entities { get; set; }

        /// <summary>
        /// Defines how entities in this view are joined across data source boundaries.
        /// These are virtual join conditions — not physical FK constraints.
        /// </summary>
        List<FederatedJoinDefinition> JoinDefinitions { get; set; }

        // ── Federation Execution Policy ───────────────────────────────────────
        /// <summary>Determines whether data is cached (Cached) or always fetched live (DirectQuery).</summary>
        FederationExecutionMode ExecutionMode { get; set; }

        /// <summary>Duration in seconds the local materialized cache is valid. Ignored in DirectQuery mode.</summary>
        int CacheTTLSeconds { get; set; }

        /// <summary>Timestamp of the last successful cache refresh. Used to evaluate TTL expiry.</summary>
        DateTime CacheLastRefresh { get; set; }
    }
}
