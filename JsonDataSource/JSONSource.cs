using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.Editor;
using TheTechIdea.DataManagment_Engine.Report;
using TheTechIdea.DataManagment_Engine.Workflow;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.FileManager
{
    public class JSONSource : IDataSource
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
        public List<EntityStructure> Entities { get; set; } = new List<EntityStructure>();
        public IDMEEditor DMEEditor { get ; set ; }
        public List<object> Records { get ; set ; }
        public ConnectionState ConnectionStatus { get ; set ; }
        public DataTable SourceEntityData { get ; set ; }
        public JSONReader Reader { get; set; }
        public JSONSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType pDatasourceType, IErrorsInfo per)
        {
            DatasourceName = datasourcename;
            Logger = logger;
            ErrorObject = per;
            DMEEditor = pDMEEditor;
            DatasourceType = pDatasourceType;
            Dataconnection = new FileConnection
            {
                Logger = logger,
                ErrorObject = ErrorObject,

            };
         
            Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.FileName == datasourcename).FirstOrDefault();
            Reader = new JSONReader(Dataconnection.ConnectionProp.FileName, DMEEditor, Dataconnection.ConnectionProp, null);
            Dataconnection.ConnectionStatus = ConnectionStatus;
            GetEntitesList();


        }
        public bool CheckEntityExist(string EntityName)
        {
            throw new NotImplementedException();
        }

        public bool CreateEntityAs(EntityStructure entity)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo ExecuteSql(string sql)
        {
            throw new NotImplementedException();
        }

        public List<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            throw new NotImplementedException();
        }

        public DataSet GetChildTablesListFromCustomQuery(string tablename, string customquery)
        {
            throw new NotImplementedException();
        }

        public IDataReader GetDataReader(string querystring)
        {
            throw new NotImplementedException();
        }

        public  List<string> GetEntitesList()
        {
            ErrorObject.Flag = Errors.Ok;

            try
            {
              //  List<EntityStructure> entlist = new List<EntityStructure>();
                EntitiesNames = new List<string>();
                EntitiesNames=Reader.GetEntities().ToList();
                


                //  DMEEditor.ConfigEditor.DataConnections.Where(c => c.FileName == DatasourceName).FirstOrDefault().Entities =entlist ;
                Logger.WriteLog("Successfully Retrieve Entites list ");

            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Unsuccessfully Retrieve Entites list {ex.Message}");
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
            }

            return EntitiesNames;

        }

        public async Task<object> GetEntityDataAsync(string entityname, string filterstr)
        {

            ErrorObject.Flag = Errors.Ok;
            try
            {
                return await Task.Run(() => Reader.ReadList(0, true, 0, 0));



            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
                Logger.WriteLog($"Error in getting File Data ({ex.Message}) ");
                return null;
            }
           
        }

        public  object GetEntity(string EntityName, List<ReportFilter> filter)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                return   Reader.ReadDataTable(EntityName, true, 0, 0);



            }
            catch (Exception ex)
            {

                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
                Logger.WriteLog($"Error in getting File Data ({ex.Message}) ");
                return null;
            }
        }

        public List<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            throw new NotImplementedException();
        }

       

        public DataTable GetEntityDataTable(string EntityName, string filterstr)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {

                return Reader.ReadDataTable(0, true, 0, 100);

            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
                Logger.WriteLog($"Error in getting File Data ({ex.Message}) ");
                return null;
            }
            
        }

        public Type GetEntityType(string EntityName)
        {
            string filenamenoext = EntityName;
            DMTypeBuilder.CreateNewObject(EntityName, EntityName, Entities.Where(x => x.EntityName == EntityName).FirstOrDefault().Fields);
            return DMTypeBuilder.myType;
        }

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData)
        {
            throw new NotImplementedException();
        }

        public  DataTable RunQuery( string qrystr)
        {
            throw new NotImplementedException();
        }
       
        public EntityStructure GetEntityStructure(string EntityName,bool refresh=false )
        {
            if (ConnectionStatus == ConnectionState.Open)
            {
                if (Reader.Entities.Count() == 0)
                {
                    Reader.GetEntityStructures();
                }
            }
            if (Reader.Entities.Count == 1)
            {
                return Reader.Entities.FirstOrDefault();
            }
            else
                return Reader.Entities.Where(x => x.EntityName == EntityName).FirstOrDefault();

        }
        public virtual IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {


            throw new NotImplementedException();
        }
        public IErrorsInfo DeleteEntity(string EntityName, object DeletedDataRow)
        {
            throw new NotImplementedException();
        }

        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            throw new NotImplementedException();
        }
        public LScript RunScript(LScript dDLScripts)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            throw new NotImplementedException();
        }
        public List<LScript> GetCreateEntityScript(List<EntityStructure> entities = null)
        {
            throw new NotImplementedException();
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
