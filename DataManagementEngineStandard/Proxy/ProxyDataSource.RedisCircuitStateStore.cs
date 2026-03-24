using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Proxy
{
    /// <summary>
    /// Redis-backed <see cref="ICircuitStateStore"/>.
    /// All keys live under the prefix "<paramref name="keyPrefix"/>:" so multiple
    /// clusters can share a single Redis instance without key collisions.
    ///
    /// Requires <c>StackExchange.Redis</c> NuGet package.
    /// Add <c>&lt;PackageReference Include="StackExchange.Redis" Version="2.*" /&gt;</c>
    /// to the project file if not already present.
    /// </summary>
    /// <remarks>
    /// State key format:
    /// <list type="bullet">
    ///   <item><c>&lt;prefix&gt;:state:&lt;dsName&gt;</c> → "Closed" | "Open" | "HalfOpen"</item>
    ///   <item><c>&lt;prefix&gt;:fails:&lt;dsName&gt;</c> → integer (INCR)</item>
    /// </list>
    /// </remarks>
    public sealed class RedisCircuitStateStore : ICircuitStateStore, IDisposable
    {
        // ── Redis is referenced lazily via reflection so the assembly compiles
        //    without requiring StackExchange.Redis unless this class is actually used.
        // ── For a real deployment, uncomment the direct StackExchange.Redis usage
        //    below and remove the reflection shim.

        // ── In-process fallback used when Redis is unavailable at construction ──
        private readonly InProcessCircuitStateStore _fallback = new();

        // ── Config ────────────────────────────────────────────────────────
        private readonly string _prefix;
        private readonly string _connectionString;

        // ── Redis multiplexer (StackExchange.Redis types held as dynamic/object
        //    to avoid a hard compile-time dependency) ────────────────────────
        private object?  _multiplexer;   // IConnectionMultiplexer
        private object?  _db;            // IDatabase
        private volatile bool _redisAvailable;

        // ── Per-dsName config cache for re-init ───────────────────────────
        private readonly ConcurrentDictionary<string, (int Threshold, TimeSpan Timeout, int SuccessThreshold)>
            _configs = new();

        /// <summary>
        /// Creates a store backed by the specified Redis connection.
        /// Falls back silently to in-process state when Redis is unreachable.
        /// </summary>
        /// <param name="connectionString">StackExchange.Redis connection string.</param>
        /// <param name="keyPrefix">Cluster-unique prefix for all Redis keys.</param>
        public RedisCircuitStateStore(
            string connectionString,
            string keyPrefix = "beepproxy")
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException(nameof(connectionString));

            _connectionString = connectionString;
            _prefix           = keyPrefix;
            TryConnectRedis();
        }

        private void TryConnectRedis()
        {
            try
            {
                // Dynamic invocation so the assembly compiles without a hard reference.
                var seType = Type.GetType(
                    "StackExchange.Redis.ConnectionMultiplexer, StackExchange.Redis");

                if (seType is null)
                {
                    // StackExchange.Redis not loaded — use fallback
                    _redisAvailable = false;
                    return;
                }

                var connectMethod = seType.GetMethod("Connect",
                    new[] { typeof(string),
                            Type.GetType("System.IO.TextWriter") ?? typeof(object) });

                _multiplexer = connectMethod?.Invoke(null, new object?[] { _connectionString, null });

                var getDbMethod = _multiplexer?.GetType().GetMethod("GetDatabase",
                    new[] { typeof(int), typeof(object) });

                _db = getDbMethod?.Invoke(_multiplexer, new object?[] { -1, null });
                _redisAvailable = _db is not null;
            }
            catch
            {
                _redisAvailable = false;
            }
        }

        // ── Redis key helpers ─────────────────────────────────────────────
        private string StateKey(string ds)  => $"{_prefix}:state:{ds}";
        private string FailsKey(string ds)  => $"{_prefix}:fails:{ds}";

        // ── ICircuitStateStore — delegate to Redis or fallback ────────────

        /// <inheritdoc/>
        public bool CanExecute(string dsName)
            => _redisAvailable ? RedisCanExecute(dsName) : _fallback.CanExecute(dsName);

        /// <inheritdoc/>
        public void RecordSuccess(string dsName)
        {
            if (_redisAvailable) RedisRecordSuccess(dsName);
            _fallback.RecordSuccess(dsName);
        }

        /// <inheritdoc/>
        public void RecordFailure(string dsName, ProxyErrorSeverity severity = ProxyErrorSeverity.Medium)
        {
            if (_redisAvailable) RedisRecordFailure(dsName, severity);
            _fallback.RecordFailure(dsName, severity);
        }

        /// <inheritdoc/>
        public void Initialize(string dsName, int failureThreshold, TimeSpan resetTimeout, int successThreshold = 2)
        {
            _configs[dsName] = (failureThreshold, resetTimeout, successThreshold);
            if (_redisAvailable)
                RedisInitialize(dsName);
            _fallback.Initialize(dsName, failureThreshold, resetTimeout, successThreshold);
        }

        /// <inheritdoc/>
        public void Remove(string dsName)
        {
            _configs.TryRemove(dsName, out _);
            if (_redisAvailable) RedisRemove(dsName);
            _fallback.Remove(dsName);
        }

        /// <inheritdoc/>
        public CircuitBreakerState GetState(string dsName)
            => _redisAvailable ? RedisGetState(dsName) : _fallback.GetState(dsName);

        /// <inheritdoc/>
        public void ForceOpen(string dsName)
        {
            if (_redisAvailable) RedisSetState(dsName, CircuitBreakerState.Open);
            _fallback.ForceOpen(dsName);
        }

        /// <inheritdoc/>
        public void Reset(string dsName)
        {
            if (_redisAvailable)
            {
                RedisSetState(dsName, CircuitBreakerState.Closed);
                RedisResetFailures(dsName);
            }
            _fallback.Reset(dsName);
        }

        // ── Redis implementation helpers ──────────────────────────────────

        private bool RedisCanExecute(string dsName)
        {
            try
            {
                var state = RedisGetState(dsName);
                return state != CircuitBreakerState.Open;
            }
            catch { return _fallback.CanExecute(dsName); }
        }

        private void RedisRecordSuccess(string dsName)
        {
            try
            {
                RedisSetState(dsName, CircuitBreakerState.Closed);
                RedisResetFailures(dsName);
            }
            catch { /* best-effort */ }
        }

        private void RedisRecordFailure(string dsName, ProxyErrorSeverity severity)
        {
            try
            {
                int weight = severity switch
                {
                    ProxyErrorSeverity.Critical => 999,
                    ProxyErrorSeverity.High     => 2,
                    _                           => 1
                };

                var failsKey = FailsKey(dsName);
                long failures = RedisIncrement(failsKey, weight);

                if (_configs.TryGetValue(dsName, out var cfg) && failures >= cfg.Threshold)
                    RedisSetState(dsName, CircuitBreakerState.Open);
            }
            catch { /* best-effort */ }
        }

        private void RedisInitialize(string dsName)
        {
            try
            {
                // Only set if the key doesn't already exist (NX semantics via StringSet)
                RedisSetStateIfNotExists(dsName, CircuitBreakerState.Closed);
            }
            catch { /* best-effort */ }
        }

        private void RedisRemove(string dsName)
        {
            try
            {
                RedisDelete(StateKey(dsName));
                RedisDelete(FailsKey(dsName));
            }
            catch { /* best-effort */ }
        }

        // ── Thin Redis command wrappers (via reflection) ─────────────────
        // These resolve the overloads at runtime to avoid hard StackExchange.Redis
        // compile-time types.  In a project that directly references the package,
        // replace these with direct IDatabase calls for clarity and performance.

        private CircuitBreakerState RedisGetState(string dsName)
        {
            var raw = StringGet(StateKey(dsName));
            return raw switch
            {
                "Open"     => CircuitBreakerState.Open,
                "HalfOpen" => CircuitBreakerState.HalfOpen,
                _          => CircuitBreakerState.Closed
            };
        }

        private void RedisSetState(string dsName, CircuitBreakerState state)
            => StringSet(StateKey(dsName), state.ToString());

        private void RedisSetStateIfNotExists(string dsName, CircuitBreakerState state)
            => StringSetNx(StateKey(dsName), state.ToString());

        private void RedisResetFailures(string dsName)
            => StringSet(FailsKey(dsName), "0");

        private void RedisDelete(string key)
            => InvokeDb("KeyDelete", key);

        private long RedisIncrement(string key, int by)
        {
            var result = InvokeDb("StringIncrement", key, (long)by);
            return result is long l ? l : 0L;
        }

        private string? StringGet(string key)
        {
            var result = InvokeDb("StringGet", key);
            return result?.ToString();
        }

        private void StringSet(string key, string value)
            => InvokeDb("StringSet", key, value);

        private void StringSetNx(string key, string value)
        {
            // StringSet with When.NotExists
            try
            {
                var whenType = Type.GetType(
                    "StackExchange.Redis.When, StackExchange.Redis");
                if (whenType is null) return;
                var whenNx = Enum.Parse(whenType, "NotExists");
                _db?.GetType().GetMethod("StringSet",
                    new[] { typeof(string), typeof(string), typeof(TimeSpan?), whenType })
                    ?.Invoke(_db, new object?[] { key, value, null, whenNx });
            }
            catch { /* best-effort */ }
        }

        private object? InvokeDb(string methodName, params object?[] args)
        {
            try
            {
                var method = _db?.GetType().GetMethod(methodName);
                return method?.Invoke(_db, args);
            }
            catch { return null; }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            try
            {
                if (_multiplexer is IDisposable d) d.Dispose();
            }
            catch { /* best-effort */ }
        }
    }
}
