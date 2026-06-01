# MongoDB Helper

## Purpose
MongoDB-oriented `IDataSourceHelper` implementation adapting BeepDM query semantics to document-database operations with BSON handling and aggregation pipeline support.

## Key Files
- `MongoDBHelper.cs`: MongoDB helper for AppFilter translation, aggregation pipeline construction, and capability exposure.

## Features
- AppFilter to MongoDB query translation
- Aggregation pipeline support ($lookup, $group, $project)
- BSON type conversion and document handling
- ObjectId/long path mappings
- Capability signaling: `SupportsAggregations`, `SupportsTTL`, `SupportsTransactions`

## Usage Notes
- Preserve document-oriented semantics when translating AppFilter structures
- Keep type conversion and key handling aligned with MongoDB conventions
- Advertise unsupported relational capabilities through capability checks

## Related Documentation
- [RDBMS Helpers Reference](../../../Help/rdbms-helpers-reference.html)
