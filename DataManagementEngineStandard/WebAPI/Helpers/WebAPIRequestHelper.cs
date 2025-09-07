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

        public WebAPIRequestHelper(HttpClient httpClient, IDMLogger logger, string dataSourceName, int maxConcurrentRequests = 10, int retryCount = 3, int retryDelayMs = 1000)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger;
            _dataSourceName = dataSourceName ?? "WebAPI";
            _retryCount = retryCount;
            _retryDelayMs = retryDelayMs;
            _rateLimitSemaphore = new SemaphoreSlim(maxConcurrentRequests, maxConcurrentRequests);
        }

        public async Task<HttpResponseMessage> SendWithRetryAsync(HttpRequestMessage request, string operationName = null)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                await _rateLimitSemaphore.WaitAsync();
                try
                {
                    // Clone the request for potential retries
                    var clonedRequest = await CloneHttpRequestMessageAsync(request);
                    var response = await _httpClient.SendAsync(clonedRequest);
                    
                    if (response.IsSuccessStatusCode)
                        return response;
                    
                    // Handle specific HTTP status codes
                    await HandleHttpErrorResponse(response);
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

        private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string operationName)
        {
            Exception lastException = null;

            for (int attempt = 0; attempt <= _retryCount; attempt++)
            {
                try
                {
                    return await operation();
                }
                catch (Exception ex)
                {
                    lastException = ex;

                    if (attempt < _retryCount && ShouldRetry(ex))
                    {
                        var delay = CalculateDelay(attempt);
                        _logger?.WriteLog($"Attempt {attempt + 1} failed for {operationName}, retrying in {delay}ms: {ex.Message}");
                        await Task.Delay(delay);
                        continue;
                    }

                    _logger?.WriteLog($"Operation {operationName} failed after {attempt + 1} attempts: {ex.Message}");
                    throw;
                }
            }

            throw lastException ?? new InvalidOperationException("Unexpected error in retry logic");
        }

        private bool ShouldRetry(Exception ex)
        {
            // Retry on network errors, timeouts, and 5xx server errors
            if (ex is HttpRequestException || ex is TaskCanceledException)
                return true;

            if (ex is WebException webEx)
            {
                return webEx.Status == WebExceptionStatus.Timeout ||
                       webEx.Status == WebExceptionStatus.ConnectionClosed ||
                       webEx.Status == WebExceptionStatus.ConnectFailure ||
                       webEx.Status == WebExceptionStatus.ReceiveFailure;
            }

            return false;
        }

        private int CalculateDelay(int attemptNumber)
        {
            // Exponential backoff with jitter
            var delay = _retryDelayMs * Math.Pow(2, attemptNumber);
            var jitter = new Random().Next(0, (int)(delay * 0.1)); // Add up to 10% jitter
            return (int)(delay + jitter);
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

        private async Task HandleHttpErrorResponse(HttpResponseMessage response)
        {
            var statusCode = (int)response.StatusCode;
            var reasonPhrase = response.ReasonPhrase;
            var content = string.Empty;

            try
            {
                content = await response.Content.ReadAsStringAsync();
            }
            catch
            {
                // Ignore content read errors
            }

            var errorMessage = $"HTTP {statusCode} {reasonPhrase}";
            if (!string.IsNullOrEmpty(content))
            {
                errorMessage += $": {content}";
            }

            _logger?.WriteLog($"HTTP Error Response: {errorMessage}");

            // Handle specific status codes
            switch (response.StatusCode)
            {
                case HttpStatusCode.Unauthorized:
                    throw new UnauthorizedAccessException($"Authentication failed: {errorMessage}");
                
                case HttpStatusCode.Forbidden:
                    throw new UnauthorizedAccessException($"Access forbidden: {errorMessage}");
                
                case HttpStatusCode.NotFound:
                    throw new ArgumentException($"Resource not found: {errorMessage}");
                
                case HttpStatusCode.TooManyRequests:
                    // Handle rate limiting
                    var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(60);
                    throw new InvalidOperationException($"Rate limit exceeded, retry after {retryAfter.TotalSeconds} seconds");
                
                case HttpStatusCode.BadRequest:
                    throw new ArgumentException($"Bad request: {errorMessage}");
                
                default:
                    if (statusCode >= 500)
                    {
                        throw new HttpRequestException($"Server error: {errorMessage}");
                    }
                    break;
            }
        }

        public void Dispose()
        {
            _rateLimitSemaphore?.Dispose();
        }
    }
}
