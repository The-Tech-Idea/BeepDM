# Cassandra Helper

## Purpose
Cassandra-specific helper implementing `IDataSourceHelper` for wide-column operations, CQL generation, and capability reporting.

## Key Files
- `CassandraHelper.cs`: Cassandra dialect helper used by `DataSourceHelperFactory`.

## Features
- CQL (Cassandra Query Language) generation
- Wide-column schema support
- Partition key-aware queries
- No JOIN support (reflected via capability checks)
- Eventual consistency patterns

## Usage Notes
- Implement query and metadata behavior using Cassandra-compatible patterns
- Keep capability flags accurate so higher-level services avoid unsupported operations
- Align type mapping with the central database type-mapping repository

## Related Documentation
- [RDBMS Helpers Reference](../../../Help/rdbms-helpers-reference.html)
