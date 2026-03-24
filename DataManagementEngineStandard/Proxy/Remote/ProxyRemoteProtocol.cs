using System;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Proxy.Remote
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Operation name constants — shared by client and server
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// String constants that identify each remoted operation in the wire protocol.
    /// Both <see cref="RemoteProxyDataSource"/> (client) and
    /// <see cref="ProxyRemoteRequestDispatcher"/> (server) reference these.
    /// </summary>
    public static class ProxyRemoteOperations
    {
        public const string Ping                = "ping";
        public const string Openconnection      = "Openconnection";
        public const string Closeconnection     = "Closeconnection";
        public const string GetEntity           = "GetEntity";
        public const string InsertRecord        = "InsertRecord";
        public const string UpdateRecord        = "UpdateRecord";
        public const string DeleteRecord        = "DeleteRecord";
        public const string RunQuery            = "RunQuery";
        public const string ExecuteSQL          = "ExecuteSQL";
        public const string BeginTransaction    = "BeginTransaction";
        public const string EndTransaction      = "EndTransaction";
        public const string Commit              = "Commit";
        public const string GetEntitiesNames    = "GetEntitiesNames";
        public const string GetEntityStructure  = "GetEntityStructure";
        public const string ApplyPolicy         = "ApplyPolicy";
        public const string GetMetrics          = "GetMetrics";
        public const string GetSloSnapshot      = "GetSloSnapshot";
        public const string AddDataSource       = "AddDataSource";
        public const string RemoveDataSource    = "RemoveDataSource";
        public const string SetRole             = "SetRole";
        public const string ExecuteWithLB       = "ExecuteWithLB";   // generic fan-out
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  ProxyRemoteRequest — outbound (client → server)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Serializable envelope sent by <see cref="RemoteProxyDataSource"/> over
    /// the transport to the worker's <see cref="ProxyRemoteRequestDispatcher"/>.
    /// All complex payload fields are JSON-encoded strings to keep the DTO
    /// fully serializable without coupling to domain type assemblies on the
    /// transport tier.
    /// </summary>
    public sealed class ProxyRemoteRequest
    {
        /// <summary>Unique per-request correlation ID for distributed tracing.</summary>
        public string CorrelationId     { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 12);

        /// <summary>One of the <see cref="ProxyRemoteOperations"/> constants.</summary>
        public string Operation         { get; set; }

        // ── Entity / record ───────────────────────────────────────────────

        /// <summary>Target entity/table name.</summary>
        public string EntityName        { get; set; }

        /// <summary>JSON-serialized <c>List&lt;AppFilter&gt;</c>.</summary>
        public string FiltersJson       { get; set; }

        /// <summary>JSON-serialized record object (insert/update/delete payload).</summary>
        public string RecordJson        { get; set; }

        /// <summary>CLR type hint so the server can deserialize <see cref="RecordJson"/> correctly.</summary>
        public string RecordTypeHint    { get; set; }

        // ── SQL ───────────────────────────────────────────────────────────

        /// <summary>Raw SQL string for RunQuery / ExecuteSQL operations.</summary>
        public string QuerySql          { get; set; }

        /// <summary>JSON-serialized <c>List&lt;object&gt;</c> SQL parameter values.</summary>
        public string SqlArgsJson       { get; set; }

        // ── Policy / config ───────────────────────────────────────────────

        /// <summary>JSON-serialized <see cref="ProxyPolicy"/> for ApplyPolicy.</summary>
        public string PolicyJson        { get; set; }

        /// <summary>JSON-serialized <see cref="PassedArgs"/> for transaction operations.</summary>
        public string PassedArgsJson    { get; set; }

        // ── Datasource membership ──────────────────────────────────────────

        /// <summary>Datasource name for AddDataSource / RemoveDataSource / SetRole.</summary>
        public string DatasourceName    { get; set; }

        /// <summary>Routing weight for AddDataSource.</summary>
        public int    Weight            { get; set; } = 1;

        /// <summary>String representation of <see cref="ProxyDataSourceRole"/> for SetRole.</summary>
        public string RoleStr           { get; set; }

        // ── Metadata ──────────────────────────────────────────────────────

        /// <summary>When <c>true</c>, forces a metadata refresh in GetEntityStructure.</summary>
        public bool RefreshMetadata     { get; set; }

        /// <summary>SLO snapshot datasource name for GetSloSnapshot.</summary>
        public string SloTargetDs       { get; set; }

        // ── Paging ────────────────────────────────────────────────────────

        /// <summary>1-based page number for paged GetEntity calls.</summary>
        public int PageNumber           { get; set; }

        /// <summary>Page size for paged GetEntity calls.</summary>
        public int PageSize             { get; set; }

        // ── Schema / filter extras ────────────────────────────────────────

        /// <summary>Schema name for GetChildTablesList / GetEntityforeignkeys.</summary>
        public string SchemaName        { get; set; }

        /// <summary>Raw filter string for GetChildTablesList.</summary>
        public string FilterStr         { get; set; }

        // ── Security ──────────────────────────────────────────────────────

        /// <summary>
        /// UTC timestamp when this request was created.
        /// Workers reject requests where <c>|UtcNow − RequestTimestamp| > 5 minutes</c>
        /// to prevent replay attacks.
        /// Set automatically by <see cref="HttpProxyTransport"/> via
        /// <see cref="ProxyRequestSigner.Sign"/> when an HMAC secret is configured.
        /// </summary>
        public DateTimeOffset RequestTimestamp  { get; set; }

        /// <summary>
        /// HMAC-SHA256 signature of the canonical message
        /// <c>{CorrelationId}|{Operation}|{RequestTimestamp:O}</c> keyed with
        /// the shared <c>hmacSecret</c>.
        /// Left <c>null</c> when no secret is configured (insecure mode).
        /// </summary>
        public string? Signature               { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  ProxyRemoteResponse — inbound (server → client)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Serializable response envelope returned by the worker machine.
    /// All complex payloads are JSON-encoded strings for the same
    /// transport-tier isolation reason as <see cref="ProxyRemoteRequest"/>.
    /// </summary>
    public sealed class ProxyRemoteResponse
    {
        /// <summary>Mirrors the request's CorrelationId for end-to-end tracing.</summary>
        public string CorrelationId    { get; set; }

        /// <summary><c>true</c> when the operation succeeded at the worker.</summary>
        public bool   Success          { get; set; }

        /// <summary>JSON-serialized result object (GetEntity → List&lt;T&gt;, GetMetrics → dict, etc.).</summary>
        public string DataJson         { get; set; }

        /// <summary>CLR fully-qualified type name of the value in <see cref="DataJson"/>.</summary>
        public string TypeHint         { get; set; }

        /// <summary>JSON-serialized IErrorsInfo payload for write operations.</summary>
        public string ErrorsInfoJson   { get; set; }

        /// <summary>Human-readable error detail when <see cref="Success"/> is <c>false</c>.</summary>
        public string ErrorMessage     { get; set; }

        /// <summary>HTTP-style status code: 200 = OK, 429 = rate-limited, 503 = unavailable.</summary>
        public int    StatusCode       { get; set; } = 200;

        /// <summary>Worker-measured execution time in milliseconds.</summary>
        public long   ElapsedMs        { get; set; }

        // ── Forwarded metrics (lightweight piggyback) ─────────────────────

        /// <summary>Total requests processed by this worker since startup.</summary>
        public long   WorkerTotalRequests { get; set; }

        /// <summary>Total failed requests on this worker since startup.</summary>
        public long   WorkerFailedRequests { get; set; }

        /// <summary>Rolling average latency on this worker (ms).</summary>
        public double WorkerAvgLatencyMs  { get; set; }

        // ── Factory helpers ───────────────────────────────────────────────

        internal static ProxyRemoteResponse Ok(string correlationId, string dataJson,
            string typeHint, long elapsedMs)
            => new ProxyRemoteResponse
            {
                CorrelationId = correlationId,
                Success       = true,
                DataJson      = dataJson,
                TypeHint      = typeHint,
                ElapsedMs     = elapsedMs,
                StatusCode    = 200
            };

        internal static ProxyRemoteResponse Fail(string correlationId,
            string message, int statusCode = 500)
            => new ProxyRemoteResponse
            {
                CorrelationId = correlationId,
                Success       = false,
                ErrorMessage  = message,
                StatusCode    = statusCode
            };
    }
}
