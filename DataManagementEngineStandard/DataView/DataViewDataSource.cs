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
    public class DataViewDataSource :  IDataSource, IDMDataView
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
                DMEEditor = DMEEditor
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
        /// <summary>Gets the scalar value from a given query.</summary>
        /// <param name="query">The query to retrieve the scalar value.</param>
        /// <returns>The scalar value obtained from the query.</returns>
        public virtual double GetScalar(string query)
        {
            ErrorObject.Flag = Errors.Ok;

            try
            {
                // Assuming you have a database connection and command objects.

                //using (var command = GetDataCommand())
                //{
                //    command.CommandText = query;
                //    var result = command.ExecuteScalar();

                //    // Check if the result is not null and can be converted to a double.
                //    if (result != null && double.TryParse(result.ToString(), out double value))
                //    {
                //        return value;
                //    }
                //}


                // If the query executed successfully but didn't return a valid double, you can handle it here.
                // You might want to log an error or throw an exception as needed.
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error in executing scalar query ({ex.Message})", DateTime.Now, 0, "", Errors.Failed);
            }

            // Return a default value or throw an exception if the query failed.
            return 0.0; // You can change this default value as needed.
        }
        /// <summary>Retrieves an entity based on the specified entity name and filter.</summary>
        /// <param name="EntityName">The name of the entity to retrieve.</param>
        /// <param name="filter">A list of filters to apply to the entity.</param>
        /// <returns>The retrieved entity.</returns>
        public IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
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
                        switch (ent.Viewtype)
                        {
                            case ViewType.File:
                            case ViewType.Url:
                            case ViewType.Table:
                            case ViewType.Query:
                                retval = GetDataSourceObject(EntityName).GetEntity(EntityName, filter);
                                break;
                            case ViewType.Code:
                                retval = null;
                                break;
                            default:
                                retval = null;
                                break;
                        }
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
        /// <summary>Executes the given SQL statement.</summary>
        /// <param name="sql">The SQL statement to execute.</param>
        /// <returns>An object containing information about any errors that occurred during execution.</returns>
        public IErrorsInfo ExecuteSql(string sql)
        {
            DMEEditor.AddLogMessage("Beep", $"DataView DataSource {DatasourceName}  Method  {System.Reflection.MethodBase.GetCurrentMethod().Name} Not Implemented", DateTime.Now, 0, null, Errors.Ok);
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
            DMEEditor.AddLogMessage("Beep", $"DataView DataSource {DatasourceName}  Method  {System.Reflection.MethodBase.GetCurrentMethod().Name} Not Implemented", DateTime.Now, 0, null, Errors.Ok);
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
        /// <param name="dDLScripts">The ETL script to run.</param>
        /// <returns>An object containing information about any errors that occurred during script execution.</returns>
        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            if (ds.ConnectionStatus == ConnectionState.Open)
            {
                ds = DMEEditor.GetDataSource(DatasourceName);
                return ds.RunScript(dDLScripts);
            }
            else
            {
                DMEEditor.AddLogMessage("Error", "$Could not Find DataSource {DatasourceName}", DateTime.Now, 0, DatasourceName, Errors.Failed);
                return null;
            }

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
        /// <param name="EntityName">The name of the entity to retrieve.</param>
        /// <param name="Filter">A list of filters to apply to the entity.</param>
        /// <returns>A task representing the asynchronous operation. The result is the retrieved entity.</returns>
        public Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            return (Task<IEnumerable<object>>)GetEntity(EntityName, Filter);
        }
        #region "DataView Methods"

        #region "View Generating Methods"
        /// <summary>Removes an entity with the specified ID.</summary>
        /// <param name="EntityID">The ID of the entity to remove.</param>
        /// <returns>An object containing information about any errors that occurred during the removal process.</returns>
        public IErrorsInfo RemoveEntity(int EntityID)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {

                foreach (EntityStructure item in DataView.Entities.Where(x => x.ParentId == EntityID).ToList())
                {
                    RemoveEntity(item.Id);
                }
                DataView.Entities.Remove(DataView.Entities[EntityListIndex(EntityID)]);
                EntitiesNames.Remove(DataView.Entities[EntityListIndex(EntityID)].EntityName);
            }
            catch (Exception ex)
            {


                DMEEditor.AddLogMessage("Fail", ex.Message, DateTime.Now, -1, "", Errors.Failed);

            }


            return DMEEditor.ErrorObject;
        }
        /// <summary>Removes child entities associated with a parent entity.</summary>
        /// <param name="EntityID">The ID of the parent entity.</param>
        /// <returns>An object containing information about any errors that occurred during the removal process.</returns>
        public IErrorsInfo RemoveChildEntities(int EntityID)
        {

            try
            {

                // CurrentEntity = CurrentView.Entity[EntityListIndex(ViewID, EntityID)];
                var ls = DataView.Entities.Where(x => x.ParentId == EntityID);
                foreach (EntityStructure item in ls.ToList())
                {
                    if (DataView.Entities.Where(y => y.ParentId == item.Id).Any())
                    {
                        RemoveChildEntities(item.Id);
                    }
                    DataView.Entities.Remove(DataView.Entities[EntityListIndex(item.Id)]);
                    EntitiesNames.Remove(DataView.Entities[EntityListIndex(item.Id)].EntityName);
                }


                DMEEditor.AddLogMessage("Success", "Removed Child entities", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not Remove Child entities";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }

        /// <summary>
        /// Generates a view from a table using the specified parameters.
        /// </summary>
        /// <param name="viewname">The name of the view to be generated.</param>
        /// <param name="SourceConnection">The data source connection object.</param>
        /// <param name="tablename">The name of the table to generate the view from.</param>
        /// <param name="SchemaName">The name of the schema containing the table.</param>
        /// <param name="Filterparamters">The filter parameters to be applied to the view.</param>
        /// <returns>The number of rows affected by the view generation process.</returns>
        public int GenerateViewFromTable(string viewname, IDataSource SourceConnection, string tablename, string SchemaName, string Filterparamters)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            int retval = 0;
            try
            {

                DataView.EntityDataSourceID = SourceConnection.DatasourceName;

                DataView.VID = Guid.NewGuid().ToString();
                retval = GenerateDataView(SourceConnection, tablename, SchemaName, Filterparamters);


            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Faile", $"Error in Loading View from file ({ex.Message}) ", DateTime.Now, 0, FileName, Errors.Failed);

            }
            return retval;

        }
        /// <summary>
        /// Generates a data view based on the provided data source, table name, schema name, and filter parameters.
        /// </summary>
        /// <param name="conn">The data source to generate the data view from.</param>
        /// <param name="tablename">The name of the table to generate the data view for.</param>
        /// <param name="SchemaName">The name of the schema to generate the data view for.</param>
        /// <param name="Filterparamters">The filter parameters to apply to the data view.</param>
        /// <returns>An integer representing the result of the data view generation.</returns>
        public int GenerateDataView(IDataSource conn, string tablename, string SchemaName, string Filterparamters)
        {
            //int maxcnt;

            DMEEditor.ErrorObject.Flag = Errors.Ok;
            List<ChildRelation> ds = (List<ChildRelation>)conn.GetChildTablesList(tablename, SchemaName, Filterparamters);
            //if (DataView.Entities.Count == 0)
            //{


            //    EntityStructure viewheader = new EntityStructure() { Id = NextHearId(), EntityName = DataView.ViewName };
            //    viewheader.DataSourceID = conn.DatasourceName;
            //    viewheader.EntityName = DataView.ViewName.ToUpper();
            //    viewheader.ParentId = 0;
            //    viewheader.ViewID = DataView.ViewID;

            //    DataView.Entities.Add(viewheader);
            //}


            //  maxcnt = DataView.Entities.Max(m => m.Id);
            EntityStructure maintab = new EntityStructure() { Id = NextHearId(), EntityName = tablename };
            maintab.DataSourceID = conn.DatasourceName;
            maintab.EntityName = tablename.ToUpper();
            maintab.ViewID = DataView.ViewID;
            maintab.DatasourceEntityName = tablename;
            maintab.ParentId = 0;

            DataView.Entities.Add(maintab);
            EntitiesNames.Add(maintab.EntityName);
            if (ds != null && ds.Count > 0)
            {

                // var tb = ds.Tables[0];
                //-------------------------------
                // Create Parent Record First
                //-------------------------------
                if (ds.Count > 0)
                {
                    foreach (ChildRelation r in ds)
                    {
                        EntityStructure a;
                        a = SetupEntityInView(DataView, DataView.Entities, r.child_table, tablename, r.child_column, r.parent_column, maintab.Id, conn.DatasourceName);



                    }

                }

            }

            return maintab.Id;

        }
        /// <summary>Generates a data view based on the specified view name and connection name.</summary>
        /// <param name="ViewName">The name of the view.</param>
        /// <param name="ConnectionName">The name of the connection.</param>
        /// <returns>The generated data view.</returns>
        public IDMDataView GenerateView(string ViewName, string ConnectionName)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            IDMDataView retval = null;

            try
            {
                retval = new DMDataView();
                retval.ViewID = 0;
                retval.EntityDataSourceID = ViewName;
                retval.ViewName = ViewName;
                retval.DataViewDataSourceID = ViewName;
                retval.Viewtype = ViewType.Table;
                retval.VID = Guid.NewGuid().ToString();
                //EntityStructure viewheader = new EntityStructure() { Id = 1, EntityName = ViewName };

                //viewheader.EntityName = ViewName;
                //viewheader.ViewID = retval.ViewID;
                //viewheader.ParentId = 0;
                //retval.Entities.Add(viewheader);

            }
            catch (Exception ex)
            {


                DMEEditor.AddLogMessage("Fail", $"Error in creating View ({ex.Message}) ", DateTime.Now, 0, ViewName, Errors.Failed);

            }
            return retval;
        }
        /// <summary>
        /// Generates a data view for a child node based on the provided parameters.
        /// </summary>
        /// <param name="conn">The data source connection.</param>
        /// <param name="pid">The parent ID.</param>
        /// <param name="tablename">The name of the table.</param>
        /// <param name="SchemaName">The name of the schema.</param>
        /// <param name="Filterparamters">The filter parameters.</param>
        /// <returns>An object representing the generated data view.</returns>
        public IErrorsInfo GenerateDataViewForChildNode(IDataSource conn, int pid, string tablename, string SchemaName, string Filterparamters)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try

            {

                EntityStructure pd = DataView.Entities.Where(c => c.Id == pid).FirstOrDefault();
                if (pd.Viewtype == ViewType.Table)
                {
                    List<ChildRelation> ds = (List<ChildRelation>)conn.GetChildTablesList(tablename, conn.Dataconnection.ConnectionProp.SchemaName, Filterparamters);
                    if (ds != null)
                    {

                        if (ds != null && ds.Count > 0)
                        {

                            // var tb = ds.Tables[0];
                            //-------------------------------
                            // Create Parent Record First
                            //-------------------------------
                            if (ds.Count > 0)
                            {
                                foreach (ChildRelation r in ds)
                                {
                                    EntityStructure a;
                                    a = SetupEntityInView(DataView, DataView.Entities, r.child_table, tablename, r.child_column, r.parent_column, pd.Id, conn.DatasourceName);

                                }
                            }
                        }
                    }
                    DMEEditor.AddLogMessage("Success", $"Getting Child from DataSource", DateTime.Now, 0, null, Errors.Ok);
                }



            }
            catch (Exception ex)
            {
                string errmsg = "Error Getting Child from DataSource";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }


            return DMEEditor.ErrorObject;
        }
        /// <summary>Adds an entity as a child to a specified parent table.</summary>
        /// <param name="conn">The data source connection.</param>
        /// <param name="tablename">The name of the table to add the entity to.</param>
        /// <param name="SchemaName">The schema name of the table.</param>
        /// <param name="Filterparamters">The filter parameters to apply.</param>
        /// <param name="viewindex">The index of the view.</param>
        /// <param name="ParentTableIndex">The index of the parent table.</param>
        /// <returns>The index of the added entity.</returns>
        public int AddEntityAsChild(IDataSource conn, string tablename, string SchemaName, string Filterparamters, int viewindex, int ParentTableIndex)
        {

            DMEEditor.ErrorObject.Flag = Errors.Ok;

            List<ChildRelation> ds = (List<ChildRelation>)conn.GetChildTablesList(tablename, SchemaName, Filterparamters);


            EntityStructure maintab = new EntityStructure() { Id = NextHearId(), EntityName = tablename };
            EntityStructure Parenttab = Entities[EntityListIndex(ParentTableIndex)];
            maintab.DataSourceID = conn.DatasourceName;
            maintab.EntityName = tablename;
            maintab.ViewID = DataView.ViewID;
            maintab.ParentId = ParentTableIndex;
            maintab.DatasourceEntityName = tablename;
            //if (CheckEntityExist(maintab.DatasourceEntityName))
            //{
            //    int cnt = EntitiesNames.Where(p => p.Equals(maintab.DatasourceEntityName, StringComparison.InvariantCultureIgnoreCase)).Count();
            //    maintab.EntityName = maintab.DatasourceEntityName + "_" + cnt + 1;
            //}
            maintab.Caption = $"{maintab.DatasourceEntityName}_{Parenttab.EntityName}s";
            DataView.Entities.Add(maintab);
            EntitiesNames.Add(maintab.EntityName);

            if (ds != null && ds.Count > 0)
            {

                //var tb = ds.Tables[0];
                //-------------------------------
                // Create Parent Record First
                //-------------------------------
                if (ds.Count > 0)
                {
                    foreach (ChildRelation r in ds)
                    {
                        EntityStructure a;
                        a = SetupEntityInView(DataView, DataView.Entities, r.child_table, maintab.DatasourceEntityName, r.child_column, r.parent_column, maintab.Id, conn.DatasourceName);

                    }
                    /// <summary>Adds an entity to a data view.</summary>
                    /// <param name="conn">The data source connection.</param>
                    /// <param name="tablename">The name of the table.</param>
                    /// <param name="SchemaName">The name of the schema.</param>
                    /// <param name="Filterparamters">The filter parameters.</param>
                    /// <returns>The number of entities added to the data view.</returns>

                }

            }

            return maintab.Id;

        }
        /// <summary>Adds an entity to a data view.</summary>
        /// <param name="conn">The data source connection.</param>
        /// <param name="tablename">The name of the table.</param>
        /// <param name="SchemaName">The name of the schema.</param>
        /// <param name="Filterparamters">The filter parameters.</param>
        /// <returns>The number of entities added to the data view.</returns>
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
                    int cnt = EntitiesNames.Where(p => p.Equals(maintab.DatasourceEntityName, StringComparison.InvariantCultureIgnoreCase)).Count();
                    maintab.EntityName = maintab.DatasourceEntityName + "_" + cnt + 1;
                }
                switch (maintab.DatabaseType)
                {
                    case DataSourceType.Oracle:

                    case DataSourceType.SqlServer:

                    case DataSourceType.Mysql:

                    case DataSourceType.SqlCompact:
                    case DataSourceType.Postgre:

                    case DataSourceType.Firebase:

                    case DataSourceType.FireBird:

                    case DataSourceType.Couchbase:

                    case DataSourceType.RavenDB:

                    case DataSourceType.MongoDB:

                    case DataSourceType.CouchDB:

                    case DataSourceType.VistaDB:

                    case DataSourceType.DB2:

                    case DataSourceType.SqlLite:
                        maintab.Viewtype = ViewType.Table;
                        break;
                    case DataSourceType.Text:

                    case DataSourceType.CSV:

                    case DataSourceType.Xls:
                    case DataSourceType.Json:

                    case DataSourceType.XML:
                        maintab.Viewtype = ViewType.File;
                        break;
                    

                    case DataSourceType.OPC:
                        maintab.Viewtype = ViewType.Url;
                        break;
                    default:

                        maintab.Viewtype = ViewType.Table;
                        break;

                }

                DataView.Entities.Add(maintab);
                EntitiesNames.Add(maintab.EntityName);
                if (ds != null && ds.Count > 0)
                {
                    // var tb = ds.Tables[0];
                    //-------------------------------
                    // Create Parent Record First
                    //-------------------------------
                    if (ds.Count > 0)
                    {
                        foreach (ChildRelation r in ds)
                        {
                            EntityStructure a;
                            a = SetupEntityInView(DataView, DataView.Entities, r.child_table, tablename, r.child_column, r.parent_column, maintab.Id, conn.DatasourceName);



                        }

                    }

                }

                return maintab.Id;

            }
            catch (Exception)
            {


                return -1;
            }




        }
        /// <summary>Adds an entity to the data view.</summary>
        /// <param name="maintab">The entity structure to add.</param>
        /// <returns>The index of the added entity in the data view.</returns>
        public int AddEntitytoDataView(EntityStructure maintab)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            string tablename = maintab.DatasourceEntityName;
            int k = 0;
            int y = 0;
            try
            {
                maintab.Id = NextHearId();
                maintab.ViewID = 0;
                if (maintab.ParentId == -1)
                {
                    maintab.ParentId = 0;
                }

                //--- check entity already exist , if it does change Entity Name
                if (CheckEntityExist(maintab.DatasourceEntityName))
                {
                    if (maintab.ParentId == 0)
                    {
                        int cnt = EntitiesNames.Where(p => p.Equals(maintab.DatasourceEntityName, StringComparison.InvariantCultureIgnoreCase)).Count() + 1;
                        if (cnt > 0)
                        {
                            List<EntityStructure> ls = new List<EntityStructure>();
                            foreach (string item in EntitiesNames.Where(p => p.Equals(maintab.DatasourceEntityName, StringComparison.InvariantCultureIgnoreCase)))
                            {
                                ls.Add(GetEntityStructure(item, false));
                            }
                            List<EntityStructure> lsSameDB = new List<EntityStructure>();
                            lsSameDB.AddRange(ls.Where(p => p.DataSourceID.Equals(maintab.DataSourceID, StringComparison.InvariantCultureIgnoreCase) && p.ParentId == 0).AsEnumerable());
                            List<EntityStructure> lsDiffDB = new List<EntityStructure>();
                            lsDiffDB.AddRange(ls.Where(p => !p.DataSourceID.Equals(maintab.DataSourceID, StringComparison.InvariantCultureIgnoreCase) && p.ParentId == 0).AsEnumerable());
                            k = lsDiffDB.Count();
                            y = lsSameDB.Count();
                            if (k > 0)
                            {
                                maintab.Caption = maintab.DatasourceEntityName + $"_{maintab.DataSourceID}_" + k;
                            }
                            if (y > 0)
                            {
                                maintab.Caption = maintab.DatasourceEntityName + $"_{maintab.DataSourceID}_" + y;
                            }
                            //foreach (var item in lsDiffDB)
                            //{
                            //    if (k == 0)
                            //    {

                            //        item.Caption = item.DatasourceEntityName + $"_{item.DataSourceID}";
                            //        Entities[EntityListIndex(item.DatasourceEntityName)] = item;

                            //    }
                            //    else
                            //    {
                            //        item.Caption = item.DatasourceEntityName + $"_{item.DataSourceID}" + k.ToString();
                            //        Entities[EntityListIndex(item.DatasourceEntityName)] = item;
                            //    }

                            //    k++;
                            //}
                            //y = 0;
                            //foreach (var item in lsSameDB)
                            //{
                            //    if (y == 0)
                            //    {

                            //        item.Caption = item.DatasourceEntityName + $"_{item.DataSourceID}";
                            //        Entities[EntityListIndex(item.DatasourceEntityName)] = item;
                            //    }
                            //    else
                            //    {
                            //        item.Caption = item.DatasourceEntityName + $"_{item.DataSourceID}" + y.ToString();
                            //        Entities[EntityListIndex(item.DatasourceEntityName)] = item;
                            //    }
                            //    y++;

                            //}

                        }


                    }
                    else
                    {
                        IEntityStructure parententity = Entities[EntityListIndex(maintab.ParentId)];
                        if (parententity != null)
                        {
                            maintab.Caption = parententity.DatasourceEntityName + $"_{maintab.EntityName}s";
                        }
                    }



                }
                maintab.OriginalEntityName = maintab.DatasourceEntityName;
                DataView.Entities.Add(maintab);
                EntitiesNames.Add(maintab.EntityName);
                IDataSource entityds = DMEEditor.GetDataSource(maintab.DataSourceID);
                if (entityds != null && entityds.Category == DatasourceCategory.RDBMS)
                {
                    List<ChildRelation> ds = (List<ChildRelation>)entityds.GetChildTablesList(maintab.DatasourceEntityName, entityds.Dataconnection.ConnectionProp.SchemaName, null);
                    if (DMEEditor.ErrorObject.Flag == Errors.Ok)
                    {
                        if (ds != null && ds.Count > 0)
                        {
                            // var tb = ds.Tables[0];
                            //-------------------------------
                            // Create Parent Record First
                            //-------------------------------
                            if (ds.Count > 0)
                            {
                                foreach (ChildRelation r in ds)
                                {
                                    EntityStructure a;
                                    a = SetupEntityInView(DataView, DataView.Entities, r.child_table, maintab.DatasourceEntityName, r.child_column, r.parent_column, maintab.Id, entityds.DatasourceName);
                                }
                            }
                        }
                        else
                        {
                            DMEEditor.ErrorObject.Flag = Errors.Ok;
                        }
                    }

                }

                return maintab.Id;
            }
            catch (Exception)
            {
                return -1;
            }
        }
        #endregion  "View Generating Methods"
        #region "Misc and Util Methods"
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
                    DataColumn co = new DataColumn(item.fieldname, Type.GetType(item.fieldtype));
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

        /// <summary>Loads a view and returns information about any errors that occurred.</summary>
        /// <returns>An object containing information about any errors that occurred during the view loading process.</returns>
        public IErrorsInfo LoadView()
        {
            try
            {

                if (Dataconnection.ConnectionStatus == ConnectionState.Open)
                {
                    DataView = ReadDataViewFile(DataViewFile);

                    if (DataView.VID == null)
                    {
                        DataView.VID = Guid.NewGuid().ToString();
                    }

                }

                DMEEditor.AddLogMessage("Success", "Loaded File", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not Load File";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;

        }

        /// <summary>Returns the index of the specified entity.</summary>
        /// <param name="entityName">The name of the entity.</param>
        /// <returns>The index of the entity.</returns>
        public int GetEntityIdx(string entityName)
        {
            throw new NotImplementedException();
        }

        #endregion
        #endregion
        #region "dispose"
        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
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
