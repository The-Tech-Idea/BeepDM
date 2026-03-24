using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Proxy.Remote
{
    /// <summary>
    /// HTTP/JSON implementation of <see cref="IProxyTransport"/>.
    ///
    /// Sends requests to <c>{baseUrl}/proxy/execute</c> as JSON POST and maps
    /// the JSON response back to <see cref="ProxyRemoteResponse"/>.
    ///
    /// <para>
    /// <strong>Security:</strong> pass a pre-configured <see cref="HttpClient"/> with
    /// mTLS, bearer token, or an API-key header already set in its default headers.
    /// Never log the raw request body — it may contain query results.
    /// </para>
    ///
    /// <para>
    /// <strong>Connection reuse:</strong> pass a singleton <see cref="HttpClient"/>
    /// (or one managed by <c>IHttpClientFactory</c>) from the outside.
    /// If the parameterless path is used, an internal client is created and disposed
    /// with this instance.
    /// </para>
    /// </summary>
    public sealed class HttpProxyTransport : IProxyTransport
    {
        private readonly HttpClient _http;
        private readonly bool       _ownsClient;
        private readonly string     _executeUrl;
        private readonly string     _pingUrl;
        private readonly byte[]?    _hmacSecret;   // null = signing disabled
        private bool                _disposed;

        private static readonly JsonSerializerOptions _json = new JsonSerializerOptions
        {
            PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented               = false,
            DefaultIgnoreCondition      = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        /// <inheritdoc/>
        public string NodeAddress { get; }

        // ── Constructors ─────────────────────────────────────────────────────

        /// <summary>
        /// Creates a transport that manages its own <see cref="HttpClient"/>.
        /// </summary>
        /// <param name="baseUrl">
        /// Base URL of the remote worker, e.g. <c>http://worker-b:5100</c>.
        /// Do <em>not</em> include a trailing slash.
        /// </param>
        /// <param name="timeout">Per-request timeout. Defaults to 30 s.</param>
        /// <param name="apiKey">
        /// Optional API key sent as <c>X-Proxy-Api-Key</c> header.
        /// Must be treated as a secret — never log it.
        /// </param>
        /// <param name="hmacSecret">
        /// Optional shared HMAC secret (UTF-8 encoded) used to sign every request.
        /// The worker must be configured with the same secret in
        /// <see cref="ProxyRemoteRequestDispatcher"/>.
        /// When omitted, request signing is disabled (not recommended for production).
        /// The secret must be at least 32 characters long.
        /// </param>
        public HttpProxyTransport(
            string  baseUrl,
            TimeSpan? timeout    = null,
            string? apiKey       = null,
            string? hmacSecret   = null)
            : this(baseUrl, CreateOwnedClient(timeout ?? TimeSpan.FromSeconds(30), apiKey),
                   ownsClient: true,
                   hmacSecret: hmacSecret is null ? null : Encoding.UTF8.GetBytes(hmacSecret))
        { }

        /// <summary>
        /// Creates a transport backed by the supplied <paramref name="httpClient"/>.
        /// The client is <em>not</em> disposed when this transport is disposed.
        /// </summary>
        public HttpProxyTransport(string baseUrl, HttpClient httpClient, bool ownsClient = false,
            byte[]? hmacSecret = null)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new ArgumentException("baseUrl must not be empty.", nameof(baseUrl));

            NodeAddress  = baseUrl;
            _http        = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _ownsClient  = ownsClient;
            _hmacSecret  = hmacSecret;
            _executeUrl  = baseUrl.TrimEnd('/') + "/proxy/execute";
            _pingUrl     = baseUrl.TrimEnd('/') + "/proxy/ping";
        }

        // ── Factory: build from ConnectionProperties ──────────────────────────

        /// <summary>
        /// Creates an <see cref="HttpProxyTransport"/> from a node's
        /// <see cref="ConnectionProperties"/> as stored in
        /// <c>ConfigEditor.DataConnections</c>.
        ///
        /// <para>
        /// Reads: <c>Url</c> (base URL), <c>ApiKey</c> (X-Proxy-Api-Key header),
        /// <c>Timeout</c> (seconds), and <c>ParameterList["HmacSecret"]</c>
        /// (signing secret — treat as sensitive, never log).
        /// </para>
        /// </summary>
        /// <param name="config">
        /// A <see cref="ConnectionProperties"/> record whose <c>IsRemote == true</c>
        /// and <c>DriverName == "BeepProxyNode"</c>.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown when <c>config.Url</c> is null or whitespace.
        /// </exception>
        public static HttpProxyTransport FromConnectionProperties(ConnectionProperties config)
        {
            if (config is null) throw new ArgumentNullException(nameof(config));
            if (string.IsNullOrWhiteSpace(config.Url))
                throw new ArgumentException(
                    "ConnectionProperties.Url must contain the worker base URL.", nameof(config));

            var timeout = config.Timeout > 0
                ? TimeSpan.FromSeconds(config.Timeout)
                : TimeSpan.FromSeconds(30);

            // Read HMAC secret from ParameterList — never store in plain ApiKey field
            config.ParameterList.TryGetValue("HmacSecret", out var hmacSecret);

            return new HttpProxyTransport(
                baseUrl   : config.Url,
                timeout   : timeout,
                apiKey    : config.ApiKey,
                hmacSecret: hmacSecret);
        }

        // ── IProxyTransport ──────────────────────────────────────────────────

        /// <inheritdoc/>
        public async Task<ProxyRemoteResponse> SendAsync(
            ProxyRemoteRequest request,
            CancellationToken ct = default)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(HttpProxyTransport));
            if (request is null) throw new ArgumentNullException(nameof(request));

            // Sign the request when a shared secret is configured
            if (_hmacSecret != null)
                ProxyRequestSigner.Sign(request, _hmacSecret);

            string json = JsonSerializer.Serialize(request, _json);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage httpResp;
            try
            {
                httpResp = await _http.PostAsync(_executeUrl, content, ct).ConfigureAwait(false);
            }
            catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
            {
                // HttpClient timeout fired — treat as a transient transport failure
                throw new TimeoutException(
                    $"[HttpProxyTransport] Request to {NodeAddress} timed out " +
                    $"(operation={request.Operation}, correlationId={request.CorrelationId}).", ex);
            }

            string body = await httpResp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            if (!httpResp.IsSuccessStatusCode)
            {
                return ProxyRemoteResponse.Fail(
                    request.CorrelationId,
                    $"Worker returned HTTP {(int)httpResp.StatusCode}: {body}",
                    (int)httpResp.StatusCode);
            }

            try
            {
                var resp = JsonSerializer.Deserialize<ProxyRemoteResponse>(body, _json);
                return resp ?? ProxyRemoteResponse.Fail(request.CorrelationId, "Empty response body.");
            }
            catch (JsonException jex)
            {
                return ProxyRemoteResponse.Fail(request.CorrelationId,
                    $"Failed to parse worker response: {jex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<bool> PingAsync(CancellationToken ct = default)
        {
            if (_disposed) return false;
            try
            {
                var resp = await _http.GetAsync(_pingUrl, ct).ConfigureAwait(false);
                return resp.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        // ── IDisposable ──────────────────────────────────────────────────────

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_ownsClient) _http.Dispose();
        }

        // ── Private helpers ──────────────────────────────────────────────────

        private static HttpClient CreateOwnedClient(TimeSpan timeout, string? apiKey)
        {
            var handler = new SocketsHttpHandler
            {
                PooledConnectionLifetime  = TimeSpan.FromMinutes(5),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
                MaxConnectionsPerServer   = 50,
                EnableMultipleHttp2Connections = true,
                ConnectTimeout            = TimeSpan.FromSeconds(5)
            };

            var client = new HttpClient(handler, disposeHandler: true)
            {
                Timeout = timeout
            };

            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            if (!string.IsNullOrEmpty(apiKey))
                client.DefaultRequestHeaders.Add("X-Proxy-Api-Key", apiKey);

            return client;
        }
    }
}
