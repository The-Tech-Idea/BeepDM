# ETL Phase 7 Implementation Tasks

This file breaks down **Phase 7** from `ETL_INTEGRATION_ENHANCEMENT_PLAN.md` into concrete performance and async cleanup tasks.

## Phase 7 Goal
- Remove sync-over-async bottlenecks.
- Improve ETL throughput and responsiveness for large copy/import workloads.
- Keep correctness and compatibility while optimizing internals.

## Files In Scope
- `DataManagementEngineStandard/Editor/ETL/ETLEditor.cs`
- `DataManagementEngineStandard/Editor/ETL/ETLDataCopier.cs`
- `DataManagementEngineStandard/Editor/ETL/ETLEntityCopyHelper.cs`
- `DataManagementEngineStandard/Editor/ETL/ETLScriptManager.cs`
- related extension/helper usage for batching and type normalization

---

## Workstream A - Remove Sync-over-Async Patterns

### A1. Identify and eliminate blocking waits
- [ ] Remove blocking calls such as:
  - [ ] `.Wait()`
  - [ ] `.Result`
  - [ ] `.GetAwaiter().GetResult()`
  in ETL hot paths.

### A2. Convert methods to fully async flow
- [ ] Ensure call chain from run entrypoints to fetch/transform/insert is async.
- [ ] Add/propagate `async` method signatures internally where needed.
- [ ] Preserve public API signatures where required, using safe internal async bridges only when unavoidable.

### A3. Cancellation propagation integrity
- [ ] Ensure `CancellationToken` is passed and checked in all async operations.
- [ ] Avoid background tasks that ignore cancellation.

---

## Workstream B - Data Fetch and Shape Normalization Optimization

### B1. Normalize source data with typed checks
- [ ] Replace brittle string-based type checks with robust typed checks where possible.
- [ ] Standardize conversion paths for:
  - [ ] `DataTable`
  - [ ] `IEnumerable<object>`
  - [ ] binding list variants

### B2. Minimize unnecessary allocations
- [ ] Avoid repeated list cloning in tight loops.
- [ ] Reuse buffers/collections where safe.
- [ ] Avoid creating transient objects on every record when not needed.

### B3. Streaming-friendly processing
- [ ] Prefer enumerable streaming and batch chunking over full in-memory materialization when feasible.
- [ ] Add safeguards for very large entity datasets.

---

## Workstream C - Batch Insert Throughput Tuning

### C1. Batch strategy controls
- [ ] Centralize default batch size values and bounds.
- [ ] Add adaptive strategy option (small/medium/large datasets).
- [ ] Avoid extremely high parallelism defaults that can overwhelm providers.

### C2. Parallelism guardrails
- [ ] Add max degree of parallelism controls.
- [ ] Use provider-aware safe defaults for parallel insert mode.
- [ ] Ensure deterministic behavior when parallel mode disabled.

### C3. Retry path efficiency
- [ ] Ensure retries target only failed records.
- [ ] Add exponential or bounded retry delay strategy when needed.
- [ ] Prevent runaway recursive retry patterns.

---

## Workstream D - Constraint and Transaction Cost Management

### D1. FK constraint toggling efficiency
- [ ] Avoid repeated disable/enable calls inside record loops.
- [ ] Scope FK toggles at entity/batch boundary.

### D2. Transaction strategy for performance
- [ ] Use transaction scope at sensible batch/entity granularity where supported.
- [ ] Balance commit frequency for throughput vs rollback safety.

### D3. Provider capability checks
- [ ] Use helper capability checks before enabling transaction/parallel optimizations.
- [ ] Fall back safely for providers with limited support.

---

## Workstream E - Logging and Progress Overhead Reduction

### E1. Reduce chatty logging in hot loops
- [ ] Throttle record-level logs.
- [ ] Emit periodic progress checkpoints instead of per-record messages.

### E2. Lightweight progress reporting
- [ ] Ensure progress event frequency is bounded.
- [ ] Keep progress payload minimal in high-frequency paths.

### E3. Toggle diagnostic verbosity
- [ ] Add internal verbosity option for deep diagnostics vs normal run mode.

---

## Workstream F - Memory and Resource Safety

### F1. Avoid long-lived temporary object growth
- [ ] Clear/dispose large temporary structures promptly.
- [ ] Prevent retention of large lists after step completion.

### F2. Resource lifecycle checks
- [ ] Ensure disposable resources are released deterministically.
- [ ] Avoid hidden task/resource leaks in async operations.

### F3. Backpressure and failure containment
- [ ] Add safeguards to avoid memory spikes under high error rates.
- [ ] Ensure failed batches are isolated and do not poison entire run state.

---

## Workstream G - Benchmarking and Performance Validation

### G1. Establish baseline
- [ ] Capture baseline metrics for representative ETL scenarios:
  - [ ] rows/sec
  - [ ] total duration
  - [ ] memory usage
  - [ ] error/retry counts

### G2. Compare optimized path
- [ ] Re-run same scenarios after async/performance changes.
- [ ] Record deltas and regressions.

### G3. Define acceptance thresholds
- [ ] Set minimum acceptable improvements (or no-regression thresholds).
- [ ] Block rollout if key workloads regress significantly.

---

## Workstream H - Regression and Reliability Tests

### H1. Correctness under optimization
- [ ] Verify record counts and mapped values remain correct after optimization.
- [ ] Verify retries and cancellation still behave correctly.

### H2. Stress tests
- [ ] Run large dataset stress scenarios for copy/import.
- [ ] Validate no deadlocks/thread starvation under parallel mode.

### H3. Compatibility checks
- [ ] Ensure existing ETL script executions still function with default settings.
- [ ] Ensure fallback behavior remains available if optimized path is disabled.

---

## Suggested Implementation Order
1. A1 -> A2 -> A3 (async correctness first)
2. B1 -> B2 -> B3 (fetch/shape optimization)
3. C1 -> C2 -> C3 (batch/parallel/retry tuning)
4. D1 -> D2 -> D3 (constraints/transaction tuning)
5. E1 -> E2 -> E3 (overhead reduction)
6. F1 -> F2 -> F3 (resource safety)
7. G1 -> G2 -> G3 (benchmark validation)
8. H1 -> H2 -> H3 (reliability and compatibility)

---

## Definition of Done (Phase 7)
- [ ] Sync-over-async anti-patterns are removed from ETL hot paths.
- [ ] Throughput and responsiveness are improved or at least non-regressed.
- [ ] Cancellation, retries, and correctness remain stable.
- [ ] Memory/resource behavior is safe under stress.
- [ ] Benchmark and reliability tests pass.
