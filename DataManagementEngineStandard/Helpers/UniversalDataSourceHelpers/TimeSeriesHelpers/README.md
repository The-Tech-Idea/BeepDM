# Time-Series Helper

## Purpose
Helper functionality for time-series data stores (InfluxDB, TimescaleDB, Prometheus) where time windows, ordering, and retention are first-class constraints.

## Key Files
- `TimeSeriesHelper.cs`: Time-series helper implementing `IDataSourceHelper` for window queries, downsampling, and retention policy translation.

## Features
- Time-series specific query generation
- Data downsampling/aggregation over time
- Retention policy translation
- Timestamp precision and timezone handling
- Window and range query translation
- Capability signaling: `SupportsTimeSeriesQueries`, `SupportsDownsampling`, `SupportsRetentionPolicies`

## Usage Notes
- Treat timestamp precision and timezone handling as explicit inputs
- Expose retention/aggregation capabilities through helper capability checks
- Keep window and range query translation consistent with AppFilter semantics

## Related Documentation
- [Datasource Types Reference](../../../Help/datasource-types-reference.html)
