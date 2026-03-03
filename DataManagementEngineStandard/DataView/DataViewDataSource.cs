using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using System.ComponentModel;

namespace TheTechIdea.Beep.DataView
{
    /// <summary>
    /// Represents a data source for a data view.
    /// </summary>
    public partial class DataViewDataSource : IDataSource, IDataViewOperations, IDisposable
    {
        /// <summary>
        /// Event that is raised when a specific event is passed.
        /// </summary>
        public event EventHandler<PassedArgs> PassEvent;
        /// <summary>Gets or sets the type of the data source.</summary>
        /// <value>The type of the data source.</value>
        public DataSourceType DatasourceType { get; set; }
        /// <summary>Gets or sets the category of the datasource.</summary>
        /// <value>The category of the datasource.</value>
        public DatasourceCategory Category { get; set; }
        /// <summary>Gets or sets the data connection.</summary>
        /// <value>The data connection.</value>
        public IDataConnection Dataconnection { get; set; }
        /// <summary>Gets or sets the name of the data source.</summary>
        /// <value>The name of the data source.</value>
        public string DatasourceName { get; set; }
        /// <summary>Gets or sets the error object.</summary>
        /// <value>The error object.</value>
        public IErrorsInfo ErrorObject { get; set; }
        /// <summary>Gets or sets the ID.</summary>
        /// <value>The ID.</value>
        public string Id { get; set; }
        /// <summary>Gets or sets the ID.</summary>
        /// <value>The ID.</value>
        public int ID { get; set; }
        /// <summary>Gets or sets the GUID ID.</summary>
        /// <value>The GUID ID.</value>
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
        /// <summary>Gets or sets the logger for the current object.</summary>
        /// <value>The logger.</value>
        public IDMLogger Logger { get; set; }
        /// <summary>Gets or sets the list of entity names.</summary>
        /// <value>The list of entity names.</value>
        public List<string> EntitiesNames { get; set; } = new List<string>();
        /// <summary>Gets or sets the DME editor.</summary>
        /// <value>The DME editor.</value>
        public IDMEEditor DMEEditor { get; set; }
        /// <summary>Gets or sets the current connection status.</summary>
        /// <value>The current connection status.</value>
        public ConnectionState ConnectionStatus { get { return Dataconnection.ConnectionStatus; } set { } }
        /// <summary>Gets or sets the source entity data.</summary>
        /// <value>The source entity data.</value>
        public DataTable SourceEntityData { get; set; }
        /// <summary>Gets or sets the data view for the IDM.</summary>
        /// <value>The data view for the IDM.</value>
        public IDMDataView DataView { get; set; } = new DMDataView();
        /// <summary>Gets or sets the column delimiter used in data processing.</summary>
        /// <value>The column delimiter.</value>
        /// <remarks>The default value is "''".</remarks>
        public virtual string ColumnDelimiter { get; set; } = "''";
        /// <summary>Gets or sets the delimiter used for separating parameters.</summary>
        /// <value>The parameter delimiter.</value>
        public virtual string ParameterDelimiter { get; set; } = ":";
        /// <summary>Gets or sets the list of entity structures.</summary>
        /// <value>The list of entity structures.</value>
        public List<EntityStructure> Entities
        {
            get
            {
                if (DataView != null)
                {

                    return DataView.Entities;
                }
                else
                {
                    return new List<EntityStructure>();
                }

            }
            set
            {
                DataView.Entities = value;
            }
        }
        /// <summary>Gets or sets the name of the view.</summary>
        /// <value>The name of the view.</value>
        public string ViewName { get; set; }
        /// <summary>Gets or sets the ID of the view.</summary>
        /// <value>The ID of the view.</value>
        public int ViewID
        {
            get
            {
                if (DataView != null)
                {
                    return DataView.ViewID;
                }
                else
                {
                    return -1;
                }

            }
            set
            {
                DataView.ViewID = value;
            }
        }
        /// <summary>The type of view.</summary>
        public ViewType Viewtype
        {
            get
            {
                if (DataView != null)
                {
                    return DataView.Viewtype;
                }
                else
                {
                    return ViewType.Table;
                }
            }
            set
            {
                DataView.Viewtype = value;
            }
        }

        /// <summary>Gets or sets a value indicating whether the object is editable.</summary>
        /// <value><c>true</c> if the object is editable; otherwise, <c>false</c>.</value>
        public bool Editable { get; set; }
        /// <summary>Gets or sets the ID of the entity data source.</summary>
        /// <value>The ID of the entity data source.</value>
        public string EntityDataSourceID { get; set; }
        /// <summary>Gets or sets the ID of the composite layer data source.</summary>
        /// <value>The ID of the composite layer data source.</value>
        public string CompositeLayerDataSourceID { get; set; }
        /// <summary>Gets or sets the ID of the data source for the DataView.</summary>
        /// <value>The ID of the data source.</value>
        public string DataViewDataSourceID
        {
            get
            {
                return DataView.DataViewDataSourceID;
            }
            set
            {
                DataView.DataViewDataSourceID = value;
            }
        }
        /// <summary>The Vendor ID (VID) of a device.</summary>
        /// <remarks>
        /// The Vendor ID (VID) is a unique identifier assigned to a device manufacturer by the USB Implementers Forum (USB-IF).
        /// It is used to identify the manufacturer of a USB device.
        /// </remarks>
        public string VID
        {
            get
            {
                if (DataView != null)
                {
                    return DataView.VID;
                }
                else
                {
                    return null;
                }

            }
            set
            {
                DataView.VID = value;
            }
        }
        string DataViewFile;
        string FileName;
        public bool FileLoaded { get; set; } = false;
        int EntityIndex { get; set; } = 0;
        IDataSource ds;

        // ── Federation Cache Control ────────────────────────────────────────
        /// <summary>Whether caching is enabled at all. Set to false for always-live "DirectQuery" mode.</summary>
        public bool CacheEnabled { get; set; } = true;

        // ── IDMDataView Federation Members ─────────────────────────────────
        /// <summary>Optional human-readable description for UI display.</summary>
        public string Description { get; set; }

        /// <summary>The registered connection name of the local/in-memory engine used to evaluate federated queries.</summary>
        public string LocalEngineConnectionName
        {
            get => DataView?.LocalEngineConnectionName;
            set { if (DataView != null) DataView.LocalEngineConnectionName = value; }
        }

        /// <summary>Cross-source virtual join definitions.</summary>
        public List<FederatedJoinDefinition> JoinDefinitions
        {
            get => DataView?.JoinDefinitions ?? new List<FederatedJoinDefinition>();
            set { if (DataView != null) DataView.JoinDefinitions = value; }
        }

        /// <summary>Determines whether data is cached (Cached) or always fetched live (DirectQuery).</summary>
        public FederationExecutionMode ExecutionMode
        {
            get => DataView?.ExecutionMode ?? FederationExecutionMode.Cached;
            set { if (DataView != null) DataView.ExecutionMode = value; }
        }

        /// <summary>Duration in seconds the materialized temp DB is valid.</summary>
        public int CacheTTLSeconds
        {
            get => DataView?.CacheTTLSeconds ?? 300;
            set { if (DataView != null) DataView.CacheTTLSeconds = value; }
        }

        /// <summary>Timestamp of the last successful cache refresh.</summary>
        public DateTime CacheLastRefresh
        {
            get => DataView?.CacheLastRefresh ?? DateTime.MinValue;
            set { if (DataView != null) DataView.CacheLastRefresh = value; }
        }

        /// <summary>
        /// Initializes a new instance of the DataViewDataSource class.
        /// </summary>
        /// <param name="datasourcename">The name of the data source.</param>
        /// <param name="logger">The logger object used for logging.</param>
        /// <param name="pDMEEditor">The DME editor object.</param>
        /// <param name="pDatasourceType">The type of the data source.</param>
        /// <param name="per">The error information object.</param>
        public DataViewDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType pDatasourceType, IErrorsInfo per)
        {
           
            Logger = logger;
            ErrorObject = per;
            DMEEditor = pDMEEditor;
            DatasourceType = DataSourceType.Text;
            Category = DatasourceCategory.VIEWS;
            Dataconnection = new DataViewConnection
            {
                Logger = logger,
                ErrorObject = ErrorObject,
                DMEEditor = DMEEditor,
                IsLogical = true  // DataView is always a logical/virtual source — no physical file needed for connection
            };
            string filepath;
            if (Path.GetDirectoryName(datasourcename) == null)
            {
                filepath = Path.Combine(DMEEditor.ConfigEditor.Config.Folders.FirstOrDefault(c => c.FolderFilesType == FolderFileTypes.DataView).FolderPath, datasourcename);

            }
            else
            {
                filepath = Path.GetDirectoryName(datasourcename);
            }
            string filename = Path.GetFileName(datasourcename);
          
            List<ConnectionProperties> cnlist = DMEEditor.ConfigEditor.DataConnections.Where(p => p.FileName != null && p.Category == DatasourceCategory.VIEWS).ToList();
           
            if (cnlist.Where(c => c.FileName.Equals(filename, StringComparison.InvariantCultureIgnoreCase)).Any())
            {
                Dataconnection.ConnectionProp = cnlist.Where(c => c.FileName.Equals(filename, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                filepath = Dataconnection.ConnectionProp.FilePath;
               
            }
            else
            {
                filepath = DMEEditor.ConfigEditor.Config.Folders.FirstOrDefault(c => c.FolderFilesType == FolderFileTypes.DataView).FolderPath;
                Dataconnection.ConnectionProp = new ConnectionProperties();
                Dataconnection.ConnectionProp.FileName = filename;
                Dataconnection.ConnectionProp.FilePath = filepath;
                Dataconnection.ConnectionProp.Category = DatasourceCategory.VIEWS;
                Dataconnection.ConnectionProp.DatabaseType = DataSourceType.Json;
                Dataconnection.ConnectionProp.DriverVersion = "1";
                Dataconnection.ConnectionProp.DriverName = "DataViewReader";
                Dataconnection.ConnectionProp.ConnectionName = filename;
                DMEEditor.ConfigEditor.DataConnections.Add((ConnectionProperties)Dataconnection.ConnectionProp);
                DMEEditor.ConfigEditor.SaveDataconnectionsValues();

            }
            DatasourceName = filename;
            DataViewFile = Path.Combine(filepath, filename);

        }
        /// <summary>
        /// Begins a transaction with the specified arguments.
        /// </summary>
        /// <param name="args">The arguments passed to the transaction.</param>
        /// <returns>An object that provides information about any errors that occurred during the transaction.</returns>
        public virtual IErrorsInfo BeginTransaction(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {

            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Beep", $"Error in Begin Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }

        /// <summary>Ends a transaction and returns information about any errors that occurred.</summary>
        /// <param name="args">The arguments passed to the transaction.</param>
        /// <returns>An object containing information about any errors that occurred during the transaction.</returns>
        public virtual IErrorsInfo EndTransaction(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {

            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Beep", $"Error in end Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }

        /// <summary>Commits the changes made with the provided arguments.</summary>
        /// <param name="args">The arguments containing the changes to be committed.</param>
        /// <returns>An object implementing the IErrorsInfo interface that provides information about any errors that occurred during the commit process.</returns>
        public virtual IErrorsInfo Commit(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {

            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Beep", $"Error in Begin Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        /// <summary>Returns a list of entities.</summary>
        /// <returns>A list of entities.</returns>
        public IEnumerable<string> GetEntitesList()
        {
            ErrorObject.Flag = Errors.Ok;
            List<string> retval = new List<string>();
            try
            {
                if (DataView.Entities.Count <= 2)
                {
                    LoadView();

                    if (DataView.VID == null)
                    {
                        VID = Guid.NewGuid().ToString();
                    }
                    ViewName = DataView.ViewName;

                    Entities = DataView.Entities;
                }
                foreach (EntityStructure i in DataView.Entities) //.Where(x=>x.Id>1)
                {
                    if (string.IsNullOrEmpty(i.Caption))
                    {
                        retval.Add(i.EntityName);
                    }
                    else
                        retval.Add(i.Caption);
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error in getting entity Data from ({ex.Message})", DateTime.Now, -1, "", Errors.Failed);
            }
            EntitiesNames = retval;
            return retval;
        }
        /// <summary>Retrieves a scalar value asynchronously based on the provided query.</summary>
        /// <param name="query">The query used to retrieve the scalar value.</param>
        /// <returns>A task representing the asynchronous operation. The task result is the scalar value.</returns>
        public virtual Task<double> GetScalarAsync(string query)
        {
            return Task.Run(() => GetScalar(query));
        }
        /// <summary>
        /// Gets a scalar numeric value from a query.
        /// Routes through the federated temp DB (same as RunQuery/ExecuteSql)
        /// so cross-source aggregations like COUNT, SUM, AVG work correctly.
        /// </summary>
        public virtual double GetScalar(string query)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                RefreshCacheIfExpired();
                var db = PrepareMergedQueryDB();
                if (db != null)
                {
                    return db.GetScalar(query);
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error in GetScalar ({ex.Message})", DateTime.Now, 0, "", Errors.Failed);
            }
            return 0.0;
        }
        /// <summary>
        /// Retrieves an entity by name and filter.
        /// For federated (cross-source) entities — where the entity is not natively readable from a
        /// single source — routes the read through the materialized local/in-memory database.
        /// For single-source entities, delegates directly to the source datasource.
        /// </summary>
        public IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
        {
            IEnumerable<object> retval = null;
            EntityStructure ent = GetEntityStructure(EntityName);
            if (ent == null) return null;

            // P8 Fix: Check if this entity comes from multiple sources (federated path)
            // A federated/cross-source entity is one where the view has entities from different DataSourceIDs
            bool isCrossSource = DataView.Entities
                .Where(e => e.ParentId == 0)
                .Select(e => e.DataSourceID)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count() > 1;

            if (isCrossSource && ent.Viewtype == ViewType.Table)
            {
                // Route through the local federated engine
                RefreshCacheIfExpired();
                var db = PrepareMergedQueryDB();
                if (db != null)
                {
                    return db.GetEntity(EntityName, filter);
                }
            }

            // Single-source fallback
            IDataSource srcDs = GetDataSourceObject(EntityName);
            if (srcDs != null)
            {
                if (srcDs.ConnectionStatus == ConnectionState.Open)
                {
                    switch (ent.Viewtype)
                    {
                        case ViewType.File:
                        case ViewType.Url:
                        case ViewType.Table:
                        case ViewType.Query:
                            retval = GetDataSourceObject(EntityName).GetEntity(EntityName, filter);
                            break;
                        default:
                            retval = null;
                            break;
                    }
                }
            }
            return retval;
        }
        /// <summary>Retrieves an entity based on the specified entity name and filter.</summary>
        /// <param name="EntityName">The name of the entity to retrieve.</param>
        /// <param name="filter">A list of filters to apply to the entity.</param>
        /// <returns>The retrieved entity.</returns>
        public PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            IEnumerable<object> retval = null;
            IDataSource ds = GetDataSourceObject(EntityName);
            if (ds != null)
            {
                if (ds.ConnectionStatus == ConnectionState.Open)
                {
                    EntityStructure ent = GetEntityStructure(EntityName);
                    if (ent != null)
                    {
                      return ds.GetEntity(EntityName, filter, pageNumber, pageSize);
                    }
                }
            }
            PagedResult pagedResult = null;
            return pagedResult;
        }
        /// <summary>Returns the index of an entity in the entity list.</summary>
        /// <param name="entityid">The ID of the entity.</param>
        /// <returns>The index of the entity in the entity list.</returns>
        public int EntityListIndex(int entityid)
        {
            return DataView.Entities.FindIndex(a => a.Id == entityid);
        }
        /// <summary>Returns the index of an entity in the entity list.</summary>
        /// <param name="entityname">The name of the entity.</param>
        /// <returns>The index of the entity in the entity list.</returns>
        public int EntityListIndex(string entityname)
        {
            int retval = Entities.FindIndex(a => a.DatasourceEntityName.Equals(entityname, StringComparison.InvariantCultureIgnoreCase));
            if (retval == -1)
            {
                retval = Entities.FindIndex(a => a.EntityName.Equals(entityname, StringComparison.InvariantCultureIgnoreCase));

            }
            if (retval == -1)
            {
                retval = Entities.FindIndex(a => a.Caption.Equals(entityname, StringComparison.InvariantCultureIgnoreCase));

            }
            return retval;
        }
        /// <summary>Retrieves the structure of an entity.</summary>
        /// <param name="EntityName">The name of the entity.</param>
        /// <param name="refresh">Optional. Specifies whether to refresh the structure. Default is false.</param>
        /// <returns>The structure of the entity.</returns>
        public EntityStructure GetEntityStructure(string EntityName, bool refresh = false)
        {
            try
            {
                EntityStructure r = new EntityStructure();
                EntityStructure dh = (EntityStructure)Entities[EntityListIndex(EntityName)].Clone();
                if (refresh)
                {

                    switch (dh.Viewtype)
                    {
                        case ViewType.Table:
                        case ViewType.Query:
                        case ViewType.File:
                        case ViewType.Url:
                            r = (EntityStructure)GetDataSourceObject(dh.EntityName).GetEntityStructure(dh, refresh).Clone();
                            dh.Fields = r.Fields;
                            dh.Relations = r.Relations;
                            dh.PrimaryKeys = r.PrimaryKeys;
                            break;
                        case ViewType.Code:
                        default:
                            break;
                    }
                }
                else
                {
                    return dh;
                }
                return dh;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error getting entity structure {EntityName} ({ex.Message})", DateTime.Now, -1, "", Errors.Failed);
            }
            return null;
        }
        /// <summary>Gets the type of an entity based on its name.</summary>
        /// <param name="entityname">The name of the entity.</param>
        /// <returns>The type of the entity.</returns>
        public Type GetEntityType(string entityname)
        {
            EntityStructure dh = Entities[EntityListIndex(entityname)];
            Type retval;
            switch (dh.Viewtype)
            {
                case ViewType.Table:
                    retval = GetDataSourceObject(entityname).GetEntityType(entityname);
                    break;
                case ViewType.Query:

                case ViewType.Code:

                case ViewType.File:

                case ViewType.Url:


                default:

                    DMTypeBuilder.CreateNewObject(DMEEditor, entityname, entityname, dh.Fields);
                    retval = DMTypeBuilder.MyType;
                    break;
            }
            return retval;
        }
        /// <summary>Retrieves a list of child tables for a given parent table.</summary>
        /// <param name="tablename">The name of the parent table.</param>
        /// <param name="SchemaName">The name of the schema containing the parent table.</param>
        /// <param name="Filterparamters">Additional filter parameters to refine the search.</param>
        /// <returns>A list of ChildRelation objects representing the child tables.</returns>
        public IEnumerable<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            EntityStructure dh = Entities[EntityListIndex(tablename)];

            switch (dh.Viewtype)
            {
                case ViewType.Table:
                    return GetDataSourceObject(tablename).GetChildTablesList(tablename, SchemaName, Filterparamters);
                case ViewType.Query:
                case ViewType.Code:
                case ViewType.File:
                case ViewType.Url:
                default:
                    return null;
            }

        }
        /// <summary>Retrieves the foreign keys of an entity.</summary>
        /// <param name="entityname">The name of the entity.</param>
        /// <param name="SchemaName">The name of the schema.</param>
        /// <returns>A list of RelationShipKeys representing the foreign keys of the entity.</returns>
        public IEnumerable<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            IDataSource ds = GetDataSourceObject(entityname);
            if (ds.ConnectionStatus == ConnectionState.Open)
            {
                if (ds.Category == DatasourceCategory.RDBMS)
                {

                    return ds.GetEntityforeignkeys(entityname, SchemaName);
                }
                else
                {
                    return null;
                }
            }
            else
            {
                DMEEditor.AddLogMessage("Error", "$Could not Find DataSource {DatasourceName}", DateTime.Now, 0, DatasourceName, Errors.Failed);
                return null;
            }

        }
        private IDataSource _tempDb = null;

        /// <summary>
        /// Forces a full cache invalidation. The next call to RunQuery or ExecuteSql will
        /// re-materialize all entities from their source databases.
        /// </summary>
        public void InvalidateCache()
        {
            _tempDb = null;
            CacheLastRefresh = DateTime.MinValue;
            DMEEditor.AddLogMessage("Info", "DataView federation cache invalidated.", DateTime.Now, 0, "", Errors.Ok);
        }

        /// <summary>
        /// Checks whether the cache TTL has expired and invalidates the temp DB if so.
        /// This forces the next call to PrepareMergedQueryDB() to re-sync from source databases.
        /// </summary>
        private void RefreshCacheIfExpired()
        {
            bool isDirectQuery = !CacheEnabled || ExecutionMode == FederationExecutionMode.DirectQuery;
            if (isDirectQuery)
            {
                _tempDb = null;
                return;
            }

            if (_tempDb != null && (DateTime.Now - CacheLastRefresh).TotalSeconds > CacheTTLSeconds)
            {
                DMEEditor.AddLogMessage("Info", $"DataView cache expired (TTL={CacheTTLSeconds}s). Re-materializing entities.", DateTime.Now, 0, "", Errors.Ok);
                _tempDb = null;
            }
        }

        /// <summary>
        /// Prepares a temporary InMemory or Local DB, materializes all DataView entities into it, and syncs their data.
        /// </summary>
        private IDataSource PrepareMergedQueryDB()
        {
            if (_tempDb != null) return _tempDb;

            IDataSource targetDB = null;

            // 1. Use explicitly pinned engine if set on the view
            if (!string.IsNullOrWhiteSpace(LocalEngineConnectionName))
            {
                targetDB = DMEEditor.GetDataSource(LocalEngineConnectionName);
            }

            // 2. Auto-discovery: prefer DuckDB, then SQLite, then any available local/in-memory engine
            if (targetDB == null)
            {
                var availableConns = DMEEditor.ConfigEditor.DataConnections
                    .Where(p => p.Category == DatasourceCategory.INMEMORY || p.IsLocal).ToList();

                if (availableConns.Any())
                {
                    var preferredConn = availableConns.FirstOrDefault(x => x.DatabaseType == DataSourceType.DuckDB)
                                     ?? availableConns.FirstOrDefault(x => x.DatabaseType == DataSourceType.SqlLite)
                                     ?? availableConns.First();

                    targetDB = DMEEditor.GetDataSource(preferredConn.ConnectionName);
                }
            }

            if (targetDB == null)
            {
                DMEEditor.AddLogMessage("Fail", "No InMemory or LocalDB available to evaluate merged DataView queries.", DateTime.Now, -1, "", Errors.Failed);
                return null;
            }

            if (targetDB is IInMemoryDB memDB)
            {
                memDB.OpenDatabaseInMemory("TempDataView_" + Guid.NewGuid().ToString("N"));
            }

            bool isDuckDbTarget = targetDB.DatasourceType == DataSourceType.DuckDB;

            foreach(var ent in DataView.Entities)
            {
                if (ent.ParentId == 0 && string.Equals(ent.EntityName, DataView.ViewName, StringComparison.OrdinalIgnoreCase))
                    continue;

                IDataSource sourceDB = DMEEditor.GetDataSource(ent.DataSourceID);
                if (sourceDB == null) continue;

                EntityStructure newEnt = (EntityStructure)ent.Clone();

                // Advanced Federation: DuckDB Native Pushdown
                if (isDuckDbTarget && 
                   (sourceDB.DatasourceType == DataSourceType.SqlLite || 
                    sourceDB.DatasourceType == DataSourceType.Postgre || 
                    sourceDB.DatasourceType == DataSourceType.Mysql))
                {
                    try
                    {
                        string attachmentName = $"{sourceDB.DatasourceName}_db".Replace(" ", "_").Replace(".", "_");
                        // Generate Native ATTACH for DuckDB
                        string attachSql = "";
                        if (sourceDB.DatasourceType == DataSourceType.SqlLite)
                        {
                            attachSql = $"ATTACH '{sourceDB.Dataconnection.ConnectionProp.ConnectionString}' AS {attachmentName} (TYPE SQLITE);";
                        }
                        else if (sourceDB.DatasourceType == DataSourceType.Postgre)
                        {
                            attachSql = $"ATTACH '{sourceDB.Dataconnection.ConnectionProp.ConnectionString}' AS {attachmentName} (TYPE POSTGRES);";
                        }
                        else if (sourceDB.DatasourceType == DataSourceType.Mysql)
                        {
                             attachSql = $"ATTACH '{sourceDB.Dataconnection.ConnectionProp.ConnectionString}' AS {attachmentName} (TYPE MYSQL);";
                        }

                        if (!string.IsNullOrEmpty(attachSql))
                        {
                            targetDB.ExecuteSql(attachSql);
                            // Map the logical view entity to the attached physical table
                            string createViewSql = $"CREATE OR REPLACE VIEW {newEnt.EntityName} AS SELECT * FROM {attachmentName}.{ent.DatasourceEntityName};";
                            targetDB.ExecuteSql(createViewSql);
                            continue; // Bypasses the C# memory transfer completely!
                        }
                    }
                    catch (Exception ex)
                    {
                        DMEEditor.AddLogMessage("Warning", $"Native DuckDB attach failed for {ent.EntityName}, falling back to memory transfer: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                    }
                }

                // Fallback: Materialized Copy
                if (targetDB.CheckEntityExist(newEnt.EntityName))
                {
                    if (targetDB is ILocalDB localDB)
                    {
                         localDB.DropEntity(newEnt.EntityName);
                    }
                }

                targetDB.CreateEntityAs(newEnt);

                sourceDB.Openconnection();
                try
                {
                    // Basic Predicate Pushdown (if AppFilters were extracted from a Query AST, they'd be passed here)
                    var data = sourceDB.GetEntity(ent.DatasourceEntityName, new List<AppFilter>());
                    if (data != null)
                    {
                        targetDB.UpdateEntities(newEnt.EntityName, data, DMEEditor.progress);
                    }
                }
                catch (Exception ex)
                {
                     DMEEditor.AddLogMessage("Fail", $"Failed syncing {ent.EntityName} to temp DB: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                }
            }
            
            _tempDb = targetDB;
            CacheLastRefresh = DateTime.Now;
            return _tempDb;
        }

        /// <summary>Executes the given SQL statement.</summary>
        /// <param name="sql">The SQL statement to execute.</param>
        /// <returns>An object containing information about any errors that occurred during execution.</returns>
        public IErrorsInfo ExecuteSql(string sql)
        {
            try
            {
                RefreshCacheIfExpired();
                var db = PrepareMergedQueryDB();
                if (db != null)
                {
                    return db.ExecuteSql(sql);
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error in ExecuteSql: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        /// <summary>Creates an entity using the provided entity structure.</summary>
        /// <param name="entity">The structure of the entity to be created.</param>
        /// <returns>True if the entity was successfully created, false otherwise.</returns>
        public bool CreateEntityAs(EntityStructure entity)
        {
            try
            {
                AddEntitytoDataView(entity);
                DMEEditor.AddLogMessage("Success", $"Creating Entity", DateTime.Now, 0, null, Errors.Ok);
                return true;
            }
            catch (Exception ex)
            {
                string errmsg = "Error Creating Entity ";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                return false;
            }

        }
        /// <summary>Checks if an entity with the given name exists.</summary>
        /// <param name="entityname">The name of the entity to check.</param>
        /// <returns>True if the entity exists, false otherwise.</returns>
        public bool CheckEntityExist(string entityname)
        {
            if (Entities.Any(x => x.EntityName.Equals(entityname, StringComparison.InvariantCultureIgnoreCase)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>Gets the data source object for a given entity name.</summary>
        /// <param name="entityname">The name of the entity.</param>
        /// <returns>The data source object associated with the given entity name.</returns>
        private IDataSource GetDataSourceObject(string entityname)
        {
            IDataSource retval;
            EntityStructure dh = Entities.Where(x => string.Equals(x.EntityName, entityname, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            if (dh == null)
            {
                retval = DMEEditor.GetDataSource(DataView.EntityDataSourceID);

            }
            else
            {
                retval = DMEEditor.GetDataSource(dh.DataSourceID);
            }
            if (retval != null)
            {
                retval.Dataconnection.OpenConnection();
                retval.ConnectionStatus = Dataconnection.ConnectionStatus;
            }
            return retval;
        }
        /// <summary>Opens a connection to a database.</summary>
        /// <returns>The state of the connection.</returns>
        public ConnectionState Openconnection()
        {
           ConnectionStatus = Dataconnection.OpenConnection();
            if (ConnectionStatus == ConnectionState.Open)
            {
                LoadView();
            }
            return ConnectionStatus;
        }
        /// <summary>Closes the connection and returns the current state of the connection.</summary>
        /// <returns>The current state of the connection after closing.</returns>
        public ConnectionState Closeconnection()
        {
            return ConnectionStatus;
        }

        /// <summary>Executes a query and returns the result.</summary>
        /// <param name="qrystr">The query string to execute.</param>
        /// <returns>The result of the query execution.</returns>
        public IEnumerable<object> RunQuery(string qrystr)
        {
            try
            {
                RefreshCacheIfExpired();
                var db = PrepareMergedQueryDB();
                if (db != null)
                {
                    object result = db.RunQuery(qrystr);
                    if (result is IEnumerable<object> enumResult) return enumResult;
                    return new BindingList<object> { result };
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error in RunQuery: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
            }
            return new BindingList<object>();
        }
        /// <summary>Updates entities in the system.</summary>
        /// <param name="EntityName">The name of the entity to update.</param>
        /// <param name="UploadData">The data to upload for updating the entities.</param>
        /// <param name="progress">An object used to report progress during the update process.</param>
        /// <returns>An object containing information about any errors that occurred during the update.</returns>
        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            return GetDataSourceObject(EntityName).UpdateEntities(EntityName, UploadData, progress);
        }
        /// <summary>Updates an entity with the provided data.</summary>
        /// <param name="EntityName">The name of the entity to update.</param>
        /// <param name="UploadDataRow">The data to update the entity with.</param>
        /// <returns>An object containing information about any errors that occurred during the update.</returns>
        public virtual IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            return GetDataSourceObject(EntityName).UpdateEntity(EntityName, UploadDataRow);
        }
        /// <summary>Deletes an entity from the specified entity name and data row.</summary>
        /// <param name="EntityName">The name of the entity to delete.</param>
        /// <param name="DeletedDataRow">The data row representing the entity to delete.</param>
        /// <returns>An object containing information about any errors that occurred during the deletion process.</returns>
        public IErrorsInfo DeleteEntity(string EntityName, object DeletedDataRow)
        {
            return GetDataSourceObject(EntityName).DeleteEntity(EntityName, DeletedDataRow);
        }
        /// <summary>Gets the structure of an entity.</summary>
        /// <param name="fnd">The entity structure to retrieve.</param>
        /// <param name="refresh">Optional. Specifies whether to refresh the entity structure.</param>
        /// <returns>The structure of the specified entity.</returns>
        /// <remarks>
        /// If the entity structure is of type Table, it retrieves the entity structure from the data source object.
        /// If the entity structure is of type Query, Code, File, or Url, it returns the entity structure from the Entities collection.
        /// </remarks>
        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            switch (fnd.Viewtype)
            {
                case ViewType.Table:
                    return GetDataSourceObject(fnd.EntityName).GetEntityStructure(fnd, refresh);
                case ViewType.Query:
                case ViewType.Code:
                case ViewType.File:
                case ViewType.Url:
                default:
                    return Entities[EntityListIndex(fnd.EntityName)];
            }
        }
        /// <summary>Runs an ETL script.</summary>
        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            // Fixed: was reading ds.ConnectionStatus before getting ds (null ref), and ignoring the new ds.
            ds = DMEEditor.GetDataSource(DatasourceName);
            if (ds?.ConnectionStatus == ConnectionState.Open)
            {
                return ds.RunScript(dDLScripts);
            }
            DMEEditor.AddLogMessage("Error", $"Could not Find DataSource {DatasourceName}", DateTime.Now, 0, DatasourceName, Errors.Failed);
            return DMEEditor.ErrorObject;
        }
        /// <summary>Creates entities based on the provided list of entity structures.</summary>
        /// <param name="entities">A list of entity structures.</param>
        /// <returns>An object that contains information about any errors that occurred during the creation process.</returns>
        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            Entities.AddRange(entities);
            return DMEEditor.ErrorObject;
        }
        /// <summary>Generates a list of ETL script details for creating entities.</summary>
        /// <param name="entities">Optional. A list of entity structures. If provided, the script details will be generated for these entities only. If not provided, script details will be generated for all entities.</param>
        /// <returns>A list of ETL script details for creating entities.</returns>
        public IEnumerable<ETLScriptDet> GetCreateEntityScript(List<EntityStructure> entities = null)
        {
            List<ETLScriptDet> ls = new List<ETLScriptDet>();
            foreach (EntityStructure item in entities)
            {
                ds = DMEEditor.GetDataSource(item.DataSourceID);
                if (ds.ConnectionStatus == ConnectionState.Open)
                {
                    List<EntityStructure> lsent = new List<EntityStructure>();
                    lsent.Add(item);
                    ls.AddRange(ds.GetCreateEntityScript(lsent));
                }
                else
                {
                    DMEEditor.AddLogMessage("Error", "$Could not Find DataSource {item.DataSourceID}", DateTime.Now, 0, item.DataSourceID, Errors.Failed);
                }
            }
            return ls;
        }
        /// <summary>Inserts an entity into the database.</summary>
        /// <param name="EntityName">The name of the entity.</param>
        /// <param name="InsertedData">The data to be inserted.</param>
        /// <returns>An object containing information about any errors that occurred during the insertion process.</returns>
        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            IDataSource ds = GetDataSourceObject(EntityName);
            if (ds.ConnectionStatus == ConnectionState.Open)
            {

                return ds.InsertEntity(EntityName, InsertedData);
            }
            else
            {
                DMEEditor.AddLogMessage("Error", "$Could not Find DataSource {DatasourceName}", DateTime.Now, 0, DatasourceName, Errors.Failed);
                return null;
            }
        }
        /// <summary>Retrieves an entity asynchronously.</summary>
        public Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            // Fixed: previously used an invalid cast. Task.FromResult wraps the sync result properly.
            return Task.FromResult(GetEntity(EntityName, Filter));
        }
        #region "DataView Methods"

        #region "View Generating Methods"
        /// <summary>Removes an entity with the specified ID.</summary>
        public IErrorsInfo RemoveEntity(int EntityID)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                foreach (EntityStructure item in DataView.Entities.Where(x => x.ParentId == EntityID).ToList())
                {
                    RemoveEntity(item.Id);
                }
                // Fixed: cache the entity BEFORE removing to avoid use-after-remove IndexOutOfRange
                int idx = EntityListIndex(EntityID);
                if (idx >= 0)
                {
                    var entity = DataView.Entities[idx];
                    EntitiesNames.Remove(entity.EntityName);
                    DataView.Entities.Remove(entity);
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", ex.Message, DateTime.Now, -1, "", Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        /// <summary>Removes child entities associated with a parent entity.</summary>
        public IErrorsInfo RemoveChildEntities(int EntityID)
        {
            try
            {
                var ls = DataView.Entities.Where(x => x.ParentId == EntityID);
                foreach (EntityStructure item in ls.ToList())
                {
                    if (DataView.Entities.Where(y => y.ParentId == item.Id).Any())
                    {
                        RemoveChildEntities(item.Id);
                    }
                    // Fixed: cache entity before remove to avoid use-after-remove IndexOutOfRange
                    int idx = EntityListIndex(item.Id);
                    if (idx >= 0)
                    {
                        var entity = DataView.Entities[idx];
                        EntitiesNames.Remove(entity.EntityName);
                        DataView.Entities.Remove(entity);
                    }
                }
                DMEEditor.AddLogMessage("Success", "Removed Child entities", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage(ex.Message, "Could not Remove Child entities", DateTime.Now, -1, "", Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }

        /// <summary>Generates a view from a table using the specified parameters.</summary>
        public int GenerateViewFromTable(string viewname, IDataSource SourceConnection, string tablename, string SchemaName, string Filterparamters)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            int retval = 0;
            try
            {
                // M2 Fix: actually apply viewname to the DataView (was silently ignored before)
                DataView.ViewName = viewname;
                DataView.EntityDataSourceID = SourceConnection.DatasourceName;
                DataView.VID = Guid.NewGuid().ToString();
                retval = GenerateDataView(SourceConnection, tablename, SchemaName, Filterparamters);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error in GenerateViewFromTable ({ex.Message})", DateTime.Now, 0, FileName, Errors.Failed);
            }
            return retval;
        }
        /// <summary>Generates a DataView entity tree from a root table's FK child relations.</summary>
        public int GenerateDataView(IDataSource conn, string tablename, string SchemaName, string Filterparamters)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            List<ChildRelation> ds = (List<ChildRelation>)conn.GetChildTablesList(tablename, SchemaName, Filterparamters);

            EntityStructure maintab = new EntityStructure() { Id = NextHearId(), EntityName = tablename };
            maintab.DataSourceID = conn.DatasourceName;
            maintab.EntityName = tablename.ToUpper();
            maintab.ViewID = DataView.ViewID;
            maintab.DatasourceEntityName = tablename;
            maintab.ParentId = 0;

            DataView.Entities.Add(maintab);
            EntitiesNames.Add(maintab.EntityName);

            // M6 Fix: collapsed redundant double-null checks
            if (ds?.Count > 0)
            {
                foreach (ChildRelation r in ds)
                {
                    SetupEntityInView(DataView, DataView.Entities, r.child_table, tablename, r.child_column, r.parent_column, maintab.Id, conn.DatasourceName);
                    // M7 Fix: auto-populate JoinDefinitions from discovered FK relations
                    DataView.JoinDefinitions.Add(new FederatedJoinDefinition
                    {
                        LeftEntityName  = tablename.ToUpper(),
                        LeftColumn      = r.parent_column,
                        RightEntityName = r.child_table.ToUpper(),
                        RightColumn     = r.child_column,
                        JoinType        = FederatedJoinType.LeftOuter,
                        Description     = $"Auto-discovered FK: {tablename}.{r.parent_column} → {r.child_table}.{r.child_column}"
                    });
                }
            }

            return maintab.Id;
        }
        /// <summary>Creates a new empty IDMDataView for the given ViewName and connection name.</summary>
        public IDMDataView GenerateView(string ViewName, string ConnectionName)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            IDMDataView retval = null;
            try
            {
                retval = new DMDataView();
                retval.ViewID = 0;
                // M3 Fix: EntityDataSourceID must be the datasource connection name, not the view name
                retval.EntityDataSourceID = ConnectionName;
                retval.ViewName = ViewName;
                retval.DataViewDataSourceID = ConnectionName;
                retval.LocalEngineConnectionName = null; // auto-discover at query time
                retval.Viewtype = ViewType.Table;
                retval.VID = Guid.NewGuid().ToString();
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error in creating View ({ex.Message})", DateTime.Now, 0, ViewName, Errors.Failed);
            }
            return retval;
        }
        /// <summary>Generates entity nodes for child tables of a given parent within an existing DataView.</summary>
        public IErrorsInfo GenerateDataViewForChildNode(IDataSource conn, int pid, string tablename, string SchemaName, string Filterparamters)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                EntityStructure pd = DataView.Entities.FirstOrDefault(c => c.Id == pid);
                if (pd?.Viewtype == ViewType.Table)
                {
                    List<ChildRelation> ds = (List<ChildRelation>)conn.GetChildTablesList(tablename, conn.Dataconnection.ConnectionProp.SchemaName, Filterparamters);
                    // M6 Fix: collapsed from double-null check
                    if (ds?.Count > 0)
                    {
                        foreach (ChildRelation r in ds)
                        {
                            SetupEntityInView(DataView, DataView.Entities, r.child_table, tablename, r.child_column, r.parent_column, pd.Id, conn.DatasourceName);
                            // M7 Fix: auto-populate JoinDefinitions
                            DataView.JoinDefinitions.Add(new FederatedJoinDefinition
                            {
                                LeftEntityName  = tablename.ToUpper(),
                                LeftColumn      = r.parent_column,
                                RightEntityName = r.child_table.ToUpper(),
                                RightColumn     = r.child_column,
                                JoinType        = FederatedJoinType.LeftOuter,
                                Description     = $"Auto-discovered FK: {tablename}.{r.parent_column} → {r.child_table}.{r.child_column}"
                            });
                        }
                    }
                    DMEEditor.AddLogMessage("Success", $"Getting Child from DataSource", DateTime.Now, 0, null, Errors.Ok);
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error Getting Child from DataSource:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        /// <summary>Adds a table as a child entity under a specified parent within the DataView.</summary>
        public int AddEntityAsChild(IDataSource conn, string tablename, string SchemaName, string Filterparamters, int viewindex, int ParentTableIndex)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;

            List<ChildRelation> ds = (List<ChildRelation>)conn.GetChildTablesList(tablename, SchemaName, Filterparamters);

            EntityStructure maintab = new EntityStructure() { Id = NextHearId(), EntityName = tablename };
            EntityStructure Parenttab = Entities[EntityListIndex(ParentTableIndex)];
            maintab.DataSourceID = conn.DatasourceName;
            maintab.EntityName = tablename.ToUpper();
            maintab.ViewID = DataView.ViewID;
            maintab.ParentId = ParentTableIndex;
            maintab.DatasourceEntityName = tablename;

            // M4 Fix: restore the collision guard that was commented out
            if (CheckEntityExist(maintab.DatasourceEntityName))
            {
                var existing = DataView.Entities
                    .Where(p => p.DatasourceEntityName.Equals(maintab.DatasourceEntityName, StringComparison.OrdinalIgnoreCase) && p.ParentId == 0)
                    .ToList();
                int diffDB = existing.Count(p => !p.DataSourceID.Equals(maintab.DataSourceID, StringComparison.OrdinalIgnoreCase));
                int sameDB = existing.Count(p =>  p.DataSourceID.Equals(maintab.DataSourceID, StringComparison.OrdinalIgnoreCase));
                int suffix = diffDB > 0 ? diffDB : sameDB;
                if (suffix > 0)
                    maintab.Caption = $"{maintab.DatasourceEntityName}_{maintab.DataSourceID}_{suffix}";
                else
                    maintab.Caption = $"{maintab.DatasourceEntityName}_{Parenttab.EntityName}s";
            }
            else
            {
                maintab.Caption = $"{maintab.DatasourceEntityName}_{Parenttab.EntityName}s";
            }

            DataView.Entities.Add(maintab);
            EntitiesNames.Add(maintab.EntityName);

            // M6 Fix: collapsed double-null check
            if (ds?.Count > 0)
            {
                foreach (ChildRelation r in ds)
                {
                    SetupEntityInView(DataView, DataView.Entities, r.child_table, maintab.DatasourceEntityName, r.child_column, r.parent_column, maintab.Id, conn.DatasourceName);
                    // M7 Fix: auto-populate JoinDefinitions
                    DataView.JoinDefinitions.Add(new FederatedJoinDefinition
                    {
                        LeftEntityName  = maintab.EntityName,
                        LeftColumn      = r.parent_column,
                        RightEntityName = r.child_table.ToUpper(),
                        RightColumn     = r.child_column,
                        JoinType        = FederatedJoinType.LeftOuter,
                        Description     = $"Auto-discovered FK: {tablename}.{r.parent_column} → {r.child_table}.{r.child_column}"
                    });
                }
            }

            return maintab.Id;
        }
        /// <summary>Adds a single table (and its FK child relations) from a data source into the DataView.</summary>
        public int AddEntitytoDataView(IDataSource conn, string tablename, string SchemaName, string Filterparamters)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            EntityStructure maintab;
            try
            {
                List<ChildRelation> ds = (List<ChildRelation>)conn.GetChildTablesList(tablename, SchemaName, Filterparamters);
                maintab = new EntityStructure() { Id = NextHearId(), EntityName = tablename };
                maintab.DataSourceID = conn.DatasourceName;
                maintab.EntityName = tablename.ToUpper();
                maintab.ViewID = 0;
                maintab.ParentId = 0;
                maintab.DatabaseType = conn.DatasourceType;
                maintab.DatasourceEntityName = tablename;

                if (CheckEntityExist(maintab.DatasourceEntityName))
                {
                    int cnt = EntitiesNames.Count(p => p.Equals(maintab.DatasourceEntityName, StringComparison.InvariantCultureIgnoreCase));
                    maintab.EntityName = maintab.DatasourceEntityName + "_" + (cnt + 1);
                }

                // M5 Fix: replace brittle 30-case switch with the shared helper
                maintab.Viewtype = ResolveViewType(maintab.DatabaseType);

                DataView.Entities.Add(maintab);
                EntitiesNames.Add(maintab.EntityName);

                // M6 Fix: collapsed double-null check
                if (ds?.Count > 0)
                {
                    foreach (ChildRelation r in ds)
                    {
                        SetupEntityInView(DataView, DataView.Entities, r.child_table, tablename, r.child_column, r.parent_column, maintab.Id, conn.DatasourceName);
                        // M7 Fix: auto-populate JoinDefinitions from FK relations
                        DataView.JoinDefinitions.Add(new FederatedJoinDefinition
                        {
                            LeftEntityName  = maintab.EntityName,
                            LeftColumn      = r.parent_column,
                            RightEntityName = r.child_table.ToUpper(),
                            RightColumn     = r.child_column,
                            JoinType        = FederatedJoinType.LeftOuter,
                            Description     = $"Auto-discovered FK: {tablename}.{r.parent_column} → {r.child_table}.{r.child_column}"
                        });
                    }
                }

                return maintab.Id;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Could not add entity to view: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                return -1;
            }
        }
        /// <summary>
        /// Adds a pre-built EntityStructure into the DataView, resolving naming conflicts and assigning ViewType.
        /// Also auto-populates JoinDefinitions from any discovered FK child relations (for RDBMS sources).
        /// </summary>
        public int AddEntitytoDataView(EntityStructure maintab)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                maintab.Id = NextHearId();
                maintab.ViewID = 0;
                if (maintab.ParentId == -1) maintab.ParentId = 0;

                // Collision guard: differentiate same-table from different sources
                if (CheckEntityExist(maintab.DatasourceEntityName))
                {
                    if (maintab.ParentId == 0)
                    {
                        var ls = DataView.Entities
                            .Where(p => p.DatasourceEntityName.Equals(maintab.DatasourceEntityName, StringComparison.InvariantCultureIgnoreCase) && p.ParentId == 0)
                            .ToList();
                        int diffDB = ls.Count(p => !p.DataSourceID.Equals(maintab.DataSourceID, StringComparison.InvariantCultureIgnoreCase));
                        int sameDB = ls.Count(p =>  p.DataSourceID.Equals(maintab.DataSourceID, StringComparison.InvariantCultureIgnoreCase));
                        if (diffDB > 0)
                            maintab.Caption = $"{maintab.DatasourceEntityName}_{maintab.DataSourceID}_{diffDB}";
                        if (sameDB > 0)
                            maintab.Caption = $"{maintab.DatasourceEntityName}_{maintab.DataSourceID}_{sameDB}";
                    }
                    else
                    {
                        IEntityStructure parententity = Entities[EntityListIndex(maintab.ParentId)];
                        if (parententity != null)
                            maintab.Caption = $"{parententity.DatasourceEntityName}_{maintab.EntityName}s";
                    }
                }

                maintab.OriginalEntityName = maintab.DatasourceEntityName;
                // M5 Fix: use shared helper instead of duplicating the ViewType switch
                if (maintab.Viewtype == ViewType.Table)
                    maintab.Viewtype = ResolveViewType(maintab.DatabaseType);

                DataView.Entities.Add(maintab);
                EntitiesNames.Add(maintab.EntityName);

                IDataSource entityds = DMEEditor.GetDataSource(maintab.DataSourceID);
                if (entityds != null && entityds.Category == DatasourceCategory.RDBMS)
                {
                    List<ChildRelation> ds = (List<ChildRelation>)entityds.GetChildTablesList(maintab.DatasourceEntityName, entityds.Dataconnection.ConnectionProp.SchemaName, null);
                    if (DMEEditor.ErrorObject.Flag == Errors.Ok && ds?.Count > 0)
                    {
                        foreach (ChildRelation r in ds)
                        {
                            SetupEntityInView(DataView, DataView.Entities, r.child_table, maintab.DatasourceEntityName, r.child_column, r.parent_column, maintab.Id, entityds.DatasourceName);
                            // M7 Fix: auto-populate JoinDefinitions
                            DataView.JoinDefinitions.Add(new FederatedJoinDefinition
                            {
                                LeftEntityName  = maintab.EntityName,
                                LeftColumn      = r.parent_column,
                                RightEntityName = r.child_table.ToUpper(),
                                RightColumn     = r.child_column,
                                JoinType        = FederatedJoinType.LeftOuter,
                                Description     = $"Auto-discovered FK: {maintab.DatasourceEntityName}.{r.parent_column} → {r.child_table}.{r.child_column}"
                            });
                        }
                    }
                }

                return maintab.Id;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Could not add entity to view: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                return -1;
            }
        }
        #endregion  "View Generating Methods"
        #region "Misc and Util Methods"

        /// <summary>
        /// M5: Shared helper — maps a DataSourceType to the correct ViewType.
        /// Replaces duplicated 30-case switches in AddEntitytoDataView methods.
        /// </summary>
        private static ViewType ResolveViewType(DataSourceType dbType) => dbType switch
        {
            DataSourceType.SqlServer or DataSourceType.Oracle   or DataSourceType.Mysql  or
            DataSourceType.Postgre   or DataSourceType.SqlLite  or DataSourceType.DB2    or
            DataSourceType.FireBird  or DataSourceType.VistaDB  or DataSourceType.SqlCompact or
            DataSourceType.Firebase  or DataSourceType.Couchbase or DataSourceType.RavenDB  or
            DataSourceType.MongoDB   or DataSourceType.CouchDB  or DataSourceType.DuckDB => ViewType.Table,

            DataSourceType.Text or DataSourceType.CSV or DataSourceType.Xls or
            DataSourceType.Json or DataSourceType.XML or DataSourceType.Parquet     => ViewType.File,

            DataSourceType.OPC or DataSourceType.WebApi or DataSourceType.GraphQL   => ViewType.Url,

            _ => ViewType.Table
        };

        /// <summary>Returns the icon associated with a specific view type.</summary>
        /// <param name="v">The view type.</param>
        /// <returns>The icon associated with the view type.</returns>
        public string GeticonForViewType(ViewType v)
        {
            string iconname = "entity.ico";
            switch (v)
            {
                case ViewType.Table:
                    iconname = "entity.ico";
                    break;
                case ViewType.Query:
                    iconname = "sqlicon.ico";
                    break;
                case ViewType.Code:
                    iconname = "codeicon.ico";
                    break;
                case ViewType.File:
                    iconname = "fileicon.ico";
                    break;
                case ViewType.Url:
                    iconname = "webapi.ico";
                    break;
                default:
                    break;
            }
            return iconname;
        }
        /// <summary>
        /// Sets up the entity structure in a data view.
        /// </summary>
        /// <param name="v">The data view.</param>
        /// <param name="Rootnamespacelist">The list of root namespaces.</param>
        /// <param name="childtable">The name of the child table.</param>
        /// <param name="parenttable">The name of the parent table.</param>
        /// <param name="childcolumn">The name of the child column.</param>
        /// <param name="parentcolumn">The name of the parent column.</param>
        /// <param name="pid">The parent ID.</param>
        /// <param name="Datasourcename">The name of the data source.</param>
        private EntityStructure SetupEntityInView(IDMDataView v, List<EntityStructure> Rootnamespacelist, string childtable, string parenttable, string childcolumn, string parentcolumn, int pid, string Datasourcename)
        {

            EntityStructure a = null;
            int pkid = NextHearId();
            IDataSource ds = DMEEditor.GetDataSource(Datasourcename);
            string schemaname = "";
            if (ds.Category == DatasourceCategory.RDBMS)
            {
                IRDBSource rdb = (IRDBSource)ds;
                schemaname = rdb.GetSchemaName();
            }
            if (childtable != null)
            {
                if (!Rootnamespacelist.Where(f => f.ParentId == pid && f.EntityName.Equals(childtable, StringComparison.InvariantCultureIgnoreCase)).Any())//f => f.Id == childtable &&
                {
                    //a = new EntityStructure() { Id = pkid, ParentId = pid, EntityName = childtable.ToUpper(), ViewID = v.ViewID };
                    //a.DataSourceID = v.Entities.Where(x => x.Id == pid).FirstOrDefault().DataSourceID;
                    //a.DatasourceEntityName = childtable;
                    //a.Relations = ds.GetEntityforeignkeys(childtable.ToUpper(), schemaname);

                    a = (EntityStructure)ds.GetEntityStructure(childtable, true).Clone();
                    a.ParentId = pid;
                    a.Caption = $"{parenttable}_{childtable}s";
                    a.Id = NextHearId();
                    Rootnamespacelist.Add(a);


                }
                else
                {
                    a = Rootnamespacelist.Where(f => f.ParentId == pid && f.EntityName.Equals(childtable, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault(); //f.Id == childtable &&
                                                                                                                                                                          //  a.DataSourceID = DatasourceName;
                    a.DatasourceEntityName = childtable;
                    a.Relations.Add(new RelationShipKeys { EntityColumnID = childcolumn.ToUpper(), RelatedEntityColumnID = parentcolumn.ToUpper(), RelatedEntityID = parenttable.ToUpper() });

                }
            }

            return a;
        }
        /// <summary>Retrieves the structure of an entity.</summary>
        /// <param name="entityname">The name of the entity.</param>
        /// <returns>The structure of the specified entity.</returns>
        public EntityStructure GetEntity(string entityname)
        {
            EntityStructure retval = null;


            try
            {
                retval = DataView.Entities[EntityListIndex(entityname)];
            }
            catch (Exception)
            {

                retval = null;
            }


            return retval;
        }
        /// <summary>Generates the next unique hear ID.</summary>
        /// <returns>An integer representing the next hear ID.</returns>
        public int NextHearId()
        {

            if (DataView.Entities != null)
            {
                if (DataView.Entities.Count > 0)
                {
                    if (DataView.Entities.Max(p => p.Id) > EntityIndex)
                    {
                        EntityIndex = DataView.Entities.Max(p => p.Id);
                    }
                }
                else
                {
                    EntityIndex = 0;
                }

            }

            return EntityIndex += 1;
        }
        #endregion "Misc and Util Methods"
        #region "Dataset and entity Structure Methods"
        /// <summary>Retrieves a list of data sets for a given view.</summary>
        /// <param name="viewname">The name of the view.</param>
        /// <returns>A list of data sets associated with the specified view.</returns>
        public List<DataSet> GetDataSetForView(string viewname)
        {
            List<DataSet> retval = new List<DataSet>();
            EntityStructure entity = new EntityStructure();
            try
            {

                IDataSource ds = DMEEditor.GetDataSource(DataView.DataViewDataSourceID);
                EntityStructure maine = DataView.Entities[0];
                int mainid = maine.Id;


                ///--------------- Create Main Tables
                foreach (EntityStructure item in DataView.Entities.Where(x => x.ParentId == mainid && mainid != x.Id))
                {
                    DataSet mainds = new DataSet(viewname + "." + item.EntityName);
                    retval.Add(mainds);
                    //  entity = item;
                    if (item.Fields.Count == 0 || item.Relations.Count == 0)
                    {

                        EntityStructure entity1 = ds.GetEntityStructure(item.EntityName, false);

                        item.Relations = entity1.Relations;
                        item.Fields = entity1.Fields;

                    }

                    // Add main Table
                    mainds.Tables.Add(CreateTableFromEntityStructure(item));
                    // Adding Child Tables
                    GetChildTablesForDataset(ds, ref mainds, item, DataView.Entities.Where(f => f.Id != item.Id).ToList(), item.Id);
                }




            }
            catch (Exception ex)
            {
                string mes = ex.Message;
                DMEEditor.AddLogMessage(ex.Message, "Could not Convert View to Object" + mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            DMEEditor.ConfigEditor.SaveDataconnectionsValues();
            return retval;
        }
        /// <summary>Creates a DataTable from an EntityStructure.</summary>
        /// <param name="e">The EntityStructure object.</param>
        /// <returns>A DataTable representing the structure of the entity.</returns>
        private DataTable CreateTableFromEntityStructure(EntityStructure e)
        {
            DataTable dt = new DataTable(e.EntityName);
            try
            {
                foreach (EntityField item in e.Fields)
                {
                    Type fieldType = Type.GetType(item.Fieldtype, false, true);
                    if (fieldType == null)
                    {
                        switch (item.Fieldtype.ToLowerInvariant())
                        {
                            case "string": fieldType = typeof(string); break;
                            case "int":
                            case "int32": fieldType = typeof(int); break;
                            case "long":
                            case "int64": fieldType = typeof(long); break;
                            case "decimal": fieldType = typeof(decimal); break;
                            case "double": fieldType = typeof(double); break;
                            case "bool":
                            case "boolean": fieldType = typeof(bool); break;
                            case "datetime": fieldType = typeof(DateTime); break;
                            default: fieldType = typeof(string); break;
                        }
                    }
                    DataColumn co = new DataColumn(item.FieldName, fieldType);
                    co.AllowDBNull = item.AllowDBNull;
                    co.AutoIncrement = item.IsAutoIncrement;
                    co.Unique = item.IsUnique;
                    dt.Columns.Add(co);
                }
            }
            catch (Exception ex)
            {
                string mes = ex.Message;
                DMEEditor.AddLogMessage(ex.Message, "Could not Table object from Entity structure " + mes, DateTime.Now, -1, mes, Errors.Failed);
                dt = null;
            };
            return dt;
        }
        /// <summary>
        /// Retrieves the child tables for a given dataset and parent table.
        /// </summary>
        /// <param name="ds">The data source.</param>
        /// <param name="dataset">The dataset to retrieve child tables for.</param>
        /// <param name="parenttb">The parent table.</param>
        /// <param name="ls">The list of child tables.</param>
        /// <param name="parentid">The ID of the parent table.</param>
        /// <returns>An instance of IErrorsInfo representing the child tables.</returns>
        private IErrorsInfo GetChildTablesForDataset(IDataSource ds, ref DataSet dataset, EntityStructure parenttb, List<EntityStructure> ls, int parentid)
        {

            foreach (EntityStructure tab in ls.Where(x => x.ParentId == parentid && x.Id != parentid && x.EntityName != parenttb.EntityName))
            {
                try
                {
                    DataTable tb = new DataTable();
                    EntityStructure entity = new EntityStructure();
                    entity = tab;
                    if (dataset.Tables.Contains(tab.EntityName) == false)
                    {

                        if (tab.Fields.Count == 0 || tab.Relations.Count == 0)
                        {

                            EntityStructure entity1 = ds.GetEntityStructure(tab.EntityName, false);
                            tab.Fields = entity1.Fields;
                            tab.Relations = entity1.Relations;

                        }




                        tb = CreateTableFromEntityStructure(tab);
                        dataset.Tables.Add(tb);
                    }
                    else
                    {
                        tb = dataset.Tables[tab.EntityName];
                    }




                    //---------------- Adding Relations 
                    List<RelationShipKeys> rl = entity.Relations.Where(c => c.RelatedEntityID == parenttb.EntityName).ToList();

                    DataTable Parenttb = dataset.Tables[parenttb.EntityName];
                    foreach (string relationname in rl.Select(x => x.RalationName).Distinct())
                    {
                        int k = 0;
                        int cnt = rl.Where(u => u.RalationName == relationname).Count();
                        DataColumn[] ParentColumn = new DataColumn[cnt];
                        DataColumn[] ChildColumn = new DataColumn[cnt];
                        foreach (RelationShipKeys keys in rl.Where(u => u.RalationName == relationname))
                        {
                            ParentColumn[k] = Parenttb.Columns[keys.RelatedEntityColumnID];
                            ChildColumn[k] = tb.Columns[keys.EntityColumnID];


                            k += 1;
                        }
                        tb.ParentRelations.Add(Parenttb.TableName + "." + tb.TableName, ParentColumn, ChildColumn);
                    }

                    if (ls.Where(x => x.ParentId == entity.Id).Count() > 0)
                    {
                        GetChildTablesForDataset(ds, ref dataset, entity, ls.Where(f => f.Id != entity.Id).ToList(), entity.Id);
                    }
                }
                catch (Exception ex)
                {
                    string mes = ex.Message;
                    DMEEditor.AddLogMessage(ex.Message, "Could not add child tables in Dataset " + mes, DateTime.Now, -1, mes, Errors.Failed);
                };

            }
            return DMEEditor.ErrorObject;
        }
        #endregion "Dataset and entity Structure Methods"
        #region Read/write Views to file
        /// <summary>Writes the content of a DataView to a file.</summary>
        /// <param name="filename">The name of the file to write to.</param>
        public void WriteDataViewFile(string filename)
        {
            string path;
            if (!string.IsNullOrEmpty(Dataconnection.ConnectionProp.FilePath) || !string.IsNullOrWhiteSpace(Dataconnection.ConnectionProp.FilePath))
            {
                path = Path.Combine(Dataconnection.ConnectionProp.FilePath, $"{filename}");
            }
            else
            {
                path = Path.Combine(DMEEditor.ConfigEditor.Config.Folders.Where(c => c.FolderFilesType == FolderFileTypes.DataView).FirstOrDefault().FolderPath, $"{filename}");
            }

            DMEEditor.ConfigEditor.JsonLoader.Serialize(path, DataView);

        }

        /// <summary>Writes a DataView file to the specified path and filename.</summary>
        /// <param name="path">The path where the file will be written.</param>
        /// <param name="filename">The name of the file.</param>
        public void WriteDataViewFile(string path, string filename)
        {
            string name = Path.Combine(path, $"{filename}");
            DMEEditor.ConfigEditor.JsonLoader.Serialize(name, DataView);

        }
        /// <summary>Reads a data view file from the specified path and filename.</summary>
        /// <param name="pathandfilename">The path and filename of the data view file.</param>
        /// <returns>The IDMDataView object representing the data view file.</returns>
        public IDMDataView ReadDataViewFile(string pathandfilename)
        {
            // String JSONtxt = File.ReadAllText(pathandfilename);
            return DMEEditor.ConfigEditor.JsonLoader.DeserializeSingleObject<DMDataView>(pathandfilename);


        }

        /// <summary>Loads and applies the persisted DataView definition from its backing file.</summary>
        public IErrorsInfo LoadView()
        {
            try
            {
                // M8 Fix: logical/in-memory federated views have no backing file — just initialise an empty DMDataView
                if (Dataconnection is DataViewConnection dvc && dvc.IsLogical)
                {
                    if (DataView == null)
                    {
                        DataView = new DMDataView
                        {
                            ViewName = DatasourceName,
                            VID      = Guid.NewGuid().ToString(),
                            ExecutionMode  = FederationExecutionMode.Cached,
                            CacheTTLSeconds = 300
                        };
                    }
                }
                else if (Dataconnection.ConnectionStatus == ConnectionState.Open)
                {
                    DataView = (DMDataView)ReadDataViewFile(DataViewFile);
                    if (DataView.VID == null)
                        DataView.VID = Guid.NewGuid().ToString();
                }

                DMEEditor.AddLogMessage("Success", "Loaded File", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage(ex.Message, "Could not Load File", DateTime.Now, -1, "Could not Load File", Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }

        /// <summary>Returns the index of the specified entity.</summary>
        /// <param name="entityName">The name of the entity.</param>
        /// <returns>The index of the entity.</returns>
        public int GetEntityIdx(string entityName)
        {
            try
            {
                return EntityListIndex(entityName);
            }
            catch
            {
                return -1;
            }
        }

        #endregion

        #region "Relation Builder"
        /// <summary>
        /// Manually defines a cross-source join between two entities in the DataView.
        /// Automatically invalidates the federation cache so the next query uses the new join.
        /// </summary>
        public string AddJoin(
            string leftEntityName,  string leftColumn,  string leftDataSourceID,
            string rightEntityName, string rightColumn, string rightDataSourceID,
            FederatedJoinType joinType        = FederatedJoinType.Inner,
            string description                = null,
            string additionalCondition        = null)
        {
            var join = new FederatedJoinDefinition
            {
                GuidID              = Guid.NewGuid().ToString(),
                LeftEntityName      = leftEntityName?.ToUpperInvariant(),
                LeftColumn          = leftColumn,
                LeftDataSourceID    = leftDataSourceID,
                RightEntityName     = rightEntityName?.ToUpperInvariant(),
                RightColumn         = rightColumn,
                RightDataSourceID   = rightDataSourceID,
                JoinType            = joinType,
                IsManuallyDefined   = true,
                AdditionalCondition = additionalCondition,
                Description         = description ?? $"{leftEntityName}.{leftColumn} → {rightEntityName}.{rightColumn}"
            };
            DataView.JoinDefinitions.Add(join);
            InvalidateCache(); // new join invalidates any cached temp DB
            DMEEditor.AddLogMessage("Info", $"Relation Builder: join added — {join.Description}", DateTime.Now, 0, "", Errors.Ok);
            return join.GuidID;
        }

        /// <summary>Removes a join definition by its GuidID. Returns true if found and removed.</summary>
        public bool RemoveJoin(string joinGuidID)
        {
            var join = DataView.JoinDefinitions.FirstOrDefault(j => j.GuidID == joinGuidID);
            if (join == null) return false;
            DataView.JoinDefinitions.Remove(join);
            InvalidateCache();
            DMEEditor.AddLogMessage("Info", $"Relation Builder: join removed — {join.Description}", DateTime.Now, 0, "", Errors.Ok);
            return true;
        }

        /// <summary>Updates an existing join definition in-place. Returns false if not found.</summary>
        public bool UpdateJoin(
            string joinGuidID,
            string leftEntityName,  string leftColumn,
            string rightEntityName, string rightColumn,
            FederatedJoinType joinType,
            string description        = null,
            string additionalCondition = null)
        {
            var join = DataView.JoinDefinitions.FirstOrDefault(j => j.GuidID == joinGuidID);
            if (join == null) return false;

            join.LeftEntityName      = leftEntityName?.ToUpperInvariant();
            join.LeftColumn          = leftColumn;
            join.RightEntityName     = rightEntityName?.ToUpperInvariant();
            join.RightColumn         = rightColumn;
            join.JoinType            = joinType;
            join.AdditionalCondition = additionalCondition;
            join.Description         = description ?? $"{leftEntityName}.{leftColumn} → {rightEntityName}.{rightColumn}";
            InvalidateCache();
            return true;
        }

        /// <summary>Retrieves a single join definition by GuidID. Returns null if not found.</summary>
        public FederatedJoinDefinition GetJoin(string joinGuidID)
            => DataView.JoinDefinitions.FirstOrDefault(j => j.GuidID == joinGuidID);

        /// <summary>Returns all join definitions that involve the named entity (left or right side).</summary>
        public List<FederatedJoinDefinition> GetJoinsFor(string entityName)
        {
            if (string.IsNullOrWhiteSpace(entityName)) return new List<FederatedJoinDefinition>();
            string upper = entityName.ToUpperInvariant();
            return DataView.JoinDefinitions
                .Where(j => j.LeftEntityName.Equals(upper,  StringComparison.OrdinalIgnoreCase)
                         || j.RightEntityName.Equals(upper, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        /// <summary>
        /// Validates all join definitions. Checks entity names exist in DataView.Entities
        /// and column names exist in entity Fields.
        /// Returns a list of validation error messages (empty = all valid).
        /// </summary>
        public List<string> ValidateJoins()
        {
            var errors = new List<string>();
            foreach (var join in DataView.JoinDefinitions)
            {
                var leftEnt  = DataView.Entities.FirstOrDefault(e => e.EntityName.Equals(join.LeftEntityName,  StringComparison.OrdinalIgnoreCase));
                var rightEnt = DataView.Entities.FirstOrDefault(e => e.EntityName.Equals(join.RightEntityName, StringComparison.OrdinalIgnoreCase));

                if (leftEnt  == null)
                    errors.Add($"[{join.GuidID}] Left entity '{join.LeftEntityName}' not found in DataView.");
                if (rightEnt == null)
                    errors.Add($"[{join.GuidID}] Right entity '{join.RightEntityName}' not found in DataView.");

                if (leftEnt?.Fields?.Count > 0 &&
                    leftEnt.Fields.All(f => !f.FieldName.Equals(join.LeftColumn, StringComparison.OrdinalIgnoreCase)))
                    errors.Add($"[{join.GuidID}] Column '{join.LeftColumn}' not found on entity '{join.LeftEntityName}'.");

                if (rightEnt?.Fields?.Count > 0 &&
                    rightEnt.Fields.All(f => !f.FieldName.Equals(join.RightColumn, StringComparison.OrdinalIgnoreCase)))
                    errors.Add($"[{join.GuidID}] Column '{join.RightColumn}' not found on entity '{join.RightEntityName}'.");
            }
            return errors;
        }

        /// <summary>
        /// Returns all fields on the named entity that are typed as typical FK/PK candidates
        /// (integer, string, guid) — suitable for a column-picker UI in the Relation Builder.
        /// </summary>
        public List<EntityField> GetJoinableColumns(string entityName)
        {
            var ent = DataView.Entities.FirstOrDefault(e => e.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase));
            if (ent?.Fields == null) return new List<EntityField>();

            // Types that are sensible FK/PK join candidates
            var joinableTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "System.Int32", "System.Int64", "System.Int16",
                "Int32", "Int64", "Int16", "int", "long", "short",
                "System.String", "String", "string",
                "System.Guid",  "Guid",
                "System.UInt32","UInt32"
            };

            return ent.Fields
                .Where(f => f.IsKey || joinableTypes.Contains(f.Fieldtype ?? ""))
                .ToList();
        }

        /// <summary>
        /// Builds the SQL JOIN clause for a FederatedJoinDefinition.
        /// Used when constructing federated queries against the temp DB.
        /// </summary>
        public string BuildJoinSQL(FederatedJoinDefinition join)
        {
            if (join == null) return string.Empty;

            string keyword = join.JoinType switch
            {
                FederatedJoinType.Inner      => "INNER JOIN",
                FederatedJoinType.LeftOuter  => "LEFT JOIN",
                FederatedJoinType.RightOuter => "RIGHT JOIN",
                FederatedJoinType.FullOuter  => "FULL OUTER JOIN",
                FederatedJoinType.Cross      => "CROSS JOIN",
                _                            => "JOIN"
            };

            string sql = $"{keyword} {join.RightEntityName} ON {join.LeftEntityName}.{join.LeftColumn} = {join.RightEntityName}.{join.RightColumn}";
            if (!string.IsNullOrWhiteSpace(join.AdditionalCondition))
                sql += $" AND ({join.AdditionalCondition})";

            return sql;
        }

        /// <summary>Removes all manually-defined joins. Leaves auto-discovered FK joins intact.</summary>
        public void ClearManualJoins()
        {
            DataView.JoinDefinitions.RemoveAll(j => j.IsManuallyDefined);
            InvalidateCache();
        }

        /// <summary>Removes all join definitions — both manual and auto-discovered.</summary>
        public void ClearAllJoins()
        {
            DataView.JoinDefinitions.Clear();
            InvalidateCache();
        }
        #endregion "Relation Builder"

        #endregion
        #region "dispose"
        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Close the federated temp DB connection on disposal
                    if (_tempDb != null)
                    {
                        try { _tempDb.Closeconnection(); } catch { }
                        _tempDb = null;
                    }
                }
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~RDBSource()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
