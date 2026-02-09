# Universal Helper Core

## Purpose
This folder contains factory and fallback infrastructure for selecting `IDataSourceHelper` implementations by provider type.

## Key Files
- `DataSourceHelperFactory.cs`: Registers and resolves helper factories by `DataSourceType`.
- `GeneralDataSourceHelper.cs`: Delegates helper operations to the current provider-specific helper.
- `DefaultDataSourceHelper.cs`: Baseline helper behavior when no specialized helper is available.

## Runtime Flow
1. Resolve provider type from connection metadata.
2. Ask `DataSourceHelperFactory` for the matching helper.
3. Route SQL/type/capability operations through `GeneralDataSourceHelper`.
4. Fall back to `DefaultDataSourceHelper` when no specialized helper is registered.

## Extension Guidelines
- Register new helper types through factory registration, not hardcoded switch growth.
- Keep capability checks (`SupportsCapability`) authoritative for caller branching.
- Ensure fallback helper behavior remains safe and explicit.
