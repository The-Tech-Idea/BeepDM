# Cassandra Helper

## Purpose
This folder contains the Cassandra-specific `IDataSourceHelper` implementation for query construction, schema support, and capability reporting.

## Key Files
- `CassandraHelper.cs`: Cassandra dialect helper used by `DataSourceHelperFactory`.

## Usage Notes
- Implement query and metadata behavior using Cassandra-compatible patterns.
- Keep capability flags accurate so higher-level services avoid unsupported operations.
- Align type mapping with the central database type-mapping repository.
