# DataManagementEngineStandard

Core implementation of the Beep Data Management Engine.

## Scope
This folder contains the runtime engine, data source implementations, editor/orchestration layers, configuration managers, helper libraries, and documentation assets used by BeepDM.

## Module Map
- `Addin/`: addin extension point (reserved folder).
- `Caching/`: caching providers, cached data sources, and cache manager docs.
- `ConfigUtil/`: configuration orchestration (`ConfigEditor`) and persistence managers.
- `Connections/`: data connection implementations.
- `DataBase/`: shared database-level helper components.
- `DataView/`: view-oriented datasource and connection abstractions.
- `Docs/`: HTML documentation site and navigation assets.
- `Editor/`: orchestration APIs (ETL, defaults, importing, unit of work, mapping, sync).
- `Exensions/`: extension methods and entity diff/batch helpers.
- `FileManager/`: CSV/text file datasource and parsing utilities.
- `Helpers/`: cross-cutting helper libraries (connection, data types, RDBMS, universal datasource).
- `InMemory/`: in-memory datasource implementation.
- `Json/`: JSON datasource and transformation helpers.
- `JsonLoaderService/`: JSON loader abstraction and implementation.
- `Logger/`: logging helper components.
- `Properties/`: resources and local launch settings.
- `Proxy/`: proxy datasource and resilience helpers.
- `Report/`: report data and output services.
- `Roslyn/`: runtime compilation support.
- `Rules/`: built-in rule actions.
- `Services/`: DI and environment registration services.
- `Tools/`: code generation and scaffolding tools.
- `Utils/`: general-purpose utility helpers.
- `WebAPI/`: Web API datasource implementation and helper stack.

## Existing Detailed Docs
- `Docs/index.html`
- `FOLDER_REFERENCE.md`
- `Editor/README.md`
- `Caching/README.md`
- `Proxy/README.md`
- `WebAPI/README.md`

## Notes
- `bin/` and `obj/` are build artifacts and are intentionally undocumented.
- Folder-level README files provide focused entry points for each module.

