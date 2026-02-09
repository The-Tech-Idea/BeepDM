# Mapping

Entity and object mapping utilities.

## Key Components
- `MappingManager` (static facade)
- Auto object mapper stack:
  - `Configuration/*`
  - `Core/*`
  - `Helpers/*`
  - `Utilities/*`

## Responsibilities
- Build entity maps between source and destination entities.
- Auto-map fields by name with type metadata (`FromFieldType`, `ToFieldType`).
- Persist mappings through `ConfigEditor.SaveMappingValues(...)`.
- Transform source objects into destination entity instances.
- Apply defaults post-mapping via mapping helper integration.

## Mapping Persistence
- Stored as JSON under `Config.MappingPath/{datasource}/{entity}_Mapping.json`.
- Loaded through `ConfigEditor.LoadMappingValues(entity, datasource)`.

## Typical Usage
1. Create or load map (`CreateEntityMap`, `LoadMappingValues`).
2. Adjust field map details as needed.
3. Save map.
4. Transform records (`MapObjectToAnother`) during import/migration.

## Safety Notes
- Mapping uses reflection; missing properties are logged and processing continues.
- Validate map definitions before high-volume migrations.
