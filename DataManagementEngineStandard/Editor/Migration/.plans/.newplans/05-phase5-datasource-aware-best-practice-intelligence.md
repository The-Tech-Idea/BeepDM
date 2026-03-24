# Phase 5 - Datasource-Aware Best-Practice Intelligence

## Objective
Turn datasource-aware best-practice retrieval into a versioned, evidence-backed recommendation system that aligns with capability probes and readiness decisions.

## Audited Hotspots
- `MigrationManager.Discovery.cs`
  - `GetMigrationBestPractices(...)`
- `MigrationManager.ReadinessAndExplicit.cs`
  - `CreateBaseReadinessReport(...)`
  - `LogReadinessReport(...)`
- `MigrationManager.Capabilities.cs`
  - `BuildProviderCapabilityProfile(...)`
  - probe methods and constraint builders.
- `Helpers/UniversalDataSourceHelpers/Core/GeneralDataSourceHelper.cs`
  - `SupportsCapability(...)` and helper delegation.
- `Helpers/UniversalDataSourceHelpers/Core/DataSourceHelperFactory.cs`
  - datasource type to helper mapping used by capability probes.
- `Helpers/UniversalDataSourceHelpers/RdbmsHelpers/RdbmsHelper.cs`
  - capability and size/precision constraints surfaced by helper.
- `Helpers/RDBMSHelpers/RDBMSHelper.cs`
  - `SupportsFeature(...)`, `GetSupportedFeatures(...)`, identifier/feature constraints.

## Real Constraints to Address
- Current guidance is robust but text-heavy and not profile-versioned.
- Readiness and best-practice outputs are not formally linked by recommendation ids.
- Capability probe evidence exists but is not surfaced as first-class recommendation provenance.
- Capability facts are split across universal helper capabilities and legacy RDBMS feature helpers, so recommendation provenance needs unified source markers.

## Enhancements
- Introduce recommendation profile model:
  - profile id/version
  - datasource type/category scope
  - recommendation id, text, severity, rationale
  - capability dependencies
- Link readiness issues to recommendation ids.
- Add provider probe evidence references to recommendations.
- Add capability-source attribution per recommendation:
  - `UniversalDataSourceHelpers` probe
  - `RDBMSHelpers` feature probe
  - `MigrationManager` computed constraint
- Add override mechanism for organization-specific policy overlays.

## Deliverables
- Recommendation profile schema and baseline profiles for major categories.
- mapping table: capability constraints -> recommendations.
- sample policy overlay file and merge rules.

## Acceptance Criteria
- Best-practice output includes stable ids and profile version.
- Readiness report can point to exact recommendations by id.
- Recommendation set is testable for each datasource type/category pair.
