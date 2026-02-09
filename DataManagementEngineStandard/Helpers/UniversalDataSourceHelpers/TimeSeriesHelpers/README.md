# Time-Series Helper

## Purpose
This folder contains helper functionality for time-series data stores where time windows, ordering, and retention are first-class constraints.

## Key Files
- `TimeSeriesHelper.cs`: Time-series helper implementation.

## Usage Notes
- Treat timestamp precision and timezone handling as explicit inputs.
- Expose retention/aggregation capabilities through helper capability checks.
- Keep window and range query translation consistent with `AppFilter` semantics.
