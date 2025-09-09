using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Utilities; // Errors enum

namespace TheTechIdea.Beep.WebAPI
{
    /// <summary>
    /// HTTP convenience methods for common Web API calls (GET, etc.)
    /// </summary>
    public partial class WebAPIDataSource
    {
        /// <summary>
        /// Sends a GET request to the specified endpoint or absolute URL.
        /// - If endpointOrUrl starts with http, it is treated as an absolute URL.
        /// - Otherwise it is combined with BaseUrl.
        /// Default headers and auth headers are applied automatically.
        /// </summary>
        /// <param name="endpointOrUrl">Relative endpoint or absolute URL</param>
        /// <param name="query">Optional query parameters to append</param>
        /// <param name="headers">Optional extra headers</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>HttpResponseMessage or null if request failed</returns>
        public async Task<HttpResponseMessage> GetAsync(
            string endpointOrUrl,
            Dictionary<string, string> query = null,
            Dictionary<string, string> headers = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Build final URL
                string url = BuildUrlWithQuery(endpointOrUrl, query);

                var request = new HttpRequestMessage(HttpMethod.Get, url);

                // Default headers from configuration
                var defaultHeaders = _configHelper.GetHeaders();
                foreach (var header in defaultHeaders)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                // Additional caller headers
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }

                // Ensure auth and add auth headers
                await _authHelper.EnsureAuthenticatedAsync().ConfigureAwait(false);
                _authHelper.AddAuthenticationHeaders(request);

                // Send with retry using helper
                var response = await _requestHelper.SendWithRetryAsync(request, "GET").ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    await _errorHelper.HandleErrorResponseAsync(response).ConfigureAwait(false);
                }

                return response;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"GetAsync error: {ex.Message}");
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                return null;
            }
        }

        /// <summary>
        /// Sends a GET request and deserializes the JSON response to type T.
        /// Returns default(T) if the request fails or content cannot be parsed.
        /// </summary>
        public async Task<T> GetAsync<T>(
            string endpointOrUrl,
            Dictionary<string, string> query = null,
            Dictionary<string, string> headers = null,
            CancellationToken cancellationToken = default)
        {
            var response = await GetAsync(endpointOrUrl, query, headers, cancellationToken).ConfigureAwait(false);
            if (response == null || !response.IsSuccessStatusCode)
                return default;

            try
            {
                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var obj = JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return obj;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"GetAsync<T> parse error: {ex.Message}");
                return default;
            }
        }

        private string BuildUrlWithQuery(string endpointOrUrl, Dictionary<string, string> query)
        {
            string url;
            if (endpointOrUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                url = endpointOrUrl;
            }
            else
            {
                // Treat as relative endpoint
                url = _dataHelper.BuildEndpointUrl(_configHelper.BaseUrl, endpointOrUrl);
            }

            if (query != null && query.Count > 0)
            {
                var sb = new StringBuilder(url);
                sb.Append(url.Contains("?") ? "&" : "?");
                bool first = true;
                foreach (var kv in query)
                {
                    if (!first) sb.Append('&');
                    sb.Append(Uri.EscapeDataString(kv.Key));
                    sb.Append('=');
                    sb.Append(Uri.EscapeDataString(kv.Value ?? string.Empty));
                    first = false;
                }
                url = sb.ToString();
            }

            return url;
        }
    }
}
