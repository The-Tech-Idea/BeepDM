using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.ConfigUtil;
using System.Net.Http;

namespace TheTechIdea.Beep.WebAPI.Helpers
{
    public interface IWebAPISchemaHelper : IDisposable
    {
        Task<List<EntityStructure>> DiscoverEntitiesAsync(HttpClient client, ConnectionProperties properties);
        Task<EntityStructure> InferEntityStructureAsync(string entityName, HttpClient client, ConnectionProperties properties);
        Type GetEntityType(string entityName);
        bool ValidateEntityStructure(EntityStructure entity);
        Task<bool> EntityExistsAsync(string entityName);
    }

    public interface IWebAPIDataHelper : IDisposable
    {
        Type CreateDynamicType(string entityName);
        object ParseJsonToSingleObject(string json);
        List<object> ParseJsonToObjects(string json);
        string TransformToJsonString(object data);
        DataValidationResult ValidateData(object data, string entityName);
    }

    public class DataValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
}
