# Phase 1 - Explicit and Discovery Migration Paths

## Objective
Align explicit-type and discovery-based migration APIs so they produce equivalent decisions, diagnostics, and policy behavior.

## Audited Hotspots
- `MigrationManager.ReadinessAndExplicit.cs`
  - `EnsureDatabaseCreatedForTypes(...)`
  - `ApplyMigrationsForTypes(...)`
- `MigrationManager.Discovery.cs`
  - `EnsureDatabaseCreated(...)`
  - `ApplyMigrations(...)`
  - `DiscoverEntityTypes(...)`
- `MigrationManager.Planning.cs`
  - `BuildMigrationPlan(...)`

## Real Constraints to Address
- Similar loops and counters exist in both paths with subtle differences in logging and summary handling.
- Explicit path trusts caller-provided type set; discovery path has scan uncertainty and framework filtering behavior.
- Both paths gate via readiness report, but discovery includes extra scan failure modes.
- Discovery today is assembly-centric; filesystem-driven class discovery needs deterministic parsing and trust boundaries (path allow-list, extension allow-list, duplicate resolution).

## Enhancements
- Introduce a shared internal "entity migration pipeline" abstraction used by both explicit and discovery entry points.
- Normalize result schema:
  - scanned entities
  - processed entities
  - created/updated/skipped/errors
  - blocking policy reasons
- Add source-of-entity metadata in result:
  - `Source = Explicit | DiscoveryAssembly | DiscoveryFileSystem`
  - `AssemblyName`
  - `NamespaceMatchMode`
- Add filesystem discovery mode for entity types:
  - discover from configured folders
  - support `.txt` manifest files containing class entity names (one per line)
  - optional namespace prefixes and assembly hints per line (simple delimiter format)
  - ignore comments/empty lines and emit parse diagnostics
- Add resolver strategy for `.txt` entries:
  - resolve by already loaded assemblies first
  - optional folder assembly load pass (policy-controlled)
  - deterministic duplicate handling (first-wins or highest-trust source policy)
- Require both paths to emit the same summary fields and error code families.

## Manifest Format Example (`.txt`)
- **Purpose**: deterministic filesystem discovery input for class entities.
- **Encoding**: UTF-8 text, one logical record per line.
- **Comments**: lines starting with `#` are ignored.
- **Empty lines**: ignored.
- **Exact syntax**:
  - minimal: `Full.Type.Name`
  - with assembly hint: `Full.Type.Name|AssemblyName`
  - with namespace prefix hint: `Full.Type.Name|AssemblyName|NamespacePrefix`
- **Delimiter**: pipe (`|`), max 3 segments; extra segments are parse error.
- **Whitespace**: trim around each segment before processing.

Example file:
```text
# Entity manifest for migration discovery
MyCompany.Domain.Entities.Customer
MyCompany.Domain.Entities.Order|MyCompany.Domain
MyCompany.Sales.Entities.Invoice|MyCompany.Sales|MyCompany.Sales.Entities
```

Validation/diagnostics:
- `MIG-MANIFEST-001`: invalid segment count (not 1..3).
- `MIG-MANIFEST-002`: empty type name segment.
- `MIG-MANIFEST-003`: unresolved type after resolution pipeline.
- `MIG-MANIFEST-004`: duplicate entity declaration (resolved by configured policy).

## Deliverables
- Refactor design note and method map for shared pipeline extraction.
- Filesystem discovery spec for class-entity text manifests:
  - supported text format and examples
  - parser rules and error codes
  - trust and path policy settings
- Unit test matrix proving path parity on:
  - same entity set
  - missing conversion
  - helper missing
  - blocking readiness errors
  - text manifest parse failures
  - duplicate entity declarations across assembly and file discovery.

## Acceptance Criteria
- Explicit and discovery paths produce structurally identical summary contracts.
- Policy gate behavior (blocking vs warning) is equivalent for same entity metadata.
- Regression tests confirm parity across at least three datasource categories.
- File-based discovery from `.txt` manifests can resolve class entities deterministically and reports unresolved class names with stable error codes.
