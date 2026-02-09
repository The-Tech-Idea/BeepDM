# Editor Docs Source

## Purpose
This folder holds reStructuredText source documents for editor subsystems such as UnitOfWork, mapping, defaults, ETL, and sync. These files are the narrative reference for editor-level APIs.

## Key Files
- `DMEEditor.rst`: Core editor orchestration narrative.
- `UnitofWork.rst`, `UnitOfWorkFactory.rst`, `UnitofWorksManager.rst`: Unit-of-work lifecycle and composition.
- `DataImportManager.rst`, `MappingManager.rst`, `DefaultsManager.rst`, `DataSyncManager.rst`: Manager-focused behavioral docs.
- `ETL.rst`, `ETLEditor.rst`, `Sync.rst`: Integration-focused workflows.

## Maintenance Rules
- Update these docs when public editor APIs or behavior change.
- Keep examples consistent with current method signatures.
- Prefer operational guidance and call flow over generated API dumps.

## Publishing Notes
- Treat this folder as source-of-truth input for generated docs.
- Validate links and cross-references after any file rename.
