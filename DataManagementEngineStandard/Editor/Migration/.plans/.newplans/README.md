# MigrationManager Focused Enhancement Plans (.newplans)

Detailed plan pack focused on the five baseline capabilities listed in `00-overview-migrationmanager-gap-matrix.md` lines 8-12:

- explicit-type and discovery-based migration paths
- migration summary and readiness reporting
- entity-level DDL operations (create/drop/rename/alter/index)
- registration of assemblies for discovery resilience
- datasource-aware migration best-practice retrieval

## Execution Order
1. [00-overview-focused-gap-matrix.md](./00-overview-focused-gap-matrix.md)
2. [01-phase1-explicit-and-discovery-migration-paths.md](./01-phase1-explicit-and-discovery-migration-paths.md)
3. [02-phase2-migration-summary-and-readiness-reporting.md](./02-phase2-migration-summary-and-readiness-reporting.md)
4. [03-phase3-entity-level-ddl-operations-hardening.md](./03-phase3-entity-level-ddl-operations-hardening.md)
5. [04-phase4-assembly-registration-and-discovery-resilience.md](./04-phase4-assembly-registration-and-discovery-resilience.md)
6. [05-phase5-datasource-aware-best-practice-intelligence.md](./05-phase5-datasource-aware-best-practice-intelligence.md)
7. [implementation-hotspots-change-plan.md](./implementation-hotspots-change-plan.md)
8. [standards-traceability-matrix.md](./standards-traceability-matrix.md)
9. [risk-register-and-cutover-checklists.md](./risk-register-and-cutover-checklists.md)

## Source Files Audited
- `MigrationManager.ReadinessAndExplicit.cs`
- `MigrationManager.Discovery.cs`
- `MigrationManager.EntityOperations.cs`
- `MigrationManager.AssemblyRegistration.cs`
- `MigrationManager.Capabilities.cs`
- `MigrationManager.Planning.cs`
- `Helpers/UniversalDataSourceHelpers/Core/GeneralDataSourceHelper.cs`
- `Helpers/UniversalDataSourceHelpers/Core/DataSourceHelperFactory.cs`
- `Helpers/UniversalDataSourceHelpers/RdbmsHelpers/RdbmsHelper.cs`
- `Helpers/RDBMSHelpers/RDBMSHelper.cs`

## Primary Outcomes
- Deterministic parity between explicit-type and discovery migration paths.
- Readiness and summary outputs that are policy-grade and CI-consumable.
- Hardened entity DDL operations with clearer capability/fallback semantics.
- Assembly discovery that is explainable, observable, and resilient in plugin-heavy deployments.
- Datasource-aware best-practice recommendations that are versioned and testable.
