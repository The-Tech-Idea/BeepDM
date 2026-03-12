# BeepDM Skills Reference

This directory contains the BeepDM skill catalog used to route work across the current codebase. The catalog is organized around entry-point skills, narrower subsystem skills, and helper-focused routing skills.

## Catalog Rules

- Keep `SKILL.md` short and routing-focused.
- Keep `reference.md` for deeper API lists, examples, and troubleshooting.
- Update both files together when a skill changes materially.
- Prefer tightening an existing skill before creating a near-duplicate.
- Ground every skill in real source files and current type names.

## How To Choose A Skill

Start with the narrowest skill that matches the task:
- Use [`beepdm`](beepdm/) for cross-cutting `IDMEEditor` orchestration and routing.
- Use [`beepserviceregistration`](beepserviceregistration/) for DI and application startup.
- Use [`beepservice`](beepservice/) for legacy desktop bootstrapping.
- Use subsystem skills only when the task is clearly isolated to that subsystem.

## Core Entry Points

### Framework Core
- [`beepdm`](beepdm/) - top-level BeepDM routing around `IDMEEditor`, `IDataSource`, helpers, config, migrations, ETL, and forms.
- [`beepserviceregistration`](beepserviceregistration/) - modern service-registration and environment-specific startup.
- [`beepservice`](beepservice/) - legacy initialization and desktop-hosted patterns.

### Connection And Configuration
- [`connectionproperties`](connectionproperties/) - build and classify `ConnectionProperties`.
- [`connection`](connection/) - driver resolution, connection-string processing, validation, and masking.
- [`configeditor`](configeditor/) - persisted configuration, delegated managers, and config synchronization.
- [`environmentservice`](environmentservice/) - environment-specific settings and app-folder behavior.

### Data Contracts And Runtime
- [`idatasource`](idatasource/) - datasource contract implementation and usage.
- [`unitofwork`](unitofwork/) - service-layer persistence, change tracking, and runtime wrappers.
- [`migration`](migration/) - schema creation and upgrade flows.
- [`mapping`](mapping/) - entity and field mapping logic.

## Domain Skills

### ETL, Import, And Sync
- [`etl`](etl/) - direct ETL orchestration, script generation, and datasource copy flows.
- [`importing`](importing/) - governed imports with validation, batching, replay, quality, staging, history, and watermarks.
- [`beepsync`](beepsync/) - sync-schema orchestration via `BeepSyncManager`, `DataSyncSchema`, and `DataImportManager`.

### Forms
- [`forms`](forms/) - entry point for `FormsManager`.
- [`forms-mode-transitions`](forms-mode-transitions/) - query/CRUD mode flow.
- [`forms-operations-navigation`](forms-operations-navigation/) - form lifecycle and navigation.
- [`forms-enhanced-data-operations`](forms-enhanced-data-operations/) - enhanced CRUD/query helpers.
- [`forms-helper-managers`](forms-helper-managers/) - relationship, dirty-state, trigger, and simulation helpers.
- [`forms-performance-configuration`](forms-performance-configuration/) - cache, metrics, and configuration management.

### Local And UI-Oriented Data
- [`inmemorydb`](inmemorydb/) - in-memory datasource behavior.
- [`localdb`](localdb/) - local/embedded database scenarios.
- [`observablebindinglist`](observablebindinglist/) - binding-oriented collection behavior.

## Helper Clusters

### Assembly And Plugin Loading
- [`assemblyhandler`](assemblyhandler/) - classic assembly-handler entry point.
- [`assemblyhandler-loading-scanning`](assemblyhandler-loading-scanning/) - load orchestration and scanning.
- [`assemblyhandler-helpers-reflection`](assemblyhandler-helpers-reflection/) - reflection, type resolution, and driver discovery helpers.
- [`assemblyhandler-nuget-operations`](assemblyhandler-nuget-operations/) - NuGet search/load/source operations.
- [`assemblyhandler-driver-statistics`](assemblyhandler-driver-statistics/) - driver provenance and load metrics.

### Shared-Context Plugin System
- [`shared-context-assemblyhandler`](shared-context-assemblyhandler/) - shared-context handler entry point.
- [`shared-context-loading-resolution`](shared-context-loading-resolution/) - shared-context load and binding behavior.
- [`shared-context-scanning-discovery`](shared-context-scanning-discovery/) - discovery services and shared-context scanning.
- [`shared-context-plugin-system`](shared-context-plugin-system/) - plugin-system managers and lifecycle.
- [`shared-context-nuget-source-tracking`](shared-context-nuget-source-tracking/) - package loading, source persistence, and provenance in shared-context mode.

### RDBMS Helpers
- [`rdbms-helper-facade`](rdbms-helper-facade/) - static `RDBMSHelper` routing layer.
- [`rdbms-schema-query-helper`](rdbms-schema-query-helper/) - schema and metadata query generation.
- [`rdbms-query-repository-helper`](rdbms-query-repository-helper/) - predefined query repository behavior.
- [`rdbms-object-creation-helper`](rdbms-object-creation-helper/) - DDL and object creation helpers.
- [`rdbms-dml-helper`](rdbms-dml-helper/) - DML query generation.
- [`rdbms-feature-helper`](rdbms-feature-helper/) - provider capability and feature checks.
- [`rdbms-entity-validation`](rdbms-entity-validation/) - entity validation and naming/keyword safety.
- [`rdbms-entity-helper-sql`](rdbms-entity-helper-sql/) - entity-structure-to-SQL generation.
- [`universal-rdbms-helper`](universal-rdbms-helper/) - instance-based universal `RdbmsHelper` implementing `IDataSourceHelper`.

## Other Skills

- [`importing`](importing/) and [`etl`](etl/) overlap intentionally but serve different layers:
  - `etl` for direct copy/script execution
  - `importing` for governed import pipelines
- [`helper-skill-template`](helper-skill-template/) is the local authoring template for new or upgraded BeepDM skills.
- [`universal-helper-factory`](universal-helper-factory/) and [`universal-general-helper`](universal-general-helper/) remain helper-routing skills for the universal helper layer.

## Skill Structure

Each skill directory should contain:
- `SKILL.md` for routing, scope, workflow, pitfalls, and related skills
- `reference.md` for deeper examples, method inventories, and troubleshooting

Recommended split:
- Keep `SKILL.md` under roughly 150 lines unless the subsystem genuinely needs more routing guidance.
- Put long examples, option matrices, and method inventories in `reference.md`.

## Maintenance Loop

When updating a skill:
1. Verify the source files and public APIs in the repository.
2. Tighten trigger language so the skill routes to the narrowest useful area.
3. Add or fix related-skill links.
4. Remove duplicated detail from `SKILL.md` when `reference.md` can own it.
5. Update this README if the catalog entry points or grouping changed.

## Version Information

- Skills Version: 2.0
- Last Updated: 2026-03-11
- BeepDM Version: 2.0+
- .NET Version: 8.0+
