using DataManagmentEngineShared.WebAPI;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.Editor;
using TheTechIdea.DataManagment_Engine.Report;
using TheTechIdea.DataManagment_Engine.Workflow;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.WebAPI.CountriesRest
{
   public class CountriesRestWebApiDatasource : IDataSource

    {
        public event EventHandler<PassedArgs> PassEvent;
        public HttpClient client { get; set; } = new HttpClient();

        WebAPIDataConnection cn;
        public CountriesRestWebApiDatasource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per) 
        {

            DatasourceName = datasourcename;
            Logger = logger;
            ErrorObject = per;
            DMEEditor = pDMEEditor;
            DatasourceType = databasetype;
            Category = DatasourceCategory.WEBAPI;
            Dataconnection = new WebAPIDataConnection
            {
                Logger = logger,
                ErrorObject = ErrorObject

            };
            client = new HttpClient();
            Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.ConnectionName == datasourcename).FirstOrDefault();
            cn = (WebAPIDataConnection)Dataconnection;
            cn.OpenConnection();
            cn.ConnectionStatus = ConnectionStatus;

        }

        public DataSourceType DatasourceType { get ; set ; }
        public DatasourceCategory Category { get ; set ; }
        public IDataConnection Dataconnection { get ; set ; }
        public string DatasourceName { get ; set ; }
        public IErrorsInfo ErrorObject { get ; set ; }
        public string Id { get ; set ; }
        public IDMLogger Logger { get ; set ; }
        public List<string> EntitiesNames { get; set; } = new List<string>();
        public List<EntityStructure> Entities { get; set; } = new List<EntityStructure>();
        public IDMEEditor DMEEditor { get ; set ; }
        public ConnectionState ConnectionStatus { get ; set ; }

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

        public List<string> GetEntitesList()
        {
            throw new NotImplementedException();
        }

        public async Task<object> GetEntityAsync(string EntityName, List<ReportFilter> Filter)
        {


            EntityStructure ent = Dataconnection.ConnectionProp.Entities.Where(o => o.EntityName == EntityName).FirstOrDefault();
            string filterstr = ent.CustomBuildQuery;
            foreach (EntityParameters item in ent.Paramenters)
            {
                filterstr = filterstr.Replace("{" + item.parameterIndex + "}", ent.Filters.Where(u => u.FieldName == item.parameterName).Select(p => p.FilterValue).FirstOrDefault());
            }

            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Get;
            request.RequestUri = new Uri(Dataconnection.ConnectionProp.Url +  filterstr);
            foreach (WebApiHeader item in Dataconnection.ConnectionProp.Headers)
            {
                request.Headers.Add(item.headername, item.headervalue);
            }

            if (!string.IsNullOrEmpty(Dataconnection.ConnectionProp.ApiKey))
            {
                Dataconnection.ConnectionProp.Url = Dataconnection.ConnectionProp.Url.Replace("@apikey", Dataconnection.ConnectionProp.ApiKey);
            }
            if (!string.IsNullOrEmpty(filterstr))
            {
                filterstr = filterstr.Replace("@apikey", Dataconnection.ConnectionProp.ApiKey);
            }
            try
            {
              //  var url = String.Format("{0}{1}", Dataconnection.ConnectionProp.Url, filterstr);
                client = new HttpClient();
                client.BaseAddress = new Uri(Dataconnection.ConnectionProp.Url);
                var response = await client.GetAsync(filterstr).ConfigureAwait(false);
                string body= await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var x = DMEEditor.ConfigEditor.JsonLoader.DeserializeObjectString<Country>(body);
                return x;

            }
            catch (Exception )
            {
                return null;
            }

        }


        public object GetEntity(string EntityName, List<ReportFilter> filter)
        {
            throw new NotImplementedException();
        }

        public List<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            throw new NotImplementedException();
        }

        public EntityStructure GetEntityStructure(string EntityName, bool refresh)
        {
            throw new NotImplementedException();
        }

        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            throw new NotImplementedException();
        }

        public Type GetEntityType(string EntityName)
        {
            throw new NotImplementedException();
        }

        public DataTable RunQuery(string qrystr)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IMapping_rep Mapping = null)
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

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            throw new NotImplementedException();
        }
    }
}
