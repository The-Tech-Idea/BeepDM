# Phase 8 (Enhanced) — Performance, Scale, and Compiled Integration Caching

## Supersedes
`../08-phase8-performance-and-scale-strategy.md`

## Objective
Maximize sync throughput by caching compiled `MappingManager` plans across batches,
caching `EntityDefaultsProfile` per schema pair, and setting lightweight `RuleExecutionPolicy`
profiles for high-volume paths to minimise rule evaluation overhead.

---

## Scope
- Compiled mapping plan cache reuse across batches and parallel sync tasks.
- Cached `EntityDefaultsProfile` per (ds, entity) pair.
- `RuleExecutionPolicy` performance profiles: `DefaultSafe` vs `FastPath`.
- Parallel sync coordination via `SemaphoreSlim` and batch partitioning.
- Throughput benchmarks per integration vector.

---

## File Targets

| File | Change Description |
|---|---|
| `BeepSync/BeepSyncManager.Orchestrator.cs` | Add batch partitioner; share compiled plan + defaults profile across tasks |
| `BeepSync/Helpers/SyncSchemaTranslator.cs` | Accept pre-compiled plan; skip re-compilation when cache hit |
| `BeepSync/Helpers/FieldMappingHelper.cs` | Add `EnsureCompiledPlan(...)` helper with thread-safe cache lookup |
| `BeepSync/Models/SyncPerformanceProfile.cs` *(new)* | Performance knobs: batch size, parallelism, rule policy mode, cache TTL |

---

## Integration Points: Mapping Manager

### 1. Compile-Once, Reuse-Across-Batches Pattern
Before the sync run starts, compile the mapping plan and hold it for all batch iterations:
```csharp
// Run startup — executed once
var compiledPlan = _mappingPlanCache.GetOrAdd(
    CacheKey(schema),
    _ => MappingManager.GetOrCompileMappingPlan(
        schema.SourceDataSourceName,
        schema.DestinationDataSourceName,
        schema.SourceEntityName));

// Per-batch — re-uses the same compiled plan
Parallel.ForEach(batches, new ParallelOptions { MaxDegreeOfParallelism = schema.PerfProfile?.MaxParallelism ?? 4 },
    batch =>
    {
        var mappedRecords = batch
            .Select(r => MappingManager.MapObjectUsingCompiledPlan(r, compiledPlan))
            .ToList();
        ...
    });
```

### 2. Mapping Plan Cache Invalidation
Invalidate the cache when mapping governance version changes:
```csharp
MappingManager.MappingUpdated += (sender, e) =>
{
    var key = CacheKey(e.SourceDs, e.DestDs, e.EntityName);
    _mappingPlanCache.TryRemove(key, out _);
    _logger.LogInformation("Mapping plan cache invalidated for {Key}", key);
};
```

### 3. Lightweight Field Accessor Compilation
Use `MappingManager.PerformanceCaching.cs` facilities for accessor cache warm-up at
schema registration time (not at first sync run):
```csharp
// Called when schema is registered with BeepSyncManager
MappingManager.WarmUpAccessorCache(
    schema.SourceDataSourceName, schema.SourceEntityName,
    schema.DestinationDataSourceName, schema.DestinationEntityName);
```

---

## Integration Points: Defaults Manager

### 1. Cached `EntityDefaultsProfile`
The `EntityDefaultsProfile` for a schema pair is loaded once at sync start and shared across
all batches — avoid per-record profile lookups:
```csharp
var defaultsProfile = _defaultsProfileCache.GetOrAdd(
    $"{schema.DestinationDataSourceName}.{schema.DestinationEntityName}",
    _ => DefaultsManager.GetProfile(
        schema.DestinationDataSourceName, schema.DestinationEntityName));

// Per-record application uses the cached profile:
DefaultsManager.ApplyFromProfile(defaultsProfile, mappedRecord, context);
```

### 2. Profile Cache TTL
Profiles are invalidated when `DefaultsManager` fires a `ProfileUpdated` event or when
cache TTL (default: 5 minutes) expires:
```csharp
_defaultsProfileCache = new MemoryCache<string, EntityDefaultsProfile>(
    ttl: schema.PerfProfile?.DefaultsCacheTtlSeconds ?? 300);
```

### 3. Batch-Level Defaults (Skip Per-Record When All Same)
For fields where the default is a static literal (not an expression), pre-compute and
embed the value once per batch:
```csharp
// If "Status" default = "Active" (literal), apply as a batch-level constant
var staticDefaults = defaultsProfile.Defaults
    .Where(d => !d.IsRule)
    .ToDictionary(d => d.FieldName, d => d.Value);

// Per-record: just merge staticDefaults into mappedRecord (no resolver invoked)
foreach (var (field, value) in staticDefaults)
    mappedRecord.TryAdd(field, value);

// Dynamic expressions (:NOW, :USERNAME, :NEWGUID) still need per-record resolution
var dynamicDefaults = defaultsProfile.Defaults.Where(d => d.IsRule).ToList();
```

---

## Integration Points: Rule Engine

### 1. Performance Rule Policy Profiles
Define two `RuleExecutionPolicy` profiles for sync use:

```csharp
public static class SyncRuleExecutionPolicies
{
    /// <summary>
    /// Safe profile for critical/standard sync paths.
    /// Includes full depth and lifecycle checks.
    /// </summary>
    public static readonly RuleExecutionPolicy DefaultSafe = new RuleExecutionPolicy
    {
        MaxDepth = 10,
        AllowDeprecatedExecution = false,
        EnforceLifecycleMinimum = true
    };

    /// <summary>
    /// Fast profile for high-volume non-critical DQ checks.
    /// Reduced depth — no lifecycle enforcement overhead.
    /// </summary>
    public static readonly RuleExecutionPolicy FastPath = new RuleExecutionPolicy
    {
        MaxDepth = 3,
        AllowDeprecatedExecution = false,
        EnforceLifecycleMinimum = false
    };
}
```

Schema selects via `SyncRulePolicy.PerformanceMode`:
- `"Safe"` → `SyncRuleExecutionPolicies.DefaultSafe`
- `"FastPath"` → `SyncRuleExecutionPolicies.FastPath`

### 2. Rule Evaluation Skip for Pure-Literal Paths
If a sync batch has no conflicts, no DQ failures in previous batches, and
`schema.PerfProfile.SkipRulesOnCleanBatch = true`, skip DQ rule evaluation:
```csharp
if (!schema.PerfProfile?.SkipRulesOnCleanBatch == true || previousBatchHadFailures)
    EvaluateDqGateRules(mappedRecord, schema);
// else: skip rule evaluation for this record (fast path)
```

---

## `SyncPerformanceProfile` Model

```csharp
public class SyncPerformanceProfile
{
    public int    BatchSize                   { get; set; } = 1000;
    public int    MaxParallelism              { get; set; } = 4;
    public string RulePolicyMode              { get; set; } = "Safe";  // "Safe" | "FastPath"
    public int    DefaultsCacheTtlSeconds     { get; set; } = 300;
    public bool   WarmUpAccessorCacheOnLoad   { get; set; } = true;
    public bool   SkipRulesOnCleanBatch       { get; set; } = false;
    public bool   UseParallelBatches          { get; set; } = true;
    public int    ParallelBatchQueueDepth      { get; set; } = 8;
}
```

---

## Expected Throughput Impact (Targets)

| Optimisation | Expected Gain |
|---|---|
| Compiled mapping plan reuse | 40–60% reduction in per-record field mapping time |
| Cached `EntityDefaultsProfile` | Eliminates profile lookup on every record |
| Static literal defaults batched upfront | Removes resolver call for non-expression fields |
| `FastPath` rule policy | ~30% rule evaluation speed-up for non-critical DQ checks |
| Parallel batches (MaxParallelism = 4) | Near-linear scaling on multi-core for independent entity pairs |

---

## Acceptance Criteria
- Mapping plan is compiled once per schema pair per run; not per-record or per-batch.
- `EntityDefaultsProfile` is loaded once and cached for TTL seconds.
- Static literal defaults are batch-applied without resolver invocation.
- `FastPath` rule policy is used on high-volume non-critical paths when configured.
- `MappingManager.MappingUpdated` event invalidates the plan cache correctly.
- Parallel batch execution is safe (no shared mutable state between batch tasks).
