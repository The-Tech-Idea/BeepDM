# ETL Validation Coverage Matrix

This matrix validates ETL integration changes from Phases 1-7 before broader rollout.

## Mandatory Scenarios

| Scenario | Entry Point | Expected Result | Gate |
|---|---|---|---|
| Create-only ETL | `RunCreateScript(copydata:false)` | Entities ensured, no data copy attempted | Blocking |
| Copy-only ETL | `RunCreateScript` with `CopyData` details | Data copied with coherent counts | Blocking |
| Create+Copy ETL | `RunCreateScript(copydata:true)` | Schema + data complete, summary emitted | Blocking |
| Mapping import bridge | `RunImportScript` | Importing bridge path succeeds or clean fallback | Blocking |
| Migration schema update | `RunCreateScript` with existing entity delta | Missing columns added when policy allows | Blocking |
| Script save/load | `SaveETL` then `LoadETL` | Canonical load first, legacy fallback valid | Blocking |
| Cancellation/stop | both run paths with token/threshold | Deterministic stop and final summary | Blocking |

## Provider Coverage Set

| Provider Class | Category | Why Included | Gate |
|---|---|---|---|
| Strict relational provider (e.g. SQL Server-style) | RDBMS | Validates FK/DDL strictness | Blocking |
| In-memory provider (e.g. DuckDB/InMemoryRDB) | In-memory/permissive | Validates fast-path and compatibility | Blocking |
| Limited-feature provider | File/embedded or constrained provider | Validates graceful capability fallback | Blocking |

## Quality Gates

| Gate | Pass Criteria | Evidence |
|---|---|---|
| Correctness | Expected entities, fields, and row counts match | Run logs + destination verification |
| Regression | Existing ETL callers run without API changes | Existing integration flow execution |
| Performance non-regression | Throughput and duration are not materially worse | Before/after run timing snapshot |
| Telemetry completeness | Correlation id + final summary present | `LoadDataLogs` + log category traces |

## Failure-Mode Validation

| Mode | Injection | Expected Behavior |
|---|---|---|
| Preflight fail | Invalid mapping/schema mismatch | ETL fails early with actionable message |
| Transient write fail | Temporary insert exception | Retry/fallback behavior is explicit |
| Stop threshold | Force repeated errors | `RunStopped` event + summary |
| Cancellation | Cancel token mid-run | Terminal cancellation event + summary |

## Sign-off Checklist

- [ ] All blocking scenarios passed on selected providers.
- [ ] Correctness and regression gates passed.
- [ ] Performance non-regression gate passed.
- [ ] Telemetry completeness gate passed.
- [ ] Fallback/rollback behavior verified.
