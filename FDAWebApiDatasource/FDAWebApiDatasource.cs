using DataManagmentEngineShared.WebAPI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.DataManagment_Engine.ConfigUtil;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.Editor;
using TheTechIdea.DataManagment_Engine.Report;
using TheTechIdea.DataManagment_Engine.Workflow;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.WebAPI.FDAWebApi
{
    [ClassProperties(Category = DatasourceCategory.WEBAPI, DatasourceType = DataSourceType.WebService)]
    public class FDAWebApiDatasource : IDataSource
    {
        public event EventHandler<PassedArgs> PassEvent;
        public HttpClient client { get; set; } = new HttpClient();

        WebAPIDataConnection cn;
        public FDAWebApiDatasource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per)
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
        public ConnectionState Openconnection()
        {
            throw new NotImplementedException();
        }

        public ConnectionState Closeconnection()
        {
            throw new NotImplementedException();
        }
        public int GetEntityIdx(string entityName)
        {
            if (Entities.Count > 0)
            {
                return Entities.FindIndex(p => p.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase) || p.DatasourceEntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                return -1;
            }


        }
        public DataSourceType DatasourceType { get ; set ; }
        public DatasourceCategory Category { get ; set ; }
        public IDataConnection Dataconnection { get ; set ; }
        public string DatasourceName { get ; set ; }
        public IErrorsInfo ErrorObject { get ; set ; }
        public string Id { get ; set ; }
        public IDMLogger Logger { get ; set ; }
        public List<string> EntitiesNames { get ; set ; }
        public List<EntityStructure> Entities { get ; set ; }
        public IDMEEditor DMEEditor { get ; set ; }
        public ConnectionState ConnectionStatus { get ; set ; }

        public bool CheckEntityExist(string EntityName)
        {
            return Dataconnection.ConnectionProp.Entities.Where(o => o.EntityName.Equals(EntityName, StringComparison.OrdinalIgnoreCase)).Any();
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
            try
            {

                //---------------Get Categories---------------- -

                if (Entities.Count == 0)

                {
                    DatasourceEntities f = DMEEditor.ConfigEditor.LoadDataSourceEntitiesValues(DatasourceName);
                    if (f != null)
                    {
                        Entities = f.Entities;
                    }


                }
                if (Entities == null || Entities.Count() == 0)
                {

                    string retval = GetCategoriesDataAsync("GetCategories", @"/category/?api_key=@apikey&category_id=371").Result;
                    var data = JObject.Parse(retval);
                    var categorytk = data.SelectToken("category");


                   // Category categorys = JsonConvert.DeserializeObject<Category>(categorytk.ToString());

                    int i = 1;
                    //foreach (var item in categorys.childcategories)
                    //{
                    //    EntityStructure x = new EntityStructure();
                    //    x.ParentId = 0;
                    //    x.Id = i;
                    //    i += 1;
                    //    x.EntityName = item.name;
                    //    x.Viewtype = ViewType.Url;
                    //    x.Category = "WEBAPI";
                    //    x.DataSourceID = DatasourceName;
                    //    x.CustomBuildQuery = $"/category/?api_key=7535ABF7E83252CB08E4300380DFA02D&category_id={item.category_id}";
                    //    Entities.Add(x);
                    //    //   retval = GetCategoriesDataAsync("GetCategories", x.CustomBuildQuery).Result;
                    //    var ou = GetCategoriesDataAsync("GetCategories", x.CustomBuildQuery);
                    //    ou.Wait();
                    //    data = JObject.Parse(ou.Result);
                    //    categorytk = data.SelectToken("category");
                        //Category ct = JsonConvert.DeserializeObject<Category>(categorytk.ToString());
                        //foreach (Childcategory cc in ct.childcategories)
                        //{
                        //    createentities(cc, x);
                        //}


                    //}

                }
                EntitiesNames.Clear();
                foreach (EntityStructure item in Entities)
                {
                    item.CustomBuildQuery = item.CustomBuildQuery.Replace("api_key=7535ABF7E83252CB08E4300380DFA02D", "api_key=@apikey");
                    EntitiesNames.Add(item.EntityName);

                }
                DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new DatasourceEntities { datasourcename = DatasourceName, Entities = Entities });
                return EntitiesNames;

            }
            catch (Exception ex)
            {
                DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new DatasourceEntities { datasourcename = DatasourceName, Entities = Entities });
                return null;
            }
        }
        //private void createentities(Childcategory item, EntityStructure entity)
        //{
        //    EntityStructure x = new EntityStructure();
        //    x.Id = Entities.Max(u => u.Id) + 1;
        //    x.ParentId = entity.Id;
        //    x.EntityName = item.name;
        //    x.Viewtype = ViewType.Url;
        //    x.Category = "WEBAPI";
        //    x.DataSourceID = DatasourceName;
        //    x.CustomBuildQuery = $"/category/?api_key=7535ABF7E83252CB08E4300380DFA02D&category_id={item.category_id}";
        //    Entities.Add(x);
        //    var retval = GetCategoriesDataAsync("GetCategories", x.CustomBuildQuery);
        //    retval.Wait();

        //    var data = JObject.Parse(retval.Result);
        //    var categorytk = data.SelectToken("category");
        //    Category categorys = JsonConvert.DeserializeObject<Category>(categorytk.ToString());

        //    //if (categorys.childseries.Count() > 0)
        //    //{
        //    //    foreach (Childsery childs in categorys.childseries)
        //    //    {
        //    //        x = new EntityStructure();
        //    //        x.Id = Entities.Max(u => u.Id) + 1;
        //    //        x.ParentId = entity.Id;
        //    //        x.EntityName = childs.name;
        //    //        x.Viewtype = ViewType.Url;
        //    //        x.CustomBuildQuery = $"/category/?api_key=7535ABF7E83252CB08E4300380DFA02D&series_id={childs.series_id}";
        //    //        Entities.Add(x);
        //    //    }
        //    //  ;
        //    //}
        //    if (categorys.childcategories.Count() > 0)
        //    {
        //        foreach (Childcategory ct in categorys.childcategories)
        //        {
        //            createentities(ct, x);
        //        }


        //    }
        //}
        private async Task<string> GetCategoriesDataAsync(string EntityName, string filterstr)
        {
            var request = new HttpRequestMessage();
            client = new HttpClient();
            client.BaseAddress = new Uri(Dataconnection.ConnectionProp.Url);

            EntityStructure ent = Dataconnection.ConnectionProp.Entities.Where(o => o.EntityName == EntityName).FirstOrDefault();
            if (!string.IsNullOrEmpty(Dataconnection.ConnectionProp.ApiKey))
            {
                Dataconnection.ConnectionProp.Url = Dataconnection.ConnectionProp.Url.Replace("@apikey", Dataconnection.ConnectionProp.ApiKey);
            }
            if (!string.IsNullOrEmpty(filterstr))
            {
                filterstr = filterstr.Replace("@apikey", Dataconnection.ConnectionProp.ApiKey);
            }
            request.Method = HttpMethod.Get;
            request.RequestUri = new Uri(Dataconnection.ConnectionProp.Url + filterstr);
            foreach (WebApiHeader item in Dataconnection.ConnectionProp.Headers)
            {
                request.Headers.Add(item.headername, item.headervalue);
            }
            using (var response = await client.SendAsync(request).ConfigureAwait(false))
            {
                //    response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                string retval = body;
                //if (!string.IsNullOrEmpty(ent.KeyToken) || !string.IsNullOrWhiteSpace(ent.KeyToken))
                //{
                //    var data = JObject.Parse(body);
                //    var location = data.SelectToken("category");
                //    var categories = location.SelectToken(ent.KeyToken);
                //    retval = categories.ToString();
                //}

                return retval;
            }
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
            client = new HttpClient();
            client.BaseAddress = new Uri(Dataconnection.ConnectionProp.Url);

           
            if (!string.IsNullOrEmpty(Dataconnection.ConnectionProp.ApiKey))
            {
                Dataconnection.ConnectionProp.Url = Dataconnection.ConnectionProp.Url.Replace("@apikey", Dataconnection.ConnectionProp.ApiKey);
            }
            if (!string.IsNullOrEmpty(filterstr))
            {
                filterstr = filterstr.Replace("@apikey", Dataconnection.ConnectionProp.ApiKey);
            }
            request.Method = HttpMethod.Get;
            request.RequestUri = new Uri(Dataconnection.ConnectionProp.Url + filterstr);
            foreach (WebApiHeader item in Dataconnection.ConnectionProp.Headers)
            {
                request.Headers.Add(item.headername, item.headervalue);
            }
            using (var response = await client.SendAsync(request).ConfigureAwait(false))
            {
                //    response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                string retval = body;
                if (!string.IsNullOrEmpty(ent.KeyToken) || !string.IsNullOrWhiteSpace(ent.KeyToken))
                {
                    var data = JObject.Parse(body);
                    var location = data.SelectToken("category");
                    var categories = location.SelectToken(ent.KeyToken);
                    retval = categories.ToString();
                }
                var y = DMEEditor.ConfigEditor.JsonLoader.DeserializeObjectString<List<dynamic>>(retval);
                return y;
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
            return Dataconnection.ConnectionProp.Entities.Where(o => o.EntityName.Equals(EntityName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

        }

        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            return Dataconnection.ConnectionProp.Entities.Where(o => o.EntityName.Equals(fnd.EntityName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
        }

        public Type GetEntityType(string EntityName)
        {
            EntityStructure x = GetEntityStructure(EntityName, false);
            DMTypeBuilder.CreateNewObject(EntityName, EntityName, x.Fields);
            return DMTypeBuilder.myType;
        }
        public  object RunQuery( string qrystr)
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
        public IErrorsInfo RunScript(LScript dDLScripts)
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

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
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
