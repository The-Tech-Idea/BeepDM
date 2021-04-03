using DataManagmentEngineShared.WebAPI;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.Editor;
using TheTechIdea.DataManagment_Engine.WebAPI;
using TheTechIdea.DataManagment_Engine.Workflow;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.WebAPI.GeoDBCitiesWebAPI
{
    public class GeoDBCitiesWebAPIDatasource : WebAPIDataSource
    {
        
        public GeoDBCitiesWebAPIDatasource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per) :base( datasourcename,  logger,  pDMEEditor,  databasetype,  per)
        {
           
            Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.FileName == datasourcename).FirstOrDefault();
        
        }
        public override async Task<object> GetEntityDataAsync(string entityname, string filterstr)
        {
            client = new HttpClient();
            client.BaseAddress = new Uri(Dataconnection.ConnectionProp.Url);
            var request = new HttpRequestMessage();
            EntityStructure ent = Entities.Where(o => o.EntityName == entityname).FirstOrDefault();
            request.Method = HttpMethod.Get;
            request.RequestUri = new Uri(Dataconnection.ConnectionProp.Url+filterstr);
            foreach (WebApiHeader item in Dataconnection.ConnectionProp.Headers)
            {
                request.Headers.Add(item.headername, item.headervalue);
            }

            //string retval = SendAsync(request).Result;

            using (var response = await client.SendAsync(request).ConfigureAwait(false))
            {
                //    response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                string retval = body;
                if (!string.IsNullOrEmpty(ent.KeyToken) || !string.IsNullOrWhiteSpace(ent.KeyToken))

                {
                    var data = JObject.Parse(body);
                    var location = data.SelectToken(ent.KeyToken);
                    retval = location.ToString();

                }
                var y = DMEEditor.ConfigEditor.JsonLoader.DeserializeObjectString<dynamic>(retval);
                return y;
            }


        }
        public LScript RunScript(LScript dDLScripts)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            throw new NotImplementedException();
        }
        public List<LScript> CreateDataSourceScript(List<string> entities = null)
        {
            throw new NotImplementedException();
        }


    }

}
//var client = new HttpClient();
//var request = new HttpRequestMessage
//{
//    Method = HttpMethod.Get,
//    RequestUri = new Uri("https://wft-geo-db.p.rapidapi.com/v1/geo/cities"),
//    Headers =
//    {
//        { "x-rapidapi-key", "84b5bcda19msh7385da521fe4381p1ead49jsn2b5e01f2271e" },
//        { "x-rapidapi-host", "wft-geo-db.p.rapidapi.com" },
//    },
//};
//using (var response = await client.SendAsync(request))
//{
//    response.EnsureSuccessStatusCode();
//    var body = await response.Content.ReadAsStringAsync();
//    Console.WriteLine(body);
//}