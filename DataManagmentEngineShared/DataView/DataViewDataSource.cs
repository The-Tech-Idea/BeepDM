using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.Editor;
using TheTechIdea.DataManagment_Engine.Report;
using TheTechIdea.DataManagment_Engine.Workflow;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.DataView
{
    public class DataViewDataSource : IDataSource,IDMDataView
    {
        public event EventHandler<PassedArgs> PassEvent;
        public DataSourceType DatasourceType { get ; set ; }
        public DatasourceCategory Category { get ; set ; }
        public IDataConnection Dataconnection { get ; set ; }
        public string DatasourceName { get ; set ; }
        public IErrorsInfo ErrorObject { get ; set ; }
        public string Id { get ; set ; }
        public IDMLogger Logger { get ; set ; }
        public List<string> EntitiesNames { get; set; } = new List<string>();
        public IDMEEditor DMEEditor { get ; set ; }
        public ConnectionState ConnectionStatus { get { return Dataconnection.ConnectionStatus; } set { }  }
        public DataTable SourceEntityData { get ; set ; }
        public IDMDataView DataView { get; set; } = new DMDataView();
        public List<EntityStructure> Entities { get 
            {
                if (DataView != null)
                {
                   
                    return DataView.Entities;
                }else
                {
                    return new List<EntityStructure>();
                }
               
            } set 
            {
                DataView.Entities = value;
            } 
            }
        public string ViewName { get ; set ; }
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
        public  bool Editable { get ; set ; }
        public string EntityDataSourceID { get ; set ; }
        public string CompositeLayerDataSourceID { get; set; }
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
    
        public DataViewDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType pDatasourceType , IErrorsInfo per)
        {
            DatasourceName = datasourcename;
            Logger = logger;
            ErrorObject = per;
            DMEEditor = pDMEEditor;
            DatasourceType = DataSourceType.Text;
            Category = DatasourceCategory.VIEWS;
            Dataconnection = new DataViewConnection
            {
                Logger = logger,
                ErrorObject = ErrorObject,

            };
            string filename = Path.GetFileName(datasourcename);
            string filepath; //= DMEEditor.ConfigEditor.Config.Folders.Where(c => c.FolderFilesType == FolderFileTypes.DataView).FirstOrDefault().FolderPath;
            if (DMEEditor.ConfigEditor.DataConnections.Where(c => c.FileName == filename).Any())
            {
                Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.FileName == filename).FirstOrDefault();
                filepath = Dataconnection.ConnectionProp.FilePath;


            }
            else
            {
                filepath = DMEEditor.ConfigEditor.Config.Folders.Where(c => c.FolderFilesType == FolderFileTypes.DataView).FirstOrDefault().FolderPath;
                Dataconnection.ConnectionProp = new ConnectionProperties();
                Dataconnection.ConnectionProp.FileName = filename;
                Dataconnection.ConnectionProp.FilePath = filepath;
                Dataconnection.ConnectionProp.Category = DatasourceCategory.VIEWS;
                Dataconnection.ConnectionProp.DatabaseType = DataSourceType.Json;
                Dataconnection.ConnectionProp.DriverVersion = "1";
                Dataconnection.ConnectionProp.DriverName = "DataViewReader";
                DMEEditor.ConfigEditor.DataConnections.Add((ConnectionProperties)Dataconnection.ConnectionProp);
                DMEEditor.ConfigEditor.SaveDataconnectionsValues();

            }
            DataViewFile = Path.Combine(filepath, filename);
            Dataconnection.OpenConnection();
            if (Dataconnection.ConnectionStatus== ConnectionState.Open)
            {
                LoadView();
            }
            else
            {
                DataView = GenerateView(filename, filepath);
                WriteDataViewFile(DataViewFile);
            }

        }
        public List<string> GetEntitesList()
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
                    if (string.IsNullOrEmpty(i.DatasourceEntityName))
                    {
                        retval.Add(i.EntityName);
                    }
                    else
                        retval.Add(i.DatasourceEntityName);
                    
                }



            }

            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
                Logger.WriteLog($"Error in getting entity Data ({ex.Message}) ");
            }
            //if ((Records == null)||(Records.Count==0))
            //{
            //    Records.Add(x);
            //}
            EntitiesNames = retval;
            return retval;
        }
        public object GetEntity(string EntityName, List<ReportFilter> filter)
        {
            object retval = null;
            IDataSource ds = GetDataSourceObject(EntityName);
            if (ds != null)
            {
                if(ds.ConnectionStatus== ConnectionState.Open)
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
                                //retval = GetDataSourceObject(EntityName).RunQuery(querystr);
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
        public int EntityListIndex(int entityid)
        {

            return DataView.Entities.FindIndex(a => a.Id == entityid);
        }
        public int EntityListIndex( string entityname)
        {

            return Entities.FindIndex(a => a.EntityName.Equals(entityname,StringComparison.OrdinalIgnoreCase));
        }
        public EntityStructure GetEntityStructure(string EntityName, bool refresh = false)
        {
            try

            {
                EntityStructure r = new EntityStructure();
                EntityStructure dh = Entities[EntityListIndex(EntityName)];
                ds = DMEEditor.GetDataSource(dh.DataSourceID);
                if (ds == null)
                {
                    DMEEditor.AddLogMessage("Error", "Could not Find DataSource " + dh.DataSourceID, DateTime.Now, dh.Id, dh.EntityName, Errors.Failed);

                }
                else
                {
                    ds.Dataconnection.OpenConnection();
                    ds.ConnectionStatus = ds.Dataconnection.ConnectionStatus;
                    if (ds.ConnectionStatus== ConnectionState.Open)
                    {
                        switch (dh.Viewtype)
                        {
                            case ViewType.Table:
                            case ViewType.Query:
                            case ViewType.File:
                            case ViewType.Url:
                                r = GetDataSourceObject(dh.EntityName).GetEntityStructure(dh, refresh);
                                dh.Fields = r.Fields;
                                dh.Relations = r.Relations;
                                dh.PrimaryKeys = r.PrimaryKeys;
                                break;
                           

                            case ViewType.Code:
                            default:
                                break;

                        }
                        
                    }
                   
                }


                return dh;
            }
            catch (Exception ex)
            {

                ErrorObject.Flag = Errors.Failed;
                string errmsg = "Error getting entity structure";
                ErrorObject.Message = $"{errmsg}:{ex.Message}";
                errmsg = ErrorObject.Message;

                Logger.WriteLog($" {errmsg} :{ex.Message}");
            }
            return null;
            //      return GetDataSourceObject(EntityName).GetEntityStructure(EntityName, refresh);
        }
        public Type GetEntityType(string entityname)
        {
            EntityStructure dh = Entities[EntityListIndex(entityname)];
            Type retval;
            switch (dh.Viewtype)
            {
                case ViewType.Table:
                    retval=GetDataSourceObject(entityname).GetEntityType(entityname);
                    break;
                case ViewType.Query:

                case ViewType.Code:

                case ViewType.File:

                case ViewType.Url:


                default:
                   
                    DMTypeBuilder.CreateNewObject(entityname, entityname,dh.Fields);
                    retval= DMTypeBuilder.myType;
                    break;

            }
            return retval;
        }
        public List<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            EntityStructure dh = Entities[EntityListIndex(tablename)];
           
            switch (dh.Viewtype)
            {
                case ViewType.Table:
                    return GetDataSourceObject(tablename).GetChildTablesList(tablename, SchemaName, Filterparamters);
                    ;
                case ViewType.Query:

                case ViewType.Code:

                case ViewType.File:

                case ViewType.Url:


                default:

                    return null;
                   
            }
            
        }
        public List<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            IDataSource ds = GetDataSourceObject(entityname);
            if (ds.ConnectionStatus == ConnectionState.Open)
            {
                if (ds.Category == DatasourceCategory.RDBMS)
                {
                    RDBSource rdb = (RDBSource)ds;
                    return rdb.GetEntityforeignkeys(entityname, SchemaName);
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
        public IErrorsInfo ExecuteSql(string sql)
        {
            throw new NotImplementedException();
         
        }
        public bool CreateEntityAs(EntityStructure entity)
        {
            try

            {
                AddEntitytoDataView(entity);
               // Dataview.Entities.Add(entity);
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
        public bool CheckEntityExist(string entityname)
        {
            if (Entities.Any(x=> x.EntityName.Equals(entityname, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }else
            {
                return false;
            }
      
        }
        private IDataSource GetDataSourceObject(string entityname)
        {
            IDataSource retval;
            EntityStructure dh = Entities.Where(x => string.Equals(x.EntityName, entityname, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            if (dh==null)
            {
                retval = DMEEditor.GetDataSource(DataView.EntityDataSourceID);
               
            }
            else
            {
                retval= DMEEditor.GetDataSource(dh.DataSourceID);
            }
            retval.Dataconnection.OpenConnection();
            retval.ConnectionStatus = Dataconnection.ConnectionStatus;
            return retval;


        }
        public  object RunQuery( string qrystr)
        {
            throw new NotImplementedException();
           // ds = DMEEditor.GetDataSource(DatasourceName);
           //return ds.RunQuery(qrystr);
        }
        public IErrorsInfo UpdateEntities(string EntityName, object UploadData)
        {
            return GetDataSourceObject(EntityName).UpdateEntities(EntityName, UploadData);
        }
        public virtual IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {


            return GetDataSourceObject(EntityName).UpdateEntity(EntityName, UploadDataRow);
        }
        public IErrorsInfo DeleteEntity(string EntityName, object DeletedDataRow)
        {
            return GetDataSourceObject(EntityName).DeleteEntity(EntityName, DeletedDataRow);
        }
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
        public LScript RunScript(LScript dDLScripts)
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
        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
          //  ds = DMEEditor.GetDataSource(DatasourceName);
            Entities.AddRange(entities);
            // return ds.CreateEntities(entities);
            return DMEEditor.ErrorObject;
        }
        public List<LScript> GetCreateEntityScript(List<EntityStructure> entities = null)
        {
            List<LScript> ls = new List<LScript>();
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
        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            throw new NotImplementedException();
        }
        public Task<object> GetEntityAsync(string EntityName, List<ReportFilter> Filter)
        {
            return (Task<object>)GetEntity(EntityName, Filter);
        }
        #region "DataView Methods"

        #region "View Generating Methods"
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
        public int GenerateDataView(IDataSource conn, string tablename, string SchemaName, string Filterparamters)
        {
            //int maxcnt;

            DMEEditor.ErrorObject.Flag = Errors.Ok;
            List<ChildRelation> ds = conn.GetChildTablesList(tablename, SchemaName, Filterparamters);
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
        public IErrorsInfo GenerateDataViewForChildNode(IDataSource conn, int pid, string tablename, string SchemaName, string Filterparamters)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try

            {

                EntityStructure pd = DataView.Entities.Where(c => c.Id == pid).FirstOrDefault();
                if (pd.Viewtype== ViewType.Table ) 
                {
                    List<ChildRelation> ds = conn.GetChildTablesList(tablename, conn.Dataconnection.ConnectionProp.SchemaName, Filterparamters);
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
        public int AddEntityAsChild(IDataSource conn, string tablename, string SchemaName, string Filterparamters, int viewindex, int ParentTableIndex)
        {

            DMEEditor.ErrorObject.Flag = Errors.Ok;

            List<ChildRelation> ds = conn.GetChildTablesList(tablename, SchemaName, Filterparamters);


            EntityStructure maintab = new EntityStructure() { Id = NextHearId(), EntityName = tablename };
            maintab.DataSourceID = conn.DatasourceName;
            maintab.EntityName = tablename;
            maintab.ViewID = DataView.ViewID;
            maintab.ParentId = ParentTableIndex;
            maintab.DatasourceEntityName = tablename;
            if (CheckEntityExist(maintab.DatasourceEntityName))
            {
                int cnt = EntitiesNames.Where(p => p.Equals(maintab.DatasourceEntityName, StringComparison.OrdinalIgnoreCase)).Count();
                maintab.EntityName = maintab.DatasourceEntityName + "_" + cnt + 1;
            }
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
                        a = SetupEntityInView(DataView, DataView.Entities, r.child_table, maintab.DatasourceEntityName, r.child_column, r.parent_column, maintab.Id,conn.DatasourceName);

                    }

                }

            }

            return maintab.Id;

        }
        public int AddEntitytoDataView(IDataSource conn, string tablename, string SchemaName, string Filterparamters)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;

            EntityStructure maintab;
            try
            {

                List<ChildRelation> ds = conn.GetChildTablesList(tablename, SchemaName, Filterparamters);
                maintab = new EntityStructure() { Id = NextHearId(), EntityName = tablename };
                maintab.DataSourceID = conn.DatasourceName;
                maintab.EntityName = tablename.ToUpper();
                maintab.ViewID = 0;
                maintab.ParentId = 0;
                maintab.DatabaseType = conn.DatasourceType;
                maintab.DatasourceEntityName = tablename;
                if (CheckEntityExist(maintab.DatasourceEntityName))
                {
                    int cnt = EntitiesNames.Where(p => p.Equals(maintab.DatasourceEntityName, StringComparison.OrdinalIgnoreCase)).Count();
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

                    case DataSourceType.xml:
                        maintab.Viewtype = ViewType.File;
                        break;
                    case DataSourceType.WebService:

                    case DataSourceType.OPC:
                        maintab.Viewtype = ViewType.Url;
                        break;
                    default:
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
        public int AddEntitytoDataView(EntityStructure maintab)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            string tablename = maintab.DatasourceEntityName;
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
                    int cnt = EntitiesNames.Where(p => p.Equals(maintab.DatasourceEntityName, StringComparison.OrdinalIgnoreCase)).Count()+1;
                    maintab.EntityName = maintab.DatasourceEntityName + "_" + cnt ;
                }

                DataView.Entities.Add(maintab);
                EntitiesNames.Add(maintab.EntityName);
                IDataSource entityds = DMEEditor.GetDataSource(maintab.DataSourceID);
                if (entityds != null && entityds.Category== DatasourceCategory.RDBMS)
                {
                    List<ChildRelation> ds = entityds.GetChildTablesList(maintab.DatasourceEntityName, entityds.Dataconnection.ConnectionProp.SchemaName, null);
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
        private EntityStructure SetupEntityInView(IDMDataView v, List<EntityStructure> Rootnamespacelist, string childtable, string parenttable, string childcolumn, string parentcolumn, int pid,string Datasourcename)
        {

            EntityStructure a;
            int pkid = NextHearId();
            IDataSource ds = DMEEditor.GetDataSource(Datasourcename);
            string schemaname = "";
            if (ds.Category == DatasourceCategory.RDBMS)
            {
                IRDBSource rdb = (IRDBSource)ds;
                schemaname = rdb.GetSchemaName();
            }

            if (!Rootnamespacelist.Where(f => f.ParentId == pid && f.EntityName.Equals(childtable,StringComparison.OrdinalIgnoreCase)).Any())//f => f.Id == childtable &&
            {
                //a = new EntityStructure() { Id = pkid, ParentId = pid, EntityName = childtable.ToUpper(), ViewID = v.ViewID };
                //a.DataSourceID = v.Entities.Where(x => x.Id == pid).FirstOrDefault().DataSourceID;
                //a.DatasourceEntityName = childtable;
                //a.Relations = ds.GetEntityforeignkeys(childtable.ToUpper(), schemaname);
               
                a = (EntityStructure)ds.GetEntityStructure(childtable, true).Clone();
                a.ParentId = pid;
                a.Id = NextHearId();
                Rootnamespacelist.Add(a);


            }
            else
            {
                a = Rootnamespacelist.Where(f => f.ParentId == pid && f.EntityName.Equals(childtable, StringComparison.OrdinalIgnoreCase)).FirstOrDefault(); //f.Id == childtable &&
              //  a.DataSourceID = DatasourceName;
                a.DatasourceEntityName = childtable;
                a.Relations.Add(new RelationShipKeys { EntityColumnID = childcolumn.ToUpper(), ParentEntityColumnID = parentcolumn.ToUpper(), ParentEntityID = parenttable.ToUpper() });

            }
            return a;
        }
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
        public int NextHearId()
        {

            if (DataView.Entities != null)
            {
                if (DataView.Entities.Count >0)
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
                    List<RelationShipKeys> rl = entity.Relations.Where(c => c.ParentEntityID == parenttb.EntityName).ToList();

                    DataTable Parenttb = dataset.Tables[parenttb.EntityName];
                    foreach (string relationname in rl.Select(x => x.RalationName).Distinct())
                    {
                        int k = 0;
                        int cnt = rl.Where(u => u.RalationName == relationname).Count();
                        DataColumn[] ParentColumn = new DataColumn[cnt];
                        DataColumn[] ChildColumn = new DataColumn[cnt];
                        foreach (RelationShipKeys keys in rl.Where(u => u.RalationName == relationname))
                        {
                            ParentColumn[k] = Parenttb.Columns[keys.ParentEntityColumnID];
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
        public void WriteDataViewFile(string filename)
        {
            string path;
            if (!string.IsNullOrEmpty(Dataconnection.ConnectionProp.FilePath)|| !string.IsNullOrWhiteSpace(Dataconnection.ConnectionProp.FilePath))
            {
                path = Path.Combine(Dataconnection.ConnectionProp.FilePath, $"{filename}");
            }
            else
            {
                path = Path.Combine(DMEEditor.ConfigEditor.Config.Folders.Where(c => c.FolderFilesType == FolderFileTypes.DataView).FirstOrDefault().FolderPath, $"{filename}");
            }
             
            DMEEditor.ConfigEditor.JsonLoader.Serialize(path, DataView);

        }

        public void WriteDataViewFile(string path, string filename)
        {
            string name = Path.Combine(path, $"{filename}");
            DMEEditor.ConfigEditor.JsonLoader.Serialize(name, DataView);

        }
        public IDMDataView ReadDataViewFile(string pathandfilename)
        {
            // String JSONtxt = File.ReadAllText(pathandfilename);
            return DMEEditor.ConfigEditor.JsonLoader.DeserializeSingleObject<DMDataView>(pathandfilename);


        }
      
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

        #endregion
        #endregion
    }
}
