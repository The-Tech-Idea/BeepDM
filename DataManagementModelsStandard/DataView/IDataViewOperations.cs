using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.DataView;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.DataBase
{
    /// <summary>
    /// Defines the runtime operations of a federated DataView engine — i.e. HOW the DataView WORKS.
    /// Separate from <see cref="IDMDataView"/> which is the pure data model (WHAT it IS).
    ///
    /// Implementation: DataViewDataSource : IDataSource, IDataViewOperations
    /// </summary>
    public interface IDataViewOperations
    {
        /// <summary>The DataView definition this engine is currently managing.</summary>
        IDMDataView DataView { get; set; }

        // ── Entity Management ─────────────────────────────────────────────────
        /// <summary>Returns the next available header ID for a new entity entry.</summary>
        int NextHearId();
        /// <summary>Returns the list position of an entity by its integer ID. Returns -1 if not found.</summary>
        int EntityListIndex(int entityId);
        /// <summary>Returns the list position of an entity by its name. Returns -1 if not found.</summary>
        int EntityListIndex(string entityName);
        /// <summary>Removes the entity with the given ID and all its logical children from the view.</summary>
        IErrorsInfo RemoveEntity(int EntityID);
        /// <summary>Removes all child entities belonging to the given parent entity ID.</summary>
        IErrorsInfo RemoveChildEntities(int EntityID);

        // ── Entity Lifecycle ──────────────────────────────────────────────────
        /// <summary>
        /// Moves an entity to a new parent in the view tree.
        /// If updateJoins=true, auto-discovered FK joins to the old parent are removed.
        /// </summary>
        IErrorsInfo MoveEntity(int entityId, int newParentId, bool updateJoins = true);

        /// <summary>
        /// Returns auto-discovered (non-manual) FK joins that link the entity to its current parent.
        /// Used to identify which joins to remove or rewire after a move.
        /// </summary>
        List<FederatedJoinDefinition> GetAutoJoinsToParent(int entityId);

        /// <summary>Renames an entity in the view (caption + name), updating all join references.</summary>
        IErrorsInfo RenameEntity(int entityId, string newName);

        /// <summary>Deep-clones an entity (fields, relations, metadata) into the view under an optional new parent.</summary>
        EntityStructure DuplicateEntity(int entityId, string newName, int newParentId = 0);

        /// <summary>Persists a display ordering for entities (for tree rendering).</summary>
        IErrorsInfo ReorderEntities(List<int> orderedEntityIds);

        /// <summary>Retrieves a full EntityStructure by name. Returns null if not found.</summary>
        EntityStructure GetEntityStructure(string entityName);

        // ── View Population ───────────────────────────────────────────────────
        /// <summary>Generates the DataView structure from a single table and its child relations.</summary>
        int GenerateViewFromTable(string viewname, IDataSource SourceConnection, string tablename, string SchemaName, string Filterparamters);
        /// <summary>Adds all FK child relations of a root table to the DataView entity list. Returns root entity ID.</summary>
        int GenerateDataView(IDataSource conn, string tablename, string SchemaName, string Filterparamters);
        /// <summary>Creates a new empty IDMDataView object for the given ViewName and connection name.</summary>
        IDMDataView GenerateView(string ViewName, string ConnectionName);
        /// <summary>Generates entity nodes for child tables of a given parent within an existing DataView.</summary>
        IErrorsInfo GenerateDataViewForChildNode(IDataSource conn, int pid, string tablename, string SchemaName, string Filterparamters);
        /// <summary>Adds a single table (and FK child relations) from a data source. Returns root entity ID.</summary>
        int AddEntitytoDataView(IDataSource conn, string tablename, string SchemaName, string Filterparamters);
        /// <summary>Adds a pre-built EntityStructure into the DataView. Returns entity ID.</summary>
        int AddEntitytoDataView(EntityStructure maintab);
        /// <summary>Adds a table as a child entity under a specified parent within the DataView.</summary>
        int AddEntityAsChild(IDataSource conn, string tablename, string SchemaName, string Filterparamters, int viewindex, int ParentTableIndex);

        // ── Field / Column Management ─────────────────────────────────────────
        /// <summary>Returns all fields for the named entity in the view.</summary>
        List<EntityField> GetEntityFields(string entityName);

        /// <summary>Adds a computed/virtual field to a view entity definition (does not modify source).</summary>
        IErrorsInfo AddFieldToEntity(string entityName, EntityField field);

        /// <summary>Removes (hides) a field from the view entity definition.</summary>
        IErrorsInfo RemoveFieldFromEntity(string entityName, string fieldName);

        /// <summary>
        /// Re-fetches the entity's field schema from its source datasource.
        /// Returns a list of detected change descriptions (added/removed/modified fields).
        /// </summary>
        List<string> RefreshEntitySchema(string entityName);

        // ── Cache Management ──────────────────────────────────────────────────
        /// <summary>Forces a full cache invalidation. The next query re-materializes all entities from sources.</summary>
        void InvalidateCache();

        /// <summary>Returns a snapshot of the current cache state.</summary>
        (bool IsExpired, int RowCount, DateTime LastRefresh, int SecondsTTLRemaining) GetCacheStatus();

        /// <summary>Async materialisation of all entities into the temp DB. Respects cancellation.</summary>
        Task MaterializeAsync(CancellationToken cancellationToken = default);

        /// <summary>Returns the SQL JOIN chain that will be executed for the current view (for debugging).</summary>
        string GetExecutionPlan();

        // ── Persistence ───────────────────────────────────────────────────────
        /// <summary>Serializes the DataView definition to a JSON file in the default DataView folder.</summary>
        void WriteDataViewFile(string filename);
        /// <summary>Serializes the DataView definition to a JSON file at an explicit path.</summary>
        void WriteDataViewFile(string path, string filename);
        /// <summary>Deserializes a DataView definition from the JSON file at the given path.</summary>
        IDMDataView ReadDataViewFile(string pathandfilename);
        /// <summary>Loads and applies the persisted DataView definition from its backing file.</summary>
        IErrorsInfo LoadView();

        // ── Relation Builder ───────────────────────────────────────────────────
        /// <summary>Manually defines a cross-source join. Returns the GuidID of the new FederatedJoinDefinition.</summary>
        string AddJoin(string leftEntityName,  string leftColumn,  string leftDataSourceID,
                       string rightEntityName, string rightColumn, string rightDataSourceID,
                       FederatedJoinType joinType        = FederatedJoinType.Inner,
                       string description                = null,
                       string additionalCondition        = null);
        /// <summary>Removes a join by GuidID. Returns true if found and removed.</summary>
        bool RemoveJoin(string joinGuidID);
        /// <summary>Updates an existing join in-place. Returns false if not found.</summary>
        bool UpdateJoin(string joinGuidID,
                        string leftEntityName,  string leftColumn,
                        string rightEntityName, string rightColumn,
                        FederatedJoinType joinType,
                        string description        = null,
                        string additionalCondition = null);
        /// <summary>Retrieves a single join by GuidID. Returns null if not found.</summary>
        FederatedJoinDefinition GetJoin(string joinGuidID);
        /// <summary>Returns all joins involving the named entity (left or right side).</summary>
        List<FederatedJoinDefinition> GetJoinsFor(string entityName);
        /// <summary>Validates all joins: checks entity and column existence. Returns error messages.</summary>
        List<string> ValidateJoins();
        /// <summary>Returns FK/PK-typed fields for a named entity (for a column-picker UI).</summary>
        List<EntityField> GetJoinableColumns(string entityName);
        /// <summary>Builds the SQL JOIN clause for a FederatedJoinDefinition.</summary>
        string BuildJoinSQL(FederatedJoinDefinition join);
        /// <summary>Removes all manually-defined joins (IsManuallyDefined=true). Leaves auto-discovered ones.</summary>
        void ClearManualJoins();
        /// <summary>Removes all join definitions — manual and auto-discovered.</summary>
        void ClearAllJoins();

        // ── Join Graph Intelligence ───────────────────────────────────────────
        /// <summary>Re-runs FK discovery across all entities; adds newly discovered joins only.</summary>
        IErrorsInfo AutoDetectJoins();

        /// <summary>
        /// Finds the shortest join path between two entities through existing JoinDefinitions.
        /// Returns ordered entity names (e.g. ["ORDERS","ORDER_ITEMS","PRODUCTS"]), or null if unreachable.
        /// </summary>
        List<string> FindJoinPath(string fromEntityName, string toEntityName);

        /// <summary>Returns the full join graph as an adjacency list: {EntityName → [connected entity names]}.</summary>
        Dictionary<string, List<string>> GetJoinGraph();

        // ── Validation & Health ───────────────────────────────────────────────
        /// <summary>Comprehensive view check: entities in sources + joins valid + datasources reachable.</summary>
        List<string> ValidateView();

        /// <summary>Checks all view entities still exist in their source datasources.</summary>
        List<string> ValidateEntities();

        /// <summary>Returns entity names whose source datasource is unavailable.</summary>
        List<string> GetBrokenEntities();

        /// <summary>Returns entities not connected to any join (isolated in the join graph).</summary>
        List<string> GetUnconnectedEntities();

        /// <summary>Diffs the named entity's current fields vs its live source schema. Returns change descriptions.</summary>
        List<string> DetectSchemaChanges(string entityName);

        // ── Query & Preview ───────────────────────────────────────────────────
        /// <summary>Fetches up to maxRows rows from a single entity using its source datasource directly.</summary>
        IEnumerable<object> GetEntityPreview(string entityName, int maxRows = 100);

        /// <summary>Fetches up to maxRows rows from the full joined view (materializes cache if needed).</summary>
        IEnumerable<object> GetViewPreview(int maxRows = 100);

        // ── Filter Management ─────────────────────────────────────────────────
        /// <summary>Stores a WHERE-clause filter on an entity applied during cache materialization.</summary>
        IErrorsInfo SetEntityFilter(string entityName, string filterExpression);

        /// <summary>Returns the current filter expression for a named entity.</summary>
        string GetEntityFilter(string entityName);

        /// <summary>Removes the filter for a named entity.</summary>
        IErrorsInfo ClearEntityFilter(string entityName);

        /// <summary>Clears all entity filters across the entire view.</summary>
        void ClearAllFilters();

        // ── View Utilities ────────────────────────────────────────────────────
        /// <summary>Deep copies the entire DataView (entities, joins, filters) under a new name.</summary>
        IDMDataView CloneView(string newViewName);

        /// <summary>Absorbs entities and joins from another view into this one (union, no duplicates).</summary>
        IErrorsInfo MergeView(IDMDataView otherView);

        /// <summary>Returns distinct DataSourceIDs referenced by all entities in the view.</summary>
        List<string> GetAllDataSourceIDs();

        /// <summary>Returns a lightweight summary of the view's current state.</summary>
        ViewSummary GetViewSummary();
    }
}
