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

        // Additional IWebAPIDataSource methods

        public async Task<HttpResponseMessage> PostAsync(
            string endpointOrUrl,
            object body = null,
            Dictionary<string, string> query = null,
            Dictionary<string, string> headers = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var url = BuildUrlWithQuery(endpointOrUrl, query);
                var request = new HttpRequestMessage(HttpMethod.Post, url);

                if (body != null)
                {
                    var json = JsonSerializer.Serialize(body);
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                }

                var defaultHeaders = _configHelper.GetHeaders();
                foreach (var header in defaultHeaders)
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);

                if (headers != null)
                    foreach (var header in headers)
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value);

                await _authHelper.EnsureAuthenticatedAsync().ConfigureAwait(false);
                _authHelper.AddAuthenticationHeaders(request);

                var response = await _requestHelper.SendWithRetryAsync(request, "POST").ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    await _errorHelper.HandleErrorResponseAsync(response).ConfigureAwait(false);

                return response;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"PostAsync error: {ex.Message}");
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                return null;
            }
        }

        public async Task<T> PostAsync<T>(
            string endpointOrUrl,
            object body = null,
            Dictionary<string, string> query = null,
            Dictionary<string, string> headers = null,
            CancellationToken cancellationToken = default)
        {
            var response = await PostAsync(endpointOrUrl, body, query, headers, cancellationToken).ConfigureAwait(false);
            if (response == null || !response.IsSuccessStatusCode)
                return default;
            try
            {
                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"PostAsync<T> parse error: {ex.Message}");
                return default;
            }
        }

        public async Task<HttpResponseMessage> PutAsync(
            string endpointOrUrl,
            object body = null,
            Dictionary<string, string> query = null,
            Dictionary<string, string> headers = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var url = BuildUrlWithQuery(endpointOrUrl, query);
                var request = new HttpRequestMessage(HttpMethod.Put, url);
                if (body != null)
                {
                    var json = JsonSerializer.Serialize(body);
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                }

                var defaultHeaders = _configHelper.GetHeaders();
                foreach (var header in defaultHeaders)
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                if (headers != null)
                    foreach (var header in headers)
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value);

                await _authHelper.EnsureAuthenticatedAsync().ConfigureAwait(false);
                _authHelper.AddAuthenticationHeaders(request);

                var response = await _requestHelper.SendWithRetryAsync(request, "PUT").ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    await _errorHelper.HandleErrorResponseAsync(response).ConfigureAwait(false);
                return response;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"PutAsync error: {ex.Message}");
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                return null;
            }
        }

        public async Task<T> PutAsync<T>(
            string endpointOrUrl,
            object body = null,
            Dictionary<string, string> query = null,
            Dictionary<string, string> headers = null,
            CancellationToken cancellationToken = default)
        {
            var response = await PutAsync(endpointOrUrl, body, query, headers, cancellationToken).ConfigureAwait(false);
            if (response == null || !response.IsSuccessStatusCode)
                return default;
            try
            {
                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"PutAsync<T> parse error: {ex.Message}");
                return default;
            }
        }

        public async Task<HttpResponseMessage> PatchAsync(
            string endpointOrUrl,
            object body = null,
            Dictionary<string, string> query = null,
            Dictionary<string, string> headers = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var url = BuildUrlWithQuery(endpointOrUrl, query);
                var method = new HttpMethod("PATCH");
                var request = new HttpRequestMessage(method, url);
                if (body != null)
                {
                    var json = JsonSerializer.Serialize(body);
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                }

                var defaultHeaders = _configHelper.GetHeaders();
                foreach (var header in defaultHeaders)
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                if (headers != null)
                    foreach (var header in headers)
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value);

                await _authHelper.EnsureAuthenticatedAsync().ConfigureAwait(false);
                _authHelper.AddAuthenticationHeaders(request);

                var response = await _requestHelper.SendWithRetryAsync(request, "PATCH").ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    await _errorHelper.HandleErrorResponseAsync(response).ConfigureAwait(false);
                return response;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"PatchAsync error: {ex.Message}");
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                return null;
            }
        }

        public async Task<T> PatchAsync<T>(
            string endpointOrUrl,
            object body = null,
            Dictionary<string, string> query = null,
            Dictionary<string, string> headers = null,
            CancellationToken cancellationToken = default)
        {
            var response = await PatchAsync(endpointOrUrl, body, query, headers, cancellationToken).ConfigureAwait(false);
            if (response == null || !response.IsSuccessStatusCode)
                return default;
            try
            {
                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"PatchAsync<T> parse error: {ex.Message}");
                return default;
            }
        }

        public async Task<HttpResponseMessage> DeleteAsync(
            string endpointOrUrl,
            object body = null,
            Dictionary<string, string> query = null,
            Dictionary<string, string> headers = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var url = BuildUrlWithQuery(endpointOrUrl, query);
                var request = new HttpRequestMessage(HttpMethod.Delete, url);
                if (body != null)
                {
                    var json = JsonSerializer.Serialize(body);
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                }

                var defaultHeaders = _configHelper.GetHeaders();
                foreach (var header in defaultHeaders)
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                if (headers != null)
                    foreach (var header in headers)
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value);

                await _authHelper.EnsureAuthenticatedAsync().ConfigureAwait(false);
                _authHelper.AddAuthenticationHeaders(request);

                var response = await _requestHelper.SendWithRetryAsync(request, "DELETE").ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    await _errorHelper.HandleErrorResponseAsync(response).ConfigureAwait(false);
                return response;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"DeleteAsync error: {ex.Message}");
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                return null;
            }
        }

        public async Task<T> DeleteAsync<T>(
            string endpointOrUrl,
            object body = null,
            Dictionary<string, string> query = null,
            Dictionary<string, string> headers = null,
            CancellationToken cancellationToken = default)
        {
            var response = await DeleteAsync(endpointOrUrl, body, query, headers, cancellationToken).ConfigureAwait(false);
            if (response == null || !response.IsSuccessStatusCode)
                return default;
            try
            {
                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"DeleteAsync<T> parse error: {ex.Message}");
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
