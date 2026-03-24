# Phase 4 - Resolver Extensibility and Context Model

## Objective
Improve resolver extensibility by formalizing context payloads, resolver capabilities, and execution contracts.

## Scope
- Standardize parameter/context model for resolvers.
- Add resolver capability descriptors (supports async, supports query, deterministic).
- Add resolver chain policy (priority/fallback).

## File Targets
- `Defaults/Interfaces/IDefaultValueInterfaces.cs`
- `Defaults/Resolvers/DefaultValueResolverManager.cs`
- `Defaults/Resolvers/BaseDefaultValueResolver.cs`
- `Defaults/DefaultsManager.cs`

## Planned Enhancements
- Add context abstraction (wrapper over `IPassedArgs` + dictionary extras).
- Add resolver metadata contract:
  - `Priority`
  - `IsDeterministic`
  - `SupportsCaching`
  - `SupportsAsync` (future-ready even if current API remains sync)
- Add controlled resolver precedence and fallback:
  - primary resolver by operator,
  - fallback resolver only when explicit.

## Implementation Rules (Skill Constraints)
- Treat `IDMEEditor` as the core orchestration entry and avoid introducing a parallel resolver-container model (`beepdm`).
- Preserve compatibility for existing custom resolver registrations and interfaces; evolve with adapters/defaults rather than hard breaks (`idatasource` style contract stability).
- Persist resolver metadata and capability profiles through `ConfigEditor` patterns when persistence is required (`configeditor`).
- If resolver metadata needs local storage, use sanctioned `EnvironmentService` app/container paths (`environmentservice`).
- Ensure resolver lifecycle assumptions fit shared service/editor startup patterns (`beepservice`) and do not depend on transient per-form state.

## Acceptance Criteria
- Resolver manager can expose capabilities and precedence.
- Context lookup is consistent across all built-in resolvers.
- Custom resolvers can register with explicit priority and metadata.

## Risks and Mitigations
- Risk: breaking custom resolver implementations.
  - Mitigation: adapter layer and default interface methods/extension approach.
- Risk: ambiguity in operator ownership.
  - Mitigation: duplicate operator detection at registration time.

## Test Plan
- Resolver registration conflict tests.
- Priority routing tests.
- Backward-compat tests for existing custom resolver registrations.
