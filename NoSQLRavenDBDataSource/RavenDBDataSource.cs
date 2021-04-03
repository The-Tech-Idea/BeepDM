
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
using TheTechIdea.DataManagment_Engine.Workflow;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.NOSQL.RavenDB
{
    public class RavenDBDataSource :  IDataSource
    {
        public BindingList<string> Databases { get ; set ; }
        public DataSourceType DatasourceType { get ; set ; }
        public DatasourceCategory Category { get ; set ; }
        public IDataConnection Dataconnection { get ; set ; }
        public string DatasourceName { get ; set ; }
        public IErrorsInfo ErrorObject { get ; set ; }
        public string Id { get ; set ; }
        public IDMLogger Logger { get ; set ; }
        public List<string> Entities { get ; set ; }
        public IDMEEditor DME_Editor { get ; set ; }
        public List<object> Records { get ; set ; }
        public ConnectionState ConnectionStatus { get ; set ; }
        public DataTable SourceEntityData { get ; set ; }
        public RavenDBReader Reader { get; set; }
        public string CurrentDatabase { get; set; }

        public RavenDBDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDME_Editor, DataSourceType databasetype, IErrorsInfo per)
        {
            DatasourceName = datasourcename;
            Logger = logger;
            ErrorObject = per;
            DME_Editor = pDME_Editor;
            DatasourceType = databasetype;
            Category = DatasourceCategory.NOSQL;
            Dataconnection = new WebAPIDataConnection
            {
                Logger = logger,
                ErrorObject = ErrorObject

            };
            Dataconnection.ConnectionProp = DME_Editor.ConfigEditor.DataConnections.Where(c => c.ConnectionName == datasourcename).FirstOrDefault();

            Reader = new RavenDBReader(Dataconnection.ConnectionProp.ConnectionName, DME_Editor,Dataconnection, null);
            Dataconnection.ConnectionStatus = Reader.State;
            ConnectionStatus = Reader.State;
            CurrentDatabase = Dataconnection.ConnectionProp.Database;
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

        public DataSet GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
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
                Entities = new List<string>();
                Entities = Reader.GetEntities(CurrentDatabase).ToList();



                //  DME_Editor.ConfigEditor.DataConnections.Where(c => c.FileName == DatasourceName).FirstOrDefault().Entities =entlist ;
                Logger.WriteLog("Successfully Retrieve Entites list ");

            }
            catch (Exception ex)
            {
                string mes = "";
                DME_Editor.AddLogMessage(ex.Message, "Could not Open Store in RavenDB " + mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return Entities;
        }

        public Task<List<object>> GetEntityDataAsListAsync(string EntityName, string filterstr)
        {
            throw new NotImplementedException();
        }

        public Task<DataTable> GetEntityDataAsTableAsync(string EntityName, string filterstr)
        {
            throw new NotImplementedException();
        }

        public List<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            throw new NotImplementedException();
        }

        public EntityStructure GetEntityStructure(string EntityName)
        {
            return Reader.GetEntityStructure(EntityName, CurrentDatabase);
        }

        public DataTable GetEntityTable(string EntityName, string filterstr)
        {
            throw new NotImplementedException();
        }

        public Type GetEntityType(string EntityName)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo LoadDataToEntity(string EntityName, DataTable UploadData, IMapping_rep Mapping)
        {
            throw new NotImplementedException();
        }

        public DataTable RunQuery(string qrystr)
        {
            throw new NotImplementedException();
        }
    }
}
