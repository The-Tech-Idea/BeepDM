# ETL

ETL orchestration and script generation APIs.

## Key Types
- `ETLEditor`
- `ETLDataCopier`
- `ETLEntityCopyHelper`
- `ETLEntityProcessor`
- `ETLScriptBuilder`
- `ETLScriptManager`
- `ETLValidator`

## Responsibilities
- Build ETL script headers/details from source metadata.
- Generate create/copy scripts for entities.
- Copy entity structures and optionally data across datasources.
- Validate ETL script definitions before execution.
- Manage ETL script persistence and run logs.

## Typical Flow
1. Discover source entities and structures.
2. Generate script details (`GetCreateEntityScript`, copy scripts).
3. Validate script and datasource connectivity.
4. Execute with progress and cancellation support.

## Notes
- ETL operations are coordinated through `IDMEEditor` and datasource abstractions.
- Existing `.howto.readme.md` files in this folder provide task-focused examples.
