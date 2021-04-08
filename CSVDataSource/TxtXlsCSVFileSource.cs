
using TheTechIdea.Logger;
using System.Data;
using TheTechIdea.Util;

using System.IO;
using System.Collections.Specialized;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using TheTechIdea.DataManagment_Engine.Workflow;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.Editor;
using TheTechIdea.DataManagment_Engine.Report;

namespace TheTechIdea.DataManagment_Engine.FileManager
{
    public class TxtXlsCSVFileSource : IDataSource
    {
        public event EventHandler<PassedArgs> PassEvent;
        public string Id { get; set; }
        public string DatasourceName { get; set; }
     //   public IFileConnection Fileconnection { get; set; }

        public FileTypes FileType { get; set; }

        public DataSourceType DatasourceType { get; set; }
        public DatasourceCategory Category { get; set; } = DatasourceCategory.FILE;
        public ConnectionState ConnectionStatus { get ;set ;}
        public List<string> EntitiesNames { get; set; }

        public IDMEEditor DMEEditor { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public IDMLogger Logger { get; set; }
        public List<object> Records { get; set; }
        public List<EntityStructure> Entities {
            get {
                return Reader.Entities;
            }
            set {
                Reader.Entities = value; 
            } 
        } 

        public List<object> SampleLines { get; set; }
        public object MyType { get; set; }
        public DataTable SourceEntityData { get; set; }
        public bool HeaderExist { get; set; }
        public int fromline { get; set; }
        public int  toline { get; set; }
        public TxtXlsCSVReader Reader { get; set; }
        public IDataConnection Dataconnection { get; set ; }
      
        public TxtXlsCSVFileSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType pDatasourceType ,  IErrorsInfo per)
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
         
            FileTypes ft = FileTypes.Text;
            switch (Dataconnection.ConnectionProp.Ext.ToLower())
            {
                case ".txt":
                case ".csv":
                    ft = FileTypes.Text;
                    ; break;
                case ".xls":
                case ".xlsx":
                    ft = FileTypes.Excel;
                    break;

            }
            FileType = ft;
            Category = DatasourceCategory.FILE;
            
         
           
            Reader = new TxtXlsCSVReader(Dataconnection.ConnectionProp.FileName, Logger, DMEEditor, FileType, per, Dataconnection.ConnectionProp.FilePath, null);
            ConnectionStatus = Reader.OpenConnection();
            Dataconnection.ConnectionStatus = ConnectionStatus;
            if (ConnectionStatus == ConnectionState.Open)
            {
               // Entities = Reader.Entities;
                EntitiesNames = Reader.Entities.Select(o => o.EntityName).ToList();
            }
           // GetEntitesList();
            
          
        }
        public List<string> GetEntitesList()
        {
            ErrorObject.Flag = Errors.Ok;

            try
            {
               // List<EntityStructure> entlist = new List<EntityStructure>();
                EntitiesNames = new List<string>();
                EntitiesNames = Reader.getWorksheetNames().ToList();
                

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
        public EntityStructure GetEntityDataType(string EntityName)
        {

            return Entities.Where(x => x.EntityName == EntityName).FirstOrDefault();
        }
        public Type GetEntityType(string EntityName)
        {
            string filenamenoext = EntityName;
            DMTypeBuilder.CreateNewObject(EntityName, EntityName, Entities.Where(x=>x.EntityName== EntityName).FirstOrDefault().Fields);
            return DMTypeBuilder.myType;
        }
        public async Task<object> GetEntityDataAsync(string entityname, string filterstr)
        {

            ErrorObject.Flag = Errors.Ok;
            try
            {
                return await Task.Run(() => Reader.ReadList(0, HeaderExist, 0, 0));
              
              

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
                return  Reader.ReadDataTable(EntityName, HeaderExist, 0, 0);



            }
            catch (Exception ex)
            {
                
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
                Logger.WriteLog($"Error in getting File Data ({ex.Message}) ");
                return null;
            }
           // return Records;
        }
        public IErrorsInfo UpdateEntities(string EntityName, object UploadData)
        {
            ErrorObject.Flag = Errors.Ok;

            try
            {



                //Entities = new List<string>();
                //Entities.Add(Fileconnection.FileName);



                Logger.WriteLog("Successfully Retrieve Entites list ");

            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Unsuccessfully Retrieve Entites list {ex.Message}");
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
            }

            return ErrorObject;
        }
     
        public async Task<List<object>> GetSampleData(bool HeaderExist)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {


                SampleLines = await Task.Run(() => Reader.ReadList(0, HeaderExist, 0,100));
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
                Logger.WriteLog($"Error in getting Sample Data  ({ex.Message}) ");
            }
            return SampleLines;
        }
        public DataTable GetSampleDataTable(bool HeaderExist)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {


                SourceEntityData = Reader.ReadDataTable(0,HeaderExist, 0, 100);
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
                Logger.WriteLog($"Error in getting Sample Data  ({ex.Message}) ");
            }
            return SourceEntityData;
        }
        public DataTable GetEntityDataTable(string EntityName, string filterstr)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {

                SourceEntityData = Reader.ReadDataTable(0, HeaderExist, fromline, toline);

            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
                Logger.WriteLog($"Error in getting File Data ({ex.Message}) ");
            }
            return SourceEntityData;

        }

        public List<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            return null;
        }

        public DataSet GetChildTablesListFromCustomQuery(string tablename, string customquery)
        {
            throw new NotImplementedException();
        }

        public List<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo ExecuteSql(string sql)
        {
            throw new NotImplementedException();
        }

        public bool CreateEntityAs(EntityStructure entity)
        {
            throw new NotImplementedException();
        }

        public bool CheckEntityExist(string EntityName)
        {
            bool retval=false;
            if (Reader.Entities != null)
            {
                if (Reader.Entities.Where(x => string.Equals(x.EntityName, EntityName,StringComparison.OrdinalIgnoreCase)).Count() > 0)
                {
                    retval = true;
                }
                else
                    retval = false;

            }

            return retval;
        }

        public EntityStructure GetEntityStructure(string EntityName,bool refresh=false )
        {
            if (ConnectionStatus == ConnectionState.Open)
            {
                if (refresh || Reader.Entities.Count() == 0)
                {
                    Reader.GetEntityStructures( refresh);
                    DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new ConfigUtil.DatasourceEntities { datasourcename = DatasourceName, Entities = Reader.Entities });
                }

            }
       
           return Reader.Entities.Where(x => string.Equals(x.EntityName, EntityName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

        }

        public IDataReader GetDataReader(string querystring)
        {
            throw new NotImplementedException();
        }

        public  DataTable RunQuery( string qrystr)
        {
            throw new NotImplementedException();
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
            if (ConnectionStatus == ConnectionState.Open)
            {
                if (refresh || Reader.Entities.Count() == 0)
                {
                    Reader.GetEntityStructures(refresh);
                }
            }
            return Reader.Entities.Where(x => x.EntityName == fnd.EntityName).FirstOrDefault();
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
