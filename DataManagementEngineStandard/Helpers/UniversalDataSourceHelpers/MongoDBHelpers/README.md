# MongoDB Helper

## Purpose
This folder contains the MongoDB-oriented `IDataSourceHelper` implementation used to adapt BeepDM query semantics to document-database operations.

## Key Files
- `MongoDBHelper.cs`: MongoDB helper implementation for mapping, query construction, and capability exposure.

## Usage Notes
- Preserve document-oriented semantics when translating `AppFilter` structures.
- Keep type conversion and key handling aligned with MongoDB conventions.
- Advertise unsupported relational capabilities through capability checks.
