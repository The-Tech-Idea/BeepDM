# UOW

Unit of Work implementation for BeepDM stateful CRUD workflows.

## Core Types
- `UnitofWork<T>` (partial: core, CRUD, extensions, utilities)
- `UnitOfWorkWrapper`
- `UnitOfWorkFactory`
- Helper classes under `Helpers/`
- `MultiDataSourceUnitOfWork` (parent folder) for cross-entity coordination

## What `UnitofWork<T>` Provides
- Local state tracking (`InsertedKeys`, `UpdatedKeys`, `DeletedKeys`, `DeletedUnits`).
- Paging and filtered view support (`Units`, `FilteredUnits`).
- Primary key/guid key handling.
- Optional soft delete and optimistic concurrency fields.
- Data validation before commit operations.

## Supporting Helpers
- Collection/data/state/event/validation/default helpers split concerns.
- Interface contracts in `Interfaces/` allow extension and testing.

## Usage Pattern
1. Create via factory or constructor with datasource/entity context.
2. Load records (`Get`) or initialize for add/new flows.
3. Perform add/update/delete operations.
4. Validate and commit.
5. Dispose to release resources.

## Multi-Source Coordination
When parent-child entity consistency is required across units, use `MultiDataSourceUnitOfWork` to manage relationship mapping, navigation, and coordinated commits.
