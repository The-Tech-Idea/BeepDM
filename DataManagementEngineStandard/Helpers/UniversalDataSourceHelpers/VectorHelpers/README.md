# Vector Helper

## Purpose
This folder provides helper logic for vector-database style providers, including vector similarity search, embedding storage, and ANN (Approximate Nearest Neighbor) operations.

## Key Files
- `VectorDbHelper.cs`: Vector provider helper implementing `IDataSourceHelper` for vector-oriented data sources like ChromaDB, Pinecone, Milvus, and pgvector-enabled PostgreSQL.

## Features
- Vector similarity search (cosine, euclidean, dot product)
- Embedding storage and retrieval
- ANN algorithm support (HNSW, IVF)
- Capability signaling: `SupportsVectorSearch`, `SupportsEmbeddings`, `SupportsANN`

## Usage Notes
- Separate vector similarity capabilities from standard relational features
- Keep embedding/vector field type handling explicit and validated
- Ensure capability checks communicate whether ANN/similarity operations are available

## Related Documentation
- [RDBMS Helpers Reference](../../../Help/rdbms-helpers-reference.html)
- [Datasource Types Reference](../../../Help/datasource-types-reference.html)
