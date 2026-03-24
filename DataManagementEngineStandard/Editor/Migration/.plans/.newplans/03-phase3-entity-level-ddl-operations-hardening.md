# Phase 3 - Entity-Level DDL Operations Hardening

## Objective
Make entity-level DDL operations deterministic across provider types with explicit unsupported-path behavior and fallback evidence.

## Audited Hotspots
- `MigrationManager.EntityOperations.cs`
  - `EnsureEntity(...)`
  - `CreateEntity(...)`
  - `DropEntity(...)`
  - `RenameEntity(...)`
  - `AlterColumn(...)`
  - `DropColumn(...)`
  - `RenameColumn(...)`
  - `CreateIndex(...)`
  - `AddColumn(...)`
- `Helpers/UniversalDataSourceHelpers/RdbmsHelpers/RdbmsHelper.cs`
  - `GenerateCreateTableSql(...)`
  - `GenerateAddColumnSql(...)`
  - `GenerateAlterColumnSql(...)`
  - `GenerateDropColumnSql(...)`
  - `GenerateRenameTableSql(...)`
  - `GenerateRenameColumnSql(...)`
  - `GenerateCreateIndexSql(...)`
- `Helpers/UniversalDataSourceHelpers/Core/GeneralDataSourceHelper.cs`
  - delegation boundary for all DDL methods.
- `Helpers/RDBMSHelpers/RDBMSHelper.cs`
  - facade methods for DDL and feature checks.
- helper-driven SQL generation and file-based operation branches.

## Real Constraints to Address
- Operation behavior depends heavily on helper availability and provider support; current outcomes can vary in detail.
- DDL SQL currently can come from two helper stacks (`RdbmsHelper` vs `RDBMSHelper` facade), making provenance and fallback reporting inconsistent unless explicitly captured.
- File datasource branches use file mutations while RDBMS paths use generated SQL; cross-path evidence is uneven.
- Some "no DDL required" outcomes are success-like but semantically different from true execution.

## Enhancements
- Define standardized operation outcome model:
  - `Executed`
  - `NoOp`
  - `Unsupported`
  - `Emulated`
  - `Failed`
- Add capability pre-check per operation before SQL generation/mutation.
- Add helper-source tagging in operation evidence:
  - `UniversalRdbmsHelper`
  - `LegacyRdbmsFacade`
  - `FileMutation`
- Require emitted operation evidence:
  - operation id
  - entity/column/index targets
  - strategy (`HelperSql`, `FileMutation`, `NoOp`, `Unsupported`)
  - sql hash when SQL is produced
- Add fallback policy for embedded/file providers:
  - explicit table-rebuild guidance path
  - deny-list for unsafe in-place operations.

## Deliverables
- DDL operation contract update proposal.
- operation-to-capability matrix for create/drop/rename/alter/index.
- compatibility test matrix across representative providers and file sources.

## Acceptance Criteria
- Every DDL method returns a classified operation outcome, not only a message.
- Unsupported operations surface deterministic codes and recommendations.
- Audit trail can distinguish helper SQL execution from file emulation paths.
