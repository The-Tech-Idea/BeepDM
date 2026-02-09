# Defaults Interfaces

## Purpose
This folder contains contracts for default-value resolution, management, and validation in editor and unit-of-work pipelines.

## Key Interfaces
- `IDefaultValueResolver`: Resolves a rule token into a concrete value.
- `IDefaultValueHelper`: Loads, saves, and queries defaults.
- `IDefaultValueResolverManager`: Resolver registration and dispatch contract.
- `IDefaultValueValidationHelper`: Rule and field validation contract.

## Integration Notes
- Resolver implementations should be deterministic for the same rule and context.
- Keep rule parsing isolated in resolvers; manager logic should focus on dispatch.
- Validation helpers should return rich `IErrorsInfo` payloads, not throw for expected rule issues.
