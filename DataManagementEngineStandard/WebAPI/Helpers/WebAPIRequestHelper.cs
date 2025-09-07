using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using System.Net;

namespace TheTechIdea.Beep.WebAPI.Helpers
{
    /// <summary>
    /// HTTP request helper with retry logic, rate limiting, and error handling
    /// </summary>
    public class WebAPIRequestHelper : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly IDMLogger _logger;
        private readonly SemaphoreSlim _rateLimitSemaphore;
        private readonly int _retryCount;
        private readonly int _retryDelayMs;
        private readonly string _dataSourceName;
        private readonly WebAPIErrorHelper _errorHelper;

        public WebAPIRequestHelper(HttpClient httpClient, IDMLogger logger, string dataSourceName, WebAPIErrorHelper errorHelper, int maxConcurrentRequests = 10, int retryCount = 3, int retryDelayMs = 1000)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger;
            _dataSourceName = dataSourceName ?? "WebAPI";
            _errorHelper = errorHelper ?? throw new ArgumentNullException(nameof(errorHelper));
            _retryCount = retryCount;
            _retryDelayMs = retryDelayMs;
            _rateLimitSemaphore = new SemaphoreSlim(maxConcurrentRequests, maxConcurrentRequests);
        }

        public async Task<HttpResponseMessage> SendWithRetryAsync(HttpRequestMessage request, string operationName = null)
        {
            return await _errorHelper.ExecuteWithRetryAsync(async () =>
            {
                await _rateLimitSemaphore.WaitAsync();
                try
                {
                    // Clone the request for potential retries
                    var clonedRequest = await CloneHttpRequestMessageAsync(request);
                    var response = await _httpClient.SendAsync(clonedRequest);
                    
                    if (response.IsSuccessStatusCode)
                        return response;
                    
                    // Use error helper for comprehensive error handling
                    await _errorHelper.HandleErrorResponseAsync(response);
                    return response;
                }
                finally
                {
                    _rateLimitSemaphore.Release();
                }
            }, operationName ?? "HTTP Request");
        }

        public async Task<T> SendWithRetryAsync<T>(HttpRequestMessage request, Func<HttpResponseMessage, Task<T>> responseHandler, string operationName = null)
        {
            var response = await SendWithRetryAsync(request, operationName);
            return await responseHandler(response);
        }

        private async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage original)
        {
            var clone = new HttpRequestMessage(original.Method, original.RequestUri)
            {
                Version = original.Version
            };

            // Copy headers
            foreach (var header in original.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            // Copy content if present
            if (original.Content != null)
            {
                var originalContent = await original.Content.ReadAsByteArrayAsync();
                clone.Content = new ByteArrayContent(originalContent);

                // Copy content headers
                foreach (var header in original.Content.Headers)
                {
                    clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            return clone;
        }

        public void Dispose()
        {
            _rateLimitSemaphore?.Dispose();
        }
    }
}
