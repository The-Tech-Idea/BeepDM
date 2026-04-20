# Phase 05 — Redaction, PII & Secret Scrubbing

## Objective

Provide a reusable, composable redaction layer that runs **inside the pipeline before any sink ever sees the envelope**, with built-in patterns for the most common sensitive data and an extension point for custom redactors per sink.

## Dependencies

- Phase 02 pipeline.
- `Proxy/ProxyLogRedactor.cs` (existing pattern source).

## Scope

- **In**: `IRedactor` chain, default redactors, structured-property redactor, per-sink stacks, bridge over the existing `ProxyLogRedactor`.
- **Out**: Encryption (v2), tokenization (v2).

## Target files

```
Services/Telemetry/Redaction/
  IRedactor.cs
  RedactionContext.cs
  RegexRedactor.cs
  KeywordRedactor.cs
  KeyValueRedactor.cs
  StructuredFieldRedactor.cs
  ConnectionStringRedactor.cs
  CreditCardRedactor.cs
  EmailRedactor.cs
  JwtRedactor.cs
  CompositeRedactor.cs
  DefaultRedactionPresets.cs
  ProxyRedactorAdapter.cs            // bridges Proxy/ProxyLogRedactor
```

## Design notes

### Contract

```csharp
public interface IRedactor
{
    string Name { get; }
    void Redact(TelemetryEnvelope envelope, RedactionContext ctx);
}
```

`RedactionContext` carries opt-in mode (`Mask | Hash | Drop`) and a default replacement token (e.g. `"***"` or `"[REDACTED:JWT]"`).

### Application order

Redactors run in registration order, *after* enrichers and *before* sampling/dedup. Both `IBeepLog` and `IBeepAudit` go through the same chain; **audit gets a stricter preset by default** (`Hash` mode for PII keys vs `Mask` for logs) so audit can still join records by stable hash without leaking the value.

### Built-in redactors

| Redactor | Targets |
|---|---|
| `ConnectionStringRedactor` | `password=`, `pwd=`, `account_key=`, `secret=` (extends the proxy pattern) |
| `CreditCardRedactor`       | Luhn-checked digit groups |
| `EmailRedactor`            | `[a-z0-9.+-]+@[a-z0-9.-]+\.[a-z]{2,}` (lowercase folded) |
| `JwtRedactor`              | `eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+` |
| `KeywordRedactor`          | Configurable list, exact word match |
| `KeyValueRedactor`         | `key=value` style, with key allowlist/denylist |
| `StructuredFieldRedactor`  | Operates on `envelope.Properties` by key (e.g. `"Email"`, `"Phone"`) |
| `RegexRedactor`            | Operator-supplied pattern + replacement |

### Per-sink redactor stacks

Redactors can be **global** (applied once, in pipeline) or **per-sink** (applied just before that sink writes). Audit sinks frequently want stricter rules than log sinks.

```csharp
opt.AddGlobalRedactor(new ConnectionStringRedactor());
opt.AddSinkRedactor("audit-sqlite", new StructuredFieldRedactor(new[]{"Ssn","Phone"}, mode: RedactionMode.Hash));
```

### Bridge to Proxy

`ProxyRedactorAdapter` wraps `Proxy/ProxyLogRedactor` so existing proxy callers keep working, and the same redaction can be reused by `IBeepLog` without copy-pasting patterns.

### `DefaultRedactionPresets`

Three presets:

- `Off` — empty list.
- `LogsBalanced` — `ConnectionString`, `Jwt`, `Password` keyword. Mode = `Mask`.
- `AuditStrict`   — adds `CreditCard`, `Email`, `Phone`, `Ssn`. Mode = `Hash` for PII keys, `Mask` for tokens.

## Implementation steps

1. Add `IRedactor` + `RedactionContext` + `RedactionMode`.
2. Add the seven concrete redactors and `CompositeRedactor`.
3. Add `DefaultRedactionPresets` (no behavior change unless operator chooses it).
4. Add `ProxyRedactorAdapter`; deprecate direct construction of `ProxyLogRedactor` in new code (existing code untouched).
5. Wire global + per-sink redactor execution in `TelemetryPipeline`.
6. Tests: every built-in pattern + property-key redaction + per-sink isolation.

## TODO checklist

- [ ] P05-01 `IRedactor.cs`, `RedactionContext.cs`, `RedactionMode.cs`.
- [ ] P05-02 Built-in redactors (one file per class).
- [ ] P05-03 `CompositeRedactor.cs`, `DefaultRedactionPresets.cs`.
- [ ] P05-04 `ProxyRedactorAdapter.cs`.
- [ ] P05-05 Pipeline integration (global + per-sink).
- [ ] P05-06 Tests for every built-in + presets + per-sink stack.

## Verification

- An envelope containing `password=secret123;Server=...` is masked to `password=***;Server=...` when `LogsBalanced` is on.
- An envelope with `Properties["Ssn"] = "123-45-6789"` becomes a SHA-256 hex digest under `AuditStrict`.
- Disabling redaction keeps allocations within ±1% of baseline.
- Existing `Proxy/FileProxyAuditSink` callers see no behavior change.

## Risks

- **R1**: Regex over a high-throughput log path is expensive. Mitigation: redactors short-circuit on `Message == null`; per-redactor opt-in; document costs.
- **R2**: Hashing PII at audit time prevents lookup by raw value. Mitigation: provide a documented `HashSalt` per environment so operators can replay queries with the same hash.
- **R3**: Operator forgets to enable redaction. Mitigation: `LogsBalanced` recommended in extension method docs; warning logged once on startup if logging is enabled with zero redactors.
