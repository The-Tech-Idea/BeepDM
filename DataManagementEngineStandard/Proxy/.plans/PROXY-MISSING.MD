What's Still Missing / Incomplete After the Watchdog
P0 — Real gaps (code compiles but behaves incorrectly)
#	Gap	Where	Problem
1	GetConnection not implemented	IProxyDataSource declares GetConnection(string dsName) but there's no implementation in any partial	Will throw NotImplementedException at runtime
2	GetMetrics not implemented	IProxyDataSource.GetMetrics() is in the interface — no matching public method exists	Same
3	Watchdog ProbeDatasource uses .Wait() on async inside ProbeAllNodes which is called sync	Watchdog.cs	task.Wait() on a Task.FromResult is benign, but ProxyLivenessHelper.IsAlive is synchronous anyway — the wrapping is fine. However, true async RDBMS probes in future will deadlock if WatchdogLoop is ever called on a context with a sync-context (WinForms/ASP.NET classic)
4	_healthCheckTimer set to null in the field declaration but InitializeFromPolicy re-assigns it — the readonly keyword is missing, meaning ApplyPolicy can create a second timer and the first one leaks	ProxyDataSource.cs:40, InitializeFromPolicy, ApplyPolicy	Timer leak — old timer keeps firing and the new policy is never fully applied
5	WatchdogIntervalMs used as timeout for TestConnectionAsync but it's also the loop delay	Watchdog.cs	Probe timeout = probe interval = 5 s. When a probe takes the full 5 s to time out, the next probe fires immediately. Add a separate WatchdogProbeTimeoutMs property
6	No IConnection / reconnect on pool stale connections	GetPooledConnection	If the connection in the pool has gone stale (Broken state, not just Closed), Openconnection() may silently return a broken connection. Need stale guard
P1 — Missing interface contract vs. implementations
#	Gap	Where
7	IProxyDataSource.OnRolePromoted / OnRoleDemoted not sync'd to IDataSource — the events compile but are only available on the concrete type, not via the interface (they ARE declared in IProxyDataSource — confirmed)	✅ Already covered
8	GAP-008 — Distributed circuit state	Documented in plans but not implemented. In-process only — tracked as known limit
9	Phase 7 — Security: query/filter value redaction in logs	All log calls pass raw entity/filter strings. PII can leak into dm.log
10	Phase 7 — Audit trail	No immutable route-decision record per execution (correlation ID exists but no persisted audit log)
11	Phase 8 — Adaptive throttling	Saturation error class is classified by ProxyErrorClassifier but never acted on — no backpressure / throttle response
12	Phase 9 — Failover simulation test harness	No tests at all for the proxy layer
P2 — Missing features (design gaps)
#	Gap	Details
13	Write replication / fan-out	Writes go to the single Primary. There's no opt-in multi-primary fan-out mode (e.g. dual-write to two SQL Servers in an active-active topology)
14	Read/write split routing in ExecuteWithLoadBalancing	The public ExecuteWithLoadBalancing<T> method ignores roles entirely — it will send reads to the Primary like writes do
15	ProxyDataSourceOptions is created but never mutated	_options field is set in the constructor and passed to nothing — it's vestigial
16	SafeFailover only logs — it doesn't actually call Failover()	Callers in ExecuteReadWithPolicy hit SafeFailover(operationName) when a result is "unsuccessful" — if it doesn't trigger a real failover we silently move to the next candidate in the loop without updating _currentIndex
17	GetWatchdogStatus() not implemented	IProxyDataSource declares it; looking at Watchdog.cs 
What's Still Missing from ProxyDataSource After the Watchdog
P0 — Behavioural bugs (compile-clean, runtime-wrong)
#	Gap	File	Detail
1	_healthCheckTimer is leaked on ApplyPolicy	ProxyDataSource.cs / Observability.cs	[InitializeFromPolicy] creates a new Timer each call. ApplyPolicy calls nothing to stop the old one first → two timers fire concurrently after any policy change at runtime
2	WatchdogIntervalMs doubles as both loop-delay AND probe timeout	Watchdog.cs	A slow probe blocks the 5 s window. Add a separate WatchdogProbeTimeoutMs (default 2 000 ms)
3	Stale pool connection returned without broken-state guard	Routing.cs → GetPooledConnection	Openconnection() is called if != Open, but if the state is Broken, the driver may throw or silently succeed while returning a non-functional connection. Need to discard Broken-state pool entries
4	ExecuteWithLoadBalancing ignores roles — can send writes via it to a Replica	Routing.cs	The public Task<T> ExecuteWithLoadBalancing calls SelectCandidates() (read-pool), not SelectWriteCandidates(). Callers using it for mutations bypass the Primary-only write guard
P1 — Missing features (interface gap or behaviour gap)
#	Gap	Detail
5	No ProxyDataSource.Metrics.cs partial — GetMetrics() lives in Caching.cs (wrong file)	Should be in Observability.cs alongside GetSloSnapshot
6	GAP-008: Distributed circuit state	All _circuitBreakers / _healthStatus are in-process. Web-farm deployments have split-brain health views. Documented as a known limit, but the swappable ICircuitStateStore interface described in the plan docs was never created
7	No Phase 7 security / audit layer	Log calls pass raw query strings and filter values — PII leaks to dm.log. No immutable audit envelope per execution
8	Saturation category classified but never acted on	ProxyErrorClassifier can return Saturation — but ExecuteReadWithPolicy treats it identically to Transient. No backpressure, no sleep-under-load, no adaptive throttle
9	No multi-primary fan-out write mode	Writes go strictly to one Primary. There's no opt-in dual-write for active-active topologies
P2 — Missing quality / testing layer (Phase 9)
#	Gap
10	Zero tests — no unit tests for ExecuteReadWithPolicy, ExecuteWriteWithPolicy, circuit breaker, watchdog promotion/demotion, cache invalidation, SLO snapshot
11	No failover simulation harness — no IFaultInjector / chaos contract to stub a datasource into failing in tests
12	ProxyDataSourceOptions is vestigial — created in constructor, never used by any logic after _policy became the single source of truth
Recommended Priority Order
Fix ApplyPolicy timer leak (P0 — silent resource leak, easy fix)
Add WatchdogProbeTimeoutMs (P0 — prevents probe deadlocking the loop)
Guard Broken-state pool connections (P0 — silent broken connection bug)
ExecuteWithLoadBalancing read-only guard (P0 — write-via-replica bug)
ICircuitStateStore swappable interface (P1 — needed before web-farm deploy)
Saturation backpressure (P1 — needed under heavy load)
Tests + fault injection (P2 — before any production use)