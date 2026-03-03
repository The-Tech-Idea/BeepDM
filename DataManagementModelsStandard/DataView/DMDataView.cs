using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;

using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.DataView
{
    /// <summary>
    /// Pure data model for a federated DataView definition.
    /// This class IS the data — it is serialised to/from JSON by the DataViewDataSource engine.
    /// It does NOT contain any engine logic (generating views, writing files, running queries).
    /// 
    /// For the engine behaviour, see DataViewDataSource which implements IDataViewOperations.
    /// </summary>
    public class DMDataView : IDMDataView
    {
        // ── Identity ────────────────────────────────────────────────────────
        public int ID { get; set; }
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
        public string VID { get; set; } = Guid.NewGuid().ToString();
        public string ViewName { get; set; }
        public int ViewID { get; set; }
        public ViewType Viewtype { get; set; }
        public bool Editable { get; set; }

        /// <summary>Optional human-readable description for UI display.</summary>
        public string Description { get; set; }

        // ── Source & Engine Registration ────────────────────────────────────
        public string EntityDataSourceID { get; set; }
        public string DataViewDataSourceID { get; set; }
        public string CompositeLayerDataSourceID { get; set; }

        /// <summary>
        /// The registered connection name of the local/in-memory engine (DuckDB, SQLite, etc.)
        /// used to evaluate federated queries. Null = auto-discover at runtime.
        /// </summary>
        public string LocalEngineConnectionName { get; set; }

        // ── Federated Entities & Joins ──────────────────────────────────────
        public List<EntityStructure> Entities { get; set; } = new List<EntityStructure>();

        /// <summary>
        /// Cross-source virtual join definitions. These are logical join conditions, not physical FKs.
        /// Populated automatically from FK discovery, or manually via the Relation Builder API.
        /// </summary>
        public List<FederatedJoinDefinition> JoinDefinitions { get; set; } = new List<FederatedJoinDefinition>();

        // ── Federation Execution Policy ─────────────────────────────────────
        /// <summary>Default to Cached for performance. Set to DirectQuery for always-live data.</summary>
        public FederationExecutionMode ExecutionMode { get; set; } = FederationExecutionMode.Cached;

        /// <summary>Duration in seconds the materialized cache is valid. Default: 5 minutes.</summary>
        public int CacheTTLSeconds { get; set; } = 300;

        /// <summary>Timestamp of last successful cache refresh.</summary>
        public DateTime CacheLastRefresh { get; set; } = DateTime.MinValue;

        // ── Constructors ────────────────────────────────────────────────────
        public DMDataView() { }

        public DMDataView(string pTableName)
        {
            ViewName = pTableName;
            Viewtype = ViewType.Table;
        }

        public DMDataView(string pTableName, ViewType viewtype)
        {
            ViewName = pTableName;
            Viewtype = viewtype;
        }
    }
}
