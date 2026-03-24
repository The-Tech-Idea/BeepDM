# Phase 4 - Assembly Registration and Discovery Resilience

## Objective
Harden assembly discovery and registration so discovery-based migration stays reproducible in plugin-heavy and multi-project solutions.

## Audited Hotspots
- `MigrationManager.AssemblyRegistration.cs`
  - `RegisterAssembly(...)`
  - `RegisterAssemblies(...)`
  - `GetRegisteredAssemblies(...)`
- `MigrationManager.Discovery.cs`
  - `GetSearchableAssemblies(...)`
  - `ShouldScanAssembly(...)`
  - `IsFrameworkAssemblyName(...)`
  - `LogLoaderExceptions(...)`
  - `DiscoverEntityTypes(...)`

## Real Constraints to Address
- Discovery currently blends manual registrations, entry/calling references, app domain assemblies, and assembly-handler plugins.
- Broad loading improves coverage but can increase nondeterminism and scan noise.
- Reflection load failures are logged, but scan source ranking is not explicit in outputs.

## Enhancements
- Add assembly-source classification:
  - `ManualRegistered`
  - `EntryReference`
  - `CallingReference`
  - `AppDomainLoaded`
  - `AssemblyHandlerPlugin`
- Add deterministic scan ordering with source precedence and tie-breaks.
- Add discovery evidence payload:
  - scanned assemblies
  - skipped assemblies + reason
  - loader exceptions grouped by assembly
  - entity type origin map.
- Add namespace scope diagnostics when zero entities found.

## Deliverables
- Discovery execution spec with source precedence rules.
- Discovery evidence model and log/event examples.
- Regression test scenarios:
  - explicit assembly provided
  - plugin-only entity source
  - partial reflection load failure.

## Acceptance Criteria
- Same environment and inputs produce same assembly scan order and entity result set.
- Discovery reports include source and skip reasons for each assembly.
- Troubleshooting of "no entities found" can be done without enabling ad hoc debug logging.
