# ProxyDataSource — Future Implementation Details

Items: P1-9 (Log Redaction), P1-10 (Audit Trail), P1-12 (Test Harness), P2-13 (Write Fan-Out)

Root: `DataManagementEngineStandard/Proxy/`

---

## P1-9 — PII / Log Redaction in All Proxy Log Calls

### Problem

Every `_dmeEditor.AddLogMessage(...)` call in the proxy layer passes raw strings that
may contain entity names, filter values, column values, or query fragments sourced
directly from caller-supplied parameters.  All of these can contain PII (names,
emails, SSNs, etc.) that end up verbatim in `dm.log`.

Affected call-sites (non-exhaustive):

| File | Pattern |
|------|---------|
| `ExecutionHelpers.cs` | `$"[{correlationId}] {operationName}: unsuccessful result on {dsName}"` |
| `ExecutionHelpers.cs` | `$"Operation failed on {dsName}: {ex.Message}"` — `ex.Message` may echo a query value |
| `Routing.cs` | `$"Health check for {dsName} failed: {ex.Message}"` |
| `Watchdog.cs` | All promotion / demotion messages (dsName only — safe) |

### Design

#### 1. `ProxyLogRedactor` — new static helper class

New file: `ProxyLogRedactor.cs`

```csharp
internal static class ProxyLogRedactor
{
    // Patterns that may contain PII in a SQL error / message
    private static readonly Regex[] _patterns = new[]
    {
        // Numeric values in WHERE clauses:  = 123456789
        new Regex(@"=\s*\d{6,}", RegexOptions.Compiled),
        // Quoted string values:  = 'anything'  or  = "anything"
        new Regex(@"=\s*('[^']*'|""[^""]*"")", RegexOptions.Compiled),
        // Email addresses
        new Regex(@"[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled),
        // SSN-like  ddd-dd-dddd
        new Regex(@"\b\d{3}-\d{2}-\d{4}\b", RegexOptions.Compiled),
        // Credit-card-like 16-digit runs
        new Regex(@"\b(?:\d[ -]?){16}\b", RegexOptions.Compiled),
    };

    /// <summary>
    /// Returns a redacted copy of <paramref name="raw"/> safe to write to the log.
    /// PII patterns are replaced with [REDACTED].
    /// </summary>
    public static string Redact(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return raw;
        foreach (var p in _patterns)
            raw = p.Replace(raw, "[REDACTED]");
        return raw;
    }

    /// <summary>
    /// Redacts the Message and InnerException.Message of an exception before logging.
    /// </summary>
    public static string RedactException(Exception ex)
        => ex == null ? string.Empty
            : Redact(ex.InnerException != null
                ? $"{ex.Message} → {ex.InnerException.Message}"
                : ex.Message);
}
```

#### 2. Opt-in via `ProxyPolicy`

Add a flag on `ProxyPolicy` so redaction can be disabled in development environments:

```csharp
// ProxyotherClasses.cs — ProxyPolicy class
public bool EnableLogRedaction { get; init; } = true;   // default ON in production
```

#### 3. Wrapper extension on `ProxyDataSource`

Add a private helper in `ProxyDataSource.ExecutionHelpers.cs` after the existing
`DelayWithBackoff` helpers:

```csharp
/// <summary>
/// Writes a log message, optionally redacting PII based on the current policy.
/// Use this instead of calling _dmeEditor.AddLogMessage() directly with
/// user-supplied strings.
/// </summary>
private void LogSafe(string message)
{
    var text = _policy.EnableLogRedaction
        ? ProxyLogRedactor.Redact(message)
        : message;
    _dmeEditor.AddLogMessage(text);
}

private void LogSafe(string message, Exception ex)
{
    var text = _policy.EnableLogRedaction
        ? $"{ProxyLogRedactor.Redact(message)} — {ProxyLogRedactor.RedactException(ex)}"
        : $"{message} — {ex.Message}";
    _dmeEditor.AddLogMessage(text);
}
```

#### 4. Migration — replace unsafe `AddLogMessage` call-sites

Go through every `_dmeEditor.AddLogMessage(...)` in the following files and replace
with `LogSafe(...)` where the message contains any of:

- `ex.Message` / `ex.InnerException`
- Entity names passed in by callers (`entityName`, `qrystr`, filter values)
- Column values from `InsertRecord` / `UpdateRecord` payloads

Files to touch:

- `ProxyDataSource.ExecutionHelpers.cs` — all catch blocks
- `ProxyDataSource.Routing.cs` — health-check catch + failover catch
- `ProxyDataSource.Transactions.cs` — all catch blocks
- `ProxyDataSource.cs` — catch blocks in read/write ops

Watchdog and Observability messages reference only `dsName` (internal config — not
PII) and may stay as-is.

### Acceptance Criteria

- [ ] `ProxyLogRedactor` exists and test input `"SELECT * FROM Users WHERE email = 'foo@bar.com'"` → `"SELECT * FROM Users WHERE email = [REDACTED]"`
- [ ] `ProxyPolicy.EnableLogRedaction` defaults to `true`; setting `false` bypasses redaction (dev convenience)
- [ ] No `ex.Message` string reaches `AddLogMessage` without passing through `LogSafe`
- [ ] Zero new compilation errors

---

## P1-10 — Audit Trail (Immutable Route-Decision Record per Execution)

### Problem

`ProxyExecutionContext` already collects `List<ProxyAttemptRecord>` and assigns a
`CorrelationId`, but this data is discarded at the end of every
`ExecuteReadWithPolicy` / `ExecuteWriteWithPolicy` call.  There is no way to
reconstruct which datasource handled a given operation, what the latency was, or
whether retries occurred.

### Design

#### 1. `IProxyAuditSink` — swappable write destination

New file: `ProxyDataSource.Audit.cs`

```csharp
namespace TheTechIdea.Beep.Proxy
{
    /// <summary>
    /// Receives a completed <see cref="ProxyAuditEntry"/> after every proxied operation.
    /// Implement to persist to a DB, append-only log file, or telemetry pipeline.
    /// The default <see cref="NullProxyAuditSink"/> discards entries (no overhead).
    /// </summary>
    public interface IProxyAuditSink
    {
        /// <summary>Called synchronously after the operation completes. Must not throw.</summary>
        void Write(ProxyAuditEntry entry);
    }

    /// <summary>No-op default — replaced at construction time to enable auditing.</summary>
    public sealed class NullProxyAuditSink : IProxyAuditSink
    {
        public static readonly NullProxyAuditSink Instance = new();
        public void Write(ProxyAuditEntry entry) { }
    }

    /// <summary>
    /// Appends entries as JSON lines to a rolling daily file.
    /// Thread-safe; uses a dedicated background queue to avoid blocking the data path.
    /// </summary>
    public sealed class FileProxyAuditSink : IProxyAuditSink, IDisposable
    {
        private readonly string                  _directory;
        private readonly BlockingCollection<string> _queue = new(boundedCapacity: 10_000);
        private readonly Thread                  _writer;

        public FileProxyAuditSink(string directory)
        {
            _directory = directory;
            Directory.CreateDirectory(directory);
            _writer = new Thread(DrainLoop) { IsBackground = true, Name = "ProxyAuditWriter" };
            _writer.Start();
        }

        public void Write(ProxyAuditEntry entry)
        {
            try { _queue.Add(System.Text.Json.JsonSerializer.Serialize(entry)); }
            catch (InvalidOperationException) { /* queue disposed */ }
        }

        private void DrainLoop()
        {
            foreach (var line in _queue.GetConsumingEnumerable())
            {
                var path = Path.Combine(_directory, $"proxy-audit-{DateTime.UtcNow:yyyyMMdd}.jsonl");
                File.AppendAllText(path, line + Environment.NewLine);
            }
        }

        public void Dispose()
        {
            _queue.CompleteAdding();
            _writer.Join(timeout: TimeSpan.FromSeconds(5));
            _queue.Dispose();
        }
    }
}
```

#### 2. `ProxyAuditEntry` — new record type (add to `ProxyotherClasses.cs`)

```csharp
public class ProxyAuditEntry
{
    public string        CorrelationId    { get; init; }
    public string        OperationName    { get; init; }
    public string        SelectedSource   { get; init; }   // final winning DS
    public bool          Succeeded        { get; init; }
    public int           TotalAttempts    { get; init; }
    public long          ElapsedMs        { get; init; }
    public DateTime      OccurredAtUtc    { get; init; } = DateTime.UtcNow;
    public string        FailureReason    { get; init; }   // null on success
    public ProxyOperationSafety Safety   { get; init; }
    public List<ProxyAttemptRecord> Attempts { get; init; }
}
```

#### 3. Wiring into `ProxyDataSource`

**Constructor** (`ProxyDataSource.cs`) — add optional parameter:

```csharp
public ProxyDataSource(
    IDMEEditor dmeEditor,
    List<string> dataSourceNames,
    ProxyPolicy policy,
    ICircuitStateStore circuitStateStore = null,
    IProxyAuditSink auditSink = null)   // ← new
{
    ...
    _auditSink = auditSink ?? NullProxyAuditSink.Instance;
    ...
}
```

Add field to `ProxyDataSource.cs`:

```csharp
private IProxyAuditSink _auditSink;
```

**`ExecuteReadWithPolicy`** (`ExecutionHelpers.cs`) — emit on exit:

```csharp
// At the top of the method, record start time (ctx.StartedAt already exists)

// On success return — add before return:
_auditSink.Write(new ProxyAuditEntry
{
    CorrelationId  = ctx.CorrelationId,
    OperationName  = operationName,
    SelectedSource = dsName,
    Succeeded      = true,
    TotalAttempts  = ctx.Attempts.Count,
    ElapsedMs      = (long)(DateTime.UtcNow - ctx.StartedAt).TotalMilliseconds,
    Safety         = ctx.OperationSafety,
    Attempts       = ctx.Attempts
});

// On all-candidates-exhausted throw — add before throw:
_auditSink.Write(new ProxyAuditEntry
{
    CorrelationId  = ctx.CorrelationId,
    OperationName  = operationName,
    SelectedSource = null,
    Succeeded      = false,
    TotalAttempts  = ctx.Attempts.Count,
    ElapsedMs      = (long)(DateTime.UtcNow - ctx.StartedAt).TotalMilliseconds,
    FailureReason  = lastEx?.Message,
    Safety         = ctx.OperationSafety,
    Attempts       = ctx.Attempts
});
```

Apply the same pattern in `ExecuteWriteWithPolicy` and `ExecuteReadWithPolicyAsync`.

#### 4. Update `IProxyDataSource`

```csharp
// Audit
IProxyAuditSink AuditSink { get; set; }   // replaceable at runtime
```

This allows hot-swap (e.g. enable file sink in production without restarting).

### Acceptance Criteria

- [ ] `IProxyAuditSink` and `NullProxyAuditSink` exist; proxy defaults to null sink
- [ ] `FileProxyAuditSink` writes `proxy-audit-YYYYMMDD.jsonl`, one JSON object per line
- [ ] Every `ExecuteReadWithPolicy`, `ExecuteWriteWithPolicy`, and `ExecuteReadWithPolicyAsync` call emits exactly one entry with correct `CorrelationId`, `SelectedSource`, `Succeeded`, and `ElapsedMs`
- [ ] `AuditSink` is settable on `IProxyDataSource` to allow runtime swap
- [ ] Zero new compilation errors; `NullProxyAuditSink` adds no measurable overhead (benchmark: <1 µs per call)

---

## P1-12 — Test Harness (Proxy Fault-Injection Suite)

### Location

New project: `tests/ProxyTests/ProxyDataSource.Tests.csproj`

Framework: **xUnit 2.x** + **Moq 4.x** (already used in the BeepDM test suite).

### Scope

#### Unit tests (`ProxyDataSource.UnitTests/`)

| Test class | Covers |
|------------|--------|
| `CircuitBreakerTests` | Closed→Open transition at threshold; Open→HalfOpen after reset timeout; HalfOpen→Closed after N successes; Critical severity immediately opens |
| `InProcessCircuitStateStoreTests` | `CanExecute`, `RecordSuccess/Failure`, `Initialize`, `Remove`, `ForceOpen`, `Reset` |
| `ProxyLogRedactorTests` | Email, SSN, credit card, quoted value redaction; passthrough when no PII; `EnableLogRedaction=false` disables |
| `ProxyErrorClassifierTests` | Each exception type maps to correct `ProxyErrorCategory` and `ProxyErrorSeverity` |
| `ProxyAuditEntryTests` | Correct field population (CorrelationId, ElapsedMs, Attempts) |

#### Integration / fault-injection tests (`ProxyDataSource.IntegrationTests/`)

These use a **fake `IDataSource`** implemented with Moq (or a hand-rolled stub) that
can be configured to throw, hang, or return bad data.

##### Stub approach

```csharp
public class FakeDataSource : IDataSource
{
    public ConnectionState ConnectionStatus { get; set; } = ConnectionState.Open;
    public Func<object> GetDataBehavior { get; set; } = () => new object();
    public Func<ConnectionState> OpenBehavior { get; set; } = () => ConnectionState.Open;

    public ConnectionState Openconnection() => OpenBehavior();
    public object GetData(string entityName, ref IEnumerable<AppFilter> filter, string squery, bool pager, int pagenum, int pagesize)
        => GetDataBehavior();
    // ... other members throw NotImplementedException by default
}
```

##### Test scenarios

| Test | Setup | Assert |
|------|-------|--------|
| `HappyPath_ReadReturnsData` | 1 source, always succeeds | data returned, 1 audit entry, Succeeded=true |
| `SingleSource_TransientFail_Retries` | 1 source, fails twice then succeeds | returns data on 3rd attempt; audit entry TotalAttempts=3 |
| `AllSources_Fail_ThrowsAggregate` | 2 sources, both always throw `TimeoutException` | `AggregateException` thrown; audit entry Succeeded=false |
| `CircuitOpen_SkipsSource` | source 1 circuit manually tripped open, source 2 healthy | op routes to source 2 without touching source 1 |
| `Saturation_DoublesBackoff` | source returns `HttpRequestException("429")` | `DelayWithBackoff(attempt*2)` called (spy via `Stopwatch`) |
| `BrokenPool_DiscardedAndRefreshed` | pool connection in `Broken` state | `Dispose()` called on broken conn; fresh conn used |
| `WriteRouting_GoesToPrimaryOnly` | 2 sources: one Primary, one Replica | write op uses Primary; read op may use either |
| `Failover_SwitchesToReplica` | Primary throws; Replica healthy | `OnFailover` event fires with correct `FromDataSource`/`ToDataSource` |
| `Watchdog_PromotesReplica` | Primary probe fails `WatchdogFailureThreshold` times | `OnRolePromoted` event fires; Replica becomes Primary |
| `ApplyPolicy_NewTimerInterval` | `ApplyPolicy` called twice | old timer interval is abandoned, new one installed, no second timer |
| `AuditSink_ReceivesEntryPerOperation` | custom `IProxyAuditSink` spy | `Write()` called once per `ExecuteReadWithPolicy` call |
| `LogRedaction_ExceptionMessageScrubbed` | source throws with PII in message | `AddLogMessage` never called with original message |

##### Watchdog integration test pattern

```csharp
[Fact]
public void Watchdog_PromotesReplica_WhenPrimaryFailsThreshold()
{
    // Arrange
    var fakePrimary = new FakeDataSource { ConnectionStatus = ConnectionState.Open };
    fakePrimary.OpenBehavior = () => ConnectionState.Broken;    // always fails probe

    var fakeReplica = new FakeDataSource { ConnectionStatus = ConnectionState.Open };
    fakeReplica.OpenBehavior = () => ConnectionState.Open;

    var dmeEditorMock = new Mock<IDMEEditor>();
    // wire GetDataSource to return the right fake
    dmeEditorMock.Setup(e => e.GetDataSource("primary")).Returns(fakePrimary);
    dmeEditorMock.Setup(e => e.GetDataSource("replica")).Returns(fakeReplica);

    var proxy = new ProxyDataSource(dmeEditorMock.Object,
        new List<string> { "primary", "replica" },
        ProxyPolicy.Default);
    proxy.SetRole("primary", ProxyDataSourceRole.Primary);
    proxy.SetRole("replica", ProxyDataSourceRole.Replica);
    proxy.WatchdogIntervalMs = 100;
    proxy.WatchdogFailureThreshold = 2;

    string promotedName = null;
    proxy.OnRolePromoted += (_, e) => promotedName = e.NewPrimary;

    // Act
    proxy.StartWatchdog();
    Thread.Sleep(700);   // let watchdog fire a few times
    proxy.StopWatchdog();

    // Assert
    Assert.Equal("replica", promotedName);
}
```

### Project file template

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="xunit" Version="2.9.*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.*" />
    <PackageReference Include="Moq" Version="4.20.*" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\DataManagementEngineStandard\DataManagementEngineStandard.csproj" />
  </ItemGroup>
</Project>
```

### Acceptance Criteria

- [ ] All 13 scenario tests pass on `dotnet test`
- [ ] No tests use `Thread.Sleep` for anything other than watchdog timing (and even then use a multiplier so CI can scale with `WatchdogIntervalMs=50`)
- [ ] Code coverage for `Proxy/` folder ≥ 80% (measure via `dotnet test --collect:"Code Coverage"`)
- [ ] Tests are deterministic — no flaky timing dependencies outside the watchdog tests

---

## P2-13 — Write Replication / Fan-Out (Active-Active Dual-Write)

### Problem

Currently `ExecuteWriteWithPolicy` selects exactly one Primary with
`SelectWriteCandidates().FirstOrDefault()`.  In an active-active topology
(e.g. two regional SQL Servers, or primary + warm standby with sync replication
disabled) the caller wants writes to land on **all** Primary-role sources, failing
atomically if any Primary rejects.

### Design

#### 1. New `ProxyWriteMode` enum (add to `ProxyotherClasses.cs`)

```csharp
/// <summary>Controls how writes are distributed across Primary-role datasources.</summary>
public enum ProxyWriteMode
{
    /// <summary>Default. Route write to the first available Primary only.</summary>
    SinglePrimary,

    /// <summary>
    /// Fan-out: attempt write on ALL Primary-role datasources concurrently.
    /// Partial failure is treated as a full failure — see <see cref="ProxyPolicy.WriteFanOutQuorum"/>.
    /// </summary>
    FanOut,

    /// <summary>
    /// Quorum write: succeed if at least <see cref="ProxyPolicy.WriteFanOutQuorum"/> Primaries
    /// acknowledge. Remaining writes are best-effort.
    /// </summary>
    QuorumWrite
}
```

#### 2. Updates to `ProxyPolicy` (add to `ProxyotherClasses.cs`)

```csharp
// Inside ProxyPolicy class
public ProxyWriteMode WriteMode          { get; init; } = ProxyWriteMode.SinglePrimary;
/// <summary>
/// Minimum number of successful Primary writes before the operation is declared successful.
/// Only used when <see cref="WriteMode"/> is <see cref="ProxyWriteMode.QuorumWrite"/>.
/// Defaults to 1 (equivalent to SinglePrimary behaviour).
/// </summary>
public int WriteFanOutQuorum             { get; init; } = 1;
```

#### 3. New partial: `ProxyDataSource.FanOut.cs`

```csharp
namespace TheTechIdea.Beep.Proxy
{
    public partial class ProxyDataSource
    {
        /// <summary>
        /// Executes <paramref name="operation"/> on all Primary targets concurrently.
        /// Returns the result of the first successful Primary; throws if quorum is not met.
        /// </summary>
        private async Task<(bool Success, T Result)> ExecuteFanOutWriteAsync<T>(
            string operationName,
            Func<IDataSource, Task<T>> operation,
            Func<T, bool> successPredicate = null,
            CancellationToken cancellationToken = default)
        {
            var primaries = SelectWriteCandidates();
            if (primaries.Count == 0)
                throw new InvalidOperationException($"[FanOut] No Primary sources available for {operationName}.");

            var tasks = primaries.Select(dsName => ExecuteSingleWriteAsync(dsName, operationName, operation, cancellationToken)).ToList();

            int required = _policy.WriteMode == ProxyWriteMode.QuorumWrite
                ? Math.Max(1, Math.Min(_policy.WriteFanOutQuorum, primaries.Count))
                : primaries.Count;   // FanOut = all must succeed

            var results = await Task.WhenAll(tasks).ConfigureAwait(false);

            var successes = results.Where(r => r.Success).ToList();
            if (successes.Count >= required)
                return successes[0];

            var failures = results.Where(r => !r.Success).Select(r => r.Error).ToList();
            throw new AggregateException(
                $"[FanOut:{operationName}] Only {successes.Count}/{primaries.Count} Primaries succeeded (quorum={required}).",
                failures.Where(e => e != null));
        }

        private async Task<(bool Success, T Result, Exception Error)> ExecuteSingleWriteAsync<T>(
            string dsName,
            string operationName,
            Func<IDataSource, Task<T>> operation,
            CancellationToken cancellationToken)
        {
            var ds = GetPooledConnection(dsName);
            if (ds == null) return (false, default, new InvalidOperationException($"No connection for {dsName}"));

            var sw = Stopwatch.StartNew();
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var result = await operation(ds).ConfigureAwait(false);
                sw.Stop();
                RecordSuccess(dsName, sw.Elapsed);
                ReturnConnection(dsName, ds);
                return (true, result, null);
            }
            catch (Exception ex)
            {
                sw.Stop();
                var (_, severity) = ProxyErrorClassifier.Classify(ex);
                RecordFailure(dsName, severity);
                LogSafe($"[FanOut] Write to {dsName} failed for {operationName}.", ex);
                return (false, default, ex);
            }
        }
    }
}
```

#### 4. Integration into `ExecuteWriteWithPolicy`

At the top of `ExecuteWriteWithPolicy` (and its async variant), branch on the policy:

```csharp
// Fan-out path
if (_policy.WriteMode == ProxyWriteMode.FanOut || _policy.WriteMode == ProxyWriteMode.QuorumWrite)
{
    // Wrap the sync operation in a Task for FanOut
    return await ExecuteFanOutWriteAsync(operationName,
        ds => Task.FromResult(operation(ds)),
        successPredicate,
        CancellationToken.None).ConfigureAwait(false);
}

// Default: SinglePrimary (existing code below unchanged)
```

#### 5. Rollback / compensation (optional — Phase 2-13b)

Fan-out writes are **not transactional** across multiple datasources.  If Primary-2
fails after Primary-1 succeeded, data divergence occurs.  Mitigation options (out of
scope for initial implementation, tracked as 2-13b):

- Record each successful fan-out target in `ProxyAuditEntry.FanOutSucceeded`
- Expose `IDataSource.RollbackLastWrite(correlationId)` hook (requires driver support)
- Alternatively document as "caller must implement saga-style compensation"

#### 6. New test cases for fan-out (add to P1-12 test project)

| Test | Setup | Assert |
|------|-------|--------|
| `FanOut_AllPrimariesSucceed_ReturnsFirst` | 2 Primaries, both succeed | success, both called |
| `FanOut_OnePrimaryFails_ThrowsAggregate` | 2 Primaries, P2 throws | AggregateException |
| `Quorum_HalfSucceed_ReturnsSuccess` | 4 Primaries, quorum=2, 2 succeed | success returned |
| `Quorum_BelowQuorum_Throws` | 4 Primaries, quorum=3, only 1 succeeds | AggregateException |
| `FanOut_CircuitOpen_SkipsThatPrimary` | P1 circuit open, P2 healthy, mode=FanOut | only P2 called; because only 1 of 2 → throws if quorum=all |

### Acceptance Criteria

- [ ] `ProxyWriteMode.SinglePrimary` (default) behaves identically to current code — no regression
- [ ] `FanOut` calls all Primaries concurrently via `Task.WhenAll`; any single failure → `AggregateException`
- [ ] `QuorumWrite` with `WriteFanOutQuorum=N` succeeds if N or more Primaries succeed
- [ ] `ProxyAuditEntry` records which Primaries succeeded in fan-out mode (new `FanOutSucceeded : List<string>` field)
- [ ] All four fan-out tests pass
- [ ] Zero new compilation errors; no change in behaviour when `WriteMode = SinglePrimary`

---

## Implementation Order Recommendation

```
P1-9  (Log Redaction)  →  low risk, self-contained, do first
P1-10 (Audit Trail)    →  medium — builds on ProxyExecutionContext already in place
P2-13 (Fan-Out)        →  medium — new partial file, branches existing write path
P1-12 (Test Harness)   →  last — validates all three above
```

Each item can be done in isolation; none blocks another except the test harness
(P1-12) which covers all of them.
