using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.WebAPI
{
    public interface IWebAPIDataSource : IDataSource
    {
       
        List<EntityField> Fields { get; set; }
        string          ApiKey { get; set; }
        string          Resource { get; set; }
        Dictionary<string,string> Parameters { get; set; }
        Task<List<object>> ReadData(bool HeaderExist, int fromline = 0, int toline = 100);

         Task<HttpResponseMessage> GetAsync(
         string endpointOrUrl,
         Dictionary<string, string> query = null,
         Dictionary<string, string> headers = null,
         CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a GET request and deserializes the JSON response to type T.
        /// Returns default(T) if the request fails or content cannot be parsed.
        /// </summary>
        Task<T> GetAsync<T>(
            string endpointOrUrl,
            Dictionary<string, string> query = null,
            Dictionary<string, string> headers = null,
            CancellationToken cancellationToken = default);

        // New generic HTTP helpers
        Task<HttpResponseMessage> PostAsync(
            string endpointOrUrl,
            object body = null,
            Dictionary<string, string> query = null,
            Dictionary<string, string> headers = null,
            CancellationToken cancellationToken = default);

        Task<T> PostAsync<T>(
            string endpointOrUrl,
            object body = null,
            Dictionary<string, string> query = null,
            Dictionary<string, string> headers = null,
            CancellationToken cancellationToken = default);

        Task<HttpResponseMessage> PutAsync(
            string endpointOrUrl,
            object body = null,
            Dictionary<string, string> query = null,
            Dictionary<string, string> headers = null,
            CancellationToken cancellationToken = default);

        Task<T> PutAsync<T>(
            string endpointOrUrl,
            object body = null,
            Dictionary<string, string> query = null,
            Dictionary<string, string> headers = null,
            CancellationToken cancellationToken = default);

        Task<HttpResponseMessage> PatchAsync(
            string endpointOrUrl,
            object body = null,
            Dictionary<string, string> query = null,
            Dictionary<string, string> headers = null,
            CancellationToken cancellationToken = default);

        Task<T> PatchAsync<T>(
            string endpointOrUrl,
            object body = null,
            Dictionary<string, string> query = null,
            Dictionary<string, string> headers = null,
            CancellationToken cancellationToken = default);

        Task<HttpResponseMessage> DeleteAsync(
            string endpointOrUrl,
            object body = null,
            Dictionary<string, string> query = null,
            Dictionary<string, string> headers = null,
            CancellationToken cancellationToken = default);

        Task<T> DeleteAsync<T>(
            string endpointOrUrl,
            object body = null,
            Dictionary<string, string> query = null,
            Dictionary<string, string> headers = null,
            CancellationToken cancellationToken = default);
      
    }
}
