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
        public List<string> EntitiesNames { get ; set ; }
        public IDMEEditor DMEEditor { get ; set ; }
        public List<object> Records { get ; set ; }
        public ConnectionState ConnectionStatus { get ; set ; }
        public DataTable SourceEntityData { get ; set ; }
        public List<EntityStructure> Entities { get 
            {
               return ViewReader.DataView.Entities;
            } set 
            {
                ViewReader.DataView.Entities = value;
            } 
            }
        public string ViewName { get ; set ; }
        public int ViewID
        {
            get
            {
                return ViewReader.DataView.ViewID;
            }
            set
            {
                ViewReader.DataView.ViewID = value;
            }
        }
        public ViewType Viewtype
        {
            get
            {
                return ViewReader.DataView.Viewtype;
            }
            set
            {
                ViewReader.DataView.Viewtype = value;
            }
        }
        public  bool Editable { get ; set ; }
        public string EntityDataSourceID { get ; set ; }
        public string CompositeLayerDataSourceID { get; set; }
        public IDMDataView Dataview
        {
            get
            {
                return ViewReader.DataView;
            }
            set
            {
                ViewReader.DataView = value;
            }
        }
        public string DataViewDataSourceID
        {
            get
            {
                return ViewReader.DataView.DataViewDataSourceID;
            }
            set
            {
                ViewReader.DataView.DataViewDataSourceID = value;
            }
        }
        public string VID
        {
            get
            {
                return ViewReader.DataView.VID;
            }
            set
            {
                ViewReader.DataView.VID = value;
            }
        }

        public IDataViewReader ViewReader { get; set; }

        IDataSource ds;
      //  IRDBSource rdbms;
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
            string filepath = DMEEditor.ConfigEditor.Config.Folders.Where(c => c.FolderFilesType == FolderFileTypes.DataView).FirstOrDefault().FolderPath;
            ViewReader = new DataViewReader(datasourcename, DMEEditor, filepath, filename);
            if (DMEEditor.ConfigEditor.DataConnections.Where(c => c.FileName == filename).Any())
            {
                Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.FileName == filename).FirstOrDefault();
            }
            else
            {
                Dataconnection = ViewReader.Dataconnection;
            }
            ViewReader.LoadView();
            Entities = ViewReader.DataView.Entities;



        }
        //public IErrorsInfo LoadView()
        //{
        //    try
        //    {
        //        ConnectionStatus = Dataconnection.OpenConnection();
        //        if (ConnectionStatus == ConnectionState.Open)
        //        {
        //            ViewReader.LoadView();
        //            Dataview = ViewReader.DataView;
        //            if (Dataview.VID == null)
        //            {
        //                VID = Guid.NewGuid().ToString();
        //            }
        //            ViewName = Dataview.ViewName;

        //            Entities = Dataview.Entities;
        //           // GetEntitesList();
        //        }

        //        DMEEditor.AddLogMessage("Success", "Loaded File", DateTime.Now, 0, null, Errors.Ok);
        //    }
        //    catch (Exception ex)
        //    {
        //        string mes = "Could not Load File";
        //        DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
        //    };
        //    return DMEEditor.ErrorObject;

        //}
        public List<string> GetEntitesList()
        {
            ErrorObject.Flag = Errors.Ok;
            List<string> retval = new List<string>();
            try
            {
                Dataview = ViewReader.DataView;
                if (Dataview.Entities.Count <= 2)
                {
                    ViewReader.LoadView();
                    Dataview = ViewReader.DataView;
                    if (Dataview.VID == null)
                    {
                        VID = Guid.NewGuid().ToString();
                    }
                    ViewName = Dataview.ViewName;

                    Entities = Dataview.Entities;
                }
                foreach (EntityStructure i in Dataview.Entities.Where(x=>x.Id>1))
                {
                    retval.Add(i.EntityName);
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
            return GetDataSourceObject(EntityName).GetEntity(EntityName, filter);
        }
       
        public int EntityListIndex( string entityname)
        {

            return Entities.FindIndex(a => a.EntityName == entityname);
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
                    if (ds.ConnectionStatus== ConnectionState.Open)
                    {
                        if (dh.Viewtype == ViewType.Query)
                        {
                            r = ds.GetEntityStructure(dh, true);

                        }
                        else
                        {
                            r = ds.GetEntityStructure(dh, true);
                        }
                        if (r != null)
                        {
                            dh.Fields = r.Fields;
                            dh.Relations = r.Relations;
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
            return GetDataSourceObject(entityname).GetEntityType(entityname);
        }

     

        public List<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            return GetDataSourceObject(tablename).GetChildTablesList(tablename, SchemaName, Filterparamters);
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
            if (ds.ConnectionStatus == ConnectionState.Open)
            {
                ds = DMEEditor.GetDataSource(DatasourceName);
                return ds.ExecuteSql(sql);
            }
            else
            {
                DMEEditor.AddLogMessage("Error", "$Could not Find DataSource {DatasourceName}", DateTime.Now, 0, DatasourceName, Errors.Failed);
            }
                return DMEEditor.ErrorObject;
             
        }

        public bool CreateEntityAs(EntityStructure entity)
        {
            try

            {
                Dataview.Entities.Add(entity);
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
            if (Entities.Any(x=> string.Equals(x.EntityName, entityname, StringComparison.OrdinalIgnoreCase)))
            {
                return GetDataSourceObject(entityname).CheckEntityExist(entityname);
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
                retval = DMEEditor.GetDataSource(Dataview.EntityDataSourceID);
            }
            else
            {
                retval= DMEEditor.GetDataSource(dh.DataSourceID);
            }
            return retval;


        }


        public DataTable RunQuery(string qrystr)
        {
           
            ds = DMEEditor.GetDataSource(DatasourceName);
           return ds.RunQuery(qrystr);
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
            return GetDataSourceObject(fnd.EntityName).GetEntityStructure(fnd, refresh);
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
            throw new NotImplementedException();
        }
    }
}
