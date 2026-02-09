# Mapping Configuration

## Purpose
This folder defines option and type-map configuration models used by AutoObjMapper registration and runtime behavior.

## Key Files
- `AutoObjMapperOptions.cs`: Runtime options and mapping statistics controls.
- `MapperConfiguration.cs`: Type-map registry and lookup implementation.
- `MapperConfig.cs`: High-level config facade and registry lifecycle helpers.
- `TypeMapBase.cs` and `TypeMap.cs`: Property ignore/resolver rules plus before/after hooks.

## Runtime Flow
1. Register source/destination maps.
2. Configure per-property rules (`Ignore`, `ForMember`, `BeforeMap`, `AfterMap`).
3. Resolve type-map metadata during mapper execution.

## Extension Guidelines
- Keep type-map keying stable for fast lookups.
- Prefer additive options in `AutoObjMapperOptions`.
- Validate map registration conflicts early.
