# Phase 07 — Sampling, Deduplication & Rate Limiting

## Objective

Keep the **log volume bounded** so storage budget and CPU stay healthy under noisy code paths. Add three independent, opt-in mechanisms that the operator can layer: sampling (level-aware drop ratio), deduplication (collapse identical messages), and per-source rate limiting (token bucket).

> Audit traffic **bypasses all three** by contract.

## Dependencies

- Phase 02 (pipeline pre-queue stage).
- Phase 06 (enrichers populate `category` / `correlationId` used by the deduper).

## Scope

- **In**: Three components plus pipeline wiring and metrics counters.
- **Out**: Adaptive sampling (v2 — Phase 14 stub).

## Target files

```
Services/Telemetry/Sampling/
  ISampler.cs                        // canonical (was forward-decl)
  LevelSampler.cs
  CategorySampler.cs
  AlwaysSampler.cs
  NeverSampler.cs

Services/Telemetry/Dedup/
  IMessageDeduper.cs
  WindowedDeduper.cs                 // partial: .Core, .Window
  WindowedDeduper.Core.cs
  WindowedDeduper.Window.cs

Services/Telemetry/RateLimit/
  ITokenBucketRateLimiter.cs
  TokenBucketRateLimiter.cs
  RateLimitedCategoryFilter.cs
```

## Design notes

### `ISampler`

```csharp
public interface ISampler
{
    string Name { get; }
    bool ShouldSample(TelemetryEnvelope envelope);
}
```

Pipeline calls samplers in registration order. First `false` short-circuits and the envelope is dropped (counter incremented).

### Built-in samplers

| Sampler | Behavior |
|---|---|
| `LevelSampler(level, rate)`        | Sample `rate ∈ [0,1]` of envelopes at level `<= level`; always keep above. |
| `CategorySampler(category, rate)`  | Per-category rate. |
| `AlwaysSampler` / `NeverSampler`   | Pinning helpers for tests and explicit suppression. |

Sampling is **deterministic** when the envelope carries a `correlationId` (hash to 0..1) so a single trace stays whole or wholly dropped. Falls back to `Random` when no correlationId is present.

### `WindowedDeduper`

Collapses identical `(level, category, message-template)` within `Window` seconds into a single envelope with property `dedup.count = N`.

- Identity: `xxhash(level | category | message-template)`.
- Eviction: `LRU + max items` cap so memory is bounded (default 1024 keys).
- After window expiry, a synthetic envelope `"[dedup] N occurrences of: <template>"` is emitted.

`message-template` is the original message with numbers / GUIDs / quoted strings replaced by `{n}`/`{g}`/`{s}` placeholders. This is computed cheaply once per message.

### `TokenBucketRateLimiter`

Per-key (default key is `category`). Refills at `RefillPerSecond`, capped at `Burst`. Rejected envelopes increment a per-key drop counter and emit a single `"[rate-limited] category=X for window=Y"` summary every 30 s.

### Pipeline order (locked)

```
Producer → Enrichment → Redaction → Sampler → Deduper → RateLimiter → Queue → Sinks
                                       ↑           ↑          ↑
                                    Audit envelopes BYPASS these three stages.
```

This ordering means redacted PII is what the deduper sees (so identical bodies after redaction collapse together), and rate limiting is the final pre-queue gate.

### Operator surface

```csharp
opt.AddSampler(new LevelSampler(LogLevel.Debug, rate: 0.05));
opt.AddSampler(new CategorySampler("HotLoop", rate: 0.01));
opt.SetDeduper(new WindowedDeduper(window: TimeSpan.FromSeconds(10), maxKeys: 1024));
opt.SetRateLimiter(new TokenBucketRateLimiter(refillPerSecond: 200, burst: 1000, keyBy: env => env.Category));
```

## Implementation steps

1. Add `ISampler`, three built-ins.
2. Add deduper with template normalization (small helper class `MessageTemplateNormalizer`).
3. Add token bucket rate limiter.
4. Insert the three stages into the pipeline in the locked order.
5. Add a hard guard so audit envelopes skip the entire stage block.
6. Per-stage drop counters in `PipelineMetrics` (Phase 11 will surface them).
7. Tests covering: deterministic sampling per correlationId, dedup eviction, burst then refill.

## TODO checklist

- [ ] P07-01 `ISampler.cs` + four built-ins.
- [ ] P07-02 `IMessageDeduper.cs` + `WindowedDeduper.{Core,Window}.cs` + `MessageTemplateNormalizer.cs`.
- [ ] P07-03 `ITokenBucketRateLimiter.cs` + `TokenBucketRateLimiter.cs`.
- [ ] P07-04 Pipeline wiring + audit bypass guard.
- [ ] P07-05 Drop counters per stage.
- [ ] P07-06 Tests for each stage + property-based test for sampler determinism.

## Verification

- 1 M debug logs → with `LevelSampler(Debug, 0.05)` exactly ~50k reach sinks (±1%).
- 100 identical errors in 1 s → 1 envelope with `dedup.count = 100` after window expires.
- Burst > token bucket → exact `Burst` envelopes pass before any drop.
- All audit events pass straight through regardless of sampler/deduper/rate limiter config.

## Risks

- **R1**: Template normalization is a hot path. Mitigation: normalize once and cache by `string.GetHashCode()`; small per-thread buffer; opt-out switch.
- **R2**: A misconfigured sampler could drop important `Error`-level logs. Mitigation: documentation rule — `LevelSampler` only drops at or below configured level; defaults stop at `Debug`.
