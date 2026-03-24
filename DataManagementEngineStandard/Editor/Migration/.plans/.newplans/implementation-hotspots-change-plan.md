# MigrationManager Focused Implementation Hotspots Change Plan

## Scope
Focused implementation plan for baseline capabilities in `00-overview-migrationmanager-gap-matrix.md` lines 8-12.

## Hotspot 1: Explicit/Discovery Path Convergence
- **Files**: `MigrationManager.ReadinessAndExplicit.cs`, `MigrationManager.Discovery.cs`
- **Current Risk**: Similar loops diverge in behavior under edge conditions.
- **Planned Change**: Extract shared entity-processing pipeline and unify summary/result contract.

## Hotspot 2: Discovery Result Provenance
- **Files**: `MigrationManager.Discovery.cs`
- **Current Risk**: Entity origin is not formally attached to final decisions.
- **Planned Change**: Add per-entity source metadata (assembly, source channel, namespace match mode).

## Hotspot 3: Readiness Report Machine Contract
- **Files**: `MigrationManager.ReadinessAndExplicit.cs`
- **Current Risk**: CI gating depends on message parsing.
- **Planned Change**: Add stable issue/decision codes and serialized machine-friendly report shape.

## Hotspot 4: Summary Decision Codes
- **Files**: `MigrationManager.Discovery.cs` (`GetMigrationSummary`)
- **Current Risk**: create/update/uptodate buckets lack explicit reason codes.
- **Planned Change**: Add per-entity decision reason and capability context snapshot.

## Hotspot 5: DDL Outcome Classification
- **Files**: `MigrationManager.EntityOperations.cs`
- **Current Risk**: operation results may conflate executed/no-op/unsupported states.
- **Planned Change**: Introduce outcome classification and enforce across all entity DDL methods.

## Hotspot 6: File vs Helper Strategy Evidence
- **Files**: `MigrationManager.EntityOperations.cs`
- **Current Risk**: difficult to audit whether helper SQL or file mutation path was used.
- **Planned Change**: Emit strategy metadata and operation evidence payload for every operation.

## Hotspot 7: Assembly Scan Determinism
- **Files**: `MigrationManager.Discovery.cs`
- **Current Risk**: scan order/source blending can reduce reproducibility.
- **Planned Change**: add deterministic source-precedence ordering and explicit skip reasons.

## Hotspot 8: Loader Exception Diagnostics
- **Files**: `MigrationManager.Discovery.cs` (`LogLoaderExceptions`)
- **Current Risk**: loader failures visible in logs but not connected to migration outcome.
- **Planned Change**: persist grouped loader diagnostics in discovery evidence output.

## Hotspot 9: Capability Probe to Recommendation Link
- **Files**: `MigrationManager.Capabilities.cs`, `MigrationManager.Discovery.cs`
- **Current Risk**: best-practice text not explicitly tied to probe evidence.
- **Planned Change**: map recommendation ids to capability probe outputs and constraints.

## Hotspot 10: Recommendation Profile Versioning
- **Files**: `MigrationManager.Discovery.cs` (`GetMigrationBestPractices`)
- **Current Risk**: recommendations are not versioned for controlled rollout.
- **Planned Change**: introduce recommendation profile id/version and policy overlay support.

## Hotspot 11: Helper Selection Determinism
- **Files**: `Helpers/UniversalDataSourceHelpers/Core/GeneralDataSourceHelper.cs`, `Helpers/UniversalDataSourceHelpers/Core/DataSourceHelperFactory.cs`
- **Current Risk**: helper selection path is implicit in runtime behavior and not included in migration evidence.
- **Planned Change**: record selected helper type and datasource mapping source in readiness and DDL evidence payloads.

## Hotspot 12: Universal vs Legacy RDBMS Capability Drift
- **Files**: `Helpers/UniversalDataSourceHelpers/RdbmsHelpers/RdbmsHelper.cs`, `Helpers/RDBMSHelpers/RDBMSHelper.cs`
- **Current Risk**: capability answers and generated SQL may diverge across helper stacks for the same datasource type.
- **Planned Change**: add parity checks and explicit helper-source tags so migration decisions can be audited and reconciled.
