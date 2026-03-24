using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Proxy.Remote
{
    /// <summary>
    /// Server-side dispatcher: receives a <see cref="ProxyRemoteRequest"/>,
    /// routes it to the appropriate method on the local <see cref="IDMEEditor"/>-managed
    /// <see cref="IDataSource"/> (or <see cref="IProxyDataSource"/>), and returns a
    /// <see cref="ProxyRemoteResponse"/>.
    ///
    /// <para>
    /// This class has <strong>no dependency on ASP.NET Core</strong>.
    /// Wire it into any hosting framework:
    /// </para>
    ///
    /// <code>
    /// // ASP.NET Core Minimal API — Program.cs on Worker Machine
    /// var dispatcher = new ProxyRemoteRequestDispatcher(editor, "MyWorkerDS");
    ///
    /// app.MapPost("/proxy/execute", async (HttpContext ctx) =>
    /// {
    ///     var req  = await ctx.Request.ReadFromJsonAsync&lt;ProxyRemoteRequest&gt;();
    ///     var resp = await dispatcher.DispatchAsync(req, ctx.RequestAborted);
    ///     await ctx.Response.WriteAsJsonAsync(resp);
    /// });
    ///
    /// app.MapGet("/proxy/ping", () => Results.Ok("pong"));
    /// </code>
    ///
    /// <para>
    /// The <paramref name="datasourceName"/> is the name under which the
    /// backing datasource (or <see cref="ProxyDataSource"/>) is registered
    /// in <c>ConfigEditor</c>.  When <c>null</c>, the dispatcher looks up
    /// the primary datasource from the request payload.
    /// </para>
    /// </summary>
    public sealed class ProxyRemoteRequestDispatcher
    {
        private readonly IDMEEditor _editor;
        private readonly string?    _defaultDsName;
        private readonly byte[]?    _hmacSecret;    // null = signature verification disabled

        private static readonly JsonSerializerOptions _json = new JsonSerializerOptions
        {
            PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented               = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        // ── Constructor ──────────────────────────────────────────────────────

        /// <param name="editor">Local DMEEditor that resolves the backing IDataSource.</param>
        /// <param name="defaultDatasourceName">
        /// Optional default datasource name used when the request does not specify one.
        /// Typically the name registered via <c>ConfigEditor.AddDataConnection</c>.
        /// </param>
        /// <param name="hmacSecret">
        /// Optional shared HMAC secret (UTF-8 encoded) used to verify every inbound
        /// request.  Must match the secret configured in <see cref="HttpProxyTransport"/>
        /// on the coordinator side.
        ///
        /// <para>
        /// When set, requests with a missing, invalid, or expired signature are rejected
        /// with HTTP-equivalent error code 401.  This protects against unauthenticated
        /// callers and replay attacks.
        /// </para>
        ///
        /// <para>
        /// Use <c>null</c> (default) only for local-loopback testing — never in production.
        /// </para>
        /// </param>
        public ProxyRemoteRequestDispatcher(
            IDMEEditor editor,
            string?    defaultDatasourceName = null,
            string?    hmacSecret            = null)
        {
            _editor        = editor ?? throw new ArgumentNullException(nameof(editor));
            _defaultDsName = defaultDatasourceName;
            _hmacSecret    = hmacSecret is null ? null : System.Text.Encoding.UTF8.GetBytes(hmacSecret);
        }

        // ── Entry point ──────────────────────────────────────────────────────

        /// <summary>
        /// Dispatches the <paramref name="request"/> to the appropriate
        /// <see cref="IDataSource"/> / <see cref="IProxyDataSource"/> method
        /// and returns the wrapped result.
        /// </summary>
        public async Task<ProxyRemoteResponse> DispatchAsync(
            ProxyRemoteRequest request,
            CancellationToken  ct = default)
        {
            if (request is null)
                return ProxyRemoteResponse.Fail("(null)", "Null request envelope.");

            // ── Security: verify HMAC signature and replay-attack protection ─────
            if (_hmacSecret != null)
            {
                var sigResult = ProxyRequestSigner.Verify(request, _hmacSecret);
                if (sigResult != SignatureVerificationResult.Valid)
                {
                    _editor.AddLogMessage("ProxyDispatcher",
                        $"Request rejected: {sigResult} (op={request.Operation}, correlationId={request.CorrelationId})",
                        DateTime.Now, 0, null, Errors.Failed);
                    return ProxyRemoteResponse.Fail(request.CorrelationId,
                        $"Unauthorized: {sigResult}.", 401);
                }
            }
            // ── End security check ───────────────────────────────────────────────

            var sw = Stopwatch.StartNew();
            try
            {
                return await RouteAsync(request, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage("ProxyDispatcher",
                    $"Unhandled error dispatching '{request.Operation}': {ex.Message}",
                    DateTime.Now, 0, null, Errors.Failed);

                return ProxyRemoteResponse.Fail(request.CorrelationId, ex.Message);
            }
            finally
            {
                sw.Stop();
            }
        }

        // ── Routing table ────────────────────────────────────────────────────

        private async Task<ProxyRemoteResponse> RouteAsync(
            ProxyRemoteRequest request,
            CancellationToken  ct)
        {
            if (request.Operation == ProxyRemoteOperations.Ping
                || request.Operation == "ping")
                return ProxyRemoteResponse.Ok(request.CorrelationId, "\"pong\"", "System.String", 0);

            var sw = Stopwatch.StartNew();
            var ds = ResolveDataSource(request);

            if (ds is null)
                return ProxyRemoteResponse.Fail(request.CorrelationId,
                    $"DataSource '{request.DatasourceName ?? _defaultDsName}' not found.", 503);

            ProxyRemoteResponse resp;

            switch (request.Operation)
            {
                // ── Connection ──────────────────────────────────────────────
                case ProxyRemoteOperations.Openconnection:
                    resp = await Task.Run(() =>
                    {
                        var state = ds.Openconnection();
                        return OkJson(request, state.ToString(), sw);
                    }, ct);
                    break;

                case ProxyRemoteOperations.Closeconnection:
                    resp = await Task.Run(() =>
                    {
                        var state = ds.Closeconnection();
                        return OkJson(request, state.ToString(), sw);
                    }, ct);
                    break;

                // ── Reads ───────────────────────────────────────────────────
                case ProxyRemoteOperations.GetEntity:
                {
                    var filters = DeserializeFilters(request.FiltersJson);
                    resp = await Task.Run(() =>
                    {
                        var data = ds.GetEntity(request.EntityName, filters);
                        return OkJson(request, Serialize(data), sw, data?.GetType().AssemblyQualifiedName);
                    }, ct);
                    break;
                }

                case ProxyRemoteOperations.RunQuery:
                    resp = await Task.Run(() =>
                    {
                        var data = ds.RunQuery(request.QuerySql);
                        return OkJson(request, Serialize(data), sw, data?.GetType().AssemblyQualifiedName);
                    }, ct);
                    break;

                case ProxyRemoteOperations.ExecuteSQL:
                    resp = await Task.Run(() =>
                    {
                        var ei = ds.ExecuteSql(request.QuerySql);
                        return ErrorsInfoResponse(request, ei, sw);
                    }, ct);
                    break;

                // ── Writes ──────────────────────────────────────────────────
                case ProxyRemoteOperations.InsertRecord:
                {
                    var record = DeserializeRecord(request.RecordJson, request.RecordTypeHint);
                    resp = await Task.Run(() =>
                    {
                        var ei = ds.InsertEntity(request.EntityName, record);
                        return ErrorsInfoResponse(request, ei, sw);
                    }, ct);
                    break;
                }

                case ProxyRemoteOperations.UpdateRecord:
                {
                    var record = DeserializeRecord(request.RecordJson, request.RecordTypeHint);
                    resp = await Task.Run(() =>
                    {
                        var ei = ds.UpdateEntity(request.EntityName, record);
                        return ErrorsInfoResponse(request, ei, sw);
                    }, ct);
                    break;
                }

                case ProxyRemoteOperations.DeleteRecord:
                {
                    var record = DeserializeRecord(request.RecordJson, request.RecordTypeHint);
                    resp = await Task.Run(() =>
                    {
                        var ei = ds.DeleteEntity(request.EntityName, record);
                        return ErrorsInfoResponse(request, ei, sw);
                    }, ct);
                    break;
                }

                // ── Transactions ────────────────────────────────────────────
                case ProxyRemoteOperations.BeginTransaction:
                case ProxyRemoteOperations.EndTransaction:
                case ProxyRemoteOperations.Commit:
                {
                    var pargs = DeserializePassedArgs(request.PassedArgsJson);
                    resp = await Task.Run(() =>
                    {
                        IErrorsInfo ei = request.Operation switch
                        {
                            ProxyRemoteOperations.BeginTransaction => ds.BeginTransaction(pargs),
                            ProxyRemoteOperations.EndTransaction   => ds.EndTransaction(pargs),
                            _                                      => ds.Commit(pargs)
                        };
                        return ErrorsInfoResponse(request, ei, sw);
                    }, ct);
                    break;
                }

                // ── Metadata ────────────────────────────────────────────────
                case ProxyRemoteOperations.GetEntitiesNames:
                    resp = await Task.Run(() =>
                    {
                        var names = ds.GetEntitesList();
                        return OkJson(request, Serialize(names), sw);
                    }, ct);
                    break;

                case ProxyRemoteOperations.GetEntityStructure:
                    resp = await Task.Run(() =>
                    {
                        var structure = ds.GetEntityStructure(request.EntityName, request.RefreshMetadata);
                        return OkJson(request, Serialize(structure), sw);
                    }, ct);
                    break;

                // ── IProxyDataSource: policy ────────────────────────────────
                case ProxyRemoteOperations.ApplyPolicy:
                {
                    if (ds is IProxyDataSource pds && !string.IsNullOrEmpty(request.PolicyJson))
                    {
                        var policy = JsonSerializer.Deserialize<ProxyPolicy>(request.PolicyJson, _json);
                        if (policy != null) pds.ApplyPolicy(policy);
                    }
                    resp = OkJson(request, "true", sw);
                    break;
                }

                // ── IProxyDataSource: metrics ───────────────────────────────
                case ProxyRemoteOperations.GetMetrics:
                {
                    if (ds is IProxyDataSource pds)
                    {
                        var metrics = pds.GetMetrics();
                        resp = OkJson(request, Serialize(metrics), sw);
                    }
                    else
                    {
                        resp = OkJson(request, "{}", sw);
                    }
                    break;
                }

                case ProxyRemoteOperations.GetSloSnapshot:
                {
                    if (ds is IProxyDataSource pds)
                    {
                        object snapshot = string.IsNullOrEmpty(request.SloTargetDs)
                            ? (object)pds.GetAllSloSnapshots()
                            : pds.GetSloSnapshot(request.SloTargetDs);
                        resp = OkJson(request, Serialize(snapshot), sw,
                            snapshot?.GetType().AssemblyQualifiedName);
                    }
                    else
                    {
                        resp = OkJson(request, "[]", sw);
                    }
                    break;
                }

                // ── IProxyDataSource: membership ────────────────────────────
                case ProxyRemoteOperations.AddDataSource:
                    if (ds is IProxyDataSource addPds)
                        addPds.AddDataSource(request.DatasourceName, request.Weight);
                    resp = OkJson(request, "true", sw);
                    break;

                case ProxyRemoteOperations.RemoveDataSource:
                    if (ds is IProxyDataSource remPds)
                        remPds.RemoveDataSource(request.DatasourceName);
                    resp = OkJson(request, "true", sw);
                    break;

                case ProxyRemoteOperations.SetRole:
                    if (ds is IProxyDataSource rolePds
                        && Enum.TryParse<ProxyDataSourceRole>(request.RoleStr, out var role))
                        rolePds.SetRole(request.DatasourceName, role);
                    resp = OkJson(request, "true", sw);
                    break;

                case "InvalidateCache":
                    if (ds is IProxyDataSource cachePds)
                        cachePds.InvalidateCache(request.EntityName);
                    resp = OkJson(request, "true", sw);
                    break;

                case "CheckEntityExist":
                case "CheckEntityExistance":
                    resp = await Task.Run(() =>
                    {
                        bool exists = ds.CheckEntityExist(request.EntityName);
                        return OkJson(request, exists ? "true" : "false", sw);
                    }, ct);
                    break;

                default:
                    resp = ProxyRemoteResponse.Fail(request.CorrelationId,
                        $"Unknown operation: '{request.Operation}'.");
                    break;
            }

            resp.CorrelationId = request.CorrelationId;
            return resp;
        }

        // ── Datasource resolution ────────────────────────────────────────────

        private IDataSource ResolveDataSource(ProxyRemoteRequest request)
        {
            string name = request.DatasourceName ?? _defaultDsName;
            if (string.IsNullOrEmpty(name)) return null;

            var ds = _editor.GetDataSource(name);
            if (ds == null)
                _editor.OpenDataSource(name);

            return _editor.GetDataSource(name);
        }

        // ── Response helpers ─────────────────────────────────────────────────

        private static ProxyRemoteResponse OkJson(
            ProxyRemoteRequest request,
            string             dataJson,
            Stopwatch          sw,
            string?            typeHint = null)
            => new ProxyRemoteResponse
            {
                CorrelationId = request.CorrelationId,
                Success       = true,
                DataJson      = dataJson,
                TypeHint      = typeHint,
                ElapsedMs     = sw.ElapsedMilliseconds,
                StatusCode    = 200
            };

        private static ProxyRemoteResponse ErrorsInfoResponse(
            ProxyRemoteRequest request,
            IErrorsInfo        ei,
            Stopwatch          sw)
        {
            bool ok = ei?.Flag == Errors.Ok;
            return new ProxyRemoteResponse
            {
                CorrelationId  = request.CorrelationId,
                Success        = ok,
                ErrorMessage   = ok ? null : ei?.Message,
                ErrorsInfoJson = Serialize(ei),
                ElapsedMs      = sw.ElapsedMilliseconds,
                StatusCode     = ok ? 200 : 500
            };
        }

        // ── Deserialization helpers ──────────────────────────────────────────

        private static List<AppFilter> DeserializeFilters(string? json)
        {
            if (string.IsNullOrEmpty(json)) return new List<AppFilter>();
            return JsonSerializer.Deserialize<List<AppFilter>>(json, _json) ?? new List<AppFilter>();
        }

        private static List<object>? DeserializeArgs(string? json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            return JsonSerializer.Deserialize<List<object>>(json, _json);
        }

        private static object? DeserializeRecord(string? json, string? typeHint)
        {
            if (string.IsNullOrEmpty(json)) return null;
            if (!string.IsNullOrEmpty(typeHint))
            {
                var t = Type.GetType(typeHint, throwOnError: false);
                if (t != null) return JsonSerializer.Deserialize(json, t, _json);
            }
            return JsonSerializer.Deserialize<object>(json, _json);
        }

        private static PassedArgs? DeserializePassedArgs(string? json)
        {
            if (string.IsNullOrEmpty(json)) return new PassedArgs();
            return JsonSerializer.Deserialize<PassedArgs>(json, _json) ?? new PassedArgs();
        }

        private static string Serialize(object? obj)
        {
            if (obj is null) return "null";
            return JsonSerializer.Serialize(obj, _json);
        }
    }
}
