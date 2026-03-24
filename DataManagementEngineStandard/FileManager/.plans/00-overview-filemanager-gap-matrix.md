# 00 - Overview: FileManager Gap Matrix

## Objective
Baseline current `FileManager` capabilities and define phased enhancements for reliability, scale, quality, and enterprise operations.



## Gap Matrix

| Capability | Current State | Gap | Target |
|---|---|---|---|
| Contracts/readers | Reader interface + implementation exist | Column mapping/name/ordinal behavior can diverge when projections are used | Explicit reader contract semantics and invariant tests |
| CSV correctness | Custom parser supports quotes/escapes | Header parsing in some paths uses naive `Split`, and parser/writer concerns are mixed | Unified parser usage and strict/lenient parsing profiles |
| Schema inference | Analyzer exists | Type inference and uniqueness/null heuristics are simplistic and sometimes unstable | Confidence-scored inference + robust heuristics |
| Large files | Streaming reader exists | Multiple paths still load whole file into memory (`Insert/BulkInsert`) | Streaming-first mutation/query strategy |
| Error quality | Logs exist | Many catch blocks swallow exceptions and lose row/column context | Structured diagnostics with row/column/op metadata |
| Query correctness | Filter and paging APIs exist | Header-to-field index mapping can mis-resolve when key defaults to 0 | Deterministic field resolution and filter correctness |
| Transaction semantics | Begin/Commit/Rollback methods exist | Writes are immediate; transaction state is snapshot-only and not true atomic unit | Explicit transaction contract or remove misleading semantics |
| Validation/safety | Row validation helper exists | Validation not consistently wired into all write paths | Mandatory validation pipeline with policy modes |
| Security/governance | Basic file path usage | No path policy, masking, or audit profile | Policy-driven file access and masking/audit controls |
| Rollout operations | Basic logging | No KPI-based rollout/canary controls | Observable rollout with hard KPI gates |

## Concrete Code Constraints
- `GetFieldsbyTableScan` loop uses wrong counter variables (`i` vs `findex`) and risky condition logic.
- `GetEntityStructure`/`GetEntityType` contain `Entities.Count == 0` then `Entities[0]` patterns.
- `ValidateCSVHeaders` uses `Split(Delimiter)` and ignores quoted delimiters.
- `CSVTypeMapper.ConvertValue` enum parse path can throw and is not guarded.
