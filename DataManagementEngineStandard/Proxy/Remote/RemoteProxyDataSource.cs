using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Addin;

namespace TheTechIdea.Beep.Proxy.Remote
{
    /// <summary>
    /// An <see cref="IProxyDataSource"/> that routes every operation to a remote
    /// worker machine via an <see cref="IProxyTransport"/>.
    ///
    /// <para>
    /// Drop this into a <see cref="ProxyNode"/> inside a <see cref="ProxyCluster"/>
    /// to make the cluster topology span multiple machines:
    /// </para>
    /// <code>
    /// var transport = new HttpProxyTransport("http://worker-b:5100");
    /// var remote    = new RemoteProxyDataSource(transport, "worker-b", editor);
    /// cluster.AddNode(new ProxyNode("worker-b", remote, weight: 2,
    ///     role: ProxyDataSourceRole.Primary));
    /// </code>
    ///
    /// <para>
    /// All complex arguments are serialized as JSON before being sent over the wire.
    /// All complex return values are deserialized from the worker's JSON response.
    /// </para>
    /// </summary>
    public sealed class RemoteProxyDataSource : IProxyDataSource
    {
        private readonly IProxyTransport _transport;
        private readonly IDMEEditor      _editor;
        private bool    _disposed;

        private static readonly JsonSerializerOptions _json = new JsonSerializerOptions
        {
            PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented               = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        // ── IDataSource identity ─────────────────────────────────────────────

        public string             GuidID             { get; set; } = Guid.NewGuid().ToString();
        public string             DatasourceName     { get; set; }
        public DataSourceType     DatasourceType     { get; set; } = DataSourceType.Other;
        public DatasourceCategory Category           { get; set; } = DatasourceCategory.RDBMS;
        public IErrorsInfo        ErrorObject        { get; set; }
        public string             Id                 { get; set; }
        public IDMLogger          Logger             { get; set; }
        public ConnectionState    ConnectionStatus   { get; set; } = ConnectionState.Closed;
        public IDataConnection    Dataconnection     { get; set; }
        public string             ColumnDelimiter    { get; set; }
        public string             ParameterDelimiter { get; set; }
        public List<string>       EntitiesNames      { get; set; } = new List<string>();
        public List<EntityStructure> Entities        { get; set; } = new List<EntityStructure>();
        public IDMEEditor         DMEEditor          { get { return _editor; } set { } }

        // ── IProxyDataSource backward-compat knobs ───────────────────────────

        public int MaxRetries                       { get; set; } = 3;
        public int RetryDelayMilliseconds           { get; set; } = 500;
        public int HealthCheckIntervalMilliseconds  { get; set; } = 30_000;
        public int WatchdogIntervalMs               { get; set; } = 5_000;
        public int WatchdogProbeTimeoutMs           { get; set; } = 2_000;
        public int WatchdogFailureThreshold         { get; set; } = 2;
        public int WatchdogRecoveryThreshold        { get; set; } = 3;

        // ── Audit ────────────────────────────────────────────────────────────

        public IProxyAuditSink AuditSink { get; set; } = NullProxyAuditSink.Instance;

        // ── Events ───────────────────────────────────────────────────────────

        public event EventHandler<PassedArgs>          PassEvent;
        public event EventHandler<FailoverEventArgs>   OnFailover;
        public event EventHandler<RecoveryEventArgs>   OnRecovery;
        public event EventHandler<RoleChangeEventArgs> OnRolePromoted;
        public event EventHandler<RoleChangeEventArgs> OnRoleDemoted;

        // ── Constructor ──────────────────────────────────────────────────────

        /// <param name="transport">Network transport (HTTP, gRPC, …)</param>
        /// <param name="nodeName">Logical name shown in metrics and logs.</param>
        /// <param name="editor">Local DMEEditor (for logging only — data ops go remote).</param>
        public RemoteProxyDataSource(IProxyTransport transport, string nodeName, IDMEEditor editor)
        {
            _transport     = transport ?? throw new ArgumentNullException(nameof(transport));
            _editor        = editor    ?? throw new ArgumentNullException(nameof(editor));
            DatasourceName = nodeName  ?? transport.NodeAddress;
        }

        /// <summary>
        /// Creates a <see cref="RemoteProxyDataSource"/> from a node's
        /// <see cref="TheTechIdea.Beep.ConfigUtil.ConnectionProperties"/> as
        /// stored in ConfigEditor (DriverName = "BeepProxyNode").
        /// </summary>
        public static RemoteProxyDataSource FromConnectionProperties(
            TheTechIdea.Beep.ConfigUtil.ConnectionProperties config,
            IDMEEditor editor)
        {
            if (config is null) throw new ArgumentNullException(nameof(config));
            var transport = HttpProxyTransport.FromConnectionProperties(config);
            return new RemoteProxyDataSource(transport, config.ConnectionName, editor);
        }

        // ── Connection ───────────────────────────────────────────────────────

        public ConnectionState Openconnection()
        {
            var resp = Send(ProxyRemoteOperations.Openconnection, new ProxyRemoteRequest
            {
                Operation = ProxyRemoteOperations.Openconnection
            });

            ConnectionStatus = resp.Success ? ConnectionState.Open : ConnectionState.Broken;
            return ConnectionStatus;
        }

        public ConnectionState Closeconnection()
        {
            Send(ProxyRemoteOperations.Closeconnection, new ProxyRemoteRequest
            {
                Operation = ProxyRemoteOperations.Closeconnection
            });
            ConnectionStatus = ConnectionState.Closed;
            return ConnectionState.Closed;
        }

        // ── IDataSource — read operations ────────────────────────────────────

        public IEnumerable<string> GetEntitesList()
        {
            var resp = Send(ProxyRemoteOperations.GetEntitiesNames, new ProxyRemoteRequest
            {
                Operation = ProxyRemoteOperations.GetEntitiesNames
            });
            if (!resp.Success || string.IsNullOrEmpty(resp.DataJson))
                return EntitiesNames ?? new List<string>();

            var names = JsonSerializer.Deserialize<List<string>>(resp.DataJson, _json);
            if (names != null) EntitiesNames = names;
            return EntitiesNames;
        }

        public IEnumerable<object> RunQuery(string qrystr)
        {
            var resp = Send(ProxyRemoteOperations.RunQuery, new ProxyRemoteRequest
            {
                Operation = ProxyRemoteOperations.RunQuery,
                QuerySql  = qrystr
            });
            return resp.Success ? DeserializeList(resp) : Enumerable.Empty<object>();
        }

        public IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
        {
            var resp = Send(ProxyRemoteOperations.GetEntity, new ProxyRemoteRequest
            {
                Operation   = ProxyRemoteOperations.GetEntity,
                EntityName  = EntityName,
                FiltersJson = Serialize(filter)
            });
            return resp.Success ? DeserializeList(resp) : Enumerable.Empty<object>();
        }

        public PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            var resp = Send(ProxyRemoteOperations.GetEntity, new ProxyRemoteRequest
            {
                Operation   = "GetEntityPaged",
                EntityName  = EntityName,
                FiltersJson = Serialize(filter),
                PageNumber  = pageNumber,
                PageSize    = pageSize
            });
            if (!resp.Success || string.IsNullOrEmpty(resp.DataJson)) return null;
            return JsonSerializer.Deserialize<PagedResult>(resp.DataJson, _json);
        }

        public async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            var resp = await _transport.SendAsync(new ProxyRemoteRequest
            {
                Operation   = ProxyRemoteOperations.GetEntity,
                EntityName  = EntityName,
                FiltersJson = Serialize(Filter)
            }, CancellationToken.None).ConfigureAwait(false);

            return resp.Success ? DeserializeList(resp) : Enumerable.Empty<object>();
        }

        public double GetScalar(string query)
        {
            var resp = Send(ProxyRemoteOperations.RunQuery, new ProxyRemoteRequest
            {
                Operation = "GetScalar",
                QuerySql  = query
            });
            if (!resp.Success || string.IsNullOrEmpty(resp.DataJson)) return 0.0;
            if (double.TryParse(resp.DataJson.Trim('"'), out var d)) return d;
            return 0.0;
        }

        public async Task<double> GetScalarAsync(string query)
        {
            var resp = await _transport.SendAsync(new ProxyRemoteRequest
            {
                Operation = "GetScalar",
                QuerySql  = query
            }, CancellationToken.None).ConfigureAwait(false);

            if (!resp.Success || string.IsNullOrEmpty(resp.DataJson)) return 0.0;
            if (double.TryParse(resp.DataJson.Trim('"'), out var d)) return d;
            return 0.0;
        }

        // ── IDataSource — metadata ───────────────────────────────────────────

        public EntityStructure GetEntityStructure(string EntityName, bool refresh)
        {
            var resp = Send(ProxyRemoteOperations.GetEntityStructure, new ProxyRemoteRequest
            {
                Operation       = ProxyRemoteOperations.GetEntityStructure,
                EntityName      = EntityName,
                RefreshMetadata = refresh
            });
            if (!resp.Success || string.IsNullOrEmpty(resp.DataJson)) return null;
            return JsonSerializer.Deserialize<EntityStructure>(resp.DataJson, _json);
        }

        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
            => GetEntityStructure(fnd?.EntityName, refresh);

        public Type GetEntityType(string EntityName)
        {
            var es = GetEntityStructure(EntityName, false);
            if (es == null) return typeof(object);
            // Best effort: return dynamic proxy type or object
            return typeof(object);
        }

        public int GetEntityIdx(string entityName)
        {
            if (EntitiesNames == null) return -1;
            return EntitiesNames.IndexOf(entityName);
        }

        public bool CheckEntityExist(string EntityName)
        {
            var resp = Send("CheckEntityExist", new ProxyRemoteRequest
            {
                Operation  = "CheckEntityExist",
                EntityName = EntityName
            });
            return resp.Success;
        }

        public IEnumerable<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            var resp = Send("GetChildTablesList", new ProxyRemoteRequest
            {
                Operation  = "GetChildTablesList",
                EntityName = tablename,
                SchemaName = SchemaName,
                FilterStr  = Filterparamters
            });
            if (!resp.Success || string.IsNullOrEmpty(resp.DataJson))
                return new List<ChildRelation>();
            return JsonSerializer.Deserialize<List<ChildRelation>>(resp.DataJson, _json)
                   ?? new List<ChildRelation>();
        }

        public IEnumerable<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            var resp = Send("GetEntityforeignkeys", new ProxyRemoteRequest
            {
                Operation  = "GetEntityforeignkeys",
                EntityName = entityname,
                SchemaName = SchemaName
            });
            if (!resp.Success || string.IsNullOrEmpty(resp.DataJson))
                return new List<RelationShipKeys>();
            return JsonSerializer.Deserialize<List<RelationShipKeys>>(resp.DataJson, _json)
                   ?? new List<RelationShipKeys>();
        }

        public IEnumerable<ETLScriptDet> GetCreateEntityScript(List<EntityStructure> entities = null)
        {
            var resp = Send("GetCreateEntityScript", new ProxyRemoteRequest
            {
                Operation  = "GetCreateEntityScript",
                RecordJson = Serialize(entities)
            });
            if (!resp.Success || string.IsNullOrEmpty(resp.DataJson))
                return new List<ETLScriptDet>();
            return JsonSerializer.Deserialize<List<ETLScriptDet>>(resp.DataJson, _json)
                   ?? new List<ETLScriptDet>();
        }

        public bool CreateEntityAs(EntityStructure entity)
        {
            var resp = Send("CreateEntityAs", new ProxyRemoteRequest
            {
                Operation  = "CreateEntityAs",
                RecordJson = Serialize(entity)
            });
            return resp.Success;
        }

        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            var resp = Send("CreateEntities", new ProxyRemoteRequest
            {
                Operation  = "CreateEntities",
                RecordJson = Serialize(entities)
            });
            return resp.Success ? MakeOkErrors() : MakeFailErrors(resp.ErrorMessage);
        }

        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            var resp = Send("RunScript", new ProxyRemoteRequest
            {
                Operation  = "RunScript",
                RecordJson = Serialize(dDLScripts)
            });
            return resp.Success ? MakeOkErrors() : MakeFailErrors(resp.ErrorMessage);
        }

        // ── IDataSource — write operations ───────────────────────────────────

        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
            => SendWrite("InsertEntity", EntityName, InsertedData);

        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
            => SendWrite("UpdateEntity", EntityName, UploadDataRow);

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
            => SendWrite("UpdateEntities", EntityName, UploadData);

        public IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow)
            => SendWrite("DeleteEntity", EntityName, UploadDataRow);

        public IErrorsInfo ExecuteSql(string sql)
        {
            var resp = Send(ProxyRemoteOperations.ExecuteSQL, new ProxyRemoteRequest
            {
                Operation = ProxyRemoteOperations.ExecuteSQL,
                QuerySql  = sql
            });
            return resp.Success ? MakeOkErrors() : MakeFailErrors(resp.ErrorMessage);
        }

        // ── Transactions ─────────────────────────────────────────────────────

        public IErrorsInfo BeginTransaction(PassedArgs args)
            => SendPassedArgs(ProxyRemoteOperations.BeginTransaction, args);

        public IErrorsInfo EndTransaction(PassedArgs args)
            => SendPassedArgs(ProxyRemoteOperations.EndTransaction, args);

        public IErrorsInfo Commit(PassedArgs args)
            => SendPassedArgs(ProxyRemoteOperations.Commit, args);

        // ── IProxyDataSource — policy & routing ──────────────────────────────

        public void ApplyPolicy(ProxyPolicy policy)
        {
            Send(ProxyRemoteOperations.ApplyPolicy, new ProxyRemoteRequest
            {
                Operation  = ProxyRemoteOperations.ApplyPolicy,
                PolicyJson = Serialize(policy)
            });
        }

        public async Task<T> ExecuteWithLoadBalancing<T>(
            Func<IDataSource, Task<T>> operation,
            bool isWrite = false,
            CancellationToken cancellationToken = default)
        {
            // The coordinator delegates routing to the remote worker.
            return await operation(this).ConfigureAwait(false);
        }

        // ── IProxyDataSource — metrics ───────────────────────────────────────

        public IDictionary<string, DataSourceMetrics> GetMetrics()
        {
            var resp = Send(ProxyRemoteOperations.GetMetrics, new ProxyRemoteRequest
            {
                Operation = ProxyRemoteOperations.GetMetrics
            });
            if (!resp.Success || string.IsNullOrEmpty(resp.DataJson))
                return new Dictionary<string, DataSourceMetrics>();

            return JsonSerializer.Deserialize<Dictionary<string, DataSourceMetrics>>(
                       resp.DataJson, _json)
                   ?? new Dictionary<string, DataSourceMetrics>();
        }

        public ProxySloSnapshot GetSloSnapshot(string dsName)
        {
            var resp = Send(ProxyRemoteOperations.GetSloSnapshot, new ProxyRemoteRequest
            {
                Operation   = ProxyRemoteOperations.GetSloSnapshot,
                SloTargetDs = dsName
            });
            if (!resp.Success || string.IsNullOrEmpty(resp.DataJson)) return null;
            return JsonSerializer.Deserialize<ProxySloSnapshot>(resp.DataJson, _json);
        }

        public IReadOnlyList<ProxySloSnapshot> GetAllSloSnapshots()
        {
            var resp = Send(ProxyRemoteOperations.GetSloSnapshot, new ProxyRemoteRequest
            {
                Operation = ProxyRemoteOperations.GetSloSnapshot
            });
            if (!resp.Success || string.IsNullOrEmpty(resp.DataJson))
                return Array.Empty<ProxySloSnapshot>();
            return JsonSerializer.Deserialize<List<ProxySloSnapshot>>(resp.DataJson, _json)
                   ?? (IReadOnlyList<ProxySloSnapshot>)Array.Empty<ProxySloSnapshot>();
        }

        // ── IProxyDataSource — datasource membership ─────────────────────────

        public void AddDataSource(string dsName, int weight = 1)
            => Send(ProxyRemoteOperations.AddDataSource, new ProxyRemoteRequest
            {
                Operation      = ProxyRemoteOperations.AddDataSource,
                DatasourceName = dsName,
                Weight         = weight
            });

        public void RemoveDataSource(string dsName)
            => Send(ProxyRemoteOperations.RemoveDataSource, new ProxyRemoteRequest
            {
                Operation      = ProxyRemoteOperations.RemoveDataSource,
                DatasourceName = dsName
            });

        public void SetRole(string dsName, ProxyDataSourceRole role)
            => Send(ProxyRemoteOperations.SetRole, new ProxyRemoteRequest
            {
                Operation      = ProxyRemoteOperations.SetRole,
                DatasourceName = dsName,
                RoleStr        = role.ToString()
            });

        // ── IProxyDataSource — watchdog ──────────────────────────────────────
        // Watchdog runs at the worker — client surface is a no-op.

        public void StartWatchdog() { }
        public void StopWatchdog()  { }

        public IReadOnlyList<WatchdogNodeStatus> GetWatchdogStatus()
            => Array.Empty<WatchdogNodeStatus>();

        // ── IProxyDataSource — cache ─────────────────────────────────────────

        public object GetEntityWithCache(string entityName, List<AppFilter> filter,
            TimeSpan? expiration = null)
            => GetEntity(entityName, filter);   // cache lives at worker

        public void InvalidateCache(string entityName = null)
            => Send("InvalidateCache", new ProxyRemoteRequest
            {
                Operation  = "InvalidateCache",
                EntityName = entityName
            });

        // ── IProxyDataSource — connection pool ───────────────────────────────

        public IDataSource GetPooledConnection(string dsName) => this;
        public void        ReturnConnection(string dsName, IDataSource connection) { }
        public IDataSource GetConnection(string dsName) => this;

        // ── Dispose ──────────────────────────────────────────────────────────

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _transport.Dispose();
        }

        // ── Private helpers ──────────────────────────────────────────────────

        private ProxyRemoteResponse Send(string op, ProxyRemoteRequest req)
        {
            if (_disposed) return ProxyRemoteResponse.Fail(req.CorrelationId, "Transport disposed.");
            try
            {
                return _transport.SendAsync(req, CancellationToken.None)
                    .GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Log($"[RemoteProxy:{DatasourceName}] Transport error on '{op}': {ex.Message}");
                return ProxyRemoteResponse.Fail(req.CorrelationId, ex.Message);
            }
        }

        private IErrorsInfo SendWrite(string op, string entityName, object record)
        {
            var resp = Send(op, new ProxyRemoteRequest
            {
                Operation      = op,
                EntityName     = entityName,
                RecordJson     = Serialize(record),
                RecordTypeHint = record?.GetType().AssemblyQualifiedName
            });
            return resp.Success ? MakeOkErrors() : MakeFailErrors(resp.ErrorMessage);
        }

        private IErrorsInfo SendPassedArgs(string op, PassedArgs args)
        {
            var resp = Send(op, new ProxyRemoteRequest
            {
                Operation      = op,
                PassedArgsJson = Serialize(args)
            });
            return resp.Success ? MakeOkErrors() : MakeFailErrors(resp.ErrorMessage);
        }

        private static string Serialize(object obj)
            => obj is null ? null : JsonSerializer.Serialize(obj, _json);

        private static IEnumerable<object> DeserializeList(ProxyRemoteResponse resp)
        {
            if (string.IsNullOrEmpty(resp.DataJson)) return Enumerable.Empty<object>();
            if (!string.IsNullOrEmpty(resp.TypeHint))
            {
                var t = Type.GetType(resp.TypeHint, throwOnError: false);
                if (t != null)
                {
                    var listType = typeof(List<>).MakeGenericType(t);
                    var list = JsonSerializer.Deserialize(resp.DataJson, listType, _json);
                    if (list is IEnumerable<object> typed) return typed;
                    if (list is System.Collections.IEnumerable raw)
                        return raw.Cast<object>();
                }
            }
            return JsonSerializer.Deserialize<List<object>>(resp.DataJson, _json)
                   ?? Enumerable.Empty<object>();
        }

        private IErrorsInfo MakeOkErrors()
        {
            var ei = _editor.ErrorObject;
            if (ei != null) { ei.Flag = Errors.Ok; return ei; }
            return new ErrorsInfo { Flag = Errors.Ok };
        }

        private IErrorsInfo MakeFailErrors(string message)
        {
            Log($"[RemoteProxy:{DatasourceName}] {message}");
            return new ErrorsInfo { Flag = Errors.Failed, Message = message };
        }

        private void Log(string msg)
            => _editor?.AddLogMessage("RemoteProxy", msg, DateTime.Now, 0, null, Errors.Warning);
    }
}
