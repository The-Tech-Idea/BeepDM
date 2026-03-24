# MigrationManager Focused Gap Matrix (Lines 8-12 Scope)

## Objective
Define a detailed enhancement plan for the five already-present MigrationManager capabilities so they become enterprise-grade, measurable, and rollout-safe.

## Current Baseline (Observed)
- Explicit-type migration APIs exist:
  - `EnsureDatabaseCreatedForTypes(...)`
  - `ApplyMigrationsForTypes(...)`
- Discovery migration APIs exist:
  - `DiscoverEntityTypes(...)`
  - `EnsureDatabaseCreated(...)`
  - `ApplyMigrations(...)`
- Summary/readiness exists:
  - `GetMigrationSummary(...)`
  - `GetMigrationReadiness(...)`
  - `GetMigrationReadinessForTypes(...)`
  - `BuildReadinessReport(...)`
- Entity-level DDL operations exist:
  - `CreateEntity`, `DropEntity`, `RenameEntity`
  - `AddColumn`, `DropColumn`, `RenameColumn`, `AlterColumn`
  - `CreateIndex`
- Assembly registration exists:
  - `RegisterAssembly(...)`, `RegisterAssemblies(...)`, `GetRegisteredAssemblies(...)`
- Datasource-aware best practices exist:
  - `GetMigrationBestPractices(...)`
  - capability probes in `MigrationManager.Capabilities.cs`
- Helper stacks already available and used by migration flows:
  - `Helpers/UniversalDataSourceHelpers/Core/GeneralDataSourceHelper.cs`
  - `Helpers/UniversalDataSourceHelpers/Core/DataSourceHelperFactory.cs`
  - `Helpers/UniversalDataSourceHelpers/RdbmsHelpers/RdbmsHelper.cs`
  - `Helpers/RDBMSHelpers/RDBMSHelper.cs`

## Focused Gaps
- Path parity: explicit and discovery paths share intent but differ in diagnostics and operation envelope details.
- Readiness/summaries: output is useful for humans but not normalized for CI quality gates.
- Entity DDL hardening: operation semantics are mostly helper-driven; fallback and unsupported-path reporting need stricter contracts.
- Helper alignment: `UniversalDataSourceHelpers.RdbmsHelper` and `RDBMSHelper` overlap in responsibilities; migration evidence should declare which helper path generated SQL.
- Discovery resilience: assembly resolution is broad but not ranked/scored for source trust and repeatability.
- Best-practice retrieval: recommendations are static strings; lacks profile versioning and evidence links.

## Gap Matrix

| Focus Area | Current | Target | Priority |
|---|---|---|---|
| Explicit vs discovery path parity | Both paths exist with partly duplicated flow | Single normalized pipeline model + deterministic result shape | P0 |
| Readiness and summary reporting | Rich narrative issues, mixed machine-readability | Stable contract for CI gates, policy checks, and trend diffs | P0 |
| Entity-level DDL operations | Broad operation coverage, helper dependent | Capability-first execution with explicit fallback taxonomy | P0 |
| Assembly discovery resilience | Multi-source scanning + manual registration | Scored assembly source model + deterministic selection evidence | P1 |
| Datasource-aware best practices | Category/type guidance strings | Versioned recommendation profiles tied to capability probes | P1 |

## Planned Phases
1. Explicit and discovery migration path convergence.
2. Readiness and summary reporting hardening.
3. Entity-level DDL operation hardening.
4. Assembly registration and discovery resilience hardening.
5. Datasource-aware best-practice intelligence hardening.

## Success Criteria
- Same entity set produces equivalent migration decisions under explicit and discovery modes.
- Readiness output can be consumed directly by CI policy gates without string parsing.
- Unsupported DDL operations return deterministic capability-coded responses.
- Discovery path emits reproducible assembly scan evidence.
- Best-practice guidance is profile-versioned and mapped to capability probes.
