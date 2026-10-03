# Phase 4: Performance And Observability

NFEL-1 history bounds are now locally verified under P4-07: 128 structures,
1,048,576 source code units and 32,768 tokens; private views and synchronized
Clear/parse preserve active evaluations. Source/token/node/depth caps also apply.
This is not a process-wide memory benchmark, all-history retention policy or plugin
lifetime qualification. P4-07 remains unchecked; see Engine Rules/NFEL.md.

Status: proposed. Finding: F09. Depends on Phase 3 capability contracts.

## Work Items

- [ ] P4-01 Benchmark OBL change tracking, UOW commit, import, sync, and plugin
  startup. Record workload, provider, hardware, allocations, and baseline timings.
- [ ] P4-02 Add optional async/paging/streaming datasource interfaces with
  CancellationToken; keep legacy adapters and document their limitations.
- [ ] P4-03 Replace whole-source materialization with bounded producer/consumer
  batches. Carry backpressure through source, transforms, sink, and error handling.
- [ ] P4-04 Use native bulk writes only when supported, with per-record/partial
  acknowledgement semantics and explicit transactional boundaries.
- [ ] P4-05 Add correlation/run IDs, structured logs, spans, and low-cardinality
  counters for attempted, committed, failed, skipped, retried, and cancelled work.
  Redact credentials and avoid row payloads in default telemetry.
- [ ] P4-06 Report checkpoint/recovery progress distinctly from heuristic percent
  estimates. Expose actionable diagnostics for stalled and partially applied runs.
- [ ] P4-07 Bound error-history retention and examine cache/registry lifetime.
  Provide predictable limits for long-lived desktop and hosted processes.
- [ ] P4-08 Extend baseline workloads to large Forms/master-detail datasets,
  validation/trigger chains, helper resolution and repeated plugin load/unload.
  Measure allocations, timer/subscription retention and UI dispatch latency.
  Optimize only demonstrated bottlenecks; preserve dirty-state/UOW semantics.

## Verification And Acceptance

- For streaming-capable providers, peak retained memory is governed by configured
  buffer/batch bounds rather than total source row count. The first target write
  occurs before the entire source is read.
- Measure cancellation latency; distinguish interruptible async providers from
  legacy calls that can only observe cancellation between operations.
- Publish before/after measurements for representative workloads. Set numerical
  targets after establishing a baseline, not arbitrary throughput promises.
- Telemetry reconciles with acknowledged results and cannot imply success after
  rollback. Diagnostic output contains no credentials or sensitive row data.
- Bound the full source/transform/sink path, including retry/error buffers and
  durable-history rewrites. Measure increasing source sizes at fixed configured
  bounds; nominal batch size alone is not a memory guarantee.
- Include NFEL parser structure retention and repeated plugin lifecycle workloads
  in long-lived-process measurements; any eviction policy must preserve active
  operations and documented diagnostic access.

Next: [release and developer experience](PHASE-05-Release-And-Developer-Experience.md).
