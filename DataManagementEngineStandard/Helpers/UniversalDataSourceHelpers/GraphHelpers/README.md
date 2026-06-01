# Graph Helper

## Purpose
Graph database helper logic for node/edge oriented providers including Neo4j and ArangoDB. Translates entity relationships into graph traversal semantics and generates Cypher/Gremlin queries.

## Key Files
- `GraphDbHelper.cs`: Graph helper implementing `IDataSourceHelper` for graph traversal, Cypher query generation, and capability signaling.

## Features
- Graph traversal operations
- Cypher query generation (Neo4j)
- Gremlin query generation (CosmosDB, JanusGraph)
- Relationship-to-edge translation
- Capability signaling: `SupportsGraphTraversal`, `SupportsCypherQuery`, `SupportsGremlinQuery`

## Usage Notes
- Translate entity relationships into graph traversal semantics
- Keep graph query generation separate from relational SQL pathways
- Clearly advertise graph-specific capabilities to callers

## Related Documentation
- [RDBMS Helpers Reference](../../../Help/rdbms-helpers-reference.html)
