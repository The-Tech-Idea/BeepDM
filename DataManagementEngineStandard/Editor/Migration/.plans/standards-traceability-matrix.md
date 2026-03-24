# MigrationManager Standards Traceability Matrix

## Purpose
Map migration best-practice expectations to phased artifacts and MigrationManager code touchpoints.

| Best-Practice Capability | Phase(s) | Primary Targets |
|---|---|---|
| Plan-first migration workflow | 1, 4, 9 | `IMigrationManager.cs`, `MigrationManager.cs` |
| Safety policy and destructive-change gating | 2 | `MigrationManager.cs` |
| Cross-provider portability planning | 3 | `MigrationManager.cs`, `README.md` |
| Dry-run/preflight before apply | 4 | `MigrationManager.cs` |
| Resumable execution and checkpointing | 5 | `MigrationManager.cs` |
| Rollback and compensation readiness | 6 | `MigrationManager.cs`, `README.md` |
| Observability and audit trail | 7 | `MigrationManager.cs`, `IMigrationManager.cs` |
| Scale and lock-aware execution | 8 | `MigrationManager.cs` |
| CI/CD automation and release evidence | 9 | `README.md`, `MigrationManager.cs` |
| Governance rollout with KPI gates | 10 | `README.md`, operational artifacts |

## Traceability Rule
- Every migration enhancement PR should include:
  - phase ID,
  - standards row mapping,
  - impacted files,
  - migration risk note.
