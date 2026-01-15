using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.JsonLoaderService
{
    /// <summary>
    /// Enhanced interface for JSON loading and saving operations
    /// </summary>
    public interface IEnhancedJsonLoader : IJsonLoader
    {
        // Async methods
        Task<List<T>> DeserializeObjectAsync<T>(string filename);
        Task<T> DeserializeSingleObjectAsync<T>(string filename);
        Task SerializeAsync(string filename, object obj);

        // Enhanced serialization methods
        string SerializeObject(object obj, JsonSerializerSettings settings);
        string SerializeObject(object obj, Formatting formatting);

        // Validation methods
        bool IsValidJson(string jsonString);
    }
}