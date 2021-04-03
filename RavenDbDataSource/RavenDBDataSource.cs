
using DataManagmentEngineShared.NOSQL;
using DataManagmentEngineShared.WebAPI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.Editor;
using TheTechIdea.DataManagment_Engine.Workflow;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.NOSQL.RavenDB
{
    public class RavenDBDataSource :  IDataSource
    {
        public event EventHandler<PassedArgs> PassEvent;
        public BindingList<string> Databases { get ; set ; }
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
        public RavenDBReader Reader { get; set; }
        public string CurrentDatabase { get; set; }

        public RavenDBDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per)
        {
            DatasourceName = datasourcename;
            Logger = logger;
            ErrorObject = per;
            DMEEditor = pDMEEditor;
            DatasourceType = databasetype;
            Category = DatasourceCategory.NOSQL;
            Dataconnection = new WebAPIDataConnection
            {
                Logger = logger,
                ErrorObject = ErrorObject

            };
            Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.ConnectionName == datasourcename).FirstOrDefault();

            Reader = new RavenDBReader(Dataconnection.ConnectionProp.ConnectionName, Dataconnection.ConnectionProp.Database, DMEEditor,Dataconnection, null);
            Dataconnection.ConnectionStatus = Reader.State;
            ConnectionStatus = Reader.State;
            CurrentDatabase = Dataconnection.ConnectionProp.Database;
            if (CurrentDatabase != null)
            {
                if (CurrentDatabase.Length > 0)
                {
                    GetEntitesList();
                }
            }
          


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

        public List<string> GetEntitesList()
        {

            try
            {
                //  List<EntityStructure> entlist = new List<EntityStructure>();
                EntitiesNames = new List<string>();
                EntitiesNames = Reader.GetEntities().ToList();



                //  DMEEditor.ConfigEditor.DataConnections.Where(c => c.FileName == DatasourceName).FirstOrDefault().Entities =entlist ;
                Logger.WriteLog("Successfully Retrieve Entites list ");

            }
            catch (Exception ex)
            {
                string mes = "";
                DMEEditor.AddLogMessage(ex.Message, "Could get entities List" + mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return EntitiesNames;
        }

        public Task<object> GetEntityDataAsync(string entityname, string filterstr)
        {
            throw new NotImplementedException();
        }

        public DataTable GetEntity(string EntityName, string filterstr)
        {
            throw new NotImplementedException();
        }

        public List<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            throw new NotImplementedException();
        }

        public EntityStructure GetEntityStructure(string EntityName, bool refresh = false)
        {
            return Reader.GetEntityStructure(EntityName);
        }

        public DataTable GetEntityDataTable(string EntityName, string filterstr)
        {
            throw new NotImplementedException();
        }

        public Type GetEntityType(string EntityName)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IMapping_rep Mapping)
        {
            throw new NotImplementedException();
        }

        public  DataTable RunQuery( string qrystr)
        {
            throw new NotImplementedException();
        }
        public virtual IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow, IMapping_rep Mapping = null)
        {


            throw new NotImplementedException();
        }
        public IErrorsInfo DeleteEntity(string EntityName, object DeletedDataRow, IMapping_rep Mapping = null)
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
    }
}
